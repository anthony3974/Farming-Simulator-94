using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The player: reads input, drives the active <see cref="Vehicle"/>, moves the follow camera and switches
/// between the vehicles you own (1 / 2 / 3, or CAR on touch). Sits on the starting truck, which is the
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

    // Testing switch: when true the mouse does not move the camera at all (look code is kept, just skipped)
    public bool staticCamera = true;

    private float pitch;
    private bool mouseLook;
    private int ignoreMouseFrames;

    private readonly Dictionary<VehicleKind, Vehicle> owned = new();
    private Speedometer speedo;

    void Start()
    {
        // the truck this script sits on is the starting vehicle (the Dealer Builder tool adds its Vehicle,
        // this is a fallback with the truck's default values)
        var truck = GetComponent<Vehicle>();
        if (truck == null) truck = gameObject.AddComponent<Vehicle>();
        if (truck.cameraOffset == Vector3.zero && characterCam.parent == truck.transform)
            truck.cameraOffset = characterCam.localPosition;
        AddVehicle(truck);

        // the speedometer is calibrated on the truck (its top speed shows as its topSpeed km/h), so faster vehicles show more
        speedo = gameObject.AddComponent<Speedometer>();
        speedo.target = truck.transform;
        speedo.maxWorldSpeed = truck.WorldTopSpeed;
        speedo.maxDisplayKmh = truck.topSpeed;

        DriveVehicle(truck);

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
        if (Input.GetKeyDown(KeyCode.Escape)) SetCursorLocked(false);
        // a click on an open menu (the dealer) is for its buttons, not for grabbing the mouse
        else if (!inputManager.TouchControlsActive && !inputManager.MenuOpen && Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0)) SetCursorLocked(true);

        mouseLook = !staticCamera && Cursor.lockState == CursorLockMode.Locked && ignoreMouseFrames <= 0;
        if (ignoreMouseFrames > 0) ignoreMouseFrames--;
    }

    // Update is called once per frame
    void Update()
    {
        HandleCursor();
        HandleSwitching();
        if (Vehicle.Active != null) Vehicle.Active.Drive(inputManager);
        if (cameraY) RotateY();
    }

    private void RotateY()
    {
        if (!mouseLook) return;

        // moving the mouse up (positive) looks up (negative pitch)
        pitch = Mathf.Clamp(pitch - inputManager.MouseY * mouseSensitivity, minPitch, maxPitch);
        characterCam.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    // ---- vehicles ----

    public bool Owns(VehicleKind kind) => owned.TryGetValue(kind, out var v) && v != null;

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
            // CAR button: the next vehicle you own, in the order truck, van, harvester
            var current = Vehicle.Active != null ? Vehicle.Active.kind : VehicleKind.Truck;
            int count = System.Enum.GetValues(typeof(VehicleKind)).Length;
            for (int i = 1; i < count; i++)
            {
                var next = (VehicleKind)(((int)current + i) % count);
                if (Owns(next)) { SwitchTo(next); return; }
            }
            ScreenPrompt.Instance.Flash("You only own the truck. Buy more vehicles at the dealer.", 3f);
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
        // the camera moves to the vehicle, keeping its tilt
        characterCam.SetParent(vehicle.transform, false);
        if (vehicle.cameraOffset != Vector3.zero) characterCam.localPosition = vehicle.cameraOffset;
        if (speedo != null) speedo.SetTarget(vehicle.transform);
        Vehicle.SetActiveVehicle(vehicle);
    }
}
