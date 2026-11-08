using UnityEngine;

/// <summary>
/// The headlights of one vehicle (owner, 2026-11-08). They are on only for the vehicle the player is driving, and only
/// at night (<see cref="DayNightCycle.LampsOn"/>, the same moment the street lamps come on). Parked vehicles stay dark.
/// In a scene with no day / night cycle the driven vehicle's lights stay on, like <see cref="NightLight"/>.
/// Set up on the three vehicle prefabs by Tools > Vehicle Lights Setup.
/// </summary>
[RequireComponent(typeof(Vehicle))]
public class VehicleLights : MonoBehaviour
{
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    [Tooltip("The one real light, pointing forward.")]
    public Light beam;
    [Tooltip("The lens parts that glow while the lights are on (their material has emission turned on).")]
    public Renderer[] lenses;

    private Vehicle vehicle;
    private MaterialPropertyBlock dark;

    void Awake()
    {
        vehicle = GetComponent<Vehicle>();
        dark = new MaterialPropertyBlock();
        dark.SetColor(EmissionColor, Color.black);
    }

    void OnEnable()
    {
        DayNightCycle.LampsChanged += Refresh;
        Vehicle.ActiveChanged += Refresh;
        Refresh();
    }

    void Start() => Refresh(); // the cycle may wake after this vehicle

    void OnDisable()
    {
        DayNightCycle.LampsChanged -= Refresh;
        Vehicle.ActiveChanged -= Refresh;
    }

    private void Refresh()
    {
        bool on = vehicle.IsActive && (DayNightCycle.Instance == null || DayNightCycle.LampsOn);
        if (beam != null) beam.enabled = on;
        foreach (var r in lenses)
            if (r != null) r.SetPropertyBlock(on ? null : dark);
    }
}
