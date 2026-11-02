using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Fills in missing InputManager / PlayerValues references on every Building in the open scene.</summary>
public static class WireBuildings
{
    [MenuItem("Tools/Wire Buildings")]
    public static void Run()
    {
        var input = Object.FindFirstObjectByType<InputManager>();
        var values = Object.FindFirstObjectByType<PlayerValues>();
        if (input == null || values == null) { Debug.LogError("InputManager or PlayerValues not found in the scene."); return; }

        int changed = 0;
        foreach (var b in Object.FindObjectsByType<Building>(FindObjectsSortMode.None))
        {
            Undo.RecordObject(b, "Wire building");
            if (b.inputManager == null) { b.inputManager = input; changed++; }
            if (b.playerValues == null) { b.playerValues = values; changed++; }
            EditorUtility.SetDirty(b);
            EditorSceneManager.MarkSceneDirty(b.gameObject.scene);
        }
        Debug.Log($"Wire Buildings: filled {changed} reference(s). Save the scene (Ctrl+S).");
    }
}
