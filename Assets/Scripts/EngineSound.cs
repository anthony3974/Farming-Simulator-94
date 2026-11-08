using UnityEngine;

/// <summary>
/// Engine sound for one <see cref="Vehicle"/>: two loops played together. The idle loop is loudest when standing
/// still; the drive loop fades in as the vehicle speeds up. Both pitches rise with the speed (km/h / top speed),
/// and holding the gas adds a little pitch and volume (the engine under load).
/// Only the vehicle you drive is heard; parked ones fade out. Paused (Time.timeScale 0) stops the sound.
/// Getting into a vehicle plays its start clip once (also when the game begins), and it crossfades into the loops over its end.
/// On the Tractor, Van and Harvester prefabs (added by the one-off Engine Sound Setup tool, now in git history d58093e).
/// </summary>
[RequireComponent(typeof(Vehicle))]
public class EngineSound : MonoBehaviour
{
    [Header("Clips (loops)")]
    public AudioClip idleClip;
    public AudioClip driveClip;

    [Header("Start (one shot when you get in)")]
    [Tooltip("Starter crank and catch. Empty = the loops just fade in.")]
    public AudioClip startClip;
    [Tooltip("Start clip pitch (1 = as recorded). Lower also makes it longer.")]
    public float startPitch = 1f;
    [Tooltip("Start clip volume. Multiplied by Master Volume.")]
    [Range(0f, 1f)] public float startVolume = 1f;
    [Tooltip("How much of the start clip crossfades into the loops: 0.55 = over its last 55%. Higher = a smoother, earlier blend.")]
    [Range(0.05f, 1f)] public float startBlend = 0.55f;

    [Header("Pitch (1 = as recorded)")]
    [Tooltip("Idle loop pitch standing still, and at top speed.")]
    public Vector2 idlePitch = new(1f, 1.4f);
    [Tooltip("Drive loop pitch standing still, and at top speed.")]
    public Vector2 drivePitch = new(0.8f, 1.5f);
    [Tooltip("Extra pitch while the gas is held (the engine works harder).")]
    public float throttlePitch = 0.08f;

    [Header("Volume (0 to 1)")]
    [Tooltip("Idle loop volume standing still, and at top speed.")]
    public Vector2 idleVolume = new(0.8f, 0.25f);
    [Tooltip("Drive loop volume standing still, and at top speed.")]
    public Vector2 driveVolume = new(0f, 0.9f);
    [Tooltip("Extra drive loop volume while the gas is held.")]
    public float throttleVolume = 0.15f;
    [Tooltip("Everything above is multiplied by this.")]
    [Range(0f, 1f)] public float masterVolume = 1f;

    [Header("Smoothing")]
    [Tooltip("How fast pitch and volume follow the speed (higher = quicker).")]
    public float response = 4f;
    [Tooltip("How fast a parked vehicle's engine fades out, and the one you drive fades in, in seconds.")]
    public float fadeTime = 0.4f;

    [Header("Debug")]
    [Tooltip("Show the pitch, speed and volume on screen for the vehicle you drive (for tuning). " +
             "Untick before a real build.")]
    public bool showDebug = false;
    [Tooltip("Where the debug box sits: its top-left corner as a fraction of the screen (0,0 = top-left, 1,1 = bottom-right).")]
    public Vector2 debugPosition = new(0.01f, 0.3f);

    private Vehicle vehicle;
    private AudioSource idleSource, driveSource, startSource;
    private float speed01, load, pitchIdle, pitchDrive, volIdle, volDrive;
    private float onOff; // 0 = engine silent (parked), 1 = running (driven), faded between
    private bool paused, wasActive, startPaused;
    private float startLeft, startLength; // seconds until the start clip ends (counts only while the game runs), and its length
    private GUIStyle debugStyle;
    private const float StartCutTime = 0.1f; // seconds; a short fade so cutting the start clip off does not click

    void Awake()
    {
        vehicle = GetComponent<Vehicle>();
        idleSource = MakeSource(idleClip);
        driveSource = MakeSource(driveClip);
        if (startClip != null)
        {
            startSource = gameObject.AddComponent<AudioSource>();
            startSource.clip = startClip;
            startSource.playOnAwake = false;
            startSource.spatialBlend = 0f;
        }
        pitchIdle = idlePitch.x;
        pitchDrive = drivePitch.x;
    }

    private AudioSource MakeSource(AudioClip clip)
    {
        if (clip == null) return null;
        var source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f; // the camera rides on the vehicle you drive, so 2D sounds the same and is simpler
        source.volume = 0f;
        // start somewhere random, so two vehicles with the same clip never sound in step
        source.timeSamples = Random.Range(0, clip.samples);
        source.Play();
        return source;
    }

    void Update()
    {
        if (idleSource == null && driveSource == null && startSource == null) return;

        // paused: stop (and continue from the same place after)
        bool nowPaused = Time.timeScale == 0f;
        if (nowPaused != paused)
        {
            paused = nowPaused;
            foreach (var s in new[] { idleSource, driveSource })
            {
                if (s == null) continue;
                if (paused) s.Pause(); else s.UnPause();
            }
            // only a clip that was really playing: UnPause must not bring back one that had finished
            if (startSource != null)
            {
                if (paused && startSource.isPlaying) { startSource.Pause(); startPaused = true; }
                else if (!paused && startPaused) { startSource.UnPause(); startPaused = false; }
            }
        }
        if (paused) return;

        float dt = Time.deltaTime;
        UpdateStart(dt);
        float fadeStep = dt / Mathf.Max(0.01f, fadeTime);
        if (vehicle.IsActive && startLeft > 0f)
        {
            // following the crossfade; a loop still fading out from a moment ago comes down to it first
            float target = Mathf.Sin(StartBlend01() * Mathf.PI / 2f);
            onOff = onOff > target ? Mathf.MoveTowards(onOff, target, fadeStep) : target;
        }
        else onOff = Mathf.MoveTowards(onOff, vehicle.IsActive ? 1f : 0f, fadeStep);

        float k = 1f - Mathf.Exp(-response * dt);
        float target01 = vehicle.topSpeed > 0f ? Mathf.Clamp01(Mathf.Abs(vehicle.SpeedKmh) / vehicle.topSpeed) : 0f;
        speed01 = Mathf.Lerp(speed01, target01, k);
        load = Mathf.Lerp(load, vehicle.Throttle ? 1f : 0f, k);

        pitchIdle = Mathf.Lerp(idlePitch.x, idlePitch.y, speed01) + throttlePitch * load;
        pitchDrive = Mathf.Lerp(drivePitch.x, drivePitch.y, speed01) + throttlePitch * load;
        volIdle = Mathf.Lerp(idleVolume.x, idleVolume.y, speed01) * masterVolume * onOff;
        volDrive = Mathf.Clamp01(Mathf.Lerp(driveVolume.x, driveVolume.y, speed01) + throttleVolume * load) * masterVolume * onOff;

        if (idleSource != null) { idleSource.pitch = pitchIdle; idleSource.volume = volIdle; }
        if (driveSource != null) { driveSource.pitch = pitchDrive; driveSource.volume = volDrive; }
    }

    /// <summary>Plays the start clip on getting in, and cuts it off quickly if you switch away while it plays.</summary>
    private void UpdateStart(float dt)
    {
        bool active = vehicle.IsActive;
        if (active && !wasActive && startSource != null)
        {
            startSource.pitch = Mathf.Max(0.1f, startPitch);
            startSource.volume = startVolume * masterVolume;
            startSource.Play();
            startLeft = startLength = startClip.length / startSource.pitch;
        }
        wasActive = active;

        if (startLeft > 0f) startLeft = Mathf.Max(0f, startLeft - dt);
        if (startSource == null || !startSource.isPlaying) return;
        if (active)
        {
            startSource.volume = startVolume * masterVolume * Mathf.Cos(StartBlend01() * Mathf.PI / 2f);
            return;
        }

        startLeft = 0f;
        startSource.volume = Mathf.MoveTowards(startSource.volume, 0f, dt / StartCutTime);
        if (startSource.volume <= 0f) startSource.Stop();
    }

    /// <summary>Where the start clip is in its crossfade: 0 = only the clip so far, 1 = only the loops (the clip has ended).</summary>
    private float StartBlend01() =>
        startLength > 0f ? Mathf.Clamp01(1f - startLeft / (startLength * startBlend)) : 1f;

    void OnGUI()
    {
        if (!showDebug || !vehicle.IsActive) return;

        debugStyle ??= new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = Mathf.Max(14, Screen.height / 40),
            normal = { textColor = new Color32(255, 201, 51, 255) },
        };
        string text =
            $"ENGINE  {vehicle.displayName}\n" +
            $"speed      {vehicle.SpeedKmh,6:0.0} km/h  ({speed01 * 100f,3:0}%)\n" +
            $"gas        {(vehicle.Throttle ? "ON " : "off")}  load {load,4:0.00}\n" +
            $"start      {startLeft,5:0.00} s left\n" +
            $"idle  pitch {pitchIdle,5:0.00}  vol {volIdle,4:0.00}\n" +
            $"drive pitch {pitchDrive,5:0.00}  vol {volDrive,4:0.00}";
        var size = debugStyle.CalcSize(new GUIContent(text));
        // default: left side, below the HELP / PAUSE buttons and above the speedometer; move it with debugPosition
        GUI.Box(new Rect(debugPosition.x * Screen.width, debugPosition.y * Screen.height, size.x + 8f, size.y + 4f), text, debugStyle);
    }
}
