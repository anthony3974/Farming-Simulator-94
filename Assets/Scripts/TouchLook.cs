using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Drag-to-look on touch screens: dragging a finger up or down on the "Look Area" tilts the camera, like moving
/// the mouse on PC. Sits on the invisible full-screen "Look Area" object (made by Tools > Build Touch Look Area).
/// Its canvas is sorted below every other canvas, so a touch that starts on a button or a panel goes to that
/// instead; only touches on the open game view tilt the camera. One finger at a time (the first one down);
/// another finger can hold GAS or a steering button meanwhile.
/// Turned off on PC unless the touch controls are shown (InputManager "Show Touch Controls").
/// The tilt speed and the up/down direction are on <see cref="GameCharacter"/> (Touch Look Sensitivity, Invert Touch Look).
/// </summary>
public class TouchLook : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Tooltip("Found automatically if left empty.")]
    public InputManager input;

    private const int NoPointer = int.MinValue;
    private int pointer = NoPointer;

    void Start()
    {
        if (input == null) input = FindFirstObjectByType<InputManager>();
        // on PC the mouse looks around (locked cursor); this full-screen area would only get in the way
        if (input == null || !input.TouchControlsActive) gameObject.SetActive(false);
    }

    void OnDisable()
    {
        pointer = NoPointer;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (pointer == NoPointer) pointer = eventData.pointerId;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != pointer) return;
        // as a fraction of the screen height, so the same drag tilts the same amount on every phone
        input.AddTouchLook(eventData.delta.y / Screen.height);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == pointer) pointer = NoPointer;
    }
}
