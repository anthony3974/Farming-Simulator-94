using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// One-off editor tool: adds the "Touch Look" object (its own Canvas) to the open scene (GameScene), with the
// TouchLook script and its InputManager reference filled in.
// Use: open GameScene, Tools > Build Touch Look Area, save the scene. Delete this file afterwards (git history).
//
// Touch Look        Canvas, sort order -100: below every other canvas (counters 0, speedometer 40, touch
//                   buttons 50, pause 55, help and dealer 60), so their buttons and panels get the touch first
//   Look Area       invisible full-screen image that catches the drags + TouchLook. Resize it in the editor
//                   to keep drag-to-look out of part of the screen.
public static class TouchLookBuilder
{
    const string RootName = "Touch Look";

    [MenuItem("Tools/Build Touch Look Area")]
    static void Build()
    {
        var input = Object.FindFirstObjectByType<InputManager>();
        if (input == null)
        {
            EditorUtility.DisplayDialog("Touch Look", "No InputManager in the open scene. Open GameScene first.", "OK");
            return;
        }

        var old = GameObject.Find(RootName);
        if (old != null)
        {
            if (!EditorUtility.DisplayDialog("Touch Look", "The scene already has a Touch Look object. Replace it? Any changes made to it are lost.", "Replace", "Cancel"))
                return;
            Undo.DestroyObjectImmediate(old);
        }

        var root = new GameObject(RootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(root, "Build Touch Look Area");
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -100;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var area = new GameObject("Look Area", typeof(RectTransform), typeof(Image), typeof(TouchLook));
        area.transform.SetParent(root.transform, false);
        var rt = (RectTransform)area.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        // fully transparent, but still catches touches (raycasts ignore the alpha)
        var image = area.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = true;
        area.GetComponent<TouchLook>().input = input;

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = area;
        Debug.Log("Touch Look added. Save the scene (Ctrl+S). To test in the editor, tick Show Touch Controls on the InputManager and drag on the game view.", area);
    }
}
