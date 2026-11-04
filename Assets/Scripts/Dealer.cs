using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The vehicle dealer: the red L-shaped building in the top-left corner of the map. Press E / USE in its
/// lot to open the shop menu. The van and the harvester are real objects in the scene (Van.prefab and
/// Harvester.prefab, parked on "Parking" by the house); buying one makes that vehicle yours, where it stands.
/// Switch to it with 1 / 2 / 3 (CAR on touch). Vehicles are red (their paint material); there is no colour choice.
/// The menu is built here at runtime: the title is dealerName, each row's name, price and description come
/// from VehicleCatalog (VehicleCatalog.cs), and the money, footer and button texts are in Build / Refresh below.
/// </summary>
public class Dealer : MonoBehaviour
{
    public string dealerName = "Vehicle Dealer";
    public PlayerValues playerValues;
    public InputManager inputManager;
    [Tooltip("Empty = found at runtime.")]
    public GameCharacter player;

    private static readonly Color Gold = new Color32(255, 201, 51, 255);
    private static readonly Color Yellow = new Color32(255, 204, 0, 255);
    private static readonly Color Panel = new Color32(31, 31, 31, 245);
    private static readonly Color ButtonFill = new Color32(60, 45, 20, 255);
    private static readonly Color ButtonOff = new Color32(70, 70, 70, 255);

    private GameObject root;
    private TMP_Text moneyText;
    private readonly List<(VehicleCatalog.Info info, Button button, TMP_Text label)> rows = new();
    private int openedFrame;

    public bool IsOpen => root != null && root.activeSelf;

    void Start()
    {
        if (player == null) player = FindFirstObjectByType<GameCharacter>();
        if (playerValues == null) playerValues = FindFirstObjectByType<PlayerValues>();
        if (inputManager == null) inputManager = FindFirstObjectByType<InputManager>();
    }

    void Update()
    {
        if (!IsOpen || Time.frameCount == openedFrame) return;

        // the game keys are blocked while the menu is open, so read the close keys directly
        if (Input.GetKeyDown(KeyCode.Escape)) Close(relockMouse: false);
        else if (Input.GetKeyDown(KeyCode.E)) Close(relockMouse: true);
    }

    public void OpenMenu()
    {
        if (player == null || playerValues == null || inputManager == null)
        {
            Debug.LogError("Dealer is missing a reference (player, playerValues or inputManager).", this);
            return;
        }
        if (root == null) Build();

        root.SetActive(true);
        openedFrame = Time.frameCount;
        inputManager.MenuOpen = true;
        player.SetCursorLocked(false);
        Refresh();
    }

    private void Close(bool relockMouse)
    {
        root.SetActive(false);
        inputManager.MenuOpen = false;
        if (relockMouse && !inputManager.TouchControlsActive) player.SetCursorLocked(true);
    }

    private void OnDisable()
    {
        if (IsOpen) Close(relockMouse: false);
    }

    // ---- buying ----

    private void Buy(VehicleCatalog.Info info)
    {
        var box = ScreenPrompt.Instance;
        if (player.Owns(info.kind)) return;

        if (playerValues.Money < info.price)
        {
            box.Flash($"Not enough money. The {info.name} costs $ {info.price:N0}", 3f);
            return;
        }
        var vehicle = FindParked(info.kind);
        if (vehicle == null)
        {
            Debug.LogError($"Dealer: there is no {info.name} in the scene to sell. Run Tools > Vehicle Prefabs, or drag " +
                           $"Assets/Prefabs/{info.name}.prefab into the scene.", this);
            box.Flash($"Sorry, there is no {info.name} on the map right now.", 3f);
            return;
        }

        playerValues.Money -= info.price;
        player.AddVehicle(vehicle);
        Close(relockMouse: true);

        string how = inputManager.TouchControlsActive ? "tap CAR" : $"press {(int)info.kind + 1}";
        box.Flash($"Bought the {info.name} for $ {info.price:N0}.\nIt is parked by your house, {how} to drive it.", 5f);
    }

    // the van or harvester standing in the scene (you can only own one of each, so any one of that kind)
    private static Vehicle FindParked(VehicleKind kind)
    {
        foreach (var v in FindObjectsByType<Vehicle>(FindObjectsSortMode.None))
            if (v.kind == kind) return v;
        return null;
    }

    // ---- menu ----

    private void Refresh()
    {
        moneyText.text = $"Your money: $ {playerValues.Money:N0}";
        foreach (var (info, button, label) in rows)
        {
            bool owned = player.Owns(info.kind);
            button.interactable = !owned;
            button.GetComponent<Image>().color = owned ? ButtonOff : ButtonFill;
            label.text = owned ? "OWNED" : $"BUY  $ {info.price:N0}";
        }
    }

    private void Build()
    {
        bool touch = inputManager.TouchControlsActive;

        var canvasGo = new GameObject("DealerMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60; // above the touch buttons (50) and the message box (50)
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        root = new GameObject("DealerMenu", typeof(RectTransform));
        root.transform.SetParent(canvasGo.transform, false);
        Stretch((RectTransform)root.transform);

        // full-screen dimmer; tapping outside the panel closes the menu (the panel is a sibling, so taps on it do not)
        var dimmer = new GameObject("Dimmer", typeof(RectTransform), typeof(Image), typeof(Button));
        dimmer.transform.SetParent(root.transform, false);
        Stretch((RectTransform)dimmer.transform);
        dimmer.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        dimmer.GetComponent<Button>().onClick.AddListener(() => Close(relockMouse: true));

        // yellow border that grows with the content, dark panel inside it
        var border = new GameObject("Border", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        border.transform.SetParent(root.transform, false);
        var borderRt = (RectTransform)border.transform;
        borderRt.anchorMin = borderRt.anchorMax = borderRt.pivot = new Vector2(0.5f, 0.5f);
        borderRt.sizeDelta = new Vector2(1100, 0);
        border.GetComponent<Image>().color = Yellow;
        var outer = border.GetComponent<VerticalLayoutGroup>();
        outer.padding = new RectOffset(4, 4, 4, 4);
        outer.childControlWidth = outer.childControlHeight = true;
        outer.childForceExpandWidth = true;
        outer.childForceExpandHeight = false;
        border.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var inner = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        inner.transform.SetParent(border.transform, false);
        inner.GetComponent<Image>().color = Panel;
        var lg = inner.GetComponent<VerticalLayoutGroup>();
        lg.padding = new RectOffset(34, 34, 22, 22);
        lg.spacing = 14;
        lg.childControlWidth = lg.childControlHeight = true;
        lg.childForceExpandWidth = true;
        lg.childForceExpandHeight = false;

        Height(MakeText(inner.transform, dealerName.ToUpper(), 44, Gold, TextAlignmentOptions.Center, true), 60);
        moneyText = MakeText(inner.transform, "", 28, Color.white, TextAlignmentOptions.Center, false);
        Height(moneyText, 40);

        foreach (var info in VehicleCatalog.ForSale)
            AddVehicleRow(inner.transform, info);

        var footer = MakeText(inner.transform, touch ? "Tap outside to close" : "Click BUY.  E or Esc to close", 26, Color.white, TextAlignmentOptions.Center, false);
        Height(footer, 44);

        root.SetActive(false);
    }

    private void AddVehicleRow(Transform parent, VehicleCatalog.Info info)
    {
        var row = new GameObject(info.name + " Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);
        var h = row.GetComponent<HorizontalLayoutGroup>();
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = h.childForceExpandHeight = false;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.spacing = 24;

        var text = MakeText(row.transform, $"<color=#FFC933><b>{info.name}</b></color>\n<size=80%>{info.description}</size>", 32, Color.white, TextAlignmentOptions.Left, false);
        var le = text.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = 720;
        le.flexibleWidth = 1;

        var (button, label) = MakeButton(row.transform, "BUY", 260, 90);
        button.onClick.AddListener(() => Buy(info));
        rows.Add((info, button, label));
    }

    // ---- UI helpers ----

    private (Button, TMP_Text) MakeButton(Transform parent, string text, float width, float height)
    {
        var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        var le = go.GetComponent<LayoutElement>();
        le.preferredWidth = le.minWidth = width;
        le.preferredHeight = le.minHeight = height;
        go.GetComponent<Image>().color = ButtonFill;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = Yellow;
        outline.effectDistance = new Vector2(3, -3);

        var label = MakeText(go.transform, text, 30, Gold, TextAlignmentOptions.Center, true);
        Stretch((RectTransform)label.transform);
        label.raycastTarget = false;
        return (go.GetComponent<Button>(), label);
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void Height(TMP_Text t, float min)
    {
        t.gameObject.AddComponent<LayoutElement>().minHeight = min;
    }

    private static TMP_Text MakeText(Transform parent, string text, float size, Color color, TextAlignmentOptions align, bool bold)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        return t;
    }
}
