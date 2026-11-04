using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > Road Look > Fix top road texture.
/// Makes the TopRoad material look better on every road that uses it:
/// 1. Adds a RoadTiling script to each of those roads, so the texture is tiled by metres and not stretched.
/// 2. Turns on anisotropic filtering and trilinear filtering for the road texture, so the road stays sharp
///    when you look along it from the vehicle (without it, the road goes blurry a few metres ahead).
/// 3. Gives the asphalt a little smoothness so it is not completely flat looking.
/// Run it once with GameScene open, then save the scene. Running it again only updates the values.
/// </summary>
public static class RoadLookTool
{
    private const string MaterialPath = "Assets/Materials/Road/TopRoad.mat";
    private const float MetresPerTile = 6f;
    private const int AnisoLevel = 8;      // 8 is sharp and still cheap on phones
    private const float Smoothness = 0.12f; // dry asphalt: a hint of sheen, no mirror

    [MenuItem("Tools/Road Look/Fix top road texture")]
    public static void FixTopRoad()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat == null)
        {
            Debug.LogError($"Road Look: could not find {MaterialPath}.");
            return;
        }

        FixTexture(mat.mainTexture);

        Undo.RecordObject(mat, "Road look");
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", Smoothness);
        EditorUtility.SetDirty(mat);

        int count = 0;
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (r.sharedMaterial != mat) continue;
            var tiling = r.GetComponent<RoadTiling>();
            if (tiling == null) tiling = Undo.AddComponent<RoadTiling>(r.gameObject);
            Undo.RecordObject(tiling, "Road look");
            tiling.metresPerTile = MetresPerTile;
            tiling.Apply();
            EditorUtility.SetDirty(tiling);
            count++;
            Debug.Log($"Road Look: {r.name} tiled at {MetresPerTile} m per tile.", r);
        }

        AssetDatabase.SaveAssets();
        if (count == 0) Debug.LogWarning("Road Look: no object in the open scene uses TopRoad.mat. Is GameScene open?");
        else Debug.Log($"Road Look: done, {count} road(s) updated. Save the scene (Ctrl+S). "
                     + "Change 'Metres Per Tile' on a road's RoadTiling to make the grain finer or coarser.");
    }

    private static void FixTexture(Texture tex)
    {
        if (tex == null) return;
        string path = AssetDatabase.GetAssetPath(tex);
        if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return;

        bool changed = false;
        if (importer.anisoLevel < AnisoLevel) { importer.anisoLevel = AnisoLevel; changed = true; }
        if (importer.filterMode != FilterMode.Trilinear) { importer.filterMode = FilterMode.Trilinear; changed = true; }
        if (importer.wrapMode != TextureWrapMode.Repeat) { importer.wrapMode = TextureWrapMode.Repeat; changed = true; }
        if (!importer.mipmapEnabled) { importer.mipmapEnabled = true; changed = true; }
        if (changed)
        {
            importer.SaveAndReimport();
            Debug.Log($"Road Look: {path} now uses anisotropic {AnisoLevel}x, trilinear filtering.");
        }
    }
}
