using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Touch camera on the invisible full-screen "Look Area" (made by Tools > Build Touch Look Area):
/// one finger dragging turns the camera around the vehicle (left / right) and tilts it (up / down), two fingers
/// pinching zoom in and out, and a double tap brings the camera back behind the vehicle.
/// Its canvas is sorted below every other canvas, so a touch that starts on a button or a panel goes to that
/// instead; only touches on the open game view count. A finger that holds GAS or a steering button meanwhile is
/// not on this area, so it does not count as a second finger for the pinch.
/// Turned off on PC unless the touch controls are shown (InputManager "Show Touch Controls").
/// The speeds and directions are on <see cref="GameCharacter"/> (Touch Look Sensitivity, Invert Touch Look, Pinch Zoom Sensitivity).
/// </summary>
public class TouchLook : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Tooltip("Found automatically if left empty.")]
    public InputManager input;

    [Tooltip("Two taps within this many seconds recenter the camera.")]
    public float doubleTapSeconds = 0.4f;
    [Tooltip("A tap is a touch shorter than this many seconds...")]
    public float tapSeconds = 0.25f;
    [Tooltip("...that moves less than this fraction of the screen height.")]
    public float tapMoveFraction = 0.03f;

    // the fingers on this area (at most the first two count) and where each one is
    private readonly List<int> ids = new();
    private readonly Dictionary<int, Vector2> positions = new();
    private float downTime;
    private Vector2 downPos;
    private bool tapPossible;
    private float lastTapTime = -10f;

    void Start()
    {
        if (input == null) input = FindFirstObjectByType<InputManager>();
        // on PC the mouse looks around (locked cursor); this full-screen area would only get in the way
        if (input == null || !input.TouchControlsActive) gameObject.SetActive(false);
    }

    void OnDisable()
    {
        ids.Clear();
        positions.Clear();
        tapPossible = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (ids.Count >= 2 || ids.Contains(eventData.pointerId)) return;
        ids.Add(eventData.pointerId);
        positions[eventData.pointerId] = eventData.position;
        if (ids.Count == 1)
        {
            downTime = Time.unscaledTime;
            downPos = eventData.position;
            tapPossible = true;
        }
        else tapPossible = false; // a second finger: this is a pinch, not a tap
    }

    public void OnDrag(PointerEventData eventData)
    {
        int id = eventData.pointerId;
        if (!positions.ContainsKey(id)) return;

        if (ids.Count == 2)
        {
            // pinch: the change of the distance between the two fingers, as a fraction of the screen height
            float before = Vector2.Distance(positions[ids[0]], positions[ids[1]]);
            positions[id] = eventData.position;
            float after = Vector2.Distance(positions[ids[0]], positions[ids[1]]);
            input.AddTouchZoom((after - before) / Screen.height);
            return;
        }

        positions[id] = eventData.position;
        if (id != ids[0]) return;
        // as a fraction of the screen height, so the same drag turns the same amount on every phone
        input.AddTouchLook(eventData.delta.y / Screen.height);
        input.AddTouchLookX(eventData.delta.x / Screen.height);
        if ((eventData.position - downPos).magnitude > tapMoveFraction * Screen.height) tapPossible = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        int id = eventData.pointerId;
        if (!ids.Contains(id)) return;

        // a quick touch that barely moved is a tap; two of them close together recenter the camera
        if (tapPossible && ids.Count == 1 && Time.unscaledTime - downTime <= tapSeconds
            && (eventData.position - downPos).magnitude <= tapMoveFraction * Screen.height)
        {
            if (Time.unscaledTime - lastTapTime <= doubleTapSeconds)
            {
                input.RequestRecenter();
                lastTapTime = -10f;
            }
            else lastTapTime = Time.unscaledTime;
        }
        else lastTapTime = -10f;

        ids.Remove(id);
        positions.Remove(id);
        tapPossible = false;
    }
}
