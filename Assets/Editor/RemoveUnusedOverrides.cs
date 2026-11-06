using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Tools > Remove Unused Prefab Overrides (one-off): removes the overrides in the open scene that point at parts
/// no longer in their prefab. Made for the Harvester: the old Model was deleted from Harvester.prefab, which left
/// 2 position overrides for its parts in GameScene. Checks every prefab copy in the scene, changes only those
/// dead overrides, and lists them in the Console. Save the scene afterwards (Ctrl+S). Delete this file after.
/// </summary>
public static class RemoveUnusedOverrides
{
    [MenuItem("Tools/Remove Unused Prefab Overrides")]
    private static void Run()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Remove Unused Prefab Overrides", "Leave Play mode first.", "OK");
            return;
        }

        var scene = SceneManager_Active();
        int removed = 0;
        var report = new System.Text.StringBuilder();

        foreach (var root in scene.GetRootGameObjects())
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            var go = t.gameObject;
            // only the top of each prefab copy holds its overrides
            if (!PrefabUtility.IsOutermostPrefabInstanceRoot(go)) continue;

            var mods = PrefabUtility.GetPropertyModifications(go);
            if (mods == null) continue;

            // a target that no longer exists in the prefab loads as null
            var dead = mods.Where(m => m.target == null).ToArray();
            if (dead.Length == 0) continue;

            Undo.RecordObject(go, "Remove unused prefab overrides");
            PrefabUtility.SetPropertyModifications(go, mods.Where(m => m.target != null).ToArray());
            removed += dead.Length;
            report.AppendLine($"{go.name}: removed {dead.Length} ({string.Join(", ", dead.Select(m => $"{m.propertyPath} = {m.value}"))})");
        }

        if (removed == 0)
        {
            EditorUtility.DisplayDialog("Remove Unused Prefab Overrides", $"No unused overrides in {scene.name}. Nothing to do.", "OK");
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"Removed {removed} unused prefab override(s) in {scene.name}:\n{report}");
        EditorUtility.DisplayDialog("Remove Unused Prefab Overrides",
            $"Removed {removed} unused override(s):\n\n{report}\nSave the scene (Ctrl+S).", "OK");
    }

    private static UnityEngine.SceneManagement.Scene SceneManager_Active() =>
        UnityEngine.SceneManagement.SceneManager.GetActiveScene();
}
