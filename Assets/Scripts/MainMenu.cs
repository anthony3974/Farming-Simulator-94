using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The home menu (MainMenu scene, the first scene in the build). The menu is made of real objects in the
/// scene hierarchy (made by Tools > Build Main Menu Scene), so it can be rearranged and restyled in the
/// editor. This script only switches between the two screens and runs the buttons:
///   Home: background screenshot, title, PLAY, TERMS OF SERVICE (does nothing yet, a reminder for
///         publishing on Google Play), and the version number (Project Settings > Player > Version).
///   Play: NEW GAME (a new farm; asks for a second press when a saved farm would be replaced),
///         LOAD GAME (the saved farm, see SaveGame.cs; greyed out when there is no save), BACK.
/// Background: drop the screenshot into Assets/Images and drag it into the Texture of the
/// "Screenshot" object (Main Menu > Background > Screenshot). It fills the screen, cropped, not stretched.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Scene objects (set by the builder; keep them set if you rearrange)")]
    public GameObject homeScreen;
    public GameObject playScreen;
    public Button playButton;
    public Button termsButton;
    public Button newGameButton;
    public Button loadGameButton;
    public Button backButton;
    [Tooltip("Shows the version, e.g. v0.2, from Project Settings > Player > Version.")]
    public TMP_Text versionText;
    [Tooltip("Shows 'Loading...' after NEW GAME or LOAD GAME, and the NEW GAME warning.")]
    public TMP_Text statusText;
    [Tooltip("The background screenshot. Hidden when it has no texture (the plain background shows).")]
    public RawImage screenshot;
    [Tooltip("Optional: kept inside the phone's safe area (notches, rounded corners).")]
    public RectTransform safeArea;

    [Header("Settings")]
    public string gameSceneName = "GameScene";
    [Tooltip("Shown after the first NEW GAME press when there is a saved farm.")]
    public string newGameWarning = "Press again to replace your saved farm";

    private bool loading;
    private bool newGameAsked;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        if (homeScreen == null || playScreen == null || playButton == null || newGameButton == null || backButton == null)
        {
            Debug.LogError("MainMenu is missing a reference (homeScreen, playScreen, playButton, newGameButton or backButton).", this);
            enabled = false;
            return;
        }

        playButton.onClick.AddListener(ShowPlay);
        newGameButton.onClick.AddListener(NewGame);
        backButton.onClick.AddListener(ShowHome);
        // TERMS OF SERVICE: nothing yet. When publishing: termsButton.onClick.AddListener(() => Application.OpenURL("https://..."));
        // LOAD GAME: only with a save (the button's greyed-out look is its disabled colour)
        if (loadGameButton != null)
        {
            loadGameButton.onClick.AddListener(LoadGame);
            loadGameButton.interactable = SaveGame.HasSave;
        }

        if (versionText != null) versionText.text = "v" + Application.version;

        if (screenshot != null)
        {
            bool hasPicture = screenshot.texture != null;
            screenshot.gameObject.SetActive(hasPicture);
            var fitter = screenshot.GetComponent<AspectRatioFitter>();
            if (hasPicture && fitter != null) fitter.aspectRatio = (float)screenshot.texture.width / screenshot.texture.height;
        }

        if (safeArea != null)
        {
            Rect s = Screen.safeArea;
            safeArea.anchorMin = new Vector2(s.xMin / Screen.width, s.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(s.xMax / Screen.width, s.yMax / Screen.height);
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
        }

        ShowHome();
    }

    void Update()
    {
        // Escape (or the Android back button) goes back from the play screen
        if (!loading && playScreen.activeSelf && Input.GetKeyDown(KeyCode.Escape)) ShowHome();
    }

    private void ShowHome()
    {
        homeScreen.SetActive(true);
        playScreen.SetActive(false);
    }

    private void ShowPlay()
    {
        homeScreen.SetActive(false);
        playScreen.SetActive(true);
        newGameAsked = false;
        if (statusText != null) statusText.text = "";
    }

    private void NewGame()
    {
        if (loading) return;
        // the new farm's first save replaces the old one, so ask once
        if (SaveGame.HasSave && !newGameAsked)
        {
            newGameAsked = true;
            if (statusText != null) statusText.text = newGameWarning;
            return;
        }
        StartGame(load: false);
    }

    private void LoadGame()
    {
        if (loading || !SaveGame.HasSave) return;
        StartGame(load: true);
    }

    private void StartGame(bool load)
    {
        loading = true;
        SaveGame.LoadRequested = load;
        if (statusText != null) statusText.text = "Loading...";
        SceneManager.LoadSceneAsync(gameSceneName);
    }
}
