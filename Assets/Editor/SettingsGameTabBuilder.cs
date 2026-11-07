using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// One-off editor tool (owner, 2026-11-07): gives the SETTINGS panel two tabs, CONTROLS (the touch options, as they
// are) and GAME (mirrors, minimap, day / night cycle, day and night length), and puts the DayNightCycle on the sun.
// Use: open GameScene, Tools > Settings: Game Tab, save the scene (Ctrl+S). Delete this file afterwards (git history).
//
// What it changes in the existing "Settings Menu" (everything else, and any changes made to it, is kept):
//   Border               taller: 980 x 760 (was 640), so the tab buttons fit under the title
//     Panel
//       Title            moved up (y 315)
//       Controls Tab Button, Game Tab Button   under the title (UI Button prefab copies)
//       Controls Tab     new group holding the touch options that were in the Panel (moved down 45 so they clear the tabs)
//       Game Tab         new: 5 rows, a label on the left and the buttons on the right
//                          MIRRORS [ON][OFF], MINIMAP [ON][OFF], DAY / NIGHT CYCLE [ON][OFF],
//                          DAY LENGTH [-] 10 MIN [+], NIGHT LENGTH [-] 5 MIN [+]
//       Close Button     moved down (y -310)
// Plus: a DayNightCycle on the Directional Light, and a NightLight on Assets/Prefabs/Street Lamp.prefab if it exists.
// Running it again rebuilds only the tab buttons and the Game Tab.
public static class SettingsGameTabBuilder
{
    const string ButtonPrefabPath = "Assets/Prefabs/UI/UI Button.prefab";
    const string LampPrefabPath = "Assets/Prefabs/Street Lamp.prefab";
    const string Undoname = "Settings: Game Tab";

    static readonly Color Gold = new Color32(255, 201, 51, 255);
    static readonly Vector2 Center5 = new Vector2(0.5f, 0.5f);

    static GameObject buttonPrefab;

    [MenuItem("Tools/Settings: Game Tab")]
    static void Build()
    {
        const string title = "Settings: Game Tab";
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(title, "Stop Play mode first. Changes made in Play mode are lost.", "OK");
            return;
        }
        var sm = Object.FindFirstObjectByType<SettingsMenu>(FindObjectsInactive.Include);
        if (sm == null || sm.menuRoot == null)
        {
            EditorUtility.DisplayDialog(title, "No Settings Menu (with its Menu Root) in the open scene. Open GameScene first.", "OK");
            return;
        }
        var border = sm.menuRoot.transform.Find("Border") as RectTransform;
        var panel = border != null ? border.Find("Panel") as RectTransform : null;
        if (panel == null)
        {
            EditorUtility.DisplayDialog(title, "The Settings Menu has no Menu > Border > Panel. Was it rearranged? Put the options back under Border > Panel and run this again.", "OK");
            return;
        }
        buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPrefabPath);
        if (buttonPrefab == null) { EditorUtility.DisplayDialog(title, $"Missing {ButtonPrefabPath}.", "OK"); return; }

        Undo.SetCurrentGroupName(Undoname);
        int undoGroup = Undo.GetCurrentGroup();

        // a run before: rebuild the tab buttons and the Game Tab (the Controls Tab and its options stay)
        foreach (string old in new[] { "Controls Tab Button", "Game Tab Button", "Game Tab" })
        {
            var t = panel.Find(old);
            if (t != null) Undo.DestroyObjectImmediate(t.gameObject);
        }

        Undo.RecordObject(border, Undoname);
        border.sizeDelta = new Vector2(border.sizeDelta.x, 760f);
        var titleText = panel.Find("Title") as RectTransform;
        if (titleText != null) { Undo.RecordObject(titleText, Undoname); titleText.anchoredPosition = new Vector2(0f, 315f); titleText.sizeDelta = new Vector2(800f, 70f); }
        var close = sm.closeButton != null ? (RectTransform)sm.closeButton.transform : panel.Find("Close Button") as RectTransform;
        if (close != null) { Undo.RecordObject(close, Undoname); close.anchoredPosition = new Vector2(0f, -310f); }

        // the touch options go into the Controls Tab (same places, 45 lower as a group)
        var controlsTab = panel.Find("Controls Tab") as RectTransform;
        if (controlsTab == null)
        {
            controlsTab = Group(panel, "Controls Tab");
            controlsTab.offsetMin = controlsTab.offsetMax = new Vector2(0f, -45f);
            var move = new System.Collections.Generic.List<RectTransform>();
            foreach (Transform child in panel)
                if (child != controlsTab && child != titleText && child != close && child is RectTransform rt) move.Add(rt);
            foreach (var rt in move)
            {
                Vector2 pos = rt.anchoredPosition;
                Undo.SetTransformParent(rt, controlsTab, Undoname);
                Undo.RecordObject(rt, Undoname);
                rt.anchoredPosition = pos;
            }
        }

        // tab buttons under the title
        var controlsTabButton = PrefabButton(panel, "Controls Tab Button", "CONTROLS", new Vector2(-160f, 235f), new Vector2(300f, 70f));
        var gameTabButton = PrefabButton(panel, "Game Tab Button", "GAME", new Vector2(160f, 235f), new Vector2(300f, 70f));

        // the Game Tab: five rows
        var gameTab = Group(panel, "Game Tab");
        float[] rowY = { 150f, 60f, -30f, -120f, -210f };
        Label(gameTab, "Mirrors Label", "MIRRORS", rowY[0]);
        var mirrorsOn = Half(gameTab, "Mirrors On Option", "ON", rowY[0], 0);
        var mirrorsOff = Half(gameTab, "Mirrors Off Option", "OFF", rowY[0], 1);
        Label(gameTab, "Minimap Label", "MINIMAP", rowY[1]);
        var minimapOn = Half(gameTab, "Minimap On Option", "ON", rowY[1], 0);
        var minimapOff = Half(gameTab, "Minimap Off Option", "OFF", rowY[1], 1);
        Label(gameTab, "Cycle Label", "DAY / NIGHT CYCLE", rowY[2]);
        var cycleOn = Half(gameTab, "Cycle On Option", "ON", rowY[2], 0);
        var cycleOff = Half(gameTab, "Cycle Off Option", "OFF", rowY[2], 1);
        Label(gameTab, "Day Length Label", "DAY LENGTH", rowY[3]);
        var dayLess = PrefabButton(gameTab, "Day Less Button", "-", new Vector2(70f, rowY[3]), new Vector2(90f, 78f));
        var dayText = Text(gameTab, "Day Length Text", $"{GameSettings.DefaultDayMinutes} MIN", 38f, new Vector2(210f, rowY[3]), new Vector2(190f, 78f), TextAlignmentOptions.Center);
        var dayMore = PrefabButton(gameTab, "Day More Button", "+", new Vector2(350f, rowY[3]), new Vector2(90f, 78f));
        Label(gameTab, "Night Length Label", "NIGHT LENGTH", rowY[4]);
        var nightLess = PrefabButton(gameTab, "Night Less Button", "-", new Vector2(70f, rowY[4]), new Vector2(90f, 78f));
        var nightText = Text(gameTab, "Night Length Text", $"{GameSettings.DefaultNightMinutes} MIN", 38f, new Vector2(210f, rowY[4]), new Vector2(190f, 78f), TextAlignmentOptions.Center);
        var nightMore = PrefabButton(gameTab, "Night More Button", "+", new Vector2(350f, rowY[4]), new Vector2(90f, 78f));
        gameTab.gameObject.SetActive(false); // the script shows the tab picked; tick it to work on it

        Undo.RecordObject(sm, Undoname);
        sm.controlsTabButton = controlsTabButton;
        sm.gameTabButton = gameTabButton;
        sm.controlsTab = controlsTab.gameObject;
        sm.gameTab = gameTab.gameObject;
        sm.mirrorsOnOption = mirrorsOn;
        sm.mirrorsOffOption = mirrorsOff;
        sm.minimapOnOption = minimapOn;
        sm.minimapOffOption = minimapOff;
        sm.cycleOnOption = cycleOn;
        sm.cycleOffOption = cycleOff;
        sm.dayLessButton = dayLess;
        sm.dayMoreButton = dayMore;
        sm.dayLengthText = dayText;
        sm.nightLessButton = nightLess;
        sm.nightMoreButton = nightMore;
        sm.nightLengthText = nightText;
        EditorUtility.SetDirty(sm);

        string sunNote = AddCycleToSun();
        string lampNote = AddNightLightToLamp();

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(sm.gameObject.scene);
        Selection.activeGameObject = gameTab.gameObject;
        EditorUtility.DisplayDialog(title,
            "SETTINGS now has a CONTROLS and a GAME tab. " + sunNote + " " + lampNote + "\n\n" +
            "Save the scene (Ctrl+S), then delete Assets/Editor/SettingsGameTabBuilder.cs.\n\n" +
            "Game Tab is turned off in the editor (tick it, or Menu, to look at it). In Play mode, O opens SETTINGS on PC.", "OK");
    }

    // the cycle goes on the scene's sun (Lighting > Sun Source, else the first directional light)
    static string AddCycleToSun()
    {
        if (Object.FindFirstObjectByType<DayNightCycle>(FindObjectsInactive.Include) != null) return "The sun already has its DayNightCycle.";
        Light sun = RenderSettings.sun;
        if (sun == null)
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { sun = l; break; }
        if (sun == null)
        {
            Debug.LogWarning("No directional light found: no day / night cycle. Add a DayNightCycle to the sun by hand.");
            return "No sun (directional light) found, so there is no day / night cycle yet (see the Console).";
        }
        Undo.AddComponent<DayNightCycle>(sun.gameObject);
        return $"A DayNightCycle was added to '{sun.name}'.";
    }

    // the street lamp prefab (if built) switches its light on at night
    static string AddNightLightToLamp()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(LampPrefabPath) == null) return "(No street lamp prefab yet: Tools > Street Lamp Builder adds its NightLight itself.)";
        var root = PrefabUtility.LoadPrefabContents(LampPrefabPath);
        try
        {
            if (root.GetComponent<NightLight>() != null) return "The street lamp already has its NightLight.";
            root.AddComponent<NightLight>();
            PrefabUtility.SaveAsPrefabAsset(root, LampPrefabPath);
            return "The street lamp prefab now turns its light on at night (NightLight).";
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    // ---------- helpers ----------

    static RectTransform Group(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, Undoname);
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    // a UI Button prefab copy, centred at position
    static Button PrefabButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, parent);
        Undo.RegisterCreatedObjectUndo(go, Undoname);
        go.name = name;
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = Center5;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        var text = go.transform.Find("Label")?.GetComponent<TMP_Text>();
        if (text != null) text.text = label;
        return go.GetComponent<Button>();
    }

    // ON (index 0) or OFF (1) of a row, side by side on the right half
    static Button Half(Transform parent, string name, string label, float y, int index)
        => PrefabButton(parent, name, label, new Vector2(index == 0 ? 115f : 305f, y), new Vector2(180f, 78f));

    // the row's name on the left half
    static void Label(Transform parent, string name, string text, float y)
        => Text(parent, name, text, 34f, new Vector2(-235f, y), new Vector2(420f, 70f), TextAlignmentOptions.Left);

    static TMP_Text Text(Transform parent, string name, string text, float size, Vector2 position, Vector2 box, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(go, Undoname);
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = Gold;
        t.alignment = align;
        t.fontStyle = FontStyles.Bold;
        t.raycastTarget = false;
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = Center5;
        rt.anchoredPosition = position;
        rt.sizeDelta = box;
        return t;
    }
}
