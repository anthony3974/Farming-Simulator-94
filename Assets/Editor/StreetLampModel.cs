using UnityEngine;

// The street lamp's materials (saved as Assets/StreetLamp/<name>.mat).
public enum LampMat { Concrete, Steel, PoleGreen, Housing, Lens }

// Where the parts go. StreetLampModel only talks to this, so the part list can also be checked outside Unity.
public abstract class LampKit
{
    public abstract void Box(string name, Vector3 pos, Vector3 size, LampMat mat, Vector3 euler = default);
    // diameter across, height along the cylinder's own axis (up, before the euler turn)
    public abstract void Cyl(string name, Vector3 pos, float diameter, float height, LampMat mat, bool collider = false, Vector3 euler = default);
    public abstract void Ball(string name, Vector3 pos, float diameter, LampMat mat);
    public abstract void Spot(string name, Vector3 pos, Vector3 euler, float range, float angle, float intensity, Color color);
}

// One street lamp, the design for the whole map (PLAN item 13). A classic farm-road lamp: a concrete footing,
// a dark green steel pole in two sections with a base housing, a top cap, an arm reaching out over the road with
// a diagonal brace, and a flat "cobra head" lamp with a glowing lens and a warm spot light pointing down.
//
// Space: the origin is the ground at the foot of the pole, +Z points out over the road (the arm's direction).
// Sizes here are the 1x design (the builder's Lamp Scale multiplies them all). About 8.1 m tall; the head hangs
// headOut from the pole, 7.8 m up (the harvester, 5.1 m, passes under it). The builder window sets headOut.
// Like Tractor V2 and the gas station, every part reaches a little into the part it stands on or hangs on, so
// nothing floats. Comments give each part's span (Y up, Z out) to show which part it overlaps.
public static class StreetLampModel
{
    public const float Height = 8.1f;      // top of the cap
    public const float MinHeadOut = 0.6f;  // any closer and the head runs into the pole

    public static readonly Color LightColor = new Color32(255, 214, 160, 255); // warm white (sodium-ish, softer)

    public static (string name, Color color, float gloss, bool glow) MaterialInfo(LampMat m) => m switch
    {
        LampMat.Concrete => ("Lamp Concrete", new Color32(150, 148, 142, 255), 0.05f, false),
        LampMat.Steel => ("Lamp Steel", new Color32(95, 98, 102, 255), 0.45f, false),
        LampMat.PoleGreen => ("Lamp Pole Green", new Color32(38, 62, 48, 255), 0.35f, false),
        LampMat.Housing => ("Lamp Housing", new Color32(55, 58, 62, 255), 0.4f, false),
        LampMat.Lens => ("Lamp Lens", new Color32(255, 226, 170, 255), 0.8f, true),
        _ => ("Lamp Steel", Color.grey, 0.4f, false),
    };

    // headOut: centre of the head, from the pole
    public static void Build(LampKit k, float headOut)
    {
        // base: footing Y -0.10..0.40 (0.1 m buried), plate 0.38..0.46, housing 0.42..1.62
        k.Cyl("Footing", new Vector3(0f, 0.15f, 0f), 0.70f, 0.50f, LampMat.Concrete);
        k.Box("Base Plate", new Vector3(0f, 0.42f, 0f), new Vector3(0.46f, 0.08f, 0.46f), LampMat.Steel);
        k.Cyl("Base Housing", new Vector3(0f, 1.02f, 0f), 0.30f, 1.20f, LampMat.PoleGreen, collider: true);
        k.Box("Service Door", new Vector3(0f, 0.95f, -0.135f), new Vector3(0.14f, 0.42f, 0.05f), LampMat.Housing); // Z -0.16..-0.11, sunk into the housing (r 0.15)

        // pole: lower 1.50..4.50, joint ring 4.39..4.51, upper 4.40..8.00, cap 7.90..8.10
        k.Cyl("Pole Lower", new Vector3(0f, 3.00f, 0f), 0.20f, 3.00f, LampMat.PoleGreen, collider: true);
        k.Cyl("Joint Ring", new Vector3(0f, 4.45f, 0f), 0.23f, 0.12f, LampMat.PoleGreen);
        k.Cyl("Pole Upper", new Vector3(0f, 6.20f, 0f), 0.15f, 3.60f, LampMat.PoleGreen);
        k.Ball("Top Cap", new Vector3(0f, 8.00f, 0f), 0.20f, LampMat.PoleGreen);

        // arm: along +Z at Y 7.85, Z -0.05..headOut - 0.15 (starts inside the pole, ends inside the head)
        k.Cyl("Arm", new Vector3(0f, 7.85f, (headOut - 0.20f) / 2f), 0.09f, headOut - 0.10f, LampMat.PoleGreen, euler: new Vector3(90f, 0f, 0f));
        // brace: from the pole up to the arm about halfway along it; 49.6 degrees from upright, 0.1 m longer so both ends sink in
        float bz = headOut * 0.49f, by = bz * 0.85f;
        float len = Mathf.Sqrt(bz * bz + by * by) + 0.10f;
        float tilt = Mathf.Atan2(bz, by) * Mathf.Rad2Deg;
        k.Cyl("Brace", new Vector3(0f, 7.85f - by / 2f, bz / 2f), 0.05f, len, LampMat.PoleGreen, euler: new Vector3(tilt, 0f, 0f));
        k.Cyl("Arm Collar", new Vector3(0f, 7.85f, 0.03f), 0.18f, 0.14f, LampMat.PoleGreen, euler: new Vector3(90f, 0f, 0f)); // Z -0.04..0.10

        // head: housing Y 7.77..7.93, Z headOut +- 0.425; hood 7.91..7.99; lens 7.75..7.79 (glowing, under the housing)
        k.Box("Head Housing", new Vector3(0f, 7.85f, headOut), new Vector3(0.42f, 0.16f, 0.85f), LampMat.Housing);
        k.Box("Head Hood", new Vector3(0f, 7.95f, headOut - 0.03f), new Vector3(0.36f, 0.08f, 0.72f), LampMat.Housing);
        k.Box("Lens", new Vector3(0f, 7.77f, headOut + 0.02f), new Vector3(0.32f, 0.04f, 0.58f), LampMat.Lens);

        // the light: just under the lens, pointing straight down (forward turned to -Y)
        k.Spot("Lamp Light", new Vector3(0f, 7.72f, headOut), new Vector3(90f, 0f, 0f), range: 18f, angle: 120f, intensity: 2.5f, LightColor);
    }
}
