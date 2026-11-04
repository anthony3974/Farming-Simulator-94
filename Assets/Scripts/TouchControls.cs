using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Builds the on-screen buttons at runtime (steer left/right, gas, brake, use) and sends
/// their presses to <see cref="InputManager"/>. Added automatically by InputManager on touch devices.
/// </summary>
public class TouchControls : MonoBehaviour
{
    private static readonly Color Yellow = new Color32(255, 204, 0, 255);
    private static readonly Color Fill = new Color32(31, 31, 31, 235);
    private static readonly Color FillPressed = new Color32(110, 85, 10, 245);
    private static readonly Color LabelColor = new Color32(255, 201, 51, 255);

    private InputManager input;
    private Font font;

    void Start()
    {
        input = GetComponent<InputManager>();
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        EnsureEventSystem();
        Canvas canvas = CreateCanvas();

        // steering, bottom-left, to the right of the 260 px speedometer dial (it fills x 20 to 280)
        CreateButton(canvas, TouchButton.Left, "<", bottomRight: false, x: 310, y: 60, size: 150);
        CreateButton(canvas, TouchButton.Right, ">", bottomRight: false, x: 490, y: 60, size: 150);

        // right side: 130 px buttons in two columns and three rows. The bottom row sits above the field
        // pop-up (bottom-right, about 260 px tall + 20 px margin) and the top row stays below the
        // Money/Seeds/Fertilizer panel (top-right, about 195 px) even on very wide phones.
        const float col1 = 60, col2 = 200, col3 = 340, row1 = 295, row2 = 435, row3 = 575, btn = 130;
        CreateButton(canvas, TouchButton.Gas, "GAS", bottomRight: true, x: col1, y: row1, size: btn);
        CreateButton(canvas, TouchButton.Brake, "BRAKE", bottomRight: true, x: col2, y: row1, size: btn);
        CreateButton(canvas, TouchButton.Interact, "USE", bottomRight: true, x: col1, y: row2, size: btn);
        CreateButton(canvas, TouchButton.Trade, "BUY/SELL", bottomRight: true, x: col2, y: row2, size: btn);
        CreateButton(canvas, TouchButton.CycleCrop, "CROP", bottomRight: true, x: col1, y: row3, size: btn);
        CreateButton(canvas, TouchButton.Vehicle, "CAR", bottomRight: true, x: col2, y: row3, size: btn);
        // fertilizer: a third column beside CAR (temporary place until the touch buttons are reworked)
        CreateButton(canvas, TouchButton.Fertilize, "FERT", bottomRight: true, x: col3, y: row3, size: btn);

        // help menu, top-left corner
        CreateButton(canvas, TouchButton.Help, "HELP", bottomRight: false, x: 40, y: 40, size: 110, top: true);
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    private Canvas CreateCanvas()
    {
        var go = new GameObject("TouchControlsCanvas");
        go.transform.SetParent(transform, false);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private void CreateButton(Canvas canvas, TouchButton button, string label, bool bottomRight, float x, float y, float size, bool top = false)
    {
        // yellow border (this object receives the touches)
        var go = new GameObject(button + "Button", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(canvas.transform, false);

        var rt = (RectTransform)go.transform;
        Vector2 anchor = new Vector2(bottomRight ? 1 : 0, top ? 1 : 0);
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = new Vector2(bottomRight ? -x : x, top ? -y : y);

        var border = go.GetComponent<Image>();
        border.color = Yellow;

        // dark fill inset by 5px
        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(go.transform, false);
        var fillRt = (RectTransform)fillGo.transform;
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(5, 5);
        fillRt.offsetMax = new Vector2(-5, -5);
        var fill = fillGo.GetComponent<Image>();
        fill.color = Fill;
        fill.raycastTarget = false;

        // label
        var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        var textRt = (RectTransform)textGo.transform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(8, 8);
        textRt.offsetMax = new Vector2(-8, -8);
        var text = textGo.GetComponent<Text>();
        text.font = font;
        text.text = label;
        text.color = LabelColor;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 20;
        text.resizeTextMaxSize = 80;
        text.raycastTarget = false;

        var hold = go.AddComponent<HoldButton>();
        hold.Init(input, button, fill);
    }

    /// <summary>Reports press and release of one on-screen button to the InputManager.</summary>
    private class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private InputManager input;
        private TouchButton button;
        private Image fill;

        public void Init(InputManager input, TouchButton button, Image fill)
        {
            this.input = input;
            this.button = button;
            this.fill = fill;
        }

        public void OnPointerDown(PointerEventData eventData) => Set(true);
        public void OnPointerUp(PointerEventData eventData) => Set(false);
        public void OnPointerExit(PointerEventData eventData) => Set(false);
        private void OnDisable() => Set(false);

        private void Set(bool pressed)
        {
            // both can already be destroyed when the scene shuts down
            if (input != null) input.SetTouch(button, pressed);
            if (fill != null) fill.color = pressed ? FillPressed : Fill;
        }
    }
}
