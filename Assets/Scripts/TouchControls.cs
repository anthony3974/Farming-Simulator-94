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

        // steering, bottom-left
        CreateButton(canvas, TouchButton.Left, "<", bottomRight: false, x: 60, y: 60, size: 220);
        CreateButton(canvas, TouchButton.Right, ">", bottomRight: false, x: 320, y: 60, size: 220);

        // driving + use, right side, kept above the field pop-up in the bottom-right corner
        CreateButton(canvas, TouchButton.Gas, "GAS", bottomRight: true, x: 60, y: 330, size: 200);
        CreateButton(canvas, TouchButton.Brake, "BRAKE", bottomRight: true, x: 290, y: 330, size: 200);
        CreateButton(canvas, TouchButton.Interact, "USE", bottomRight: true, x: 60, y: 560, size: 200);
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

    private void CreateButton(Canvas canvas, TouchButton button, string label, bool bottomRight, float x, float y, float size)
    {
        // yellow border (this object receives the touches)
        var go = new GameObject(button + "Button", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(canvas.transform, false);

        var rt = (RectTransform)go.transform;
        Vector2 anchor = bottomRight ? new Vector2(1, 0) : new Vector2(0, 0);
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = new Vector2(bottomRight ? -x : x, y);

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
            input.SetTouch(button, pressed);
            fill.color = pressed ? FillPressed : Fill;
        }
    }
}
