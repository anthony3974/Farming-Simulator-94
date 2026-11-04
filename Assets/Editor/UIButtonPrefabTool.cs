using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// One-off editor tool for the shared button prefab, Assets/Prefabs/UI/UI Button.prefab.
//   Tools > UI Button > 1. Create Prefab
//       Makes the prefab: a yellow square (the Button), a brown "Fill" inset 5 px, a gold bold "Label"
//       (TextMeshPro, shrinks to fit between 14 and 80). Change the prefab to restyle every button at once.
//   Tools > UI Button > 2. Replace Buttons In Open Scene
//       Swaps each yellow-border button in the open scene (the ones with a "Fill" child) for a copy of the
//       prefab: same name, place, size, text and text size, and every script slot that pointed at the old
//       button or its label points at the new one. A greyed-out button (LOAD GAME) is dimmed with a CanvasGroup.
// Run 1 once, then 2 in GameScene and in MainMenu (save each). Delete this file afterwards (git history).
public static class UIButtonPrefabTool
{
    const string Folder = "Assets/Prefabs/UI";
    const string PrefabPath = Folder + "/UI Button.prefab";

    static readonly Color Yellow = new Color32(255, 204, 0, 255);
    static readonly Color Fill = new Color32(60, 45, 20, 255);
    static readonly Color Gold = new Color32(255, 201, 51, 255);

    [MenuItem("Tools/UI Button/1. Create Prefab")]
    static void CreatePrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null &&
            !EditorUtility.DisplayDialog("UI Button", "UI Button.prefab already exists. Overwrite it? Changes made to it are lost.", "Overwrite", "Cancel"))
            return;
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Prefabs", "UI");

        var root = new GameObject("UI Button", typeof(RectTransform), typeof(Image), typeof(Button));
        ((RectTransform)root.transform).sizeDelta = new Vector2(300, 110);
        root.GetComponent<Image>().color = Yellow; // the border

        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(root.transform, false);
        var fillRt = (RectTransform)fillGo.transform;
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(5, 5);
        fillRt.offsetMax = new Vector2(-5, -5);
        var fill = fillGo.GetComponent<Image>();
        fill.color = Fill;
        fill.raycastTarget = false;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(root.transform, false);
        var labelRt = (RectTransform)labelGo.transform;
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(8, 8);
        labelRt.offsetMax = new Vector2(-8, -8);
        var label = labelGo.GetComponent<TextMeshProUGUI>();
        label.text = "BUTTON";
        label.color = Gold;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = 14;
        label.fontSizeMax = 80;
        label.fontSize = 80;
        label.raycastTarget = false;

        // pressing darkens the fill a little; a turned-off button gets a darker fill
        var button = root.GetComponent<Button>();
        button.targetGraphic = fill;
        var colors = button.colors;
        colors.normalColor = colors.highlightedColor = colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Debug.Log("Created " + PrefabPath + ". Now open a scene and run Tools > UI Button > 2. Replace Buttons In Open Scene.");
    }

    [MenuItem("Tools/UI Button/2. Replace Buttons In Open Scene")]
    static void ReplaceInScene()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) { Debug.LogError("Run Tools > UI Button > 1. Create Prefab first."); return; }

        var scene = EditorSceneManager.GetActiveScene();
        var scripts = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;

        foreach (var old in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (old.transform.Find("Fill") == null) continue;                      // not a yellow-border button
            if (PrefabUtility.GetCorrespondingObjectFromSource(old.gameObject) == prefab) continue; // already done

            var oldRt = (RectTransform)old.transform;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(go, "Replace Buttons");
            go.transform.SetParent(oldRt.parent, false);
            go.transform.SetSiblingIndex(oldRt.GetSiblingIndex());
            go.name = old.name;
            go.SetActive(old.gameObject.activeSelf);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = oldRt.anchorMin;
            rt.anchorMax = oldRt.anchorMax;
            rt.pivot = oldRt.pivot;
            rt.anchoredPosition = oldRt.anchoredPosition;
            rt.sizeDelta = oldRt.sizeDelta;
            rt.localRotation = oldRt.localRotation;
            rt.localScale = oldRt.localScale;

            var button = go.GetComponent<Button>();
            button.interactable = old.interactable;
            if (!old.interactable) go.AddComponent<CanvasGroup>().alpha = 0.5f; // greyed out (LOAD GAME)
            if (old.onClick.GetPersistentEventCount() > 0)
                Debug.LogWarning($"'{old.name}' had On Click events set in the Inspector; set them again on the new button.", go);

            // the text and its size
            var label = go.GetComponentInChildren<TMP_Text>(true);
            var oldTmp = old.GetComponentInChildren<TMP_Text>(true);
            var oldText = old.GetComponentInChildren<Text>(true);
            if (oldTmp != null)
            {
                label.text = oldTmp.text;
                label.enableAutoSizing = oldTmp.enableAutoSizing;
                label.fontSize = oldTmp.fontSize;
                if (oldTmp.enableAutoSizing) { label.fontSizeMin = oldTmp.fontSizeMin; label.fontSizeMax = oldTmp.fontSizeMax; }
            }
            else if (oldText != null)
            {
                label.text = oldText.text;
                label.enableAutoSizing = oldText.resizeTextForBestFit;
                if (oldText.resizeTextForBestFit) { label.fontSizeMin = oldText.resizeTextMinSize; label.fontSizeMax = oldText.resizeTextMaxSize; }
                else label.fontSize = oldText.fontSize;
            }

            // point every script slot that used the old button (or its parts) at the new one
            var map = new Dictionary<Object, Object>
            {
                { old, button },
                { old.gameObject, go },
                { oldRt, rt },
                { old.GetComponent<Image>(), go.GetComponent<Image>() },
            };
            if (oldTmp != null) map[oldTmp] = label;
            foreach (var s in scripts)
            {
                if (s == null || s.transform.IsChildOf(oldRt)) continue;
                var so = new SerializedObject(s);
                var p = so.GetIterator();
                bool changed = false;
                while (p.Next(true))
                {
                    if (p.propertyType != SerializedPropertyType.ObjectReference || p.objectReferenceValue == null) continue;
                    if (map.TryGetValue(p.objectReferenceValue, out var replacement)) { p.objectReferenceValue = replacement; changed = true; }
                }
                if (changed) so.ApplyModifiedProperties(); // records its own undo step
            }

            Undo.DestroyObjectImmediate(old.gameObject);
            count++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"Replaced {count} button(s) with UI Button prefab copies in {scene.name}. Save the scene.");
    }
}
