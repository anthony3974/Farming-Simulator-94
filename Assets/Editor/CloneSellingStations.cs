using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>One-off: clones the Wheat Selling station into Barley and Oat stations, offset so they are easy to select.</summary>
public static class CloneSellingStations
{
    [MenuItem("Tools/Clone Selling Stations")]
    public static void Run()
    {
        var wheat = GameObject.Find("Wheat Selling");
        if (wheat == null) { Debug.LogError("'Wheat Selling' not found in the open scene."); return; }

        Make(wheat, "Barley Selling", Building.Crop.Barley, 12f);
        Make(wheat, "Oat Selling", Building.Crop.Oat, 24f);
        EditorSceneManager.MarkSceneDirty(wheat.scene);
        Debug.Log("Selling stations cloned. Move them where you want, then save (Ctrl+S).");
    }

    private static void Make(GameObject source, string newName, Building.Crop crop, float offsetX)
    {
        if (GameObject.Find(newName) != null) { Debug.Log($"{newName} already exists."); return; }

        var clone = Object.Instantiate(source, source.transform.parent);
        clone.name = newName;
        Undo.RegisterCreatedObjectUndo(clone, "Clone selling station");
        clone.transform.position = source.transform.position + new Vector3(offsetX, 0, 0);

        foreach (var b in clone.GetComponentsInChildren<Building>(true))
        {
            b.crop = crop;
            EditorUtility.SetDirty(b);
        }
    }
}
