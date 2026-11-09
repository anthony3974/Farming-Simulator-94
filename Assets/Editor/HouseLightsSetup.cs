using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools > House Lights Setup (one-off): makes the two light fixtures at home work, the porch light on the farmhouse and
/// the light over the garage's side door. Each gets the street lamps' glowing lens, a small warm Point Light called
/// "Night Light" just in front of it, and a <see cref="NightLight"/>, which turns them on only at night.
/// Safe to run again: a Night Light that already exists keeps its values, so tuning done in the scene is kept.
/// Delete this file once it has been run and the scene saved.
/// </summary>
public static class HouseLightsSetup
{
    private const string Title = "House Lights Setup";
    private const string SceneName = "GameScene";
    private const string LensPath = "Assets/StreetLamp/Lamp Lens.mat";
    private const string LightName = "Night Light";
    private const float OutFromWall = 0.4f; // so the wall behind the fixture is lit too
    private static readonly Color LightColor = new Color32(255, 214, 160, 255); // the same warm white as the street lamps

    private static readonly (string path, Vector3 outward, float range, float intensity)[] Fixtures =
    {
        ("Buildings/HomeArea/Farmhouse/Model/House/Porch Light", Vector3.left, 14f, 2f),
        ("Buildings/HomeArea/Garage/Model/Side Door/Light", Vector3.forward, 10f, 1.5f),
    };

    [MenuItem("Tools/House Lights Setup")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(Title, "Stop Play mode first. Changes made in Play mode are lost.", "OK");
            return;
        }
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != SceneName || Terrain.activeTerrain == null)
        {
            EditorUtility.DisplayDialog(Title, $"Open {SceneName} first.", "OK");
            return;
        }
        var lens = AssetDatabase.LoadAssetAtPath<Material>(LensPath);
        if (lens == null)
        {
            EditorUtility.DisplayDialog(Title, $"{LensPath} is missing.", "OK");
            return;
        }

        Undo.SetCurrentGroupName(Title);
        int undoGroup = Undo.GetCurrentGroup();
        Transform terrain = Terrain.activeTerrain.transform;
        int done = 0;
        foreach (var (path, outward, range, intensity) in Fixtures)
        {
            Transform fixture = terrain.Find(path);
            var renderer = fixture != null ? fixture.GetComponent<Renderer>() : null;
            if (renderer == null)
            {
                Debug.LogWarning($"{Title}: nothing found at {path}, skipped.");
                continue;
            }
            SetUp(fixture, renderer, lens, outward, range, intensity);
            done++;
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorUtility.DisplayDialog(Title, $"{done} of {Fixtures.Length} lights set up. Save the scene (Ctrl+S).", "OK");
    }

    private static void SetUp(Transform fixture, Renderer renderer, Material lens, Vector3 outward, float range, float intensity)
    {
        if (renderer.sharedMaterial != lens)
        {
            Undo.RecordObject(renderer, Title);
            renderer.sharedMaterial = lens; // this object only: the shared house material is also on the door knobs
        }

        if (fixture.GetComponent<NightLight>() == null) Undo.AddComponent<NightLight>(fixture.gameObject);
        if (fixture.Find(LightName) != null) return;

        var go = new GameObject(LightName);
        Undo.RegisterCreatedObjectUndo(go, Title);
        go.transform.SetParent(fixture, false);
        Bounds bounds = renderer.bounds;
        go.transform.position = bounds.center + outward * (Vector3.Dot(bounds.extents, new Vector3(Mathf.Abs(outward.x), 0f, Mathf.Abs(outward.z))) + OutFromWall);

        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = range;
        light.intensity = intensity;
        light.color = LightColor;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.ForcePixel; // "Important", so it shows on phones too
    }
}
