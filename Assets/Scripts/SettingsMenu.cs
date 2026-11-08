using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The SETTINGS panel ("Settings Menu" canvas in GameScene, made by Tools > Build Touch Controls, tabs added by
/// Tools > Settings: Game Tab). Opened by the SETTINGS touch button below HELP, or the O key on PC. Not the pause
/// menu: the game keeps running (crops keep growing); only driving and the game keys stop while it is open, like
/// the dealer. Two tabs:
/// - CONTROLS: STEERING (buttons, wheel, slider) and, on the right, BUTTON SHAPE (square, rounded) and PEDALS
///   (GAS / BRAKE buttons, slider), remembered by <see cref="TouchSettings"/>.
/// - GAME: MIRRORS, MINIMAP and DAY / NIGHT CYCLE (on / off), and DAY LENGTH and NIGHT LENGTH (- / + in minutes,
///   default 10 and 5), remembered by <see cref="GameSettings"/>. The lengths are greyed out while the cycle is off.
/// Choices show straight away. CLOSE, a tap outside the panel, O, or Esc / Android back closes it.
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
    public Button pedalButtonsOption;
    public Button pedalSliderOption;

    [Header("Tabs (optional: without them the panel shows only the controls)")]
    public Button controlsTabButton;
    public Button gameTabButton;
    [Tooltip("The CONTROLS tab's content (the touch options).")]
    public GameObject controlsTab;
    [Tooltip("The GAME tab's content.")]
    public GameObject gameTab;

    [Header("Game tab")]
    public Button mirrorsOnOption;
    public Button mirrorsOffOption;
    public Button minimapOnOption;
    public Button minimapOffOption;
    public Button cycleOnOption;
    public Button cycleOffOption;
    public Button dayLessButton;
    public Button dayMoreButton;
    public TMP_Text dayLengthText;
    public Button nightLessButton;
    public Button nightMoreButton;
    public TMP_Text nightLengthText;
    [Tooltip("Text of the day and night lengths; {0} is the minutes.")]
    public string lengthFormat = "{0} MIN";

    [Header("Keys")]
    [Tooltip("Opens and closes the panel on PC.")]
    public KeyCode settingsKey = KeyCode.O;

    [Header("Look")]
    [Tooltip("Fill of the option that is picked.")]
    public Color selectedFill = new Color32(110, 85, 10, 255);
    [Tooltip("Fill of the other options (the UI Button prefab's brown).")]
    public Color normalFill = new Color32(60, 45, 20, 255);
    [Tooltip("Fill of a button that cannot be used (the lengths while the cycle is off).")]
    public Color disabledFill = new Color32(70, 70, 70, 255);
    [Tooltip("The gold text colour.")]
    public Color textColor = new Color32(255, 201, 51, 255);
    [Tooltip("Text colour while greyed out.")]
    public Color disabledTextColor = new Color32(140, 140, 140, 255);

    private InputManager input;
    private GameCharacter player; // frees the mouse while open on PC, so the options can be clicked
    private bool showGameTab; // the tab last looked at, kept while the game runs

    public bool IsOpen => menuRoot != null && menuRoot.activeSelf;

    void Start()
    {
        input = FindFirstObjectByType<InputManager>();
        player = FindFirstObjectByType<GameCharacter>();
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
        Hook(pedalButtonsOption, () => TouchSettings.Pedals = PedalMode.Buttons);
        Hook(pedalSliderOption, () => TouchSettings.Pedals = PedalMode.Slider);

        Hook(controlsTabButton, () => showGameTab = false);
        Hook(gameTabButton, () => showGameTab = true);
        Hook(mirrorsOnOption, () => GameSettings.Mirrors = true);
        Hook(mirrorsOffOption, () => GameSettings.Mirrors = false);
        Hook(minimapOnOption, () => GameSettings.Minimap = true);
        Hook(minimapOffOption, () => GameSettings.Minimap = false);
        Hook(cycleOnOption, () => GameSettings.DayNightCycle = true);
        Hook(cycleOffOption, () => GameSettings.DayNightCycle = false);
        Hook(dayLessButton, () => GameSettings.DayMinutes = GameSettings.Step(GameSettings.DayMinutes, -1));
        Hook(dayMoreButton, () => GameSettings.DayMinutes = GameSettings.Step(GameSettings.DayMinutes, 1));
        Hook(nightLessButton, () => GameSettings.NightMinutes = GameSettings.Step(GameSettings.NightMinutes, -1));
        Hook(nightMoreButton, () => GameSettings.NightMinutes = GameSettings.Step(GameSettings.NightMinutes, 1));

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
        else if (settingsKey != KeyCode.None && Input.GetKeyDown(settingsKey))
        {
            if (IsOpen) Close();
            else Open();
        }
    }

    public void Open()
    {
        if (IsOpen || input.MenuOpen) return; // not over the pause menu or the dealer
        menuRoot.SetActive(true);
        input.MenuOpen = true;
        if (player != null) player.SetCursorLocked(false); // like the pause menu and the dealer
        Refresh();
        GameSounds.Play(GameSounds.Sound.MenuOpen);
    }

    public void Close()
    {
        if (!IsOpen) return;
        menuRoot.SetActive(false);
        input.MenuOpen = false;
        if (player != null && !input.TouchControlsActive) player.SetCursorLocked(true);
        GameSounds.Play(GameSounds.Sound.MenuClose);
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
        Mark(pedalButtonsOption, TouchSettings.Pedals == PedalMode.Buttons);
        Mark(pedalSliderOption, TouchSettings.Pedals == PedalMode.Slider);

        // tabs (without a GAME tab in the scene, the controls are always shown)
        bool game = showGameTab && gameTab != null;
        if (controlsTab != null) controlsTab.SetActive(!game);
        if (gameTab != null) gameTab.SetActive(game);
        Mark(controlsTabButton, !game);
        Mark(gameTabButton, game);

        Mark(mirrorsOnOption, GameSettings.Mirrors);
        Mark(mirrorsOffOption, !GameSettings.Mirrors);
        Mark(minimapOnOption, GameSettings.Minimap);
        Mark(minimapOffOption, !GameSettings.Minimap);
        bool cycle = GameSettings.DayNightCycle;
        Mark(cycleOnOption, cycle);
        Mark(cycleOffOption, !cycle);
        Length(dayLessButton, dayMoreButton, dayLengthText, GameSettings.DayMinutes, cycle);
        Length(nightLessButton, nightMoreButton, nightLengthText, GameSettings.NightMinutes, cycle);
    }

    // a - / + row: the minutes, the buttons greyed out at the ends of the range and while the cycle is off
    private void Length(Button less, Button more, TMP_Text text, int minutes, bool usable)
    {
        int[] steps = GameSettings.LengthSteps;
        Usable(less, usable && minutes > steps[0]);
        Usable(more, usable && minutes < steps[steps.Length - 1]);
        if (text != null)
        {
            text.text = string.Format(lengthFormat, minutes);
            text.color = usable ? textColor : disabledTextColor;
        }
    }

    private void Usable(Button button, bool usable)
    {
        if (button == null) return;
        button.interactable = usable;
        var fill = button.transform.Find("Fill")?.GetComponent<Image>();
        if (fill != null) fill.color = usable ? normalFill : disabledFill;
        var label = button.transform.Find("Label")?.GetComponent<TMP_Text>();
        if (label != null) label.color = usable ? textColor : disabledTextColor;
    }

    private void Mark(Button option, bool selected)
    {
        if (option == null) return;
        var fill = option.transform.Find("Fill")?.GetComponent<Image>();
        if (fill != null) fill.color = selected ? selectedFill : normalFill;
    }
}
