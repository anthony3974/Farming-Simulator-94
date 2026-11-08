using UnityEngine;

/// <summary>The on-screen buttons shown on touch devices.</summary>
public enum TouchButton { Left, Right, Gas, Brake, Interact, Trade, CycleCrop, Help, Vehicle }
// SETTINGS is a plain Button (SettingsMenu), not a TouchButton.

/// <summary>
/// Single place the game reads input from. Keyboard/mouse always work; on Android (or when
/// <see cref="showTouchControls"/> is ticked in the editor) on-screen buttons feed the same values.
/// </summary>
// Run before every other script so they all see this frame's input.
[DefaultExecutionOrder(-100)]
public class InputManager : MonoBehaviour
{
    [Tooltip("Show the on-screen touch buttons in the editor too (they always show on Android/iOS).")]
    public bool showTouchControls = false;

    private bool debug;
    private float horizontal;
    private float vertical;
    private float mouseX;
    private float mouseY;
    private bool interact;
    private bool trade;
    private bool cycleCrop;
    private bool help;
    private int switchVehicle;
    private bool up;
    private bool right;
    private bool down;
    private bool left;
    private float steer;
    private float throttle;

    // touch state, set by TouchControls
    private bool touchLeft;
    private bool touchRight;
    private bool touchGas;
    private bool touchBrake;
    private bool touchInteractQueued;
    private bool touchTradeQueued;
    private bool touchCropQueued;
    private bool touchHelpQueued;
    private bool touchVehicleQueued;
    private float touchSteer; // the touch steering wheel or slider, -1 to 1
    private float touchThrottle; // the touch pedal slider, -1 (full brake) to 1 (full gas)
    // drag-to-look, set by TouchLook: summed since the last frame, in screen heights (up = positive)
    private float touchLookPending;
    private float touchLookY;
    private float touchLookPendingX;
    private float touchLookX;
    // pinch zoom (screen heights the two fingers moved apart) and double-tap recenter, set by TouchLook
    private float touchZoomPending;
    private float touchZoom;
    private bool touchRecenterQueued;
    private bool recenter;
    private float mouseScroll;

    /// <summary>True when the on-screen buttons are active.</summary>
    public bool TouchControlsActive { get; private set; }

    /// <summary>
    /// Set by a menu that needs the mouse (the dealer). While true, driving and the game keys are ignored
    /// and a click does not grab the mouse.
    /// </summary>
    public bool MenuOpen
    {
        get => menuOpen;
        set { if (menuOpen && !value) menuClosedFrame = Time.frameCount; menuOpen = value; }
    }
    private bool menuOpen;
    private int menuClosedFrame = -1;

    /// <summary>True on the frame a menu closed, so the same Esc press does not also open the pause menu.</summary>
    public bool MenuJustClosed => menuClosedFrame == Time.frameCount;

    void Awake()
    {
        // the touch buttons are a scene object ("Touch Controls", TouchControls.cs) that hides itself when this is false
        TouchControlsActive = Application.isMobilePlatform || showTouchControls;
    }

    /// <summary>Called by <see cref="TouchLook"/> while a finger drags on the game view (dy in screen heights, up = positive).</summary>
    public void AddTouchLook(float dy)
    {
        touchLookPending += dy;
    }

    /// <summary>Called by <see cref="TouchLook"/> for a horizontal drag (dx in screen heights, right = positive).</summary>
    public void AddTouchLookX(float dx)
    {
        touchLookPendingX += dx;
    }

    /// <summary>Called by <see cref="TouchLook"/> while two fingers pinch (change of their distance in screen heights, apart = positive).</summary>
    public void AddTouchZoom(float spread)
    {
        touchZoomPending += spread;
    }

    /// <summary>Called by <see cref="TouchLook"/> on a double tap: bring the camera back behind the vehicle.</summary>
    public void RequestRecenter()
    {
        touchRecenterQueued = true;
    }

    /// <summary>Called every frame by the touch steering wheel or slider: -1 full left, 0 straight, 1 full right.</summary>
    public void SetSteer(float value)
    {
        touchSteer = Mathf.Clamp(value, -1f, 1f);
    }

    /// <summary>Called every frame by the touch pedal slider: -1 full brake, 0 nothing, 1 full gas.</summary>
    public void SetThrottle(float value)
    {
        touchThrottle = Mathf.Clamp(value, -1f, 1f);
    }

    /// <summary>Called by the on-screen buttons.</summary>
    public void SetTouch(TouchButton button, bool pressed)
    {
        switch (button)
        {
            case TouchButton.Left: touchLeft = pressed; break;
            case TouchButton.Right: touchRight = pressed; break;
            case TouchButton.Gas: touchGas = pressed; break;
            case TouchButton.Brake: touchBrake = pressed; break;
            case TouchButton.Interact: if (pressed) touchInteractQueued = true; break;
            case TouchButton.Trade: if (pressed) touchTradeQueued = true; break;
            case TouchButton.CycleCrop: if (pressed) touchCropQueued = true; break;
            case TouchButton.Help: if (pressed) touchHelpQueued = true; break;
            case TouchButton.Vehicle: if (pressed) touchVehicleQueued = true; break;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
            debug = !debug;

        float touchH = (touchRight ? 1f : 0f) - (touchLeft ? 1f : 0f);
        float touchV = (touchGas ? 1f : 0f) - (touchBrake ? 1f : 0f);

        horizontal = Mathf.Clamp(Input.GetAxis("Horizontal") + touchH, -1f, 1f);
        vertical = Mathf.Clamp(Input.GetAxis("Vertical") + touchV, -1f, 1f);
        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y");
        mouseScroll = Input.mouseScrollDelta.y;

        up = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) || touchGas;
        down = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) || touchBrake;
        throttle = Mathf.Clamp((up ? 1f : 0f) - (down ? 1f : 0f) + touchThrottle, -1f, 1f);
        up |= touchThrottle > 0f;
        down |= touchThrottle < 0f;
        right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) || touchRight;
        left = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) || touchLeft;
        steer = Mathf.Clamp((right ? 1f : 0f) - (left ? 1f : 0f) + touchSteer, -1f, 1f);

        interact = Input.GetKeyDown(KeyCode.E) || touchInteractQueued;
        touchInteractQueued = false;

        trade = Input.GetKeyDown(KeyCode.B) || touchTradeQueued;
        touchTradeQueued = false;
        cycleCrop = Input.GetKeyDown(KeyCode.C) || touchCropQueued;
        touchCropQueued = false;
        help = Input.GetKeyDown(KeyCode.H) || touchHelpQueued;
        touchHelpQueued = false;

        switchVehicle = 0;
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) switchVehicle = 1;
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) switchVehicle = 2;
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) switchVehicle = 3;
        else if (Input.GetKeyDown(KeyCode.Tab) || touchVehicleQueued) switchVehicle = -1; // Tab on PC = the CAR button
        touchVehicleQueued = false;

        touchLookY = touchLookPending;
        touchLookPending = 0f;
        touchLookX = touchLookPendingX;
        touchLookPendingX = 0f;
        touchZoom = touchZoomPending;
        touchZoomPending = 0f;
        recenter = touchRecenterQueued;
        touchRecenterQueued = false;

        // a menu is open: the vehicle stops taking input (it rolls to a stop) and the game keys do nothing,
        // H / HELP included, so the help panel cannot open over the pause, dealer or settings menu
        if (MenuOpen)
        {
            horizontal = vertical = 0f;
            up = down = left = right = false;
            steer = 0f;
            throttle = 0f;
            interact = trade = cycleCrop = help = false;
            touchLookY = touchLookX = touchZoom = 0f;
            mouseScroll = 0f;
            recenter = false;
            switchVehicle = 0;
        }
    }

    public float Horizontal { get => horizontal; set => horizontal = value; }
    public float Vertical { get => vertical; set => vertical = value; }
    public float MouseX { get => mouseX; set => mouseX = value; }
    public float MouseY { get => mouseY; set => mouseY = value; }
    /// <summary>Drag-to-look on touch this frame, in screen heights (up = positive). 0 while a menu is open.</summary>
    public float TouchLookY => touchLookY;
    /// <summary>Drag-to-look on touch this frame, in screen heights (right = positive). 0 while a menu is open.</summary>
    public float TouchLookX => touchLookX;
    /// <summary>Pinch this frame, in screen heights (fingers moving apart = positive). 0 while a menu is open.</summary>
    public float TouchZoom => touchZoom;
    /// <summary>Mouse wheel this frame (scroll up = positive). 0 while a menu is open.</summary>
    public float MouseScroll => mouseScroll;
    /// <summary>True on the frame of a double tap on the game view (touch): recenter the camera behind the vehicle.</summary>
    public bool Recenter => recenter;
    public bool Interact { get => interact; set => interact = value; }
    /// <summary>B key: buy or sell the field you are standing in (press twice to confirm).</summary>
    public bool Trade { get => trade; set => trade = value; }
    /// <summary>C key: pick the next crop to plant.</summary>
    public bool CycleCrop { get => cycleCrop; set => cycleCrop = value; }
    /// <summary>H key: open or close the help menu.</summary>
    public bool Help { get => help; set => help = value; }
    /// <summary>1 / 2 / 3 keys: drive the tractor, van or harvester (1 to 3). Tab key or CAR touch button: -1 = next vehicle. 0 = no request.</summary>
    public int SwitchVehicle => switchVehicle;
    public bool Up { get => up; set => up = value; }
    public bool Right { get => right; set => right = value; }
    public bool Down { get => down; set => down = value; }
    public bool Left { get => left; set => left = value; }
    /// <summary>Steering, -1 full left to 1 full right. Keys and the &lt; &gt; buttons give -1 / 0 / 1, the touch wheel and slider anything in between.</summary>
    public float Steer => steer;
    /// <summary>Gas and brake, -1 full brake to 1 full gas. Keys and the GAS / BRAKE buttons give -1 / 0 / 1, the touch pedal slider anything in between.</summary>
    public float Throttle => throttle;
    public bool DebugMode { get => debug; set => debug = value; }
}
