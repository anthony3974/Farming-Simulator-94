using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// A building with a trigger zone. Press the interact key (E / USE) inside the zone to run
/// the building's action. Same pattern as FieldController: only the last zone entered reacts.
/// </summary>
public class Building : MonoBehaviour
{
    public enum BuildingType { Sell, Buy, Paint, Dealer, Gas }
    public enum Crop { Wheat, Barley, Oat }

    public InputManager inputManager;
    public PlayerValues playerValues;

    public BuildingType type = BuildingType.Sell;
    [Tooltip("Which crop this station buys (Sell type only).")]
    public Crop crop = Crop.Wheat;

    [Tooltip("Price per unit is random between these two values on each sale (2D game: 0.65 to 0.75).")]
    public float minPrice = 0.65f;
    public float maxPrice = 0.75f;

    [Tooltip("Optional. Text object used for the short sale message.")]
    public TMP_Text messageText;
    public float messageSeconds = 3f;

    private static readonly List<Building> buildingsInside = new();
    private static ScreenPrompt prompt;
    private string flashText;
    private float flashUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { buildingsInside.Clear(); prompt = null; }

    void Start()
    {
        if (inputManager == null || playerValues == null)
        {
            Debug.LogError($"Building '{name}' is missing a reference (inputManager or playerValues).", this);
            enabled = false;
            return;
        }
        if (messageText != null) messageText.text = "";
        if (prompt == null) prompt = ScreenPrompt.Instance;
    }

    void Update()
    {
        if (inputManager.Interact && buildingsInside.Count > 0 && buildingsInside[^1] == this)
            Interact();

        UpdatePrompt();
    }

    private void UpdatePrompt()
    {
        if (prompt == null || buildingsInside.Count == 0 || buildingsInside[^1] != this) return;
        prompt.Show(Time.time < flashUntil ? flashText : PromptText());
    }

    private string PromptText()
    {
        string key = inputManager.TouchControlsActive ? "USE" : "E";
        return type == BuildingType.Sell ? $"Press {key} to sell {crop}" : $"{type} (coming soon)";
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        buildingsInside.Remove(this);
        buildingsInside.Add(this);
        flashUntil = 0;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        buildingsInside.Remove(this);
        if (buildingsInside.Count == 0 && prompt != null) prompt.Hide();
    }

    private void OnDisable()
    {
        buildingsInside.Remove(this);
        if (buildingsInside.Count == 0 && prompt != null) prompt.Hide();
    }

    private void Interact()
    {
        switch (type)
        {
            case BuildingType.Sell: Sell(); break;
            default: Debug.Log($"{type} building is not implemented yet."); break;
        }
    }

    private void Sell()
    {
        int amount;
        switch (crop)
        {
            case Crop.Wheat: amount = playerValues.InWheat; playerValues.InWheat = 0; break;
            case Crop.Barley: amount = playerValues.InBarley; playerValues.InBarley = 0; break;
            default: amount = playerValues.InOat; playerValues.InOat = 0; break;
        }

        if (amount <= 0)
        {
            ShowMessage($"No {crop} to sell");
            return;
        }

        float profit = amount * Random.Range(minPrice, maxPrice);
        playerValues.Money += profit;
        ShowMessage($"{crop} Sold, Profit ({profit:N0}$)");
    }

    private void ShowMessage(string text)
    {
        Debug.Log(text);
        flashText = text;
        flashUntil = Time.time + messageSeconds;
        if (messageText == null) return;
        StopAllCoroutines();
        StartCoroutine(MessageRoutine(text));
    }

    private IEnumerator MessageRoutine(string text)
    {
        messageText.text = text;
        yield return new WaitForSeconds(messageSeconds);
        messageText.text = "";
    }
}

/// <summary>A small text box at the bottom of the screen, built at runtime (no scene setup needed).</summary>
public class ScreenPrompt : MonoBehaviour
{
    private GameObject panel;
    private TMP_Text label;
    private float flashUntil;

    private static ScreenPrompt instance;

    /// <summary>The shared on-screen box (created on first use).</summary>
    public static ScreenPrompt Instance
    {
        get { if (instance == null) instance = Create(); return instance; }
    }

    /// <summary>Show a message for a few seconds, then hide.</summary>
    public void Flash(string text, float seconds)
    {
        Show(text);
        flashUntil = Time.time + seconds;
    }

    void Update()
    {
        if (flashUntil > 0 && Time.time >= flashUntil) { flashUntil = 0; Hide(); }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    public static ScreenPrompt Create()
    {
        var canvasGo = new GameObject("BuildingPrompt", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var p = canvasGo.AddComponent<ScreenPrompt>();

        // gold border (outer) that grows with the text, dark panel inside it
        p.panel = new GameObject("Border", typeof(RectTransform), typeof(UnityEngine.UI.Image),
            typeof(UnityEngine.UI.VerticalLayoutGroup), typeof(UnityEngine.UI.ContentSizeFitter));
        p.panel.transform.SetParent(canvasGo.transform, false);
        var rt = (RectTransform)p.panel.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(760, 0);
        p.panel.GetComponent<UnityEngine.UI.Image>().color = new Color(1f, 0.8f, 0f, 1f);
        var outer = p.panel.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        outer.padding = new RectOffset(4, 4, 4, 4);
        outer.childControlWidth = outer.childControlHeight = true;
        outer.childForceExpandWidth = true;   // inner panel fills the border's full width
        outer.childForceExpandHeight = false;
        p.panel.GetComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

        var inner = new GameObject("Panel", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.VerticalLayoutGroup));
        inner.transform.SetParent(p.panel.transform, false);
        inner.GetComponent<UnityEngine.UI.Image>().color = new Color32(60, 45, 20, 255);
        var lg = inner.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        lg.padding = new RectOffset(24, 24, 14, 14);
        lg.childControlWidth = lg.childControlHeight = true;
        lg.childForceExpandWidth = true;
        lg.childForceExpandHeight = false;

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(inner.transform, false);
        p.label = textGo.GetComponent<TextMeshProUGUI>();
        p.label.alignment = TextAlignmentOptions.Center;
        p.label.fontSize = 30;
        p.label.color = Color.white;

        p.panel.SetActive(false);
        return p;
    }

    public void Show(string text)
    {
        label.text = text;
        if (!panel.activeSelf) panel.SetActive(true);
    }

    public void Hide() => panel.SetActive(false);
}
