using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Editor tool: adds a simple V (chevron) tread to every tire of the Tractor, Van and Harvester prefabs, so you can see
// the tires roll (Vehicle spins them while driving, see "Roll Wheels" on Vehicle).
// Use: Tools > Tire Tread Builder, press "Build / rebuild tread". No scene needed: it changes the three prefabs, and
// the vehicles in GameScene (copies of the prefabs) update with them. Then commit the prefabs and Assets/Vehicles/TireTread.mat.
//
// - Each tire (FlTire, FrTire, RlTire, RrTire, BlTire, BrTire) gets a child "Tread" holding the lugs: real cubes,
//   two per lug ("Lug 01 L" and "Lug 01 R"), meeting in the middle of the tire to make a V that points forward on top
//   of the tire, like a tractor tire. Select and move them like any other part.
// - "Tread" undoes the tire's squashed scale (the tires are scaled cylinders), so the lugs are not stretched and can
//   be angled. The lugs have no collider and cast no shadow (cheap on phones).
// - Running it again deletes the old "Tread" objects and builds new ones with the settings in the window, so hand
//   changes to the lugs are lost. "Remove tread" takes them all off again.
// - Lugs use Assets/Vehicles/TireTread.mat (a copy of the tire rubber, a bit lighter so the V shows). Made on the first
//   run only, so a colour picked on it later is kept.
// Delete this file when the owner is happy with the tread (it stays in git history).
public class TireTreadBuilder : EditorWindow
{
    static readonly string[] PrefabPaths =
    {
        "Assets/Prefabs/Tractor.prefab",
        "Assets/Prefabs/Van.prefab",
        "Assets/Prefabs/Harvester.prefab",
    };
    const string MaterialPath = "Assets/Vehicles/TireTread.mat";
    const string TreadName = "Tread";
    static readonly Color TreadColor = new(0.2f, 0.2f, 0.2f, 1f);

    // window settings (sizes in metres, before the Van's overall 1.28 scale)
    float lugSpacing = 0.4f;   // distance between lugs around the tire
    float vAngle = 35f;        // how far back each half of the V sweeps, in degrees (0 = straight bars across)
    float lugHeight = 0.09f;   // how far a lug sticks out, as a part of the tire radius
    float lugThickness = 0.12f; // how thick a lug bar is, as a part of the tire radius

    [MenuItem("Tools/Tire Tread Builder")]
    static void Open() { GetWindow<TireTreadBuilder>("Tire Tread"); }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("Adds a V tread (cube lugs) to every tire of the Tractor, Van and Harvester prefabs. " +
            "The vehicles in the scene update with their prefabs. Commit the three prefabs and Assets/Vehicles/TireTread.mat afterwards.",
            MessageType.Info);
        lugSpacing = EditorGUILayout.Slider(new GUIContent("Lug spacing (m)", "Distance between lugs around the tire. Smaller = more lugs."), lugSpacing, 0.2f, 1f);
        vAngle = EditorGUILayout.Slider(new GUIContent("V angle", "How far back each half of the V sweeps. 0 = straight bars."), vAngle, 0f, 60f);
        lugHeight = EditorGUILayout.Slider(new GUIContent("Lug height", "How far a lug sticks out, as a part of the tire radius."), lugHeight, 0.03f, 0.2f);
        lugThickness = EditorGUILayout.Slider(new GUIContent("Lug thickness", "How thick a lug bar is, as a part of the tire radius."), lugThickness, 0.05f, 0.3f);
        EditorGUILayout.HelpBox("Running it again replaces the tread (hand changes to the lugs are lost).", MessageType.None);
        if (GUILayout.Button("Build / rebuild tread")) Run(true);
        if (GUILayout.Button("Remove tread")) Run(false);
    }

    void Run(bool build)
    {
        const string title = "Tire Tread";
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(title, "Stop Play mode first.", "OK");
            return;
        }

        Material mat = null;
        var report = new List<string>();
        foreach (var path in PrefabPaths)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                report.Add($"{path}: not found, skipped");
                continue;
            }
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                int tires = 0, lugs = 0;
                foreach (var tire in FindTires(root.transform))
                {
                    var old = tire.Find(TreadName);
                    if (old != null) DestroyImmediate(old.gameObject);
                    if (build)
                    {
                        if (mat == null) mat = TreadMaterial(tire);
                        lugs += BuildTread(root.transform, tire, mat);
                    }
                    tires++;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path, out bool ok);
                report.Add(ok
                    ? $"{path}: {tires} tires" + (build ? $", {lugs} lugs" : ", tread removed")
                    : $"{path}: could not save, see the Console");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        string text = string.Join("\n", report);
        Debug.Log("Tire Tread Builder:\n" + text);
        EditorUtility.DisplayDialog(title, text, "OK");
    }

    // FlTire, FrTire, RlTire, RrTire, BlTire, BrTire (the same names Vehicle steers and rolls), turned off ones too
    static List<Transform> FindTires(Transform root)
    {
        var list = new List<Transform>();
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (Vehicle.IsTireName(t.name)) list.Add(t);
        return list;
    }

    // the lugs' material: made once from the tire's rubber, then reused as it is
    static Material TreadMaterial(Transform tire)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat != null) return mat;
        var rubber = tire.GetComponent<Renderer>() != null ? tire.GetComponent<Renderer>().sharedMaterial : null;
        mat = rubber != null ? new Material(rubber) : new Material(Shader.Find("Standard"));
        mat.color = TreadColor;
        if (!AssetDatabase.IsValidFolder("Assets/Vehicles")) AssetDatabase.CreateFolder("Assets", "Vehicles");
        AssetDatabase.CreateAsset(mat, MaterialPath);
        return mat;
    }

    // Tires are Unity cylinders (radius 0.5, height 2 along their own Y = the axle) scaled to size. The Tread object
    // cancels that scale, so inside it one unit is one unit of the tire's parent and the lugs can be angled freely.
    int BuildTread(Transform vehicle, Transform tire, Material mat)
    {
        Vector3 s = tire.localScale;
        var tread = new GameObject(TreadName).transform;
        tread.SetParent(tire, false);
        tread.localPosition = Vector3.zero;
        tread.localRotation = Quaternion.identity;
        tread.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);
        tread.gameObject.layer = tire.gameObject.layer;

        float radius = (Mathf.Abs(s.x) + Mathf.Abs(s.z)) * 0.25f; // 0.5 * the average of the two round sides
        float halfWidth = Mathf.Abs(s.y);                          // the cylinder is 2 high
        float height = radius * lugHeight;
        float thick = radius * lugThickness;
        int count = Mathf.Max(8, Mathf.RoundToInt(2f * Mathf.PI * radius / lugSpacing));

        // Which way the top of the tire moves when the vehicle drives forward, around the tire's own Y.
        // Vehicle rolls forward = turning around the vehicle's right axis; the tire's Y points left or right.
        Vector3 axle = tire.rotation * Vector3.up;
        float axleSign = Vector3.Dot(axle, vehicle.right) >= 0f ? 1f : -1f;
        float lead = -axleSign; // +1: forward rolling moves the surface along increasing angle

        float reach = halfWidth * 0.9f;                                    // each half of the V goes this far out from the middle
        float sweep = reach * Mathf.Tan(vAngle * Mathf.Deg2Rad);           // and this far back along the tire
        float surface = radius + height * 0.5f - Mathf.Min(0.01f, height * 0.25f); // sunk a little into the rubber

        for (int i = 0; i < count; i++)
        {
            float a = i * 2f * Mathf.PI / count;
            for (int side = -1; side <= 1; side += 2)
            {
                // the middle of this half: halfway out, halfway back
                float ac = a - lead * (sweep * 0.5f) / radius;
                var outward = new Vector3(Mathf.Cos(ac), 0f, Mathf.Sin(ac));
                var along = new Vector3(-Mathf.Sin(ac), 0f, Mathf.Cos(ac)); // around the tire, increasing angle
                Vector3 dir = (Vector3.up * side * reach - along * lead * sweep).normalized;
                float length = Mathf.Sqrt(reach * reach + sweep * sweep) + thick * 0.5f; // overlap in the middle so the V joins

                var lug = GameObject.CreatePrimitive(PrimitiveType.Cube);
                DestroyImmediate(lug.GetComponent<Collider>());
                lug.name = $"Lug {i + 1:00} {(side < 0 ? (axleSign < 0 ? "R" : "L") : (axleSign < 0 ? "L" : "R"))}";
                lug.layer = tire.gameObject.layer;
                var t = lug.transform;
                t.SetParent(tread, false);
                t.localPosition = outward * surface + Vector3.up * side * reach * 0.5f;
                t.localRotation = Quaternion.LookRotation(dir, outward);
                t.localScale = new Vector3(thick, height, length);

                var r = lug.GetComponent<MeshRenderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
        }
        return count * 2;
    }
}
