using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Editor tool: builds the vehicle dealer in the top-left corner of the map, like the 2D game (a red
// L-shaped building on a grey concrete lot), and sets up the truck as the first Vehicle.
// Use: open GameScene, then Tools > Dealer Builder, press "Build dealer", save the scene (Ctrl+S).
// Running it again deletes the old "Dealer" object and rebuilds it. Placeholder boxes, a real model can replace them later.
public class DealerBuilder : EditorWindow
{
    const string RootName = "Dealer";
    const string MaterialFolder = "Assets/Dealer";

    // Positions come from the 2D map (backround.png, 3500 x 3000 px). The 3D terrain has the same
    // proportions turned on its side: 2D up is +X and 2D right is -Z, so the 2D top-left corner is the
    // high X, high Z corner. Rectangles are (left, top, right, bottom) in 2D pixels.
    static readonly Rect2D Lot = new(43, 59, 505, 367);   // the grey concrete pad
    static readonly Rect2D Wing = new(63, 80, 336, 146);  // the long part of the L
    static readonly Rect2D Leg = new(63, 146, 123, 211);  // the short part of the L
    const float MapWidthPx = 3500f, MapHeightPx = 3000f;

    string dealerName = "Vehicle Dealer";
    float buildingHeight = 12f;

    [MenuItem("Tools/Dealer Builder")]
    static void Open() { GetWindow<DealerBuilder>("Dealer"); }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("Builds the vehicle dealer (red L-shaped building and concrete lot) in the top-left corner of the map, " +
            "the same place as in the 2D game, and makes the truck the first vehicle. Run it with GameScene open, then save the scene.", MessageType.Info);
        dealerName = EditorGUILayout.TextField("Dealer name", dealerName);
        buildingHeight = EditorGUILayout.FloatField("Building height (m)", buildingHeight);

        EditorGUILayout.Space();
        if (GUILayout.Button("Build dealer")) Build();
        if (GUILayout.Button("Remove dealer"))
        {
            var old = FindOld();
            if (old != null) Undo.DestroyObjectImmediate(old);
        }
    }

    struct Rect2D
    {
        public float left, top, right, bottom;
        public Rect2D(float l, float t, float r, float b) { left = l; top = t; right = r; bottom = b; }
    }

    static GameObject FindOld()
    {
        var buildings = GameObject.Find("Buildings");
        var t = buildings != null ? buildings.transform.Find(RootName) : null;
        if (t != null) return t.gameObject;
        var any = GameObject.Find(RootName);
        return any != null && any.GetComponent<Dealer>() != null ? any : null;
    }

    void Build()
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) { EditorUtility.DisplayDialog("Dealer", "No terrain found in the open scene. Open GameScene first.", "OK"); return; }
        var input = FindFirstObjectByType<InputManager>();
        var values = FindFirstObjectByType<PlayerValues>();
        var player = FindFirstObjectByType<GameCharacter>();
        if (input == null || values == null || player == null)
        {
            EditorUtility.DisplayDialog("Dealer", "Could not find the InputManager, PlayerValues or GameCharacter in the open scene.", "OK");
            return;
        }

        var old = FindOld();
        if (old != null) Undo.DestroyObjectImmediate(old);

        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;

        // 2D rectangle -> world centre and size (X from the 2D vertical, Z from the 2D horizontal)
        (Vector3 center, Vector3 extent) ToWorld(Rect2D r)
        {
            float x1 = origin.x + size.x * (1f - r.bottom / MapHeightPx), x2 = origin.x + size.x * (1f - r.top / MapHeightPx);
            float z1 = origin.z + size.z * (1f - r.right / MapWidthPx), z2 = origin.z + size.z * (1f - r.left / MapWidthPx);
            return (new Vector3((x1 + x2) / 2f, 0, (z1 + z2) / 2f), new Vector3(x2 - x1, 0, z2 - z1));
        }

        var (lotC, lotS) = ToWorld(Lot);
        var (wingC, wingS) = ToWorld(Wing);
        var (legC, legS) = ToWorld(Leg);

        // the highest ground under the lot, so the slab never sinks into a bump
        float ground = float.MinValue;
        for (int i = 0; i <= 4; i++)
            for (int k = 0; k <= 4; k++)
            {
                var p = lotC + new Vector3((i / 4f - 0.5f) * lotS.x, 0, (k / 4f - 0.5f) * lotS.z);
                ground = Mathf.Max(ground, terrain.SampleHeight(p) + origin.y);
            }
        float floor = ground + 0.05f; // top of the concrete

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Build dealer");
        var buildings = GameObject.Find("Buildings");
        if (buildings != null) root.transform.SetParent(buildings.transform, false);
        root.transform.position = new Vector3(lotC.x, floor, lotC.z);

        var red = GetMaterial("DealerRed", new Color(0.85f, 0.1f, 0.12f), 0.2f);
        var roof = GetMaterial("DealerRoof", new Color(0.55f, 0.06f, 0.08f), 0.1f);
        var concrete = GetMaterial("DealerLot", new Color(0.76f, 0.76f, 0.74f), 0.05f);
        var glass = GetMaterial("DealerGlass", new Color(0.14f, 0.22f, 0.3f), 0.85f);
        var white = GetMaterial("DealerSign", new Color(0.95f, 0.95f, 0.95f), 0.2f);

        // concrete lot: a 1 m slab whose top sits just above the ground (and above the road that ends here)
        Box(root.transform, "Lot", new Vector3(lotC.x, floor - 0.5f, lotC.z), new Vector3(lotS.x, 1f, lotS.z), concrete);

        // the L: a long wing along the top of the lot and a short leg at its far end
        float h = buildingHeight;
        Box(root.transform, "Building", new Vector3(wingC.x, floor + h / 2f, wingC.z), new Vector3(wingS.x, h, wingS.z), red);
        Box(root.transform, "Building Leg", new Vector3(legC.x, floor + h / 2f, legC.z), new Vector3(legS.x, h, legS.z), red);
        Box(root.transform, "Roof", new Vector3(wingC.x, floor + h + 0.25f, wingC.z), new Vector3(wingS.x + 0.6f, 0.5f, wingS.z + 0.6f), roof, collider: false);
        Box(root.transform, "Roof Leg", new Vector3(legC.x, floor + h + 0.25f, legC.z), new Vector3(legS.x + 0.6f, 0.5f, legS.z + 0.6f), roof, collider: false);

        // shop front on the side facing the open lot (-X), between the road end and the leg
        float front = wingC.x - wingS.x / 2f;
        float openStart = wingC.z - wingS.z / 2f;          // the end of the wing nearest the road
        float openEnd = legC.z - legS.z / 2f;               // where the leg starts
        float shopZ = (openStart + openEnd) / 2f;
        float shopLen = openEnd - openStart;
        Box(root.transform, "Windows", new Vector3(front - 0.1f, floor + 3.2f, shopZ - shopLen * 0.12f), new Vector3(0.3f, 4.4f, shopLen * 0.6f), glass, collider: false);
        Box(root.transform, "Door", new Vector3(front - 0.12f, floor + 2.5f, shopZ + shopLen * 0.3f), new Vector3(0.35f, 5f, 6f), glass, collider: false);
        Box(root.transform, "Sign", new Vector3(front - 0.15f, floor + h - 2.6f, shopZ), new Vector3(0.3f, 3.2f, shopLen * 0.7f), white, collider: false);

        var textGo = new GameObject("Sign Text");
        textGo.transform.SetParent(root.transform, false);
        var tmp = textGo.AddComponent<TextMeshPro>();
        tmp.text = dealerName.ToUpper();
        tmp.color = new Color(0.75f, 0.08f, 0.1f);
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 1f;
        tmp.fontSizeMax = 60f;
        tmp.rectTransform.sizeDelta = new Vector2(shopLen * 0.66f, 2.8f);
        // TextMeshPro reads from its -Z side; turned 90 degrees it faces -X, toward the lot
        textGo.transform.SetPositionAndRotation(new Vector3(front - 0.32f, floor + h - 2.6f, shopZ), Quaternion.Euler(0, 90, 0));

        // the whole lot is the shop zone (like the 2D pad): press E / USE anywhere on it
        var zone = new GameObject("Triggerbox");
        zone.transform.SetParent(root.transform, false);
        zone.transform.position = new Vector3(lotC.x, floor + 4f, lotC.z);
        var trigger = zone.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(lotS.x, 8f, lotS.z);

        var dealer = root.AddComponent<Dealer>();
        dealer.dealerName = dealerName;
        dealer.playerValues = values;
        dealer.inputManager = input;
        dealer.player = player;

        var building = zone.AddComponent<Building>();
        building.type = Building.BuildingType.Dealer;
        building.inputManager = input;
        building.playerValues = values;
        building.dealer = dealer;

        // parking spots in a row across the open lot, facing the road end (-Z). The first is nearest the road.
        const int spots = 5;
        const float spacing = 12f;
        float rowZ = openStart - 14f;
        dealer.parkingSpots = new Transform[spots];
        for (int i = 0; i < spots; i++)
        {
            var spot = new GameObject("Parking " + (i + 1)).transform;
            spot.SetParent(root.transform, false);
            spot.SetPositionAndRotation(new Vector3(front - 10f - i * spacing, floor, rowZ), Quaternion.Euler(0, 180, 0));
            dealer.parkingSpots[i] = spot;
        }

        foreach (var t in root.GetComponentsInChildren<Transform>())
            if (t.GetComponent<Building>() == null && t.GetComponent<Dealer>() == null && t.parent == root.transform && !t.name.StartsWith("Parking"))
                t.gameObject.isStatic = true;

        SetUpTruck(player);

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log($"Dealer built at X {lotC.x:0}, Z {lotC.z:0} (lot {lotS.x:0} x {lotS.z:0} m). Save the scene (Ctrl+S).", root);
    }

    // the truck is the starting vehicle: give it a Vehicle with its tuned driving values and its camera position
    static void SetUpTruck(GameCharacter player)
    {
        var go = player.gameObject;
        var vehicle = go.GetComponent<Vehicle>();
        if (vehicle == null) vehicle = Undo.AddComponent<Vehicle>(go);
        else Undo.RecordObject(vehicle, "Set up truck");

        vehicle.kind = VehicleKind.Truck;
        vehicle.displayName = "Truck";
        if (player.characterCam != null && player.characterCam.parent == go.transform)
            vehicle.cameraOffset = player.characterCam.localPosition;

        PrefabUtility.RecordPrefabInstancePropertyModifications(vehicle);
        EditorUtility.SetDirty(vehicle);
    }

    static GameObject Box(Transform parent, string name, Vector3 position, Vector3 scale, Material mat, bool collider = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        if (!collider) DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static Material GetMaterial(string name, Color color, float gloss)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets", "Dealer");
        mat = new Material(Shader.Find("Standard")) { color = color };
        mat.SetFloat("_Glossiness", gloss);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
