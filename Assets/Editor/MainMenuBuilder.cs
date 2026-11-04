using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-off editor tool: creates Assets/Scenes/MainMenu.unity (a camera and a "Main Menu" object with the
// MainMenu script, which builds the menu at runtime) and makes it the first scene in Build Settings,
// with GameScene second. Use: Tools > Build Main Menu Scene. Running it again rebuilds the scene.
// Once run and checked, this file can be deleted (keep it in git history).
public static class MainMenuBuilder
{
    const string ScenePath = "Assets/Scenes/MainMenu.unity";
    const string GameScenePath = "Assets/Scenes/GameScene.unity";

    [MenuItem("Tools/Build Main Menu Scene")]
    static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cam = new GameObject("Main Camera", typeof(Camera));
        cam.tag = "MainCamera";
        var c = cam.GetComponent<Camera>();
        c.clearFlags = CameraClearFlags.SolidColor;
        c.backgroundColor = Color.black;

        new GameObject("Main Menu", typeof(MainMenu));

        EditorSceneManager.SaveScene(scene, ScenePath);

        // MainMenu first (it is what the game opens with), then GameScene, then anything else already listed
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true),
        };
        scenes.AddRange(EditorBuildSettings.scenes.Where(s => s.path != ScenePath && s.path != GameScenePath));
        EditorBuildSettings.scenes = scenes.ToArray();

        Selection.activeGameObject = GameObject.Find("Main Menu");
        Debug.Log("MainMenu scene built and added to Build Settings (first). Drag your screenshot into Background on Main Menu, then save.");
    }
}
