using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-off (owner, 2026-11-06): the help menu's touch panel gets a SETTINGS row (below HELP and PAUSE, the panel
/// grows by one row) and the touch pause row loses "(releases the mouse)", which is PC only.
/// Run once in GameScene, save, then delete this file.
/// </summary>
public static class HelpTouchRowsFix
{
    const float RowStep = 48f;

    [MenuItem("Tools/Help: Touch Settings Row")]
    private static void Run()
    {
        var help = Object.FindFirstObjectByType<HelpMenu>(FindObjectsInactive.Include);
        var panel = help != null && help.touchPanel != null ? help.touchPanel.transform.Find("Panel") : null;
        if (panel == null)
        {
            EditorUtility.DisplayDialog("Help: Touch Settings Row", "No HelpMenu with a Touch Panel > Panel in the open scene. Open GameScene first.", "OK");
            return;
        }
        if (panel.Find("Row Settings") != null)
        {
            EditorUtility.DisplayDialog("Help: Touch Settings Row", "The touch panel already has a Row Settings.", "OK");
            return;
        }

        var pause = panel.Find("Row Pause (releases the mouse)");
        var footer = panel.Find("Footer") as RectTransform;
        if (pause == null || footer == null)
        {
            EditorUtility.DisplayDialog("Help: Touch Settings Row", "Could not find the touch panel's pause row or Footer.", "OK");
            return;
        }

        // the new row: a copy of the pause row, one row below it, where the footer was
        var row = Object.Instantiate(pause.gameObject, panel);
        Undo.RegisterCreatedObjectUndo(row, "Help touch settings row");
        row.name = "Row Settings";
        row.transform.SetSiblingIndex(pause.GetSiblingIndex() + 1);
        var rowRt = (RectTransform)row.transform;
        rowRt.anchoredPosition = ((RectTransform)pause).anchoredPosition + new Vector2(0, -RowStep);
        SetText(row.transform, "Action", "Touch settings (steering, pedals, button shape)");
        SetText(row.transform, "Control", "SETTINGS");

        // the pause row on touch does not release a mouse
        Undo.RecordObject(pause.gameObject, "Help touch settings row");
        pause.name = "Row Pause";
        SetText(pause, "Action", "Pause");

        Undo.RecordObject(footer, "Help touch settings row");
        footer.anchoredPosition += new Vector2(0, -RowStep);
        var touchRt = (RectTransform)help.touchPanel.transform;
        Undo.RecordObject(touchRt, "Help touch settings row");
        touchRt.sizeDelta += new Vector2(0, RowStep);

        EditorSceneManager.MarkSceneDirty(help.gameObject.scene);
        EditorUtility.DisplayDialog("Help: Touch Settings Row", "Added the SETTINGS row. Save the scene (Ctrl+S), then delete Assets/Editor/HelpTouchRowsFix.cs.", "OK");
    }

    private static void SetText(Transform row, string child, string text)
    {
        var t = row.Find(child)?.GetComponent<TMP_Text>();
        if (t == null) return;
        Undo.RecordObject(t, "Help touch settings row");
        t.text = text;
        EditorUtility.SetDirty(t);
    }
}
