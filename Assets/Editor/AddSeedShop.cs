using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-off helper. Turns the cloned "Seed Buying" station into the seed shop: finds the Building
/// in it (the cloned wheat Triggerbox), sets its type to Buy and fills the missing references.
/// The trigger box and the model are left as they are, move or resize them in the scene.
/// Safe to run twice. Delete this file afterwards.
/// </summary>
public static class AddSeedShop
{
    private const string StationName = "Seed Buying";

    [MenuItem("Tools/Set Up Seed Buying")]
    private static void Run()
    {
        // use the selected object if there is one, otherwise look for the station by name
        var target = Selection.activeGameObject != null ? Selection.activeGameObject : GameObject.Find(StationName);
        if (target == null)
        {
            EditorUtility.DisplayDialog("Seed shop", $"Select '{StationName}' in the Hierarchy first.", "OK");
            return;
        }

        var building = target.GetComponentInChildren<Building>(true);
        if (building == null)
        {
            EditorUtility.DisplayDialog("Seed shop", $"'{target.name}' has no Building inside it. Select the cloned '{StationName}' object.", "OK");
            return;
        }

        var inputManager = Object.FindFirstObjectByType<InputManager>();
        var playerValues = Object.FindFirstObjectByType<PlayerValues>();
        if (inputManager == null || playerValues == null)
        {
            EditorUtility.DisplayDialog("Seed shop", "Could not find InputManager or PlayerValues in the scene.", "OK");
            return;
        }

        Undo.RecordObject(building, "Set up seed buying");
        building.type = Building.BuildingType.Buy;
        if (building.inputManager == null) building.inputManager = inputManager;
        if (building.playerValues == null) building.playerValues = playerValues;

        // the shop needs a trigger zone, the clone should already have one
        var col = building.GetComponent<Collider>();
        if (col == null) Debug.LogWarning($"'{building.name}' has no collider, add a trigger BoxCollider so the player can enter it.", building);
        else if (!col.isTrigger)
        {
            Undo.RecordObject(col, "Set up seed buying");
            col.isTrigger = true;
        }

        EditorUtility.SetDirty(building);
        EditorSceneManager.MarkSceneDirty(building.gameObject.scene);
        Selection.activeGameObject = building.gameObject;
        Debug.Log($"'{building.name}' is now a seed shop (type Buy). Press Ctrl+S to save the scene.");
    }
}
