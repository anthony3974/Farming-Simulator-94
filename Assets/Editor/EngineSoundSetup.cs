using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > Engine Sound Setup (one-off): sets the import settings of the three engine loops in
/// Assets/Sounds/Loops and adds an <see cref="EngineSound"/> to the Tractor, Van and Harvester prefabs, with a
/// starting tune for each. The scene's vehicles are copies of the prefabs, so they get it too (no scene save needed).
/// Safe to run again: a vehicle that already has an EngineSound keeps its tuning, only empty clip slots are filled.
/// The loops are made from the downloaded clips by Art/Sounds/make_loops.py (see Assets/Sounds/AUDIO_NOTES.md).
/// </summary>
public static class EngineSoundSetup
{
    private const string LoopFolder = "Assets/Sounds/Loops/";
    private const string Idle1 = LoopFolder + "DieselIdle1Loop.wav"; // slow, lumpy diesel idle (43 s)
    private const string Idle2 = LoopFolder + "DieselIdle2Loop.wav"; // faster, punchier idle (17 s)
    private const string Drive = LoopFolder + "DieselDriveLoop.wav"; // deep engine under load (15 s)

    private struct Tune
    {
        public string prefab, idle, drive;
        public Vector2 idlePitch, drivePitch;
        public float masterVolume;
    }

    private static readonly Tune[] Tunes =
    {
        // tractor: the slow diesel idle, the drive loop rising with speed
        new() { prefab = "Assets/Prefabs/Tractor.prefab", idle = Idle1, drive = Drive,
                idlePitch = new(1f, 1.4f), drivePitch = new(0.8f, 1.5f), masterVolume = 1f },
        // van: the quicker idle and everything pitched up (smaller engine, faster)
        new() { prefab = "Assets/Prefabs/Van.prefab", idle = Idle2, drive = Drive,
                idlePitch = new(1.1f, 1.6f), drivePitch = new(1f, 1.9f), masterVolume = 0.85f },
        // harvester: the slow idle pitched down (big, heavy engine)
        new() { prefab = "Assets/Prefabs/Harvester.prefab", idle = Idle1, drive = Drive,
                idlePitch = new(0.85f, 1.15f), drivePitch = new(0.7f, 1.2f), masterVolume = 1f },
    };

    [MenuItem("Tools/Engine Sound Setup")]
    private static void Run()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Engine Sound Setup", "Leave Play mode first.", "OK");
            return;
        }

        foreach (var path in new[] { Idle1, Idle2, Drive })
            if (!SetImport(path)) return;

        var report = new System.Text.StringBuilder();
        foreach (var tune in Tunes) report.AppendLine(SetupPrefab(tune));

        AssetDatabase.SaveAssets();
        Debug.Log("Engine Sound Setup done:\n" + report);
        EditorUtility.DisplayDialog("Engine Sound Setup", report +
            "\nPress Play and drive: the engine values show on screen (Show Debug on each vehicle's Engine Sound). " +
            "Tune them in Play mode, then copy the values back (Play mode changes are lost).", "OK");
    }

    /// <summary>Mono, kept uncompressed in memory once loaded (loops cleanly, and these are small), Vorbis in the build.</summary>
    private static bool SetImport(string path)
    {
        if (AssetImporter.GetAtPath(path) is not AudioImporter importer)
        {
            EditorUtility.DisplayDialog("Engine Sound Setup",
                $"{path} is missing. Run  python3 Art/Sounds/make_loops.py  in the project folder first.", "OK");
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

    private static string SetupPrefab(Tune tune)
    {
        var root = PrefabUtility.LoadPrefabContents(tune.prefab);
        try
        {
            if (root.GetComponent<Vehicle>() == null) return $"{tune.prefab}: no Vehicle on it, skipped.";

            var engine = root.GetComponent<EngineSound>();
            bool isNew = engine == null;
            if (isNew)
            {
                engine = root.AddComponent<EngineSound>();
                engine.idlePitch = tune.idlePitch;
                engine.drivePitch = tune.drivePitch;
                engine.masterVolume = tune.masterVolume;
                engine.showDebug = true; // on while tuning; untick before a real build
            }
            if (engine.idleClip == null) engine.idleClip = AssetDatabase.LoadAssetAtPath<AudioClip>(tune.idle);
            if (engine.driveClip == null) engine.driveClip = AssetDatabase.LoadAssetAtPath<AudioClip>(tune.drive);

            PrefabUtility.SaveAsPrefabAsset(root, tune.prefab);
            return $"{System.IO.Path.GetFileNameWithoutExtension(tune.prefab)}: " +
                   (isNew ? "Engine Sound added." : "already had an Engine Sound, tuning kept.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
