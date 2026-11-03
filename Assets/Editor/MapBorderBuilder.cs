using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-off editor tool: builds a wall of concrete road-divider blocks around the edge of the terrain.
// Use: open GameScene, then Tools > Map Border Builder, check the numbers, press "Build border", save the scene.
// Placeholder blocks are plain grey boxes (a jersey barrier is roughly 3.7 m long, 0.6 m wide, 0.8 m high).
// To use a real model later, drag it into "Barrier prefab" and the tool places that instead of boxes.
// Running it again deletes the old "Map Border" object and rebuilds it.
public class MapBorderBuilder : EditorWindow
{
    const string RootName = "Map Border";

    float length = 3.7f;      // along the wall
    float width = 0.6f;       // thickness
    float height = 0.8f;      // visible height above the ground
    float gap = 0.0f;         // space between blocks (0 = touching)
    float inset = 1.0f;       // how far in from the terrain edge the wall sits
    float sink = 0.3f;        // extra depth buried below ground so slopes show no gaps
    GameObject barrierPrefab; // optional: your own model (length along its X axis, pivot at its centre)

    [MenuItem("Tools/Map Border Builder")]
    static void Open() { GetWindow<MapBorderBuilder>("Map Border"); }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("Builds a ring of concrete-style blocks around the edge of the terrain. Run it with GameScene open, then save the scene.", MessageType.Info);
        length = EditorGUILayout.FloatField("Block length (m)", length);
        width = EditorGUILayout.FloatField("Block width (m)", width);
        height = EditorGUILayout.FloatField("Block height (m)", height);
        gap = EditorGUILayout.FloatField("Gap between blocks (m)", gap);
        inset = EditorGUILayout.FloatField("Inset from edge (m)", inset);
        sink = EditorGUILayout.FloatField("Buried depth (m)", sink);
        barrierPrefab = (GameObject)EditorGUILayout.ObjectField("Barrier prefab (optional)", barrierPrefab, typeof(GameObject), false);

        EditorGUILayout.Space();
        if (GUILayout.Button("Build border")) Build();
        if (GUILayout.Button("Remove border"))
        {
            var old = GameObject.Find(RootName);
            if (old != null) Undo.DestroyObjectImmediate(old);
        }
    }

    void Build()
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) { EditorUtility.DisplayDialog("Map Border", "No terrain found in the open scene. Open GameScene first.", "OK"); return; }
        if (length <= 0.1f || width <= 0.05f || height <= 0.05f) { EditorUtility.DisplayDialog("Map Border", "Block sizes must be above zero.", "OK"); return; }

        var old = GameObject.Find(RootName);
        if (old != null) Undo.DestroyObjectImmediate(old);

        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        float minX = origin.x + inset, maxX = origin.x + size.x - inset;
        float minZ = origin.z + inset, maxZ = origin.z + size.z - inset;

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Build map border");
        root.isStatic = true;

        Material mat = barrierPrefab == null ? GetConcreteMaterial() : null;
        int count = 0;

        // Bottom and top walls run along X; left and right walls run along Z (rotated 90 degrees).
        count += Wall(root.transform, terrain, mat, new Vector3(minX, 0, minZ), new Vector3(maxX, 0, minZ), 0f);
        count += Wall(root.transform, terrain, mat, new Vector3(minX, 0, maxZ), new Vector3(maxX, 0, maxZ), 0f);
        count += Wall(root.transform, terrain, mat, new Vector3(minX, 0, minZ), new Vector3(minX, 0, maxZ), 90f);
        count += Wall(root.transform, terrain, mat, new Vector3(maxX, 0, minZ), new Vector3(maxX, 0, maxZ), 90f);

        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log($"Map border built: {count} blocks. Save the scene (Ctrl+S).");
    }

    // Places blocks from a to b (same Z or same X), snapped to the terrain height. Returns how many were placed.
    int Wall(Transform parent, Terrain terrain, Material mat, Vector3 a, Vector3 b, float yaw)
    {
        float step = length + gap;
        float total = Vector3.Distance(a, b);
        int n = Mathf.CeilToInt(total / step);
        Vector3 dir = (b - a).normalized;
        int placed = 0;
        for (int i = 0; i <= n; i++)
        {
            // The last block is pulled back so it never pokes outside the wall end.
            float d = Mathf.Min(i * step + length * 0.5f, total - length * 0.5f);
            if (i > 0 && d <= (i - 1) * step + length * 0.5f + 0.01f) break;
            Vector3 p = a + dir * d;

            // Highest ground under the block, so no gap shows on slopes.
            float ground = float.MinValue;
            for (int k = -1; k <= 1; k++)
            {
                Vector3 q = p + dir * (k * length * 0.5f);
                ground = Mathf.Max(ground, terrain.SampleHeight(q) + terrain.transform.position.y);
            }
            p.y = ground - sink + (height + sink) * 0.5f;

            GameObject block;
            if (barrierPrefab != null)
            {
                block = (GameObject)PrefabUtility.InstantiatePrefab(barrierPrefab);
                p.y = ground;
            }
            else
            {
                block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.transform.localScale = new Vector3(length, height + sink, width);
                block.GetComponent<Renderer>().sharedMaterial = mat;
            }
            block.name = "Barrier " + (placed + 1);
            block.transform.SetParent(parent, true);
            block.transform.position = p;
            block.transform.rotation = Quaternion.Euler(0, yaw, 0);
            block.isStatic = true;
            placed++;
            if (d >= total - length * 0.5f - 0.001f) break;
        }
        return placed;
    }

    static Material GetConcreteMaterial()
    {
        const string folder = "Assets/MapBorder";
        const string path = folder + "/ConcreteBarrier.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "MapBorder");
        mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.62f, 0.62f, 0.60f);
        mat.SetFloat("_Glossiness", 0.1f);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
