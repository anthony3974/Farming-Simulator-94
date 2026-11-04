using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// One-off editor tool: adds the "Help Menu" object (its own Canvas) to the open scene (GameScene), with the
// HelpMenu script and every reference filled in. All the parts are plain positioned objects with no layout
// groups, so they can be moved, resized, restyled and reworded freely in the editor afterwards.
// Use: open GameScene, Tools > Build Help Menu, save the scene. Delete this file afterwards (git history).
//
// Help Menu              Canvas (sort order 60: above the touch buttons 50 and the pause menu 55) + HelpMenu
//   Menu                 full-screen dimmer + button (a tap closes it). Left turned OFF in the editor so it does
//                        not cover the game view; tick it to work on it (it hides itself on Play either way)
//     Keyboard Panel     shown on PC: yellow border > dark Panel > Title, header, one Row per control, Footer
//     Touch Panel        shown with touch controls (phones): the same with the touch buttons (turned off in
//                        the editor so the two panels do not overlap; tick it to work on it)
public static class HelpMenuBuilder
{
    const string RootName = "Help Menu";

    static readonly Color Gold = new Color32(255, 201, 51, 255);
    static readonly Color Yellow = new Color32(255, 204, 0, 255);
    static readonly Color Panel = new Color32(31, 31, 31, 245);

    // the same columns and sizes as the old runtime menu (action column a little wider so no row wraps)
    const float ActionWidth = 620f, KeyWidth = 360f, TouchWidth = 220f;
    const float Border = 4f, PadX = 30f, PadY = 20f;
    const float TitleHeight = 60f, RowHeight = 40f, FooterHeight = 44f, Spacing = 8f;
    const float TextSize = 30f, TitleSize = 44f, FooterSize = 26f;

    // action, key, touch button (null = no touch button: left out of the touch panel)
    static readonly string[][] Rows =
    {
        new[] { "Drive forward / back", "W / S or Up / Down", "GAS / BRAKE" },
        new[] { "Steer left / right", "A / D or Left / Right", "< / >" },
        new[] { "Look up / down", "Mouse", "DRAG" },
        new[] { "Use (plant, harvest, sell, shops)", "E", "USE" },
        new[] { "Buy / sell field", "B", "BUY/SELL" },
        new[] { "Choose crop", "C", "CROP" },
        new[] { "Fertilize field (while it grows)", "F", "FERT" },
        new[] { "Switch vehicle (tractor, van, harvester)", "1 / 2 / 3", "CAR" },
        new[] { "Help", "H", "HELP" },
        new[] { "Pause (releases the mouse)", "Esc", "PAUSE" },
        new[] { "Debug toggle", "F1", null },
    };

    [MenuItem("Tools/Build Help Menu")]
    static void Build()
    {
        if (Object.FindFirstObjectByType<InputManager>() == null)
        {
            EditorUtility.DisplayDialog("Help Menu", "No InputManager in the open scene. Open GameScene first.", "OK");
            return;
        }

        var old = GameObject.Find(RootName);
        if (old != null)
        {
            if (!EditorUtility.DisplayDialog("Help Menu", "The scene already has a Help Menu. Replace it? Any changes made to it are lost.", "Replace", "Cancel"))
                return;
            Undo.DestroyObjectImmediate(old);
        }

        var root = new GameObject(RootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(root, "Build Help Menu");
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // full-screen dimmer; a tap anywhere on it closes the menu
        var menu = new GameObject("Menu", typeof(RectTransform), typeof(Image), typeof(Button));
        menu.transform.SetParent(root.transform, false);
        Stretch((RectTransform)menu.transform);
        menu.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        var close = menu.GetComponent<Button>();
        close.transition = Selectable.Transition.None;

        var keyboardPanel = MakePanel(menu.transform, "Keyboard Panel", touch: false, "Press H to close");
        var touchPanel = MakePanel(menu.transform, "Touch Panel", touch: true, "Tap anywhere to close");
        touchPanel.SetActive(false);
        menu.SetActive(false);

        var help = root.AddComponent<HelpMenu>();
        help.menuRoot = menu;
        help.closeButton = close;
        help.keyboardPanel = keyboardPanel;
        help.touchPanel = touchPanel;

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log("Help Menu added. Save the scene (Ctrl+S). Its Menu object is turned off in the editor: tick it to see and edit the panels.", root);
    }

    // yellow border with the dark panel inset 4 px, sized to fit its rows, centred on screen
    static GameObject MakePanel(Transform parent, string name, bool touch, string footerText)
    {
        float secondWidth = touch ? TouchWidth : KeyWidth;
        int rows = 0;
        foreach (var r in Rows) if (!touch || r[2] != null) rows++;

        float innerWidth = PadX * 2f + ActionWidth + secondWidth;
        // title, header row, the control rows, footer, with Spacing between each
        float innerHeight = PadY * 2f + TitleHeight + (rows + 1) * (RowHeight + Spacing) + Spacing + FooterHeight;

        var border = new GameObject(name, typeof(RectTransform), typeof(Image));
        border.transform.SetParent(parent, false);
        border.GetComponent<Image>().color = Yellow;
        var borderRt = (RectTransform)border.transform;
        borderRt.anchorMin = borderRt.anchorMax = borderRt.pivot = new Vector2(0.5f, 0.5f);
        borderRt.anchoredPosition = Vector2.zero;
        borderRt.sizeDelta = new Vector2(innerWidth + Border * 2f, innerHeight + Border * 2f);

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(border.transform, false);
        panel.GetComponent<Image>().color = Panel;
        var panelRt = (RectTransform)panel.transform;
        Stretch(panelRt);
        panelRt.offsetMin = new Vector2(Border, Border);
        panelRt.offsetMax = new Vector2(-Border, -Border);

        float contentWidth = ActionWidth + secondWidth;
        float y = PadY;
        var title = MakeText(panel.transform, "Title", "CONTROLS", TitleSize, Gold, TextAlignmentOptions.Center, true);
        Place(title.rectTransform, PadX, y, contentWidth, TitleHeight);
        y += TitleHeight + Spacing;

        MakeRow(panel.transform, "Header", "", touch ? "TOUCH" : "KEYBOARD", secondWidth, y, Gold, Gold, true);
        y += RowHeight + Spacing;

        foreach (var r in Rows)
        {
            if (touch && r[2] == null) continue;
            MakeRow(panel.transform, "Row " + r[0], r[0], touch ? r[2] : r[1], secondWidth, y, Gold, Color.white, false);
            y += RowHeight + Spacing;
        }

        var footer = MakeText(panel.transform, "Footer", footerText, FooterSize, Color.white, TextAlignmentOptions.Center, false);
        Place(footer.rectTransform, PadX, y, contentWidth, FooterHeight);
        return border;
    }

    // one row: an empty holder with the action text and the key / touch button text side by side
    static void MakeRow(Transform panel, string name, string action, string control, float controlWidth, float y,
        Color actionColor, Color controlColor, bool bold)
    {
        var row = new GameObject(name, typeof(RectTransform));
        row.transform.SetParent(panel, false);
        Place((RectTransform)row.transform, PadX, y, ActionWidth + controlWidth, RowHeight);

        var a = MakeText(row.transform, "Action", action, TextSize, actionColor, TextAlignmentOptions.MidlineLeft, bold);
        Place(a.rectTransform, 0f, 0f, ActionWidth, RowHeight);
        var c = MakeText(row.transform, "Control", control, TextSize, controlColor, TextAlignmentOptions.MidlineLeft, bold);
        Place(c.rectTransform, ActionWidth, 0f, controlWidth, RowHeight);
    }

    static TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions align, bool bold)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        // one line; a longer text (after rewording) shrinks to fit instead of spilling into the next row
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.enableAutoSizing = true;
        t.fontSizeMax = size;
        t.fontSizeMin = 18f;
        t.raycastTarget = false;
        return t;
    }

    // top-left anchored, x / y measured from the parent's top-left corner (y down)
    static void Place(RectTransform rt, float x, float y, float width, float height)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(width, height);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
