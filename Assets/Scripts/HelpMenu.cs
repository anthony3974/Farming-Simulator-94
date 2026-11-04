using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A pop-up listing the controls (keyboard and touch). Press H, or the HELP button on touch
/// devices, to open and close it. Built at runtime, added automatically by InputManager.
/// </summary>
public class HelpMenu : MonoBehaviour
{
    private static readonly Color Gold = new Color32(255, 201, 51, 255);
    private static readonly Color Yellow = new Color32(255, 204, 0, 255);
    private static readonly Color Panel = new Color32(31, 31, 31, 245);

    private const float ActionWidth = 560f;
    private const float KeyboardWidth = 360f;
    private const float TouchWidth = 220f;
    private const float SidePadding = 34f; // border 4 + inner padding 30 on each side

    // action, keyboard, touch button ("-" = no touch equivalent)
    private static readonly string[][] Rows =
    {
        new[] { "Drive forward / back", "W / S or Up / Down", "GAS / BRAKE" },
        new[] { "Steer left / right", "A / D or Left / Right", "< / >" },
        new[] { "Look around", "Mouse", "-" },
        new[] { "Use (plant, harvest, sell, shops)", "E", "USE" },
        new[] { "Buy / sell field", "B", "BUY/SELL" },
        new[] { "Choose crop", "C", "CROP" },
        new[] { "Fertilize field (while it grows)", "F", "FERT" },
        new[] { "Switch vehicle (tractor, van, harvester)", "1 / 2 / 3", "CAR" },
        new[] { "Help", "H", "HELP" },
        new[] { "Release mouse", "Esc", "-" },
        new[] { "Debug toggle", "F1", "-" },
    };

    private InputManager input;
    private GameObject root;

    void Start()
    {
        input = GetComponent<InputManager>();
        if (input == null) input = FindFirstObjectByType<InputManager>();
        Build();
    }

    void Update()
    {
        if (input != null && input.Help && root != null)
            root.SetActive(!root.activeSelf);
    }

    private void Build()
    {
        bool showKeyboard = !Application.isMobilePlatform;
        bool showTouch = input != null && input.TouchControlsActive;

        var canvasGo = new GameObject("HelpMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60; // above the touch buttons (50) and the message box (50)
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // full-screen dimmer; tapping it closes the menu
        root = new GameObject("Dimmer", typeof(RectTransform), typeof(Image), typeof(Button));
        root.transform.SetParent(canvasGo.transform, false);
        var dimRt = (RectTransform)root.transform;
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = dimRt.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        root.GetComponent<Button>().onClick.AddListener(() => root.SetActive(false));

        float width = ActionWidth + SidePadding * 2f;
        if (showKeyboard) width += KeyboardWidth;
        if (showTouch) width += TouchWidth;

        // yellow border that grows with the content, dark panel inside it
        var border = new GameObject("Border", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        border.transform.SetParent(root.transform, false);
        var borderRt = (RectTransform)border.transform;
        borderRt.anchorMin = borderRt.anchorMax = borderRt.pivot = new Vector2(0.5f, 0.5f);
        borderRt.anchoredPosition = Vector2.zero;
        borderRt.sizeDelta = new Vector2(width, 0);
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
        lg.padding = new RectOffset(30, 30, 20, 20);
        lg.spacing = 8;
        lg.childControlWidth = lg.childControlHeight = true;
        lg.childForceExpandWidth = true;
        lg.childForceExpandHeight = false;

        var title = MakeText(inner.transform, "CONTROLS", 44, Gold, TextAlignmentOptions.Center, bold: true);
        title.gameObject.AddComponent<LayoutElement>().minHeight = 60;

        AddRow(inner.transform, "", "KEYBOARD", "TOUCH", showKeyboard, showTouch, Gold, bold: true);
        foreach (var row in Rows)
        {
            // a phone has no mouse or keyboard, so skip controls without a touch button
            if (!showKeyboard && row[2] == "-") continue;
            AddRow(inner.transform, row[0], row[1], row[2], showKeyboard, showTouch, Color.white, bold: false, actionColor: Gold);
        }

        var footer = MakeText(inner.transform, showTouch ? "Tap anywhere to close" : "Press H to close", 26, Color.white, TextAlignmentOptions.Center, bold: false);
        footer.gameObject.AddComponent<LayoutElement>().minHeight = 44;

        root.SetActive(false);
    }

    private void AddRow(Transform parent, string action, string keyboard, string touch, bool showKeyboard, bool showTouch,
        Color color, bool bold, Color? actionColor = null)
    {
        var row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);
        var h = row.GetComponent<HorizontalLayoutGroup>();
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = h.childForceExpandHeight = false;
        h.spacing = 0;

        Cell(row.transform, action, ActionWidth, actionColor ?? color, bold);
        if (showKeyboard) Cell(row.transform, keyboard, KeyboardWidth, color, bold);
        if (showTouch) Cell(row.transform, touch, TouchWidth, color, bold);
    }

    private static void Cell(Transform parent, string text, float width, Color color, bool bold)
    {
        var t = MakeText(parent, text, 30, color, TextAlignmentOptions.Left, bold);
        var le = t.gameObject.AddComponent<LayoutElement>();
        le.minWidth = le.preferredWidth = width;
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
