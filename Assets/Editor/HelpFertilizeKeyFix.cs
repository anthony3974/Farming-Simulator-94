using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-off (owner, 2026-11-06): on PC, E now fertilizes a growing field and the F key is gone.
/// Sets the Keyboard Panel's "Fertilize field" row control from "F" to "E". Delete this file after running it.
/// </summary>
public static class HelpFertilizeKeyFix
{
    [MenuItem("Tools/Help: Fertilize Key F to E")]
    private static void Run()
    {
        var help = Object.FindFirstObjectByType<HelpMenu>(FindObjectsInactive.Include);
        if (help == null || help.keyboardPanel == null)
        {
            EditorUtility.DisplayDialog("Help: Fertilize Key", "No HelpMenu with a Keyboard Panel in the open scene. Open GameScene first.", "OK");
            return;
        }

        int changed = 0;
        foreach (Transform row in help.keyboardPanel.GetComponentsInChildren<Transform>(true))
        {
            var action = row.Find("Action")?.GetComponent<TMP_Text>();
            var control = row.Find("Control")?.GetComponent<TMP_Text>();
            if (action == null || control == null || !action.text.StartsWith("Fertilize")) continue;
            if (control.text == "E") continue;
            Undo.RecordObject(control, "Fertilize key F to E");
            control.text = "E";
            EditorUtility.SetDirty(control);
            changed++;
        }

        if (changed > 0) EditorSceneManager.MarkSceneDirty(help.gameObject.scene);
        EditorUtility.DisplayDialog("Help: Fertilize Key",
            changed > 0 ? "The Fertilize row now says E. Save the scene (Ctrl+S), then delete Assets/Editor/HelpFertilizeKeyFix.cs."
                        : "Nothing to change (no Fertilize row with F found, or it already says E).", "OK");
    }
}
