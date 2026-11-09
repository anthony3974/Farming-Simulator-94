using UnityEngine;

/// <summary>
/// On a street lamp and on the house and garage lights: its lights and glowing parts are on only at night,
/// following <see cref="DayNightCycle.LampsOn"/>. Finds its own lights (every Light below it) and glowing parts
/// (renderers whose material has emission turned on). In a scene with no day / night cycle it stays on.
/// A lamp with a <see cref="LightPool"/> leaves its real light to <see cref="StreetLampLights"/> and only does the glow here.
/// </summary>
public class NightLight : MonoBehaviour
{
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    /// <summary>True while night lights should be on.</summary>
    public static bool LitNow => DayNightCycle.Instance == null || DayNightCycle.LampsOn;

    private Light[] lights;
    private Renderer[] glows;
    private MaterialPropertyBlock dark;

    void Awake()
    {
        lights = GetComponentInChildren<LightPool>(true) != null ? new Light[0] : GetComponentsInChildren<Light>(true);
        var list = new System.Collections.Generic.List<Renderer>();
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            var m = r.sharedMaterial;
            if (m != null && m.IsKeywordEnabled("_EMISSION")) list.Add(r);
        }
        glows = list.ToArray();
        dark = new MaterialPropertyBlock();
        dark.SetColor(EmissionColor, Color.black);
    }

    void OnEnable()
    {
        DayNightCycle.LampsChanged += Refresh;
        Refresh();
    }

    void Start() => Refresh(); // the cycle may wake after this lamp

    void OnDisable() => DayNightCycle.LampsChanged -= Refresh;

    private void Refresh()
    {
        if (lights == null) return;
        bool on = LitNow;
        foreach (var l in lights) if (l != null) l.enabled = on;
        foreach (var r in glows)
            if (r != null) r.SetPropertyBlock(on ? null : dark);
    }
}
