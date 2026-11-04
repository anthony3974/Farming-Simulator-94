using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

// One-off editor tool: sets the Farming Simulator 94 app icon in Project Settings > Player > Icon, from the
// PNGs in Assets/Images/Icon (drawn by Art/Icon/make_icon.py):
//   Default icon (every platform)  Icon.png
//   Android Adaptive               IconBackground.png (back layer) + IconForeground.png (front layer)
//   Android Round                  IconRound.png
//   Android Legacy                 Icon.png
// It also sets the PNGs' import settings so Unity keeps them sharp (no compression, no resizing to a power of 2).
// Use: Tools > Set App Icons, then build to the phone. Delete this file afterwards (git history).
// The Play Store listing icon (Art/Icon/PlayStoreIcon.png, 512 x 512) is uploaded in the Play Console, not here.
public static class AppIconSetter
{
    const string Folder = "Assets/Images/Icon/";

    [MenuItem("Tools/Set App Icons")]
    static void SetIcons()
    {
        var icon = Load("Icon.png");
        var round = Load("IconRound.png");
        var background = Load("IconBackground.png");
        var foreground = Load("IconForeground.png");
        if (icon == null || round == null || background == null || foreground == null)
        {
            EditorUtility.DisplayDialog("App Icons", $"Missing icon images in {Folder}. Run Art/Icon/make_icon.py first.", "OK");
            return;
        }

        PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

        var android = NamedBuildTarget.Android;
        var adaptive = PlayerSettings.GetPlatformIcons(android, AndroidPlatformIconKind.Adaptive);
        foreach (var i in adaptive) { i.SetTexture(background, 0); i.SetTexture(foreground, 1); }
        PlayerSettings.SetPlatformIcons(android, AndroidPlatformIconKind.Adaptive, adaptive);

        var roundIcons = PlayerSettings.GetPlatformIcons(android, AndroidPlatformIconKind.Round);
        foreach (var i in roundIcons) i.SetTexture(round);
        PlayerSettings.SetPlatformIcons(android, AndroidPlatformIconKind.Round, roundIcons);

        var legacy = PlayerSettings.GetPlatformIcons(android, AndroidPlatformIconKind.Legacy);
        foreach (var i in legacy) i.SetTexture(icon);
        PlayerSettings.SetPlatformIcons(android, AndroidPlatformIconKind.Legacy, legacy);

        AssetDatabase.SaveAssets();
        Debug.Log($"App icons set: default, Android adaptive ({adaptive.Length} sizes), round ({roundIcons.Length}) and legacy ({legacy.Length}). "
            + "See Project Settings > Player > Icon, then build to the phone.");
    }

    static Texture2D Load(string file)
    {
        string path = Folder + file;
        AssetDatabase.ImportAsset(path);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return null;

        bool changed = importer.textureType != TextureImporterType.Default || importer.npotScale != TextureImporterNPOTScale.None
            || importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed || !importer.alphaIsTransparency;
        if (changed)
        {
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None; // the 432 px layers stay 432
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
