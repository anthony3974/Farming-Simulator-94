using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Editor tool: turns the old truck into a tractor (red, big back wheels, small front wheels, engine hood,
// cab with glass, posts and roof, exhaust pipe). Placeholder parts made of boxes and cylinders.
// Use: open GameScene, then Tools > Tractor Builder, press "Rebuild tractor", save the scene (Ctrl+S).
// It edits the prefab (Assets/Prefabs, renamed to Tractor.prefab): every child except FollowCam is replaced
// by a new "Model". The camera, the controller and the scripts on the tractor are kept. Safe to run again.
public class TractorBuilder : EditorWindow
{
    const string MaterialFolder = "Assets/Tractor";
    const string PaintPrefix = "Paint ";   // parts that take the vehicle colour (for the paint shop later)

    float steerAngle = 20f;

    [MenuItem("Tools/Tractor Builder")]
    static void Open() { GetWindow<TractorBuilder>("Tractor"); }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("Rebuilds the starting vehicle as a tractor (new model in the prefab, keeps the camera and scripts), " +
            "renames it to Tractor and sets its front wheel steering angle. Run it with GameScene open, then save the scene.", MessageType.Info);
        steerAngle = EditorGUILayout.FloatField("Front wheel angle (deg)", steerAngle);
        EditorGUILayout.Space();
        if (GUILayout.Button("Rebuild tractor")) Build();
    }

    void Build()
    {
        var player = FindFirstObjectByType<GameCharacter>();
        if (player == null) { EditorUtility.DisplayDialog("Tractor", "No GameCharacter found. Open GameScene first.", "OK"); return; }
        var go = player.gameObject;
        string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
        if (string.IsNullOrEmpty(path)) { EditorUtility.DisplayDialog("Tractor", "The starting vehicle is not a prefab instance.", "OK"); return; }

        // 1. the prefab: replace the old body with the tractor model
        var contents = PrefabUtility.LoadPrefabContents(path);
        for (int i = contents.transform.childCount - 1; i >= 0; i--)
        {
            var child = contents.transform.GetChild(i);
            if (child.name != "FollowCam") DestroyImmediate(child.gameObject);
        }
        BuildModel(contents.transform);
        contents.name = "Tractor";
        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);

        if (Path.GetFileNameWithoutExtension(path) != "Tractor")
        {
            string error = AssetDatabase.RenameAsset(path, "Tractor");
            if (!string.IsNullOrEmpty(error)) Debug.LogWarning("Could not rename the prefab: " + error);
        }

        // 2. the scene: name, steering angle, paint parts. Overrides on the old, deleted parts are cleaned up.
        PrefabUtility.RemoveUnusedOverrides(new[] { go }, InteractionMode.AutomatedAction);
        Undo.RecordObject(go, "Rebuild tractor");
        go.name = "Tractor";

        var vehicle = go.GetComponent<Vehicle>();
        if (vehicle == null) vehicle = Undo.AddComponent<Vehicle>(go);
        else Undo.RecordObject(vehicle, "Rebuild tractor");
        vehicle.kind = VehicleKind.Tractor;
        vehicle.displayName = "Tractor";
        vehicle.maxSteerAngle = steerAngle;
        vehicle.paintRenderers.Clear();
        var model = go.transform.Find("Model");
        if (model != null)
            foreach (var r in model.GetComponentsInChildren<Renderer>())
                if (r.name.StartsWith(PaintPrefix)) vehicle.paintRenderers.Add(r);
        PrefabUtility.RecordPrefabInstancePropertyModifications(vehicle);
        EditorUtility.SetDirty(vehicle);

        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeGameObject = go;
        if (model == null) Debug.LogWarning("Tractor rebuilt, but the scene did not show the new model yet. Run the tool once more.", go);
        else Debug.Log($"Tractor rebuilt ({vehicle.paintRenderers.Count} paint parts, wheel angle {steerAngle}). Save the scene (Ctrl+S).", go);
    }

    // Forward is +Z. Ground is local y = 0. The controller ball is centred at z -1.74, so the model is centred there too.
    static void BuildModel(Transform root)
    {
        var paint = Mat("TractorPaint", new Color(0.78f, 0.1f, 0.08f), 0.45f);
        var dark = Mat("TractorDark", new Color(0.13f, 0.13f, 0.14f), 0.2f);
        var rubber = Mat("TractorRubber", new Color(0.07f, 0.07f, 0.07f), 0.05f);
        var hub = Mat("TractorHub", new Color(0.92f, 0.78f, 0.2f), 0.4f);
        var glass = Mat("TractorGlass", new Color(0.2f, 0.32f, 0.42f), 0.9f);
        var light = Mat("TractorLight", new Color(1f, 0.96f, 0.8f), 0.8f);

        var model = new GameObject("Model").transform;
        model.SetParent(root, false);

        // engine and frame
        Part(model, PrimitiveType.Cube, PaintPrefix + "Hood", new Vector3(0, 1.55f, -0.35f), new Vector3(1.0f, 0.8f, 2.3f), paint);
        Part(model, PrimitiveType.Cube, "Grille", new Vector3(0, 1.5f, 0.82f), new Vector3(0.8f, 0.65f, 0.06f), dark);
        Part(model, PrimitiveType.Cube, "Light L", new Vector3(-0.33f, 1.82f, 0.84f), new Vector3(0.18f, 0.12f, 0.05f), light);
        Part(model, PrimitiveType.Cube, "Light R", new Vector3(0.33f, 1.82f, 0.84f), new Vector3(0.18f, 0.12f, 0.05f), light);
        Part(model, PrimitiveType.Cube, "Frame", new Vector3(0, 1.0f, -1.3f), new Vector3(0.85f, 0.6f, 3.6f), dark);
        Part(model, PrimitiveType.Cube, "Front Weights", new Vector3(0, 0.95f, 1.05f), new Vector3(0.9f, 0.45f, 0.35f), dark);
        Part(model, PrimitiveType.Cube, "Front Axle", new Vector3(0, 0.55f, 0.1f), new Vector3(1.6f, 0.16f, 0.18f), dark);
        Part(model, PrimitiveType.Cylinder, "Exhaust", new Vector3(0.32f, 2.35f, 0.35f), new Vector3(0.1f, 0.45f, 0.1f), dark);
        Part(model, PrimitiveType.Cube, "Hitch", new Vector3(0, 0.8f, -4.05f), new Vector3(0.3f, 0.25f, 0.4f), dark);

        // cab over the back wheels
        Part(model, PrimitiveType.Cube, PaintPrefix + "Cab Base", new Vector3(0, 1.55f, -2.85f), new Vector3(1.5f, 0.55f, 1.6f), paint);
        Part(model, PrimitiveType.Cube, PaintPrefix + "Fender L", new Vector3(-1.15f, 2.0f, -2.9f), new Vector3(0.65f, 0.12f, 2.0f), paint);
        Part(model, PrimitiveType.Cube, PaintPrefix + "Fender R", new Vector3(1.15f, 2.0f, -2.9f), new Vector3(0.65f, 0.12f, 2.0f), paint);
        Part(model, PrimitiveType.Cube, "Cab Glass", new Vector3(0, 2.7f, -2.85f), new Vector3(1.4f, 1.5f, 1.4f), glass);
        foreach (float x in new[] { -0.72f, 0.72f })
            foreach (float z in new[] { -2.17f, -3.53f })
                Part(model, PrimitiveType.Cylinder, "Post", new Vector3(x, 2.7f, z), new Vector3(0.09f, 0.85f, 0.09f), dark);
        Part(model, PrimitiveType.Cube, PaintPrefix + "Roof", new Vector3(0, 3.6f, -2.85f), new Vector3(1.75f, 0.12f, 1.85f), paint);

        // wheels: FlTire / FrTire are found by Vehicle and turned with the steering
        Wheel(model, "FlTire", new Vector3(-0.95f, 0.55f, 0.1f), 1.1f, 0.4f, rubber, hub, 0.5f);
        Wheel(model, "FrTire", new Vector3(0.95f, 0.55f, 0.1f), 1.1f, 0.4f, rubber, hub, 0.5f);
        Wheel(model, "RlTire", new Vector3(-1.15f, 0.95f, -2.9f), 1.9f, 0.6f, rubber, hub, 0.9f);
        Wheel(model, "RrTire", new Vector3(1.15f, 0.95f, -2.9f), 1.9f, 0.6f, rubber, hub, 0.9f);

        // one box makes the parked tractor solid (the tractor's own controller ignores it)
        var box = model.gameObject.AddComponent<BoxCollider>();
        box.center = new Vector3(0, 1.8f, -1.6f);
        box.size = new Vector3(2.9f, 3.6f, 5.4f);
    }

    // a cylinder lying on its side (2 units tall along Y, turned 90 degrees so Y points along -X), with a hub cap outside
    static void Wheel(Transform parent, string name, Vector3 pos, float diameter, float width, Material tire, Material hub, float hubDiameter)
    {
        var t = Part(parent, PrimitiveType.Cylinder, name, pos, new Vector3(diameter, width / 2f, diameter), tire, Quaternion.Euler(0, 0, 90));
        // outward is -X on the left (local +Y after the turn) and +X on the right (local -Y)
        float side = pos.x < 0 ? 1f : -1f;
        var cap = Part(t, PrimitiveType.Cylinder, "Hub", Vector3.zero, Vector3.one, hub);
        cap.localPosition = new Vector3(0, side * (1f + 0.02f / (width / 2f)), 0);
        cap.localScale = new Vector3(hubDiameter / diameter, 0.02f / (width / 2f), hubDiameter / diameter);
    }

    static Transform Part(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat, Quaternion? rot = null)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot ?? Quaternion.identity;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go.transform;
    }

    static Material Mat(string name, Color color, float gloss)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets", "Tractor");
        mat = new Material(Shader.Find("Standard")) { color = color };
        mat.SetFloat("_Glossiness", gloss);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
