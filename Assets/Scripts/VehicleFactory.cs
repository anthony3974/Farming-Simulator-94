using System.Collections.Generic;
using UnityEngine;

/// <summary>Prices and stats of every vehicle, and the four paint colours.</summary>
public static class VehicleCatalog
{
    public struct Info
    {
        public VehicleKind kind;
        public string name;
        public int price;
        public float topSpeed;     // km/h on the speedometer (the tractor is 50)
        public float harvestBonus; // 0.6 = 60% more harvest
        public string description;
    }

    // 2D game: the van and the harvester cost 5,000 each. Speeds keep the 2D ratios
    // (tractor 8, van 10, harvester 6), so the van is 25% faster and the harvester 25% slower.
    private static readonly Info[] All =
    {
        new() { kind = VehicleKind.Tractor, name = "Tractor", price = 0, topSpeed = 50, description = "Your starting vehicle." },
        new() { kind = VehicleKind.Van, name = "Van", price = 5000, topSpeed = 62, description = "The fastest on the road (top speed 62 km/h, the tractor does 50)." },
        new() { kind = VehicleKind.Harvester, name = "Harvester", price = 5000, topSpeed = 38, harvestBonus = 0.6f, description = "Harvests 60% more from every field, but is slow (38 km/h)." },
    };

    public static Info Get(VehicleKind kind) => All[(int)kind];

    /// <summary>The vehicles sold at the dealer (everything but the starting tractor).</summary>
    public static IEnumerable<Info> ForSale
    {
        get { foreach (var info in All) if (info.price > 0) yield return info; }
    }

    // the 2D game's four colours
    public static readonly (string name, Color color)[] Colors =
    {
        ("Red", new Color32(200, 35, 30, 255)),
        ("Yellow", new Color32(235, 185, 20, 255)),
        ("Blue", new Color32(35, 85, 200, 255)),
        ("Green", new Color32(45, 150, 55, 255)),
    };
}

/// <summary>
/// Builds a drivable vehicle. There are no models for the van and the harvester yet, so they are
/// placeholders made of boxes and cylinders (like the tractor). Forward is +Z, the pivot is on the ground.
/// </summary>
public static class VehicleFactory
{
    private static readonly Color Dark = new Color32(35, 35, 40, 255);
    private static readonly Color Glass = new Color32(40, 60, 80, 255);
    private static readonly Color Metal = new Color32(120, 120, 125, 255);
    private static readonly Color Light = new Color32(255, 245, 200, 255);

    private static readonly Dictionary<Color, Material> materials = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => materials.Clear();

    public static Vehicle Build(VehicleKind kind, Color paint, Vector3 position, Quaternion rotation)
    {
        var info = VehicleCatalog.Get(kind);
        var root = new GameObject(info.name);
        root.transform.SetPositionAndRotation(position, rotation);
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
        v.SetColor(paint);
        return v;
    }

    // ---- placeholder models ----

    private static void BuildVan(Transform root, List<Renderer> paint)
    {
        paint.Add(Part(root, PrimitiveType.Cube, "Body", new Vector3(0, 1.5f, 0), new Vector3(2.1f, 1.9f, 5.0f), Color.white));
        Part(root, PrimitiveType.Cube, "Windshield", new Vector3(0, 1.95f, 2.48f), new Vector3(1.85f, 0.75f, 0.1f), Glass);
        Part(root, PrimitiveType.Cube, "Side Windows", new Vector3(0, 1.95f, 1.55f), new Vector3(2.14f, 0.6f, 1.3f), Glass);
        Part(root, PrimitiveType.Cube, "Front Bumper", new Vector3(0, 0.65f, 2.55f), new Vector3(2.15f, 0.3f, 0.2f), Dark);
        Part(root, PrimitiveType.Cube, "Back Bumper", new Vector3(0, 0.65f, -2.55f), new Vector3(2.15f, 0.3f, 0.2f), Dark);
        Part(root, PrimitiveType.Cube, "Light L", new Vector3(-0.7f, 1.05f, 2.52f), new Vector3(0.4f, 0.2f, 0.06f), Light);
        Part(root, PrimitiveType.Cube, "Light R", new Vector3(0.7f, 1.05f, 2.52f), new Vector3(0.4f, 0.2f, 0.06f), Light);

        Wheel(root, "FlTire", new Vector3(-1.0f, 0.45f, 1.6f), 0.9f, 0.3f);
        Wheel(root, "FrTire", new Vector3(1.0f, 0.45f, 1.6f), 0.9f, 0.3f);
        Wheel(root, "BlTire", new Vector3(-1.0f, 0.45f, -1.6f), 0.9f, 0.3f);
        Wheel(root, "BrTire", new Vector3(1.0f, 0.45f, -1.6f), 0.9f, 0.3f);
    }

    private static void BuildHarvester(Transform root, List<Renderer> paint)
    {
        paint.Add(Part(root, PrimitiveType.Cube, "Body", new Vector3(0, 2.0f, -0.4f), new Vector3(2.6f, 2.2f, 5.2f), Color.white));
        paint.Add(Part(root, PrimitiveType.Cube, "Grain Tank", new Vector3(0, 3.55f, -1.3f), new Vector3(2.4f, 0.9f, 2.6f), Color.white));
        Part(root, PrimitiveType.Cube, "Cab Glass", new Vector3(0, 3.85f, 1.3f), new Vector3(1.7f, 1.4f, 1.5f), Glass);
        paint.Add(Part(root, PrimitiveType.Cube, "Cab Roof", new Vector3(0, 4.65f, 1.3f), new Vector3(1.9f, 0.25f, 1.7f), Color.white));
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

    private static void Wheel(Transform root, string name, Vector3 pos, float diameter, float width)
    {
        // a cylinder is 2 units tall along Y; lying on its side it becomes a wheel along X
        Part(root, PrimitiveType.Cylinder, name, pos, new Vector3(diameter, width / 2f, diameter), Dark, Quaternion.Euler(0, 0, 90));
    }

    private static Renderer Part(Transform root, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Color color, Quaternion? rot = null)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        // only the vehicle's controller and body box collide, not every part
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(root, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot ?? Quaternion.identity;
        go.transform.localScale = scale;
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = GetMaterial(r.sharedMaterial, color);
        return r;
    }

    // one shared material per colour, copied from the primitive's default material (so it works in builds)
    private static Material GetMaterial(Material baseMaterial, Color color)
    {
        if (!materials.TryGetValue(color, out var mat) || mat == null)
        {
            mat = new Material(baseMaterial) { color = color };
            materials[color] = mat;
        }
        return mat;
    }
}
