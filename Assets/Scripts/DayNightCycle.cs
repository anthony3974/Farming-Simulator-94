using System;
using UnityEngine;

/// <summary>
/// Day and night (owner, 2026-11-07). On the Directional Light in GameScene (added by Tools > Settings: Game Tab).
/// The sun rises, crosses the sky and sets over <see cref="GameSettings.DayMinutes"/> real minutes (default 10), then a
/// dim blue moonlight takes over for <see cref="GameSettings.NightMinutes"/> (default 5). The sky, the ambient light and
/// the reflections dim with it, and the street lamps (<see cref="NightLight"/>) come on from just before sunset to just
/// after sunrise. Game time: it stops while paused. With the cycle turned off in SETTINGS > GAME it is always day,
/// exactly as the scene was made (the light's own angle, colour and strength).
///
/// The scene's own light angle is "noon": the sun rises 90 degrees to one side of it and sets 90 degrees to the other,
/// highest at the light's own height. The skybox is copied when the game starts, so the sky asset is never changed.
/// For testing in Play mode: the component's menu (the three dots) has Skip To Morning / Sunset / Night.
/// </summary>
[RequireComponent(typeof(Light))]
public class DayNightCycle : MonoBehaviour
{
    [Header("Start")]
    [Tooltip("Where in the day a game starts: 0 = sunrise, 0.5 = noon, 1 = sunset.")]
    [Range(0f, 1f)] public float startDayPart = 0.15f;

    [Header("Sun")]
    [Tooltip("Sun colour low in the sky (sunrise and sunset). The midday colour is the light's own.")]
    public Color lowSunColor = new Color(1f, 0.55f, 0.3f);
    [Tooltip("Lowest the sun is drawn from (degrees), so the ground is never lit exactly side-on.")]
    public float minSunHeight = 3f;

    [Header("Night")]
    public Color moonColor = new Color(0.55f, 0.65f, 1f);
    [Tooltip("Strength of the moonlight (the sun's is the light's own, about 1).")]
    public float moonIntensity = 0.2f;
    [Tooltip("Highest the moon gets (degrees).")]
    public float moonHeight = 40f;
    [Tooltip("Ambient light at night, as a part of the day's (keep it high enough to see on a phone).")]
    [Range(0f, 1f)] public float nightAmbient = 0.35f;
    [Tooltip("Sky brightness at night, as a part of the day's.")]
    [Range(0f, 1f)] public float nightSky = 0.18f;
    public Color nightSkyTint = new Color(0.3f, 0.35f, 0.55f);

    [Header("Lamps")]
    [Tooltip("The lamps are on while the sun is lower than this (0 = horizon, 1 = noon): a little before sunset to a little after sunrise.")]
    [Range(0f, 0.5f)] public float lampsOnBelow = 0.1f;

    /// <summary>The cycle in the scene (null in a scene without one).</summary>
    public static DayNightCycle Instance { get; private set; }

    /// <summary>True while the street lamps should be on.</summary>
    public static bool LampsOn { get; private set; }

    /// <summary>Raised when <see cref="LampsOn"/> changes.</summary>
    public static event Action LampsChanged;

    /// <summary>True from sunset to sunrise.</summary>
    public bool IsNight => night;

    /// <summary>How far through the current day or night (0 to 1).</summary>
    public float Part => part;

    private Light sun;
    private Quaternion baseRotation;
    private float baseHeight, baseYaw, baseIntensity, baseAmbient, baseReflection;
    private Color baseColor;
    private Material sky;
    private float baseExposure = 1f;
    private Color baseTint = new Color(0.5f, 0.5f, 0.5f);
    private bool night;
    private float part;

    void Awake()
    {
        Instance = this;
        sun = GetComponent<Light>();
        baseRotation = transform.rotation;
        Vector3 e = baseRotation.eulerAngles;
        baseHeight = Mathf.Clamp(e.x > 180f ? e.x - 360f : e.x, 10f, 89f);
        baseYaw = e.y;
        baseIntensity = sun.intensity;
        baseColor = sun.color;
        baseAmbient = RenderSettings.ambientIntensity;
        baseReflection = RenderSettings.reflectionIntensity;

        // a copy of the sky to dim, so the material asset is never changed
        if (RenderSettings.skybox != null)
        {
            sky = new Material(RenderSettings.skybox);
            RenderSettings.skybox = sky;
            if (sky.HasProperty("_Exposure")) baseExposure = sky.GetFloat("_Exposure");
            if (sky.HasProperty("_Tint")) baseTint = sky.GetColor("_Tint");
        }

        // TEMP (lamp testing): start at night. Put back: night = false; part = startDayPart;
        night = true;
        part = 0.1f;
        LampsOn = false;
        Apply();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (sky != null) Destroy(sky);
    }

    void Update()
    {
        if (GameSettings.DayNightCycle)
        {
            float seconds = (night ? GameSettings.NightMinutes : GameSettings.DayMinutes) * 60f;
            part += Time.deltaTime / seconds; // game time: stops while paused
            if (part >= 1f)
            {
                part -= 1f;
                night = !night;
            }
        }
        Apply();
    }

    // Puts the sun (or moon), sky, ambient light and lamps where the current time says.
    private void Apply()
    {
        if (!GameSettings.DayNightCycle)
        {
            transform.rotation = baseRotation;
            sun.color = baseColor;
            sun.intensity = baseIntensity;
            SetSky(1f, baseTint);
            RenderSettings.ambientIntensity = baseAmbient;
            RenderSettings.reflectionIntensity = baseReflection;
            SetLamps(false);
            return;
        }

        // how high the sun is: 0 at sunrise and sunset, 1 at noon, below 0 at night (-1 at midnight)
        float height = Mathf.Sin(part * Mathf.PI) * (night ? -1f : 1f);
        float day = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.3f, 0.3f, height)); // 0 night .. 1 day, soft twilight

        if (height > 0f)
        {
            float angle = Mathf.Max(minSunHeight, baseHeight * height);
            transform.rotation = Quaternion.Euler(angle, baseYaw + Mathf.Lerp(-90f, 90f, part), 0f);
            sun.color = Color.Lerp(lowSunColor, baseColor, Mathf.SmoothStep(0f, 1f, height * 2f));
            sun.intensity = baseIntensity * Mathf.SmoothStep(0f, 1f, height / 0.3f);
        }
        else
        {
            // the moon crosses the other side of the sky; it fades in and out at the horizon, so the swap is not seen
            float angle = Mathf.Max(minSunHeight, moonHeight * -height);
            transform.rotation = Quaternion.Euler(angle, baseYaw + 180f + Mathf.Lerp(-60f, 60f, part), 0f);
            sun.color = moonColor;
            sun.intensity = moonIntensity * Mathf.SmoothStep(0f, 1f, -height / 0.3f);
        }

        Color tint = Color.Lerp(nightSkyTint, baseTint, day);
        SetSky(Mathf.Lerp(nightSky, 1f, day), tint);
        RenderSettings.ambientIntensity = baseAmbient * Mathf.Lerp(nightAmbient, 1f, day);
        RenderSettings.reflectionIntensity = baseReflection * Mathf.Lerp(nightAmbient, 1f, day);
        SetLamps(height < lampsOnBelow);
    }

    private void SetSky(float brightness, Color tint)
    {
        if (sky == null) return;
        if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", baseExposure * brightness);
        if (sky.HasProperty("_Tint")) sky.SetColor("_Tint", tint);
    }

    private static void SetLamps(bool on)
    {
        if (LampsOn == on) return;
        LampsOn = on;
        LampsChanged?.Invoke();
    }

    [ContextMenu("Skip To Morning")]
    private void SkipToMorning() { night = false; part = 0.1f; Apply(); }

    [ContextMenu("Skip To Sunset")]
    private void SkipToSunset() { night = false; part = 0.9f; Apply(); }

    [ContextMenu("Skip To Night")]
    private void SkipToNight() { night = true; part = 0.3f; Apply(); }
}
