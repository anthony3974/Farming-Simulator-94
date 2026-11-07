using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor tool: builds the street lamp (PLAN item 13) as a prefab and puts one in the middle of the map to look at.
// Use: open GameScene, Tools > Street Lamp Builder, press "Build / rebuild street lamp", save the scene (Ctrl+S).
//
// - The prefab: Assets/Prefabs/Street Lamp.prefab (the parts are listed in StreetLampModel.cs). Every lamp on the map
//   will be a copy of it, so a change to the prefab changes them all. Materials are saved in Assets/StreetLamp.
// - The test lamp: "Street Lamp 1" under Terrain > "Street Lamps". First run: on the farm side of the main road
//   ("Road"), as close to the middle of the map as a free spot allows, arm out over the road. Later runs keep
//   wherever it is (move it, run again, and it is set back on the ground; the prefab is rebuilt).
// - The light is a warm spot light pointing down, on all the time for now (the day / night cycle will switch it).
//   It is set to Important so it shows on phones too (Android Medium quality has 1 pixel light, the sun's). With
//   many lamps that will need another plan (see PLAN, daylight cycle). No shadows, to keep it cheap.
public class StreetLampBuilder : EditorWindow
{
    const string SceneName = "GameScene";
    const string PrefabPath = "Assets/Prefabs/Street Lamp.prefab";
    const string MaterialFolder = "Assets/StreetLamp";
    const string GroupName = "Street Lamps";
    const string LampName = "Street Lamp 1";
    const string RoadName = "Road";
    const float RoadGap = 1.0f;   // pole centre this far back from the road edge
    const float Clearance = 1.5f; // free space wanted around the pole

    [MenuItem("Tools/Street Lamp Builder")]
    static void Open() { GetWindow<StreetLampBuilder>("Street Lamp"); }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("Builds the street lamp prefab (Assets/Prefabs/Street Lamp.prefab) and puts one lamp, light on, beside the " +
            "main road in the middle of the map. Run it with GameScene open, then save the scene (Ctrl+S).", MessageType.Info);
        EditorGUILayout.HelpBox("Running it again rebuilds the prefab and keeps the lamp where it is. The light is easier to see " +
            "with the Directional Light turned off for a moment (it is daytime in the scene).", MessageType.None);
        if (GUILayout.Button("Build / rebuild street lamp")) Build();
    }

    static void Build()
    {
        const string title = "Street Lamp";
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(title, "Stop Play mode first. Changes made in Play mode are lost.", "OK");
            return;
        }
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != SceneName)
        {
            EditorUtility.DisplayDialog(title, $"Open {SceneName} first (the open scene is '{scene.name}').", "OK");
            return;
        }
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) { EditorUtility.DisplayDialog(title, "No terrain found in the open scene.", "OK"); return; }

        var prefab = BuildPrefab();
        if (prefab == null) { EditorUtility.DisplayDialog(title, "Could not save the prefab, see the Console.", "OK"); return; }

        Undo.SetCurrentGroupName("Build street lamp");
        int undoGroup = Undo.GetCurrentGroup();

        // the group under Terrain (next to Buildings, Fields, Roads and paths)
        var terrainRoot = terrain.transform;
        Transform group = terrainRoot.Find(GroupName);
        if (group == null)
        {
            group = new GameObject(GroupName).transform;
            Undo.RegisterCreatedObjectUndo(group.gameObject, "Build street lamp");
            group.SetParent(terrainRoot, false);
            group.localPosition = Vector3.zero;
        }

        // the test lamp: kept if it is there (and still a copy of the prefab), else placed on the first run
        Transform lamp = group.Find(LampName);
        Vector3 pos;
        Quaternion rot;
        if (lamp != null && PrefabUtility.GetCorrespondingObjectFromSource(lamp.gameObject) == prefab)
        {
            pos = lamp.position;
            rot = lamp.rotation;
        }
        else
        {
            if (lamp != null)
            {
                pos = lamp.position;
                rot = lamp.rotation;
                Undo.DestroyObjectImmediate(lamp.gameObject);
            }
            else FirstSpot(terrain, out pos, out rot);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
            Undo.RegisterCreatedObjectUndo(go, "Build street lamp");
            go.name = LampName;
            lamp = go.transform;
        }

        Undo.RecordObject(lamp, "Build street lamp");
        pos.y = terrain.SampleHeight(pos) + terrain.transform.position.y;
        lamp.SetPositionAndRotation(pos, rot);
        lamp.localScale = Vector3.one;

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = lamp.gameObject;
        SceneView.lastActiveSceneView?.Frame(new Bounds(pos + Vector3.up * 4f, new Vector3(8f, 10f, 8f)), false);
        Debug.Log($"Street lamp built: prefab {PrefabPath}, '{LampName}' at X {pos.x:0.0}, Z {pos.z:0.0}. Save the scene (Ctrl+S).", lamp);
    }

    // Builds the lamp in a temporary object, saves it over the prefab (same file, so copies in the scene update), then
    // deletes the temporary object.
    static GameObject BuildPrefab()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        var temp = new GameObject("Street Lamp");
        try
        {
            var model = new GameObject("Model").transform;
            model.SetParent(temp.transform, false);
            StreetLampModel.Build(new SceneKit(model, new Mats()));
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
                if (t.GetComponent<Light>() == null) t.gameObject.isStatic = true;
            var saved = PrefabUtility.SaveAsPrefabAsset(temp, PrefabPath, out bool ok);
            return ok ? saved : null;
        }
        finally { DestroyImmediate(temp); }
    }

    // First run: just off the farm-side edge of the main road (lower Z, where the farm is), at the middle of the map
    // in X; slides along the road in 3 m steps if a building, field or something else is in the way.
    static void FirstSpot(Terrain terrain, out Vector3 pos, out Quaternion rot)
    {
        Vector3 o = terrain.transform.position, size = terrain.terrainData.size;
        var middle = new Vector3(o.x + size.x / 2f, 0f, o.z + size.z / 2f);
        rot = Quaternion.identity; // arm along +Z

        var roads = GameObject.Find("Roads and paths");
        var road = roads != null ? roads.transform.Find(RoadName) : null;
        var roadRenderer = road != null ? road.GetComponent<Renderer>() : null;
        if (roadRenderer == null)
        {
            Debug.LogWarning($"No '{RoadName}' object found, the lamp goes in the middle of the map. Move it and run the tool again.");
            pos = middle;
            return;
        }
        Bounds rb = roadRenderer.bounds;
        // the side of the road nearer the middle of the map; the arm points across the road
        bool below = middle.z <= rb.center.z;
        float z = below ? rb.min.z - RoadGap : rb.max.z + RoadGap;
        if (!below) rot = Quaternion.Euler(0f, 180f, 0f);

        var blockers = Blockers(roadRenderer);
        for (int step = 0; step <= 40; step++)
        {
            float dx = (step % 2 == 1 ? 1 : -1) * ((step + 1) / 2) * 3f; // 0, +3, -3, +6, -6, ...
            var p = new Vector3(middle.x + dx, 0f, z);
            if (p.x < o.x + 5f || p.x > o.x + size.x - 5f) continue;
            if (!Blocked(p, blockers)) { pos = p; return; }
        }
        Debug.LogWarning("No free spot found beside the road near the middle of the map, the lamp may stand in something. Move it and run the tool again.");
        pos = new Vector3(middle.x, 0f, z);
    }

    static List<Bounds> Blockers(Renderer road)
    {
        var list = new List<Bounds>();
        foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (r == road || r is ParticleSystemRenderer || r.GetComponent<Terrain>() != null) continue;
            if (r.GetComponentInParent<Canvas>() != null) continue; // UI
            if (r.transform.IsChildOf(road.transform.parent) && r.bounds.size.y < 0.2f) continue; // other flat roads and paths are fine to stand beside, not on
            list.Add(r.bounds);
        }
        return list;
    }

    static bool Blocked(Vector3 p, List<Bounds> blockers)
    {
        foreach (var b in blockers)
            if (p.x > b.min.x - Clearance && p.x < b.max.x + Clearance && p.z > b.min.z - Clearance && p.z < b.max.z + Clearance)
                return true;
        return false;
    }

    // materials are saved as assets in Assets/StreetLamp, so the prefab keeps them; an existing one is reused as it is
    class Mats
    {
        readonly Dictionary<LampMat, Material> cache = new();

        public Material Get(LampMat key)
        {
            if (cache.TryGetValue(key, out var mat)) return mat;
            var (name, color, gloss, glow) = StreetLampModel.MaterialInfo(key);
            string path = $"{MaterialFolder}/{name}.mat";
            mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets", "StreetLamp");
                mat = new Material(Shader.Find("Standard")) { color = color };
                mat.SetFloat("_Glossiness", gloss);
                if (glow)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", color * 1.6f);
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                }
                AssetDatabase.CreateAsset(mat, path);
            }
            cache[key] = mat;
            return mat;
        }
    }

    // makes the parts as Unity primitives under a Transform
    class SceneKit : LampKit
    {
        readonly Transform parent;
        readonly Mats mats;
        public SceneKit(Transform parent, Mats mats) { this.parent = parent; this.mats = mats; }

        public override void Box(string name, Vector3 pos, Vector3 size, LampMat mat, Vector3 euler = default)
            => Prim(PrimitiveType.Cube, name, pos, size, mat, false, euler);

        // Unity's cylinder is 2 units tall, so the Y scale is half the height
        public override void Cyl(string name, Vector3 pos, float diameter, float height, LampMat mat, bool collider = false, Vector3 euler = default)
            => Prim(PrimitiveType.Cylinder, name, pos, new Vector3(diameter, height / 2f, diameter), mat, collider, euler);

        public override void Ball(string name, Vector3 pos, float diameter, LampMat mat)
            => Prim(PrimitiveType.Sphere, name, pos, Vector3.one * diameter, mat, false, default);

        public override void Spot(string name, Vector3 pos, Vector3 euler, float range, float angle, float intensity, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            var light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            light.range = range;
            light.spotAngle = angle;
            light.innerSpotAngle = angle * 0.6f;
            light.intensity = intensity;
            light.color = color;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel; // "Important"
            light.lightmapBakeType = LightmapBakeType.Realtime;
        }

        // all positions are local to the parent; only the parts asked for keep their collider (the pole you could drive into)
        void Prim(PrimitiveType type, string name, Vector3 pos, Vector3 scale, LampMat mat, bool collider, Vector3 euler)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (!collider) DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mats.Get(mat);
        }
    }
}
