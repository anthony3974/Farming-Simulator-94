using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor tool: builds Jerrys Gas Station (diesel) at the bottom of the map, beside the main road.
// Use: open GameScene, Tools > Gas Station Builder, press "Build / rebuild gas station", save the scene (Ctrl+S).
//
// Layout like the shops: "Gas Station" (under Terrain > Buildings) holds a "Model" (all the looks) and a
// "Triggerbox" (a trigger BoxCollider and the Building, type Gas, over the whole pump area). Running the tool
// again keeps the "Gas Station" object, its place and its Triggerbox (with any Building values set on it),
// and only rebuilds the Model, the trigger size and the Building type. Materials are saved in Assets/GasStation.
//
// The parts are listed in GasStationModel (below), one line per part. Like Tractor V2, every part reaches a
// little into the part it stands on or hangs on, so nothing floats. Only the solid things you could drive into
// keep a collider: the kiosk, the canopy posts, the pump bodies and the sign posts (plus the flat forecourt slab,
// whose top is only 5 cm above the grass). Placeholder boxes; a real model can replace them later.
public class GasStationBuilder : EditorWindow
{
    const string SceneName = "GameScene";
    const string RootName = "Gas Station";
    const string ModelName = "Model";
    const string TriggerName = "Triggerbox";
    const string MaterialFolder = "Assets/GasStation";

    // First build only: the lot centre (world X, Z). Free grass at the bottom of the map (low X), between the
    // main road (its edge is at Z 631.5) and Random Field 1 (Z below 549), clear of the map border (X 1.5).
    // Later runs keep wherever the "Gas Station" object is.
    static readonly Vector2 FirstCentre = new(31f, 596f);

    [MenuItem("Tools/Gas Station Builder")]
    static void Open() { GetWindow<GasStationBuilder>("Gas Station"); }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("Builds Jerrys Gas Station: a forecourt off the main road, a canopy over two islands with four diesel " +
            "pumps, a kiosk and a tall price sign. Run it with GameScene open, then save the scene (Ctrl+S).", MessageType.Info);
        EditorGUILayout.HelpBox("Running it again keeps the 'Gas Station' object, its place and its Triggerbox (Building values), " +
            "and only rebuilds the look.", MessageType.None);
        if (GUILayout.Button("Build / rebuild gas station")) Build();
    }

    static void Build()
    {
        const string title = "Gas Station";
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
        if (!FindReferences(out var input, out var values))
        {
            EditorUtility.DisplayDialog(title, "Could not find the InputManager and PlayerValues (looked on the other buildings and in the scene).", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Build gas station");
        int undoGroup = Undo.GetCurrentGroup();

        // the station object: kept if it is there, made on the first run
        var buildings = GameObject.Find("Buildings");
        if (buildings == null) Debug.LogWarning("No 'Buildings' object found, the gas station goes at the top of the scene.");
        Transform root = buildings != null ? buildings.transform.Find(RootName) : null;
        if (root == null)
        {
            var found = GameObject.Find(RootName);
            if (found != null) root = found.transform;
        }
        Vector2 centre = FirstCentre;
        if (root == null)
        {
            root = new GameObject(RootName).transform;
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Build gas station");
            if (buildings != null) root.SetParent(buildings.transform, false);
        }
        else
        {
            centre = new Vector2(root.position.x, root.position.z);
            Undo.RecordObject(root, "Build gas station");
        }

        // the lot runs from its back edge forward to the edge of the main road
        float lotFront = GasStationModel.DefaultLotFront;
        float? road = RoadEdgeInFront(new Vector3(centre.x, 0f, centre.y + 20f), 30f);
        if (road.HasValue) lotFront = road.Value - 0.05f - centre.y;
        else Debug.LogWarning($"No road found in front of the gas station, the lot is {lotFront - GasStationModel.LotBack:0} m deep.");

        var lot = GasStationModel.LotRect(lotFront);
        var lotCentre = new Vector3(centre.x + lot.center.x, 0f, centre.y + lot.center.y);
        GroundRange(terrain, lotCentre, lot.width, lot.height, out float low, out float high);
        float floor = high + 0.05f; // top of the forecourt: just above the highest ground under it
        if (high - low > 0.1f)
            Debug.LogWarning($"The ground under the gas station is not flat ({high - low:0.00} m from low to high), so the slab edge stands up to " +
                $"{high - low + 0.05f:0.00} m above the grass in places.");
        float pylonGround = terrain.SampleHeight(new Vector3(centre.x + GasStationModel.PylonPos.x, 0f, centre.y + GasStationModel.PylonPos.y))
                            + terrain.transform.position.y - floor;

        root.SetPositionAndRotation(new Vector3(centre.x, floor, centre.y), Quaternion.identity);
        root.localScale = Vector3.one;

        // the look: thrown away and rebuilt on every run
        var oldModel = root.Find(ModelName);
        if (oldModel != null) Undo.DestroyObjectImmediate(oldModel.gameObject);
        var model = new GameObject(ModelName).transform;
        Undo.RegisterCreatedObjectUndo(model.gameObject, "Build gas station");
        model.SetParent(root, false);
        model.SetSiblingIndex(0);
        GasStationModel.Build(new SceneKit(model, new Mats()), lotFront, pylonGround);
        foreach (var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = true;

        SetUpTrigger(root, input, values);
        WarnIfCrowded(root, terrain, lot);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        Selection.activeGameObject = root.gameObject;
        Debug.Log($"Gas station built at X {centre.x:0}, Z {centre.y:0} (lot X {centre.x + lot.xMin:0} to {centre.x + lot.xMax:0}, " +
                  $"Z {centre.y + lot.yMin:0} to {centre.y + lot.yMax:0}). Save the scene (Ctrl+S).", root);
    }

    // The trigger over the pump area, the same as the shops: a child "Triggerbox" with a trigger BoxCollider and the
    // Building. A vehicle driving into it makes it the current zone, then E / USE runs the Gas action.
    static void SetUpTrigger(Transform root, InputManager input, PlayerValues values)
    {
        var zone = root.Find(TriggerName);
        if (zone == null)
        {
            zone = new GameObject(TriggerName).transform;
            Undo.RegisterCreatedObjectUndo(zone.gameObject, "Build gas station");
            zone.SetParent(root, false);
        }
        Undo.RecordObject(zone, "Build gas station");
        zone.localPosition = GasStationModel.TriggerCentre;
        zone.localRotation = Quaternion.identity;
        zone.localScale = Vector3.one;

        var box = zone.GetComponent<BoxCollider>();
        if (box == null) box = Undo.AddComponent<BoxCollider>(zone.gameObject);
        Undo.RecordObject(box, "Build gas station");
        box.isTrigger = true;
        box.center = Vector3.zero;
        box.size = GasStationModel.TriggerSize;

        var building = zone.GetComponent<Building>();
        if (building == null) building = Undo.AddComponent<Building>(zone.gameObject);
        Undo.RecordObject(building, "Build gas station");
        building.type = Building.BuildingType.Gas;
        if (building.inputManager == null) building.inputManager = input;
        if (building.playerValues == null) building.playerValues = values;
        EditorUtility.SetDirty(building);
    }

    // the same InputManager and PlayerValues the other buildings (seed shop, fertilizer shop) use
    static bool FindReferences(out InputManager input, out PlayerValues values)
    {
        input = null; values = null;
        foreach (var b in FindObjectsByType<Building>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (b.type == Building.BuildingType.Gas || b.inputManager == null || b.playerValues == null) continue;
            input = b.inputManager; values = b.playerValues;
            break;
        }
        if (input == null) input = FindFirstObjectByType<InputManager>();
        if (values == null) values = FindFirstObjectByType<PlayerValues>();
        return input != null && values != null;
    }

    // a warning in the Console if the lot runs into a field, a building or the map border
    static void WarnIfCrowded(Transform self, Terrain terrain, Rect lot)
    {
        var mine = new Bounds(self.position + new Vector3(lot.center.x, 0f, lot.center.y), new Vector3(lot.width, 20f, lot.height));
        var problems = new System.Collections.Generic.List<string>();
        foreach (string group in new[] { "Fields", "Buildings", "Map Border" })
        {
            var g = GameObject.Find(group);
            if (g == null) continue;
            foreach (var r in g.GetComponentsInChildren<Renderer>())
            {
                if (r.transform.IsChildOf(self) || r is ParticleSystemRenderer) continue;
                var b = r.bounds;
                if (b.max.x > mine.min.x && b.min.x < mine.max.x && b.max.z > mine.min.z && b.min.z < mine.max.z) problems.Add(r.name);
            }
        }
        Vector3 o = terrain.transform.position, s = terrain.terrainData.size;
        if (mine.min.x < o.x + 2f || mine.max.x > o.x + s.x - 2f || mine.min.z < o.z + 2f || mine.max.z > o.z + s.z - 2f) problems.Add("the map edge");
        if (problems.Count > 0)
            Debug.LogWarning($"The gas station lot overlaps: {string.Join(", ", problems)}. Move the 'Gas Station' object and run the tool again.", self);
    }

    // the lowest edge of a road in front (+Z) of a point, if one starts within maxDistance (as the shop builder did)
    static float? RoadEdgeInFront(Vector3 point, float maxDistance)
    {
        var roads = GameObject.Find("Roads and paths");
        if (roads == null) return null;
        float? best = null;
        foreach (var r in roads.GetComponentsInChildren<Renderer>())
        {
            var b = r.bounds;
            if (point.x < b.min.x || point.x > b.max.x) continue;
            float gap = b.min.z - point.z;
            if (gap < -0.01f || gap > maxDistance) continue;
            if (best == null || b.min.z < best.Value) best = b.min.z;
        }
        return best;
    }

    // the lowest and highest ground under a rectangle
    static void GroundRange(Terrain terrain, Vector3 centre, float sizeX, float sizeZ, out float low, out float high)
    {
        low = float.MaxValue; high = float.MinValue;
        for (int i = 0; i <= 8; i++)
            for (int k = 0; k <= 8; k++)
            {
                var p = centre + new Vector3((i / 8f - 0.5f) * sizeX, 0f, (k / 8f - 0.5f) * sizeZ);
                float h = terrain.SampleHeight(p) + terrain.transform.position.y;
                low = Mathf.Min(low, h); high = Mathf.Max(high, h);
            }
    }

    // materials are saved as assets in Assets/GasStation, so the scene keeps them; an existing one is reused as it is
    class Mats
    {
        readonly System.Collections.Generic.Dictionary<GasMat, Material> cache = new();

        public Material Get(GasMat key)
        {
            if (cache.TryGetValue(key, out var mat)) return mat;
            var (name, color, gloss) = GasStationModel.MaterialInfo(key);
            string path = $"{MaterialFolder}/{name}.mat";
            mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets", "GasStation");
                mat = new Material(Shader.Find("Standard")) { color = color };
                mat.SetFloat("_Glossiness", gloss);
                AssetDatabase.CreateAsset(mat, path);
            }
            cache[key] = mat;
            return mat;
        }
    }

    // makes the parts as Unity primitives under a Transform
    class SceneKit : GasKit
    {
        readonly Transform parent;
        readonly Mats mats;
        public SceneKit(Transform parent, Mats mats) { this.parent = parent; this.mats = mats; }

        public override GasKit Group(string name, Vector3 pos, float yaw = 0f)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return new SceneKit(t, mats);
        }

        public override void Box(string name, Vector3 pos, Vector3 size, GasMat mat, bool collider = false, Vector3 euler = default)
            => Prim(PrimitiveType.Cube, name, pos, size, mat, collider, euler);

        // Unity's cylinder is 2 units tall, so the Y scale is half the height
        public override void Cyl(string name, Vector3 pos, float diameter, float height, GasMat mat, Vector3 euler = default)
            => Prim(PrimitiveType.Cylinder, name, pos, new Vector3(diameter, height / 2f, diameter), mat, false, euler);

        public override void Ball(string name, Vector3 pos, float diameter, GasMat mat)
            => Prim(PrimitiveType.Sphere, name, pos, Vector3.one * diameter, mat, false, default);

        // TextMeshPro text is read from its -Z side, so yaw 180 makes it face +Z
        public override void Label(string name, string text, Vector3 pos, Vector2 size, GasMat color, float yaw)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.color = GasStationModel.MaterialInfo(color).color;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 0.5f;
            tmp.fontSizeMax = 60f;
            tmp.rectTransform.sizeDelta = size;
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        // all positions are local to the parent; only the parts asked for keep their (box) collider
        void Prim(PrimitiveType type, string name, Vector3 pos, Vector3 scale, GasMat mat, bool collider, Vector3 euler)
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

// the materials of the gas station (saved as Assets/GasStation/<name>.mat)
public enum GasMat { Asphalt, Concrete, ConcreteDark, White, Cream, Orange, Navy, Yellow, Steel, Black, Glass, Light, Red, Green }

// Where the parts go. GasStationModel only talks to this, so the part list can also be checked outside Unity.
public abstract class GasKit
{
    public abstract GasKit Group(string name, Vector3 pos, float yaw = 0f);
    public abstract void Box(string name, Vector3 pos, Vector3 size, GasMat mat, bool collider = false, Vector3 euler = default);
    public abstract void Cyl(string name, Vector3 pos, float diameter, float height, GasMat mat, Vector3 euler = default);
    public abstract void Ball(string name, Vector3 pos, float diameter, GasMat mat);
    public abstract void Label(string name, string text, Vector3 pos, Vector2 size, GasMat color, float yaw);
}

// The parts of Jerrys Gas Station. Space: the origin is the lot centre on top of the forecourt slab (y = 0),
// +Z points to the main road, +X along the road away from the map border. Comments give each part's span,
// to show which part it overlaps.
//
// Plan (local metres): forecourt X -25..+25, Z -28 (back) .. lotFront (the road edge, about +35.4).
// Canopy X -14..+14, Z +8..+24, underside 6.2 m up (the harvester is 5.1 m tall). Two pump islands at X -6
// and +6 run along Z, so you drive straight in off the road: lanes are 10.4 m (middle) and 7.2 m (outer,
// open at the side) wide, room for the harvester's 6.8 m wide controller. Kiosk at the back left
// (X -23..-7, Z -26..-16) with three parking bays in front; the open forecourt between it and the canopy
// (and 11 m on each side of the canopy) is room to turn round.
// The price sign stands on the grass by the road, at the far corner from the map border.
public static class GasStationModel
{
    public const float LotHalfWidth = 25f, LotBack = -28f;
    public const float DefaultLotFront = 35.44f;            // road edge at world Z 631.49 when the lot centre is at Z 596
    public static readonly Vector2 PylonPos = new(28.5f, 29f); // on the grass, 3.5 m past the lot's +X edge

    // the Building trigger: the canopy and 3 m around it, 8 m high (like the shops' Triggerbox)
    public static readonly Vector3 TriggerCentre = new(0f, 4f, 16f);
    public static readonly Vector3 TriggerSize = new(34f, 8f, 22f);

    const float CanopyZ = 16f, CanopyHalfW = 14f, CanopyHalfD = 8f, CanopyLow = 6.2f, CanopyHigh = 7.1f;
    const float IslandH = 0.25f;
    static readonly float[] IslandX = { -6f, 6f };
    static readonly Vector3 KioskPos = new(-15f, 0f, -21f);

    public static Rect LotRect(float lotFront) => new(-LotHalfWidth, LotBack, 2f * LotHalfWidth, lotFront - LotBack);

    public static (string name, Color color, float gloss) MaterialInfo(GasMat m) => m switch
    {
        GasMat.Asphalt => ("GasAsphalt", new Color(0.3f, 0.3f, 0.32f), 0.05f),
        GasMat.Concrete => ("GasConcrete", new Color(0.74f, 0.74f, 0.72f), 0.05f),
        GasMat.ConcreteDark => ("GasConcreteDark", new Color(0.5f, 0.5f, 0.5f), 0.05f),
        GasMat.White => ("GasWhite", new Color(0.95f, 0.95f, 0.95f), 0.2f),
        GasMat.Cream => ("GasCream", new Color(0.93f, 0.89f, 0.8f), 0.1f),
        GasMat.Orange => ("GasOrange", new Color(0.96f, 0.45f, 0.08f), 0.3f),
        GasMat.Navy => ("GasNavy", new Color(0.1f, 0.16f, 0.36f), 0.3f),
        GasMat.Yellow => ("GasYellow", new Color(0.97f, 0.8f, 0.12f), 0.2f),
        GasMat.Steel => ("GasSteel", new Color(0.55f, 0.57f, 0.6f), 0.5f),
        GasMat.Black => ("GasBlack", new Color(0.07f, 0.07f, 0.07f), 0.3f),
        GasMat.Glass => ("GasGlass", new Color(0.14f, 0.22f, 0.3f), 0.85f),
        GasMat.Light => ("GasLight", new Color(1f, 0.97f, 0.85f), 0.2f),
        GasMat.Red => ("GasRed", new Color(0.75f, 0.12f, 0.1f), 0.3f),
        _ => ("GasGreen", new Color(0.3f, 0.95f, 0.4f), 0.2f),
    };

    // lotFront: local Z of the lot's road edge. pylonGround: the ground height under the sign, local (about -0.05).
    public static void Build(GasKit model, float lotFront, float pylonGround)
    {
        BuildForecourt(model.Group("Forecourt", Vector3.zero), lotFront);
        BuildCanopy(model.Group("Canopy", Vector3.zero));
        for (int i = 0; i < IslandX.Length; i++) BuildIsland(model.Group("Pump Island", new Vector3(IslandX[i], 0f, CanopyZ)), i);
        BuildKiosk(model.Group("Kiosk", KioskPos));
        BuildYard(model.Group("Yard", Vector3.zero));
        BuildPylon(model.Group("Price Sign", new Vector3(PylonPos.x, pylonGround, PylonPos.y), 90f));
        Lamp(model, new Vector3(24.4f, 0f, 0f), -90f);
        Lamp(model, new Vector3(-24.4f, 0f, 8f), 90f);
    }

    // ---- forecourt: slab, concrete pad under the canopy, kerbs, markings ----

    static void BuildForecourt(GasKit g, float front)
    {
        float depth = front - LotBack, mid = (front + LotBack) / 2f;
        // asphalt slab with a collider (y -1..0, about 5 cm above the grass), from the back edge to the road edge
        g.Box("Slab", new Vector3(0f, -0.5f, mid), new Vector3(2f * LotHalfWidth, 1f, depth), GasMat.Asphalt, collider: true);
        // light concrete pad under the canopy (y 0..0.02, X -15..15, Z 7..25)
        g.Box("Pump Pad", new Vector3(0f, 0.01f, CanopyZ), new Vector3(30f, 0.02f, 18f), GasMat.Concrete);
        // kerbs along the sides and the back (y 0..0.15, on the slab edge); the road side stays open
        g.Box("Kerb", new Vector3(-LotHalfWidth + 0.15f, 0.075f, mid), new Vector3(0.3f, 0.15f, depth), GasMat.Concrete);
        g.Box("Kerb", new Vector3(LotHalfWidth - 0.15f, 0.075f, mid), new Vector3(0.3f, 0.15f, depth), GasMat.Concrete);
        g.Box("Kerb", new Vector3(0f, 0.075f, LotBack + 0.15f), new Vector3(2f * LotHalfWidth, 0.15f, 0.3f), GasMat.Concrete);
        // white lines along the pad's sides (y 0.02..0.03, on the pad)
        g.Box("Pad Line", new Vector3(-14.9f, 0.025f, CanopyZ), new Vector3(0.15f, 0.01f, 17.6f), GasMat.White);
        g.Box("Pad Line", new Vector3(14.9f, 0.025f, CanopyZ), new Vector3(0.15f, 0.01f, 17.6f), GasMat.White);
        // "drive in" arrows in the three lanes between the road and the canopy, pointing at the pumps (-Z)
        foreach (float x in new[] { -10f, 0f, 10f }) Arrow(g, new Vector3(x, 0f, 28.6f));
    }

    // a white arrow painted on the asphalt (y 0..0.02); tip is the point the arrow points to (toward -Z)
    static void Arrow(GasKit g, Vector3 tip)
    {
        g.Box("Arrow", tip + new Vector3(0f, 0.01f, 1.55f), new Vector3(0.35f, 0.02f, 2.6f), GasMat.White);          // shaft, Z +0.25..+2.85
        g.Box("Arrow", tip + new Vector3(-0.42f, 0.01f, 0.42f), new Vector3(0.35f, 0.02f, 1.3f), GasMat.White, euler: new Vector3(0f, -45f, 0f));
        g.Box("Arrow", tip + new Vector3(0.42f, 0.01f, 0.42f), new Vector3(0.35f, 0.02f, 1.3f), GasMat.White, euler: new Vector3(0f, 45f, 0f));
    }

    // ---- canopy roof (no collider, it is 6.2 m up) ----

    static void BuildCanopy(GasKit g)
    {
        float midY = (CanopyLow + CanopyHigh) / 2f, w = 2f * CanopyHalfW, d = 2f * CanopyHalfD;
        float front = CanopyZ + CanopyHalfD, back = CanopyZ - CanopyHalfD;
        // the roof box, y 6.2..7.1, X -14..14, Z 8..24; the posts on the islands reach 5 cm into its underside
        g.Box("Canopy", new Vector3(0f, midY, CanopyZ), new Vector3(w, CanopyHigh - CanopyLow, d), GasMat.White);
        // orange fascia band on all four sides (y 6.35..7.05), set 2 cm into the roof faces
        g.Box("Fascia", new Vector3(0f, 6.7f, front + 0.03f), new Vector3(w + 0.2f, 0.7f, 0.1f), GasMat.Orange);
        g.Box("Fascia", new Vector3(0f, 6.7f, back - 0.03f), new Vector3(w + 0.2f, 0.7f, 0.1f), GasMat.Orange);
        g.Box("Fascia", new Vector3(-CanopyHalfW - 0.03f, 6.7f, CanopyZ), new Vector3(0.1f, 0.7f, d + 0.2f), GasMat.Orange);
        g.Box("Fascia", new Vector3(CanopyHalfW + 0.03f, 6.7f, CanopyZ), new Vector3(0.1f, 0.7f, d + 0.2f), GasMat.Orange);
        // navy stripe under the fascia (y 6.24..6.36) and a navy cap on top (y 7.06..7.18)
        g.Box("Stripe", new Vector3(0f, 6.3f, front + 0.04f), new Vector3(w + 0.22f, 0.12f, 0.12f), GasMat.Navy);
        g.Box("Stripe", new Vector3(0f, 6.3f, back - 0.04f), new Vector3(w + 0.22f, 0.12f, 0.12f), GasMat.Navy);
        g.Box("Stripe", new Vector3(-CanopyHalfW - 0.04f, 6.3f, CanopyZ), new Vector3(0.12f, 0.12f, d + 0.22f), GasMat.Navy);
        g.Box("Stripe", new Vector3(CanopyHalfW + 0.04f, 6.3f, CanopyZ), new Vector3(0.12f, 0.12f, d + 0.22f), GasMat.Navy);
        // light grey roof sheet on top (y 7.09..7.13) inside a navy rim along the top edges (y 7.06..7.18)
        g.Box("Roof Sheet", new Vector3(0f, CanopyHigh + 0.01f, CanopyZ), new Vector3(w - 0.1f, 0.04f, d - 0.1f), GasMat.Steel);
        g.Box("Rim", new Vector3(0f, CanopyHigh + 0.02f, front + 0.05f), new Vector3(w + 0.4f, 0.12f, 0.3f), GasMat.Navy);
        g.Box("Rim", new Vector3(0f, CanopyHigh + 0.02f, back - 0.05f), new Vector3(w + 0.4f, 0.12f, 0.3f), GasMat.Navy);
        g.Box("Rim", new Vector3(-CanopyHalfW - 0.05f, CanopyHigh + 0.02f, CanopyZ), new Vector3(0.3f, 0.12f, d + 0.4f), GasMat.Navy);
        g.Box("Rim", new Vector3(CanopyHalfW + 0.05f, CanopyHigh + 0.02f, CanopyZ), new Vector3(0.3f, 0.12f, d + 0.4f), GasMat.Navy);
        // names on the fascia: the road side, the far side (toward the kiosk) and DIESEL on both ends
        g.Label("Name Front", "JERRYS GAS STATION", new Vector3(0f, 6.7f, front + 0.11f), new Vector2(24f, 0.6f), GasMat.White, 180f);
        g.Label("Name Back", "JERRYS GAS STATION", new Vector3(0f, 6.7f, back - 0.11f), new Vector2(24f, 0.6f), GasMat.White, 0f);
        g.Label("Diesel East", "DIESEL", new Vector3(CanopyHalfW + 0.11f, 6.7f, CanopyZ), new Vector2(12f, 0.6f), GasMat.White, -90f);
        g.Label("Diesel West", "DIESEL", new Vector3(-CanopyHalfW - 0.11f, 6.7f, CanopyZ), new Vector2(12f, 0.6f), GasMat.White, 90f);
        // light panels set into the underside (y 6.15..6.21), two over each lane
        foreach (float x in new[] { -10.4f, 0f, 10.4f })
            foreach (float z in new[] { CanopyZ - 4f, CanopyZ + 4f })
                g.Box("Canopy Light", new Vector3(x, 6.18f, z), new Vector3(2.4f, 0.06f, 0.8f), GasMat.Light);
    }

    // ---- a pump island: kerbed island, two canopy posts, two double-sided diesel pumps, bin, bollards ----
    // Local: centre of the island, y 0 = forecourt top. The island runs along Z (z -6.5..+6.5).

    static void BuildIsland(GasKit g, int index)
    {
        // island, y 0..0.25 (on the pad), yellow noses at both ends (z ±6.2..6.5 wide, y 0..0.26)
        g.Box("Island", new Vector3(0f, IslandH / 2f, 0f), new Vector3(1.6f, IslandH, 13f), GasMat.Concrete);
        g.Box("Island Nose", new Vector3(0f, 0.13f, -6.2f), new Vector3(1.64f, 0.26f, 0.6f), GasMat.Yellow);
        g.Box("Island Nose", new Vector3(0f, 0.13f, 6.2f), new Vector3(1.64f, 0.26f, 0.6f), GasMat.Yellow);
        foreach (float z in new[] { -6.2f, 6.2f })
        {
            // yellow bollard standing in the nose (y 0.24..1.24) with a black band
            g.Cyl("Bollard", new Vector3(0f, 0.74f, z), 0.25f, 1f, GasMat.Yellow);
            g.Cyl("Bollard Band", new Vector3(0f, 1.0f, z), 0.26f, 0.1f, GasMat.Black);
        }

        foreach (float z in new[] { -5.5f, 5.5f })
        {
            // canopy post, y 0.2 (in the island) .. 6.25 (5 cm into the canopy underside), with an orange cladding at the foot
            g.Box("Canopy Post", new Vector3(0f, (0.2f + CanopyLow + 0.05f) / 2f, z), new Vector3(0.5f, CanopyLow + 0.05f - 0.2f, 0.5f), GasMat.White, collider: true);
            g.Box("Post Cladding", new Vector3(0f, 0.7f, z), new Vector3(0.7f, 1f, 0.7f), GasMat.Orange);
        }
        // fire extinguisher against the road-side post's cladding (cladding z 5.15..5.85; this one z 4.98..5.16)
        g.Cyl("Fire Extinguisher", new Vector3(0f, 0.52f, 5.07f), 0.18f, 0.56f, GasMat.Red);

        Pump(g.Group("Pump", new Vector3(0f, IslandH, -2.6f)), index * 2 + 1);
        Pump(g.Group("Pump", new Vector3(0f, IslandH, 2.6f)), index * 2 + 2);

        // bin in the middle of the island (y 0.24..1.14) with a lid and a window squeegee stuck in it
        g.Cyl("Bin", new Vector3(0f, 0.69f, 0f), 0.5f, 0.9f, GasMat.Navy);
        g.Cyl("Bin Lid", new Vector3(0f, 1.15f, 0f), 0.54f, 0.08f, GasMat.Orange);
        g.Box("Squeegee", new Vector3(0.12f, 1.35f, 0f), new Vector3(0.04f, 0.6f, 0.04f), GasMat.Black);
        g.Box("Squeegee Blade", new Vector3(0.12f, 1.66f, 0f), new Vector3(0.06f, 0.06f, 0.3f), GasMat.Black);
    }

    // A double-sided diesel pump. Local: y 0 = island top, the two faces look along ±X into the lanes.
    static void Pump(GasKit g, int number)
    {
        // plinth sunk 2 cm into the island (y -0.02..0.18)
        g.Box("Plinth", new Vector3(0f, 0.08f, 0f), new Vector3(1.1f, 0.2f, 1.4f), GasMat.ConcreteDark);
        // body (y 0.15..1.85, x ±0.35, z ±0.6), the part with a collider
        g.Box("Pump Body", new Vector3(0f, 1.0f, 0f), new Vector3(0.7f, 1.7f, 1.2f), GasMat.Orange, collider: true);
        // white lower panel wrapped round the body (y 0.3..0.8, 1 cm proud on each face)
        g.Box("Lower Panel", new Vector3(0f, 0.55f, 0f), new Vector3(0.72f, 0.5f, 1.0f), GasMat.White);
        // head on top (y 1.8..2.3) with a navy band round its top (y 2.22..2.32)
        g.Box("Pump Head", new Vector3(0f, 2.05f, 0f), new Vector3(0.8f, 0.5f, 1.3f), GasMat.White);
        g.Box("Head Band", new Vector3(0f, 2.27f, 0f), new Vector3(0.82f, 0.1f, 1.32f), GasMat.Navy);
        // pump number on both ends of the head
        g.Label("Pump Number", number.ToString(), new Vector3(0f, 2.03f, 0.67f), new Vector2(0.6f, 0.36f), GasMat.Navy, 180f);
        g.Label("Pump Number", number.ToString(), new Vector3(0f, 2.03f, -0.67f), new Vector2(0.6f, 0.36f), GasMat.Navy, 0f);

        foreach (float s in new[] { -1f, 1f })
        {
            float yaw = s > 0f ? -90f : 90f; // labels face this side's lane
            // black display set into the face (x 0.34..0.38, y 1.17..1.67) with green numbers
            g.Box("Display", new Vector3(s * 0.36f, 1.42f, 0f), new Vector3(0.04f, 0.5f, 0.8f), GasMat.Black);
            g.Label("Display Numbers", "0.00 L", new Vector3(s * 0.39f, 1.42f, 0f), new Vector2(0.7f, 0.36f), GasMat.Green, yaw);
            // DIESEL on the head (head face at x 0.40)
            g.Label("Diesel", "DIESEL", new Vector3(s * 0.42f, 2.02f, 0f), new Vector2(1.1f, 0.32f), GasMat.Navy, yaw);
            // nozzle holster on the face (x 0.34..0.46, y 0.84..1.12, z 0.24..0.46) and the nozzle in it (x 0.40..0.50)
            g.Box("Holster", new Vector3(s * 0.40f, 0.98f, 0.35f), new Vector3(0.12f, 0.28f, 0.22f), GasMat.Steel);
            g.Box("Nozzle", new Vector3(s * 0.45f, 0.98f, 0.35f), new Vector3(0.1f, 0.32f, 0.08f), GasMat.Black);
            // hose: drops down the face beside the display from y 1.8 (x 0.345..0.415, into the face), loops along Z at y 0.5,
            // and rises into the nozzle
            g.Cyl("Hose", new Vector3(s * 0.38f, 1.15f, -0.48f), 0.07f, 1.3f, GasMat.Black);
            g.Cyl("Hose", new Vector3(s * 0.38f, 0.5f, -0.065f), 0.07f, 0.9f, GasMat.Black, new Vector3(90f, 0f, 0f));
            g.Cyl("Hose", new Vector3(s * 0.38f, 0.69f, 0.35f), 0.07f, 0.4f, GasMat.Black);
        }
    }

    // ---- kiosk: the little shop at the back. Local: kiosk centre on the ground, front (door) faces +Z ----

    static void BuildKiosk(GasKit g)
    {
        const float hw = 8f, hd = 5f, H = 4.2f; // 16 x 10 m, 4.2 m high; front face at z +5
        // walls, with a collider (x -8..8, y 0..4.2, z -5..5)
        g.Box("Kiosk", new Vector3(0f, H / 2f, 0f), new Vector3(2f * hw, H, 2f * hd), GasMat.Cream, collider: true);
        g.Box("Base Band", new Vector3(0f, 0.3f, 0f), new Vector3(2f * hw + 0.08f, 0.6f, 2f * hd + 0.08f), GasMat.Navy);
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                g.Box("Corner", new Vector3(sx * hw, H / 2f, sz * hd), new Vector3(0.3f, H, 0.3f), GasMat.White);
        // flat grey roof (y 4.18..4.48, 0.4 m overhang) and an orange fascia round it (y 3.95..4.75)
        g.Box("Roof", new Vector3(0f, H + 0.13f, 0f), new Vector3(2f * hw + 0.8f, 0.3f, 2f * hd + 0.8f), GasMat.ConcreteDark);
        g.Box("Fascia", new Vector3(0f, 4.35f, hd + 0.45f), new Vector3(2f * hw + 0.9f, 0.8f, 0.15f), GasMat.Orange);
        g.Box("Fascia", new Vector3(0f, 4.35f, -hd - 0.45f), new Vector3(2f * hw + 0.9f, 0.8f, 0.15f), GasMat.Orange);
        g.Box("Fascia", new Vector3(-hw - 0.45f, 4.35f, 0f), new Vector3(0.15f, 0.8f, 2f * hd + 1.05f), GasMat.Orange);
        g.Box("Fascia", new Vector3(hw + 0.45f, 4.35f, 0f), new Vector3(0.15f, 0.8f, 2f * hd + 1.05f), GasMat.Orange);
        g.Label("Shop Sign", "JERRYS SHOP", new Vector3(0f, 4.35f, hd + 0.55f), new Vector2(14f, 0.6f), GasMat.White, 180f);
        g.Label("Shop Sign Side", "JERRYS", new Vector3(hw + 0.55f, 4.35f, 0f), new Vector2(9f, 0.6f), GasMat.White, -90f);

        // front: sidewalk (y 0..0.15, z 4.975..7.025, 2.5 cm into the wall), big window, ice chest, glass door, small window
        g.Box("Sidewalk", new Vector3(0f, 0.075f, hd + 1f), new Vector3(2f * hw + 1f, 0.15f, 2.05f), GasMat.Concrete);
        Window(g, "Shop Window", new Vector3(-3f, 1.95f, hd), 6f, 1.7f, 3, GasMat.White);                   // x -6..0, y 1.1..2.8
        g.Box("Sill", new Vector3(-3f, 1.05f, hd + 0.14f), new Vector3(6.4f, 0.1f, 0.25f), GasMat.White);
        Window(g, "Door", new Vector3(4f, 1.4f, hd), 2f, 2.5f, 1, GasMat.Navy);                              // x 3..5, y 0.15..2.65
        g.Box("Door Handle", new Vector3(4.75f, 1.3f, hd + 0.11f), new Vector3(0.06f, 0.6f, 0.08f), GasMat.Steel);
        g.Box("Door Canopy", new Vector3(4f, 2.95f, hd + 0.6f), new Vector3(2.8f, 0.15f, 1.25f), GasMat.Orange); // z 4.975..6.225
        Window(g, "Window", new Vector3(6.4f, 1.95f, hd), 2f, 1.7f, 1, GasMat.White);                       // x 5.4..7.4
        g.Box("Sill", new Vector3(6.4f, 1.05f, hd + 0.14f), new Vector3(2.4f, 0.1f, 0.25f), GasMat.White);
        // ice chest on the sidewalk between the shop window and the door (y 0.15..1.25)
        g.Box("Ice Chest", new Vector3(1.4f, 0.7f, hd + 0.45f), new Vector3(1.6f, 1.1f, 0.8f), GasMat.White);
        g.Box("Ice Chest Lid", new Vector3(1.4f, 1.28f, hd + 0.45f), new Vector3(1.64f, 0.08f, 0.84f), GasMat.Navy);
        g.Label("Ice Text", "ICE", new Vector3(1.4f, 0.75f, hd + 0.87f), new Vector2(1.2f, 0.5f), GasMat.Navy, 180f);
        // bin at the front corner (on the sidewalk, y 0.14..1.04)
        g.Cyl("Bin", new Vector3(hw + 0.1f, 0.59f, hd + 0.6f), 0.5f, 0.9f, GasMat.Navy);
        g.Cyl("Bin Lid", new Vector3(hw + 0.1f, 1.05f, hd + 0.6f), 0.54f, 0.08f, GasMat.Orange);
        // stacks of tyres for sale at the other front corner (on the sidewalk)
        TyreStack(g, new Vector3(-hw + 0.6f, 0.15f, hd + 1f), 5);
        TyreStack(g, new Vector3(-hw + 1.45f, 0.15f, hd + 1.2f), 3);

        // back door (z -5.09..-4.99)
        g.Box("Back Door", new Vector3(-4f, 1.1f, -hd - 0.04f), new Vector3(1f, 2.2f, 0.1f), GasMat.Steel);
        // the tanks' vent pipes up the +X side, outside the roof overhang (x 8.59..8.71), held by a bracket into the wall
        foreach (float z in new[] { -4f, -3.6f, -3.2f })
        {
            g.Cyl("Vent Pipe", new Vector3(hw + 0.65f, 2.75f, z), 0.12f, 5.5f, GasMat.Steel);   // y 0..5.5
            g.Ball("Vent Cap", new Vector3(hw + 0.65f, 5.5f, z), 0.2f, GasMat.Steel);
        }
        g.Box("Vent Bracket", new Vector3(hw + 0.35f, 3f, -3.6f), new Vector3(0.8f, 0.06f, 1.2f), GasMat.Steel); // x 7.95..8.75

        // pallet of four oil drums by the +X side (x 8.4..9.6, z -2.1..-0.9) and two loose drums beside it (y 0..0.88)
        var drums = g.Group("Oil Drums", new Vector3(hw + 1f, 0f, -1.5f));
        drums.Box("Pallet", new Vector3(0f, 0.075f, 0f), new Vector3(1.2f, 0.15f, 1.2f), GasMat.ConcreteDark);
        GasMat[] colours = { GasMat.Navy, GasMat.Orange, GasMat.Red, GasMat.Navy };
        for (int i = 0; i < 4; i++)
        {
            var p = new Vector3(i % 2 == 0 ? -0.3f : 0.3f, 0.585f, i < 2 ? -0.3f : 0.3f);
            drums.Cyl("Drum", p, 0.58f, 0.88f, colours[i]);                                    // y 0.145..1.025, on the pallet
            drums.Cyl("Drum Rim", p + new Vector3(0f, 0.25f, 0f), 0.6f, 0.04f, GasMat.Steel);
        }
        g.Cyl("Drum", new Vector3(hw + 1.1f, 0.44f, 0.4f), 0.58f, 0.88f, GasMat.Red);
        g.Cyl("Drum", new Vector3(hw + 1.8f, 0.44f, 0.1f), 0.58f, 0.88f, GasMat.Navy);

        // air and water post on the asphalt past the sidewalk's +X end (y 0..1.3), with a coiled hose on its side
        g.Box("Air Post", new Vector3(hw + 1.6f, 0.65f, hd + 0.9f), new Vector3(0.4f, 1.3f, 0.3f), GasMat.Orange);
        g.Box("Air Post Head", new Vector3(hw + 1.6f, 1.15f, hd + 0.9f), new Vector3(0.44f, 0.3f, 0.34f), GasMat.White);
        g.Cyl("Air Hose", new Vector3(hw + 1.83f, 0.8f, hd + 0.9f), 0.3f, 0.08f, GasMat.Black, new Vector3(0f, 0f, 90f)); // x 1.79..1.87, into the post side at 1.8
        g.Label("Air Text", "AIR", new Vector3(hw + 1.6f, 1.15f, hd + 1.09f), new Vector2(0.4f, 0.24f), GasMat.Navy, 180f);

        // three parking bays in front of the shop window (white lines on the asphalt, y 0..0.02, z 7.1..12.6)
        foreach (float x in new[] { -7.5f, -4.5f, -1.5f, 1.5f })
            g.Box("Parking Line", new Vector3(x, 0.01f, hd + 4.85f), new Vector3(0.15f, 0.02f, 5.5f), GasMat.White);
    }

    // tyres stacked on the ground; pos = middle of the bottom tyre's underside
    static void TyreStack(GasKit g, Vector3 pos, int count)
    {
        for (int i = 0; i < count; i++)
            g.Cyl("Tyre", pos + new Vector3(0f, 0.125f + i * 0.24f, 0f), 0.75f, 0.25f, GasMat.Black); // each 1 cm into the one below
        g.Cyl("Hub", pos + new Vector3(0f, count * 0.24f + 0.005f, 0f), 0.4f, 0.03f, GasMat.ConcreteDark); // on top of the stack
    }

    // ---- yard: the underground tanks' fill point ----

    static void BuildYard(GasKit g)
    {
        // underground tank fill point behind the canopy: yellow frame painted on the asphalt with three caps in it
        g.Box("Fill Point", new Vector3(10f, 0.01f, -2f), new Vector3(4.6f, 0.02f, 1.6f), GasMat.Yellow);
        for (int i = 0; i < 3; i++)
            g.Cyl("Tank Cap", new Vector3(8.5f + i * 1.5f, 0.025f, -2f), 0.7f, 0.04f, GasMat.Black);
    }

    // ---- the tall price sign. Local: foot of the sign on the ground, faces ±Z (the group is turned to face ±X) ----

    static void BuildPylon(GasKit g)
    {
        // concrete foot, 0.3 m in the ground (y -0.3..0.6)
        g.Box("Foot", new Vector3(0f, 0.15f, 0f), new Vector3(3.6f, 0.9f, 1.2f), GasMat.Concrete);
        // two posts standing in the foot (y 0.4..10.4), with colliders
        g.Box("Post", new Vector3(-1.3f, 5.4f, 0f), new Vector3(0.4f, 10f, 0.4f), GasMat.Navy, collider: true);
        g.Box("Post", new Vector3(1.3f, 5.4f, 0f), new Vector3(0.4f, 10f, 0.4f), GasMat.Navy, collider: true);
        // orange brand board round the posts (y 7.7..10.3) and a navy cap over it (y 10.275..10.525)
        g.Box("Brand Board", new Vector3(0f, 9f, 0f), new Vector3(3.8f, 2.6f, 0.5f), GasMat.Orange);
        g.Box("Cap", new Vector3(0f, 10.4f, 0f), new Vector3(4f, 0.25f, 0.7f), GasMat.Navy);
        // white price board below it (y 5.7..7.5) with a black display (y 5.85..6.45)
        g.Box("Price Board", new Vector3(0f, 6.6f, 0f), new Vector3(3.8f, 1.8f, 0.4f), GasMat.White);
        g.Box("Display", new Vector3(0f, 6.15f, 0f), new Vector3(3f, 0.6f, 0.46f), GasMat.Black);
        foreach (float s in new[] { -1f, 1f })
        {
            float yaw = s > 0f ? 180f : 0f;
            g.Label("Brand", "JERRYS", new Vector3(0f, 9.45f, s * 0.27f), new Vector2(3.4f, 1.2f), GasMat.White, yaw);
            g.Label("Brand Line", "GAS STATION", new Vector3(0f, 8.35f, s * 0.27f), new Vector2(3.4f, 0.6f), GasMat.White, yaw);
            g.Label("Fuel", "DIESEL", new Vector3(0f, 7.0f, s * 0.22f), new Vector2(3.2f, 0.7f), GasMat.Navy, yaw);
            g.Label("Display Text", "OPEN 24 H", new Vector3(0f, 6.15f, s * 0.25f), new Vector2(2.8f, 0.45f), GasMat.Green, yaw);
        }
    }

    // ---- shared pieces ----

    // a window (or glass door) on a wall that faces +Z; centre is on the wall face. Glass and frame sit 1 cm into the wall.
    static void Window(GasKit parent, string name, Vector3 centre, float w, float h, int panes, GasMat frame)
    {
        var g = parent.Group(name, centre);
        g.Box("Glass", new Vector3(0f, 0f, 0.04f), new Vector3(w, h, 0.1f), GasMat.Glass);
        const float t = 0.15f, z = 0.08f;
        g.Box("Frame", new Vector3(0f, h / 2f, z), new Vector3(w + t, t, 0.18f), frame);
        g.Box("Frame", new Vector3(0f, -h / 2f, z), new Vector3(w + t, t, 0.18f), frame);
        g.Box("Frame", new Vector3(-w / 2f, 0f, z), new Vector3(t, h + t, 0.18f), frame);
        g.Box("Frame", new Vector3(w / 2f, 0f, z), new Vector3(t, h + t, 0.18f), frame);
        for (int i = 1; i < panes; i++)
            g.Box("Mullion", new Vector3(-w / 2f + w * i / panes, 0f, z), new Vector3(t * 0.7f, h, 0.16f), frame);
    }

    // a street lamp on a base plate; the arm points along the lamp's local +Z (yaw 90 = toward +X)
    static void Lamp(GasKit parent, Vector3 pos, float yaw)
    {
        var g = parent.Group("Lamp", pos, yaw);
        g.Box("Base", new Vector3(0f, 0.05f, 0f), new Vector3(0.4f, 0.1f, 0.4f), GasMat.ConcreteDark);   // y 0..0.1
        g.Box("Pole", new Vector3(0f, 3.05f, 0f), new Vector3(0.2f, 6f, 0.2f), GasMat.Steel);             // y 0.05..6.05
        g.Box("Arm", new Vector3(0f, 6f, 0.6f), new Vector3(0.15f, 0.15f, 1.4f), GasMat.Steel);           // z -0.1..1.3
        g.Box("Light", new Vector3(0f, 5.88f, 1.2f), new Vector3(0.5f, 0.12f, 0.35f), GasMat.Light);
    }
}
