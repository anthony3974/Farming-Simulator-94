using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The on-screen touch controls: the "Touch Controls" canvas in GameScene (made by Tools > Build Touch Controls),
/// real objects that can be moved, resized and restyled in the editor. Each button is a UI Button prefab copy
/// with a <see cref="TouchButtonInput"/>. This script only:
///  - hides the whole canvas on PC (unless Show Touch Controls is ticked on the InputManager),
///  - shows the steering picked in SETTINGS (<see cref="TouchSettings"/>): the &lt; &gt; buttons, the wheel or the slider,
///  - gives the buttons the picked shape: square (plain) or rounded (the rounded sprites, 9-sliced).
/// </summary>
public class TouchControls : MonoBehaviour
{
    [Header("Steering (one is shown, picked in SETTINGS)")]
    public GameObject steeringButtons;
    public GameObject steeringWheel;
    public GameObject steeringSlider;

    [Header("Button shape")]
    [Tooltip("Every part that turns rounded: the button's yellow border Image; its child \"Fill\" is rounded too.")]
    public List<Image> shapedParts = new();
    public Sprite roundedBorder;
    public Sprite roundedFill;
    [Tooltip("The rounded sprites are drawn at 2x, so 2 gives a 24 px corner.")]
    public float roundedPixelsPerUnit = 2f;

    private InputManager input;

    void Start()
    {
        input = FindFirstObjectByType<InputManager>();
        if (input == null || !input.TouchControlsActive)
        {
            gameObject.SetActive(false);
            return;
        }
        TouchSettings.Changed += Apply;
        Apply();
    }

    void OnDestroy() => TouchSettings.Changed -= Apply;

    private void Apply()
    {
        var mode = TouchSettings.Steering;
        // a missing option falls back to the buttons, so there is always a way to steer
        if ((mode == SteeringMode.Wheel && steeringWheel == null) || (mode == SteeringMode.Slider && steeringSlider == null))
            mode = SteeringMode.Buttons;
        if (steeringButtons != null) steeringButtons.SetActive(mode == SteeringMode.Buttons);
        if (steeringWheel != null) steeringWheel.SetActive(mode == SteeringMode.Wheel);
        if (steeringSlider != null) steeringSlider.SetActive(mode == SteeringMode.Slider);

        bool rounded = TouchSettings.Rounded;
        foreach (var border in shapedParts)
        {
            if (border == null) continue;
            SetShape(border, rounded ? roundedBorder : null);
            var fill = border.transform.Find("Fill")?.GetComponent<Image>();
            if (fill != null) SetShape(fill, rounded ? roundedFill : null);
        }
    }

    private void SetShape(Image image, Sprite sprite)
    {
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.pixelsPerUnitMultiplier = sprite != null ? roundedPixelsPerUnit : 1f;
    }
}
