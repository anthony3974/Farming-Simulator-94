using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// An on-screen slider that springs back to the middle: the steering slider (left / right) and the pedal slider
/// (up = gas, down = brake). Touch the bar and slide: the knob follows the finger, and how far it is from the
/// middle is how much (the ends = full). Let go and the knob springs back to the middle.
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

    // how far the knob can go from the middle, in the bar's units
    private float Travel
    {
        get
        {
            if (knob == null) return 1f;
            var bar = ((RectTransform)transform).rect;
            return Mathf.Max(1f, Vertical ? (bar.height - knob.rect.height) / 2f : (bar.width - knob.rect.width) / 2f);
        }
    }

    private void Follow(PointerEventData eventData)
    {
        var rt = (RectTransform)transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.position, eventData.pressEventCamera, out Vector2 local);
        float offset = Vertical ? local.y - rt.rect.center.y : local.x - rt.rect.center.x;
        value = Mathf.Clamp(offset / Travel, -1f, 1f);
    }

    private void PlaceKnob()
    {
        if (knob == null) return;
        float at = value * Travel;
        knob.anchoredPosition = Vertical ? new Vector2(knob.anchoredPosition.x, at) : new Vector2(at, knob.anchoredPosition.y);
    }
}
