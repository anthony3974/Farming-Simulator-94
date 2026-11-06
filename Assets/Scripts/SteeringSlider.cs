using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The on-screen steering slider (touch steering option "Slider"). Touch the bar and slide left or right: the
/// knob follows the finger, and how far it is from the middle is how hard you steer (the ends = full lock).
/// Let go and the knob springs back to the middle (straight).
/// </summary>
public class SteeringSlider : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IInitializePotentialDragHandler
{
    [Tooltip("The knob that slides (a child of this bar).")]
    public RectTransform knob;
    [Tooltip("Optional: the knob's Fill, shown in the pressed colour while held.")]
    public Image knobFill;
    public Color pressedColor = new Color32(110, 85, 10, 255);
    [Tooltip("How fast it springs back to the middle when let go (full steering per second).")]
    public float returnSpeed = 8f;
    [Tooltip("Near the middle counts as straight (fraction of full steering).")]
    [Range(0f, 0.3f)] public float deadZone = 0.05f;

    private InputManager input;
    private int finger = int.MinValue; // pointer holding the knob (MinValue = none)
    private float value;               // -1 full left, 1 full right
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
        if (input != null) input.SetSteer(0f);
    }

    void Update()
    {
        if (Held && !TouchButtonInput.IsDown(finger)) Release(); // lifted somewhere we did not hear
        if (!Held) value = Mathf.MoveTowards(value, 0f, returnSpeed * Time.unscaledDeltaTime);
        PlaceKnob();
        float steer = Mathf.Abs(value) < deadZone ? 0f : Mathf.Sign(value) * Mathf.InverseLerp(deadZone, 1f, Mathf.Abs(value));
        if (input != null) input.SetSteer(steer);
    }

    private void Release()
    {
        finger = int.MinValue;
        if (knobFill != null) knobFill.color = normalColor;
    }

    // how far the knob can go from the middle, in the bar's units
    private float Travel => knob == null ? 1f : Mathf.Max(1f, (((RectTransform)transform).rect.width - knob.rect.width) / 2f);

    private void Follow(PointerEventData eventData)
    {
        var rt = (RectTransform)transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.position, eventData.pressEventCamera, out Vector2 local);
        value = Mathf.Clamp((local.x - rt.rect.center.x) / Travel, -1f, 1f);
    }

    private void PlaceKnob()
    {
        if (knob != null) knob.anchoredPosition = new Vector2(value * Travel, knob.anchoredPosition.y);
    }
}
