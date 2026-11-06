using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-off (owner, 2026-11-06): the F key and the FERT touch button are gone; E / USE fertilizes a growing field.
/// Sets the help menu's "Fertilize field" row to E (Keyboard Panel) and USE (Touch Panel). Delete this file after running it.
/// </summary>
public static class HelpFertilizeRowFix
{
    [MenuItem("Tools/Help: Fertilize Row to E and USE")]
    private static void Run()
    {
        var help = Object.FindFirstObjectByType<HelpMenu>(FindObjectsInactive.Include);
        if (help == null || help.keyboardPanel == null || help.touchPanel == null)
        {
            EditorUtility.DisplayDialog("Help: Fertilize Row", "No HelpMenu with a Keyboard Panel and Touch Panel in the open scene. Open GameScene first.", "OK");
            return;
        }

        int changed = SetControl(help.keyboardPanel, "E") + SetControl(help.touchPanel, "USE");
        if (changed > 0) EditorSceneManager.MarkSceneDirty(help.gameObject.scene);
        EditorUtility.DisplayDialog("Help: Fertilize Row",
            changed > 0 ? $"Changed {changed} row(s). Save the scene (Ctrl+S), then delete Assets/Editor/HelpFertilizeRowFix.cs."
                        : "Nothing to change (the Fertilize rows already say E and USE).", "OK");
    }

    // the row whose Action text starts with "Fertilize": set its Control text
    private static int SetControl(GameObject panel, string key)
    {
        int changed = 0;
        foreach (Transform row in panel.GetComponentsInChildren<Transform>(true))
        {
            var action = row.Find("Action")?.GetComponent<TMP_Text>();
            var control = row.Find("Control")?.GetComponent<TMP_Text>();
            if (action == null || control == null || !action.text.StartsWith("Fertilize") || control.text == key) continue;
            Undo.RecordObject(control, "Fertilize row key");
            control.text = key;
            EditorUtility.SetDirty(control);
            changed++;
        }
        return changed;
    }
}
