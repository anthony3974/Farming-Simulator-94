using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One-off editor tool (owner, 2026-11-06: touch rework). Builds the touch controls in the open scene (GameScene)
// as real objects, replacing the buttons TouchControls.cs used to build in code. Every button is a copy of the
// UI Button prefab. Plain positioned objects, no layout groups, so everything can be moved and resized freely.
// Use: open GameScene, Tools > Build Touch Controls, save the scene. Delete this file afterwards (git history).
//
// Touch Controls         Canvas (sort 50) + TouchControls: hidden on PC unless Show Touch Controls is ticked
//   Steering Buttons     < >  (bottom-left, right of the speedometer)
//   Steering Wheel       touch area + SteeringWheel > Wheel (the picture that turns)
//   Steering Slider      bar + SteeringSlider > Fill, Middle, Knob
//   Pedals               GAS BRAKE (bottom-right, unchanged)
//   Actions              USE BUY/SELL CROP CAR
//   Help Button          top-left
//   Settings Button      below HELP
// Settings Menu          Canvas (sort 58) + SettingsMenu
//   Menu                 turned off in the editor (tick it to work on it)
//     Dimmer             tap closes
//     Border > Panel     Title, STEERING column (3 options), BUTTON SHAPE column (2 options), Close Button
public static class TouchControlsBuilder
{
    const string ControlsName = "Touch Controls";
    const string SettingsName = "Settings Menu";
    const string ButtonPrefabPath = "Assets/Prefabs/UI/UI Button.prefab";
    const string SpriteFolder = "Assets/Images/Touch/";

    static readonly Color Gold = new Color32(255, 201, 51, 255);
    static readonly Color Yellow = new Color32(255, 204, 0, 255);
    static readonly Color Brown = new Color32(60, 45, 20, 255);
    static readonly Color PanelGrey = new Color32(31, 31, 31, 255);

    static GameObject buttonPrefab;

    [MenuItem("Tools/Build Touch Controls")]
    static void Build()
    {
        if (Object.FindFirstObjectByType<InputManager>() == null)
        {
            EditorUtility.DisplayDialog("Touch Controls", "No InputManager in the open scene. Open GameScene first.", "OK");
            return;
        }
        buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPrefabPath);
        if (buttonPrefab == null)
        {
            EditorUtility.DisplayDialog("Touch Controls", $"Missing {ButtonPrefabPath}.", "OK");
            return;
        }

        var oldControls = GameObject.Find(ControlsName);
        var oldSettings = GameObject.Find(SettingsName);
        if (oldControls != null || oldSettings != null)
        {
            if (!EditorUtility.DisplayDialog("Touch Controls", "The scene already has Touch Controls / Settings Menu. Replace them? Changes made to them are lost.", "Replace", "Cancel"))
                return;
            if (oldControls != null) Undo.DestroyObjectImmediate(oldControls);
            if (oldSettings != null) Undo.DestroyObjectImmediate(oldSettings);
        }

        var roundedBorder = ImportSprite("RoundedBorder.png", 48);
        var roundedFill = ImportSprite("RoundedFill.png", 38);
        var wheelSprite = ImportSprite("SteeringWheel.png", 0);

        EnsureEventSystem();
        var shaped = new List<Image>();

        // ---------- Touch Controls ----------
        var controls = MakeCanvas(ControlsName, 50);
        var bottomLeft = new Vector2(0, 0);
        var bottomRight = new Vector2(1, 0);
        var topLeft = new Vector2(0, 1);

        // steering: < > buttons, bottom-left, to the right of the 260 px speedometer dial (x 20 to 280)
        var steerButtons = MakeGroup(controls.transform, "Steering Buttons");
        shaped.Add(TouchBtn(steerButtons, "Left Button", "<", TouchButton.Left, bottomLeft, new Vector2(310, 60), 150));
        shaped.Add(TouchBtn(steerButtons, "Right Button", ">", TouchButton.Right, bottomLeft, new Vector2(490, 60), 150));

        // steering wheel: the touch area stays still, the Wheel child turns
        var wheelArea = new GameObject("Steering Wheel", typeof(RectTransform), typeof(Image), typeof(SteeringWheel));
        wheelArea.transform.SetParent(controls.transform, false);
        Place((RectTransform)wheelArea.transform, bottomLeft, new Vector2(300, 20), new Vector2(320, 320));
        wheelArea.GetComponent<Image>().color = new Color(0, 0, 0, 0); // invisible, catches the touches
        var wheelPic = new GameObject("Wheel", typeof(RectTransform), typeof(Image));
        wheelPic.transform.SetParent(wheelArea.transform, false);
        Stretch((RectTransform)wheelPic.transform, 10);
        var wheelImage = wheelPic.GetComponent<Image>();
        wheelImage.sprite = wheelSprite;
        wheelImage.preserveAspect = true;
        wheelImage.raycastTarget = false;
        var wheel = wheelArea.GetComponent<SteeringWheel>();
        wheel.wheel = wheelImage;

        // steering slider: yellow bar with a brown fill, a centre mark and a knob that follows the finger
        var slider = new GameObject("Steering Slider", typeof(RectTransform), typeof(Image), typeof(SteeringSlider));
        slider.transform.SetParent(controls.transform, false);
        Place((RectTransform)slider.transform, bottomLeft, new Vector2(310, 60), new Vector2(560, 150));
        slider.GetComponent<Image>().color = Yellow;
        MakeFill(slider.transform);
        var middle = new GameObject("Middle", typeof(RectTransform), typeof(Image));
        middle.transform.SetParent(slider.transform, false);
        Center((RectTransform)middle.transform, Vector2.zero, new Vector2(4, 110));
        middle.GetComponent<Image>().color = new Color(Yellow.r, Yellow.g, Yellow.b, 0.6f);
        middle.GetComponent<Image>().raycastTarget = false;
        var knob = new GameObject("Knob", typeof(RectTransform), typeof(Image));
        knob.transform.SetParent(slider.transform, false);
        Center((RectTransform)knob.transform, Vector2.zero, new Vector2(130, 130));
        knob.GetComponent<Image>().color = Yellow;
        knob.GetComponent<Image>().raycastTarget = false; // the bar takes the touch, not the knob
        var knobFill = MakeFill(knob.transform);
        var knobLabel = MakeText(knob.transform, "Label", "< >", 44, Gold, true);
        Stretch(knobLabel.rectTransform, 0);
        knobLabel.raycastTarget = false;
        var sliderScript = slider.GetComponent<SteeringSlider>();
        sliderScript.knob = (RectTransform)knob.transform;
        sliderScript.knobFill = knobFill;
        shaped.Add(slider.GetComponent<Image>());
        shaped.Add(knob.GetComponent<Image>());

        // right side: 130 px buttons, two columns, three rows (owner, 2026-11-06):
        //   CROP      CAR
        //   BUY SELL  GAS
        //   USE       BRAKE
        const float right = 60, left = 200, row1 = 295, row2 = 435, row3 = 575, btn = 130;
        var pedals = MakeGroup(controls.transform, "Pedals");
        shaped.Add(TouchBtn(pedals, "Gas Button", "GAS", TouchButton.Gas, bottomRight, new Vector2(-right, row2), btn));
        shaped.Add(TouchBtn(pedals, "Brake Button", "BRAKE", TouchButton.Brake, bottomRight, new Vector2(-right, row1), btn));
        var actions = MakeGroup(controls.transform, "Actions");
        shaped.Add(TouchBtn(actions, "Use Button", "USE", TouchButton.Interact, bottomRight, new Vector2(-left, row1), btn));
        shaped.Add(TouchBtn(actions, "Buy Sell Button", "BUY\nSELL", TouchButton.Trade, bottomRight, new Vector2(-left, row2), btn));
        shaped.Add(TouchBtn(actions, "Crop Button", "CROP", TouchButton.CycleCrop, bottomRight, new Vector2(-left, row3), btn));
        shaped.Add(TouchBtn(actions, "Car Button", "CAR", TouchButton.Vehicle, bottomRight, new Vector2(-right, row3), btn));

        // top-left: HELP (as before) and SETTINGS below it (PAUSE sits to the right of HELP, in the Pause Menu)
        shaped.Add(TouchBtn(controls.transform, "Help Button", "HELP", TouchButton.Help, topLeft, new Vector2(40, -40), 110));
        var settingsButtonGo = PrefabButton(controls.transform, "Settings Button", "SETTINGS", topLeft, new Vector2(40, -170), new Vector2(110, 110));
        shaped.Add(settingsButtonGo.GetComponent<Image>());

        // the PAUSE button follows the shape too
        var pause = Object.FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include);
        if (pause != null && pause.pauseButton != null) shaped.Add(pause.pauseButton.GetComponent<Image>());

        var tc = controls.AddComponent<TouchControls>();
        tc.steeringButtons = steerButtons.gameObject;
        tc.steeringWheel = wheelArea;
        tc.steeringSlider = slider;
        tc.shapedParts = shaped;
        tc.roundedBorder = roundedBorder;
        tc.roundedFill = roundedFill;
        // only the picked steering shows in the game; in the editor all three show, the wheel and slider
        // are turned off here so they do not cover each other (tick them to work on them)
        wheelArea.SetActive(false);
        slider.SetActive(false);

        // ---------- Settings Menu ----------
        var settings = MakeCanvas(SettingsName, 58);
        var menu = MakeGroup(settings.transform, "Menu");
        var dimmer = new GameObject("Dimmer", typeof(RectTransform), typeof(Image), typeof(Button));
        dimmer.transform.SetParent(menu, false);
        Stretch((RectTransform)dimmer.transform, 0);
        dimmer.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        dimmer.GetComponent<Button>().transition = Selectable.Transition.None;

        var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(menu, false);
        border.GetComponent<Image>().color = Yellow;
        Center((RectTransform)border.transform, Vector2.zero, new Vector2(980, 640));
        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(border.transform, false);
        panel.GetComponent<Image>().color = PanelGrey;
        Stretch((RectTransform)panel.transform, 4);

        var title = MakeText(panel.transform, "Title", "SETTINGS", 56, Gold, true);
        Center(title.rectTransform, new Vector2(0, 250), new Vector2(800, 80));

        const float leftX = -235, rightX = 235;
        var optionSize = new Vector2(400, 95);
        var steeringHeader = MakeText(panel.transform, "Steering Header", "STEERING", 36, Gold, true);
        Center(steeringHeader.rectTransform, new Vector2(leftX, 170), new Vector2(420, 50));
        var optButtons = PrefabButton(panel.transform, "Steering Buttons Option", "< >  BUTTONS", Center5, new Vector2(leftX, 80), optionSize);
        var optWheel = PrefabButton(panel.transform, "Steering Wheel Option", "WHEEL", Center5, new Vector2(leftX, -30), optionSize);
        var optSlider = PrefabButton(panel.transform, "Steering Slider Option", "SLIDER", Center5, new Vector2(leftX, -140), optionSize);

        var shapeHeader = MakeText(panel.transform, "Shape Header", "BUTTON SHAPE", 36, Gold, true);
        Center(shapeHeader.rectTransform, new Vector2(rightX, 170), new Vector2(420, 50));
        var optSquare = PrefabButton(panel.transform, "Square Option", "SQUARE", Center5, new Vector2(rightX, 80), optionSize);
        var optRounded = PrefabButton(panel.transform, "Rounded Option", "ROUNDED", Center5, new Vector2(rightX, -30), optionSize);
        var later = MakeText(panel.transform, "Pedals Note", "Gas pedal layouts: later", 26, new Color(1f, 1f, 1f, 0.5f), false);
        Center(later.rectTransform, new Vector2(rightX, -140), new Vector2(420, 50));

        var divider = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        divider.transform.SetParent(panel.transform, false);
        Center((RectTransform)divider.transform, new Vector2(0, 15), new Vector2(3, 350));
        divider.GetComponent<Image>().color = new Color(Yellow.r, Yellow.g, Yellow.b, 0.5f);
        divider.GetComponent<Image>().raycastTarget = false;

        var close = PrefabButton(panel.transform, "Close Button", "CLOSE", Center5, new Vector2(0, -250), new Vector2(280, 85));

        var sm = settings.AddComponent<SettingsMenu>();
        sm.menuRoot = menu.gameObject;
        sm.settingsButton = settingsButtonGo.GetComponent<Button>();
        sm.closeButton = close.GetComponent<Button>();
        sm.dimmerButton = dimmer.GetComponent<Button>();
        sm.steeringButtonsOption = optButtons.GetComponent<Button>();
        sm.steeringWheelOption = optWheel.GetComponent<Button>();
        sm.steeringSliderOption = optSlider.GetComponent<Button>();
        sm.squareOption = optSquare.GetComponent<Button>();
        sm.roundedOption = optRounded.GetComponent<Button>();
        menu.gameObject.SetActive(false); // like the help and dealer menus: tick Menu to work on it

        // the help menu's touch panel names the button the way it is labelled now (no slash)
        var help = Object.FindFirstObjectByType<HelpMenu>(FindObjectsInactive.Include);
        if (help != null && help.touchPanel != null)
            foreach (var t in help.touchPanel.GetComponentsInChildren<TMP_Text>(true))
                if (t.text == "BUY/SELL") { Undo.RecordObject(t, "Build Touch Controls"); t.text = "BUY SELL"; EditorUtility.SetDirty(t); }

        Selection.activeGameObject = controls;
        EditorSceneManager.MarkSceneDirty(controls.scene);
        EditorUtility.DisplayDialog("Touch Controls",
            "Built Touch Controls and Settings Menu. Save the scene (Ctrl+S), then delete Assets/Editor/TouchControlsBuilder.cs.\n\n"
            + "Steering Wheel and Steering Slider are turned off in the editor (the game shows the one picked in SETTINGS).", "OK");
    }

    static readonly Vector2 Center5 = new Vector2(0.5f, 0.5f);

    // ---------- helpers ----------

    static Sprite ImportSprite(string file, int border)
    {
        string path = SpriteFolder + file;
        AssetDatabase.ImportAsset(path);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) { Debug.LogError($"Missing {path} (run Art/Touch/make_touch_sprites.py)."); return null; }
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spriteBorder = new Vector4(border, border, border, border);
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(go, "Build Touch Controls");
    }

    static GameObject MakeCanvas(string name, int sortOrder)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(root, "Build Touch Controls");
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return root;
    }

    static Transform MakeGroup(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Stretch((RectTransform)go.transform, 0);
        return go.transform;
    }

    // a UI Button prefab copy; position is the anchored position of its corner (or centre) at the anchor
    static GameObject PrefabButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, parent);
        go.name = name;
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        var text = go.transform.Find("Label")?.GetComponent<TMP_Text>();
        if (text != null) text.text = label;
        return go;
    }

    // a touch button: a prefab copy whose Button only keeps the look (no tint, no clicks); TouchButtonInput does the input
    static Image TouchBtn(Transform parent, string name, string label, TouchButton action, Vector2 anchor, Vector2 position, float size)
    {
        var go = PrefabButton(parent, name, label, anchor, position, new Vector2(size, size));
        var b = go.GetComponent<Button>();
        if (b != null)
        {
            b.transition = Selectable.Transition.None;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
        }
        var input = go.AddComponent<TouchButtonInput>();
        input.button = action;
        return go.GetComponent<Image>();
    }

    static Image MakeFill(Transform parent)
    {
        var go = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Stretch((RectTransform)go.transform, 5);
        var img = go.GetComponent<Image>();
        img.color = Brown;
        img.raycastTarget = false;
        return img;
    }

    static TMP_Text MakeText(Transform parent, string name, string text, float size, Color color, bool bold)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        t.raycastTarget = false;
        return t;
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    static void Center(RectTransform rt, Vector2 position, Vector2 size) => Place(rt, new Vector2(0.5f, 0.5f), position, size);

    static void Stretch(RectTransform rt, float inset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }
}
