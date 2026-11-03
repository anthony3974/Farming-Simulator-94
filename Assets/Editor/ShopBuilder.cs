using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Editor tool: gives the seed shop a real shop look and builds the fertilizer shop next to it.
// Use: open GameScene, then Tools > Shop Builder, press the buttons, save the scene (Ctrl+S).
// Running it again replaces what it built before. Placeholder boxes, a real model can replace them later.
//
// Seed shop: keeps the "Seed Buying" object, its position and its Triggerbox (with the Building values).
// Only the old plain cube look goes away; the new look is a "Model" child that is rebuilt on every run.
// Fertilizer shop: a new "Fertilizer Buying" object, deleted and rebuilt on every run.
public class ShopBuilder : EditorWindow
{
    const string SeedRootName = "Seed Buying";
    const string FertRootName = "Fertilizer Buying";
    const string ModelName = "Model";
    const string MaterialFolder = "Assets/Shops";
    const float ClearGap = 15f; // free ground wanted around a shop lot

    // Fertilizer shop lot (metres). Its front (+Z) faces the main road.
    const float FertLotWidth = 70f, FertLotDepth = 30f;

    // Free ground west of the seed shop, between Random Field 1 and the main road, with a 15 m gap on every side.
    // (The 2D game put the shop near X 420, Z 820, but fields cover that spot in 3D.)
    Vector2 fertCentre = new(340f, 601.1f);

    [MenuItem("Tools/Shop Builder")]
    static void Open() { GetWindow<ShopBuilder>("Shops"); }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("Builds the shop looks. Run it with GameScene open, then save the scene (Ctrl+S). " +
            "Running it again replaces the old look.", MessageType.Info);

        EditorGUILayout.LabelField("Seed shop", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Keeps the 'Seed Buying' object, its place and its Triggerbox (prices stay as they are). " +
            "Removes the plain cube and builds a farm-supply store with a green roof and a SEED SHOP sign.", MessageType.None);
        if (GUILayout.Button("Rebuild seed shop")) BuildSeedShop();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Fertilizer shop", EditorStyles.boldLabel);
        fertCentre = EditorGUILayout.Vector2Field("Lot centre (X, Z)", fertCentre);
        if (GUILayout.Button("Build / rebuild fertilizer shop")) BuildFertilizerShop();
        if (GUILayout.Button("Remove fertilizer shop"))
        {
            var old = FindFertRoot();
            if (old != null) Undo.DestroyObjectImmediate(old);
        }
    }

    // ---------------------------------------------------------------- seed shop

    void BuildSeedShop()
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) { EditorUtility.DisplayDialog("Shops", "No terrain found in the open scene. Open GameScene first.", "OK"); return; }
        var root = FindSeedRoot();
        if (root == null) { EditorUtility.DisplayDialog("Shops", $"Could not find '{SeedRootName}' in the open scene.", "OK"); return; }
        var zone = FindZone(root, Building.BuildingType.Buy);
        if (zone == null) { EditorUtility.DisplayDialog("Shops", $"'{SeedRootName}' has no Triggerbox with a Building (Buy) on it.", "OK"); return; }

        Undo.SetCurrentGroupName("Rebuild seed shop");
        int undoGroup = Undo.GetCurrentGroup();

        UnscaleRoot(root.transform, zone);
        RemovePlainCube(root);
        var oldModel = root.transform.Find(ModelName);
        if (oldModel != null) Undo.DestroyObjectImmediate(oldModel.gameObject);

        // the lot is the trigger area, stretched forward to the edge of the main road
        var box = zone.GetComponent<BoxCollider>();
        Bounds zoneBox = box != null ? WorldBox(box) : new Bounds(zone.transform.position, new Vector3(41f, 10f, 53f));
        Vector3 o = root.transform.position;
        float minX = zoneBox.min.x - o.x, maxX = zoneBox.max.x - o.x;
        float minZ = zoneBox.min.z - o.z, maxZ = zoneBox.max.z - o.z;
        float? road = RoadEdgeInFront(new Vector3(o.x, 0f, zoneBox.max.z), 12f);
        if (road.HasValue) maxZ = road.Value - 0.05f - o.z;

        var lotCentre = new Vector3(o.x + (minX + maxX) / 2f, 0f, o.z + (minZ + maxZ) / 2f);
        float floor = Ground(terrain, lotCentre, maxX - minX, maxZ - minZ) + 0.05f; // top of the concrete

        var model = new GameObject(ModelName).transform;
        Undo.RegisterCreatedObjectUndo(model.gameObject, "Rebuild seed shop");
        model.SetParent(root.transform, false);
        model.SetPositionAndRotation(new Vector3(o.x, floor, o.z), Quaternion.identity);

        var m = new Mats();
        var concrete = m.Get("ShopConcrete", new Color(0.76f, 0.76f, 0.74f), 0.05f);
        var paving = m.Get("ShopPaving", new Color(0.86f, 0.85f, 0.81f), 0.05f);
        var glass = m.Get("ShopGlass", new Color(0.14f, 0.22f, 0.3f), 0.85f);
        var white = m.Get("ShopWhite", new Color(0.95f, 0.95f, 0.95f), 0.2f);
        var metal = m.Get("ShopMetal", new Color(0.5f, 0.52f, 0.55f), 0.5f);
        var wood = m.Get("ShopWood", new Color(0.62f, 0.46f, 0.28f), 0.05f);
        var yellow = m.Get("ShopYellow", new Color(0.97f, 0.8f, 0.12f), 0.2f);
        var cream = m.Get("SeedWall", new Color(0.93f, 0.89f, 0.78f), 0.1f);
        var green = m.Get("SeedGreen", new Color(0.12f, 0.45f, 0.18f), 0.2f);
        var roofGreen = m.Get("SeedRoof", new Color(0.15f, 0.36f, 0.19f), 0.3f);
        var sack = m.Get("SeedSack", new Color(0.86f, 0.78f, 0.6f), 0.05f);
        var sackGreen = m.Get("SeedSackGreen", new Color(0.46f, 0.63f, 0.3f), 0.05f);

        // concrete lot with a collider, so vehicles drive on it (its top is just above the ground)
        Box(model, "Lot", new Vector3((minX + maxX) / 2f, -0.5f, (minZ + maxZ) / 2f), new Vector3(maxX - minX, 1f, maxZ - minZ), concrete, collider: true);

        // the store: at the back of the lot, front (+Z) toward the road, gable end on the front like a farm store
        float cx = (minX + maxX) / 2f;
        const float W = 28f, D = 17f, H = 6.5f, rise = 5f;
        float back = minZ + 1f, zc = back + D / 2f, f = back + D; // f = front wall face
        Box(model, "Shop", new Vector3(cx, H / 2f, zc), new Vector3(W, H, D), cream, collider: true);
        Box(model, "Base Trim", new Vector3(cx, 0.4f, zc), new Vector3(W + 0.08f, 0.8f, D + 0.08f), green);
        foreach (float sx in new[] { -1f, 1f })
            foreach (float z in new[] { back, f })
                Box(model, "Corner", new Vector3(cx + sx * W / 2f, H / 2f, z), new Vector3(0.35f, H, 0.35f), white);
        PitchedRoof(model, new Vector3(cx, H, zc), W, D, rise, 0.8f, 0.35f, cream, roofGreen, white, ridgeAlongX: false);

        // big shop windows and a glass double door
        Box(model, "Sidewalk", new Vector3(cx, 0.075f, f + 1.25f), new Vector3(W + 2f, 0.15f, 2.5f), paving);
        foreach (float sx in new[] { -1f, 1f })
        {
            Window(model, "Window", new Vector3(cx + sx * 7.75f, 2.45f, f), 9.5f, 3.1f, 3, glass, white);
            Box(model, "Sill", new Vector3(cx + sx * 7.75f, 0.82f, f + 0.15f), new Vector3(9.9f, 0.12f, 0.3f), white);
            Box(model, "Door Handle", new Vector3(cx + sx * 0.35f, 1.2f, f + 0.22f), new Vector3(0.06f, 0.8f, 0.08f), metal);
        }
        Window(model, "Door", new Vector3(cx, 1.65f, f), 3.2f, 3f, 2, glass, green);

        // green and white striped awning over the windows and door
        const float awTop = 4.6f, awDepth = 2.6f, tilt = 18f, awWidth = 27f;
        const int stripes = 18;
        float cos = Mathf.Cos(tilt * Mathf.Deg2Rad), sin = Mathf.Sin(tilt * Mathf.Deg2Rad);
        var awning = Group(model, "Awning", Vector3.zero);
        for (int i = 0; i < stripes; i++)
        {
            float x = cx - awWidth / 2f + awWidth / stripes * (i + 0.5f);
            Box(awning, "Stripe", new Vector3(x, awTop - sin * awDepth / 2f, f + cos * awDepth / 2f), new Vector3(awWidth / stripes, 0.08f, awDepth),
                i % 2 == 0 ? green : white, euler: new Vector3(tilt, 0f, 0f));
        }
        Box(awning, "Edge", new Vector3(cx, awTop - sin * awDepth - 0.2f, f + cos * awDepth), new Vector3(awWidth, 0.4f, 0.06f), green);

        // signs: a small one under the eave, the big SEED SHOP sign in the gable
        Box(model, "Sub Sign", new Vector3(cx, 5.5f, f + 0.08f), new Vector3(16f, 0.9f, 0.16f), white);
        Label(model, "Sub Sign Text", "SEEDS  -  GRAIN  -  SUPPLIES", new Vector3(cx, 5.5f, f + 0.18f), new Vector2(15.4f, 0.75f), green.color, 180f);
        float signY = H + 1.3f;
        Box(model, "Sign Border", new Vector3(cx, signY, f + 0.08f), new Vector3(13.4f, 2.3f, 0.16f), yellow);
        Box(model, "Sign", new Vector3(cx, signY, f + 0.14f), new Vector3(13f, 1.9f, 0.2f), green);
        Label(model, "Sign Text", "SEED SHOP", new Vector3(cx, signY, f + 0.26f), new Vector2(12.4f, 1.7f), Color.white, 180f);
        Pylon(model, new Vector3(maxX - 3f, 0f, maxZ - 3f), "SEEDS", new Vector2(5.5f, 2.2f), 7.5f, green, yellow, metal, Color.white);

        // parking bays in front, on both sides of the door
        foreach (float x in new[] { -13f, -9.5f, -6f, 6f, 9.5f, 13f })
            Box(model, "Parking Line", new Vector3(cx + x, 0.01f, f + 5.25f), new Vector3(0.15f, 0.02f, 5.5f), white);

        // seed sacks on pallets: two by the door, a row along the west wall, two by the silo
        SackPallet(model, new Vector3(cx - 4.3f, 0.15f, f + 1.3f), 0f, wood, sack, sackGreen);
        SackPallet(model, new Vector3(cx + 4.3f, 0.15f, f + 1.3f), 0f, wood, sackGreen, sack);
        for (int i = 0; i < 5; i++)
            SackPallet(model, new Vector3(cx - W / 2f - 4f, 0f, back + 1.5f + i * 2.2f), 90f, wood, i % 2 == 0 ? sack : sackGreen, sack);
        float siloX = cx + W / 2f + 3.4f;
        SackPallet(model, new Vector3(siloX, 0f, back + 9f), 90f, wood, sack, sackGreen);
        SackPallet(model, new Vector3(siloX, 0f, back + 11.2f), 90f, wood, sackGreen, sack);
        SeedSilo(model, new Vector3(siloX, 0f, back + 4f), green, white, metal);

        // lamps along the west edge of the lot
        Lamp(model, new Vector3(minX + 0.8f, 0f, f + 6f), 90f, metal, white);
        Lamp(model, new Vector3(minX + 0.8f, 0f, maxZ - 2f), 90f, metal, white);

        SetStatic(model.gameObject);
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log($"Seed shop rebuilt at X {o.x:0}, Z {o.z:0} (lot {maxX - minX:0} x {maxZ - minZ:0} m). Save the scene (Ctrl+S).", root);
    }

    static GameObject FindSeedRoot()
    {
        var buildings = GameObject.Find("Buildings");
        var t = buildings != null ? buildings.transform.Find(SeedRootName) : null;
        if (t != null) return t.gameObject;
        foreach (var b in FindObjectsByType<Building>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (b.type == Building.BuildingType.Buy && b.transform.parent != null) return b.transform.parent.gameObject;
        return null;
    }

    static Building FindZone(GameObject root, Building.BuildingType type)
    {
        foreach (var b in root.GetComponentsInChildren<Building>(true))
            if (b.type == type) return b;
        return null;
    }

    // The old shop was one scaled cube, so its trigger child was scaled with it. Set the root's scale to 1
    // and move the size onto the trigger's BoxCollider, so the trigger keeps the same box in the world.
    static void UnscaleRoot(Transform root, Building zone)
    {
        Vector3 oldScale = root.localScale;
        if ((oldScale - Vector3.one).sqrMagnitude < 1e-6f) return; // already done on an earlier run

        var box = zone.GetComponent<BoxCollider>();
        Bounds zoneBox = box != null ? WorldBox(box) : default;
        var kids = new List<(Transform t, Vector3 pos, Quaternion rot)>();
        foreach (Transform c in root) kids.Add((c, c.position, c.rotation));

        Undo.RecordObject(root, "Rebuild seed shop");
        foreach (var k in kids) Undo.RecordObject(k.t, "Rebuild seed shop");
        if (box != null) Undo.RecordObject(box, "Rebuild seed shop");

        root.localScale = Vector3.one;
        foreach (var k in kids)
        {
            k.t.SetPositionAndRotation(k.pos, k.rot);
            k.t.localScale = Vector3.Scale(k.t.localScale, oldScale);
        }
        if (box != null)
        {
            zone.transform.localScale = Vector3.one;
            zone.transform.position = zoneBox.center;
            box.center = Vector3.zero;
            box.size = zoneBox.size;
        }
    }

    // the old look: the cube's renderer and solid collider on the root itself
    static void RemovePlainCube(GameObject root)
    {
        var r = root.GetComponent<MeshRenderer>();
        if (r != null) Undo.DestroyObjectImmediate(r);
        var col = root.GetComponent<Collider>();
        if (col != null && !col.isTrigger) Undo.DestroyObjectImmediate(col);
        var filter = root.GetComponent<MeshFilter>();
        if (filter != null) Undo.DestroyObjectImmediate(filter);
    }

    static void SeedSilo(Transform parent, Vector3 pos, Material body, Material cap, Material metal)
    {
        var s = Group(parent, "Seed Silo", pos);
        foreach (float x in new[] { -1f, 1f })
            foreach (float z in new[] { -1f, 1f })
                Box(s, "Leg", new Vector3(x, 1.75f, z), new Vector3(0.22f, 3.5f, 0.22f), metal);
        Cyl(s, "Chute", new Vector3(0f, 1.9f, 0f), 0.35f, 1.2f, metal);
        Ball(s, "Hopper", new Vector3(0f, 3.4f, 0f), new Vector3(3.2f, 2.4f, 3.2f), body);
        Cyl(s, "Bin", new Vector3(0f, 5.6f, 0f), 3.2f, 4.4f, body);
        Ball(s, "Cap", new Vector3(0f, 7.8f, 0f), new Vector3(3.2f, 1.3f, 3.2f), cap);
        Cyl(s, "Band", new Vector3(0f, 5f, 0f), 3.26f, 0.12f, cap);
        Cyl(s, "Band", new Vector3(0f, 6.6f, 0f), 3.26f, 0.12f, cap);
    }

    // ---------------------------------------------------------------- fertilizer shop

    void BuildFertilizerShop()
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) { EditorUtility.DisplayDialog("Shops", "No terrain found in the open scene. Open GameScene first.", "OK"); return; }
        var input = FindFirstObjectByType<InputManager>();
        var values = FindFirstObjectByType<PlayerValues>();
        if (input == null || values == null)
        {
            EditorUtility.DisplayDialog("Shops", "Could not find the InputManager or PlayerValues in the open scene.", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Build fertilizer shop");
        int undoGroup = Undo.GetCurrentGroup();

        var old = FindFertRoot();
        if (old != null) Undo.DestroyObjectImmediate(old);

        var c = new Vector3(fertCentre.x, 0f, fertCentre.y);
        float floor = Ground(terrain, c, FertLotWidth, FertLotDepth) + 0.05f; // top of the concrete

        var root = new GameObject(FertRootName);
        Undo.RegisterCreatedObjectUndo(root, "Build fertilizer shop");
        var buildings = GameObject.Find("Buildings");
        if (buildings != null) root.transform.SetParent(buildings.transform, false);
        root.transform.SetPositionAndRotation(new Vector3(c.x, floor, c.z), Quaternion.identity);
        var model = Group(root.transform, ModelName, Vector3.zero);

        var m = new Mats();
        var concrete = m.Get("ShopConcrete", new Color(0.76f, 0.76f, 0.74f), 0.05f);
        var concreteDark = m.Get("ShopConcreteDark", new Color(0.56f, 0.56f, 0.55f), 0.05f);
        var glass = m.Get("ShopGlass", new Color(0.14f, 0.22f, 0.3f), 0.85f);
        var white = m.Get("ShopWhite", new Color(0.95f, 0.95f, 0.95f), 0.2f);
        var metal = m.Get("ShopMetal", new Color(0.5f, 0.52f, 0.55f), 0.5f);
        var wood = m.Get("ShopWood", new Color(0.62f, 0.46f, 0.28f), 0.05f);
        var yellow = m.Get("ShopYellow", new Color(0.97f, 0.8f, 0.12f), 0.2f);
        var black = m.Get("ShopBlack", new Color(0.08f, 0.08f, 0.08f), 0.3f);
        var brown = m.Get("FertWall", new Color(0.48f, 0.3f, 0.18f), 0.1f);
        var brownDark = m.Get("FertWallDark", new Color(0.36f, 0.22f, 0.13f), 0.1f);
        var blue = m.Get("FertBlue", new Color(0.1f, 0.3f, 0.66f), 0.3f);
        var roof = m.Get("FertRoof", new Color(0.22f, 0.26f, 0.32f), 0.3f);
        var door = m.Get("FertDoor", new Color(0.78f, 0.79f, 0.8f), 0.3f);
        var tankBlue = m.Get("FertTank", new Color(0.16f, 0.42f, 0.75f), 0.5f);
        var bag = m.Get("FertBag", new Color(0.93f, 0.93f, 0.9f), 0.05f);
        var sack = m.Get("FertSack", new Color(0.55f, 0.38f, 0.22f), 0.05f);

        // concrete lot with a collider, so vehicles drive on it
        const float halfW = FertLotWidth / 2f, halfD = FertLotDepth / 2f;
        Box(model, "Lot", new Vector3(0f, -0.5f, 0f), new Vector3(FertLotWidth, 1f, FertLotDepth), concrete, collider: true);

        // driveway from the lot to the main road in front, if the road is close
        const float driveX = -10f, driveWidth = 16f;
        float? road = RoadEdgeInFront(new Vector3(c.x + driveX, 0f, c.z + halfD), 40f);
        if (road.HasValue)
        {
            float end = road.Value - 0.05f - c.z;
            Box(model, "Driveway", new Vector3(driveX, -0.5f, (halfD + end) / 2f), new Vector3(driveWidth, 1f, end - halfD), concrete, collider: true);
        }

        // warehouse at the back-left of the lot, long side to the road, brown walls with blue bands
        const float wc = -15f, W = 36f, D = 13f, H = 8f;
        float back = -halfD + 1f, zc = back + D / 2f, f = back + D; // f = front wall face
        Box(model, "Warehouse", new Vector3(wc, H / 2f, zc), new Vector3(W, H, D), brown, collider: true);
        Box(model, "Base Band", new Vector3(wc, 0.35f, zc), new Vector3(W + 0.08f, 0.7f, D + 0.08f), blue);
        Box(model, "Top Band", new Vector3(wc, H - 0.35f, zc), new Vector3(W + 0.08f, 0.7f, D + 0.08f), blue);
        foreach (float sx in new[] { -1f, 1f })
            foreach (float z in new[] { back, f })
                Box(model, "Corner", new Vector3(wc + sx * W / 2f, H / 2f, z), new Vector3(0.35f, H, 0.35f), blue);
        // metal-sheet ribs on the side and back walls
        var ribs = Group(model, "Wall Ribs", Vector3.zero);
        for (float z = back + 1.2f; z < f - 0.6f; z += 1.6f)
            foreach (float sx in new[] { -1f, 1f })
                Box(ribs, "Rib", new Vector3(wc + sx * (W / 2f + 0.05f), H / 2f, z), new Vector3(0.1f, H - 1.4f, 0.12f), brownDark);
        for (float x = wc - W / 2f + 1.5f; x < wc + W / 2f - 0.6f; x += 2f)
            Box(ribs, "Rib", new Vector3(x, H / 2f, back - 0.05f), new Vector3(0.12f, H - 1.4f, 0.1f), brownDark);
        PitchedRoof(model, new Vector3(wc, H, zc), D, W, 2.2f, 0.6f, 0.3f, brown, roof, blue, ridgeAlongX: true);

        // two big roller doors with yellow bollards
        foreach (float dx in new[] { -27f, -17f })
        {
            var d = Group(model, "Roller Door", new Vector3(dx, 0f, f));
            Box(d, "Door", new Vector3(0f, 2.5f, 0.06f), new Vector3(7f, 5f, 0.12f), door);
            for (int i = 1; i <= 7; i++)
                Box(d, "Slat", new Vector3(0f, i * 0.62f, 0.13f), new Vector3(7f, 0.05f, 0.03f), metal);
            Box(d, "Frame", new Vector3(-3.7f, 2.7f, 0.1f), new Vector3(0.4f, 5.4f, 0.2f), blue);
            Box(d, "Frame", new Vector3(3.7f, 2.7f, 0.1f), new Vector3(0.4f, 5.4f, 0.2f), blue);
            Box(d, "Frame", new Vector3(0f, 5.2f, 0.1f), new Vector3(7.8f, 0.4f, 0.2f), blue);
            Cyl(d, "Bollard", new Vector3(-4.2f, 0.55f, 0.8f), 0.3f, 1.1f, yellow);
            Cyl(d, "Bollard", new Vector3(4.2f, 0.55f, 0.8f), 0.3f, 1.1f, yellow);
        }

        // small office: glass door with a canopy and a window
        Window(model, "Office Door", new Vector3(-8.5f, 1.25f, f), 1.4f, 2.5f, 1, glass, blue);
        Box(model, "Door Canopy", new Vector3(-8.5f, 2.85f, f + 0.75f), new Vector3(2.6f, 0.15f, 1.5f), blue);
        Window(model, "Office Window", new Vector3(-3f, 2f, f), 5f, 1.6f, 2, glass, white);

        // big FERTILIZER SHOP sign over the doors
        Box(model, "Sign Border", new Vector3(wc, 6.75f, f + 0.07f), new Vector3(25f, 2.3f, 0.14f), white);
        Box(model, "Sign", new Vector3(wc, 6.75f, f + 0.14f), new Vector3(24.5f, 1.9f, 0.2f), blue);
        Label(model, "Sign Text", "FERTILIZER SHOP", new Vector3(wc, 6.75f, f + 0.26f), new Vector2(23.5f, 1.6f), Color.white, 180f);

        // tank yard: three blue liquid fertilizer tanks (N, P, K) on a pad with a yellow curb
        var yard = Group(model, "Tank Yard", Vector3.zero);
        const float tz = -7.5f, tankD = 6.4f, tankH = 10f, padTop = 0.3f;
        Box(yard, "Pad", new Vector3(20f, padTop / 2f, tz), new Vector3(26f, padTop, 12.5f), concreteDark);
        Box(yard, "Curb", new Vector3(20f, 0.55f, tz + 6.1f), new Vector3(26f, 0.5f, 0.3f), yellow);
        Box(yard, "Curb", new Vector3(20f, 0.55f, tz - 6.1f), new Vector3(26f, 0.5f, 0.3f), yellow);
        Box(yard, "Curb", new Vector3(7.15f, 0.55f, tz), new Vector3(0.3f, 0.5f, 12.5f), yellow);
        Box(yard, "Curb", new Vector3(32.85f, 0.55f, tz), new Vector3(0.3f, 0.5f, 12.5f), yellow);
        float[] tankX = { 11.5f, 20f, 28.5f };
        string[] letters = { "N", "P", "K" };
        for (int i = 0; i < tankX.Length; i++)
        {
            float x = tankX[i];
            Cyl(yard, "Tank", new Vector3(x, padTop + tankH / 2f, tz), tankD, tankH, tankBlue, collider: true);
            Ball(yard, "Tank Top", new Vector3(x, padTop + tankH, tz), new Vector3(tankD, 1.8f, tankD), white);
            Cyl(yard, "Tank Band", new Vector3(x, 3.6f, tz), tankD + 0.1f, 0.2f, white);
            Cyl(yard, "Tank Band", new Vector3(x, 7f, tz), tankD + 0.1f, 0.2f, white);
            Label(yard, "Tank Letter", letters[i], new Vector3(x, 5.3f, tz + tankD / 2f + 0.05f), new Vector2(2.6f, 2.6f), Color.white, 180f);
            Cyl(yard, "Pipe Stub", new Vector3(x, 1.3f, tz + tankD / 2f + 0.1f), 0.3f, 0.8f, metal, euler: new Vector3(90f, 0f, 0f));
        }
        // walkway over the tank tops, with railings, out to a ladder on the side of the last tank
        float walkY = padTop + tankH + 1f;
        float ladderX = tankX[2] + tankD / 2f + 0.15f;
        float walkStart = tankX[0], walkLen = ladderX - walkStart;
        Box(yard, "Walkway", new Vector3(walkStart + walkLen / 2f, walkY, tz), new Vector3(walkLen, 0.15f, 1.1f), metal);
        foreach (float sz in new[] { -0.55f, 0.55f })
        {
            Box(yard, "Rail", new Vector3(walkStart + walkLen / 2f, walkY + 1f, tz + sz), new Vector3(walkLen, 0.06f, 0.06f), yellow);
            for (int i = 0; i <= 10; i++)
                Box(yard, "Rail Post", new Vector3(walkStart + i * walkLen / 10f, walkY + 0.5f, tz + sz), new Vector3(0.06f, 1f, 0.06f), yellow);
        }
        foreach (float sz in new[] { -0.25f, 0.25f })
            Box(yard, "Ladder Rail", new Vector3(ladderX, padTop + (walkY - padTop) / 2f, tz + sz), new Vector3(0.06f, walkY - padTop, 0.06f), yellow);
        for (float y = padTop + 0.4f; y < walkY; y += 0.5f)
            Box(yard, "Ladder Rung", new Vector3(ladderX, y, tz), new Vector3(0.05f, 0.05f, 0.5f), yellow);

        // pipes to a loading arm over the forecourt, with a yellow gantry and a fill bay
        float pipeZ = tz + tankD / 2f + 0.7f;
        Cyl(yard, "Pipe", new Vector3(20f, 1.3f, pipeZ), 0.4f, 20f, metal, euler: new Vector3(0f, 0f, 90f));
        Cyl(yard, "Riser", new Vector3(20f, 3.8f, pipeZ), 0.4f, 5f, metal);
        const float armZ = 4.2f;
        Cyl(yard, "Loading Arm", new Vector3(20f, 6.3f, (pipeZ + armZ + 0.2f) / 2f), 0.4f, armZ + 0.2f - pipeZ, metal, euler: new Vector3(90f, 0f, 0f));
        Cyl(yard, "Hose", new Vector3(20f, 5.3f, armZ), 0.3f, 2f, black);
        Box(yard, "Gantry Post", new Vector3(17f, 3.4f, armZ), new Vector3(0.3f, 6.8f, 0.3f), yellow);
        Box(yard, "Gantry Post", new Vector3(23f, 3.4f, armZ), new Vector3(0.3f, 6.8f, 0.3f), yellow);
        Box(yard, "Gantry Beam", new Vector3(20f, 6.8f, armZ), new Vector3(6.4f, 0.3f, 0.3f), yellow);
        Box(yard, "Bay Line", new Vector3(16.6f, 0.01f, armZ + 1f), new Vector3(0.15f, 0.02f, 9f), yellow);
        Box(yard, "Bay Line", new Vector3(23.4f, 0.01f, armZ + 1f), new Vector3(0.15f, 0.02f, 9f), yellow);
        Box(yard, "Bay Line", new Vector3(20f, 0.01f, armZ + 5.5f), new Vector3(6.95f, 0.02f, 0.15f), yellow);

        // big white bulk bags at the front-left corner, and brown sacks by the office
        for (int row = 0; row < 2; row++)
            for (int col = 0; col < 3; col++)
            {
                var p = new Vector3(-halfW + 1.6f + col * 1.6f, 0f, halfD - 5f + row * 1.6f);
                BulkBag(model, p, 0f, wood, bag, blue);
                if ((row + col) % 2 == 0) BulkBag(model, p, 1.42f, wood, bag, blue);
            }
        SackPallet(model, new Vector3(1.5f, 0f, f + 0.6f), 0f, wood, sack, bag);
        SackPallet(model, new Vector3(1.5f, 0f, f + 2f), 0f, wood, bag, sack);

        Pylon(model, new Vector3(halfW - 4f, 0f, halfD - 2f), "FERTILIZER", new Vector2(7f, 2.2f), 7.5f, blue, white, metal, Color.white);
        Lamp(model, new Vector3(-halfW + 0.8f, 0f, 2f), 90f, metal, white);
        Lamp(model, new Vector3(halfW - 0.8f, 0f, 2f), -90f, metal, white);
        SetStatic(model.gameObject);

        // the whole lot is the shop zone (like the dealer): press E / USE anywhere on it
        var zone = new GameObject("Triggerbox");
        zone.transform.SetParent(root.transform, false);
        zone.transform.localPosition = new Vector3(0f, 4f, 0f);
        var trigger = zone.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(FertLotWidth, 8f, FertLotDepth);
        var building = zone.AddComponent<Building>();
        building.type = Building.BuildingType.Fertilizer;
        building.inputManager = input;
        building.playerValues = values;

        WarnIfCrowded(root, new Bounds(new Vector3(c.x, floor, c.z), new Vector3(FertLotWidth, 1f, FertLotDepth)), terrain);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log($"Fertilizer shop built at X {c.x:0}, Z {c.z:0} (lot {FertLotWidth:0} x {FertLotDepth:0} m). Save the scene (Ctrl+S).", root);
    }

    static GameObject FindFertRoot()
    {
        var buildings = GameObject.Find("Buildings");
        var t = buildings != null ? buildings.transform.Find(FertRootName) : null;
        if (t != null) return t.gameObject;
        foreach (var b in FindObjectsByType<Building>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (b.type == Building.BuildingType.Fertilizer && b.transform.parent != null && b.transform.parent.name == FertRootName)
                return b.transform.parent.gameObject;
        return null;
    }

    static void BulkBag(Transform parent, Vector3 pos, float baseY, Material wood, Material bag, Material band)
    {
        var g = Group(parent, "Bulk Bag", pos + Vector3.up * baseY);
        Box(g, "Pallet", new Vector3(0f, 0.075f, 0f), new Vector3(1.4f, 0.15f, 1.4f), wood);
        Box(g, "Bag", new Vector3(0f, 0.775f, 0f), new Vector3(1.3f, 1.25f, 1.3f), bag);
        Box(g, "Band", new Vector3(0f, 1.15f, 0f), new Vector3(1.32f, 0.12f, 1.32f), band);
        if (baseY == 0f) return; // only the top bag shows its lifting loops
        Box(g, "Loop", new Vector3(-0.4f, 1.5f, 0f), new Vector3(0.08f, 0.25f, 0.5f), band);
        Box(g, "Loop", new Vector3(0.4f, 1.5f, 0f), new Vector3(0.08f, 0.25f, 0.5f), band);
    }

    // a warning in the Console if the lot is closer than 15 m to a field, road, building or the map edge
    static void WarnIfCrowded(GameObject self, Bounds lot, Terrain terrain)
    {
        var problems = new List<string>();
        void Check(string group)
        {
            var g = GameObject.Find(group);
            if (g == null) return;
            foreach (var r in g.GetComponentsInChildren<Renderer>())
            {
                if (r.transform.IsChildOf(self.transform) || r is ParticleSystemRenderer) continue;
                var b = r.bounds;
                float gapX = Mathf.Max(b.min.x - lot.max.x, lot.min.x - b.max.x);
                float gapZ = Mathf.Max(b.min.z - lot.max.z, lot.min.z - b.max.z);
                if (Mathf.Max(gapX, gapZ) < ClearGap) problems.Add($"{r.name} ({Mathf.Max(gapX, gapZ, 0f):0.0} m)");
            }
        }
        Check("Fields");
        Check("Roads and paths");
        Check("Buildings");
        Vector3 o = terrain.transform.position, s = terrain.terrainData.size;
        if (lot.min.x - o.x < ClearGap + 1f || o.x + s.x - lot.max.x < ClearGap + 1f || lot.min.z - o.z < ClearGap + 1f || o.z + s.z - lot.max.z < ClearGap + 1f)
            problems.Add("the map edge");
        if (problems.Count > 0)
            Debug.LogWarning($"Fertilizer shop lot is closer than {ClearGap:0} m to: {string.Join(", ", problems)}. Try another lot centre.", self);
    }

    // ---------------------------------------------------------------- shared pieces

    // the lowest edge of a road in front (+Z) of a point, if one starts within maxDistance
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

    // the highest ground under a rectangle, so a slab never sinks into a bump
    static float Ground(Terrain terrain, Vector3 centre, float sizeX, float sizeZ)
    {
        float ground = float.MinValue;
        for (int i = 0; i <= 4; i++)
            for (int k = 0; k <= 4; k++)
            {
                var p = centre + new Vector3((i / 4f - 0.5f) * sizeX, 0f, (k / 4f - 0.5f) * sizeZ);
                ground = Mathf.Max(ground, terrain.SampleHeight(p) + terrain.transform.position.y);
            }
        return ground;
    }

    // a BoxCollider's box in world space (no rotation)
    static Bounds WorldBox(BoxCollider box)
    {
        var s = Vector3.Scale(box.size, box.transform.lossyScale);
        return new Bounds(box.transform.TransformPoint(box.center), new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z)));
    }

    // A pitched roof on a box building. top = centre of the wall top; span = width under the two slopes;
    // length = along the ridge. The ridge runs along Z, or along X when ridgeAlongX is set.
    static void PitchedRoof(Transform parent, Vector3 top, float span, float length, float rise, float overhang, float thickness,
        Material wallMat, Material roofMat, Material ridgeMat, bool ridgeAlongX)
    {
        var roof = Group(parent, "Roof", top, ridgeAlongX ? 90f : 0f);
        float half = span / 2f;

        // the triangle gable ends: a cube turned 45 degrees and squashed flat makes a diamond,
        // and its lower half hides inside the walls
        var gable = Group(roof, "Gable", Vector3.zero);
        gable.localScale = new Vector3(1f, rise / half, 1f);
        float side = span / Mathf.Sqrt(2f);
        Box(gable, "Gable Fill", Vector3.zero, new Vector3(side, side, length - 0.02f), wallMat, euler: new Vector3(0f, 0f, 45f));

        // two roof slabs; their undersides run from the wall top edge up to the ridge
        float a = Mathf.Atan2(rise, half);
        float slope = Mathf.Sqrt(half * half + rise * rise) + overhang;
        var mid = new Vector2((-half - overhang * Mathf.Cos(a)) / 2f, (rise - overhang * Mathf.Sin(a)) / 2f);
        var p = mid + new Vector2(-Mathf.Sin(a), Mathf.Cos(a)) * (thickness / 2f);
        float deg = a * Mathf.Rad2Deg;
        var size = new Vector3(slope, thickness, length + 2f * overhang);
        Box(roof, "Roof Slope", new Vector3(p.x, p.y, 0f), size, roofMat, euler: new Vector3(0f, 0f, deg));
        Box(roof, "Roof Slope", new Vector3(-p.x, p.y, 0f), size, roofMat, euler: new Vector3(0f, 0f, -deg));
        Box(roof, "Ridge", new Vector3(0f, rise + thickness, 0f), new Vector3(0.9f, 0.35f, length + 2f * overhang + 0.1f), ridgeMat);
    }

    // a window (or glass door) on a wall that faces +Z; centre is on the wall face
    static void Window(Transform parent, string name, Vector3 centre, float w, float h, int panes, Material glass, Material frame)
    {
        var g = Group(parent, name, centre);
        Box(g, "Glass", new Vector3(0f, 0f, 0.05f), new Vector3(w, h, 0.1f), glass);
        const float t = 0.15f, z = 0.09f; // the frame sticks out a little past the glass
        Box(g, "Frame", new Vector3(0f, h / 2f, z), new Vector3(w + t, t, 0.18f), frame);
        Box(g, "Frame", new Vector3(0f, -h / 2f, z), new Vector3(w + t, t, 0.18f), frame);
        Box(g, "Frame", new Vector3(-w / 2f, 0f, z), new Vector3(t, h + t, 0.18f), frame);
        Box(g, "Frame", new Vector3(w / 2f, 0f, z), new Vector3(t, h + t, 0.18f), frame);
        for (int i = 1; i < panes; i++)
            Box(g, "Mullion", new Vector3(-w / 2f + w * i / panes, 0f, z), new Vector3(t * 0.7f, h, 0.16f), frame);
    }

    // a pallet with sacks stacked in layers of four
    static void SackPallet(Transform parent, Vector3 pos, float yaw, Material wood, Material sackA, Material sackB, int layers = 3)
    {
        var g = Group(parent, "Sack Pallet", pos, yaw);
        Box(g, "Pallet", new Vector3(0f, 0.075f, 0f), new Vector3(1.2f, 0.15f, 1f), wood);
        for (int l = 0; l < layers; l++)
            for (int i = 0; i < 2; i++)
                for (int k = 0; k < 2; k++)
                {
                    float turn = (i * 3 + k * 5 + l * 7) % 5 - 2; // a small fixed twist so the stack looks hand-made
                    Box(g, "Sack", new Vector3(i * 0.58f - 0.29f, 0.15f + 0.13f + l * 0.27f, k * 0.48f - 0.24f), new Vector3(0.55f, 0.26f, 0.45f),
                        (i + k + l) % 2 == 0 ? sackA : sackB, euler: new Vector3(0f, turn, 0f));
                }
    }

    // a tall sign on two posts by the lot entrance, readable from both sides
    static void Pylon(Transform parent, Vector3 pos, string text, Vector2 board, float height, Material boardMat, Material borderMat, Material postMat, Color textColor)
    {
        var g = Group(parent, "Pylon Sign", pos);
        Box(g, "Post", new Vector3(-board.x * 0.3f, height / 2f, 0f), new Vector3(0.3f, height, 0.3f), postMat);
        Box(g, "Post", new Vector3(board.x * 0.3f, height / 2f, 0f), new Vector3(0.3f, height, 0.3f), postMat);
        float y = height - board.y / 2f;
        Box(g, "Border", new Vector3(0f, y, 0f), new Vector3(board.x + 0.3f, board.y + 0.3f, 0.3f), borderMat);
        Box(g, "Board", new Vector3(0f, y, 0f), new Vector3(board.x, board.y, 0.4f), boardMat);
        Label(g, "Text Front", text, new Vector3(0f, y, 0.22f), board * 0.9f, textColor, 180f);
        Label(g, "Text Back", text, new Vector3(0f, y, -0.22f), board * 0.9f, textColor, 0f);
    }

    // a street lamp; the arm points along the lamp's local +Z (yaw 90 = toward +X)
    static void Lamp(Transform parent, Vector3 pos, float yaw, Material metal, Material light)
    {
        var g = Group(parent, "Lamp", pos, yaw);
        Box(g, "Pole", new Vector3(0f, 3f, 0f), new Vector3(0.2f, 6f, 0.2f), metal);
        Box(g, "Arm", new Vector3(0f, 6f, 0.6f), new Vector3(0.15f, 0.15f, 1.4f), metal);
        Box(g, "Light", new Vector3(0f, 5.88f, 1.2f), new Vector3(0.5f, 0.12f, 0.35f), light);
    }

    // TextMeshPro text is read from its -Z side, so yaw 180 makes it face +Z (toward the road)
    static TextMeshPro Label(Transform parent, string name, string text, Vector3 pos, Vector2 size, Color color, float yaw)
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
        tmp.fontSizeMax = 60f;
        tmp.rectTransform.sizeDelta = size;
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        return tmp;
    }

    static Transform Group(Transform parent, string name, Vector3 pos, float yaw = 0f)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = pos;
        t.localRotation = Quaternion.Euler(0f, yaw, 0f);
        return t;
    }

    static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, bool collider = false, Vector3 euler = default)
        => Prim(PrimitiveType.Cube, parent, name, pos, size, mat, collider, euler);

    // Unity's cylinder is 2 units tall, so the Y scale is half the height
    static GameObject Cyl(Transform parent, string name, Vector3 pos, float diameter, float height, Material mat, bool collider = false, Vector3 euler = default)
        => Prim(PrimitiveType.Cylinder, parent, name, pos, new Vector3(diameter, height / 2f, diameter), mat, collider, euler);

    static GameObject Ball(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        => Prim(PrimitiveType.Sphere, parent, name, pos, size, mat, false, default);

    // all positions are local to the parent; only pieces asked for keep their collider
    static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 pos, Vector3 scale, Material mat, bool collider, Vector3 euler)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        if (!collider) DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static void SetStatic(GameObject root)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            t.gameObject.isStatic = true;
    }

    // materials are saved as assets in Assets/Shops, so the scene keeps them; an existing one is reused as it is
    class Mats
    {
        public Material Get(string name, Color color, float gloss)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets", "Shops");
            mat = new Material(Shader.Find("Standard")) { color = color };
            mat.SetFloat("_Glossiness", gloss);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}
