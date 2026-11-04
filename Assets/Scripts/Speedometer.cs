using TMPro;
using UnityEngine;

/// <summary>
/// Dial speedometer in the bottom-left corner with a moving needle and a number in the middle. A real object
/// in the scene hierarchy ("Speedometer", made by Tools > Build Speedometer: the dial face and hub are images in
/// Assets/Images/Speedometer, the numbers are texts), so it can be moved, resized and restyled in the editor.
/// This script only turns the needle and writes the number. GameCharacter points it at the vehicle you drive.
/// </summary>
public class Speedometer : MonoBehaviour
{
    [Tooltip("The vehicle being measured. Set by GameCharacter (the vehicle you drive).")]
    public Transform target;

    [Tooltip("The tractor's top speed in world units per second (set by GameCharacter). Other vehicles use the same scale.")]
    public float maxWorldSpeed = 30f;

    [Tooltip("What the speedometer shows at maxWorldSpeed (the current tractor's top speed).")]
    public float maxDisplayKmh = 50f;

    [Tooltip("The speed at the end of the dial. Higher than the tractor's top speed, so faster vehicles fit later. " +
             "The dial's numbers and tick marks are drawn by Tools > Build Speedometer: run it again after changing this.")]
    public float dialMaxKmh = 100f;

    [Header("Scene objects (set by the builder; keep them set if you rearrange)")]
    [Tooltip("Turns around its pivot (the dial centre): pointing straight up is half the dial.")]
    public RectTransform needle;
    public TMP_Text numberLabel;
    [Tooltip("{0} = the speed.")]
    public string numberFormat = "{0}\n<size=40%><color=#FFC933>km/h</color></size>";

    /// <summary>The needle sweeps this many degrees: -135 (zero, bottom-left) to +135 (dial max, bottom-right).</summary>
    public const float SweepDegrees = 270f;

    private Vector3 lastPos;
    private float smoothed;
    private int lastShown = -1;

    void Start()
    {
        if (needle == null || numberLabel == null)
        {
            Debug.LogError($"Speedometer turned off: '{name}' is missing{(needle == null ? " Needle" : "")}{(numberLabel == null ? " Number Label" : "")}. "
                + "Drag the objects back into its Speedometer slots, or run Tools > Build Speedometer again.", this);
            enabled = false;
            return;
        }
        if (target != null) lastPos = target.position;
    }

    /// <summary>Follow another vehicle (after switching). Starts from its position, so the needle does not jump.</summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null) lastPos = target.position;
    }

    void LateUpdate()
    {
        if (target == null || Time.deltaTime <= 0f) return;

        Vector3 delta = target.position - lastPos;
        delta.y = 0f; // ignore gravity / hills
        lastPos = target.position;

        float kmh = delta.magnitude / Time.deltaTime / maxWorldSpeed * maxDisplayKmh;
        smoothed = Mathf.Lerp(smoothed, kmh, 10f * Time.deltaTime);

        // only rebuild the text when the shown number changes (it rebuilds the TextMeshPro mesh)
        int shown = Mathf.RoundToInt(smoothed);
        if (shown != lastShown)
        {
            lastShown = shown;
            numberLabel.text = string.Format(numberFormat, shown);
        }

        float t = Mathf.Clamp01(smoothed / dialMaxKmh);
        needle.localRotation = Quaternion.Euler(0, 0, SweepDegrees / 2f - t * SweepDegrees);
    }
}
