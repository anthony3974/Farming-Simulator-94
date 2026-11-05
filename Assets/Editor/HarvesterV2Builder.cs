using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Editor tool: builds a redesigned harvester model ("Model V2") inside Harvester.prefab, next to the old one.
// The old model is NOT removed: on the first run its 12 boxes (they sat straight under the Harvester) are
// gathered into a "Model" object and only turned off, and the buttons below switch between the two.
//
// The new model (see HarvesterV2Model.cs for the parts) is built like Tractor V2: every part overlaps the part
// it hangs on, so nothing floats. It steers with the back wheels like a real combine, so the tool also ticks
// Steer Rear Wheels on the harvester (and unticks it for the old model, which steers with the front).
//
// Use: open GameScene, Tools > Harvester Builder V2, press "Build new harvester", save the scene (Ctrl+S).
// Safe to run again: only "Model V2" is rebuilt. The controller, collider and scripts are kept.
public class HarvesterV2Builder : EditorWindow
{
    const string PrefabPath = "Assets/Prefabs/Harvester.prefab";
    const string OldModel = "Model";
    const string NewModel = HarvesterV2Model.ModelName;
    const string GrainPath = "Assets/Vehicles/HarvesterGrain.mat";

    [MenuItem("Tools/Harvester Builder V2")]
    static void Open() { GetWindow<HarvesterV2Builder>("Harvester V2"); }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("Builds the redesigned harvester (\"" + NewModel + "\") in Harvester.prefab and turns the old model (\"" +
            OldModel + "\") off. The old model stays in the prefab, switch back any time. Run it with GameScene open, then save the scene.",
            MessageType.Info);
        if (GUILayout.Button("Build new harvester")) Build();
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
            EditorUtility.DisplayDialog("Harvester V2", "No prefab at " + PrefabPath + ".", "OK");
            return;
        }
        var materials = LoadMaterials();
        if (materials == null) return;

        var contents = PrefabUtility.LoadPrefabContents(PrefabPath);
        GatherOldModel(contents.transform);
        var old = contents.transform.Find(NewModel);
        if (old != null) DestroyImmediate(old.gameObject);   // only ever the V2 model, never the old one
        HarvesterV2Model.Build(contents.transform, materials);
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
            EditorUtility.DisplayDialog("Harvester V2", "There is no new model yet. Press \"Build new harvester\" first.", "OK");
            return;
        }
        GatherOldModel(contents.transform);
        SetActiveModel(contents.transform, useNew);
        PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
        PrefabUtility.UnloadPrefabContents(contents);
        UpdateScene(useNew);
    }

    // The old harvester's boxes sit straight under the Harvester. Put them in a "Model" object (at the origin,
    // so nothing moves) so one switch turns them all off. Moving them keeps their ids, so overrides in the
    // scene (the cab roof and glass the owner moved) still apply to them.
    static void GatherOldModel(Transform root)
    {
        if (root.Find(OldModel) != null) return;
        var parts = new System.Collections.Generic.List<Transform>();
        foreach (Transform child in root)
            if (child.name != NewModel && child.GetComponent<Renderer>() != null) parts.Add(child);
        if (parts.Count == 0) return;

        var model = new GameObject(OldModel).transform;
        model.SetParent(root, false);
        foreach (var part in parts) part.SetParent(model, false);
    }

    // turns one model on and the other off, and sets up the Vehicle for the one in use
    static void SetActiveModel(Transform root, bool useNew)
    {
        var oldModel = root.Find(OldModel);
        var newModel = root.Find(NewModel);
        if (oldModel != null) oldModel.gameObject.SetActive(!useNew);
        if (newModel != null) newModel.gameObject.SetActive(useNew);

        var vehicle = root.GetComponent<Vehicle>();
        if (vehicle == null) return;
        // the new model steers with the back wheels (BlTire / BrTire), the old one with the front
        vehicle.steerRearWheels = useNew;
        // paint parts: the "Paint ..." parts of the new model, the red boxes of the old one
        vehicle.paintRenderers.Clear();
        var model = useNew ? newModel : oldModel;
        if (model != null)
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                if (useNew ? r.name.StartsWith(HarvesterV2Model.PaintPrefix)
                           : r.sharedMaterial != null && r.sharedMaterial.name == "HarvesterPaint")
                    vehicle.paintRenderers.Add(r);
    }

    // the harvester in the open scene: drop any override that would hide the switch (model on/off, steering, paint list)
    static void UpdateScene(bool useNew)
    {
        Vehicle vehicle = null;
        foreach (var v in FindObjectsByType<Vehicle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(v.gameObject) == PrefabPath) { vehicle = v; break; }
        if (vehicle == null)
        {
            Debug.Log("Harvester prefab updated. No Harvester in the open scene, so only the prefab changed.");
            return;
        }

        var go = vehicle.gameObject;
        foreach (var name in new[] { OldModel, NewModel })
        {
            var t = go.transform.Find(name);
            if (t != null && PrefabUtility.IsPartOfPrefabInstance(t.gameObject))
                PrefabUtility.RevertObjectOverride(t.gameObject, InteractionMode.AutomatedAction);
        }
        var so = new SerializedObject(vehicle);
        foreach (var field in new[] { "steerRearWheels", "paintRenderers" })
        {
            var p = so.FindProperty(field);
            if (p != null && p.prefabOverride) PrefabUtility.RevertPropertyOverride(p, InteractionMode.AutomatedAction);
        }

        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeGameObject = go;
        var model = go.transform.Find(useNew ? NewModel : OldModel);
        if (model == null) Debug.LogWarning("Prefab updated, but the scene did not show the model yet. Press the button once more.", go);
        else Debug.Log($"Harvester now uses \"{model.name}\" ({vehicle.paintRenderers.Count} paint parts, " +
            (vehicle.steerRearWheels ? "steers with the back wheels" : "steers with the front wheels") + "). Save the scene (Ctrl+S).", go);
    }

    // ---- materials ----
    // The harvester's own materials (Assets/Vehicles) plus the tractor's tire rubber and yellow hubs, so the
    // two match. Only the grain is new, made if missing.

    static HarvesterMaterials LoadMaterials()
    {
        var m = new HarvesterMaterials
        {
            paint = Load("Assets/Vehicles/HarvesterPaint.mat"),
            dark = Load("Assets/Vehicles/VehicleDark.mat"),
            metal = Load("Assets/Vehicles/VehicleMetal.mat"),
            glass = Load("Assets/Vehicles/VehicleGlass.mat"),
            light = Load("Assets/Vehicles/VehicleLight.mat"),
            amber = Load("Assets/Vehicles/VehicleAmber.mat"),
            tailRed = Load("Assets/Vehicles/VehicleTailRed.mat"),
            rubber = Load("Assets/Tractor/TractorRubber.mat"),
            hub = Load("Assets/Tractor/TractorHub.mat"),
            grain = Grain(),
        };
        if (m.paint && m.dark && m.metal && m.glass && m.light && m.amber && m.tailRed && m.rubber && m.hub && m.grain) return m;
        EditorUtility.DisplayDialog("Harvester V2", "Some materials are missing, see the Console. Nothing was changed.", "OK");
        return null;
    }

    static Material Load(string path)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) Debug.LogError("Harvester V2: missing material " + path);
        return mat;
    }

    static Material Grain()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(GrainPath);
        if (mat != null) return mat;
        mat = new Material(Shader.Find("Standard")) { color = new Color(0.86f, 0.70f, 0.32f) };
        mat.SetFloat("_Glossiness", 0.1f);
        AssetDatabase.CreateAsset(mat, GrainPath);
        return mat;
    }
}
