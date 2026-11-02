using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shows the target's ground speed in the bottom-left corner. Built at runtime.</summary>
public class Speedometer : MonoBehaviour
{
    public Transform target;

    [Tooltip("The vehicle's fastest speed in world units per second (set by GameCharacter).")]
    public float maxWorldSpeed = 30f;

    [Tooltip("What the speedometer shows at maxWorldSpeed.")]
    public float maxDisplayKmh = 50f;

    private TMP_Text label;
    private Vector3 lastPos;
    private float smoothed;

    void Start()
    {
        if (target == null) target = transform;
        lastPos = target.position;

        var canvasGo = new GameObject("Speedometer", typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // gold border (outer) with the dark panel inset inside it
        var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(canvasGo.transform, false);
        var brt = (RectTransform)border.transform;
        brt.anchorMin = brt.anchorMax = brt.pivot = Vector2.zero;
        brt.anchoredPosition = new Vector2(20, 20);
        brt.sizeDelta = new Vector2(220, 80);
        border.GetComponent<Image>().color = new Color(1f, 0.8f, 0f, 1f);

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(border.transform, false);
        var rt = (RectTransform)panel.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(4, 4); rt.offsetMax = new Vector2(-4, -4);
        panel.GetComponent<Image>().color = new Color32(60, 45, 20, 255);

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(panel.transform, false);
        var trt = (RectTransform)textGo.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        label = textGo.GetComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 40;
        label.color = Color.white;
    }

    void LateUpdate()
    {
        if (target == null || Time.deltaTime <= 0f) return;

        Vector3 delta = target.position - lastPos;
        delta.y = 0f; // ignore gravity / hills
        lastPos = target.position;

        float kmh = delta.magnitude / Time.deltaTime / maxWorldSpeed * maxDisplayKmh;
        smoothed = Mathf.Lerp(smoothed, kmh, 10f * Time.deltaTime);
        label.text = $"{smoothed:0} <size=50%><color=#FFC933>km/h</color></size>";
    }
}
