using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The SETTINGS panel ("Settings Menu" canvas in GameScene, made by Tools > Build Touch Controls). Opened by the
/// SETTINGS touch button below HELP. Not the pause menu: the game keeps running (crops keep growing); only
/// driving and the game keys stop while it is open, like the dealer. Two columns: STEERING (buttons, wheel,
/// slider) and BUTTONS (square, rounded). The choice shows straight away and is remembered (<see cref="TouchSettings"/>).
/// CLOSE, a tap outside the panel, or Esc / Android back closes it.
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    [Header("Scene objects (set by the builder; keep them set if you rearrange)")]
    [Tooltip("The part shown while open (dimmer and panel).")]
    public GameObject menuRoot;
    [Tooltip("The SETTINGS touch button (in the Touch Controls canvas).")]
    public Button settingsButton;
    public Button closeButton;
    [Tooltip("Optional: the full-screen dimmer; a tap on it closes the panel.")]
    public Button dimmerButton;

    [Header("Options")]
    public Button steeringButtonsOption;
    public Button steeringWheelOption;
    public Button steeringSliderOption;
    public Button squareOption;
    public Button roundedOption;

    [Header("Look")]
    [Tooltip("Fill of the option that is picked.")]
    public Color selectedFill = new Color32(110, 85, 10, 255);
    [Tooltip("Fill of the other options (the UI Button prefab's brown).")]
    public Color normalFill = new Color32(60, 45, 20, 255);

    private InputManager input;

    public bool IsOpen => menuRoot != null && menuRoot.activeSelf;

    void Start()
    {
        input = FindFirstObjectByType<InputManager>();
        if (menuRoot == null || input == null)
        {
            Debug.LogError($"Settings menu turned off: '{name}' is missing its Menu Root (or there is no InputManager). Run Tools > Build Touch Controls again.", this);
            enabled = false;
            return;
        }

        if (settingsButton != null) settingsButton.onClick.AddListener(Open);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (dimmerButton != null) dimmerButton.onClick.AddListener(Close);
        Hook(steeringButtonsOption, () => TouchSettings.Steering = SteeringMode.Buttons);
        Hook(steeringWheelOption, () => TouchSettings.Steering = SteeringMode.Wheel);
        Hook(steeringSliderOption, () => TouchSettings.Steering = SteeringMode.Slider);
        Hook(squareOption, () => TouchSettings.Rounded = false);
        Hook(roundedOption, () => TouchSettings.Rounded = true);

        menuRoot.SetActive(false);
        Refresh();
    }

    private void Hook(Button option, UnityEngine.Events.UnityAction pick)
    {
        if (option == null) return;
        option.onClick.AddListener(pick);
        option.onClick.AddListener(Refresh);
    }

    void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    public void Open()
    {
        if (IsOpen || input.MenuOpen) return; // not over the pause menu or the dealer
        menuRoot.SetActive(true);
        input.MenuOpen = true;
        Refresh();
    }

    public void Close()
    {
        if (!IsOpen) return;
        menuRoot.SetActive(false);
        input.MenuOpen = false;
    }

    void OnDisable()
    {
        if (IsOpen && input != null) input.MenuOpen = false;
    }

    // the picked option of each column shows the selected fill
    private void Refresh()
    {
        var steering = TouchSettings.Steering;
        bool rounded = TouchSettings.Rounded;
        Mark(steeringButtonsOption, steering == SteeringMode.Buttons);
        Mark(steeringWheelOption, steering == SteeringMode.Wheel);
        Mark(steeringSliderOption, steering == SteeringMode.Slider);
        Mark(squareOption, !rounded);
        Mark(roundedOption, rounded);
    }

    private void Mark(Button option, bool selected)
    {
        if (option == null) return;
        var fill = option.transform.Find("Fill")?.GetComponent<Image>();
        if (fill != null) fill.color = selected ? selectedFill : normalFill;
    }
}
