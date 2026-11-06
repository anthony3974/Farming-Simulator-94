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
    // the same brown as the counters panels, speedometer face and message box; opaque so the yellow border does not show through
    private static readonly Color Fill = new Color32(60, 45, 20, 255);
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
        const float col1 = 60, col2 = 200, row1 = 295, row2 = 435, row3 = 575, btn = 130;
        CreateButton(canvas, TouchButton.Gas, "GAS", bottomRight: true, x: col1, y: row1, size: btn);
        CreateButton(canvas, TouchButton.Brake, "BRAKE", bottomRight: true, x: col2, y: row1, size: btn);
        CreateButton(canvas, TouchButton.Interact, "USE", bottomRight: true, x: col1, y: row2, size: btn);
        CreateButton(canvas, TouchButton.Trade, "BUY/SELL", bottomRight: true, x: col2, y: row2, size: btn);
        CreateButton(canvas, TouchButton.CycleCrop, "CROP", bottomRight: true, x: col1, y: row3, size: btn);
        CreateButton(canvas, TouchButton.Vehicle, "CAR", bottomRight: true, x: col2, y: row3, size: btn);

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

    /// <summary>
    /// Reports press and release of one on-screen button to the InputManager. Held while any finger that is
    /// still down is on it: sliding off lets go, sliding back on (without lifting) presses it again, and a
    /// finger that slides onto it from another button presses it too (slide from &lt; to &gt; to steer).
    /// Only the held buttons (&lt; &gt; GAS BRAKE) do the sliding: the one-press buttons (USE, BUY/SELL, ...) act
    /// on the press, so a finger passing over them, or sliding off and back, must not press them again.
    /// Before 2026-11-05 a slide off released a held button until the finger was lifted and put down again.
    /// </summary>
    private class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private InputManager input;
        private TouchButton button;
        private Image fill;
        private readonly System.Collections.Generic.HashSet<int> fingers = new(); // pointers down and on this button
        private bool pressed;
        private bool slide; // a held button (steering, gas, brake)

        public void Init(InputManager input, TouchButton button, Image fill)
        {
            this.input = input;
            this.button = button;
            this.fill = fill;
            slide = button is TouchButton.Left or TouchButton.Right or TouchButton.Gas or TouchButton.Brake;
        }

        public void OnPointerDown(PointerEventData eventData) { fingers.Add(eventData.pointerId); Refresh(); }
        public void OnPointerUp(PointerEventData eventData) { fingers.Remove(eventData.pointerId); Refresh(); }
        public void OnPointerExit(PointerEventData eventData) { fingers.Remove(eventData.pointerId); Refresh(); }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // a mouse only hovering, or a finger that is lifting, must not press it
            if (!slide || !IsDown(eventData.pointerId)) return;
            fingers.Add(eventData.pointerId);
            Refresh();
        }

        // a finger that slid on from another button is lifted here, but its "up" goes to the button it first
        // pressed: drop fingers that are no longer down, so the button never sticks
        void Update()
        {
            if (fingers.Count == 0) return;
            fingers.RemoveWhere(id => !IsDown(id));
            Refresh();
        }

        private void OnDisable()
        {
            fingers.Clear();
            Refresh();
        }

        private static bool IsDown(int pointerId)
        {
            if (pointerId < 0) return Input.GetMouseButton(-1 - pointerId); // mouse: -1 left, -2 right, -3 middle
            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.fingerId == pointerId)
                    return touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;
            }
            return false;
        }

        private void Refresh()
        {
            bool now = fingers.Count > 0;
            if (now == pressed) return;
            pressed = now;
            // both can already be destroyed when the scene shuts down
            if (input != null) input.SetTouch(button, pressed);
            if (fill != null) fill.color = pressed ? FillPressed : Fill;
        }
    }
}
