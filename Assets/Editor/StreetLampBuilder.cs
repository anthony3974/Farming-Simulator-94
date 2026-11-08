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
// - The light is a warm spot light pointing down. The prefab's NightLight turns it (and the glowing lens) on only at
//   night, following the DayNightCycle on the sun; in a scene with no cycle it stays on.
//   It is set to Important so it shows on phones too (Android Medium quality has 1 pixel light, the sun's). With
//   many lamps that will need another plan (see PLAN, daylight cycle). No shadows, to keep it cheap.
// - Lamp Scale (in the window) sizes the whole lamp: StreetLampModel stays the 1x design and its "Model" object is
//   scaled. A Light's Range ignores scale, so the tool multiplies it too; the Intensity stays, because Unity's falloff
//   depends on distance / Range, which scaling both leaves the same. Head Distance (in the window) sets the arm's
//   length, with the brace following it. To keep a new value, change its default below.
// - "Place lamps along the main road" adds copies of the prefab ("Road Lamp 01", ...) in line with Street Lamp 1, every
//   Road Lamp Spacing metres, all on Street Lamp 1's side and stopping at a road that crosses the whole main road.
//   A lamp whose spot is taken slides up to 12 m along the road and is left out if that is not free either.
//   Running it again replaces those lamps, so hand moves to them are lost.
// - "Add 3 spare lamps" adds "Spare Lamp 1" to "Spare Lamp 3" just along the road from Street Lamp 1, to move by hand.
//   Running it again only adds the ones that are missing, and the road lamp button leaves them alone.
// - "Even out lamp spacing" spreads every lamp in line with Street Lamp 1 equally along that line, moving X only. The
//   two end lamps stay, and so does a lamp whose even spot is taken (the lamps either side of it are evened around it).
// - "Place lamps along the sub road" adds "Sub Road Lamp 01", ... on the west side of SubRoad, stepping south from the
//   junction every Road Lamp Spacing metres, arms out over the road. Same sliding and replacing as the main road button.
public class StreetLampBuilder : EditorWindow
{
    const string SceneName = "GameScene";
    const string PrefabPath = "Assets/Prefabs/Street Lamp.prefab";
    const string MaterialFolder = "Assets/StreetLamp";
    const string GroupName = "Street Lamps";
    const string LampName = "Street Lamp 1";
    const string RoadName = "Road";
    const string SubRoadName = "SubRoad";
    const float RoadGap = 1.0f;   // pole centre this far back from the road edge
    const float Clearance = 1.5f; // free space wanted around the pole
    const string RoadLampPrefix = "Road Lamp ";
    const string SubRoadLampPrefix = "Sub Road Lamp ";
    const float EndGap = 5f;      // no lamp closer than this to the end of the road
    const int RoadLampSlides = 8; // 3 m steps each way, so up to 12 m
    const string SpareLampPrefix = "Spare Lamp ";
    const int SpareLamps = 3;
    const float SpareLampGap = 15f;
    const float SameLine = 1f;    // a lamp this close to Street Lamp 1's Z counts as in line with it

    [SerializeField] float lampScale = 2.5f;
    [SerializeField] float headDistance = 10f; // real metres from the pole to the centre of the lamp head; keeps its length when Lamp Scale changes
    [SerializeField] float roadLampSpacing = 70f; // the light reaches about 35 m each way at 2.5x, so the pools just touch

    [MenuItem("Tools/Street Lamp Builder")]
    static void Open() { GetWindow<StreetLampBuilder>("Street Lamp"); }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("Builds the street lamp prefab (Assets/Prefabs/Street Lamp.prefab) and puts one lamp, light on, beside the " +
            "main road in the middle of the map. Run it with GameScene open, then save the scene (Ctrl+S).", MessageType.Info);
        EditorGUILayout.HelpBox("Running it again rebuilds the prefab and keeps the lamp where it is. The light is easier to see " +
            "with the Directional Light turned off for a moment (it is daytime in the scene).", MessageType.None);
        lampScale = Mathf.Max(0.1f, EditorGUILayout.FloatField("Lamp Scale", lampScale));
        EditorGUILayout.LabelField($"Lamp height {StreetLampModel.Height * lampScale:0.0} m");
        headDistance = Mathf.Max(StreetLampModel.MinHeadOut * lampScale, EditorGUILayout.FloatField("Head Distance (m)", headDistance));
        if (GUILayout.Button("Build / rebuild street lamp")) Build(lampScale, headDistance);
        EditorGUILayout.Space();
        roadLampSpacing = Mathf.Max(20f, EditorGUILayout.FloatField("Road Lamp Spacing (m)", roadLampSpacing));
        if (GUILayout.Button("Place lamps along the main road")) PlaceAlongRoad(roadLampSpacing);
        if (GUILayout.Button("Place lamps along the sub road")) PlaceAlongSubRoad(roadLampSpacing);
        if (GUILayout.Button("Add 3 spare lamps")) AddSpareLamps();
        if (GUILayout.Button("Even out lamp spacing")) EvenOutLamps();
    }

    static bool FindRoad(string title, string roadName, out Renderer roadRenderer)
    {
        var roads = GameObject.Find("Roads and paths");
        var road = roads != null ? roads.transform.Find(roadName) : null;
        roadRenderer = road != null ? road.GetComponent<Renderer>() : null;
        if (roadRenderer != null) return true;
        EditorUtility.DisplayDialog(title, $"No '{roadName}' object found under 'Roads and paths'.", "OK");
        return false;
    }

    // Everything a pole must not stand in. Other roads count here (Blockers lets flat ones through); terrain trees are not Renderers.
    static List<Bounds> PlacementBlockers(Terrain terrain, Renderer roadRenderer, Transform ignore = null)
    {
        var blockers = Blockers(roadRenderer, ignore);
        foreach (var r in roadRenderer.transform.parent.GetComponentsInChildren<Renderer>())
            if (r != roadRenderer) blockers.Add(r.bounds);
        Vector3 terrainPos = terrain.transform.position, terrainSize = terrain.terrainData.size;
        foreach (var tree in terrain.terrainData.treeInstances)
            blockers.Add(new Bounds(terrainPos + Vector3.Scale(tree.position, terrainSize), Vector3.one));
        return blockers;
    }

    static bool FindFirstLamp(string title, Terrain terrain, out GameObject prefab, out Transform group, out Transform first)
    {
        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        group = terrain.transform.Find(GroupName);
        first = group != null ? group.Find(LampName) : null;
        if (prefab != null && first != null) return true;
        EditorUtility.DisplayDialog(title, "Press \"Build / rebuild street lamp\" first, the other lamps line up with that lamp.", "OK");
        return false;
    }

    static bool CanEdit(string title, out Scene scene, out Terrain terrain)
    {
        scene = SceneManager.GetActiveScene();
        terrain = Terrain.activeTerrain;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(title, "Stop Play mode first. Changes made in Play mode are lost.", "OK");
            return false;
        }
        if (scene.name != SceneName)
        {
            EditorUtility.DisplayDialog(title, $"Open {SceneName} first (the open scene is '{scene.name}').", "OK");
            return false;
        }
        if (terrain == null)
        {
            EditorUtility.DisplayDialog(title, "No terrain found in the open scene.", "OK");
            return false;
        }
        return true;
    }

    static void Build(float scale, float headDistance)
    {
        const string title = "Street Lamp";
        if (!CanEdit(title, out Scene scene, out Terrain terrain)) return;

        var prefab = BuildPrefab(scale, headDistance / scale);
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
        SceneView.lastActiveSceneView?.Frame(new Bounds(pos + Vector3.up * 4f * scale, new Vector3(8f, 10f, 8f) * scale), false);
        Debug.Log($"Street lamp built: prefab {PrefabPath}, '{LampName}' at X {pos.x:0.0}, Z {pos.z:0.0}. Save the scene (Ctrl+S).", lamp);
    }

    // Lamps along the main road, in line with Street Lamp 1 and keeping its height above the ground and its direction.
    static void PlaceAlongRoad(float spacing)
    {
        const string title = "Road Lamps";
        if (!CanEdit(title, out Scene scene, out Terrain terrain)) return;

        if (!FindFirstLamp(title, terrain, out GameObject prefab, out Transform group, out Transform first)) return;
        if (!FindRoad(title, RoadName, out Renderer roadRenderer)) return;
        Transform roads = roadRenderer.transform.parent;

        if (!ConfirmOldLamps(title, group, RoadLampPrefix, out var old)) return;

        Undo.SetCurrentGroupName("Place road lamps");
        int undoGroup = Undo.GetCurrentGroup();
        foreach (var lamp in old) Undo.DestroyObjectImmediate(lamp);

        var blockers = PlacementBlockers(terrain, roadRenderer);
        Vector3 terrainPos = terrain.transform.position;

        Bounds rb = roadRenderer.bounds;
        float z = first.position.z;
        float minX = rb.min.x + EndGap, maxX = rb.max.x - EndGap;
        foreach (var r in roads.GetComponentsInChildren<Renderer>())
        {
            Bounds b = r.bounds;
            if (r == roadRenderer || b.min.z > rb.min.z || b.max.z < rb.max.z) continue; // only a road right across this one
            if (b.center.x > first.position.x) maxX = Mathf.Min(maxX, b.min.x - EndGap);
            else minX = Mathf.Max(minX, b.max.x + EndGap);
        }

        var spots = new List<Vector3>();
        var missed = new List<float>();
        for (int dir = -1; dir <= 1; dir += 2)
            for (float x = first.position.x + dir * spacing; x >= minX && x <= maxX; x += dir * spacing)
            {
                if (FreeSpot(new Vector3(x, 0f, z), Vector3.right, minX, maxX, blockers, out Vector3 p)) spots.Add(p);
                else missed.Add(x);
            }
        spots.Sort((a, b) => a.x.CompareTo(b.x));

        for (int i = 0; i < spots.Count; i++)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
            Undo.RegisterCreatedObjectUndo(go, "Place road lamps");
            go.name = $"{RoadLampPrefix}{i + 1:00}";
            Vector3 p = spots[i];
            p.y = terrain.SampleHeight(p) + terrainPos.y;
            go.transform.SetPositionAndRotation(p, first.rotation);
            Debug.Log($"{go.name} at X {p.x:0.0}, Z {p.z:0.0}");
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = group.gameObject;
        string left = missed.Count > 0 ? $" No free spot near X {string.Join(", ", missed)}." : "";
        Debug.Log($"Placed {spots.Count} road lamps, {spacing:0} m apart.{left} Save the scene (Ctrl+S).", group);
    }

    // One row on the west side of the sub road (the east side has the farmhouse and garage against it), starting one
    // spacing south of the main road lamps.
    static void PlaceAlongSubRoad(float spacing)
    {
        const string title = "Sub Road Lamps";
        if (!CanEdit(title, out Scene scene, out Terrain terrain)) return;
        if (!FindFirstLamp(title, terrain, out GameObject prefab, out Transform group, out Transform first)) return;
        if (!FindRoad(title, SubRoadName, out Renderer roadRenderer)) return;
        if (!ConfirmOldLamps(title, group, SubRoadLampPrefix, out var old)) return;

        Undo.SetCurrentGroupName("Place sub road lamps");
        int undoGroup = Undo.GetCurrentGroup();
        foreach (var lamp in old) Undo.DestroyObjectImmediate(lamp);

        var blockers = PlacementBlockers(terrain, roadRenderer);
        Bounds rb = roadRenderer.bounds;
        float x = rb.min.x - RoadGap;
        float minZ = rb.min.z + EndGap, maxZ = rb.max.z - EndGap;
        var armOverRoad = Quaternion.Euler(0f, 90f, 0f); // the lamp's arm is +Z, this turns it to +X

        int placed = 0;
        var missed = new List<float>();
        for (float z = first.position.z - spacing; z >= minZ; z -= spacing)
        {
            if (!FreeSpot(new Vector3(x, 0f, z), Vector3.forward, minZ, maxZ, blockers, out Vector3 p))
            {
                missed.Add(z);
                continue;
            }
            placed++;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
            Undo.RegisterCreatedObjectUndo(go, "Place sub road lamps");
            go.name = $"{SubRoadLampPrefix}{placed:00}";
            p.y = terrain.SampleHeight(p) + terrain.transform.position.y;
            go.transform.SetPositionAndRotation(p, armOverRoad);
            Debug.Log($"{go.name} at X {p.x:0.0}, Z {p.z:0.0}");
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = group.gameObject;
        string left = missed.Count > 0 ? $" No free spot near Z {string.Join(", ", missed)}." : "";
        Debug.Log($"Placed {placed} sub road lamps, {spacing:0} m apart.{left} Save the scene (Ctrl+S).", group);
    }

    // Lists the lamps a placement button would replace, asking first if there are any. False means cancelled.
    static bool ConfirmOldLamps(string title, Transform group, string prefix, out List<GameObject> old)
    {
        old = new List<GameObject>();
        foreach (Transform child in group)
            if (child.name.StartsWith(prefix)) old.Add(child.gameObject);
        return old.Count == 0 || EditorUtility.DisplayDialog(title,
            $"Replace the {old.Count} lamps already there? Any you moved by hand are placed again.", "Replace", "Cancel");
    }

    // Lamps to move by hand, so one already there is never touched. All three end up selected.
    static void AddSpareLamps()
    {
        const string title = "Spare Lamps";
        if (!CanEdit(title, out Scene scene, out Terrain terrain)) return;
        if (!FindFirstLamp(title, terrain, out GameObject prefab, out Transform group, out Transform first)) return;

        Undo.SetCurrentGroupName("Add spare lamps");
        int undoGroup = Undo.GetCurrentGroup();
        var spares = new List<Object>();
        int added = 0;
        for (int i = 1; i <= SpareLamps; i++)
        {
            string lampName = SpareLampPrefix + i;
            Transform spare = group.Find(lampName);
            if (spare == null)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
                Undo.RegisterCreatedObjectUndo(go, "Add spare lamps");
                go.name = lampName;
                spare = go.transform;
                Vector3 p = first.position + Vector3.right * (SpareLampGap * i);
                p.y = terrain.SampleHeight(p) + terrain.transform.position.y;
                spare.SetPositionAndRotation(p, first.rotation);
                added++;
            }
            spares.Add(spare.gameObject);
        }

        Undo.CollapseUndoOperations(undoGroup);
        if (added > 0) EditorSceneManager.MarkSceneDirty(scene);
        Selection.objects = spares.ToArray();
        Debug.Log($"Added {added} spare lamps under {GroupName}. Save the scene (Ctrl+S).", group);
    }

    // Equal gaps along the line of lamps. Only X changes, and only for lamps that can go to their even spot.
    static void EvenOutLamps()
    {
        const string title = "Even Lamp Spacing";
        if (!CanEdit(title, out Scene scene, out Terrain terrain)) return;
        if (!FindFirstLamp(title, terrain, out _, out Transform group, out Transform first)) return;
        if (!FindRoad(title, RoadName, out Renderer roadRenderer)) return;

        float z = first.position.z;
        var lamps = new List<Transform>();
        foreach (Transform child in group)
            if (Mathf.Abs(child.position.z - z) < SameLine) lamps.Add(child);
        lamps.Sort((a, b) => a.position.x.CompareTo(b.position.x));
        if (lamps.Count < 3)
        {
            EditorUtility.DisplayDialog(title, "There need to be at least 3 lamps in line with Street Lamp 1.", "OK");
            return;
        }

        var blockers = PlacementBlockers(terrain, roadRenderer, group);
        var stays = new bool[lamps.Count];
        stays[0] = stays[lamps.Count - 1] = true;
        float[] targets;
        while (true)
        {
            targets = EvenTargets(lamps, stays);
            int taken = -1;
            for (int i = 0; i < lamps.Count && taken < 0; i++)
                if (!stays[i] && Blocked(new Vector3(targets[i], 0f, z), blockers)) taken = i;
            if (taken < 0) break;
            stays[taken] = true; // stays where it is; the lamps either side are worked out again around it
        }

        Undo.SetCurrentGroupName("Even out lamp spacing");
        int undoGroup = Undo.GetCurrentGroup();
        var kept = new List<string>();
        for (int i = 0; i < lamps.Count; i++)
        {
            if (stays[i]) { kept.Add(lamps[i].name); continue; }
            Undo.RecordObject(lamps[i], "Even out lamp spacing");
            Vector3 p = lamps[i].position;
            p.x = targets[i];
            p.y = terrain.SampleHeight(p) + terrain.transform.position.y;
            lamps[i].position = p;
            Debug.Log($"{lamps[i].name} at X {p.x:0.0}");
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"Evened out {lamps.Count - kept.Count} lamps. Stayed where they were: {string.Join(", ", kept)}. Save the scene (Ctrl+S).", group);
    }

    // Each run of lamps between two that stay gets equal gaps.
    static float[] EvenTargets(List<Transform> lamps, bool[] stays)
    {
        var targets = new float[lamps.Count];
        int from = 0;
        for (int to = 1; to < lamps.Count; to++)
        {
            if (!stays[to]) continue;
            float a = lamps[from].position.x, b = lamps[to].position.x;
            for (int i = from; i <= to; i++) targets[i] = Mathf.Lerp(a, b, (i - from) / (float)(to - from));
            from = to;
        }
        return targets;
    }

    // Tries the start point, then slides along the road. min and max limit the position along that (X or Z) direction.
    static bool FreeSpot(Vector3 start, Vector3 along, float min, float max, List<Bounds> blockers, out Vector3 spot)
    {
        for (int step = 0; step <= RoadLampSlides; step++)
        {
            spot = start + along * SlideOffset(step);
            float position = Vector3.Dot(spot, along);
            if (position >= min && position <= max && !Blocked(spot, blockers)) return true;
        }
        spot = default;
        return false;
    }

    // 0, +3, -3, +6, -6, ...
    static float SlideOffset(int step) => (step % 2 == 1 ? 1 : -1) * ((step + 1) / 2) * 3f;

    // Builds the lamp in a temporary object, saves it over the prefab (same file, so copies in the scene update), then
    // deletes the temporary object.
    static GameObject BuildPrefab(float scale, float headOut)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        var temp = new GameObject("Street Lamp");
        try
        {
            var model = new GameObject("Model").transform;
            model.SetParent(temp.transform, false);
            StreetLampModel.Build(new SceneKit(model, new Mats()), headOut);
            model.localScale = Vector3.one * scale;
            foreach (var light in model.GetComponentsInChildren<Light>(true)) light.range *= scale;
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
                if (t.GetComponent<Light>() == null) t.gameObject.isStatic = true;
            temp.AddComponent<NightLight>(); // light and lens on only at night (DayNightCycle)
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
            var p = new Vector3(middle.x + SlideOffset(step), 0f, z);
            if (p.x < o.x + 5f || p.x > o.x + size.x - 5f) continue;
            if (!Blocked(p, blockers)) { pos = p; return; }
        }
        Debug.LogWarning("No free spot found beside the road near the middle of the map, the lamp may stand in something. Move it and run the tool again.");
        pos = new Vector3(middle.x, 0f, z);
    }

    static List<Bounds> Blockers(Renderer road, Transform ignore = null)
    {
        var list = new List<Bounds>();
        foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (r == road || r is ParticleSystemRenderer || r.GetComponent<Terrain>() != null) continue;
            if (ignore != null && r.transform.IsChildOf(ignore)) continue;
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
