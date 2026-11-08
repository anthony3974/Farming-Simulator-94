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
    public float topSpeed = 45;
    public float reverseSpeed = -10;
    public float accelerationMultiplier = 20;
    public float steeringMultiplier = 6;
    [Tooltip("How hard BRAKE / S slows the vehicle while it rolls forward, as a multiple of its acceleration. " +
             "Letting go of the gas slows it 1 more on top. Was 2 (3 in all); 2.45 = 15% stronger braking (owner, 2026-11-05).")]
    public float brakeStrength = 2.45f;
    [Tooltip("Turns the speed numbers into world units per second.")]
    public float speed = 10;
    public float gravity = 20f;

    [Header("Fuel")]
    [Tooltip("Diesel burned per km driven, in litres. The tank (100, shared by every vehicle) lasts 100 / this km. " +
             "The map is about 1 km across. 10 = a full tank lasts 10 km, about 6 minutes at the tractor's top speed (2D game: about 5.5).")]
    public float fuelPerKm = 10f;
    [Tooltip("With an empty tank the vehicle only crawls, at this part of its top speed (0.15 = 15%), so it can still reach the gas station.")]
    [Range(0f, 1f)] public float emptyTankSpeed = 0.15f;

    [Header("Wheel steering (visual)")]
    [Tooltip("How far the steered tires turn at full steering, in degrees.")]
    public float maxSteerAngle = 20f;
    [Tooltip("How fast the tires turn, in degrees per second.")]
    public float steerAnglePerSecond = 120f;
    [Tooltip("Steer with the back tires (BlTire / BrTire) instead of the front ones (FlTire / FrTire), like a real " +
             "combine harvester. The back tires turn the other way.")]
    public bool steerRearWheels = false;

    [Header("Wheel rolling (visual)")]
    [Tooltip("Spin the tires as the vehicle drives, so the tread (Tools > Tire Tread Builder) shows them rolling.")]
    public bool rollWheels = true;

    [Header("Camera")]
    [Tooltip("Where the follow camera sits, in the vehicle's own space. Zero = where the camera already is (the tractor).")]
    public Vector3 cameraOffset;

    [Tooltip("Body parts that take the vehicle colour (for the paint shop later; the dealer sells red only).")]
    public List<Renderer> paintRenderers = new();

    /// <summary>The vehicle the player is driving.</summary>
    public static Vehicle Active { get; private set; }

    /// <summary>Raised after the player switches to another vehicle.</summary>
    public static event Action ActiveChanged;

    public bool IsActive => Active == this;
    public CharacterController Controller { get; private set; }

    /// <summary>Top speed in world units per second.</summary>
    public float WorldTopSpeed => topSpeed * SpeedConverter * speed;

    /// <summary>The speed the vehicle is driving at, in the km/h of <see cref="topSpeed"/> (negative in reverse).</summary>
    public float SpeedKmh => acceleration / SpeedConverter;

    /// <summary>The gas (or reverse) is held this frame. Used by <see cref="EngineSound"/> to sound under load.</summary>
    public bool Throttle => IsActive && (direction.Up || direction.Down);

    // each steered tire gets an un-scaled pivot above it, so it can turn without the squashed axle shearing it
    private readonly List<Transform> steerPivots = new();
    private float steerAngle;
    private readonly List<RollingTire> rollingTires = new();
    private float steer; // -1 full left to 1 full right; the touch wheel and slider give values in between
    private float gas;   // 0 to 1; the touch pedal slider gives values in between, keys and buttons 1
    private float brake; // 0 to 1, the same

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
        SetupSteeredWheels();
        SetupRollingWheels();
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
        SteerWheels();

        // applies the movement
        Vector3 before = transform.position;
        Vector3 move = acceleration * speed * transform.forward;
        Controller.Move(Time.deltaTime * move);
        RollWheels(Vector3.Dot(transform.position - before, transform.forward));
        BurnFuel(before);
    }

    // fuel is for the ground actually covered (not for pushing against a wall). One world unit = 1 m (the map is 1 km across).
    private void BurnFuel(Vector3 before)
    {
        var values = PlayerValues.Instance;
        if (values == null) return;
        Vector3 moved = transform.position - before;
        moved.y = 0f;
        values.UseFuel(moved.magnitude / 1000f * fuelPerKm);
    }

    private bool TankEmpty => PlayerValues.Instance != null && PlayerValues.Instance.FuelEmpty;

    /// <summary>Moves the vehicle straight to a place (used when loading a save). It stops dead there.</summary>
    public void PlaceAt(Vector3 position, Quaternion rotation)
    {
        Stop();
        // a CharacterController puts the vehicle back where it was unless it is off while moving
        bool wasOn = Controller.enabled;
        Controller.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        Controller.enabled = wasOn;
        Physics.SyncTransforms();
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
        steer = input.Steer;
        gas = direction.Up ? Mathf.Max(0f, input.Throttle) : 0f;
        brake = direction.Down ? Mathf.Max(0f, -input.Throttle) : 0f;
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
        // an empty tank only crawls: the speed limits shrink and anything faster slows down to them
        float limit = TankEmpty ? emptyTankSpeed : 1f;
        float forwardMax = topSpeed * SpeedConverter * limit;
        float reverseMax = reverseSpeed * SpeedConverter * limit;

        // part gas (touch pedal slider) drives up to that part of the top speed, and eases off to it when
        // the gas is lowered; part brake brakes that much less. Keys and buttons are always full.
        if (direction.Up)
        {
            float target = forwardMax * gas;
            if (acceleration < target) Accelerate();
            else if (acceleration > target && acceleration <= forwardMax)
                acceleration = Mathf.Max(target, acceleration - accAmt * accelerationMultiplier * Time.deltaTime);
        }
        if (direction.Down)
        {
            float target = reverseMax * brake;
            if (acceleration > target)
            {
                // braking (still rolling forward) is stronger than speeding up in reverse
                float steps = acceleration > 0f ? brakeStrength * brake : 2f;
                acceleration -= steps * accAmt * accelerationMultiplier * Time.deltaTime;
                if (acceleration < 0f && acceleration < target && target > reverseMax) acceleration = target;
            }
            else if (acceleration < target && acceleration >= reverseMax)
                acceleration = Mathf.Min(target, acceleration + accAmt * accelerationMultiplier * Time.deltaTime);
        }

        if (acceleration > forwardMax) acceleration = Mathf.Max(forwardMax, acceleration - 2f * accAmt * accelerationMultiplier * Time.deltaTime);
        if (acceleration < reverseMax) acceleration = Mathf.Min(reverseMax, acceleration + 2f * accAmt * accelerationMultiplier * Time.deltaTime);
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

        if (Mathf.Abs(steer) > 0.001f)
        {
            if (acceleration > minAccF || acceleration < minAccR)
            {
                // half steering turns half as fast (touch wheel / slider); keys and buttons are always full
                float rotation = rotationAmount * steer;
                // in reverse the tractor turns the other way, like a real car (steer right = back swings right)
                if (acceleration < 0) rotation = -rotation;
                transform.Rotate(0, rotation, 0);
            }
        }
    }

    // ---- steered wheels (visual) ----

    /// <summary>
    /// Finds the two steered tires and puts each on a pivot at its centre: the front tires (FlTire / FrTire
    /// furthest forward on the vehicle), or with <see cref="steerRearWheels"/> the back tires (BlTire / BrTire
    /// furthest back). Looks at positions, not parent names, because the scene renames the axles.
    /// Only active tires: a turned-off model (an old model kept beside the new one) must stay hidden.
    /// </summary>
    private void SetupSteeredWheels()
    {
        string left = steerRearWheels ? "BlTire" : "FlTire";
        string right = steerRearWheels ? "BrTire" : "FrTire";
        var tires = new List<Transform>();
        foreach (var t in GetComponentsInChildren<Transform>())
            if (t.name == left || t.name == right) tires.Add(t);

        if (tires.Count == 0)
        {
            Debug.LogWarning($"No {left} / {right} found under {name}, the wheels will not steer.", this);
            return;
        }

        // forward is +Z in the vehicle's own space: the front tires have the largest local Z, the back tires the smallest
        float endZ = steerRearWheels ? float.MaxValue : float.MinValue;
        foreach (var t in tires)
        {
            float z = transform.InverseTransformPoint(t.position).z;
            endZ = steerRearWheels ? Mathf.Min(endZ, z) : Mathf.Max(endZ, z);
        }

        foreach (var tire in tires)
        {
            if (Mathf.Abs(transform.InverseTransformPoint(tire.position).z - endZ) > 0.5f) continue;

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

            steerPivots.Add(pivot);
        }
    }

    /// <summary>Turns the steered tires with the steering (A / D, arrows, or the touch buttons, wheel or slider).</summary>
    private void SteerWheels()
    {
        if (steerPivots.Count == 0) return;

        float target = steer * maxSteerAngle;
        steerAngle = Mathf.MoveTowards(steerAngle, target, steerAnglePerSecond * Time.deltaTime);

        // positive Y turns right, the same direction SteeringManager rotates the vehicle for Right.
        // Back tires point the other way to turn the vehicle the same way (the back swings out).
        var rot = Quaternion.Euler(0f, steerRearWheels ? -steerAngle : steerAngle, 0f);
        foreach (var pivot in steerPivots) pivot.localRotation = rot;
    }

    // ---- rolling wheels (visual) ----

    private class RollingTire
    {
        public Transform tire;
        public Quaternion rest;  // its turn before any rolling (under its steering pivot, if it has one)
        public float radius;     // in world units
        public float direction;  // +1 or -1: which way around its own Y is rolling forward
        public float angle;      // degrees rolled so far
    }

    /// <summary>The tire names the vehicles use: FlTire, FrTire (front), RlTire, RrTire or BlTire, BrTire (back).</summary>
    public static bool IsTireName(string n) =>
        n.Length == 6 && n.EndsWith("Tire") && (n[0] == 'F' || n[0] == 'R' || n[0] == 'B') && (n[1] == 'l' || n[1] == 'r');

    /// <summary>
    /// Finds every active tire. Runs after <see cref="SetupSteeredWheels"/>, so a steered tire rolls inside its pivot.
    /// The tires are cylinders turned on their side: their own Y is the axle, their X / Z scale the diameter.
    /// </summary>
    private void SetupRollingWheels()
    {
        foreach (var t in GetComponentsInChildren<Transform>())
        {
            if (!IsTireName(t.name)) continue;
            float radius = t.lossyScale.x * 0.5f;
            if (radius <= 0.01f) continue;
            // rolling forward turns the tire around the vehicle's right axis; the tire's Y points right or left
            float dir = Vector3.Dot(t.rotation * Vector3.up, transform.right) >= 0f ? 1f : -1f;
            rollingTires.Add(new RollingTire { tire = t, rest = t.localRotation, radius = radius, direction = dir });
        }
    }

    /// <summary>Spins the tires for the distance driven this frame (negative in reverse). Pushing against a wall does not spin them.</summary>
    private void RollWheels(float distance)
    {
        if (!rollWheels || rollingTires.Count == 0 || Mathf.Abs(distance) < 0.00001f) return;
        foreach (var w in rollingTires)
        {
            if (w.tire == null) continue;
            w.angle = Mathf.Repeat(w.angle + w.direction * distance / w.radius * Mathf.Rad2Deg, 360f);
            w.tire.localRotation = w.rest * Quaternion.AngleAxis(w.angle, Vector3.up);
        }
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
