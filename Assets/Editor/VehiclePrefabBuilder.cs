using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-off editor tool: turns the van and the harvester into real prefabs and parks one of each on "Parking"
// by the house. Until now they were built from code at runtime when bought, so they could not be seen or
// edited in the editor. After running this:
// - Assets/Prefabs/Van.prefab and Assets/Prefabs/Harvester.prefab hold the models. Edit them like any prefab.
// - Their materials are in Assets/Vehicles (VanPaint, HarvesterPaint, VehicleDark, VehicleGlass, ...). Change a
//   colour there and every part using it changes. Vehicles are red (VanPaint / HarvesterPaint); the dealer has no
//   colour choice, so the paint material's colour is the colour you drive.
// - A Van and a Harvester stand on Parking, nose toward the SubRoad. You don't own them until you buy them at
//   the dealer; then 2 / 3 (or CAR) drives them. Move them anywhere you like in the scene.
// Use: open GameScene (Play mode off), Tools > Vehicle Prefabs > Create van and harvester on Parking, check, Ctrl+S.
// Running it again asks before overwriting existing prefabs, and never adds a second van or harvester to the scene.
// The model code was moved here from VehicleFactory (the runtime builder, now removed). Forward is +Z, pivot on the ground.
public static class VehiclePrefabBuilder
{
    const string PrefabFolder = "Assets/Prefabs";
    const string MaterialFolder = "Assets/Vehicles";
    const string ParkingName = "Parking";
    const string UndoName = "Create van and harvester";

    static readonly Color Dark = new Color32(35, 35, 40, 255);
    static readonly Color Glass = new Color32(40, 60, 80, 255);
    static readonly Color Metal = new Color32(120, 120, 125, 255);
    static readonly Color Light = new Color32(255, 245, 200, 255);
    static readonly Color TailRed = new Color32(190, 15, 15, 255);
    static readonly Color Amber = new Color32(255, 140, 0, 255);
    static readonly Color Plate = new Color32(245, 205, 40, 255);
    static readonly Color Paint = Color.white; // stands for "the vehicle's paint material"

    // the material asset for each colour; Paint maps to the vehicle being built (VanPaint / HarvesterPaint)
    static readonly Dictionary<Color, (string name, float gloss)> FixedMaterials = new()
    {
        { Dark, ("VehicleDark", 0.15f) },
        { Glass, ("VehicleGlass", 0.85f) },
        { Metal, ("VehicleMetal", 0.55f) },
        { Light, ("VehicleLight", 0.6f) },
        { TailRed, ("VehicleTailRed", 0.6f) },
        { Amber, ("VehicleAmber", 0.6f) },
        { Plate, ("VehiclePlate", 0.3f) },
    };
    static Material paintMaterial;

    [MenuItem("Tools/Vehicle Prefabs/Create van and harvester on Parking")]
    static void Create()
    {
        if (EditorApplication.isPlaying) { Fail("Stop Play mode first. Changes made in Play mode are not saved."); return; }
        var parking = GameObject.Find(ParkingName);
        if (parking == null || !parking.TryGetComponent(out Renderer parkingRenderer))
        {
            Fail($"Could not find '{ParkingName}' (the parking lot by the house) in the open scene. Open GameScene first.");
            return;
        }

        var log = new System.Text.StringBuilder();
        var van = MakePrefab(VehicleKind.Van, log);
        var harvester = MakePrefab(VehicleKind.Harvester, log);
        if (van == null || harvester == null) { Debug.Log("Vehicle prefabs: stopped." + log); return; }

        // park them across the lot, nose toward the SubRoad (-X), van at the -Z end and the harvester at the +Z end
        Undo.SetCurrentGroupName(UndoName);
        int undoGroup = Undo.GetCurrentGroup();
        Bounds lot = parkingRenderer.bounds;
        var facing = Quaternion.Euler(0f, -90f, 0f);
        Place(VehicleKind.Van, van, parking.transform.parent, new Vector3(lot.center.x, 0f, lot.center.z - lot.extents.z * 0.45f), facing, log);
        Place(VehicleKind.Harvester, harvester, parking.transform.parent, new Vector3(lot.center.x, 0f, lot.center.z + lot.extents.z * 0.35f), facing, log);
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(parking.scene);

        Debug.Log("Vehicle prefabs done. Save the scene (Ctrl+S)." + log);
    }

    // ---- prefabs ----

    static GameObject MakePrefab(VehicleKind kind, System.Text.StringBuilder log)
    {
        var info = VehicleCatalog.Get(kind);
        string path = $"{PrefabFolder}/{info.name}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null &&
            !EditorUtility.DisplayDialog("Vehicle Prefabs", $"{path} already exists. Overwrite it with a fresh model? " +
                "Any changes you made to that prefab are lost (the material assets are kept).", "Overwrite", "Keep mine"))
        {
            log.Append($"\nKept the existing {path}.");
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        EnsureFolder(MaterialFolder);
        paintMaterial = GetOrCreate(info.name + "Paint", new Color32(200, 35, 30, 255), 0.5f); // red, the only colour

        var root = new GameObject(info.name);
        try
        {
            var paintParts = new List<Renderer>();
            Vector3 bodyCenter, bodySize, camera;
            float radius;
            if (kind == VehicleKind.Harvester)
            {
                BuildHarvester(root.transform, paintParts);
                bodyCenter = new Vector3(0, 1.9f, 0.3f); bodySize = new Vector3(3.0f, 3.6f, 7.6f);
                radius = 3.4f; camera = new Vector3(0, 8f, -11f);
            }
            else
            {
                BuildVan(root.transform, paintParts);
                bodyCenter = new Vector3(0, 1.4f, 0); bodySize = new Vector3(2.1f, 2.4f, 5.2f);
                radius = 2.5f; camera = new Vector3(0, 5f, -8.5f);
            }

            // the controller drives it. Like the tractor's it is a ball about as wide as the vehicle is long, so the
            // ends do not poke into walls. The box makes the parked vehicle solid (the vehicle ignores its own box).
            var cc = root.AddComponent<CharacterController>();
            cc.radius = radius;
            cc.height = radius * 2f;
            cc.center = new Vector3(0, radius + 0.08f, bodyCenter.z);
            cc.skinWidth = 0.08f;
            cc.stepOffset = 0.4f;
            var box = root.AddComponent<BoxCollider>();
            box.center = bodyCenter;
            box.size = bodySize;

            var v = root.AddComponent<Vehicle>();
            v.kind = kind;
            v.displayName = info.name;
            v.topSpeed = info.topSpeed;
            v.harvestBonus = info.harvestBonus;
            v.cameraOffset = camera;
            v.paintRenderers = paintParts;

            int parts = root.GetComponentsInChildren<MeshRenderer>(true).Length;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path, out bool ok);
            if (!ok || prefab == null) { Fail($"Could not save {path}."); return null; }
            log.Append($"\nSaved {path} ({parts} parts, {paintParts.Count} take the paint, paint material {MaterialFolder}/{info.name}Paint.mat).");
            return prefab;
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    static void Place(VehicleKind kind, GameObject prefab, Transform parent, Vector3 pos, Quaternion rot, System.Text.StringBuilder log)
    {
        foreach (var existing in Object.FindObjectsByType<Vehicle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (existing.kind != kind) continue;
            log.Append($"\nThere is already a {existing.name} in the scene, so no new one was added (it uses the prefab if it is an instance of it).");
            return;
        }

        // stand it on whatever is under the spot (the parking plane or the terrain)
        pos.y = 0f;
        if (Physics.Raycast(pos + Vector3.up * 200f, Vector3.down, out var hit, 400f, ~0, QueryTriggerInteraction.Ignore)) pos.y = hit.point.y;
        else if (Terrain.activeTerrain != null) pos.y = Terrain.activeTerrain.SampleHeight(pos) + Terrain.activeTerrain.transform.position.y;

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        Undo.RegisterCreatedObjectUndo(go, UndoName);
        go.transform.SetPositionAndRotation(pos, rot);
        log.Append($"\nParked a {go.name} on {ParkingName} at X {pos.x:0.0}, Z {pos.z:0.0}, facing -X (the SubRoad).");
    }

    // ---- models (moved from VehicleFactory) ----

    // A panel van like the 2D sprite: short nose, tall cargo box with black roof ribs, mirrors sticking out.
    // The chase camera sits behind and above, so most of the detail is on the back, the roof and the rear sides.
    // Body: x -1.05..1.05, y 0.55..2.45, z -2.5 (rear face)..2.1, nose to z 2.5. Detail on a face sits 1-3 cm proud.
    static void BuildVan(Transform root, List<Renderer> paint)
    {
        paint.Add(Box(root, "Body", new Vector3(0, 1.5f, -0.2f), new Vector3(2.1f, 1.9f, 4.6f), Paint));
        paint.Add(Box(root, "Nose", new Vector3(0, 1.025f, 2.29f), new Vector3(2.06f, 0.95f, 0.42f), Paint));

        // front (cab glass, grille, lights)
        Box(root, "Windshield", new Vector3(0, 1.95f, 2.115f), new Vector3(1.85f, 0.8f, 0.04f), Glass);
        Box(root, "Side Windows", new Vector3(0, 1.95f, 1.65f), new Vector3(2.14f, 0.6f, 0.8f), Glass);
        Box(root, "Grille", new Vector3(0, 1.1f, 2.51f), new Vector3(1.0f, 0.35f, 0.03f), Dark);
        Box(root, "Front Bumper", new Vector3(0, 0.65f, 2.55f), new Vector3(2.15f, 0.3f, 0.2f), Dark);

        foreach (float s in new[] { -1f, 1f })
        {
            string n = s < 0 ? " L" : " R";
            float x = s; // +1 on the right (+X), -1 on the left

            Box(root, "Light" + n, new Vector3(0.7f * x, 1.15f, 2.51f), new Vector3(0.34f, 0.2f, 0.04f), Light);
            Box(root, "Indicator" + n, new Vector3(0.95f * x, 1.15f, 2.51f), new Vector3(0.12f, 0.2f, 0.04f), Amber);

            // mirrors stick out beside the cab; from behind you see the mirror glass
            Box(root, "Mirror Arm" + n, new Vector3(1.14f * x, 1.75f, 1.95f), new Vector3(0.2f, 0.05f, 0.06f), Dark);
            paint.Add(Box(root, "Mirror" + n, new Vector3(1.3f * x, 1.85f, 1.95f), new Vector3(0.12f, 0.34f, 0.2f), Paint));
            Box(root, "Mirror Glass" + n, new Vector3(1.3f * x, 1.85f, 1.84f), new Vector3(0.09f, 0.3f, 0.03f), Metal);

            // black plastic wheel arches (the tire sticks out past them)
            Box(root, "Arch Front" + n, new Vector3(1.06f * x, 0.76f, 1.6f), new Vector3(0.04f, 0.45f, 1.1f), Dark);
            Box(root, "Arch Back" + n, new Vector3(1.06f * x, 0.76f, -1.6f), new Vector3(0.04f, 0.45f, 1.1f), Dark);

            // sides: cab door seam, rub strip, amber side marker at the back
            Box(root, "Cab Door Seam" + n, new Vector3(1.06f * x, 1.51f, 1.2f), new Vector3(0.03f, 1.58f, 0.025f), Dark);
            Box(root, "Rub Strip" + n, new Vector3(1.06f * x, 1.25f, -0.2f), new Vector3(0.04f, 0.1f, 4.4f), Dark);
            Box(root, "Side Marker" + n, new Vector3(1.09f * x, 1.25f, -2.3f), new Vector3(0.03f, 0.06f, 0.12f), Amber);

            // roof gutter along the top edge
            Box(root, "Gutter" + n, new Vector3(1.055f * x, 2.44f, -0.205f), new Vector3(0.04f, 0.05f, 4.57f), Dark);

            // rear corner lights: dark housing, red / amber / white (reverse) sections, red wrapping round the side
            Box(root, "Tail Housing" + n, new Vector3(0.94f * x, 1.37f, -2.51f), new Vector3(0.24f, 0.9f, 0.03f), Dark);
            Box(root, "Tail Light" + n, new Vector3(0.94f * x, 1.59f, -2.54f), new Vector3(0.2f, 0.38f, 0.04f), TailRed);
            Box(root, "Tail Indicator" + n, new Vector3(0.94f * x, 1.28f, -2.54f), new Vector3(0.2f, 0.2f, 0.04f), Amber);
            Box(root, "Reverse Light" + n, new Vector3(0.94f * x, 1.06f, -2.54f), new Vector3(0.2f, 0.2f, 0.04f), Light);
            Box(root, "Tail Side" + n, new Vector3(1.065f * x, 1.59f, -2.46f), new Vector3(0.03f, 0.38f, 0.12f), TailRed);

            // twin rear doors: outer frame edge, window in rubber, handle by the centre seam, two hinges
            Box(root, "Door Edge" + n, new Vector3(0.81f * x, 1.57f, -2.51f), new Vector3(0.025f, 1.48f, 0.03f), Dark);
            Box(root, "Rear Window Seal" + n, new Vector3(0.4f * x, 1.95f, -2.505f), new Vector3(0.68f, 0.51f, 0.03f), Dark);
            Box(root, "Rear Window" + n, new Vector3(0.4f * x, 1.95f, -2.525f), new Vector3(0.62f, 0.45f, 0.03f), Glass);
            Box(root, "Door Handle" + n, new Vector3(0.1f * x, 1.35f, -2.53f), new Vector3(0.05f, 0.16f, 0.04f), Metal);
            Box(root, "Hinge Low" + n, new Vector3(0.79f * x, 1.05f, -2.53f), new Vector3(0.06f, 0.12f, 0.04f), Metal);
            Box(root, "Hinge High" + n, new Vector3(0.79f * x, 2.1f, -2.53f), new Vector3(0.06f, 0.12f, 0.04f), Metal);

            // bumper reflectors and mud flaps behind the back wheels
            Box(root, "Reflector" + n, new Vector3(0.8f * x, 0.65f, -2.66f), new Vector3(0.18f, 0.06f, 0.02f), TailRed);
            Box(root, "Mud Flap" + n, new Vector3(1.0f * x, 0.35f, -2.12f), new Vector3(0.3f, 0.42f, 0.02f), Dark);

            // roof ribs like the black lines on the 2D sprite: the middle ones start furthest forward
            Box(root, "Roof Rib Inner" + n, new Vector3(0.15f * x, 2.46f, -0.66f), new Vector3(0.05f, 0.03f, 3.32f), Dark);
            Box(root, "Roof Rib Mid" + n, new Vector3(0.47f * x, 2.46f, -0.81f), new Vector3(0.05f, 0.03f, 3.02f), Dark);
            Box(root, "Roof Rib Outer" + n, new Vector3(0.78f * x, 2.46f, -0.96f), new Vector3(0.05f, 0.03f, 2.72f), Dark);
        }

        // rear doors: centre seam, top and bottom of the door frame, pressed line below the windows
        Box(root, "Door Seam", new Vector3(0, 1.57f, -2.51f), new Vector3(0.025f, 1.48f, 0.03f), Dark);
        Box(root, "Door Top", new Vector3(0, 2.3f, -2.51f), new Vector3(1.645f, 0.025f, 0.03f), Dark);
        Box(root, "Door Bottom", new Vector3(0, 0.84f, -2.51f), new Vector3(1.645f, 0.025f, 0.03f), Dark);
        Box(root, "Door Crease", new Vector3(0, 1.55f, -2.505f), new Vector3(1.58f, 0.02f, 0.02f), Dark);

        // third brake light in a pod on the rear roof edge (the orange mark at the back of the 2D sprite)
        Box(root, "Brake Light Pod", new Vector3(0, 2.47f, -2.46f), new Vector3(0.44f, 0.08f, 0.1f), Dark);
        Box(root, "Brake Light", new Vector3(0, 2.47f, -2.52f), new Vector3(0.36f, 0.05f, 0.02f), TailRed);

        // back bumper with a step on top, number plate (frame, plate, "letters"), plate light, exhaust underneath
        Box(root, "Back Bumper", new Vector3(0, 0.65f, -2.55f), new Vector3(2.15f, 0.3f, 0.2f), Dark);
        Box(root, "Rear Step", new Vector3(0, 0.808f, -2.56f), new Vector3(0.7f, 0.025f, 0.16f), Metal);
        Box(root, "Plate Frame", new Vector3(0, 0.62f, -2.66f), new Vector3(0.56f, 0.16f, 0.02f), Metal);
        Box(root, "Number Plate", new Vector3(0, 0.62f, -2.67f), new Vector3(0.52f, 0.12f, 0.02f), Plate);
        Box(root, "Plate Letters", new Vector3(0, 0.62f, -2.684f), new Vector3(0.4f, 0.05f, 0.01f), Dark);
        Box(root, "Plate Light", new Vector3(0, 0.74f, -2.66f), new Vector3(0.14f, 0.03f, 0.02f), Light);
        Part(root, PrimitiveType.Cylinder, "Exhaust", new Vector3(0.55f, 0.4f, -2.5f), new Vector3(0.08f, 0.14f, 0.08f), Metal, Quaternion.Euler(90, 0, 0));
        Part(root, PrimitiveType.Cylinder, "Rear Axle", new Vector3(0, 0.45f, -1.6f), new Vector3(0.12f, 0.85f, 0.12f), Dark, Quaternion.Euler(0, 0, 90));

        // sliding side door on the right: back edge, top and bottom edges, the track behind it, handle
        Box(root, "Slide Door Edge", new Vector3(1.06f, 1.51f, -0.1f), new Vector3(0.03f, 1.58f, 0.025f), Dark);
        Box(root, "Slide Door Top", new Vector3(1.06f, 2.3f, 0.55f), new Vector3(0.03f, 0.025f, 1.3f), Dark);
        Box(root, "Slide Door Bottom", new Vector3(1.06f, 0.72f, 0.55f), new Vector3(0.03f, 0.025f, 1.3f), Dark);
        Box(root, "Slide Track", new Vector3(1.06f, 1.85f, -1.2f), new Vector3(0.03f, 0.04f, 2.2f), Dark);
        Box(root, "Slide Door Handle", new Vector3(1.08f, 1.5f, 1.0f), new Vector3(0.03f, 0.05f, 0.16f), Metal);

        HubCap(Wheel(root, "FlTire", new Vector3(-1.0f, 0.45f, 1.6f), 0.9f, 0.3f), 0.9f, 0.3f, 0.45f);
        HubCap(Wheel(root, "FrTire", new Vector3(1.0f, 0.45f, 1.6f), 0.9f, 0.3f), 0.9f, 0.3f, 0.45f);
        HubCap(Wheel(root, "BlTire", new Vector3(-1.0f, 0.45f, -1.6f), 0.9f, 0.3f), 0.9f, 0.3f, 0.45f);
        HubCap(Wheel(root, "BrTire", new Vector3(1.0f, 0.45f, -1.6f), 0.9f, 0.3f), 0.9f, 0.3f, 0.45f);
    }

    static void BuildHarvester(Transform root, List<Renderer> paint)
    {
        paint.Add(Part(root, PrimitiveType.Cube, "Body", new Vector3(0, 2.0f, -0.4f), new Vector3(2.6f, 2.2f, 5.2f), Paint));
        paint.Add(Part(root, PrimitiveType.Cube, "Grain Tank", new Vector3(0, 3.55f, -1.3f), new Vector3(2.4f, 0.9f, 2.6f), Paint));
        Part(root, PrimitiveType.Cube, "Cab Glass", new Vector3(0, 3.85f, 1.3f), new Vector3(1.7f, 1.4f, 1.5f), Glass);
        paint.Add(Part(root, PrimitiveType.Cube, "Cab Roof", new Vector3(0, 4.65f, 1.3f), new Vector3(1.9f, 0.25f, 1.7f), Paint));
        Part(root, PrimitiveType.Cube, "Feeder", new Vector3(0, 1.3f, 2.5f), new Vector3(1.4f, 0.8f, 1.4f), Dark);
        Part(root, PrimitiveType.Cube, "Header", new Vector3(0, 0.7f, 3.5f), new Vector3(6.0f, 0.6f, 1.4f), Metal);
        Part(root, PrimitiveType.Cylinder, "Reel", new Vector3(0, 1.35f, 3.6f), new Vector3(0.7f, 2.9f, 0.7f), Light, Quaternion.Euler(0, 0, 90));
        Part(root, PrimitiveType.Cylinder, "Unloading Pipe", new Vector3(-1.45f, 3.7f, -1.2f), new Vector3(0.3f, 1.9f, 0.3f), Metal, Quaternion.Euler(90, 0, 0));

        // big drive wheels at the front, small wheels at the back
        Wheel(root, "FlTire", new Vector3(-1.55f, 0.8f, 1.2f), 1.6f, 0.5f);
        Wheel(root, "FrTire", new Vector3(1.55f, 0.8f, 1.2f), 1.6f, 0.5f);
        Wheel(root, "BlTire", new Vector3(-1.3f, 0.5f, -2.4f), 1.0f, 0.35f);
        Wheel(root, "BrTire", new Vector3(1.3f, 0.5f, -2.4f), 1.0f, 0.35f);
    }

    static Renderer Wheel(Transform root, string name, Vector3 pos, float diameter, float width)
    {
        // a cylinder is 2 units tall along Y; lying on its side it becomes a wheel along X
        return Part(root, PrimitiveType.Cylinder, name, pos, new Vector3(diameter, width / 2f, diameter), Dark, Quaternion.Euler(0, 0, 90));
    }

    // a thin metal disc on the outside of a wheel. It is a child of the tire, so it turns with the steering.
    static void HubCap(Renderer tire, float diameter, float width, float hubDiameter)
    {
        // turned 90 degrees, the tire's local +Y points along -X: outward on the left wheel, inward on the right
        float side = tire.transform.localPosition.x < 0 ? 1f : -1f;
        float half = width / 2f, thick = 0.02f;
        Part(tire.transform, PrimitiveType.Cylinder, "Hub", new Vector3(0, side * (1f + thick / half), 0),
            new Vector3(hubDiameter / diameter, thick / half, hubDiameter / diameter), Metal);
    }

    static Renderer Box(Transform root, string name, Vector3 pos, Vector3 size, Color color) =>
        Part(root, PrimitiveType.Cube, name, pos, size, color);

    static Renderer Part(Transform root, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Color color, Quaternion? rot = null)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        // only the vehicle's controller and body box collide, not every part
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(root, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot ?? Quaternion.identity;
        go.transform.localScale = scale;
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = color == Paint ? paintMaterial : GetFixed(color);
        return r;
    }

    // ---- materials (saved as assets, so the prefabs keep them and you can edit them) ----

    static Material GetFixed(Color color)
    {
        var (name, gloss) = FixedMaterials[color];
        return GetOrCreate(name, color, gloss);
    }

    // an existing material is reused as it is (so your edits to it stay); a missing one is created
    static Material GetOrCreate(string name, Color color, float gloss)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        mat = new Material(Shader.Find("Standard")) { name = name, color = color };
        mat.SetFloat("_Glossiness", gloss);
        mat.enableInstancing = true;
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }

    static void Fail(string message)
    {
        Debug.LogError("Vehicle Prefabs: " + message);
        EditorUtility.DisplayDialog("Vehicle Prefabs", message, "OK");
    }
}
