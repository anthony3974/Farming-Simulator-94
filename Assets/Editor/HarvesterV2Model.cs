using UnityEngine;

// The parts of the redesigned harvester ("Model V2"), used by HarvesterV2Builder. Kept apart from the editor
// window so the shape is easy to read and change. Only plain Unity primitives, no colliders except two boxes.
//
// Space: forward is +Z, ground is local y = 0, right is +X. The comments give each part's span (y, z or x)
// to show which part it overlaps: like Tractor V2, nothing floats. Every part reaches a few cm into the part
// it hangs on, and the chain always ends at the body.
//
// Layout, front to back: header (cutting table with the reel and auger, z +2.9..+5.2), feeder house (the
// sloped neck from the header up into the body), big drive wheels under the cab, grain tank behind the cab,
// engine at the back on top, small steering wheels at the back (a combine steers with the back wheels).
// The unloading auger lies folded along the left side of the tank, the ladder goes up the left side.
public class HarvesterMaterials
{
    public Material paint, dark, metal, rubber, hub, glass, light, amber, tailRed, grain;
}

public static class HarvesterV2Model
{
    public const string ModelName = "Model V2";
    public const string PaintPrefix = "Paint ";   // parts that take the vehicle colour (Vehicle.paintRenderers)

    public static Transform Build(Transform root, HarvesterMaterials m)
    {
        var model = Group(root, ModelName);
        BuildBody(Group(model, "Body"), m);
        BuildWheels(Group(model, "Wheels"), m);
        BuildCab(Group(model, "Cab"), m);
        BuildGrainTank(Group(model, "Grain Tank"), m);
        BuildUnloadingAuger(Group(model, "Unloading Auger"), m);
        BuildLadder(Group(model, "Ladder"), m);
        BuildEngine(Group(model, "Engine"), m);
        BuildHeader(Group(model, "Header"), m);
        return model;
    }

    // ---- body, feeder house, rear ----

    static void BuildBody(Transform g, HarvesterMaterials m)
    {
        // Main body: x ±1.25, y 0.9..3.1, z -2.6..+2.0. Everything else hangs on it.
        Box(g, PaintPrefix + "Body", new Vector3(0, 2.0f, -0.3f), new Vector3(2.5f, 2.2f, 4.6f), m.paint);
        // Chassis under it (y 0.6..1.0, z -2.5..+1.9), holds both axles
        Box(g, "Chassis", new Vector3(0, 0.8f, -0.3f), new Vector3(2.3f, 0.4f, 4.4f), m.dark);

        // Feeder house: the sloped neck from the header back wall (z 3.1) up into the body front (z 2.0).
        // Tilted 12 degrees (front end down): its back end sits inside the body (z 1.72, y 0.88..1.83),
        // its front end inside the header (z 3.09..3.28, y 0.58..1.51).
        Box(g, PaintPrefix + "Feeder House", new Vector3(0, 1.2f, 2.45f), new Vector3(1.4f, 0.95f, 1.5f), m.paint, new Vector3(12, 0, 0));

        // Front of the body, above the feeder: two headlights set into the face (z 1.97..2.03)
        Box(g, "Head Light L", new Vector3(-0.85f, 2.6f, 2.0f), new Vector3(0.35f, 0.18f, 0.06f), m.light);
        Box(g, "Head Light R", new Vector3(0.85f, 2.6f, 2.0f), new Vector3(0.35f, 0.18f, 0.06f), m.light);

        // Sides: a dark stripe along the body and a vent grille at the back, both set into the side (x ±1.24..1.28)
        foreach (float side in new[] { -1f, 1f })
        {
            string s = side < 0 ? " L" : " R";
            Box(g, "Stripe" + s, new Vector3(side * 1.26f, 2.75f, -0.3f), new Vector3(0.04f, 0.12f, 4.4f), m.dark);
            Box(g, "Side Vent" + s, new Vector3(side * 1.26f, 2.2f, -1.55f), new Vector3(0.04f, 0.7f, 1.0f), m.dark);
        }

        // Rear: radiator screen on the back face (z -2.65..-2.59), straw spreader under it (z -3.05..-2.55,
        // y 0.83..1.28, into the body bottom and back), tail lights and indicators beside the screen
        Box(g, "Radiator Screen", new Vector3(0, 2.1f, -2.62f), new Vector3(2.1f, 1.7f, 0.06f), m.dark);
        // louvres across the screen (z -2.68..-2.64, into its back face at -2.65)
        for (int i = 0; i < 6; i++)
            Box(g, "Louvre", new Vector3(0, 1.45f + i * 0.26f, -2.66f), new Vector3(2.0f, 0.06f, 0.04f), m.metal);
        Box(g, "Straw Spreader", new Vector3(0, 1.05f, -2.8f), new Vector3(1.8f, 0.45f, 0.5f), m.dark);
        foreach (float side in new[] { -1f, 1f })
        {
            string s = side < 0 ? " L" : " R";
            Box(g, "Tail Light" + s, new Vector3(side * 1.12f, 2.85f, -2.62f), new Vector3(0.25f, 0.15f, 0.06f), m.tailRed);
            Box(g, "Indicator" + s, new Vector3(side * 1.12f, 2.6f, -2.62f), new Vector3(0.25f, 0.12f, 0.06f), m.amber);
        }
    }

    // ---- wheels, axles, fenders ----

    static void BuildWheels(Transform g, HarvesterMaterials m)
    {
        // Front axle through the chassis (y 0.75..1.05) and 0.45 into each drive wheel (wheels from |x| 1.25)
        Box(g, "Front Axle", new Vector3(0, 0.9f, 1.1f), new Vector3(3.4f, 0.3f, 0.3f), m.dark);
        // Big drive wheels under the cab, on the ground (centre y = radius). They do not steer.
        Wheel(g, "FlTire", new Vector3(-1.6f, 0.9f, 1.1f), 1.8f, 0.7f, m.rubber, m.hub, 1.0f);
        Wheel(g, "FrTire", new Vector3(1.6f, 0.9f, 1.1f), 1.8f, 0.7f, m.rubber, m.hub, 1.0f);

        // Fenders over the drive wheels (wheel top 1.8): the plate (y 1.85..1.95, x 1.2..2.05) runs into the
        // body side (1.25); skirts hang from its front and back ends (y 1.37..1.87), clear of the tire
        foreach (float side in new[] { -1f, 1f })
        {
            string s = side < 0 ? " L" : " R";
            Box(g, PaintPrefix + "Fender" + s, new Vector3(side * 1.62f, 1.9f, 1.1f), new Vector3(0.85f, 0.1f, 1.9f), m.paint);
            Box(g, PaintPrefix + "Fender Front" + s, new Vector3(side * 1.62f, 1.62f, 2.05f), new Vector3(0.85f, 0.5f, 0.08f), m.paint);
            Box(g, PaintPrefix + "Fender Back" + s, new Vector3(side * 1.62f, 1.62f, 0.15f), new Vector3(0.85f, 0.5f, 0.08f), m.paint);
        }

        // Rear steering axle: a mount from the chassis (y 0.53..0.98, chassis bottom 0.6) holds the beam
        // (y 0.45..0.65), which runs 0.125 into each back wheel (from |x| 1.475), so it stays hidden when they steer.
        // The wheels sit outside the body (inner edge 1.3 at full lock, body side 1.25), so they never cut into it.
        Box(g, "Rear Axle Mount", new Vector3(0, 0.75f, -1.9f), new Vector3(0.6f, 0.45f, 0.5f), m.dark);
        Box(g, "Rear Axle", new Vector3(0, 0.55f, -1.9f), new Vector3(3.2f, 0.2f, 0.2f), m.dark);
        // BlTire / BrTire: Vehicle steers these when Steer Rear Wheels is on
        Wheel(g, "BlTire", new Vector3(-1.7f, 0.55f, -1.9f), 1.1f, 0.45f, m.rubber, m.hub, 0.6f);
        Wheel(g, "BrTire", new Vector3(1.7f, 0.55f, -1.9f), 1.1f, 0.45f, m.rubber, m.hub, 0.6f);
    }

    // ---- cab ----

    static void BuildCab(Transform g, HarvesterMaterials m)
    {
        // Cab floor on the body top (y 3.05..3.35, body top 3.1), x ±1.15, z 0.2..2.2 (a little past the body front)
        Box(g, PaintPrefix + "Cab Floor", new Vector3(0, 3.2f, 1.2f), new Vector3(2.3f, 0.3f, 2.0f), m.paint);
        // Glass on the floor (y 3.3..4.8), x ±1.0, z 0.4..2.1
        Box(g, "Cab Glass", new Vector3(0, 4.05f, 1.25f), new Vector3(2.0f, 1.5f, 1.7f), m.glass);
        // corner posts on the glass edges, from inside the floor (3.25) into the roof (4.85)
        foreach (float x in new[] { -1.0f, 1.0f })
            foreach (float z in new[] { 0.4f, 2.1f })
                Box(g, "Post", new Vector3(x, 4.05f, z), new Vector3(0.12f, 1.6f, 0.12f), m.dark);
        // roof on the posts and glass (y 4.775..4.925), a little overhang
        Box(g, PaintPrefix + "Cab Roof", new Vector3(0, 4.85f, 1.25f), new Vector3(2.3f, 0.15f, 2.05f), m.paint);
        // work lights set into the roof front edge (z 2.26..2.34, roof front 2.275)
        foreach (float x in new[] { -0.8f, -0.3f, 0.3f, 0.8f })
            Box(g, "Work Light", new Vector3(x, 4.85f, 2.3f), new Vector3(0.25f, 0.12f, 0.08f), m.light);
        // beacon sitting in the roof (y 4.91..5.09)
        Box(g, "Beacon", new Vector3(0.85f, 5.0f, 0.45f), new Vector3(0.18f, 0.18f, 0.18f), m.amber);

        // mirrors: an arm out of each front post (x 1.0..1.5), the mirror on its end
        foreach (float side in new[] { -1f, 1f })
        {
            string s = side < 0 ? " L" : " R";
            Box(g, "Mirror Arm" + s, new Vector3(side * 1.25f, 4.6f, 2.1f), new Vector3(0.5f, 0.05f, 0.05f), m.dark);
            Box(g, "Mirror" + s, new Vector3(side * 1.5f, 4.6f, 2.1f), new Vector3(0.05f, 0.35f, 0.22f), m.dark);
        }
    }

    // ---- grain tank ----

    static void BuildGrainTank(Transform g, HarvesterMaterials m)
    {
        // Tank on the body top behind the cab (y 3.05..4.15), a little wider than the body (x ±1.35),
        // z -1.9..+0.25: its front reaches into the cab floor (from 0.2)
        Box(g, PaintPrefix + "Tank", new Vector3(0, 3.6f, -0.825f), new Vector3(2.7f, 1.1f, 2.15f), m.paint);
        // rim walls around the top (y 4.1..4.35, into the tank top), and the grain heaped inside them
        Box(g, "Tank Rim Front", new Vector3(0, 4.225f, 0.26f), new Vector3(2.84f, 0.25f, 0.08f), m.metal);
        Box(g, "Tank Rim Back", new Vector3(0, 4.225f, -1.91f), new Vector3(2.84f, 0.25f, 0.08f), m.metal);
        Box(g, "Tank Rim L", new Vector3(-1.38f, 4.225f, -0.825f), new Vector3(0.08f, 0.25f, 2.25f), m.metal);
        Box(g, "Tank Rim R", new Vector3(1.38f, 4.225f, -0.825f), new Vector3(0.08f, 0.25f, 2.25f), m.metal);
        Box(g, "Grain", new Vector3(0, 4.25f, -0.825f), new Vector3(2.72f, 0.1f, 2.15f), m.grain);
    }

    // ---- unloading auger (folded along the left side) ----

    static void BuildUnloadingAuger(Transform g, HarvesterMaterials m)
    {
        // Upright pipe at the back-left corner of the tank, from the body side (x -1.63..-1.23, body side -1.25)
        // up past the tank top (y 2.9..4.3)
        Cyl(g, PaintPrefix + "Auger Upright", new Vector3(-1.43f, 3.6f, -1.7f), 0.4f, 1.4f, m.paint);
        // elbow on top, then the long pipe forward along the tank side (into the tank side at -1.35),
        // past the cab to the spout (z -1.75..+2.55)
        Sphere(g, PaintPrefix + "Auger Elbow", new Vector3(-1.43f, 4.2f, -1.7f), 0.45f, m.paint);
        CylZ(g, PaintPrefix + "Auger Pipe", new Vector3(-1.43f, 4.2f, 0.4f), 0.35f, 4.3f, m.paint);
        // spout hanging from the front end (y 3.7..4.2)
        Cyl(g, "Auger Spout", new Vector3(-1.43f, 3.95f, 2.45f), 0.3f, 0.5f, m.metal);
        // rest: a bracket from the front-left cab post (x -1.0) out under the pipe (pipe bottom 4.025)
        Box(g, "Auger Rest", new Vector3(-1.25f, 4.0f, 2.1f), new Vector3(0.5f, 0.08f, 0.08f), m.dark);
    }

    // ---- ladder and walkway (left side) ----

    static void BuildLadder(Transform g, HarvesterMaterials m)
    {
        // Walkway beside the cab, on the cab floor's left edge (x -1.75..-1.15, y 3.21..3.29), above the left fender
        Box(g, "Walkway", new Vector3(-1.45f, 3.25f, 1.2f), new Vector3(0.6f, 0.08f, 1.95f), m.dark);
        // Ladder in front of the left drive wheel, against the fender's front skirt (z 2.09..2.15).
        // The rails hang from the walkway and go on up as hand grips (y 0.4..4.0).
        foreach (float x in new[] { -1.35f, -1.7f })
            Box(g, "Ladder Rail", new Vector3(x, 2.2f, 2.12f), new Vector3(0.06f, 3.6f, 0.06f), m.dark);
        for (int i = 0; i < 8; i++)
            Box(g, "Ladder Step", new Vector3(-1.525f, 0.6f + i * 0.36f, 2.12f), new Vector3(0.4f, 0.05f, 0.05f), m.metal);
        // hand rail along the walkway's outer edge: a back post and a top rail into the outer ladder rail
        Box(g, "Hand Rail Post", new Vector3(-1.7f, 3.64f, 0.35f), new Vector3(0.06f, 0.8f, 0.06f), m.dark);
        Box(g, "Hand Rail", new Vector3(-1.7f, 4.0f, 1.235f), new Vector3(0.06f, 0.06f, 1.83f), m.dark);
    }

    // ---- engine (back, on top) ----

    static void BuildEngine(Transform g, HarvesterMaterials m)
    {
        // Engine cover on the body top behind the tank (y 3.075..3.625, z -2.575..-1.825, into the tank back at -1.9)
        Box(g, PaintPrefix + "Engine Cover", new Vector3(0, 3.35f, -2.2f), new Vector3(2.2f, 0.55f, 0.75f), m.paint);
        // grille set into its top (y 3.62..3.66)
        Box(g, "Engine Grille", new Vector3(0, 3.64f, -2.2f), new Vector3(1.8f, 0.04f, 0.55f), m.dark);
        // exhaust standing in the cover (y 3.6..4.6) with a wider tip, and the round air intake beside it
        Cyl(g, "Exhaust", new Vector3(0.7f, 4.1f, -2.2f), 0.14f, 1.0f, m.dark);
        Cyl(g, "Exhaust Tip", new Vector3(0.7f, 4.6f, -2.2f), 0.18f, 0.08f, m.dark);
        Cyl(g, "Air Intake", new Vector3(-0.55f, 3.85f, -2.3f), 0.32f, 0.5f, m.dark);
        Cyl(g, "Air Intake Cap", new Vector3(-0.55f, 4.1f, -2.3f), 0.38f, 0.06f, m.metal);
    }

    // ---- header (cutting table, auger, reel) ----

    static void BuildHeader(Transform g, HarvesterMaterials m)
    {
        // Back wall (x ±3.0, y 0.275..1.425, z 2.9..3.1): the feeder house runs into it
        Box(g, PaintPrefix + "Header Back", new Vector3(0, 0.85f, 3.0f), new Vector3(6.0f, 1.15f, 0.2f), m.paint);
        // table floor in front of it (y 0.26..0.38, z 3.05..4.55, x ±2.92, into the wall and the end plates)
        Box(g, "Header Floor", new Vector3(0, 0.32f, 3.8f), new Vector3(5.84f, 0.12f, 1.5f), m.metal);
        // cutter bar along the front edge (z 4.525..4.675)
        Box(g, "Cutter Bar", new Vector3(0, 0.3f, 4.6f), new Vector3(5.84f, 0.08f, 0.15f), m.dark);

        foreach (float side in new[] { -1f, 1f })
        {
            string s = side < 0 ? " L" : " R";
            // end plates (x ±2.89..3.01, y 0.275..1.125, z 2.95..4.55), into the wall and the floor
            Box(g, PaintPrefix + "Header End" + s, new Vector3(side * 2.95f, 0.7f, 3.75f), new Vector3(0.12f, 0.85f, 1.6f), m.paint);
            // crop divider: a point in front of each end plate. The top bar slopes from the plate's top front
            // corner (y 1.125, z 4.55) down to the tip (y 0.3, z 5.15); the bottom bar runs from the floor to the tip.
            Box(g, PaintPrefix + "Divider Top" + s, new Vector3(side * 2.95f, 0.71f, 4.85f), new Vector3(0.12f, 0.12f, 1.1f), m.paint, new Vector3(54, 0, 0));
            Box(g, PaintPrefix + "Divider Bottom" + s, new Vector3(side * 2.95f, 0.32f, 4.85f), new Vector3(0.12f, 0.12f, 0.7f), m.paint);
        }

        // Cross auger on the floor (y 0.37..0.87, z 3.25..3.75), end to end between the plates (x ±2.9).
        // The flights are discs leaning in towards the middle, where the crop goes up the feeder house.
        CylX(g, "Header Auger", new Vector3(0, 0.62f, 3.5f), 0.5f, 5.8f, m.metal);
        foreach (float side in new[] { -1f, 1f })
            foreach (float x in new[] { 0.9f, 1.3f, 1.7f, 2.1f, 2.5f })
                Part(g, PrimitiveType.Cylinder, "Auger Flight", new Vector3(side * x, 0.62f, 3.5f),
                    new Vector3(0.7f, 0.02f, 0.7f), m.metal, Quaternion.Euler(0, side * 20f, 90));

        // Reel arms from the wall top (y 1.38, z 3.0) forward and up to the reel axle ends (y 1.7, z 3.95)
        foreach (float side in new[] { -1f, 1f })
            Box(g, PaintPrefix + "Reel Arm" + (side < 0 ? " L" : " R"), new Vector3(side * 2.75f, 1.54f, 3.475f),
                new Vector3(0.1f, 0.12f, 1.15f), m.paint, new Vector3(-18.6f, 0, 0));

        // The reel: its own object at the axle, so it can be spun later. Six bats on a 0.55 m radius
        // (y 1.15..2.25, z 3.4..4.5), held by three spiders whose spokes run through the axle into the bats.
        var reel = Group(g, "Reel");
        reel.localPosition = new Vector3(0, 1.7f, 3.95f);
        CylX(reel, "Reel Axle", Vector3.zero, 0.14f, 5.6f, m.metal);
        const float r = 0.55f;
        for (int k = 0; k < 6; k++)
        {
            float a = k * 60f;
            float rad = a * Mathf.Deg2Rad;
            Box(reel, "Reel Bat", new Vector3(0, r * Mathf.Sin(rad), r * Mathf.Cos(rad)), new Vector3(5.3f, 0.07f, 0.1f), m.hub, new Vector3(-a, 0, 0));
        }
        foreach (float x in new[] { -2.3f, 0f, 2.3f })
            for (int k = 0; k < 3; k++)
                Box(reel, "Reel Spoke", new Vector3(x, 0, 0), new Vector3(0.06f, 0.06f, 2 * r + 0.04f), m.dark, new Vector3(-k * 60f, 0, 0));

        // makes the parked header solid (the harvester's own controller ignores it); the body is covered by
        // the box collider on the Harvester itself
        var box = g.gameObject.AddComponent<BoxCollider>();
        box.center = new Vector3(0, 0.85f, 4.0f);
        box.size = new Vector3(6.0f, 1.3f, 2.3f);
    }

    // ---- primitives ----

    static Transform Group(Transform parent, string name)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }

    // A cylinder on its side (turned 90 degrees so its local Y points along -X) with a hub cap pressed into the
    // outer face: the cap reaches 0.01 into the tire and sticks out 0.03. Same as Tractor V2.
    static void Wheel(Transform parent, string name, Vector3 pos, float diameter, float width, Material tire, Material hub, float hubDiameter)
    {
        var t = Part(parent, PrimitiveType.Cylinder, name, pos, new Vector3(diameter, width / 2f, diameter), tire, Quaternion.Euler(0, 0, 90));
        float side = pos.x < 0 ? 1f : -1f;   // outward: local +Y on the left, local -Y on the right
        float half = width / 2f;              // local units per metre along the axle = 1 / half
        var cap = Part(t, PrimitiveType.Cylinder, "Hub", Vector3.zero, Vector3.one, hub);
        cap.localPosition = new Vector3(0, side * (1f + 0.01f / half), 0);
        cap.localScale = new Vector3(hubDiameter / diameter, 0.02f / half, hubDiameter / diameter);
    }

    static Transform Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, Vector3? euler = null) =>
        Part(parent, PrimitiveType.Cube, name, pos, size, mat, euler.HasValue ? Quaternion.Euler(euler.Value) : (Quaternion?)null);

    // a standing cylinder
    static Transform Cyl(Transform parent, string name, Vector3 pos, float diameter, float height, Material mat) =>
        Part(parent, PrimitiveType.Cylinder, name, pos, new Vector3(diameter, height / 2f, diameter), mat);

    // a cylinder lying along X (left to right)
    static Transform CylX(Transform parent, string name, Vector3 pos, float diameter, float length, Material mat) =>
        Part(parent, PrimitiveType.Cylinder, name, pos, new Vector3(diameter, length / 2f, diameter), mat, Quaternion.Euler(0, 0, 90));

    // a cylinder lying along Z (front to back)
    static Transform CylZ(Transform parent, string name, Vector3 pos, float diameter, float length, Material mat) =>
        Part(parent, PrimitiveType.Cylinder, name, pos, new Vector3(diameter, length / 2f, diameter), mat, Quaternion.Euler(90, 0, 0));

    static Transform Sphere(Transform parent, string name, Vector3 pos, float diameter, Material mat) =>
        Part(parent, PrimitiveType.Sphere, name, pos, Vector3.one * diameter, mat);

    static Transform Part(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat, Quaternion? rot = null)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot ?? Quaternion.identity;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go.transform;
    }
}
