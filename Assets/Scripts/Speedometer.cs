using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dial speedometer in the bottom-left corner with a moving needle and a number in the middle.
/// Built entirely at runtime: the dial face is drawn into a texture, the needle is a rotated rectangle.
/// </summary>
public class Speedometer : MonoBehaviour
{
    public Transform target;

    [Tooltip("The vehicle's fastest speed in world units per second (set by GameCharacter).")]
    public float maxWorldSpeed = 30f;

    [Tooltip("What the speedometer shows at maxWorldSpeed (the current truck's top speed).")]
    public float maxDisplayKmh = 50f;

    [Tooltip("The speed at the end of the dial. Higher than the truck's top speed, so faster vehicles fit later.")]
    public float dialMaxKmh = 100f;

    [Tooltip("Dial diameter in canvas pixels (canvas is 1920x1080).")]
    public float dialSize = 260f;

    // the needle sweeps 270 degrees: -135 (zero, bottom-left) to +135 (max, bottom-right)
    private const float SweepDegrees = 270f;
    private const int TexSize = 256;
    private const int MajorStep = 10; // km/h between numbered ticks
    private const int MinorStep = 5;

    private static readonly Color Gold = new(1f, 0.8f, 0f, 1f);
    private static readonly Color Face = new Color32(60, 45, 20, 255);
    private static readonly Color LabelGold = new Color32(255, 201, 51, 255);
    private static readonly Color NeedleColor = new Color32(255, 80, 50, 255);

    private TMP_Text numberLabel;
    private RectTransform needle;
    private Vector3 lastPos;
    private float smoothed;

    void Start()
    {
        if (target == null) target = transform;
        lastPos = target.position;
        BuildUI();
    }

    void LateUpdate()
    {
        if (target == null || Time.deltaTime <= 0f) return;

        Vector3 delta = target.position - lastPos;
        delta.y = 0f; // ignore gravity / hills
        lastPos = target.position;

        float kmh = delta.magnitude / Time.deltaTime / maxWorldSpeed * maxDisplayKmh;
        smoothed = Mathf.Lerp(smoothed, kmh, 10f * Time.deltaTime);

        numberLabel.text = $"{smoothed:0}\n<size=40%><color=#FFC933>km/h</color></size>";

        float t = Mathf.Clamp01(smoothed / dialMaxKmh);
        needle.localRotation = Quaternion.Euler(0, 0, SweepDegrees / 2f - t * SweepDegrees);
    }

    // ---- UI ----

    private void BuildUI()
    {
        var canvasGo = new GameObject("Speedometer", typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // dial face
        var dial = new GameObject("Dial", typeof(RectTransform), typeof(Image));
        dial.transform.SetParent(canvasGo.transform, false);
        var drt = (RectTransform)dial.transform;
        drt.anchorMin = drt.anchorMax = drt.pivot = Vector2.zero;
        drt.anchoredPosition = new Vector2(20, 20);
        drt.sizeDelta = new Vector2(dialSize, dialSize);
        var faceImage = dial.GetComponent<Image>();
        faceImage.sprite = MakeDialSprite();
        faceImage.raycastTarget = false;

        float half = dialSize / 2f;
        float scale = dialSize / TexSize;

        // numbers around the dial
        for (int v = 0; v <= dialMaxKmh + 0.01f; v += MajorStep)
        {
            float angle = AngleFor(v / dialMaxKmh); // degrees, 0 = up, clockwise positive
            float rad = angle * Mathf.Deg2Rad;
            float r = (TexSize / 2f - 44f) * scale;
            var go = new GameObject("Tick" + v, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(dial.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(50, 30);
            rt.anchoredPosition = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * r;
            var txt = go.GetComponent<TextMeshProUGUI>();
            txt.text = v.ToString();
            txt.fontSize = 22;
            txt.color = LabelGold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.raycastTarget = false;
        }

        // needle: pivots around the dial centre
        var needleGo = new GameObject("Needle", typeof(RectTransform), typeof(Image));
        needleGo.transform.SetParent(dial.transform, false);
        needle = (RectTransform)needleGo.transform;
        needle.anchorMin = needle.anchorMax = new Vector2(0.5f, 0.5f);
        needle.pivot = new Vector2(0.5f, 0f);
        needle.anchoredPosition = Vector2.zero;
        needle.sizeDelta = new Vector2(6f, half * 0.72f);
        var ni = needleGo.GetComponent<Image>();
        ni.color = NeedleColor;
        ni.raycastTarget = false;

        // hub over the needle base
        var hub = new GameObject("Hub", typeof(RectTransform), typeof(Image));
        hub.transform.SetParent(dial.transform, false);
        var hrt = (RectTransform)hub.transform;
        hrt.anchorMin = hrt.anchorMax = hrt.pivot = new Vector2(0.5f, 0.5f);
        hrt.anchoredPosition = Vector2.zero;
        hrt.sizeDelta = new Vector2(26, 26);
        var hi = hub.GetComponent<Image>();
        hi.sprite = MakeDiscSprite(Gold);
        hi.raycastTarget = false;

        // raw number under the hub
        var numGo = new GameObject("Number", typeof(RectTransform), typeof(TextMeshProUGUI));
        numGo.transform.SetParent(dial.transform, false);
        var nrt = (RectTransform)numGo.transform;
        nrt.anchorMin = nrt.anchorMax = nrt.pivot = new Vector2(0.5f, 0.5f);
        nrt.sizeDelta = new Vector2(140, 80);
        nrt.anchoredPosition = new Vector2(0, -half * 0.42f);
        numberLabel = numGo.GetComponent<TextMeshProUGUI>();
        numberLabel.fontSize = 40;
        numberLabel.color = Color.white;
        numberLabel.alignment = TextAlignmentOptions.Center;
        numberLabel.raycastTarget = false;
    }

    // angle on the dial for a 0..1 fraction: -135 at 0, +135 at 1 (0 = straight up, clockwise positive)
    private static float AngleFor(float t) => -SweepDegrees / 2f + t * SweepDegrees;

    // ---- procedural textures ----

    private Sprite MakeDialSprite()
    {
        var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };
        var pixels = new Color[TexSize * TexSize];
        float c = (TexSize - 1) / 2f;
        float outer = TexSize / 2f - 1f;
        float ringWidth = 7f;
        float majorDeg = SweepDegrees * MajorStep / dialMaxKmh;
        float minorDeg = SweepDegrees * MinorStep / dialMaxKmh;

        for (int y = 0; y < TexSize; y++)
        {
            for (int x = 0; x < TexSize; x++)
            {
                float dx = x - c, dy = y - c;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float edge = Mathf.Clamp01(outer - r + 0.5f); // antialiased outer edge

                Color col = r > outer - ringWidth ? Gold : Face;

                // tick marks inside the ring, only within the sweep
                float angle = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg; // 0 = up, clockwise positive
                if (r < outer - ringWidth - 1f && angle >= -SweepDegrees / 2f - 3f && angle <= SweepDegrees / 2f + 3f)
                {
                    float fromStart = angle + SweepDegrees / 2f;
                    float nearMajor = Mathf.Abs(fromStart - Mathf.Round(fromStart / majorDeg) * majorDeg);
                    float nearMinor = Mathf.Abs(fromStart - Mathf.Round(fromStart / minorDeg) * minorDeg);
                    float halfWidthDeg = 1.6f / Mathf.Max(r, 1f) * Mathf.Rad2Deg; // about 3 px wide at any radius

                    if (nearMajor < halfWidthDeg && r > outer - ringWidth - 20f) col = Gold;
                    else if (nearMinor < halfWidthDeg && r > outer - ringWidth - 11f) col = LabelGold;
                }

                col.a = edge;
                pixels[y * TexSize + x] = col;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, TexSize, TexSize), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite MakeDiscSprite(Color color)
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        float c = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float r = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                var col = color;
                col.a = Mathf.Clamp01(size / 2f - 1f - r + 0.5f);
                tex.SetPixel(x, y, col);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
