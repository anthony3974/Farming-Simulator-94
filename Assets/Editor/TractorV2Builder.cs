using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Editor tool: builds a redesigned tractor model ("Model V2") inside Tractor.prefab, next to the old "Model".
// The old model is NOT removed: it is only turned off, and the buttons below switch between the two.
//
// The new model is built so nothing floats: every part overlaps the part it hangs on (a few cm), and the
// chain always ends at the frame. Wheels hang on axles that run into the wheel centres, the hood and the cab
// sit on the frame, the fenders join the cab sides, the posts run from the cab base into the roof.
//
// Use: open GameScene, Tools > Tractor Builder V2, press "Build new tractor", save the scene (Ctrl+S).
// Safe to run again: only "Model V2" is rebuilt. The camera, controller and scripts are kept.
public class TractorV2Builder : EditorWindow
{
    const string PrefabPath = "Assets/Prefabs/Tractor.prefab";
    const string MaterialFolder = "Assets/Tractor";
    const string OldModel = "Model";
    const string NewModel = "Model V2";
    const string PaintPrefix = "Paint ";   // parts that take the vehicle colour (Vehicle.paintRenderers)

    [MenuItem("Tools/Tractor Builder V2")]
    static void Open() { GetWindow<TractorV2Builder>("Tractor V2"); }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("Builds the redesigned tractor (\"" + NewModel + "\") in Tractor.prefab and turns the old \"" +
            OldModel + "\" off. The old model stays in the prefab, switch back any time. Run it with GameScene open, then save the scene.",
            MessageType.Info);
        if (GUILayout.Button("Build new tractor")) Build();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Switch model", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Use new (V2)")) Use(true);
            if (GUILayout.Button("Use old")) Use(false);
        }
    }

    // ---- prefab editing ----

    static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
        {
            EditorUtility.DisplayDialog("Tractor V2", "No prefab at " + PrefabPath + ".", "OK");
            return;
        }
        var contents = PrefabUtility.LoadPrefabContents(PrefabPath);
        var old = contents.transform.Find(NewModel);
        if (old != null) DestroyImmediate(old.gameObject);   // only ever the V2 model, never the old one
        BuildModel(contents.transform);
        SetActiveModel(contents.transform, true);
        PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
        PrefabUtility.UnloadPrefabContents(contents);
        UpdateScene(true);
    }

    static void Use(bool useNew)
    {
        var contents = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (useNew && contents.transform.Find(NewModel) == null)
        {
            PrefabUtility.UnloadPrefabContents(contents);
            EditorUtility.DisplayDialog("Tractor V2", "There is no new model yet. Press \"Build new tractor\" first.", "OK");
            return;
        }
        SetActiveModel(contents.transform, useNew);
        PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
        PrefabUtility.UnloadPrefabContents(contents);
        UpdateScene(useNew);
    }

    static void SetActiveModel(Transform root, bool useNew)
    {
        var oldModel = root.Find(OldModel);
        var newModel = root.Find(NewModel);
        if (oldModel != null) oldModel.gameObject.SetActive(!useNew);
        if (newModel != null) newModel.gameObject.SetActive(useNew);
    }

    // the tractor in the open scene: drop any on/off override on the two models, point the paint list at the one in use
    static void UpdateScene(bool useNew)
    {
        Vehicle vehicle = null;
        foreach (var v in FindObjectsByType<Vehicle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(v.gameObject) == PrefabPath) { vehicle = v; break; }
        if (vehicle == null)
        {
            Debug.Log("Tractor prefab updated. No Tractor in the open scene, so only the prefab changed.");
            return;
        }

        var go = vehicle.gameObject;
        foreach (var name in new[] { OldModel, NewModel })
        {
            var t = go.transform.Find(name);
            if (t != null && PrefabUtility.IsPartOfPrefabInstance(t.gameObject))
                PrefabUtility.RevertObjectOverride(t.gameObject, InteractionMode.AutomatedAction);
        }

        Undo.RecordObject(vehicle, "Switch tractor model");
        vehicle.paintRenderers.Clear();
        var model = go.transform.Find(useNew ? NewModel : OldModel);
        if (model != null)
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                if (r.name.StartsWith(PaintPrefix)) vehicle.paintRenderers.Add(r);
        PrefabUtility.RecordPrefabInstancePropertyModifications(vehicle);
        EditorUtility.SetDirty(vehicle);

        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeGameObject = go;
        if (model == null) Debug.LogWarning("Prefab updated, but the scene did not show the model yet. Press the button once more.", go);
        else Debug.Log($"Tractor now uses \"{model.name}\" ({vehicle.paintRenderers.Count} paint parts). Save the scene (Ctrl+S).", go);
    }

    // ---- the model ----
    // Forward is +Z, ground is local y = 0, right is +X. The controller ball sits at z -1.74, the model spans
    // about z +1.3 (front weights) to -4.3 (hitch). Comments give each part's span to show what it overlaps.

    static void BuildModel(Transform root)
    {
        var paint = Mat("TractorPaint", new Color(0.78f, 0.1f, 0.08f), 0.45f);
        var dark = Mat("TractorDark", new Color(0.13f, 0.13f, 0.14f), 0.2f);
        var rubber = Mat("TractorRubber", new Color(0.07f, 0.07f, 0.07f), 0.05f);
        var hub = Mat("TractorHub", new Color(0.92f, 0.78f, 0.2f), 0.4f);
        var glass = Mat("TractorGlass", new Color(0.2f, 0.32f, 0.42f), 0.9f);
        var light = Mat("TractorLight", new Color(1f, 0.96f, 0.8f), 0.8f);

        var model = new GameObject(NewModel).transform;
        model.SetParent(root, false);

        // Frame: the spine everything else attaches to. y 0.7..1.3, z +1.0..-4.0, x ±0.45
        Box(model, "Frame", new Vector3(0, 1.0f, -1.5f), new Vector3(0.9f, 0.6f, 5.0f), dark);

        // Front: weights bolted to the frame nose (z +0.9..+1.3, y 0.675..1.225: into the frame and the hood)
        Box(model, "Front Weights", new Vector3(0, 0.95f, 1.1f), new Vector3(0.95f, 0.55f, 0.4f), dark);

        // Hood on the frame (y 1.21..1.98, into the frame top at 1.3), runs back into the cab base (z +0.95..-2.1).
        // Kept a little inside the cab base's top and bottom (1.2..2.0) so the faces inside it do not flicker.
        Box(model, PaintPrefix + "Hood", new Vector3(0, 1.595f, -0.575f), new Vector3(1.0f, 0.77f, 3.05f), paint);
        // grille set into the hood front (z 0.91..0.99, hood front is 0.95)
        Box(model, "Grille", new Vector3(0, 1.55f, 0.95f), new Vector3(0.8f, 0.6f, 0.08f), dark);
        // lights set into the hood front above the grille (z 0.92..1.0)
        Box(model, "Light L", new Vector3(-0.3f, 1.88f, 0.96f), new Vector3(0.2f, 0.12f, 0.08f), light);
        Box(model, "Light R", new Vector3(0.3f, 1.88f, 0.96f), new Vector3(0.2f, 0.12f, 0.08f), light);
        // exhaust: a collar on the hood (y 1.97..2.09, hood top 1.98) and the pipe standing in it (y 2.0..3.0)
        Box(model, "Exhaust Base", new Vector3(0.3f, 2.03f, 0.2f), new Vector3(0.2f, 0.12f, 0.2f), dark);
        Cyl(model, "Exhaust", new Vector3(0.3f, 2.5f, 0.2f), new Vector3(0.11f, 0.5f, 0.11f), dark);
        Cyl(model, "Exhaust Tip", new Vector3(0.3f, 3.0f, 0.2f), new Vector3(0.14f, 0.04f, 0.14f), dark);

        // Front axle: a mount from the frame (y 0.5..0.9, frame bottom 0.7) holds the beam (y 0.44..0.66),
        // which runs 0.1 into each front wheel (wheels from |x| 0.75), so it stays hidden when they steer
        Box(model, "Front Axle Mount", new Vector3(0, 0.7f, 0.2f), new Vector3(0.5f, 0.4f, 0.4f), dark);
        Box(model, "Front Axle", new Vector3(0, 0.55f, 0.2f), new Vector3(1.7f, 0.22f, 0.22f), dark);

        // Rear axle housing through the frame (y 0.8..1.1) and 0.2 into each rear wheel (wheels from |x| 0.8)
        Box(model, "Rear Axle", new Vector3(0, 0.95f, -2.9f), new Vector3(2.0f, 0.3f, 0.3f), dark);

        // Cab base on the frame (y 1.2..2.0, frame top 1.3), z -2.0..-3.8, x ±0.75; the hood ends inside it
        Box(model, PaintPrefix + "Cab Base", new Vector3(0, 1.6f, -2.9f), new Vector3(1.5f, 0.8f, 1.8f), paint);

        // Fenders over the rear wheels (wheel top 1.9): the top plate (y 1.94..2.06, x 0.7..1.5) overlaps the
        // cab base side (x 0.75, top 2.0); the front and back skirts hang from the plate ends (y 1.4..2.0)
        foreach (float side in new[] { -1f, 1f })
        {
            string s = side < 0 ? " L" : " R";
            Box(model, PaintPrefix + "Fender" + s, new Vector3(side * 1.1f, 2.0f, -2.9f), new Vector3(0.8f, 0.12f, 2.1f), paint);
            Box(model, PaintPrefix + "Fender Front" + s, new Vector3(side * 1.1f, 1.72f, -1.89f), new Vector3(0.8f, 0.56f, 0.08f), paint);
            Box(model, PaintPrefix + "Fender Back" + s, new Vector3(side * 1.1f, 1.72f, -3.91f), new Vector3(0.8f, 0.56f, 0.08f), paint);
        }

        // Cab glass on the cab base (y 1.96..3.40), z -2.15..-3.65, x ±0.7
        Box(model, "Cab Glass", new Vector3(0, 2.68f, -2.9f), new Vector3(1.4f, 1.44f, 1.5f), glass);
        // corner posts on the glass edges, from inside the cab base (1.95) into the roof (3.45)
        foreach (float x in new[] { -0.7f, 0.7f })
            foreach (float z in new[] { -2.15f, -3.65f })
                Box(model, "Post", new Vector3(x, 2.7f, z), new Vector3(0.12f, 1.5f, 0.12f), dark);
        // roof on the posts and glass (y 3.39..3.51), a little overhang
        Box(model, PaintPrefix + "Roof", new Vector3(0, 3.45f, -2.9f), new Vector3(1.6f, 0.12f, 1.75f), paint);
        // beacon sitting in the roof (y 3.495..3.645)
        Box(model, "Beacon", new Vector3(0.5f, 3.57f, -3.45f), new Vector3(0.15f, 0.15f, 0.15f), hub);

        // Hitch out of the frame back (z -3.9..-4.3, frame ends at -4.0)
        Box(model, "Hitch", new Vector3(0, 0.8f, -4.1f), new Vector3(0.3f, 0.25f, 0.4f), dark);

        // Wheels: on the ground (centre y = radius), on the axle ends. FlTire / FrTire are found by Vehicle and steer.
        Wheel(model, "FlTire", new Vector3(-0.95f, 0.55f, 0.2f), 1.1f, 0.4f, rubber, hub, 0.5f);
        Wheel(model, "FrTire", new Vector3(0.95f, 0.55f, 0.2f), 1.1f, 0.4f, rubber, hub, 0.5f);
        Wheel(model, "RlTire", new Vector3(-1.1f, 0.95f, -2.9f), 1.9f, 0.6f, rubber, hub, 0.9f);
        Wheel(model, "RrTire", new Vector3(1.1f, 0.95f, -2.9f), 1.9f, 0.6f, rubber, hub, 0.9f);

        // one box makes the parked tractor solid (the tractor's own controller ignores it)
        var box = model.gameObject.AddComponent<BoxCollider>();
        box.center = new Vector3(0, 1.8f, -1.5f);
        box.size = new Vector3(2.8f, 3.6f, 5.6f);
    }

    // A cylinder on its side (turned 90 degrees so its local Y points along -X) with a hub cap pressed into the
    // outer face: the cap reaches 0.01 into the tire and sticks out 0.03.
    static void Wheel(Transform parent, string name, Vector3 pos, float diameter, float width, Material tire, Material hub, float hubDiameter)
    {
        var t = Part(parent, PrimitiveType.Cylinder, name, pos, new Vector3(diameter, width / 2f, diameter), tire, Quaternion.Euler(0, 0, 90));
        float side = pos.x < 0 ? 1f : -1f;   // outward: local +Y on the left, local -Y on the right
        float half = width / 2f;              // local units per metre along the axle = 1 / half
        var cap = Part(t, PrimitiveType.Cylinder, "Hub", Vector3.zero, Vector3.one, hub);
        cap.localPosition = new Vector3(0, side * (1f + 0.01f / half), 0);
        cap.localScale = new Vector3(hubDiameter / diameter, 0.02f / half, hubDiameter / diameter);
    }

    static Transform Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat) =>
        Part(parent, PrimitiveType.Cube, name, pos, size, mat);

    // a standing cylinder: scale.y is half its height
    static Transform Cyl(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat) =>
        Part(parent, PrimitiveType.Cylinder, name, pos, scale, mat);

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

    // the same materials as the old tractor (Assets/Tractor), made only if missing
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
