using TMPro;
using UnityEngine;

/// <summary>
/// The message box in the middle of the screen: building prompts ("Press E to sell Wheat"), sale messages,
/// field messages, vehicle switching. A real object in the scene hierarchy ("Message Box", made by
/// Tools > Build Message Box), so it can be moved, resized and restyled in the editor. This script only shows
/// and hides it, sets the text and (with <see cref="fitHeightToText"/>) makes the box as tall as the text.
/// Everything uses the one in the scene through <see cref="Instance"/>.
/// </summary>
public class ScreenPrompt : MonoBehaviour
{
    [Header("Scene objects (set by the builder; keep them set if you rearrange)")]
    [Tooltip("The box shown with a message (yellow border with the panel and text inside). Hidden when empty.")]
    public RectTransform box;
    public TMP_Text label;

    [Tooltip("Make the box as tall as its text (one line, two lines, ...); its width and position stay as set.")]
    public bool fitHeightToText = true;

    private float flashUntil;
    private float chrome = -1f; // box height minus text height (border + padding), measured once
    private static ScreenPrompt instance;
    private static bool warned;

    /// <summary>The message box in the scene.</summary>
    public static ScreenPrompt Instance
    {
        get
        {
            if (instance != null) return instance;
            instance = FindFirstObjectByType<ScreenPrompt>();
            if (instance == null)
            {
                // keep the game running without a box: messages then only go to the Console
                if (!warned) Debug.LogError("No Message Box in the scene: run Tools > Build Message Box in GameScene. Messages go to the Console only.");
                warned = true;
                instance = new GameObject("Message Box (missing)").AddComponent<ScreenPrompt>();
            }
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; warned = false; }

    void Awake()
    {
        if (instance == null) instance = this;
        if (box != null && label != null) box.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private bool HasBox => box != null && label != null;

    /// <summary>True while a timed message from <see cref="Flash"/> is showing.</summary>
    public bool IsFlashing => flashUntil > 0;

    public bool Visible => HasBox && box.gameObject.activeSelf;
    public string CurrentText => HasBox ? label.text : "";

    /// <summary>Show a message for a few seconds, then hide.</summary>
    public void Flash(string text, float seconds)
    {
        Show(text);
        flashUntil = Time.time + seconds;
    }

    void Update()
    {
        if (flashUntil > 0 && Time.time >= flashUntil) { flashUntil = 0; Hide(); }
    }

    public void Show(string text)
    {
        if (!HasBox) { Debug.Log(text); return; }
        label.text = text;
        if (!box.gameObject.activeSelf) box.gameObject.SetActive(true);
        if (fitHeightToText) FitHeight();
    }

    public void Hide()
    {
        if (HasBox) box.gameObject.SetActive(false);
    }

    // the text sits inside the box with a fixed margin (border + padding), so box height = text height + margin
    private void FitHeight()
    {
        var labelRt = label.rectTransform;
        if (chrome < 0f) chrome = Mathf.Max(0f, box.rect.height - labelRt.rect.height);
        float textHeight = label.GetPreferredValues(label.text, labelRt.rect.width, 0f).y;
        box.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textHeight + chrome);
    }
}
