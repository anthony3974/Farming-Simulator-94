using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// One-off editor tool: makes the buttons in the open scene match the on-screen touch buttons
// (TouchControls.cs): a yellow square with a brown fill inset 5 px (the brown of the counters panels),
// instead of a brown button with an Outline effect.
//  - "Pause Button" (GameScene) becomes an exact copy of the touch style: bold Arial label, best fit 20 to 80.
//  - Every other button with an Outline (pause menu, main menu) gets the same border and fill and keeps its
//    text. A greyed-out button (LOAD GAME) keeps its grey.
// Use: open GameScene, Tools > Match Button Style, save. Then the same in MainMenu. Delete this file afterwards.
public static class ButtonStyleFixer
{
    static readonly Color Yellow = new Color32(255, 204, 0, 255);
    static readonly Color Fill = new Color32(60, 45, 20, 255);
    static readonly Color Grey = new Color32(70, 70, 70, 255);
    static readonly Color GreyBorder = new Color32(153, 153, 153, 255);
    static readonly Color Gold = new Color32(255, 201, 51, 255);
    const float Inset = 5f;

    [MenuItem("Tools/Match Button Style")]
    static void Run()
    {
        int count = 0;
        foreach (var button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var outline = button.GetComponent<Outline>();
            var image = button.GetComponent<Image>();
            if (outline == null || image == null || button.transform.Find("Fill") != null) continue;

            bool greyedOut = !button.interactable;
            Undo.RegisterFullObjectHierarchyUndo(button.gameObject, "Match Button Style");
            Undo.DestroyObjectImmediate(outline);

            image.color = greyedOut ? GreyBorder : Yellow;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(fillGo, "Match Button Style");
            fillGo.transform.SetParent(button.transform, false);
            fillGo.transform.SetAsFirstSibling(); // behind the label
            var fillRt = (RectTransform)fillGo.transform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(Inset, Inset);
            fillRt.offsetMax = new Vector2(-Inset, -Inset);
            var fill = fillGo.GetComponent<Image>();
            fill.color = greyedOut ? Grey : Fill;
            fill.raycastTarget = false;

            // the press tint goes on the fill (darkens it a little while held)
            button.targetGraphic = fill;
            var colors = button.colors;
            colors.normalColor = colors.highlightedColor = colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            if (button.name == "Pause Button") MakeTouchLabel(button.transform);
            count++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Match Button Style: restyled {count} button(s). Save the scene.");
    }

    // swap the TMP label for the same label the touch buttons use (legacy Text, bold Arial, best fit)
    static void MakeTouchLabel(Transform button)
    {
        var old = button.GetComponentInChildren<TMP_Text>(true);
        string text = old != null ? old.text : "PAUSE";
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
        Undo.RegisterCreatedObjectUndo(go, "Match Button Style");
        go.transform.SetParent(button, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(8, 8);
        rt.offsetMax = new Vector2(-8, -8);
        var t = go.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = text;
        t.color = Gold;
        t.alignment = TextAnchor.MiddleCenter;
        t.fontStyle = FontStyle.Bold;
        t.resizeTextForBestFit = true;
        t.resizeTextMinSize = 20;
        t.resizeTextMaxSize = 80;
        t.raycastTarget = false;
    }
}
