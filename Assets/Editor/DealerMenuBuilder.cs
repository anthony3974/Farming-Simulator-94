using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// One-off editor tool: adds the "Dealer Menu" object (its own Canvas) to the open scene (GameScene) and fills
// in the menu slots on the scene's Dealer. All the parts are plain positioned objects with no layout groups,
// and the BUY buttons are copies of Assets/Prefabs/UI/UI Button.prefab, so everything can be moved, resized,
// restyled and reworded freely in the editor afterwards.
// Use: open GameScene, Tools > Build Dealer Menu, save the scene. Delete this file afterwards (git history).
//
// Dealer Menu                 Canvas (sort order 60: above the touch buttons 50 and the pause menu 55)
//   Menu                      hidden while playing. Left turned OFF in the editor so it does not cover the
//                             game view; tick it to work on it (it hides itself on Play either way)
//     Dimmer                  full-screen, a tap on it (outside the panel) closes the menu
//     Border > Panel          yellow border, dark panel
//       Title, Money
//       <Vehicle> Row         Name, Description, Buy Button (UI Button copy: its Label shows the price or OWNED)
//       Footer Keyboard       shown on PC
//       Footer Touch          shown with touch controls
public static class DealerMenuBuilder
{
    const string RootName = "Dealer Menu";
    const string ButtonPrefabPath = "Assets/Prefabs/UI/UI Button.prefab";

    static readonly Color Gold = new Color32(255, 201, 51, 255);
    static readonly Color Yellow = new Color32(255, 204, 0, 255);
    static readonly Color Panel = new Color32(31, 31, 31, 245);

    // the same size and spacing as the old runtime menu
    const float Width = 1100f, Border = 4f, PadX = 34f, PadY = 22f, Spacing = 14f;
    const float TitleHeight = 60f, MoneyHeight = 40f, RowHeight = 90f, FooterHeight = 44f;
    const float ButtonWidth = 260f, TextGap = 24f;

    [MenuItem("Tools/Build Dealer Menu")]
    static void Build()
    {
        var dealer = Object.FindFirstObjectByType<Dealer>();
        if (dealer == null)
        {
            EditorUtility.DisplayDialog("Dealer Menu", "No Dealer in the open scene. Open GameScene first.", "OK");
            return;
        }
        var buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPrefabPath);
        if (buttonPrefab == null)
        {
            EditorUtility.DisplayDialog("Dealer Menu", $"Can't find the button prefab at {ButtonPrefabPath}.", "OK");
            return;
        }

        var old = GameObject.Find(RootName);
        if (old != null)
        {
            if (!EditorUtility.DisplayDialog("Dealer Menu", "The scene already has a Dealer Menu. Replace it? Any changes made to it are lost.", "Replace", "Cancel"))
                return;
            Undo.DestroyObjectImmediate(old);
        }

        var root = new GameObject(RootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(root, "Build Dealer Menu");
        SceneManager_MoveTo(root, dealer.gameObject);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var menu = new GameObject("Menu", typeof(RectTransform));
        menu.transform.SetParent(root.transform, false);
        Stretch((RectTransform)menu.transform);

        // the dimmer and the panel are siblings, so a tap on the panel does not close the menu
        var dimmer = new GameObject("Dimmer", typeof(RectTransform), typeof(Image), typeof(Button));
        dimmer.transform.SetParent(menu.transform, false);
        Stretch((RectTransform)dimmer.transform);
        dimmer.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        var close = dimmer.GetComponent<Button>();
        close.transition = Selectable.Transition.None;

        int vehicles = 0;
        foreach (var _ in VehicleCatalog.ForSale) vehicles++;
        float innerWidth = Width - Border * 2f;
        float innerHeight = PadY * 2f + TitleHeight + Spacing + MoneyHeight + vehicles * (Spacing + RowHeight) + Spacing + FooterHeight;
        float contentWidth = innerWidth - PadX * 2f;

        var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(menu.transform, false);
        border.GetComponent<Image>().color = Yellow;
        var borderRt = (RectTransform)border.transform;
        borderRt.anchorMin = borderRt.anchorMax = borderRt.pivot = new Vector2(0.5f, 0.5f);
        borderRt.anchoredPosition = Vector2.zero;
        borderRt.sizeDelta = new Vector2(Width, innerHeight + Border * 2f);

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(border.transform, false);
        panel.GetComponent<Image>().color = Panel;
        var panelRt = (RectTransform)panel.transform;
        Stretch(panelRt);
        panelRt.offsetMin = new Vector2(Border, Border);
        panelRt.offsetMax = new Vector2(-Border, -Border);

        float y = PadY;
        var title = MakeText(panel.transform, "Title", "VEHICLE DEALER", 44, Gold, TextAlignmentOptions.Center, true);
        Place(title.rectTransform, PadX, y, contentWidth, TitleHeight);
        y += TitleHeight + Spacing;

        // the money text is filled in when the menu opens; this is only a sample to lay it out
        var money = MakeText(panel.transform, "Money", "Your money: $ 12,345", 28, Color.white, TextAlignmentOptions.Center, false);
        Place(money.rectTransform, PadX, y, contentWidth, MoneyHeight);
        y += MoneyHeight + Spacing;

        Undo.RecordObject(dealer, "Build Dealer Menu");
        dealer.rows.Clear();
        float textWidth = contentWidth - ButtonWidth - TextGap;
        foreach (var info in VehicleCatalog.ForSale)
        {
            var row = new GameObject(info.name + " Row", typeof(RectTransform));
            row.transform.SetParent(panel.transform, false);
            Place((RectTransform)row.transform, PadX, y, contentWidth, RowHeight);

            var name = MakeText(row.transform, "Name", info.name, 32, Gold, TextAlignmentOptions.BottomLeft, true);
            Place(name.rectTransform, 0f, 0f, textWidth, RowHeight * 0.5f);
            var description = MakeText(row.transform, "Description", info.description, 26, Color.white, TextAlignmentOptions.TopLeft, false);
            Place(description.rectTransform, 0f, RowHeight * 0.5f, textWidth, RowHeight * 0.5f);

            var buttonGo = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, row.transform);
            buttonGo.name = "Buy Button";
            Place((RectTransform)buttonGo.transform, contentWidth - ButtonWidth, 0f, ButtonWidth, RowHeight);
            var label = buttonGo.transform.Find("Label").GetComponent<TMP_Text>();
            label.text = $"BUY  $ {info.price:N0}"; // replaced when the menu opens (price, or OWNED)
            label.fontSizeMax = 30f;
            var fill = buttonGo.transform.Find("Fill").GetComponent<Image>();

            dealer.rows.Add(new Dealer.Row { kind = info.kind, button = buttonGo.GetComponent<Button>(), label = label, fill = fill });
            y += RowHeight + Spacing;
        }

        var footerKeys = MakeText(panel.transform, "Footer Keyboard", "Click BUY.  E or Esc to close", 26, Color.white, TextAlignmentOptions.Center, false);
        Place(footerKeys.rectTransform, PadX, y, contentWidth, FooterHeight);
        var footerTouch = MakeText(panel.transform, "Footer Touch", "Tap outside to close", 26, Color.white, TextAlignmentOptions.Center, false);
        Place(footerTouch.rectTransform, PadX, y, contentWidth, FooterHeight);
        footerTouch.gameObject.SetActive(false); // same place as the keyboard one; the Dealer shows the right one

        menu.SetActive(false);

        dealer.menuRoot = menu;
        dealer.closeButton = close;
        dealer.moneyText = money;
        dealer.keyboardFooter = footerKeys.gameObject;
        dealer.touchFooter = footerTouch.gameObject;
        PrefabUtility.RecordPrefabInstancePropertyModifications(dealer);

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log("Dealer Menu added and linked to the Dealer. Save the scene (Ctrl+S). Its Menu object is turned off in the editor: tick it to see and edit it.", root);
    }

    // a new object goes into the active scene; keep it in the dealer's scene in case another scene is active
    static void SceneManager_MoveTo(GameObject go, GameObject sceneOf)
    {
        if (go.scene != sceneOf.scene) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, sceneOf.scene);
    }

    static TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions align, bool bold)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        // one line; a longer text (after rewording) shrinks to fit instead of spilling out
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.enableAutoSizing = true;
        t.fontSizeMax = size;
        t.fontSizeMin = 18f;
        t.raycastTarget = false;
        return t;
    }

    // top-left anchored, x / y measured from the parent's top-left corner (y down)
    static void Place(RectTransform rt, float x, float y, float width, float height)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(width, height);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
