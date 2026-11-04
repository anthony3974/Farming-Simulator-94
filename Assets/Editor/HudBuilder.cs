using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// One-off editor tool: adds the message box and the speedometer to the open scene (GameScene) as real objects,
// with their scripts and references filled in. Plain positioned objects, no layout groups, so they can be moved,
// resized and restyled freely in the editor afterwards. Same look and place as the old runtime versions.
// Use: open GameScene, run both menu items, save the scene. Delete this file afterwards (git history).
//
// Tools > Build Message Box
//   Message Box        Canvas (sort order 50, same as the touch buttons) + ScreenPrompt
//     Box              yellow border, centre of the screen; turned OFF in the editor (tick it to work on it).
//                      ScreenPrompt sets its height to fit the text; move it and change its width freely
//       Panel > Text   brown panel inset 4 px, white text with 24 / 14 px padding
//
// Tools > Build Speedometer
//   Speedometer        Canvas (sort order 40: under the touch buttons) + Speedometer
//     Dial             dial face image (Assets/Images/Speedometer/Dial.png, drawn by this tool), bottom-left
//       Numbers        0, 10, ... 100 around the dial
//       Needle         turns around its pivot (the dial centre)
//       Hub            gold disc over the needle base (Hub.png)
//       Number         the speed
public static class HudBuilder
{
    static readonly Color Yellow = new(1f, 0.8f, 0f, 1f);
    static readonly Color Brown = new Color32(60, 45, 20, 255);
    static readonly Color LabelGold = new Color32(255, 201, 51, 255);
    static readonly Color NeedleColor = new Color32(255, 80, 50, 255);

    const string ImageFolder = "Assets/Images/Speedometer";

    // ---------------- message box ----------------

    [MenuItem("Tools/Build Message Box")]
    static void BuildMessageBox()
    {
        if (!ReplaceExisting("Message Box")) return;

        var root = MakeCanvas("Message Box", 50);
        var prompt = root.AddComponent<ScreenPrompt>();

        var box = MakeImage(root.transform, "Box", Yellow);
        var boxRt = (RectTransform)box.transform;
        boxRt.anchorMin = boxRt.anchorMax = boxRt.pivot = new Vector2(0.5f, 0.5f);
        boxRt.anchoredPosition = Vector2.zero;
        boxRt.sizeDelta = new Vector2(760, 80); // one line; the height follows the text at runtime

        var panel = MakeImage(box.transform, "Panel", Brown);
        Stretch((RectTransform)panel.transform, 4f, 4f);

        var label = MakeText(panel.transform, "Text", "Press E to sell Wheat", 30, Color.white);
        Stretch(label.rectTransform, 24f, 14f);

        prompt.box = boxRt;
        prompt.label = label;
        box.SetActive(false);

        Done(root, "Message Box added. Save the scene (Ctrl+S). Its Box is turned off in the editor: tick it to see and edit it.");
    }

    // ---------------- speedometer ----------------

    const float DialSize = 260f;
    const int TexSize = 256;
    const int MajorStep = 10; // km/h between numbered ticks
    const int MinorStep = 5;

    [MenuItem("Tools/Build Speedometer")]
    static void BuildSpeedometer()
    {
        // keep the dial range of an existing speedometer
        var existing = Object.FindFirstObjectByType<Speedometer>();
        float dialMax = existing != null ? existing.dialMaxKmh : 100f;
        if (!ReplaceExisting("Speedometer")) return;

        var dialSprite = SaveSprite("Dial.png", MakeDialTexture(dialMax));
        var hubSprite = SaveSprite("Hub.png", MakeDiscTexture(Yellow));

        var root = MakeCanvas("Speedometer", 40);
        var speedo = root.AddComponent<Speedometer>();
        speedo.dialMaxKmh = dialMax;

        var dial = MakeImage(root.transform, "Dial", Color.white);
        dial.GetComponent<Image>().sprite = dialSprite;
        var drt = (RectTransform)dial.transform;
        drt.anchorMin = drt.anchorMax = drt.pivot = Vector2.zero;
        drt.anchoredPosition = new Vector2(20, 20);
        drt.sizeDelta = new Vector2(DialSize, DialSize);

        float half = DialSize / 2f;
        float scale = DialSize / TexSize;

        // numbers around the dial
        var numbers = new GameObject("Numbers", typeof(RectTransform));
        numbers.transform.SetParent(dial.transform, false);
        Stretch((RectTransform)numbers.transform, 0f, 0f);
        for (int v = 0; v <= dialMax + 0.01f; v += MajorStep)
        {
            float rad = AngleFor(v / dialMax) * Mathf.Deg2Rad;
            float r = (TexSize / 2f - 44f) * scale;
            var txt = MakeText(numbers.transform, v.ToString(), v.ToString(), 22, LabelGold);
            var rt = txt.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(50, 30);
            rt.anchoredPosition = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * r;
        }

        // needle: pivots around the dial centre; shown at zero (Speedometer turns it)
        var needle = MakeImage(dial.transform, "Needle", NeedleColor);
        var nrt = (RectTransform)needle.transform;
        nrt.anchorMin = nrt.anchorMax = new Vector2(0.5f, 0.5f);
        nrt.pivot = new Vector2(0.5f, 0f);
        nrt.anchoredPosition = Vector2.zero;
        nrt.sizeDelta = new Vector2(6f, half * 0.72f);
        nrt.localRotation = Quaternion.Euler(0, 0, Speedometer.SweepDegrees / 2f);

        var hub = MakeImage(dial.transform, "Hub", Color.white);
        hub.GetComponent<Image>().sprite = hubSprite;
        var hrt = (RectTransform)hub.transform;
        hrt.anchorMin = hrt.anchorMax = hrt.pivot = new Vector2(0.5f, 0.5f);
        hrt.anchoredPosition = Vector2.zero;
        hrt.sizeDelta = new Vector2(26, 26);

        var number = MakeText(dial.transform, "Number", string.Format(speedo.numberFormat, 0), 40, Color.white);
        var numRt = number.rectTransform;
        numRt.anchorMin = numRt.anchorMax = numRt.pivot = new Vector2(0.5f, 0.5f);
        numRt.sizeDelta = new Vector2(140, 80);
        numRt.anchoredPosition = new Vector2(0, -half * 0.42f);

        speedo.needle = nrt;
        speedo.numberLabel = number;

        Done(root, "Speedometer added. Save the scene (Ctrl+S). The dial images are in " + ImageFolder + ".");
    }

    // angle on the dial for a 0..1 fraction: -135 at 0, +135 at 1 (0 = straight up, clockwise positive)
    static float AngleFor(float t) => -Speedometer.SweepDegrees / 2f + t * Speedometer.SweepDegrees;

    // the old runtime dial face: gold ring, brown face, gold tick marks every 5 and 10 km/h
    static Texture2D MakeDialTexture(float dialMax)
    {
        const float sweep = Speedometer.SweepDegrees;
        var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
        var pixels = new Color[TexSize * TexSize];
        float c = (TexSize - 1) / 2f;
        float outer = TexSize / 2f - 1f;
        float ringWidth = 7f;
        float majorDeg = sweep * MajorStep / dialMax;
        float minorDeg = sweep * MinorStep / dialMax;

        for (int y = 0; y < TexSize; y++)
        {
            for (int x = 0; x < TexSize; x++)
            {
                float dx = x - c, dy = y - c;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float edge = Mathf.Clamp01(outer - r + 0.5f); // antialiased outer edge

                Color col = r > outer - ringWidth ? Yellow : Brown;

                // tick marks inside the ring, only within the sweep
                float angle = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg; // 0 = up, clockwise positive
                if (r < outer - ringWidth - 1f && angle >= -sweep / 2f - 3f && angle <= sweep / 2f + 3f)
                {
                    float fromStart = angle + sweep / 2f;
                    float nearMajor = Mathf.Abs(fromStart - Mathf.Round(fromStart / majorDeg) * majorDeg);
                    float nearMinor = Mathf.Abs(fromStart - Mathf.Round(fromStart / minorDeg) * minorDeg);
                    float halfWidthDeg = 1.6f / Mathf.Max(r, 1f) * Mathf.Rad2Deg; // about 3 px wide at any radius

                    if (nearMajor < halfWidthDeg && r > outer - ringWidth - 20f) col = Yellow;
                    else if (nearMinor < halfWidthDeg && r > outer - ringWidth - 11f) col = LabelGold;
                }

                col.a = edge;
                pixels[y * TexSize + x] = col;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    static Texture2D MakeDiscTexture(Color color)
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
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
        return tex;
    }

    // writes the texture as a PNG in ImageFolder and imports it as a crisp UI sprite
    static Sprite SaveSprite(string fileName, Texture2D tex)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Images")) AssetDatabase.CreateFolder("Assets", "Images");
        if (!AssetDatabase.IsValidFolder(ImageFolder)) AssetDatabase.CreateFolder("Assets/Images", "Speedometer");
        string path = ImageFolder + "/" + fileName;
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ---------------- helpers ----------------

    static bool ReplaceExisting(string rootName)
    {
        var old = GameObject.Find(rootName);
        if (old == null) return true;
        if (!EditorUtility.DisplayDialog(rootName, $"The scene already has a {rootName}. Replace it? Any changes made to it are lost.", "Replace", "Cancel"))
            return false;
        Undo.DestroyObjectImmediate(old);
        return true;
    }

    // display-only canvas: no GraphicRaycaster, so it never blocks a touch
    static GameObject MakeCanvas(string name, int sortingOrder)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Undo.RegisterCreatedObjectUndo(root, "Build " + name);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return root;
    }

    static GameObject MakeImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return go;
    }

    static TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    // fills the parent, with a margin of x on the left and right and y at the top and bottom
    static void Stretch(RectTransform rt, float x, float y)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(x, y);
        rt.offsetMax = new Vector2(-x, -y);
    }

    static void Done(GameObject root, string message)
    {
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log(message, root);
    }
}
