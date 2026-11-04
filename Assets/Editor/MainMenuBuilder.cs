using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One-off editor tool: creates Assets/Scenes/MainMenu.unity with the whole menu as real scene objects
// (camera, EventSystem, canvas, screens, buttons, texts) and every MainMenu reference filled in, then makes
// it the first scene in Build Settings, GameScene second. Plain objects with no layout groups, so
// everything can be moved, resized and restyled in the editor afterwards.
// Use: Tools > Build Main Menu Scene. Delete this file once it has been run (git history).
//
// Main Camera, EventSystem
// Main Menu                 Canvas + MainMenu script
//   Background              plain dark green
//     Screenshot            RawImage: drag the screenshot into its Texture
//     Darken                black, a little see-through, so the text reads
//   Safe Area               fitted to the phone's safe area at runtime
//     Version               bottom-left
//     Home                  Title, Play Button, Terms Button
//     Play                  Border > Panel > Title, New Game Button, Load Game Button, Status, Back Button
public static class MainMenuBuilder
{
    const string ScenePath = "Assets/Scenes/MainMenu.unity";
    const string GameScenePath = "Assets/Scenes/GameScene.unity";

    static readonly Color Gold = new Color32(255, 201, 51, 255);
    static readonly Color Yellow = new Color32(255, 204, 0, 255);
    static readonly Color Panel = new Color32(31, 31, 31, 235);
    static readonly Color ButtonFill = new Color32(60, 45, 20, 255);
    static readonly Color ButtonOff = new Color32(70, 70, 70, 255);
    static readonly Color Placeholder = new Color32(38, 58, 30, 255);

    [MenuItem("Tools/Build Main Menu Scene")]
    static void Build()
    {
        if (System.IO.File.Exists(ScenePath) &&
            !EditorUtility.DisplayDialog("Main Menu", "MainMenu.unity already exists. Rebuild it? Any changes made to it are lost.", "Rebuild", "Cancel"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cam = new GameObject("Main Camera", typeof(Camera));
        cam.tag = "MainCamera";
        var c = cam.GetComponent<Camera>();
        c.clearFlags = CameraClearFlags.SolidColor;
        c.backgroundColor = Color.black;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var root = new GameObject("Main Menu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // ---- background ----
        var bg = NewUI("Background", root.transform, typeof(Image));
        Stretch(bg);
        bg.GetComponent<Image>().color = Placeholder;

        var shot = NewUI("Screenshot", bg, typeof(RawImage), typeof(AspectRatioFitter));
        var fitter = shot.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; // fills the screen, crops the edges
        fitter.aspectRatio = 16f / 9f;
        shot.GetComponent<RawImage>().raycastTarget = false;

        var dark = NewUI("Darken", bg, typeof(Image));
        Stretch(dark);
        dark.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);
        dark.GetComponent<Image>().raycastTarget = false;

        // ---- safe area ----
        var safe = NewUI("Safe Area", root.transform);
        Stretch(safe);

        var version = MakeText(safe, "Version", "v" + PlayerSettings.bundleVersion, 30, Color.white, false);
        version.alignment = TextAlignmentOptions.BottomLeft;
        Place(version.rectTransform, Vector2.zero, new Vector2(40, 30), new Vector2(400, 50), Vector2.zero);

        // ---- home screen ----
        var home = NewUI("Home", safe);
        Stretch(home);

        var title = MakeText(home, "Title", "FARMING SIMULATOR 94", 110, Gold, true);
        Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -230), new Vector2(1700, 180));

        var (play, _) = MakeButton(home, "Play Button", "PLAY", 72);
        Place((RectTransform)play.transform, new Vector2(0.5f, 0f), new Vector2(0, 250), new Vector2(520, 150));

        var (terms, _) = MakeButton(home, "Terms Button", "TERMS OF SERVICE", 26);
        Place((RectTransform)terms.transform, new Vector2(1f, 0f), new Vector2(-200, 65), new Vector2(320, 70));

        // ---- play screen ----
        var playScreen = NewUI("Play", safe);
        Stretch(playScreen);

        var border = NewUI("Border", playScreen, typeof(Image));
        border.GetComponent<Image>().color = Yellow;
        Place(border, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 640));

        var panel = NewUI("Panel", border, typeof(Image));
        panel.GetComponent<Image>().color = Panel;
        Stretch(panel);
        panel.offsetMin = new Vector2(4, 4);
        panel.offsetMax = new Vector2(-4, -4);

        var playTitle = MakeText(panel, "Title", "PLAY", 56, Gold, true);
        Place(playTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 240), new Vector2(700, 80));

        var (newGame, _) = MakeButton(panel, "New Game Button", "NEW GAME", 44);
        Place((RectTransform)newGame.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 110), new Vector2(520, 110));

        // greyed out until save/load exists: tick Interactable and set the colours back when it does
        var (load, loadLabel) = MakeButton(panel, "Load Game Button", "LOAD GAME", 44);
        Place((RectTransform)load.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(520, 110));
        load.interactable = false;
        load.GetComponent<Image>().color = ButtonOff;
        load.GetComponent<Outline>().effectColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        loadLabel.color = new Color(1f, 1f, 1f, 0.45f);

        var status = MakeText(panel, "Status", "", 30, Color.white, false);
        Place(status.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -110), new Vector2(700, 50));

        var (back, _) = MakeButton(panel, "Back Button", "BACK", 32);
        Place((RectTransform)back.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -240), new Vector2(240, 80));

        playScreen.gameObject.SetActive(false); // the script shows it after PLAY

        // ---- script and references ----
        var menu = root.AddComponent<MainMenu>();
        menu.homeScreen = home.gameObject;
        menu.playScreen = playScreen.gameObject;
        menu.playButton = play;
        menu.termsButton = terms;
        menu.newGameButton = newGame;
        menu.loadGameButton = load;
        menu.backButton = back;
        menu.versionText = version;
        menu.statusText = status;
        menu.screenshot = shot.GetComponent<RawImage>();
        menu.safeArea = safe;

        EditorSceneManager.SaveScene(scene, ScenePath);

        // MainMenu first (it is what the game opens with), then GameScene, then anything else already listed
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true),
        };
        scenes.AddRange(EditorBuildSettings.scenes.Where(s => s.path != ScenePath && s.path != GameScenePath));
        EditorBuildSettings.scenes = scenes.ToArray();

        Selection.activeGameObject = shot.gameObject;
        Debug.Log("MainMenu scene built and put first in Build Settings. Drag your screenshot into the Texture of Main Menu > Background > Screenshot, then save.");
    }

    static RectTransform NewUI(string name, Transform parent, params System.Type[] components)
    {
        var go = new GameObject(name, components.Prepend(typeof(RectTransform)).ToArray());
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static (Button, TMP_Text) MakeButton(Transform parent, string name, string text, float fontSize)
    {
        var rt = NewUI(name, parent, typeof(Image), typeof(Button), typeof(Outline));
        rt.GetComponent<Image>().color = ButtonFill;
        var outline = rt.GetComponent<Outline>();
        outline.effectColor = Yellow;
        outline.effectDistance = new Vector2(3, -3);

        var label = MakeText(rt, "Label", text, fontSize, Gold, true);
        Stretch(label.rectTransform);
        label.raycastTarget = false;
        return (rt.GetComponent<Button>(), label);
    }

    static TMP_Text MakeText(Transform parent, string name, string text, float size, Color color, bool bold)
    {
        var t = NewUI(name, parent, typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        return t;
    }

    // anchor = the point of the parent it is placed from; its centre sits at position from there unless a pivot is given
    static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
