using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// One-off editor tool: adds the diesel gauge to the open scene (GameScene) as real objects, with the FuelGauge
// script and its references filled in. Plain positioned objects, no layout groups, so it can be moved, resized
// and restyled in the editor. Same look as the HUD (yellow border, brown panel, gold label).
// Use: open GameScene, Tools > Build Fuel Gauge, save the scene. Delete this file afterwards (git history).
//
//   Fuel Gauge         Canvas (sort order 40, like the speedometer; no raycaster, never blocks a touch) + FuelGauge
//     Box              yellow border, bottom-left, just above the speedometer dial (260 x 64)
//       Panel          brown, inset 4 px
//         Label        "DIESEL" (gold, left)
//         Number       litres left (white, right)
//         Track        dark bar background, along the bottom
//           Fill       the bar; FuelGauge sets its length and colour
public static class FuelGaugeBuilder
{
    static readonly Color Yellow = new(1f, 0.8f, 0f, 1f);
    static readonly Color Brown = new Color32(60, 45, 20, 255);
    static readonly Color LabelGold = new Color32(255, 201, 51, 255);
    static readonly Color TrackColor = new Color32(25, 18, 8, 255);

    [MenuItem("Tools/Build Fuel Gauge")]
    static void Build()
    {
        if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Fuel Gauge", "Leave Play mode first.", "OK"); return; }
        if (Object.FindFirstObjectByType<PlayerValues>() == null &&
            !EditorUtility.DisplayDialog("Fuel Gauge", "This scene has no PlayerValues (is GameScene open?). Build anyway?", "Build", "Cancel"))
            return;

        var old = GameObject.Find("Fuel Gauge");
        if (old != null)
        {
            if (!EditorUtility.DisplayDialog("Fuel Gauge", "The scene already has a Fuel Gauge. Replace it? Any changes made to it are lost.", "Replace", "Cancel"))
                return;
            Undo.DestroyObjectImmediate(old);
        }

        var root = new GameObject("Fuel Gauge", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Undo.RegisterCreatedObjectUndo(root, "Build Fuel Gauge");
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        var gauge = root.AddComponent<FuelGauge>();

        // the speedometer dial fills x 20 to 280, y 20 to 280: sit 10 px above it, the same width
        var box = MakeImage(root.transform, "Box", Yellow);
        var boxRt = (RectTransform)box.transform;
        boxRt.anchorMin = boxRt.anchorMax = boxRt.pivot = Vector2.zero;
        boxRt.anchoredPosition = new Vector2(20, 290);
        boxRt.sizeDelta = new Vector2(260, 64);

        var panel = MakeImage(box.transform, "Panel", Brown);
        Stretch((RectTransform)panel.transform, 4f, 4f);

        var label = MakeText(panel.transform, "Label", "DIESEL", 22, LabelGold, TextAlignmentOptions.Left);
        Place(label.rectTransform, new Vector2(0, 1), new Vector2(12, -4), new Vector2(130, 28));

        var number = MakeText(panel.transform, "Number", string.Format(gauge.numberFormat, 100, 100), 24, Color.white, TextAlignmentOptions.Right);
        Place(number.rectTransform, new Vector2(1, 1), new Vector2(-12, -3), new Vector2(110, 28));
        number.fontStyle = FontStyles.Bold;

        var track = MakeImage(panel.transform, "Track", TrackColor);
        var trackRt = (RectTransform)track.transform;
        trackRt.anchorMin = new Vector2(0, 0);
        trackRt.anchorMax = new Vector2(1, 0);
        trackRt.pivot = new Vector2(0.5f, 0);
        trackRt.offsetMin = new Vector2(12, 9);
        trackRt.offsetMax = new Vector2(-12, 9 + 16);

        var fill = MakeImage(track.transform, "Fill", gauge.fullColor);
        var fillRt = (RectTransform)fill.transform;
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one; // FuelGauge moves the right edge (anchor max x) to the part of the tank left
        fillRt.offsetMin = new Vector2(2, 2);
        fillRt.offsetMax = new Vector2(-2, -2);

        gauge.fill = fillRt;
        gauge.fillImage = fill.GetComponent<Image>();
        gauge.numberLabel = number;

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log("Fuel Gauge added above the speedometer. Save the scene (Ctrl+S).", root);
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

    static TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        return t;
    }

    // anchored to a corner of the parent (anchor = pivot), offset from it
    static void Place(RectTransform rt, Vector2 corner, Vector2 offset, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = corner;
        rt.anchoredPosition = offset;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt, float x, float y)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(x, y);
        rt.offsetMax = new Vector2(-x, -y);
    }
}
