using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The on-screen steering wheel (touch steering option "Wheel"). Put a finger on it and turn it around its centre
/// like a real wheel: clockwise steers right. Up to <see cref="maxAngle"/> each way = full lock; anything less
/// steers gently. Let go and it springs back to straight. It only turns: no press-down look (owner, 2026-11-06).
/// </summary>
public class SteeringWheel : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IInitializePotentialDragHandler
{
    [Tooltip("The wheel picture that turns: a child, so this object (the touch area) stays still. Found by name \"Wheel\" if empty.")]
    public Image wheel;
    [Tooltip("Degrees the wheel turns each way for full steering.")]
    public float maxAngle = 120f;
    [Tooltip("How fast it springs back to straight when let go (degrees per second).")]
    public float returnSpeed = 720f;

    private InputManager input;
    private int finger = int.MinValue; // pointer holding the wheel (MinValue = none)
    private float angle;               // degrees, positive = turned right (clockwise)
    private float lastFingerAngle;

    private bool Held => finger != int.MinValue;

    void Awake()
    {
        if (wheel == null) wheel = transform.Find("Wheel")?.GetComponent<Image>();
    }

    void Start() => input = FindFirstObjectByType<InputManager>();

    public void OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (Held) return; // one finger turns it
        finger = eventData.pointerId;
        lastFingerAngle = FingerAngle(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != finger) return;
        float now = FingerAngle(eventData);
        // clockwise on screen is a falling angle; DeltaAngle keeps it right across the 180 / -180 wrap
        angle = Mathf.Clamp(angle - Mathf.DeltaAngle(lastFingerAngle, now), -maxAngle, maxAngle);
        lastFingerAngle = now;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == finger) finger = int.MinValue;
    }

    void OnDisable()
    {
        finger = int.MinValue;
        angle = 0f;
        Apply();
        if (input != null) input.SetSteer(0f);
    }

    void Update()
    {
        if (Held && !TouchButtonInput.IsDown(finger)) finger = int.MinValue; // lifted somewhere we did not hear
        if (!Held) angle = Mathf.MoveTowards(angle, 0f, returnSpeed * Time.unscaledDeltaTime);
        Apply();
        if (input != null) input.SetSteer(angle / maxAngle);
    }

    private void Apply()
    {
        if (wheel == null) return;
        wheel.rectTransform.localEulerAngles = new Vector3(0f, 0f, -angle);
    }

    // the finger's angle around the wheel centre, in degrees (counter-clockwise positive)
    private float FingerAngle(PointerEventData eventData)
    {
        var rt = (RectTransform)transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.position, eventData.pressEventCamera, out Vector2 local);
        local -= rt.rect.center;
        return Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg;
    }
}
