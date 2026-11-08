using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The controls pop-up in GameScene. A real object in the scene hierarchy ("Help Menu", made by
/// Tools > Build Help Menu), so its panels and texts can be moved, resized, restyled and reworded in the editor.
/// This script only opens and closes it. Press H, or the HELP button on touch, to open and close it; a tap
/// anywhere on it also closes it. There are two panels: <see cref="keyboardPanel"/> on PC and
/// <see cref="touchPanel"/> when the touch controls are on (phones, or Show Touch Controls in the editor).
/// The touch panel's steer and drive rows name the controls picked in SETTINGS (buttons, wheel or slider).
/// </summary>
public class HelpMenu : MonoBehaviour
{
    [Header("Scene objects (set by the builder; keep them set if you rearrange)")]
    [Tooltip("The part shown while open (full-screen dimmer holding both panels). Hidden when the game runs.")]
    public GameObject menuRoot;
    [Tooltip("Optional: a full-screen button on the menu; a tap on it closes the menu.")]
    public Button closeButton;
    [Tooltip("Shown on PC: the controls with their keys.")]
    public GameObject keyboardPanel;
    [Tooltip("Shown with touch controls: the controls with their buttons.")]
    public GameObject touchPanel;
    [Tooltip("Optional: the touch panel's Control text for steering (found in \"Row Steer left / right\" if empty).")]
    public TMP_Text touchSteerControl;
    [Tooltip("Optional: the touch panel's Control text for driving (found in \"Row Drive forward / back\" if empty).")]
    public TMP_Text touchDriveControl;

    private InputManager input;

    public bool IsOpen => menuRoot != null && menuRoot.activeSelf;

    void Start()
    {
        input = FindFirstObjectByType<InputManager>();
        if (menuRoot == null || keyboardPanel == null || touchPanel == null || input == null)
        {
            string missing = (menuRoot == null ? " Menu Root" : "") + (keyboardPanel == null ? " Keyboard Panel" : "")
                + (touchPanel == null ? " Touch Panel" : "") + (input == null ? " (no InputManager in the scene)" : "");
            Debug.LogError($"Help menu turned off: '{name}' is missing{missing}. Drag the objects back into its Help Menu slots, "
                + "or run Tools > Build Help Menu again.", this);
            enabled = false;
            return;
        }

        bool touch = input.TouchControlsActive;
        keyboardPanel.SetActive(!touch);
        touchPanel.SetActive(touch);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (touchSteerControl == null) touchSteerControl = FindControl("Row Steer left / right");
        if (touchDriveControl == null) touchDriveControl = FindControl("Row Drive forward / back");
        menuRoot.SetActive(false);
    }

    void Update()
    {
        // a menu opened while the help was up (Esc, O, the dealer): hide the help so it is not drawn over the menu.
        // No sound, the menu plays its own. H does nothing while the menu is open (InputManager clears it).
        if (input.MenuOpen)
        {
            if (menuRoot.activeSelf) menuRoot.SetActive(false);
            return;
        }
        if (!input.Help) return;
        if (!menuRoot.activeSelf) NameTouchControls();
        menuRoot.SetActive(!menuRoot.activeSelf);
        GameSounds.Play(menuRoot.activeSelf ? GameSounds.Sound.MenuOpen : GameSounds.Sound.MenuClose);
    }

    // the touch panel's Control text in the given row
    private TMP_Text FindControl(string row)
    {
        foreach (var t in touchPanel.GetComponentsInChildren<Transform>(true))
            if (t.name == row) return t.Find("Control")?.GetComponent<TMP_Text>();
        return null;
    }

    // steer and drive show what SETTINGS has picked
    private void NameTouchControls()
    {
        if (touchSteerControl != null)
            touchSteerControl.text = TouchSettings.Steering switch
            {
                SteeringMode.Wheel => "WHEEL",
                SteeringMode.Slider => "SLIDER",
                _ => "< / >",
            };
        if (touchDriveControl != null)
            touchDriveControl.text = TouchSettings.Pedals == PedalMode.Slider ? "PEDAL SLIDER" : "GAS / BRAKE";
    }

    public void Close()
    {
        if (menuRoot.activeSelf) GameSounds.Play(GameSounds.Sound.MenuClose);
        menuRoot.SetActive(false);
    }
}
