using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-off: moves Wheat/Barley/Oat into a centred bar along the top of the screen and turns the
/// right-hand "Crops" panel into "Statistics" with Money, Seeds and Fertilizer.
/// </summary>
public static class RebuildCounters
{
    private static readonly Color Gold = new(1f, 0.8f, 0f, 1f);
    private static readonly Color Dark = new Color32(60, 45, 20, 255);

    [MenuItem("Tools/Rebuild Counters")]
    public static void Run()
    {
        var rootGo = GameObject.Find("Crops");
        var pv = Object.FindFirstObjectByType<PlayerValues>();
        if (rootGo == null || pv == null) { Debug.LogError("'Crops' panel or PlayerValues not found (already rebuilt?)."); return; }

        Transform root = rootGo.transform;
        Transform labels = root.Find("Text");
        Transform values = root.Find("Numbers");
        if (labels == null || values == null) { Debug.LogError("Expected Crops > Text and Crops > Numbers."); return; }

        Color labelGold = labels.Find("Wheat").GetComponent<TMP_Text>().color;

        // ---- top bar with Wheat / Barley / Oat ----
        var bar = new GameObject("CropBar", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        Undo.RegisterCreatedObjectUndo(bar, "Rebuild counters");
        bar.layer = root.gameObject.layer;
        bar.transform.SetParent(root.parent, false);
        var brt = (RectTransform)bar.transform;
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 1f);
        brt.pivot = new Vector2(0.5f, 1f);
        brt.anchoredPosition = new Vector2(0, -20);
        bar.GetComponent<Image>().color = Gold;
        var outer = bar.GetComponent<HorizontalLayoutGroup>();
        outer.padding = new RectOffset(4, 4, 4, 4);
        outer.childControlWidth = outer.childControlHeight = true;
        outer.childForceExpandWidth = outer.childForceExpandHeight = false;
        var fit = bar.GetComponent<ContentSizeFitter>();
        fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var inner = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        inner.layer = bar.layer;
        inner.transform.SetParent(bar.transform, false);
        inner.GetComponent<Image>().color = Dark;
        var ihl = inner.GetComponent<HorizontalLayoutGroup>();
        ihl.padding = new RectOffset(32, 32, 10, 10);
        ihl.spacing = 48;
        ihl.childAlignment = TextAnchor.MiddleCenter;
        ihl.childControlWidth = ihl.childControlHeight = true;
        ihl.childForceExpandWidth = ihl.childForceExpandHeight = false;

        foreach (string crop in new[] { "Wheat", "Barley", "Oat" })
        {
            var group = new GameObject(crop, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            group.layer = bar.layer;
            group.transform.SetParent(inner.transform, false);
            var ghl = group.GetComponent<HorizontalLayoutGroup>();
            ghl.spacing = 10;
            ghl.childAlignment = TextAnchor.MiddleCenter;
            ghl.childControlWidth = ghl.childControlHeight = true;
            ghl.childForceExpandWidth = ghl.childForceExpandHeight = false;

            Move(labels.Find(crop), group.transform, 0);
            Move(values.Find(crop), group.transform, 100);
        }

        // ---- the right-hand panel becomes "Statistics" ----
        rootGo.name = "Statistics";
        ((RectTransform)root).sizeDelta = new Vector2(300, 174);
        ((RectTransform)labels).sizeDelta = ((RectTransform)values).sizeDelta = new Vector2(270, 150);

        Transform moneyLabel = labels.Find("Money");
        Transform moneyValue = values.Find("Money");
        moneyLabel.GetComponent<TMP_Text>().color = labelGold;

        var seedsValue = AddRow(labels, values, moneyLabel, moneyValue, "Seeds", "Seeds:", "0", 1);
        var fertValue = AddRow(labels, values, moneyLabel, moneyValue, "Fertilizer", "Fertilizer:", "0", 2);
        Place(moneyLabel, 0); Place(moneyValue, 0);

        moneyValue.GetComponent<TMP_Text>().text = "$ 0.00";
        Undo.RecordObject(pv, "Rebuild counters");
        pv.txSeeds = seedsValue;
        pv.txFertilizer = fertValue;
        EditorUtility.SetDirty(pv);
        EditorSceneManager.MarkSceneDirty(pv.gameObject.scene);
        Debug.Log("Counters rebuilt. Save the scene (Ctrl+S).");
    }

    private static void Move(Transform t, Transform newParent, float minWidth)
    {
        t.SetParent(newParent, false);
        var rt = (RectTransform)t;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        var txt = t.GetComponent<TMP_Text>();
        txt.enableAutoSizing = false;
        txt.fontSize = 26;
        txt.textWrappingMode = TextWrappingModes.NoWrap;
        txt.alignment = TextAlignmentOptions.MidlineLeft;
        if (minWidth > 0) (t.GetComponent<LayoutElement>() ?? t.gameObject.AddComponent<LayoutElement>()).minWidth = minWidth;
    }

    private static TMP_Text AddRow(Transform labels, Transform values, Transform labelSrc, Transform valueSrc,
        string name, string labelText, string valueText, int row)
    {
        var l = Object.Instantiate(labelSrc.gameObject, labels); l.name = name;
        var v = Object.Instantiate(valueSrc.gameObject, values); v.name = name;
        Undo.RegisterCreatedObjectUndo(l, "Rebuild counters");
        Undo.RegisterCreatedObjectUndo(v, "Rebuild counters");
        l.GetComponent<TMP_Text>().text = labelText;
        var vt = v.GetComponent<TMP_Text>();
        vt.text = valueText;
        Place(l.transform, row);
        Place(v.transform, row);
        return vt;
    }

    private static void Place(Transform t, int row)
    {
        var rt = (RectTransform)t;
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.sizeDelta = new Vector2(0, 50);
        rt.anchoredPosition = new Vector2(0, -25 - 50 * row);
    }
}
