using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The player: reads input, drives the active <see cref="Vehicle"/>, moves the follow camera and switches
/// between the vehicles you own (1 / 2 / 3, or CAR on touch). Sits on the starting tractor, which is the
/// first vehicle you own; vehicles bought at the dealer are added with <see cref="AddVehicle"/>.
/// </summary>
public class GameCharacter : MonoBehaviour
{
    // input manager
    public InputManager inputManager;

    public bool cameraY = true;
    public Transform characterCam;

    public float mouseSensitivity = 5.0f;

    // Camera look limits in degrees: negative = looking up, positive = looking down
    public float minPitch = -10f;
    public float maxPitch = 50f;

    [Tooltip("Touch: degrees the camera tilts for a drag the full height of the screen.")]
    public float touchLookSensitivity = 120f;
    [Tooltip("Touch: tick so dragging up looks down (as if pulling the view) instead of up (like the mouse).")]
    public bool invertTouchLook = false;
    [Tooltip("Touch: degrees the camera turns around the vehicle for a drag the full height of the screen.")]
    public float touchLookSensitivityX = 180f;
    [Tooltip("Touch: tick so dragging right turns the camera left (as if pulling the view) instead of right (like the mouse).")]
    public bool invertTouchLookX = false;

    [Header("Zoom")]
    [Tooltip("Closest and farthest zoom, as a multiple of the vehicle's own camera distance (1 = the default distance).")]
    public float minZoom = 0.4f;
    public float maxZoom = 2.5f;
    [Tooltip("Mouse wheel: how much one notch zooms (0.1 = 10%).")]
    public float wheelZoomStep = 0.1f;
    [Tooltip("Touch: how much a pinch the full height of the screen zooms (higher = faster).")]
    public float pinchZoomSensitivity = 2.5f;

    [Header("Camera")]
    [Tooltip("Pull the camera in when a building, tree or another vehicle is in the way.")]
    public bool cameraCollision = true;
    [Tooltip("Size of the camera's 'bubble' in metres: walls stay at least this far from the camera, so the screen never shows through them.")]
    public float cameraRadius = 0.6f;
    [Tooltip("The camera never comes closer to the pivot than this many metres because of an obstacle.")]
    public float minObstacleDistance = 0.4f;
    [Tooltip("Seconds the camera takes to swing back behind the vehicle after a double tap.")]
    public float recenterSeconds = 0.25f;

    // Mouse look works on PC while the cursor is locked (click the game, Esc frees it): left / right turns the camera
    // all the way around the vehicle (full 360), up / down tilts it, the wheel zooms. It stays where you leave it;
    // there is no automatic recentre on PC. On phones the cursor is never locked; one finger on the game view turns and
    // tilts the camera, two fingers zoom, a double tap brings it back behind the vehicle (TouchLook, "Touch Look" object).
    // Steering turns the vehicle and the camera turns with it (the camera pivot is a child of the vehicle).

    /// <summary>How far the camera is zoomed out past its default distance: 0 = default or closer, 1 = all the way out.</summary>
    public static float ZoomOut01 { get; private set; }

    private float pitch;
    private float yaw;          // degrees around the vehicle, 0 = behind it, -180..180
    private float yawVelocity;
    private bool recentering;
    private float zoomTarget = 1f;
    private float zoom = 1f;
    private float distanceScale = 1f; // 1 = free, less = pulled in by an obstacle
    private Transform pivot;          // child of the active vehicle at camera height; turns by yaw
    private Vector3 camOffset;        // the active vehicle's camera spot (vehicle space)
    private static readonly RaycastHit[] hits = new RaycastHit[16];
    private bool mouseLook;
    private int ignoreMouseFrames;

    private readonly Dictionary<VehicleKind, Vehicle> owned = new();
    private Speedometer speedo;

    void Start()
    {
        // the tractor this script sits on is the starting vehicle (the Dealer Builder tool adds its Vehicle,
        // this is a fallback with the tractor's default values)
        var tractor = GetComponent<Vehicle>();
        if (tractor == null) tractor = gameObject.AddComponent<Vehicle>();
        if (tractor.cameraOffset == Vector3.zero && characterCam.parent == tractor.transform)
            tractor.cameraOffset = characterCam.localPosition;
        AddVehicle(tractor);

        // the speedometer (a scene object) is calibrated on the tractor (its top speed shows as its topSpeed km/h),
        // so faster vehicles show more
        speedo = FindFirstObjectByType<Speedometer>();
        if (speedo != null)
        {
            speedo.SetTarget(tractor.transform);
            speedo.maxWorldSpeed = tractor.WorldTopSpeed;
            speedo.maxDisplayKmh = tractor.topSpeed;
        }
        else Debug.LogWarning("No Speedometer in the scene: run Tools > Build Speedometer in GameScene.", this);

        // the camera hangs on a pivot that sits on the vehicle and turns by yaw, so the camera circles the vehicle
        pivot = new GameObject("Camera Pivot").transform;
        camOffset = tractor.cameraOffset != Vector3.zero ? tractor.cameraOffset : characterCam.localPosition;
        characterCam.SetParent(pivot, false);
        DriveVehicle(tractor);

        // start from the camera's current pitch (-180..180) instead of Euler 0..360
        pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, characterCam.localEulerAngles.x), minPitch, maxPitch);
        // with on-screen touch buttons the cursor must stay free so they can be clicked
        SetCursorLocked(!inputManager.TouchControlsActive);
    }

    // When the window regains focus Unity reports one huge mouse movement; skip a couple of frames.
    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) ignoreMouseFrames = 2;
    }

    public void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
        ignoreMouseFrames = 2;
    }

    private void HandleCursor()
    {
        // Esc frees the mouse, unless it is closing a menu (the pause menu relocks it when it resumes)
        if (Input.GetKeyDown(KeyCode.Escape)) { if (!inputManager.MenuOpen && !inputManager.MenuJustClosed) SetCursorLocked(false); }
        // a click on an open menu (the dealer) is for its buttons, not for grabbing the mouse
        else if (!inputManager.TouchControlsActive && !inputManager.MenuOpen && Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0)) SetCursorLocked(true);

        mouseLook = Cursor.lockState == CursorLockMode.Locked && ignoreMouseFrames <= 0;
        if (ignoreMouseFrames > 0) ignoreMouseFrames--;
    }

    // Update is called once per frame
    void Update()
    {
        HandleCursor();
        HandleSwitching();
        if (Vehicle.Active != null) Vehicle.Active.Drive(inputManager);
        if (cameraY) LookAround();
    }

    // LateUpdate: after the vehicle has moved this frame, so the camera does not lag behind it
    void LateUpdate()
    {
        if (cameraY) PlaceCamera();
    }

    private void LookAround()
    {
        // moving the mouse or a finger up (positive) looks up (negative pitch)
        float up = inputManager.TouchLookY * touchLookSensitivity * (invertTouchLook ? -1f : 1f);
        // moving right turns the camera to the right (positive yaw)
        float right = inputManager.TouchLookX * touchLookSensitivityX * (invertTouchLookX ? -1f : 1f);
        if (mouseLook)
        {
            up += inputManager.MouseY * mouseSensitivity;
            right += inputManager.MouseX * mouseSensitivity;
        }

        if (up != 0f) pitch = Mathf.Clamp(pitch - up, minPitch, maxPitch);

        if (inputManager.Recenter) recentering = true;
        if (right != 0f)
        {
            recentering = false; // the player took over
            yaw = Mathf.DeltaAngle(0f, yaw + right); // full 360: wrap, never stop
        }
        else if (recentering)
        {
            yaw = Mathf.SmoothDampAngle(yaw, 0f, ref yawVelocity, recenterSeconds);
            if (Mathf.Abs(yaw) < 0.1f) { yaw = 0f; yawVelocity = 0f; recentering = false; }
        }

        // zoom: mouse wheel (scroll up = closer) while the mouse is captured, or a pinch (fingers apart = closer)
        float zoomIn = inputManager.TouchZoom * pinchZoomSensitivity;
        if (mouseLook) zoomIn += inputManager.MouseScroll * wheelZoomStep;
        if (zoomIn != 0f) zoomTarget = Mathf.Clamp(zoomTarget * Mathf.Exp(-zoomIn), minZoom, maxZoom);
    }

    // The camera sits behind the pivot at the zoomed distance, looking along the pivot's direction tilted by pitch.
    // At yaw 0 and zoom 1 it is exactly where the vehicle's cameraOffset puts it.
    private void PlaceCamera()
    {
        if (pivot == null || pivot.parent == null) return;

        zoom = Mathf.Lerp(zoom, zoomTarget, 1f - Mathf.Exp(-12f * Time.deltaTime));
        ZoomOut01 = Mathf.InverseLerp(1f, maxZoom, zoom);
        pivot.localPosition = new Vector3(0f, camOffset.y, 0f);
        pivot.localRotation = Quaternion.Euler(0f, yaw, 0f);

        Vector3 local = new Vector3(camOffset.x, 0f, camOffset.z * zoom);
        float wanted = 1f;
        if (cameraCollision) wanted = ObstacleScale(local);
        // pull in at once (never look through a wall), ease back out
        distanceScale = wanted < distanceScale ? wanted : Mathf.Lerp(distanceScale, wanted, 1f - Mathf.Exp(-6f * Time.deltaTime));

        characterCam.localPosition = local * distanceScale;
        characterCam.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    // 1 = nothing in the way, less = the share of the distance to the nearest solid thing (not a trigger, not this vehicle)
    private float ObstacleScale(Vector3 local)
    {
        Vector3 from = pivot.position;
        Vector3 to = pivot.TransformPoint(local);
        Vector3 dir = to - from;
        float length = dir.magnitude;
        if (length < 0.01f) return 1f;

        // a sphere, not a thin ray: the camera's near plane is wider than a point, so a ray would let it poke through a wall
        int count = Physics.SphereCastNonAlloc(from, cameraRadius, dir / length, hits, length, ~0, QueryTriggerInteraction.Ignore);
        float nearest = length;
        for (int i = 0; i < count; i++)
        {
            if (hits[i].distance <= 0f) continue; // already overlapping at the start (a wall right at the pivot): no direction to pull in
            if (Vehicle.Active != null && hits[i].transform.IsChildOf(Vehicle.Active.transform)) continue;
            if (hits[i].distance < nearest) nearest = hits[i].distance;
        }
        if (nearest >= length) return 1f;
        float distance = Mathf.Min(length, Mathf.Max(nearest, minObstacleDistance));
        return distance / length;
    }

    // ---- vehicles ----

    public bool Owns(VehicleKind kind) => owned.TryGetValue(kind, out var v) && v != null;

    /// <summary>The vehicles you own (for the save file).</summary>
    public IEnumerable<Vehicle> OwnedVehicles
    {
        get { foreach (var v in owned.Values) if (v != null) yield return v; }
    }

    /// <summary>Drives this vehicle, without the "Driving the ..." message (used when loading a save).</summary>
    public void Drive(Vehicle vehicle)
    {
        if (vehicle != null) DriveVehicle(vehicle);
    }

    /// <summary>Adds a vehicle you now own (it stays parked until you switch to it).</summary>
    public void AddVehicle(Vehicle vehicle)
    {
        owned[vehicle.kind] = vehicle;
    }

    private void HandleSwitching()
    {
        int request = inputManager.SwitchVehicle;
        if (request == 0) return;

        if (request < 0)
        {
            // CAR button: the next vehicle you own, in the order tractor, van, harvester
            var current = Vehicle.Active != null ? Vehicle.Active.kind : VehicleKind.Tractor;
            int count = System.Enum.GetValues(typeof(VehicleKind)).Length;
            for (int i = 1; i < count; i++)
            {
                var next = (VehicleKind)(((int)current + i) % count);
                if (Owns(next)) { SwitchTo(next); return; }
            }
            ScreenPrompt.Instance.Flash("You only own the tractor. Buy more vehicles at the dealer.", 3f);
            return;
        }

        var kind = (VehicleKind)(request - 1);
        if (!System.Enum.IsDefined(typeof(VehicleKind), kind)) return;
        if (Owns(kind)) SwitchTo(kind);
        else ScreenPrompt.Instance.Flash($"You don't own a {VehicleCatalog.Get(kind).name} yet. Buy one at the dealer.", 3f);
    }

    private void SwitchTo(VehicleKind kind)
    {
        var vehicle = owned[kind];
        if (vehicle == Vehicle.Active) return;
        DriveVehicle(vehicle);
        ScreenPrompt.Instance.Flash($"Driving the {vehicle.displayName}", 2f);
    }

    private void DriveVehicle(Vehicle vehicle)
    {
        // the camera pivot moves to the vehicle, keeping the tilt, turn and zoom
        pivot.SetParent(vehicle.transform, false);
        if (vehicle.cameraOffset != Vector3.zero) camOffset = vehicle.cameraOffset;
        distanceScale = 1f;
        if (speedo != null) speedo.SetTarget(vehicle.transform);
        Vehicle.SetActiveVehicle(vehicle);
    }
}
