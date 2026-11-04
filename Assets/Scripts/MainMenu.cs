using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The home menu (MainMenu scene, the first scene in the build). Everything is built here at runtime.
/// Two screens:
///   Home: background screenshot, title, PLAY, TERMS OF SERVICE (does nothing yet, a reminder for
///         publishing on Google Play), and the version number (Project Settings > Player > Version).
///   Play: NEW GAME (loads the game scene) and LOAD GAME (greyed out until save/load exists), BACK.
/// To set the background: drop the screenshot into Assets/Images, then drag it into "Background" on the
/// Main Menu object. It fills the screen (cropped, not stretched) and is darkened a little so the text reads.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Tooltip("The game screenshot shown behind the menu. Empty = a plain dark green background.")]
    public Texture background;
    [Range(0f, 1f), Tooltip("How much the background is darkened so the buttons and title stand out.")]
    public float backgroundDarken = 0.35f;
    public string title = "FARMING SIMULATOR 94";
    public string gameSceneName = "GameScene";

    private static readonly Color Gold = new Color32(255, 201, 51, 255);
    private static readonly Color Yellow = new Color32(255, 204, 0, 255);
    private static readonly Color Panel = new Color32(31, 31, 31, 235);
    private static readonly Color ButtonFill = new Color32(60, 45, 20, 255);
    private static readonly Color ButtonOff = new Color32(70, 70, 70, 255);
    private static readonly Color Placeholder = new Color32(38, 58, 30, 255);

    private GameObject homeScreen;
    private GameObject playScreen;
    private TMP_Text statusText;
    private bool loading;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        if (FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        Build();
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
        statusText.text = "";
    }

    private void NewGame()
    {
        if (loading) return;
        loading = true;
        statusText.text = "Loading...";
        SceneManager.LoadSceneAsync(gameSceneName);
    }

    // ---- building the UI ----

    private void Build()
    {
        var canvasGo = new GameObject("Menu Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        BuildBackground(canvasGo.transform);

        // everything else sits inside the phone's safe area (notches, rounded corners)
        var safe = new GameObject("Safe Area", typeof(RectTransform));
        safe.transform.SetParent(canvasGo.transform, false);
        FitSafeArea((RectTransform)safe.transform);

        // version number, bottom-left, on both screens
        var version = MakeText(safe.transform, "v" + Application.version, 30, Color.white, TextAlignmentOptions.BottomLeft, false);
        Place(version.rectTransform, Vector2.zero, new Vector2(40, 30), new Vector2(400, 50), Vector2.zero);

        // ---- home screen ----
        homeScreen = new GameObject("Home", typeof(RectTransform));
        homeScreen.transform.SetParent(safe.transform, false);
        Stretch((RectTransform)homeScreen.transform);

        var titleText = MakeText(homeScreen.transform, title, 110, Gold, TextAlignmentOptions.Center, true);
        titleText.outlineWidth = 0.2f;
        titleText.outlineColor = new Color32(40, 25, 0, 255);
        Place(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -230), new Vector2(1700, 180));

        var play = MakeButton(homeScreen.transform, "PLAY", 72, new Vector2(520, 150));
        Place((RectTransform)play.transform, new Vector2(0.5f, 0f), new Vector2(0, 250), new Vector2(520, 150));
        play.onClick.AddListener(ShowPlay);

        // Reminder for publishing on Google Play: link this to the real terms page then (Application.OpenURL).
        var terms = MakeButton(homeScreen.transform, "TERMS OF SERVICE", 26, new Vector2(320, 70));
        Place((RectTransform)terms.transform, new Vector2(1f, 0f), new Vector2(-200, 65), new Vector2(320, 70));

        // ---- play screen ----
        playScreen = new GameObject("Play", typeof(RectTransform));
        playScreen.transform.SetParent(safe.transform, false);
        Stretch((RectTransform)playScreen.transform);

        var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(playScreen.transform, false);
        border.GetComponent<Image>().color = Yellow;
        Place((RectTransform)border.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 640));

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(border.transform, false);
        panel.GetComponent<Image>().color = Panel;
        var panelRt = (RectTransform)panel.transform;
        Stretch(panelRt);
        panelRt.offsetMin = new Vector2(4, 4);
        panelRt.offsetMax = new Vector2(-4, -4);

        var playTitle = MakeText(panel.transform, "PLAY", 56, Gold, TextAlignmentOptions.Center, true);
        Place(playTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(700, 80));

        var newGame = MakeButton(panel.transform, "NEW GAME", 44, new Vector2(520, 110));
        Place((RectTransform)newGame.transform, new Vector2(0.5f, 1f), new Vector2(0, -200), new Vector2(520, 110));
        newGame.onClick.AddListener(NewGame);

        // greyed out until save/load (PLAN.md item 18) exists
        var load = MakeButton(panel.transform, "LOAD GAME", 44, new Vector2(520, 110));
        Place((RectTransform)load.transform, new Vector2(0.5f, 1f), new Vector2(0, -330), new Vector2(520, 110));
        load.interactable = false;
        load.GetComponent<Image>().color = ButtonOff;
        load.GetComponentInChildren<TMP_Text>().color = new Color(1f, 1f, 1f, 0.45f);
        load.GetComponent<Outline>().effectColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        statusText = MakeText(panel.transform, "", 30, Color.white, TextAlignmentOptions.Center, false);
        Place(statusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -420), new Vector2(700, 50));

        var back = MakeButton(panel.transform, "BACK", 32, new Vector2(240, 80));
        Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0, 70), new Vector2(240, 80));
        back.onClick.AddListener(ShowHome);
    }

    private void BuildBackground(Transform parent)
    {
        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(parent, false);
        Stretch((RectTransform)bg.transform);
        bg.GetComponent<Image>().color = Placeholder;

        if (background != null)
        {
            // fill the screen keeping the picture's shape (crops the edges instead of stretching)
            var pic = new GameObject("Screenshot", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
            pic.transform.SetParent(bg.transform, false);
            pic.GetComponent<RawImage>().texture = background;
            var fitter = pic.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = (float)background.width / background.height;
        }

        var dim = new GameObject("Darken", typeof(RectTransform), typeof(Image));
        dim.transform.SetParent(bg.transform, false);
        Stretch((RectTransform)dim.transform);
        var dimImage = dim.GetComponent<Image>();
        dimImage.color = new Color(0f, 0f, 0f, background != null ? backgroundDarken : 0f);
        dimImage.raycastTarget = false;
    }

    private static void FitSafeArea(RectTransform rt)
    {
        Rect s = Screen.safeArea;
        rt.anchorMin = new Vector2(s.xMin / Screen.width, s.yMin / Screen.height);
        rt.anchorMax = new Vector2(s.xMax / Screen.width, s.yMax / Screen.height);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // ---- UI helpers ----

    private static Button MakeButton(Transform parent, string text, float fontSize, Vector2 size)
    {
        var go = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        ((RectTransform)go.transform).sizeDelta = size;
        go.GetComponent<Image>().color = ButtonFill;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = Yellow;
        outline.effectDistance = new Vector2(3, -3);

        var label = MakeText(go.transform, text, fontSize, Gold, TextAlignmentOptions.Center, true);
        Stretch(label.rectTransform);
        label.raycastTarget = false;
        return go.GetComponent<Button>();
    }

    // anchor = the point of the parent the element is placed from; the element's centre sits at position
    // from it, unless a pivot is given (the version text uses its bottom-left corner)
    private static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static TMP_Text MakeText(Transform parent, string text, float size, Color color, TextAlignmentOptions align, bool bold)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        return t;
    }
}
