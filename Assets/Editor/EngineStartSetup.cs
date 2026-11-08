using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > Engine Start Setup (one-off): sets the import settings of the two start sounds in Assets/Sounds/Starts and
/// puts them in the Engine Sound of the Tractor, Van and Harvester prefabs. The scene's vehicles are copies of the
/// prefabs, so they get it too (no scene save needed). Safe to run again: only an empty Start Clip slot is filled,
/// so tuning done on the prefabs is kept. The sounds are made by Art/Sounds/make_starts.py.
/// </summary>
public static class EngineStartSetup
{
    private const string Title = "Engine Start Setup";
    private const string Diesel = "Assets/Sounds/Starts/EngineStartDiesel.wav"; // tractor and harvester
    private const string Van = "Assets/Sounds/Starts/EngineStartVan.wav";

    private static readonly (string prefab, string clip, float pitch)[] Vehicles =
    {
        ("Assets/Prefabs/Tractor.prefab", Diesel, 1f),
        ("Assets/Prefabs/Van.prefab", Van, 1f),
        ("Assets/Prefabs/Harvester.prefab", Diesel, 0.85f), // the same pitch as its idle, a bigger engine
    };

    [MenuItem("Tools/Engine Start Setup")]
    private static void Run()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(Title, "Leave Play mode first.", "OK");
            return;
        }

        foreach (var path in new[] { Diesel, Van })
            if (!SetImport(path)) return;

        var report = new System.Text.StringBuilder();
        foreach (var (prefab, clip, pitch) in Vehicles) report.AppendLine(SetupPrefab(prefab, clip, pitch));

        AssetDatabase.SaveAssets();
        Debug.Log(Title + " done:\n" + report);
        EditorUtility.DisplayDialog(Title, report + "\nPress Play and switch vehicles to hear them.", "OK");
    }

    /// <summary>Mono, ready in memory so the first press plays at once, Vorbis in the build (the same as the loops).</summary>
    private static bool SetImport(string path)
    {
        if (AssetImporter.GetAtPath(path) is not AudioImporter importer)
        {
            EditorUtility.DisplayDialog(Title,
                $"{path} is missing. Run  python3 Art/Sounds/make_starts.py  in the project folder first.", "OK");
            return false;
        }

        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = 0.7f;
        settings.preloadAudioData = true;
        importer.defaultSampleSettings = settings;
        importer.forceToMono = true;
        importer.loadInBackground = false;
        importer.SaveAndReimport();
        return true;
    }

    private static string SetupPrefab(string prefab, string clip, float pitch)
    {
        string name = System.IO.Path.GetFileNameWithoutExtension(prefab);
        var root = PrefabUtility.LoadPrefabContents(prefab);
        try
        {
            var engine = root.GetComponent<EngineSound>();
            if (engine == null) return $"{name}: no Engine Sound on it, skipped.";
            if (engine.startClip != null) return $"{name}: already has a start clip, kept.";

            engine.startClip = AssetDatabase.LoadAssetAtPath<AudioClip>(clip);
            engine.startPitch = pitch;
            PrefabUtility.SaveAsPrefabAsset(root, prefab);
            return $"{name}: start clip set.";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
