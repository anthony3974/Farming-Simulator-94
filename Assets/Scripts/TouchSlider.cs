using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// An on-screen slider that springs back to the middle: the steering slider (left / right) and the pedal slider
/// (up = gas, down = brake). Touch the bar and slide: the knob follows the finger, and how far it is from the
/// rest point is how much (the ends = full). Let go and the knob springs back to the rest point: the middle for
/// steering, 20% up for the pedals (80% of the travel is gas, 20% brake; owner, 2026-11-06).
/// </summary>
public abstract class TouchSlider : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IInitializePotentialDragHandler
{
    [Tooltip("The knob that slides (a child of this bar).")]
    public RectTransform knob;
    [Tooltip("Optional: the knob's Fill, shown in the pressed colour while held.")]
    public Image knobFill;
    public Color pressedColor = new Color32(110, 85, 10, 255);
    [Tooltip("How fast it springs back to the middle when let go (full travel per second).")]
    public float returnSpeed = 8f;
    [Tooltip("Near the middle counts as nothing (fraction of full).")]
    [Range(0f, 0.3f)] public float deadZone = 0.05f;
    [Tooltip("Where the knob rests, along the bar (0 = left / bottom end, 0.5 = middle, 1 = right / top end). "
        + "The travel on each side of it is full: 0.2 on the pedals gives 80% gas, 20% brake.")]
    [Range(0.1f, 0.9f)] public float restPoint = 0.5f;
    [Tooltip("Optional: a mark that shows the rest point (found by name \"Middle\" if empty); kept at the rest point.")]
    public RectTransform restMark;

    /// <summary>True: slides up and down (up = positive). False: left and right (right = positive).</summary>
    protected abstract bool Vertical { get; }
    /// <summary>Sends this frame's value (-1 to 1, after the dead zone) to the InputManager.</summary>
    protected abstract void Send(InputManager input, float value);

    private InputManager input;
    private int finger = int.MinValue; // pointer holding the knob (MinValue = none)
    private float value;               // -1 to 1
    private Color normalColor;

    private bool Held => finger != int.MinValue;

    void Awake()
    {
        if (knobFill != null) normalColor = knobFill.color;
        if (restMark == null) restMark = transform.Find("Middle") as RectTransform;
    }

    void Start() => input = FindFirstObjectByType<InputManager>();

    public void OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (Held) return; // one finger slides it
        finger = eventData.pointerId;
        if (knobFill != null) knobFill.color = pressedColor;
        Follow(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId == finger) Follow(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == finger) Release();
    }

    void OnDisable()
    {
        Release();
        value = 0f;
        PlaceKnob();
        if (input != null) Send(input, 0f);
    }

    void Update()
    {
        if (Held && !TouchButtonInput.IsDown(finger)) Release(); // lifted somewhere we did not hear
        if (!Held) value = Mathf.MoveTowards(value, 0f, returnSpeed * Time.unscaledDeltaTime);
        PlaceKnob();
        float amount = Mathf.Abs(value) < deadZone ? 0f : Mathf.Sign(value) * Mathf.InverseLerp(deadZone, 1f, Mathf.Abs(value));
        if (input != null) Send(input, amount);
    }

    private void Release()
    {
        finger = int.MinValue;
        if (knobFill != null) knobFill.color = normalColor;
    }

    // the knob's centre can go Half either way of the bar's centre; it rests at Rest (in the bar's units)
    private float Half
    {
        get
        {
            if (knob == null) return 1f;
            var bar = ((RectTransform)transform).rect;
            return Mathf.Max(1f, Vertical ? (bar.height - knob.rect.height) / 2f : (bar.width - knob.rect.width) / 2f);
        }
    }
    private float Rest => (restPoint * 2f - 1f) * Half;

    private void Follow(PointerEventData eventData)
    {
        var rt = (RectTransform)transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.position, eventData.pressEventCamera, out Vector2 local);
        float offset = (Vertical ? local.y - rt.rect.center.y : local.x - rt.rect.center.x) - Rest;
        float travel = offset >= 0f ? Half - Rest : Half + Rest; // from the rest point to that end
        value = Mathf.Clamp(offset / Mathf.Max(1f, travel), -1f, 1f);
    }

    private void PlaceKnob()
    {
        float rest = Rest;
        float at = rest + value * (value >= 0f ? Half - rest : Half + rest);
        if (knob != null)
            knob.anchoredPosition = Vertical ? new Vector2(knob.anchoredPosition.x, at) : new Vector2(at, knob.anchoredPosition.y);
        if (restMark != null)
            restMark.anchoredPosition = Vertical ? new Vector2(restMark.anchoredPosition.x, rest) : new Vector2(rest, restMark.anchoredPosition.y);
    }
}
