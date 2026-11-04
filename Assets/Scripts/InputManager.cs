using UnityEngine;

/// <summary>The on-screen buttons shown on touch devices.</summary>
public enum TouchButton { Left, Right, Gas, Brake, Interact, Trade, CycleCrop, Help, Vehicle, Fertilize }

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
    private bool fertilize;
    private bool help;
    private int switchVehicle;
    private bool up;
    private bool right;
    private bool down;
    private bool left;

    // touch state, set by TouchControls
    private bool touchLeft;
    private bool touchRight;
    private bool touchGas;
    private bool touchBrake;
    private bool touchInteractQueued;
    private bool touchTradeQueued;
    private bool touchCropQueued;
    private bool touchFertilizeQueued;
    private bool touchHelpQueued;
    private bool touchVehicleQueued;

    /// <summary>True when the on-screen buttons are active.</summary>
    public bool TouchControlsActive { get; private set; }

    /// <summary>
    /// Set by a menu that needs the mouse (the dealer). While true, driving and the game keys are ignored
    /// and a click does not grab the mouse.
    /// </summary>
    public bool MenuOpen { get; set; }

    void Awake()
    {
        if (Application.isMobilePlatform || showTouchControls)
        {
            TouchControlsActive = true;
            gameObject.AddComponent<TouchControls>();
        }

        gameObject.AddComponent<HelpMenu>();
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
            case TouchButton.Fertilize: if (pressed) touchFertilizeQueued = true; break;
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

        up = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) || touchGas;
        down = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) || touchBrake;
        right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) || touchRight;
        left = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) || touchLeft;

        interact = Input.GetKeyDown(KeyCode.E) || touchInteractQueued;
        touchInteractQueued = false;

        trade = Input.GetKeyDown(KeyCode.B) || touchTradeQueued;
        touchTradeQueued = false;
        cycleCrop = Input.GetKeyDown(KeyCode.C) || touchCropQueued;
        touchCropQueued = false;
        fertilize = Input.GetKeyDown(KeyCode.F) || touchFertilizeQueued;
        touchFertilizeQueued = false;
        help = Input.GetKeyDown(KeyCode.H) || touchHelpQueued;
        touchHelpQueued = false;

        switchVehicle = 0;
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) switchVehicle = 1;
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) switchVehicle = 2;
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) switchVehicle = 3;
        else if (touchVehicleQueued) switchVehicle = -1;
        touchVehicleQueued = false;

        // a menu is open: the vehicle stops taking input (it rolls to a stop) and the game keys do nothing
        if (MenuOpen)
        {
            horizontal = vertical = 0f;
            up = down = left = right = false;
            interact = trade = cycleCrop = fertilize = false;
            switchVehicle = 0;
        }
    }

    public float Horizontal { get => horizontal; set => horizontal = value; }
    public float Vertical { get => vertical; set => vertical = value; }
    public float MouseX { get => mouseX; set => mouseX = value; }
    public float MouseY { get => mouseY; set => mouseY = value; }
    public bool Interact { get => interact; set => interact = value; }
    /// <summary>B key: buy or sell the field you are standing in (press twice to confirm).</summary>
    public bool Trade { get => trade; set => trade = value; }
    /// <summary>C key: pick the next crop to plant.</summary>
    public bool CycleCrop { get => cycleCrop; set => cycleCrop = value; }
    /// <summary>F key (FERT on touch): fertilize the field you are in while its crop grows.</summary>
    public bool Fertilize { get => fertilize; set => fertilize = value; }
    /// <summary>H key: open or close the help menu.</summary>
    public bool Help { get => help; set => help = value; }
    /// <summary>1 / 2 / 3 keys: drive the tractor, van or harvester (1 to 3). CAR touch button: -1 = next vehicle. 0 = no request.</summary>
    public int SwitchVehicle => switchVehicle;
    public bool Up { get => up; set => up = value; }
    public bool Right { get => right; set => right = value; }
    public bool Down { get => down; set => down = value; }
    public bool Left { get => left; set => left = value; }
    public bool DebugMode { get => debug; set => debug = value; }
}
