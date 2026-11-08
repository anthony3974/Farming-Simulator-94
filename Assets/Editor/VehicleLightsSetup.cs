using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > Vehicle Lights Setup (one-off): gives the Tractor, Van and Harvester prefabs their headlights. The lens parts
/// the models already have get a glowing material, and each vehicle gets one real forward Spot Light ("Headlight Beam")
/// and a <see cref="VehicleLights"/>, which turns them on only for the vehicle being driven, at night. The scene's
/// vehicles are copies of the prefabs, so they get it too (no scene save needed).
/// Safe to run again: a Headlight Beam that already exists keeps its values, so tuning done on the prefabs is kept.
/// </summary>
public static class VehicleLightsSetup
{
    private const string Title = "Vehicle Lights Setup";
    private const string Folder = "Assets/VehicleLights";
    private const string LensPath = Folder + "/Headlight Lens.mat";
    private const string BeamName = "Headlight Beam";

    private static readonly Color LensColor = new Color32(255, 240, 205, 255);
    private static readonly Color BeamColor = new Color32(255, 244, 214, 255);

    // position: in the vehicle's own space, just in front of its lenses. tilt: degrees down from straight ahead.
    // The harvester's beam comes from the cab roof (its work lights) and tilts more, so it shines past the header.
    private static readonly (string prefab, string[] lenses, int lensCount, Vector3 position, float tilt)[] Vehicles =
    {
        ("Assets/Prefabs/Tractor.prefab", new[] { "Light L", "Light R" }, 2, new Vector3(0f, 1.88f, 1.05f), 6f),
        ("Assets/Prefabs/Van.prefab", new[] { "Light L", "Light R" }, 2, new Vector3(0f, 1.15f, 2.56f), 6f),
        ("Assets/Prefabs/Harvester.prefab", new[] { "Head Light L", "Head Light R", "Work Light" }, 6, new Vector3(0f, 4.85f, 2.40f), 12f),
    };

    [MenuItem("Tools/Vehicle Lights Setup")]
    private static void Run()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(Title, "Leave Play mode first.", "OK");
            return;
        }

        var lens = GetLensMaterial();
        var report = new System.Text.StringBuilder();
        foreach (var v in Vehicles)
            report.AppendLine(SetupPrefab(v.prefab, v.lenses, v.lensCount, v.position, v.tilt, lens));

        AssetDatabase.SaveAssets();
        Debug.Log(Title + " done:\n" + report);
        EditorUtility.DisplayDialog(Title, report + "\nPress Play and switch vehicles at night to see them.", "OK");
    }

    private static Material GetLensMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(LensPath);
        if (mat != null) return mat;

        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "VehicleLights");
        mat = new Material(Shader.Find("Standard")) { color = LensColor };
        mat.SetFloat("_Glossiness", 0.8f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", LensColor * 2f);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        AssetDatabase.CreateAsset(mat, LensPath);
        return mat;
    }

    private static string SetupPrefab(string prefab, string[] lensNames, int lensCount, Vector3 position, float tilt, Material lensMaterial)
    {
        string name = System.IO.Path.GetFileNameWithoutExtension(prefab);
        var root = PrefabUtility.LoadPrefabContents(prefab);
        try
        {
            var names = new HashSet<string>(lensNames);
            var lenses = new List<Renderer>();
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                if (names.Contains(r.name)) lenses.Add(r);
            if (lenses.Count != lensCount)
                return $"{name}: found {lenses.Count} lens parts, expected {lensCount}. Skipped.";

            foreach (var r in lenses) r.sharedMaterial = lensMaterial;

            var lights = root.GetComponent<VehicleLights>();
            if (lights == null) lights = root.AddComponent<VehicleLights>();
            lights.beam = GetOrAddBeam(root.transform, position, tilt, out bool added);
            lights.lenses = lenses.ToArray();

            PrefabUtility.SaveAsPrefabAsset(root, prefab);
            return $"{name}: {lenses.Count} lenses, beam {(added ? "added" : "already there, kept")}.";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Light GetOrAddBeam(Transform root, Vector3 position, float tilt, out bool added)
    {
        var existing = root.Find(BeamName);
        added = existing == null;
        if (!added) return existing.GetComponent<Light>();

        var go = new GameObject(BeamName);
        go.transform.SetParent(root, false);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.Euler(tilt, 0f, 0f);

        var light = go.AddComponent<Light>();
        light.type = LightType.Spot;
        light.spotAngle = 60f;
        light.range = 50f;
        light.intensity = 5f;
        light.color = BeamColor;
        light.shadows = LightShadows.None;              // cheap on phones
        light.renderMode = LightRenderMode.ForcePixel;  // Important: Android Medium has only 1 ordinary pixel light, the sun's
        light.lightmapBakeType = LightmapBakeType.Realtime;
        light.enabled = false;                          // VehicleLights turns it on
        return light;
    }
}
