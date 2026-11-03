using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// A building with a trigger zone. Press the interact key (E / USE) inside the zone to run
/// the building's action. Same pattern as FieldController: only the last zone the driven vehicle
/// entered reacts.
/// </summary>
public class Building : MonoBehaviour
{
    // the scene stores these as numbers: add new types at the end only
    public enum BuildingType { Sell, Buy, Paint, Dealer, Gas, Fertilizer }
    public enum Crop { Wheat, Barley, Oat }

    public InputManager inputManager;
    public PlayerValues playerValues;

    public BuildingType type = BuildingType.Sell;
    [Tooltip("Which crop this station buys (Sell type only).")]
    public Crop crop = Crop.Wheat;

    [Tooltip("Price per unit is random between these two values on each sale (2D game: 0.65 to 0.75).")]
    public float minPrice = 0.65f;
    public float maxPrice = 0.75f;

    [Header("Seed shop (Buy type only)")]
    [Tooltip("Seeds you get per purchase. The 2D game sold 1,000 for 450 to 550 dollars (about 0.5 dollars per seed); fields are much bigger in 3D, so a purchase is 10,000.")]
    public int seedsPerPurchase = 10000;
    [Tooltip("Cost of one purchase is random between these two values (about 0.5 dollars per seed).")]
    public float minSeedCost = 4500f;
    public float maxSeedCost = 5500f;

    [Header("Fertilizer shop (Fertilizer type only)")]
    [Tooltip("Fertilizer you get per purchase. The 2D game sold 1,000 compost for 400 to 500 dollars; 3D fields are about 10x bigger, so a purchase is 10,000.")]
    public int fertilizerPerPurchase = 10000;
    [Tooltip("Cost of one purchase is random between these two values (about 0.45 dollars per unit).")]
    public float minFertilizerCost = 4000f;
    public float maxFertilizerCost = 5000f;

    [Header("Dealer (Dealer type only)")]
    [Tooltip("The dealer that opens its shop menu here. Empty = the Dealer on this object or a parent.")]
    public Dealer dealer;

    [Tooltip("Optional. Text object used for the short sale message.")]
    public TMP_Text messageText;
    public float messageSeconds = 3f;

    private static ScreenPrompt prompt;
    private string promptText; // built once per visit, not every frame

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { prompt = null; }

    // the driven vehicle is in this zone, and it is the last building zone it entered
    private bool IsCurrent => Vehicle.Active != null && Vehicle.Active.CurrentZone<Building>() == this;

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
        if (inputManager.Interact && IsCurrent)
            Interact();

        UpdatePrompt();
    }

    private void UpdatePrompt()
    {
        if (prompt == null || !IsCurrent) return;

        // a message (from here or from a field) is on screen: leave it alone until it ends
        if (prompt.IsFlashing) return;

        promptText ??= PromptText();
        if (!prompt.Visible || prompt.CurrentText != promptText) prompt.Show(promptText);
    }

    private string PromptText()
    {
        string key = inputManager.TouchControlsActive ? "USE" : "E";
        switch (type)
        {
            case BuildingType.Sell: return $"Press {key} to sell {crop}";
            case BuildingType.Buy: return $"Press {key} to buy {seedsPerPurchase:N0} seeds (about $ {(minSeedCost + maxSeedCost) / 2f:N0})";
            case BuildingType.Dealer: return $"Press {key} to buy vehicles";
            case BuildingType.Fertilizer: return $"Press {key} to buy {fertilizerPerPurchase:N0} fertilizer (about $ {(minFertilizerCost + maxFertilizerCost) / 2f:N0})";
            default: return $"{type} (coming soon)";
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Vehicle.TryGet(other, out var vehicle)) return;
        vehicle.EnterZone(this);
        promptText = null;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!Vehicle.TryGet(other, out var vehicle)) return;
        vehicle.ExitZone(this);
        HideIfNone();
    }

    private void OnEnable() => Vehicle.ActiveChanged += OnVehicleSwitched;

    private void OnDisable()
    {
        Vehicle.ActiveChanged -= OnVehicleSwitched;
        HideIfNone();
    }

    private void OnVehicleSwitched()
    {
        promptText = null;
        HideIfNone();
    }

    // the driven vehicle is in no building zone: hide the box, but never cut off a message that is still showing
    private void HideIfNone()
    {
        bool inside = Vehicle.Active != null && Vehicle.Active.CurrentZone<Building>() != null;
        if (!inside && prompt != null && !prompt.IsFlashing) prompt.Hide();
    }

    private void Interact()
    {
        switch (type)
        {
            case BuildingType.Sell: Sell(); break;
            case BuildingType.Buy: BuySeeds(); break;
            case BuildingType.Dealer: OpenDealer(); break;
            case BuildingType.Fertilizer: BuyFertilizer(); break;
            default: Debug.Log($"{type} building is not implemented yet."); break;
        }
    }

    private void OpenDealer()
    {
        if (dealer == null) dealer = GetComponentInParent<Dealer>();
        if (dealer != null) dealer.OpenMenu();
        else Debug.LogWarning($"Dealer building '{name}' has no Dealer to open.", this);
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

    private void BuySeeds()
    {
        int cost = Mathf.RoundToInt(Random.Range(minSeedCost, maxSeedCost));
        if (playerValues.Money < cost)
        {
            ShowMessage($"Not enough money. {seedsPerPurchase:N0} seeds cost $ {cost:N0}");
            return;
        }

        playerValues.Money -= cost;
        playerValues.Seeds += seedsPerPurchase;
        ShowMessage($"Bought {seedsPerPurchase:N0} seeds for $ {cost:N0}");
    }

    private void BuyFertilizer()
    {
        int cost = Mathf.RoundToInt(Random.Range(minFertilizerCost, maxFertilizerCost));
        if (playerValues.Money < cost)
        {
            ShowMessage($"Not enough money. {fertilizerPerPurchase:N0} fertilizer costs $ {cost:N0}");
            return;
        }

        playerValues.Money -= cost;
        playerValues.Fertilizer += fertilizerPerPurchase;
        ShowMessage($"Bought {fertilizerPerPurchase:N0} fertilizer for $ {cost:N0}");
    }

    private void ShowMessage(string text)
    {
        Debug.Log(text);
        if (prompt != null) prompt.Flash(text, messageSeconds);
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

    /// <summary>True while a timed message from <see cref="Flash"/> is showing.</summary>
    public bool IsFlashing => flashUntil > 0;

    public bool Visible => panel.activeSelf;
    public string CurrentText => label.text;

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
