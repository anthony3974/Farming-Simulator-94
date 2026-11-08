using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-off tool: Tools > Game Sounds Setup. Sets the import settings of the five short sounds and makes
/// Assets/Resources/GameSounds.prefab with them in its slots, which <see cref="GameSounds"/> loads at the start of
/// the game. No scene changes. Safe to run again: it only fills empty slots, so volumes tuned on the prefab stay.
/// Delete this file after running it (it stays in git history).
/// </summary>
public static class GameSoundsSetup
{
    private const string Folder = "Assets/Sounds/";
    private const string PrefabPath = "Assets/Resources/GameSounds.prefab";
    private static readonly string[] Clips = { "ButtonClick", "MenuOpen", "MenuClose", "MoneyIn", "MoneyOut" };

    [MenuItem("Tools/Game Sounds Setup")]
    public static void Run()
    {
        // short UI sounds: mono, uncompressed, in memory, so they start without a delay
        foreach (var name in Clips)
        {
            string path = Folder + name + ".wav";
            if (AssetImporter.GetAtPath(path) is not AudioImporter importer)
            {
                Debug.LogError($"Game Sounds Setup: {path} not found.");
                return;
            }
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.SaveAndReimport();
        }

        Directory.CreateDirectory("Assets/Resources");
        AssetDatabase.Refresh();

        bool existing = File.Exists(PrefabPath);
        var root = existing ? PrefabUtility.LoadPrefabContents(PrefabPath) : new GameObject("GameSounds");
        if (!root.TryGetComponent(out GameSounds sounds)) sounds = root.AddComponent<GameSounds>();
        sounds.click = Fill(sounds.click, "ButtonClick");
        sounds.menuOpen = Fill(sounds.menuOpen, "MenuOpen");
        sounds.menuClose = Fill(sounds.menuClose, "MenuClose");
        sounds.moneyIn = Fill(sounds.moneyIn, "MoneyIn");
        sounds.moneyOut = Fill(sounds.moneyOut, "MoneyOut");
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        if (existing) PrefabUtility.UnloadPrefabContents(root);
        else Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        Debug.Log($"Game Sounds Setup: done. {PrefabPath} has the five sounds; tune the volumes on it. " +
                  "Press Play to hear them (no scene save needed). You can delete Assets/Editor/GameSoundsSetup.cs now.");
    }

    private static AudioClip Fill(AudioClip current, string name)
    {
        return current != null ? current : AssetDatabase.LoadAssetAtPath<AudioClip>(Folder + name + ".wav");
    }
}
