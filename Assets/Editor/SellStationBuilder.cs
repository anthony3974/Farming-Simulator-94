using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Editor tool: turns the three crop sell stations (Wheat, Barley, Oat) from plain cubes into farm
// grain elevators: tall silos with cone roofs, an elevator tower, a shed with a pitched roof, a covered
// loading bay with a weighbridge, a small office, a concrete pad and a sign with the crop name.
// Use: open GameScene, then Tools > Sell Station Builder, press "Rebuild sell stations", save the scene (Ctrl+S).
// Running it again deletes each station's "Model" child and builds it again, so the result is the same.
// It keeps each station object, its position and its Triggerbox (with the Building component and its
// Inspector values). Placeholder primitives, a real model can replace them later.
public class SellStationBuilder : EditorWindow
{
    const string ModelName = "Model";
    const string AssetFolder = "Assets/SellStations";
    const string ConePath = AssetFolder + "/SiloCone.asset";

    // size of the old cube on X and Z. The new model stays inside it (the trigger zone is bigger).
    static readonly Vector2 Footprint = new(27.5f, 35.2f);

    // which side the loading bay faces, in 2D map terms (2D up = +X, 2D right = -Z)
    static readonly string[] SideNames = { "Auto (nearest road)", "2D up (+X)", "2D down (-X)", "2D left (+Z)", "2D right (-Z)" };
    static readonly Vector3[] SideDirs = { Vector3.zero, Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
    readonly int[] sides = new int[3]; // one per crop (Wheat, Barley, Oat)

    [MenuItem("Tools/Sell Station Builder")]
    static void Open() { GetWindow<SellStationBuilder>("Sell Stations"); }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("Rebuilds the look of the Wheat, Barley and Oat sell stations (silos, tower, shed, covered loading bay, sign). " +
            "The station objects and their Triggerbox / Building settings are kept. Run it with GameScene open, then save the scene.", MessageType.Info);
        EditorGUILayout.LabelField("Loading bay faces", EditorStyles.boldLabel);
        for (int i = 0; i < sides.Length; i++)
            sides[i] = EditorGUILayout.Popup(((Building.Crop)i).ToString(), sides[i], SideNames);

        EditorGUILayout.Space();
        if (GUILayout.Button("Rebuild sell stations")) Build();
    }

    // colours per crop: the walls and silo bands use the accent, roofs and sign text the darker trim
    static (Color accent, Color trim) Colors(Building.Crop crop)
    {
        switch (crop)
        {
            case Building.Crop.Wheat: return (new Color(0.93f, 0.75f, 0.25f), new Color(0.55f, 0.36f, 0.08f));  // golden
            case Building.Crop.Barley: return (new Color(0.76f, 0.48f, 0.28f), new Color(0.42f, 0.22f, 0.10f)); // tan / amber
            default: return (new Color(0.80f, 0.85f, 0.62f), new Color(0.32f, 0.44f, 0.24f));                   // pale cream-green
        }
    }

    void Build()
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) { EditorUtility.DisplayDialog("Sell Stations", "No terrain found in the open scene. Open GameScene first.", "OK"); return; }

        var stations = new List<Building>();
        foreach (var b in FindObjectsByType<Building>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (b.type == Building.BuildingType.Sell) stations.Add(b);
        if (stations.Count == 0) { EditorUtility.DisplayDialog("Sell Stations", "No sell station (Building of type Sell) found in the open scene.", "OK"); return; }
        stations.Sort((a, b) => a.crop.CompareTo(b.crop));

        // road strips, to find which side of each station faces a road
        var roads = new List<Bounds>();
        var roadRoot = GameObject.Find("Roads and paths");
        if (roadRoot != null)
            foreach (var r in roadRoot.GetComponentsInChildren<Renderer>()) roads.Add(r.bounds);

        Undo.SetCurrentGroupName("Rebuild sell stations");
        int group = Undo.GetCurrentGroup();
        var log = new System.Text.StringBuilder("Sell stations rebuilt. Save the scene (Ctrl+S).");
        foreach (var zone in stations)
        {
            string result = BuildStation(zone, SideDirs[sides[(int)zone.crop]], terrain, roads);
            log.Append("\n").Append(result);
        }
        Undo.CollapseUndoOperations(group);

        EditorSceneManager.MarkSceneDirty(stations[0].gameObject.scene);
        Debug.Log(log.ToString());
    }

    static string BuildStation(Building zone, Vector3 chosenFront, Terrain terrain, List<Bounds> roads)
    {
        Transform root = zone.transform.parent;
        var box = zone.GetComponent<BoxCollider>();
        if (root == null || box == null)
            return $"Skipped '{zone.name}': it needs a parent station object and a BoxCollider.";

        // 1. the root goes to scale 1 (the model is made of children). Keep the zone the same size and place in the world.
        Vector3 zoneCenter = zone.transform.TransformPoint(box.center);
        Vector3 lossy = zone.transform.lossyScale;
        Vector3 zoneSize = Vector3.Scale(box.size, new Vector3(Mathf.Abs(lossy.x), Mathf.Abs(lossy.y), Mathf.Abs(lossy.z)));
        if (root.localScale != Vector3.one)
        {
            Undo.RecordObject(root, "Rebuild sell stations");
            root.localScale = Vector3.one;
        }
        Undo.RecordObject(zone.transform, "Rebuild sell stations");
        zone.transform.localScale = Vector3.one;
        zone.transform.position = zoneCenter;
        Undo.RecordObject(box, "Rebuild sell stations");
        box.isTrigger = true;
        box.center = Vector3.zero;
        box.size = zoneSize;

        // 2. remove the old plain cube (its look and its solid box) from the root
        var oldRenderer = root.GetComponent<MeshRenderer>();
        if (oldRenderer != null) Undo.DestroyObjectImmediate(oldRenderer);
        var oldFilter = root.GetComponent<MeshFilter>();
        if (oldFilter != null) Undo.DestroyObjectImmediate(oldFilter);
        foreach (var c in root.GetComponents<Collider>()) Undo.DestroyObjectImmediate(c);

        // 3. delete the model from the last run
        var oldModel = root.Find(ModelName);
        if (oldModel != null) Undo.DestroyObjectImmediate(oldModel.gameObject);

        // 4. ground height: the highest point under the zone, so the pad never sinks into a bump
        Vector3 origin = terrain.transform.position;
        float ground = float.MinValue;
        for (int i = 0; i <= 4; i++)
            for (int k = 0; k <= 4; k++)
            {
                var p = zoneCenter + new Vector3((i / 4f - 0.5f) * zoneSize.x, 0, (k / 4f - 0.5f) * zoneSize.z);
                ground = Mathf.Max(ground, terrain.SampleHeight(p) + origin.y);
            }
        float floor = ground + 0.05f; // top of the concrete pad

        // 5. which way the loading bay faces: the chosen side, or the side with the nearest road
        Vector3 center = new(root.position.x, floor, root.position.z);
        Vector3 front = chosenFront;
        if (front == Vector3.zero)
        {
            front = Vector3.back;
            float best = float.MaxValue;
            foreach (var dir in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
            {
                float g = RoadGap(dir, center, roads);
                if (g < best) { best = g; front = dir; }
            }
        }

        // the model is built in its own frame: +Z is the front (the loading bay), X is across.
        var model = new GameObject(ModelName).transform;
        Undo.RegisterCreatedObjectUndo(model.gameObject, "Rebuild sell stations");
        model.SetParent(root, false);
        model.SetPositionAndRotation(center, Quaternion.LookRotation(front));

        float W = Mathf.Abs(front.z) > 0.5f ? Footprint.x : Footprint.y; // across the front
        float D = Mathf.Abs(front.z) > 0.5f ? Footprint.y : Footprint.x; // front to back
        float zoneDepth = Mathf.Abs(front.z) > 0.5f ? zoneSize.z : zoneSize.x;

        // silos go on the side away from the nearer side road, so the open side of the bay faces that road
        Vector3 across = model.rotation * Vector3.right;
        float s = RoadGap(across, center, roads) < RoadGap(-across, center, roads) ? -1f : 1f;
        Vector3 P(float x, float y, float z) => new(s * x, y, z);

        var (accentColor, trimColor) = Colors(zone.crop);
        string crop = zone.crop.ToString();
        var accent = GetMaterial(crop + "Accent", accentColor, 0.15f);
        var trim = GetMaterial(crop + "Trim", trimColor, 0.2f);
        var concrete = GetMaterial("Concrete", new Color(0.74f, 0.74f, 0.72f), 0.05f);
        var metal = GetMaterial("SiloMetal", new Color(0.8f, 0.82f, 0.84f), 0.55f);
        var steel = GetMaterial("Steel", new Color(0.36f, 0.38f, 0.41f), 0.35f);
        var dark = GetMaterial("DarkSteel", new Color(0.2f, 0.21f, 0.23f), 0.3f);
        var glass = GetMaterial("Glass", new Color(0.14f, 0.22f, 0.3f), 0.85f);
        var white = GetMaterial("SignWhite", new Color(0.95f, 0.95f, 0.95f), 0.2f);
        var yellow = GetMaterial("Yellow", new Color(0.95f, 0.78f, 0.1f), 0.2f);

        // ---- concrete pad: the whole zone, so the player can see where to drive ----
        var pad = Box(model, "Pad", Vector3.zero, Vector3.one, concrete);
        pad.transform.SetPositionAndRotation(new Vector3(zoneCenter.x, floor - 0.5f, zoneCenter.z), Quaternion.identity);
        pad.transform.localScale = new Vector3(zoneSize.x, 1f, zoneSize.z);

        // ---- layout ----
        const float bay = 11f;       // depth of the covered loading bay at the front
        const float shedH = 13f;     // shed wall height
        const float canopyY = 10f;   // underside of the bay roof, high enough for the harvester and its camera
        const float siloH = 20f;     // silo body height (on a 1.2 m base)
        const float towerH = 27f;    // elevator tower height
        float siloRow = D - bay - 0.6f;
        int siloCount = Mathf.Max(1, Mathf.RoundToInt(siloRow / 8f));
        float pitch = siloRow / siloCount;
        float siloD = Mathf.Min(8f, pitch - 0.6f);
        float band = siloD + 1f;                 // width of the silo strip on one side
        float siloX = W / 2f - band / 2f;
        float shedL = -W / 2f, shedR = W / 2f - band - 0.6f;
        float shedW = shedR - shedL, shedX = (shedL + shedR) / 2f;
        float shedFront = D / 2f - bay, shedBack = -D / 2f;
        float shedD = shedFront - shedBack, shedZ = (shedFront + shedBack) / 2f;
        float bayZ = shedFront + bay / 2f;
        float towerFront = D / 2f - 5f, towerBack = shedFront;
        float towerZ = (towerFront + towerBack) / 2f, towerD = towerFront - towerBack;

        // ---- shed (warehouse) with a pitched roof ----
        Box(model, "Shed", P(shedX, shedH / 2f, shedZ), new Vector3(shedW, shedH, shedD), accent);
        Box(model, "Shed Base", P(shedX, 0.4f, shedZ), new Vector3(shedW + 0.2f, 0.8f, shedD + 0.2f), concrete, collider: false);
        Box(model, "Shed Eave", P(shedX, shedH - 0.3f, shedZ), new Vector3(shedW + 0.3f, 0.6f, shedD + 0.3f), trim, collider: false);
        foreach (float cx in new[] { shedL, shedR })
            foreach (float cz in new[] { shedFront, shedBack })
                Box(model, "Corner Post", P(cx, shedH / 2f, cz), new Vector3(0.5f, shedH, 0.5f), trim, collider: false);

        // a cube turned 45 degrees inside a flattened parent makes a low gable roof
        const float roofPitch = 0.35f;
        var roof = new GameObject("Roof").transform;
        roof.SetParent(model, false);
        roof.localPosition = P(shedX, shedH, shedZ);
        roof.localScale = new Vector3(1f, roofPitch, 1f);
        float side = (shedW + 1f) / Mathf.Sqrt(2f);
        Box(roof, "Roof Panel", Vector3.zero, new Vector3(side, side, shedD + 1f), trim, collider: false, rotation: Quaternion.Euler(0, 0, 45));
        Box(model, "Ridge", P(shedX, shedH + (shedW + 1f) / 2f * roofPitch, shedZ), new Vector3(0.5f, 0.3f, shedD + 1.1f), dark, collider: false);

        // big roller door into the bay, with a few lines across it
        float doorW = Mathf.Min(9f, shedW * 0.5f);
        Box(model, "Roller Door", P(shedX, 3.75f, shedFront + 0.1f), new Vector3(doorW, 7.5f, 0.2f), steel, collider: false);
        for (int i = 1; i <= 5; i++)
            Box(model, "Door Line", P(shedX, i * 1.25f, shedFront + 0.22f), new Vector3(doorW, 0.08f, 0.06f), dark, collider: false);
        Box(model, "Door", P(shedR - 2.2f, 1.2f, shedFront + 0.1f), new Vector3(1.2f, 2.4f, 0.2f), dark, collider: false);

        // windows and a side door on the open side of the shed
        int windows = Mathf.Max(2, Mathf.FloorToInt(shedD / 5f));
        for (int i = 0; i < windows; i++)
        {
            float z = shedBack + shedD * (i + 0.5f) / windows;
            Box(model, "Window", P(shedL - 0.06f, 9f, z), new Vector3(0.2f, 1.4f, 2.6f), glass, collider: false);
        }
        Box(model, "Side Door", P(shedL - 0.1f, 3f, shedBack + shedD * 0.35f), new Vector3(0.2f, 6f, 6f), steel, collider: false);

        // ---- covered loading bay in front of the shed ----
        Box(model, "Bay Roof", P(shedX, canopyY + 0.25f, bayZ + 0.25f), new Vector3(shedW + 0.6f, 0.5f, bay + 0.5f), trim, collider: false);
        Box(model, "Bay Roof Edge", P(shedL - 0.35f, canopyY + 0.25f, bayZ + 0.25f), new Vector3(0.3f, 1f, bay + 0.5f), trim, collider: false);
        foreach (float px in new[] { shedL + 0.5f, shedR - 0.5f })
        {
            Box(model, "Bay Pillar", P(px, canopyY / 2f, D / 2f - 0.2f), new Vector3(0.6f, canopyY, 0.6f), steel);
            Box(model, "Pillar Guard", P(px, 0.6f, D / 2f - 0.2f), new Vector3(0.9f, 1.2f, 0.9f), yellow, collider: false);
        }

        // the main sign along the front edge of the bay roof
        float signZ = D / 2f + 0.65f;
        Box(model, "Sign Board", P(shedX, canopyY + 0.5f, signZ), new Vector3(shedW + 0.6f, 2.6f, 0.3f), white, collider: false);
        Box(model, "Sign Trim Top", P(shedX, canopyY + 1.85f, signZ), new Vector3(shedW + 0.8f, 0.25f, 0.4f), trim, collider: false);
        Box(model, "Sign Trim Bottom", P(shedX, canopyY - 0.85f, signZ), new Vector3(shedW + 0.8f, 0.25f, 0.4f), trim, collider: false);
        Label(model, "Sign Text", $"{crop.ToUpper()} SELLING STATION", P(shedX, canopyY + 0.5f, signZ + 0.17f), shedW * 0.92f, 2.2f, trimColor);

        // weighbridge plate under the roof, and yellow lane lines from the bay out to the edge of the pad
        float plateL = bay - 1.5f;
        Box(model, "Weighbridge", P(shedX, 0.03f, bayZ), new Vector3(5.5f, 0.06f, plateL), steel, collider: false);
        foreach (float ex in new[] { -2.9f, 2.9f })
            Box(model, "Weighbridge Edge", P(shedX + ex, 0.035f, bayZ), new Vector3(0.3f, 0.07f, plateL), yellow, collider: false);
        float padFront = zoneDepth / 2f;
        float lineL = padFront - shedFront;
        foreach (float lx in new[] { shedL + 1.5f, shedR - 1.5f })
            Box(model, "Lane Line", P(lx, 0.01f, shedFront + lineL / 2f), new Vector3(0.25f, 0.02f, lineL), yellow, collider: false);

        // ---- elevator tower with the crop name near the top ----
        Box(model, "Elevator Tower", P(siloX, towerH / 2f, towerZ), new Vector3(band, towerH, towerD), accent);
        Box(model, "Tower Cap", P(siloX, towerH + 0.3f, towerZ), new Vector3(band + 0.5f, 0.6f, towerD + 0.5f), trim, collider: false);
        Box(model, "Tower Top House", P(siloX, towerH + 1.6f, towerZ), new Vector3(band * 0.55f, 2f, towerD * 0.6f), accent, collider: false);
        foreach (float wy in new[] { 8f, 13f, 18f })
            Box(model, "Tower Window", P(siloX, wy, towerFront + 0.06f), new Vector3(1.4f, 2f, 0.2f), glass, collider: false);
        float towerSignY = towerH - 3.2f;
        Box(model, "Tower Sign", P(siloX, towerSignY, towerFront + 0.12f), new Vector3(band - 0.8f, 2.6f, 0.25f), white, collider: false);
        Label(model, "Tower Text", crop.ToUpper(), P(siloX, towerSignY, towerFront + 0.27f), band - 1.4f, 2.2f, trimColor);

        // ---- small weigh office in front of the tower ----
        float officeZ = D / 2f - 2.25f;
        Box(model, "Office", P(siloX, 2f, officeZ), new Vector3(band, 4f, 4.5f), accent);
        Box(model, "Office Roof", P(siloX, 4.15f, officeZ), new Vector3(band + 0.4f, 0.3f, 5f), trim, collider: false);
        Box(model, "Office Window", P(siloX + band * 0.15f, 2.3f, D / 2f + 0.06f), new Vector3(band * 0.45f, 1.4f, 0.2f), glass, collider: false);
        Box(model, "Office Door", P(siloX - band * 0.3f, 1.2f, D / 2f + 0.06f), new Vector3(1.2f, 2.4f, 0.2f), glass, collider: false);
        Box(model, "Weigh Window", P(siloX - band / 2f - 0.06f, 2.3f, officeZ), new Vector3(0.2f, 1.2f, 2.5f), glass, collider: false);

        // ---- silos in a row behind the tower, with spouts from the tower top ----
        var cone = GetConeMesh();
        for (int i = 0; i < siloCount; i++)
        {
            float z = towerBack - 0.3f - pitch * (i + 0.5f);
            string n = "Silo " + (i + 1);
            Cylinder(model, n + " Base", P(siloX, 0.6f, z), siloD + 0.8f, 1.2f, concrete, collider: true);
            Cylinder(model, n, P(siloX, 1.2f + siloH / 2f, z), siloD, siloH, metal, collider: true);
            for (int k = 1; k <= 3; k++)
                Cylinder(model, n + " Band", P(siloX, 1.2f + siloH * k / 4f, z), siloD + 0.15f, 0.35f, accent, collider: false);
            Cylinder(model, n + " Rim", P(siloX, 1.2f + siloH - 0.2f, z), siloD + 0.2f, 0.4f, accent, collider: false);
            float coneH = siloD * 0.35f;
            Cone(model, n + " Roof", P(siloX, 1.2f + siloH, z), siloD + 0.3f, coneH, cone, trim);
            Pipe(model, n + " Spout", P(siloX, towerH - 2f, towerBack), P(siloX, 1.2f + siloH + coneH, z), 0.5f, steel);
        }

        // everything in the model is still, so it can be batched and lit as static
        foreach (var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = true;

        return $"{root.name}: bay faces {SideName(front)}, {siloCount} silos, zone {zoneSize.x:0.#} x {zoneSize.z:0.#} m kept.";
    }

    static string SideName(Vector3 dir)
    {
        for (int i = 1; i < SideDirs.Length; i++)
            if (Vector3.Dot(dir, SideDirs[i]) > 0.9f) return SideNames[i];
        return dir.ToString();
    }

    // distance from the old footprint's side (in direction dir) to the nearest road in front of that side
    static float RoadGap(Vector3 dir, Vector3 c, List<Bounds> roads)
    {
        float hx = Footprint.x / 2f, hz = Footprint.y / 2f;
        float best = float.MaxValue;
        foreach (var r in roads)
        {
            bool facing;
            float gap;
            if (Mathf.Abs(dir.x) > 0.5f)
            {
                facing = r.max.z > c.z - hz && r.min.z < c.z + hz;
                gap = dir.x > 0 ? r.min.x - (c.x + hx) : (c.x - hx) - r.max.x;
            }
            else
            {
                facing = r.max.x > c.x - hx && r.min.x < c.x + hx;
                gap = dir.z > 0 ? r.min.z - (c.z + hz) : (c.z - hz) - r.max.z;
            }
            if (facing && gap > -1f) best = Mathf.Min(best, gap);
        }
        return best;
    }

    // ---- building blocks (positions are local to the parent) ----

    static GameObject Box(Transform parent, string name, Vector3 position, Vector3 scale, Material mat, bool collider = true, Quaternion? rotation = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        if (!collider) DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = rotation ?? Quaternion.identity;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    // a standing cylinder: position is its centre. A solid one gets an exact mesh collider (the default capsule has round ends).
    static GameObject Cylinder(Transform parent, string name, Vector3 position, float diameter, float height, Material mat, bool collider)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        DestroyImmediate(go.GetComponent<Collider>());
        if (collider) go.AddComponent<MeshCollider>();
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = new Vector3(diameter, height / 2f, diameter);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    // a thin cylinder from point a to point b (no collider)
    static void Pipe(Transform parent, string name, Vector3 a, Vector3 b, float diameter, Material mat)
    {
        var go = Cylinder(parent, name, (a + b) / 2f, diameter, (b - a).magnitude, mat, collider: false);
        go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
    }

    // a cone roof: basePosition is the centre of its bottom (no collider)
    static void Cone(Transform parent, string name, Vector3 basePosition, float diameter, float height, Mesh mesh, Material mat)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = basePosition;
        go.transform.localScale = new Vector3(diameter, height, diameter);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    // sign text that faces the front of the model (+Z)
    static void Label(Transform parent, string name, string text, Vector3 position, float width, float height, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 1f;
        tmp.fontSizeMax = 80f;
        tmp.rectTransform.sizeDelta = new Vector2(width, height);
        // TextMeshPro reads from its -Z side; turned 180 degrees it faces +Z, the front
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.Euler(0, 180, 0);
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(AssetFolder)) AssetDatabase.CreateFolder("Assets", "SellStations");
    }

    static Material GetMaterial(string name, Color color, float gloss)
    {
        string path = $"{AssetFolder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        EnsureFolder();
        mat = new Material(Shader.Find("Standard")) { color = color };
        mat.SetFloat("_Glossiness", gloss);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // a cone 1 m wide and 1 m tall, its base at y = 0, saved as an asset so the scene can use it
    static Mesh GetConeMesh()
    {
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(ConePath);
        if (mesh != null) return mesh;

        const int segments = 32;
        const float r = 0.5f, h = 1f;
        var verts = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        Vector3 apex = new(0, h, 0);
        Vector3 SideNormal(float a) => new Vector3(Mathf.Cos(a) * h, r, Mathf.Sin(a) * h).normalized;
        for (int i = 0; i < segments; i++)
        {
            float a0 = i * Mathf.PI * 2f / segments, a1 = (i + 1) * Mathf.PI * 2f / segments, am = (a0 + a1) / 2f;
            Vector3 b0 = new(Mathf.Cos(a0) * r, 0, Mathf.Sin(a0) * r);
            Vector3 b1 = new(Mathf.Cos(a1) * r, 0, Mathf.Sin(a1) * r);
            int v = verts.Count;
            verts.Add(apex); normals.Add(SideNormal(am)); uvs.Add(new Vector2((float)i / segments, 1));
            verts.Add(b0); normals.Add(SideNormal(a0)); uvs.Add(new Vector2((float)i / segments, 0));
            verts.Add(b1); normals.Add(SideNormal(a1)); uvs.Add(new Vector2((float)(i + 1) / segments, 0));
            // Unity shows the side a triangle's Cross(b - a, c - a) points to: make that the outside
            Vector3 outward = new(Mathf.Cos(am), 0, Mathf.Sin(am));
            if (Vector3.Dot(Vector3.Cross(b0 - apex, b1 - apex), outward) > 0) tris.AddRange(new[] { v, v + 1, v + 2 });
            else tris.AddRange(new[] { v, v + 2, v + 1 });
        }

        mesh = new Mesh { name = "SiloCone" };
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        EnsureFolder();
        AssetDatabase.CreateAsset(mesh, ConePath);
        return mesh;
    }
}
