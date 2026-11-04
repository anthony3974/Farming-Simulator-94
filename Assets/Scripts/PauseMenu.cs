using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The pause menu in GameScene. Unlike the other menus it is a real object in the scene hierarchy
/// ("Pause Menu", made by Tools > Build Pause Menu), so its panel, buttons and texts can be moved, resized
/// and restyled in the editor. This script only shows and hides it and runs the buttons.
/// Esc (or the PAUSE button on touch, or the Android back button) opens and closes it. While paused the
/// game time stops (Time.timeScale 0): crops stop growing and vehicles stop.
/// MAIN MENU asks for a second press, because there is no saving yet and the farm is lost.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Scene objects (set by the builder; keep them set if you rearrange)")]
    [Tooltip("The part shown while paused (dimmer and panel). Hidden when the game runs.")]
    public GameObject menuRoot;
    public Button resumeButton;
    public Button mainMenuButton;
    [Tooltip("Optional: the MAIN MENU button's text, changed to ask for confirmation.")]
    public TMP_Text mainMenuLabel;
    [Tooltip("Optional: on-screen PAUSE button, shown only with touch controls.")]
    public Button pauseButton;

    [Header("Settings")]
    public string mainMenuSceneName = "MainMenu";
    [Tooltip("Show the PAUSE button on PC too (it is always shown with touch controls).")]
    public bool showPauseButtonOnPC = false;
    public string mainMenuText = "MAIN MENU";
    public string confirmText = "PRESS AGAIN: FARM NOT SAVED";
    public float confirmSeconds = 3f;

    private InputManager input;
    private GameCharacter player;
    private float confirmUntil = -1f; // unscaled time; -1 = not asking
    private bool leaving;

    public bool IsPaused => menuRoot != null && menuRoot.activeSelf;

    void Start()
    {
        input = FindFirstObjectByType<InputManager>();
        player = FindFirstObjectByType<GameCharacter>();
        if (menuRoot == null || resumeButton == null || mainMenuButton == null || input == null || player == null)
        {
            string missing = (menuRoot == null ? " Menu Root" : "") + (resumeButton == null ? " Resume Button" : "")
                + (mainMenuButton == null ? " Main Menu Button" : "") + (input == null ? " (no InputManager in the scene)" : "")
                + (player == null ? " (no GameCharacter in the scene)" : "");
            Debug.LogError($"Pause menu turned off: '{name}' is missing{missing}. Drag the objects back into its Pause Menu slots, "
                + "run Tools > Build Pause Menu again, or delete this object if you don't want a pause menu.", this);
            enabled = false;
            return;
        }

        resumeButton.onClick.AddListener(Resume);
        mainMenuButton.onClick.AddListener(MainMenuPressed);
        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(Pause);
            pauseButton.gameObject.SetActive(input.TouchControlsActive || showPauseButtonOnPC);
        }
        menuRoot.SetActive(false);
    }

    void Update()
    {
        if (leaving) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPaused) Resume();
            // Esc that just closed another menu (the dealer) should not also pause
            else if (!input.MenuOpen && !input.MenuJustClosed) Pause();
        }

        if (confirmUntil >= 0f && Time.unscaledTime > confirmUntil) ResetConfirm();
    }

    public void Pause()
    {
        if (IsPaused || input.MenuOpen) return;
        menuRoot.SetActive(true);
        input.MenuOpen = true;
        player.SetCursorLocked(false);
        Time.timeScale = 0f;
        ResetConfirm();
    }

    public void Resume()
    {
        if (!IsPaused) return;
        menuRoot.SetActive(false);
        input.MenuOpen = false;
        Time.timeScale = 1f;
        if (!input.TouchControlsActive) player.SetCursorLocked(true);
        ResetConfirm();
    }

    private void MainMenuPressed()
    {
        if (confirmUntil < 0f)
        {
            confirmUntil = Time.unscaledTime + confirmSeconds;
            if (mainMenuLabel != null) mainMenuLabel.text = confirmText;
            return;
        }

        leaving = true;
        Time.timeScale = 1f;
        input.MenuOpen = false;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void ResetConfirm()
    {
        confirmUntil = -1f;
        if (mainMenuLabel != null) mainMenuLabel.text = mainMenuText;
    }

    void OnDestroy()
    {
        // never leave the next scene frozen
        if (IsPaused) Time.timeScale = 1f;
    }
}
