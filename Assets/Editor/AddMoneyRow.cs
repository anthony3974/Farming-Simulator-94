using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>One-off helper: adds a Money row at the top of the Crops counters panel.</summary>
public static class AddMoneyRow
{
    [MenuItem("Tools/Add Money Row To Counters")]
    public static void Run()
    {
        var crops = GameObject.Find("Crops");
        var pv = Object.FindFirstObjectByType<PlayerValues>();
        if (crops == null || pv == null) { Debug.LogError("Crops panel or PlayerValues not found."); return; }
        if (pv.txMoney != null) { Debug.Log("Money row already exists."); return; }

        var cropsRt = (RectTransform)crops.transform;
        Undo.RecordObject(cropsRt, "Add money row");
        cropsRt.sizeDelta += new Vector2(0, 50);

        RectTransform labelCol = (RectTransform)cropsRt.GetChild(1);
        RectTransform valueCol = (RectTransform)cropsRt.GetChild(2);
        TMP_Text moneyValue = null;

        foreach (var col in new[] { labelCol, valueCol })
        {
            Undo.RecordObject(col, "Add money row");
            col.sizeDelta += new Vector2(0, 50);

            var copy = Object.Instantiate(col.GetChild(0).gameObject, col);
            Undo.RegisterCreatedObjectUndo(copy, "Add money row");
            copy.name = "Money";
            copy.transform.SetSiblingIndex(0);

            for (int i = 0; i < col.childCount; i++)
            {
                var rt = (RectTransform)col.GetChild(i);
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.sizeDelta = new Vector2(0, 50);
                rt.anchoredPosition = new Vector2(0, -25 - 50 * i);
            }

            var txt = copy.GetComponent<TMP_Text>();
            if (col == labelCol) txt.text = "Money:";
            else { txt.text = "0.00$"; moneyValue = txt; }
        }

        Undo.RecordObject(pv, "Add money row");
        pv.txMoney = moneyValue;
        EditorUtility.SetDirty(pv);
        EditorSceneManager.MarkSceneDirty(pv.gameObject.scene);
        Debug.Log("Money row added. Save the scene (Ctrl+S).");
    }
}
