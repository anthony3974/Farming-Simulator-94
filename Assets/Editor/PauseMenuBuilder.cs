using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// One-off editor tool: adds the "Pause Menu" object (its own Canvas) to the open scene (GameScene), with
// the PauseMenu script and every reference filled in. All the parts are plain scene objects with no layout
// groups, so they can be moved, resized and restyled freely in the editor afterwards.
// Use: open GameScene, Tools > Build Pause Menu, save the scene. Delete this file afterwards (git history).
//
// Pause Menu            Canvas (sort order 55: above the touch buttons, below the help pop-up) + PauseMenu
//   Pause Button        top-left beside HELP; shown only with touch controls
//   Menu                full-screen dimmer, hidden while playing (left visible in the editor to work on)
//     Border > Panel    yellow border, dark panel
//       Title           "PAUSED"
//       Resume Button
//       Main Menu Button
//       Hint            "Esc to resume"
public static class PauseMenuBuilder
{
    const string RootName = "Pause Menu";

    static readonly Color Gold = new Color32(255, 201, 51, 255);
    static readonly Color Yellow = new Color32(255, 204, 0, 255);
    static readonly Color Panel = new Color32(31, 31, 31, 245);
    static readonly Color ButtonFill = new Color32(60, 45, 20, 255);

    [MenuItem("Tools/Build Pause Menu")]
    static void Build()
    {
        var old = GameObject.Find(RootName);
        if (old != null)
        {
            if (!EditorUtility.DisplayDialog("Pause Menu", "The scene already has a Pause Menu. Replace it? Any changes made to it are lost.", "Replace", "Cancel"))
                return;
            Undo.DestroyObjectImmediate(old);
        }

        var root = new GameObject(RootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(root, "Build Pause Menu");
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 55;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // PAUSE touch button, top-left, to the right of HELP (HELP fills x 40 to 150)
        var (pauseButton, _) = MakeButton(root.transform, "Pause Button", "PAUSE", 26, new Vector2(110, 110));
        var pauseRt = (RectTransform)pauseButton.transform;
        pauseRt.anchorMin = pauseRt.anchorMax = pauseRt.pivot = new Vector2(0, 1);
        pauseRt.anchoredPosition = new Vector2(170, -40);

        // dimmer over the whole screen
        var menu = new GameObject("Menu", typeof(RectTransform), typeof(Image));
        menu.transform.SetParent(root.transform, false);
        Stretch((RectTransform)menu.transform);
        menu.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(menu.transform, false);
        border.GetComponent<Image>().color = Yellow;
        Center((RectTransform)border.transform, Vector2.zero, new Vector2(640, 520));

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(border.transform, false);
        panel.GetComponent<Image>().color = Panel;
        var panelRt = (RectTransform)panel.transform;
        Stretch(panelRt);
        panelRt.offsetMin = new Vector2(4, 4);
        panelRt.offsetMax = new Vector2(-4, -4);

        var title = MakeText(panel.transform, "Title", "PAUSED", 56, Gold, true);
        Center(title.rectTransform, new Vector2(0, 175), new Vector2(560, 80));

        var (resume, _) = MakeButton(panel.transform, "Resume Button", "RESUME", 44, new Vector2(480, 110));
        Center((RectTransform)resume.transform, new Vector2(0, 50), new Vector2(480, 110));

        var (mainMenu, mainMenuLabel) = MakeButton(panel.transform, "Main Menu Button", "MAIN MENU", 36, new Vector2(480, 110));
        Center((RectTransform)mainMenu.transform, new Vector2(0, -85), new Vector2(480, 110));

        var hint = MakeText(panel.transform, "Hint", "Esc to resume", 26, new Color(1f, 1f, 1f, 0.6f), false);
        Center(hint.rectTransform, new Vector2(0, -200), new Vector2(560, 40));

        var pm = root.AddComponent<PauseMenu>();
        pm.menuRoot = menu;
        pm.resumeButton = resume;
        pm.mainMenuButton = mainMenu;
        pm.mainMenuLabel = mainMenuLabel;
        pm.pauseButton = pauseButton;

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("Pause Menu added. The Menu part stays visible in the editor so you can arrange it; it hides itself when the game starts. Save the scene.");
    }

    static (Button, TMP_Text) MakeButton(Transform parent, string name, string text, float fontSize, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
        go.transform.SetParent(parent, false);
        ((RectTransform)go.transform).sizeDelta = size;
        go.GetComponent<Image>().color = ButtonFill;
        var outline = go.GetComponent<Outline>();
        outline.effectColor = Yellow;
        outline.effectDistance = new Vector2(3, -3);

        var label = MakeText(go.transform, "Label", text, fontSize, Gold, true);
        Stretch(label.rectTransform);
        label.raycastTarget = false;
        // shrinks to fit longer texts such as the MAIN MENU confirmation
        label.enableAutoSizing = true;
        label.fontSizeMax = fontSize;
        label.fontSizeMin = 18;
        label.margin = new Vector4(12, 0, 12, 0);
        return (go.GetComponent<Button>(), label);
    }

    static TMP_Text MakeText(Transform parent, string name, string text, float size, Color color, bool bold)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        return t;
    }

    static void Center(RectTransform rt, Vector2 position, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
