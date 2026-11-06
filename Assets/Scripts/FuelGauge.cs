using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Diesel gauge above the speedometer: a bar and the litres left in the tank, which every vehicle shares
/// (<see cref="PlayerValues.Fuel"/>). A real object in the scene hierarchy ("Fuel Gauge", made by
/// Tools > Build Fuel Gauge), so it can be moved, resized and restyled in the editor. This script only sets the
/// bar length, its colour and the number.
/// </summary>
public class FuelGauge : MonoBehaviour
{
    [Header("Scene objects (set by the builder; keep them set if you rearrange)")]
    [Tooltip("The bar's fill. Its right edge (Anchor Max X) is set to the part of the tank left, so stretch it across its parent.")]
    public RectTransform fill;
    public Image fillImage;
    public TMP_Text numberLabel;
    [Tooltip("{0} = litres left, {1} = tank size.")]
    public string numberFormat = "{0:0} L";

    [Header("Colours")]
    public Color fullColor = new Color32(110, 200, 60, 255);
    [Tooltip("Below Half Below (part of the tank).")]
    public Color halfColor = new Color32(255, 204, 0, 255);
    [Tooltip("Below Low Below (part of the tank). Blinks when the tank is empty.")]
    public Color lowColor = new Color32(230, 60, 40, 255);
    [Range(0f, 1f)] public float halfBelow = 0.5f;
    [Range(0f, 1f)] public float lowBelow = 0.2f;

    private PlayerValues values;
    private int lastShown = -1;

    void Start()
    {
        if (fill == null || numberLabel == null)
        {
            Debug.LogError($"Fuel Gauge turned off: '{name}' is missing{(fill == null ? " Fill" : "")}{(numberLabel == null ? " Number Label" : "")}. "
                + "Drag the objects back into its Fuel Gauge slots, or run Tools > Build Fuel Gauge again.", this);
            enabled = false;
            return;
        }
        if (fillImage == null) fillImage = fill.GetComponent<Image>();
        values = PlayerValues.Instance;
        if (values == null) Debug.LogWarning("Fuel Gauge: no PlayerValues in the scene, nothing to show.", this);
    }

    void LateUpdate()
    {
        if (values == null) return;

        float t = values.FuelCapacity > 0f ? Mathf.Clamp01(values.Fuel / values.FuelCapacity) : 0f;
        bool empty = t <= 0f;
        // empty: the whole bar blinks faint red so it catches the eye (unscaled time, so it also blinks while paused)
        fill.anchorMax = new Vector2(empty ? 1f : t, fill.anchorMax.y);

        if (fillImage != null)
        {
            Color c = t < lowBelow ? lowColor : t < halfBelow ? halfColor : fullColor;
            if (empty) c.a = Mathf.PingPong(Time.unscaledTime * 2f, 1f) * 0.6f;
            fillImage.color = c;
            // a nearly empty bar would be narrower than its 2 px insets (a negative width): hide it instead
            fillImage.enabled = empty || fill.rect.width > 0.5f;
        }

        // only rebuild the text when the shown number changes (it rebuilds the TextMeshPro mesh)
        int shown = Mathf.CeilToInt(values.Fuel);
        if (shown != lastShown)
        {
            lastShown = shown;
            numberLabel.text = string.Format(numberFormat, shown, values.FuelCapacity);
        }
    }
}
