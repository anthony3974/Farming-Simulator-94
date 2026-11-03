using System;
using System.Collections.Generic;
using UnityEngine;

public enum VehicleKind { Tractor, Van, Harvester }

/// <summary>
/// One drivable vehicle (the tractor, a van or a harvester). Only the active vehicle, the one the player
/// is driving (see <see cref="GameCharacter"/>), reads input. The others stay parked where they were left.
/// Each vehicle also remembers which fields and buildings it is standing in, so switching to a parked
/// vehicle picks up the field or shop it is parked in.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class Vehicle : MonoBehaviour
{
    public VehicleKind kind = VehicleKind.Tractor;
    public string displayName = "Tractor";

    [Tooltip("Extra harvest from fields: 0.6 = 60% more (the harvester).")]
    public float harvestBonus = 0f;

    // The defaults are the tractor's tuned values from the scene, so a tractor without saved values drives the same.
    [Header("Driving")]
    [Tooltip("Top speed, in the km/h shown on the speedometer.")]
    public float topSpeed = 50;
    public float reverseSpeed = -10;
    public float accelerationMultiplier = 20;
    public float steeringMultiplier = 6;
    [Tooltip("Turns the speed numbers into world units per second.")]
    public float speed = 10;
    public float gravity = 20f;

    [Header("Front wheel steering (visual)")]
    [Tooltip("How far the front tires turn at full steering, in degrees.")]
    public float maxSteerAngle = 20f;
    [Tooltip("How fast the tires turn, in degrees per second.")]
    public float steerAnglePerSecond = 120f;

    [Header("Camera")]
    [Tooltip("Where the follow camera sits, in the vehicle's own space. Zero = where the camera already is (the tractor).")]
    public Vector3 cameraOffset;

    [Tooltip("Body parts that take the vehicle colour (set by the dealer now, the paint shop later).")]
    public List<Renderer> paintRenderers = new();

    /// <summary>The vehicle the player is driving.</summary>
    public static Vehicle Active { get; private set; }

    /// <summary>Raised after the player switches to another vehicle.</summary>
    public static event Action ActiveChanged;

    public bool IsActive => Active == this;
    public CharacterController Controller { get; private set; }

    /// <summary>Top speed in world units per second.</summary>
    public float WorldTopSpeed => topSpeed * SpeedConverter * speed;

    // each front tire gets an un-scaled pivot above it, so it can turn without the squashed axle shearing it
    private readonly List<Transform> frontWheelPivots = new();
    private float steerAngle;

    private float acceleration = 0;
    private const float accAmt = 0.02f;
    private const float SpeedConverter = 0.06f;
    private const float steeringPower = 10;

    private readonly Direction direction = new(false, false, false, false);

    // fields and buildings this vehicle is inside, in the order it entered them (last = current)
    private readonly List<MonoBehaviour> zones = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Active = null; ActiveChanged = null; }

    void Awake()
    {
        Controller = GetComponent<CharacterController>();

        // the vehicle's own solid parts (the tractor's body boxes, a placeholder's body box) must not block its own controller
        foreach (var c in GetComponentsInChildren<Collider>(true))
            if (c != Controller && !c.isTrigger) Physics.IgnoreCollision(Controller, c);
    }

    void Start()
    {
        SetupFrontWheels();
    }

    void OnDestroy()
    {
        if (Active == this) Active = null;
    }

    /// <summary>Makes this the vehicle the player drives. Called by GameCharacter.</summary>
    public static void SetActiveVehicle(Vehicle vehicle)
    {
        if (Active == vehicle) return;
        if (Active != null) Active.Stop();
        Active = vehicle;
        ActiveChanged?.Invoke();
    }

    /// <summary>Paints the body parts.</summary>
    public void SetColor(Color color)
    {
        foreach (var r in paintRenderers)
            if (r != null) r.material.color = color;
    }

    // ---- fields and buildings ----

    /// <summary>The collider is a vehicle's controller (not one of its body parts).</summary>
    public static bool TryGet(Collider other, out Vehicle vehicle)
    {
        vehicle = null;
        return other is CharacterController && other.TryGetComponent(out vehicle);
    }

    public void EnterZone(MonoBehaviour zone)
    {
        zones.Remove(zone);
        zones.Add(zone);
    }

    public void ExitZone(MonoBehaviour zone) => zones.Remove(zone);

    /// <summary>The last entered zone of this type that the vehicle is still inside, or null.</summary>
    public T CurrentZone<T>() where T : MonoBehaviour
    {
        for (int i = zones.Count - 1; i >= 0; i--)
        {
            if (zones[i] == null) { zones.RemoveAt(i); continue; }
            if (zones[i] is T zone && zone.isActiveAndEnabled) return zone;
        }
        return null;
    }

    // ---- driving ----

    /// <summary>Drives one frame from the player's input. Only called on the active vehicle.</summary>
    public void Drive(InputManager input)
    {
        Controller.Move(-gravity * Time.deltaTime * transform.up);

        // takes input and saves it to the class
        KeyManager(input);
        // slows the vehicle down if no input
        DecelerationManager();
        // handles acceleration of the vehicle
        AccelerationManager();
        // handles the steering of the vehicle
        SteeringManager();
        SteerFrontWheels();

        // applies the movement
        Vector3 move = acceleration * speed * transform.forward;
        Controller.Move(Time.deltaTime * move);
    }

    /// <summary>Stops dead (used when the player switches to another vehicle).</summary>
    public void Stop()
    {
        acceleration = 0;
    }

    private void KeyManager(InputManager input)
    {
        direction.Reset();
        direction.Up = input.Up;
        direction.Down = input.Down;
        direction.Right = input.Right;
        direction.Left = input.Left;
    }

    private void Accelerate()
    {
        acceleration += accAmt * accelerationMultiplier * Time.deltaTime;
    }

    private void Decelerate()
    {
        acceleration -= accAmt * accelerationMultiplier * Time.deltaTime;
    }

    private void DecelerationManager()
    {
        if (direction.Up == false && acceleration > 0)
        {
            Decelerate();
            if (acceleration < 0) acceleration = 0;
        }
        if (direction.Down == false && acceleration < 0)
            Accelerate();
    }

    private void AccelerationManager()
    {
        if (direction.Up && acceleration < topSpeed * SpeedConverter)
            Accelerate();
        if (direction.Down && acceleration > reverseSpeed * SpeedConverter)
        {
            Decelerate();
            Decelerate();
        }
    }

    private void SteeringManager()
    {
        float minAccR = -0.085f;
        float minAccF = 0.085f;
        if (acceleration == 0 || (acceleration >= minAccR && acceleration <= minAccF))
        {
            return;
        }

        float rotationAmount = Time.deltaTime * (steeringPower * steeringMultiplier);

        if (direction.Right || direction.Left)
        {
            if (acceleration > minAccF || acceleration < minAccR)
            {
                float rotation = direction.Right ? rotationAmount : -rotationAmount;
                // in reverse the tractor turns the other way, like a real car (steer right = back swings right)
                if (acceleration < 0) rotation = -rotation;
                transform.Rotate(0, rotation, 0);
            }
        }
    }

    // ---- front wheels (visual) ----

    /// <summary>
    /// Finds the two front tires (the FlTire / FrTire objects furthest forward on the vehicle) and puts each
    /// on a pivot at its centre. Looks at positions, not parent names, because the scene renames the axles.
    /// </summary>
    private void SetupFrontWheels()
    {
        var tires = new List<Transform>();
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (t.name == "FlTire" || t.name == "FrTire") tires.Add(t);

        if (tires.Count == 0)
        {
            Debug.LogWarning($"No FlTire / FrTire found under {name}, front wheels will not steer.", this);
            return;
        }

        // forward is +Z in the vehicle's own space, so the front tires have the largest local Z
        float frontZ = float.MinValue;
        foreach (var t in tires) frontZ = Mathf.Max(frontZ, transform.InverseTransformPoint(t.position).z);

        foreach (var tire in tires)
        {
            if (transform.InverseTransformPoint(tire.position).z < frontZ - 0.5f) continue;

            Vector3 worldPos = tire.position;
            Quaternion worldRot = tire.rotation;
            Vector3 worldScale = tire.lossyScale;

            var pivot = new GameObject(tire.name + " Pivot").transform;
            pivot.SetParent(transform, false);
            pivot.position = worldPos;
            pivot.rotation = transform.rotation;

            tire.SetParent(pivot, false);
            tire.position = worldPos;
            tire.rotation = worldRot;
            var ps = pivot.lossyScale;
            tire.localScale = new Vector3(worldScale.x / ps.x, worldScale.y / ps.y, worldScale.z / ps.z);

            frontWheelPivots.Add(pivot);
        }
    }

    /// <summary>Turns the front tires toward the steering button (A / D, arrows or the on-screen buttons).</summary>
    private void SteerFrontWheels()
    {
        if (frontWheelPivots.Count == 0) return;

        float target = ((direction.Right ? 1f : 0f) - (direction.Left ? 1f : 0f)) * maxSteerAngle;
        steerAngle = Mathf.MoveTowards(steerAngle, target, steerAnglePerSecond * Time.deltaTime);

        // positive Y turns right, the same direction SteeringManager rotates the vehicle for Right
        var rot = Quaternion.Euler(0f, steerAngle, 0f);
        foreach (var pivot in frontWheelPivots) pivot.localRotation = rot;
    }
}

public class Direction
{
    private bool up;
    private bool down;
    private bool left;
    private bool right;

    public Direction(bool up, bool down, bool left, bool right)
    {
        Up = up;
        Down = down;
        Left = left;
        Right = right;
    }

    public void Reset()
    {
        Up = false;
        Down = false;
        Left = false;
        Right = false;
    }
    public override string ToString()
    {
        return "Up: " + Up + " Down: " + Down + " Left: " + Left + " Right: " + Right;
    }

    public bool Up { get => up; set => up = value; }
    public bool Down { get => down; set => down = value; }
    public bool Left { get => left; set => left = value; }
    public bool Right { get => right; set => right = value; }
}
