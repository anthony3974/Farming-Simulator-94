using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One on-screen touch button (a copy of the UI Button prefab in the "Touch Controls" canvas). Reports its press
/// and release to the <see cref="InputManager"/>. Held while any finger that is still down is on it: sliding off
/// lets go, sliding back on (without lifting) presses it again, and a finger that slides onto it from another
/// button presses it too (slide from &lt; to &gt; to steer). Only the held buttons (&lt; &gt; GAS BRAKE) do the
/// sliding: the one-press buttons (USE, BUY SELL, ...) act on the press, so a finger passing over them, or sliding
/// off and back, must not press them again. While pressed, the Fill turns the pressed colour, and the held
/// buttons (&lt; &gt; GAS BRAKE) sink: they shrink a little and spring back when let go (owner, 2026-11-06).
/// </summary>
public class TouchButtonInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("What this button does.")]
    public TouchButton button;
    [Tooltip("Optional: the brown Fill that shows the press (found by name \"Fill\" if empty).")]
    public Image fill;
    public Color pressedColor = new Color32(110, 85, 10, 255);
    [Tooltip("Held buttons (< > GAS BRAKE) sink to this size while pressed. 1 = no sink.")]
    [Range(0.7f, 1f)] public float pressedScale = 0.9f;
    [Tooltip("Seconds the sink and the spring back take.")]
    public float pressTime = 0.06f;

    private InputManager input;
    private readonly HashSet<int> fingers = new(); // pointers down and on this button
    private bool pressed;
    private bool slide; // a held button (steering, gas, brake)
    private Color normalColor;
    private Image border;  // this button's own Image: it takes the touches
    private float press;   // 0 = up, 1 = fully sunk

    void Awake()
    {
        if (fill == null) fill = transform.Find("Fill")?.GetComponent<Image>();
        if (fill != null) normalColor = fill.color;
        border = GetComponent<Image>();
        slide = button is TouchButton.Left or TouchButton.Right or TouchButton.Gas or TouchButton.Brake;
    }

    void Start() => input = FindFirstObjectByType<InputManager>();

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
        if (fingers.Count > 0)
        {
            fingers.RemoveWhere(id => !IsDown(id));
            Refresh();
        }

        if (!slide) return;
        float target = pressed ? 1f : 0f;
        if (press == target) return;
        press = Mathf.MoveTowards(press, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, pressTime));
        Sink();
    }

    private void OnDisable()
    {
        fingers.Clear();
        Refresh();
        press = 0f;
        if (slide) Sink();
    }

    // shrink the button while it is held; the touch area is padded back out to full size, so a finger near
    // the edge does not slip off the shrunk button (which would let go, grow, press again, ...)
    private void Sink()
    {
        float scale = Mathf.Lerp(1f, pressedScale, press);
        transform.localScale = new Vector3(scale, scale, 1f);
        if (border == null) return;
        var rect = ((RectTransform)transform).rect;
        float grow = (1f / scale - 1f) / 2f;
        border.raycastPadding = new Vector4(-rect.width * grow, -rect.height * grow, -rect.width * grow, -rect.height * grow);
    }

    internal static bool IsDown(int pointerId)
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
        if (fill != null) fill.color = pressed ? pressedColor : normalColor;
    }
}
