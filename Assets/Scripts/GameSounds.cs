using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The short one-shot sounds: button click, menu open / close, money in / out. Call
/// <c>GameSounds.Play(GameSounds.Sound.MoneyIn)</c> from anywhere; it does nothing if the sounds are missing.
///
/// It sets itself up: at start the prefab <c>Assets/Resources/GameSounds.prefab</c> (made by Tools > Game Sounds
/// Setup) is loaded and kept for the whole game (main menu and game scene), so nothing has to be added to a scene.
/// Every UI Button in each loaded scene clicks when pressed, except the touch driving controls.
///
/// Only one sound plays per frame, the most important one (money &gt; menu &gt; click), so pressing BUY plays
/// money out and not also the click and the menu closing. Sounds are 2D and still play while paused.
/// </summary>
public class GameSounds : MonoBehaviour
{
    /// <summary>In order of importance: a later one wins when two are asked for in the same frame.</summary>
    public enum Sound { Click, MenuClose, MenuOpen, MoneyOut, MoneyIn }

    [Header("Clips")]
    public AudioClip click;
    public AudioClip menuOpen;
    public AudioClip menuClose;
    public AudioClip moneyIn;
    public AudioClip moneyOut;

    [Header("Volume (0 to 1)")]
    [Tooltip("Everything below is multiplied by this.")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Tooltip("The click is recorded louder than the others (peak -2.5 dBFS against -8), so it is turned down.")]
    [Range(0f, 1f)] public float clickVolume = 0.5f;
    [Range(0f, 1f)] public float menuVolume = 0.8f;
    [Range(0f, 1f)] public float moneyVolume = 1f;

    private const string PrefabPath = "GameSounds"; // Assets/Resources/GameSounds.prefab

    private static GameSounds instance;
    private AudioSource source;
    private bool pending;
    private Sound pendingSound;

    /// <summary>Asks for a sound; it plays at the end of this frame unless a more important one is asked for too.</summary>
    public static void Play(Sound sound)
    {
        if (instance == null) return;
        if (!instance.pending || sound > instance.pendingSound) instance.pendingSound = sound;
        instance.pending = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        var prefab = Resources.Load<GameSounds>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("GameSounds: no Assets/Resources/GameSounds.prefab, so there are no click, menu or money sounds. " +
                             "Run Tools > Game Sounds Setup.");
            return;
        }
        instance = Instantiate(prefab);
        instance.name = "Game Sounds";
        DontDestroyOnLoad(instance.gameObject);
    }

    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }

    // every button in the new scene clicks (inactive ones too: the menus are hidden at the start)
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (button.gameObject.scene != scene) continue;
            if (button.GetComponent<TouchButtonInput>() != null) continue; // driving controls: no click while driving
            button.onClick.AddListener(() => Play(Sound.Click));
        }
    }

    void LateUpdate()
    {
        if (!pending) return;
        pending = false;
        var (clip, volume) = pendingSound switch
        {
            Sound.Click => (click, clickVolume),
            Sound.MenuOpen => (menuOpen, menuVolume),
            Sound.MenuClose => (menuClose, menuVolume),
            Sound.MoneyIn => (moneyIn, moneyVolume),
            _ => (moneyOut, moneyVolume),
        };
        if (clip != null) source.PlayOneShot(clip, volume * masterVolume);
    }
}
