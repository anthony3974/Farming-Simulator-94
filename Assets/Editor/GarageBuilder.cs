using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

// Editor tool: builds the garage next to the farmhouse from primitives, in the place of the old "Garage EX".
// Use: open GameScene, then Tools > Garage Builder > Rebuild garage, check it, save the scene (Ctrl+S).
//
// A simple one-storey farm garage, about half the size of the old one (16 x 13 m instead of 34 x 28 m,
// 8.5 m to the top of the roof instead of 19 m), in the farmhouse's style: peach walls, an orange hip roof,
// white trim, a red-orange roller door with a few lines, a small side door and one side window.
// - The big door faces the same way the old garage was open (-X, toward the SubRoad), and its front sits
//   where the old front was, so the way in from the road stays the same. A concrete apron runs from the
//   door to the road.
// - The door is 6.4 m wide and 5 m high, with the roller door rolled up to 4 m. The vehicles' controllers
//   are balls about 5.2 m across (tractor and van), so the door has no collider over it; only the walls
//   (and the floor slab) have colliders, so a vehicle can drive in but not through a wall.
//
// What it does:
// - Finds the old garage (the Garage EX.prefab instance under HomeArea) and turns it off (not deleted),
//   so you can compare and delete it by hand later. Its plain colliders go away with it.
// - Builds a new "Garage" object next to it in the hierarchy, with the model in a "Model" child.
// - Running it again keeps the "Garage" object (and wherever you moved it) and only rebuilds its "Model"
//   child, keeping the Model's position, rotation and scale if you changed them. Delete "Garage" first
//   to place it again from the old garage.
// - Scripts or trigger zones on the old garage are copied to the new one (the old Garage EX has none today).
// Materials are the farmhouse's (Assets/House) where they exist; missing ones and the roof mesh are saved in Assets/Garage.
public static class GarageBuilder
{
    const string RootName = "Garage";
    const string ModelName = "Model";
    const string Folder = "Assets/Garage";
    const string HouseFolder = "Assets/House";
    const string RoofMeshPath = Folder + "/GarageRoofMesh.asset";
    const string OldPrefabGuid = "05b0baa3323f8b54a8bcb8dea46c3d77"; // Assets/Prefabs/Garage EX.prefab
    const string OldPrefabPath = "Assets/Prefabs/Garage EX.prefab";
    const string OldName = "Garage EX";
    const string UndoName = "Rebuild garage";

    // the open side of the old garage, in the old prefab's own space: its back wall is at -X, so +X is open
    static readonly Vector3 OldOpenSide = Vector3.right;

    // ---- layout, in metres, in the garage's own space: +Z is the front (the big door), y 0 the ground
    const float Floor = 0.03f;                      // top of the floor slab
    const float BodyX0 = -8f, BodyX1 = 8f;          // 16 m wide along the front
    const float BodyZ0 = -6.5f, BodyZ1 = 6.5f;      // 13 m deep
    const float Wall = 0.3f;                        // wall thickness
    const float WallH = 5.6f;
    const float RoofRise = 2.8f, RoofOverhang = 0.5f, Fascia = 0.18f;
    const float DoorW = 6.4f, DoorH = 5f;           // big door opening
    const float RollerBottom = 4f;                  // the roller door is rolled up to here (the tractor is about 3.7 m tall)
    const float SideDoorZ = 3.5f;                   // small side door on the +X side, near the front

    [MenuItem("Tools/Garage Builder/Rebuild garage")]
    static void Rebuild()
    {
        if (EditorApplication.isPlaying) { Fail("Stop Play mode first. Changes made in Play mode are not saved."); return; }
        Terrain terrain = Terrain.activeTerrain;

        var old = FindOldGarage(out int oldCount);
        var root = FindBuild();
        if (old == null && root == null)
        {
            Fail($"Could not find the old garage in the open scene (an instance of {OldPrefabPath}, or an object named '{OldName}' " +
                 "with a GarageBackWall child), and there is no 'Garage' from an earlier run. Open GameScene first.");
            return;
        }

        Undo.SetCurrentGroupName(UndoName);
        int undoGroup = Undo.GetCurrentGroup();
        var log = new System.Text.StringBuilder();

        // 1. the root: reuse the one from an earlier run (keeps where it is), or place a new one from the old garage
        if (root == null)
        {
            if (!OldFootprint(old, out Vector3 frontCentre, out Vector3 front, out Vector3 size))
            { Fail($"The old '{OldName}' has no meshes to measure."); return; }

            // the roof's front edge on the old front line, centred from side to side on the old garage
            var rot = Quaternion.LookRotation(front, Vector3.up);
            Vector3 pos = frontCentre - front * (BodyZ1 + RoofOverhang);

            root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, UndoName);
            var parent = old.transform.parent;
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
                root.transform.SetSiblingIndex(old.transform.GetSiblingIndex() + 1);
            }
            root.transform.SetPositionAndRotation(new Vector3(pos.x, 0f, pos.z), rot);

            float ground = 0f;
            if (terrain != null)
            {
                Ground(terrain, root.transform, Vector3.zero, BodyX1 - BodyX0, BodyZ1 - BodyZ0, out float low, out ground);
                if (ground - low > 0.25f)
                    log.Append($"\nWarning: the ground under the garage rises {ground - low:0.00} m; the floor sits on the highest point, check the doorway step.");
            }
            else log.Append("\nNo terrain found, so the garage stands at height 0.");
            root.transform.position = new Vector3(pos.x, ground, pos.z);

            log.Append($"\nOld garage ({old.name}{(oldCount > 1 ? $", picked 1 of {oldCount} instances" : "")}): front centre X {frontCentre.x:0.0}, Z {frontCentre.z:0.0}, " +
                       $"size {size.x:0.0} wide x {size.z:0.0} deep x {size.y:0.0} m high, open toward {AxisName(front)}.");
            log.Append($"\nNew garage: {BodyX1 - BodyX0:0} wide x {BodyZ1 - BodyZ0:0} deep x {WallH + 0.05f + RoofRise:0.0} m high, door toward {AxisName(front)}, " +
                       "front on the old front line.");
            log.Append("\n" + CarryOver(old, root.transform));
        }
        else
        {
            log.Append($"\nKept the existing '{RootName}' (position and facing as it is); rebuilt its model.");
        }

        // 2. the old garage: turned off, not deleted
        if (old != null && old.activeSelf)
        {
            Undo.RecordObject(old, UndoName);
            old.SetActive(false);
            if (PrefabUtility.IsPartOfPrefabInstance(old)) PrefabUtility.RecordPrefabInstancePropertyModifications(old);
            log.Append($"\nThe old '{old.name}' is turned off (not deleted). Delete it by hand when you are happy with the new garage.");
        }

        // 3. the model: delete the one from the last run (keeping its position, rotation and scale), build a new one
        Vector3 modelPos = Vector3.zero, modelScale = Vector3.one;
        Quaternion modelRot = Quaternion.identity;
        var oldModel = root.transform.Find(ModelName);
        if (oldModel != null)
        {
            modelPos = oldModel.localPosition; modelRot = oldModel.localRotation; modelScale = oldModel.localScale;
            Undo.DestroyObjectImmediate(oldModel.gameObject);
        }
        var model = new GameObject(ModelName).transform;
        Undo.RegisterCreatedObjectUndo(model.gameObject, UndoName);
        model.SetParent(root.transform, false);
        model.localPosition = modelPos;
        model.localRotation = modelRot;
        model.localScale = modelScale;
        if (modelPos != Vector3.zero || modelRot != Quaternion.identity || modelScale != Vector3.one)
            log.Append($"\nKept the Model's own offset {modelPos}, turn {modelRot.eulerAngles.y:0} deg and scale {modelScale}.");

        var m = new Mats();
        BuildModel(model, m);
        log.Append("\n" + Apron(model, terrain, m));
        log.Append("\n" + m.Report());
        SetStatic(model.gameObject);

        int parts = model.GetComponentsInChildren<MeshRenderer>(true).Length;
        int colliders = model.GetComponentsInChildren<Collider>(true).Length;
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Vector3 p = root.transform.position;
        Debug.Log($"Garage rebuilt at X {p.x:0.0}, Y {p.y:0.00}, Z {p.z:0.0} ({parts} parts, {colliders} colliders). Save the scene (Ctrl+S)." + log, root);
    }

    static void Fail(string message)
    {
        Debug.LogError("Garage Builder: " + message);
        EditorUtility.DisplayDialog("Garage Builder", message, "OK");
    }

    // ---------------------------------------------------------------- finding things

    // The old garage: an instance of Garage EX.prefab (or, if it was unpacked, an object named Garage EX with a
    // GarageBackWall child). The scene has a second, unrelated Garage EX inside the big "Garage " box elsewhere
    // on the map, so the one under HomeArea wins, then the one closest to the farmhouse.
    static GameObject FindOldGarage(out int count)
    {
        string path = AssetDatabase.GUIDToAssetPath(OldPrefabGuid);
        if (string.IsNullOrEmpty(path)) path = OldPrefabPath;
        var found = new List<GameObject>();
        Transform house = null;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var go = t.gameObject;
            if (t.name == "Farmhouse" && house == null) house = t;
            if (PrefabUtility.IsAnyPrefabInstanceRoot(go) && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go) == path) found.Add(go);
            else if (go.name == OldName && t.Find("GarageBackWall") != null && !PrefabUtility.IsPartOfPrefabInstance(go)) found.Add(go);
        }
        count = found.Count;
        if (found.Count == 0) return null;

        foreach (var go in found)
            if (go.transform.parent != null && go.transform.parent.name == "HomeArea") return go;
        if (house == null) return found[0];
        GameObject best = null;
        float bestDist = float.MaxValue;
        foreach (var go in found)
        {
            Vector3 c = MeshBox(go, out Bounds b) ? b.center : go.transform.position;
            float d = Vector3.Distance(c, house.position);
            if (d < bestDist) { bestDist = d; best = go; }
        }
        return best;
    }

    // the garage built by an earlier run (the other "Garage " in the scene has a trailing space and is a plain box)
    static GameObject FindBuild()
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == RootName && !PrefabUtility.IsPartOfPrefabInstance(t.gameObject) && t.GetComponent<MeshFilter>() == null) return t.gameObject;
        return null;
    }

    // The old garage's box, lined up with its open side: the centre of its front edge on the ground plane,
    // the direction it opens toward, and its size (x across the front, y high, z deep).
    // Works on a turned-off garage too (mesh bounds, not renderer bounds).
    static bool OldFootprint(GameObject old, out Vector3 frontCentre, out Vector3 front, out Vector3 size)
    {
        frontCentre = default; size = default;
        front = old.transform.rotation * OldOpenSide;
        front.y = 0f;
        front = front.sqrMagnitude > 0.01f ? front.normalized : Vector3.left;

        var inv = Quaternion.Inverse(Quaternion.LookRotation(front, Vector3.up));
        Vector3 origin = old.transform.position;
        var min = Vector3.positiveInfinity; var max = Vector3.negativeInfinity;
        bool any = false;
        foreach (var mf in old.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            var b = mf.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var l = inv * (mf.transform.TransformPoint(corner) - origin);
                min = Vector3.Min(min, l); max = Vector3.Max(max, l);
                any = true;
            }
        }
        if (!any) return false;
        frontCentre = origin + Quaternion.LookRotation(front, Vector3.up) * new Vector3((min.x + max.x) / 2f, 0f, max.z);
        size = max - min;
        return true;
    }

    // the world box around all meshes under an object (works when it is turned off)
    static bool MeshBox(GameObject go, out Bounds box)
    {
        bool any = false;
        box = new Bounds();
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            var b = mf.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var w = mf.transform.TransformPoint(corner);
                if (!any) { box = new Bounds(w, Vector3.zero); any = true; }
                else box.Encapsulate(w);
            }
        }
        return any;
    }

    static string AxisName(Vector3 d)
    {
        if (Mathf.Abs(d.x) > Mathf.Abs(d.z)) return d.x > 0 ? "+X" : "-X";
        return d.z > 0 ? "+Z" : "-Z";
    }

    // the lowest and highest ground (world heights) under a rectangle in a frame's space
    static void Ground(Terrain terrain, Transform frame, Vector3 localCentre, float sizeX, float sizeZ, out float low, out float high)
    {
        low = float.MaxValue; high = float.MinValue;
        for (int i = 0; i <= 4; i++)
            for (int k = 0; k <= 4; k++)
            {
                var w = frame.TransformPoint(localCentre + new Vector3((i / 4f - 0.5f) * sizeX, 0f, (k / 4f - 0.5f) * sizeZ));
                float h = terrain.SampleHeight(w) + terrain.transform.position.y;
                low = Mathf.Min(low, h); high = Mathf.Max(high, h);
            }
    }

    // Scripts and trigger zones on the old garage are copied onto empty objects under the new root, at the same
    // place in the world. The old Garage EX has neither (only walls with plain colliders), so this normally copies nothing.
    static string CarryOver(GameObject old, Transform root)
    {
        if (old == null) return "No old garage to copy scripts or triggers from.";
        var copied = new List<string>();
        foreach (var t in old.GetComponentsInChildren<Transform>(true))
        {
            var keep = new List<Component>();
            foreach (var c in t.GetComponents<Component>())
            {
                if (c == null) continue; // missing script
                if (c is MonoBehaviour || (c is Collider col && col.isTrigger) || (c is Rigidbody && t.GetComponent<Collider>() is Collider rc && rc.isTrigger))
                    keep.Add(c);
            }
            if (keep.Count == 0) continue;

            var holder = new GameObject(t.name);
            Undo.RegisterCreatedObjectUndo(holder, UndoName);
            holder.transform.SetParent(root, false);
            holder.transform.SetPositionAndRotation(t.position, t.rotation);
            holder.transform.localScale = t.lossyScale; // the root has no scale
            holder.layer = t.gameObject.layer;
            holder.tag = t.gameObject.tag;
            foreach (var c in keep)
            {
                ComponentUtility.CopyComponent(c);
                ComponentUtility.PasteComponentAsNew(holder);
                copied.Add($"{c.GetType().Name} ({t.name})");
            }
        }
        return copied.Count == 0
            ? "The old garage had no scripts or trigger zones (only plain colliders), so there was nothing to carry over."
            : $"Copied from the old garage: {string.Join(", ", copied)}. Check any references in them that pointed into the old garage.";
    }

    // ---------------------------------------------------------------- the garage

    static void BuildModel(Transform model, Mats m)
    {
        float cx = (BodyX0 + BodyX1) / 2f, w = BodyX1 - BodyX0, d = BodyZ1 - BodyZ0;
        float inX0 = BodyX0 + Wall, inX1 = BodyX1 - Wall, inZ0 = BodyZ0 + Wall, inZ1 = BodyZ1 - Wall;
        const float sink = 0.5f; // walls go this far into the ground, so slopes show no gaps
        float wallCy = (WallH - sink) / 2f, wallH = WallH + sink;

        // ---- shell: floor slab and walls (with colliders), the front split by the big door; header without a collider,
        // so the vehicles' round controllers (about 5.2 m high) pass under it
        var b = Group(model, "Building", Vector3.zero);
        Box(b, "Floor", new Vector3(cx, (Floor - 0.4f) / 2f, (inZ0 + BodyZ1 - 0.02f) / 2f), new Vector3(inX1 - inX0, Floor + 0.4f, BodyZ1 - 0.02f - inZ0), m.floor, collider: true);
        Box(b, "Back Wall", new Vector3(cx, wallCy, BodyZ0 + Wall / 2f), new Vector3(w, wallH, Wall), m.wall, collider: true);
        foreach (float s in new[] { -1f, 1f })
        {
            Box(b, "Side Wall", new Vector3(s * (BodyX1 - Wall / 2f), wallCy, (inZ0 + BodyZ1) / 2f), new Vector3(Wall, wallH, BodyZ1 - inZ0), m.wall, collider: true);
            float x0 = DoorW / 2f, x1 = inX1;
            Box(b, "Front Wall", new Vector3(s * (x0 + x1) / 2f, wallCy, inZ1 + Wall / 2f), new Vector3(x1 - x0, wallH, Wall), m.wall, collider: true);
        }
        Box(b, "Door Header", new Vector3(0f, (DoorH + WallH) / 2f, inZ1 + Wall / 2f), new Vector3(DoorW, WallH - DoorH, Wall), m.wall);
        foreach (float x in new[] { BodyX0, BodyX1 })
            foreach (float z in new[] { BodyZ0, BodyZ1 })
                Box(b, "Corner Board", new Vector3(x, (WallH - 0.01f - 0.05f) / 2f, z), new Vector3(0.22f, WallH - 0.01f + 0.05f, 0.22f), m.trim);

        // ---- orange hip roof (one mesh with a white fascia and soffit), 5 cm above the wall top, gutters front and back
        float roofY = WallH + 0.05f;
        var roof = new GameObject("Roof", typeof(MeshFilter), typeof(MeshRenderer));
        roof.transform.SetParent(b, false);
        roof.transform.localPosition = new Vector3(cx, roofY, (BodyZ0 + BodyZ1) / 2f);
        roof.GetComponent<MeshFilter>().sharedMesh = HipRoofMesh(w, d, RoofRise, RoofOverhang, Fascia);
        roof.GetComponent<MeshRenderer>().sharedMaterials = new[] { m.roof, m.trim };
        float eaveY = roofY - RoofOverhang * RoofRise / (d / 2f) - Fascia; // bottom of the fascia
        foreach (float s in new[] { -1f, 1f })
            Box(b, "Gutter", new Vector3(cx, eaveY + 0.07f, (BodyZ0 + BodyZ1) / 2f + s * (d / 2f + RoofOverhang + 0.06f)),
                new Vector3(w + 2f * RoofOverhang - 0.1f, 0.14f, 0.16f), m.trim);

        // ---- the big door (front, +Z): white frame on the wall face, red-orange roller door rolled up to RollerBottom
        var door = Group(model, "Garage Door", new Vector3(0f, 0f, BodyZ1));
        foreach (float s in new[] { -1f, 1f })
            Box(door, "Door Jamb", new Vector3(s * (DoorW / 2f + 0.125f), (DoorH - 0.05f) / 2f, 0.03f), new Vector3(0.25f, DoorH + 0.05f, 0.08f), m.trim);
        Box(door, "Door Top Trim", new Vector3(0f, DoorH + 0.11f, 0.03f), new Vector3(DoorW + 0.5f, 0.22f, 0.08f), m.trim);
        float rollerZ = -Wall / 2f;  // in the middle of the wall
        Box(door, "Roller Door", new Vector3(0f, (RollerBottom + DoorH + 0.02f) / 2f, rollerZ), new Vector3(DoorW + 0.04f, DoorH + 0.02f - RollerBottom, 0.08f), m.door);
        for (int i = 1; i <= 3; i++)
        {
            float y = RollerBottom + i * (DoorH - RollerBottom) / 4f;
            Box(door, "Door Line", new Vector3(0f, y, rollerZ + 0.055f), new Vector3(DoorW, 0.04f, 0.03f), m.doorLine);
        }
        Box(door, "Door Bottom Bar", new Vector3(0f, RollerBottom + 0.03f, rollerZ), new Vector3(DoorW, 0.1f, 0.12f), m.dark);

        // ---- small side door with a light over it on the +X side (toward the farmhouse), near the front
        var side = Group(model, "Side Door", new Vector3(BodyX1, Floor, SideDoorZ), 90f);
        const float sdW = 1f, sdH = 2.2f;
        Box(side, "Door Frame", new Vector3(0f, (sdH + 0.15f) / 2f + 0.005f, 0.03f), new Vector3(sdW + 0.3f, sdH + 0.15f, 0.1f), m.trim);
        Box(side, "Door", new Vector3(0f, sdH / 2f + 0.01f, 0.05f), new Vector3(sdW, sdH - 0.01f, 0.1f), m.door);
        Box(side, "Knob", new Vector3(sdW * 0.36f, sdH * 0.45f, 0.13f), new Vector3(0.07f, 0.07f, 0.07f), m.lamp);
        Box(side, "Light", new Vector3(0f, sdH + 0.45f, 0.11f), new Vector3(0.16f, 0.24f, 0.22f), m.lamp);
        Box(side, "Light Cap", new Vector3(0f, sdH + 0.6f, 0.12f), new Vector3(0.24f, 0.07f, 0.26f), m.dark);

        // ---- one window on the -X side (toward the parking), like the farmhouse's: red-orange frame, blue glass, white sill
        var win = Group(model, "Side Window", new Vector3(BodyX0, Floor + 2.4f, -0.5f), -90f);
        const float ww = 1.8f, wh = 1.1f, f = 0.12f;
        Box(win, "Frame", new Vector3(0f, 0f, 0.03f), new Vector3(ww + 2f * f, wh + 2f * f, 0.1f), m.frame);
        Box(win, "Glass", new Vector3(0f, 0f, 0.05f), new Vector3(ww, wh, 0.1f), m.glass);
        Box(win, "Centre Bar", new Vector3(0f, 0f, 0.115f), new Vector3(0.08f, wh, 0.05f), m.frame);
        Box(win, "Sill", new Vector3(0f, -wh / 2f - f - 0.03f, 0.08f), new Vector3(ww + 2f * f + 0.2f, 0.08f, 0.2f), m.trim);
    }

    // A concrete apron from the big door straight to the road in front (the SubRoad), or a short one if no road is near.
    // Flat on the ground with no collider; its top is 2 cm over the highest ground under it.
    static string Apron(Transform model, Terrain terrain, Mats m)
    {
        float z0 = BodyZ1 - 0.02f, width = DoorW + 2f;
        Vector3 dir = model.forward; dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return "The garage is tipped over, so no apron was built.";
        dir.Normalize();
        float? road = RoadAhead(model.TransformPoint(new Vector3(0f, 0f, z0)), dir, 30f, RoadBounds());
        float scale = Mathf.Max(0.01f, model.lossyScale.z);
        float length = road.HasValue ? road.Value / scale - 0.05f : 4f;
        if (length < 0.5f) return "The road starts right at the door, so no apron was built.";

        var c = new Vector3(0f, 0f, z0 + length / 2f);
        float top = Floor - 0.01f;
        if (terrain != null)
        {
            top = float.MinValue;
            for (int i = 0; i <= 4; i++)
                for (int k = 0; k <= 4; k++)
                {
                    var wp = model.TransformPoint(c + new Vector3((i / 4f - 0.5f) * width, 0f, (k / 4f - 0.5f) * length));
                    wp.y = terrain.SampleHeight(wp) + terrain.transform.position.y;
                    top = Mathf.Max(top, model.InverseTransformPoint(wp).y);
                }
            top += 0.02f / Mathf.Max(0.01f, model.lossyScale.y);
        }
        Box(Group(model, "Apron", Vector3.zero), "Apron", new Vector3(0f, top - 0.2f, c.z), new Vector3(width, 0.4f, length), m.concrete);
        return road.HasValue
            ? $"Built a concrete apron from the door to the road ({width:0.0} x {length:0.0} m)."
            : "No road found within 30 m of the door, so the apron is only 4 m long.";
    }

    // the road strips (their boxes), to find where the apron meets a road
    static List<Bounds> RoadBounds()
    {
        var list = new List<Bounds>();
        var roads = GameObject.Find("Roads and paths");
        if (roads != null)
            foreach (var r in roads.GetComponentsInChildren<Renderer>()) list.Add(r.bounds);
        return list;
    }

    // how far along dir (from a start point) the first road begins, or null if none within max
    static float? RoadAhead(Vector3 start, Vector3 dir, float max, List<Bounds> roads)
    {
        for (float s = 0f; s <= max; s += 0.25f)
        {
            var p = start + dir * s;
            foreach (var b in roads)
                if (p.x >= b.min.x && p.x <= b.max.x && p.z >= b.min.z && p.z <= b.max.z) return s;
        }
        return null;
    }

    // ---------------------------------------------------------------- pieces

    // A hip roof like the farmhouse's: four slopes of the same pitch meeting at a short ridge along X, with a
    // vertical fascia around the eaves and a flat soffit under it. Origin = centre of the roof's base at the wall line.
    // Sub-mesh 0 is the roof, sub-mesh 1 the fascia and soffit. Saved in Assets/Garage and rebuilt in place each run.
    static Mesh HipRoofMesh(float length, float span, float rise, float overhang, float fascia)
    {
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(RoofMeshPath);
        bool isNew = mesh == null;
        if (isNew) mesh = new Mesh { name = "GarageRoofMesh" };
        else mesh.Clear();

        float ye = -overhang * rise / (span / 2f), yb = ye - fascia;             // eave edge, fascia bottom
        float ex = length / 2f + overhang, ez = span / 2f + overhang, r = Mathf.Max(0f, (length - span) / 2f);
        Vector3 fl = new(-ex, ye, ez), fr = new(ex, ye, ez), bl = new(-ex, ye, -ez), br = new(ex, ye, -ez);
        Vector3 rl = new(-r, rise, 0f), rr = new(r, rise, 0f), down = new(0f, yb - ye, 0f);
        var verts = new List<Vector3>();
        var roofTris = new List<int>();
        var trimTris = new List<int>();

        // a flat convex face with its own vertices (sharp edges); Unity shows the side Cross(b - a, c - a) points to
        void Face(List<int> tris, Vector3 outward, params Vector3[] p)
        {
            int v = verts.Count;
            verts.AddRange(p);
            for (int i = 1; i < p.Length - 1; i++)
            {
                if (Vector3.Dot(Vector3.Cross(p[i] - p[0], p[i + 1] - p[0]), outward) > 0f) tris.AddRange(new[] { v, v + i, v + i + 1 });
                else tris.AddRange(new[] { v, v + i + 1, v + i });
            }
        }

        Face(roofTris, new Vector3(0f, 1f, 1f), fl, fr, rr, rl);   // front slope
        Face(roofTris, new Vector3(0f, 1f, -1f), br, bl, rl, rr);  // back slope
        Face(roofTris, new Vector3(1f, 1f, 0f), fr, br, rr);       // hip ends
        Face(roofTris, new Vector3(-1f, 1f, 0f), bl, fl, rl);
        Face(trimTris, Vector3.forward, fl, fr, fr + down, fl + down);
        Face(trimTris, Vector3.back, br, bl, bl + down, br + down);
        Face(trimTris, Vector3.right, fr, br, br + down, fr + down);
        Face(trimTris, Vector3.left, bl, fl, fl + down, bl + down);
        Face(trimTris, Vector3.down, fl + down, fr + down, br + down, bl + down); // also the white ceiling inside

        var uvs = new List<Vector2>();
        foreach (var v in verts) uvs.Add(new Vector2(v.x, v.z));
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(roofTris, 0);
        mesh.SetTriangles(trimTris, 1);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        Unwrapping.GenerateSecondaryUVSet(mesh); // lightmap UVs, like the built-in cubes have

        if (isNew)
        {
            EnsureFolder();
            AssetDatabase.CreateAsset(mesh, RoofMeshPath);
        }
        else EditorUtility.SetDirty(mesh);
        return mesh;
    }

    static Transform Group(Transform parent, string name, Vector3 pos, float yaw = 0f)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = pos;
        t.localRotation = Quaternion.Euler(0f, yaw, 0f);
        return t;
    }

    // a cube; positions are local to the parent; only pieces asked for keep their collider
    static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, bool collider = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static void SetStatic(GameObject root)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            t.gameObject.isStatic = true;
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "Garage");
    }

    // The farmhouse's materials (Assets/House) are used as they are, so the garage matches the house.
    // If one is missing, a material of the same colour is made (once) in Assets/Garage.
    class Mats
    {
        int reused, made;
        public readonly Material wall, roof, trim, door, doorLine, frame, glass, floor, concrete, dark, lamp;

        public Mats()
        {
            wall = Get("HouseWall", "GarageWall", new Color(0.97f, 0.76f, 0.5f), 0.1f);          // peach walls
            roof = Get("HouseRoof", "GarageRoof", new Color(0.92f, 0.5f, 0.21f), 0.2f);          // orange roof
            trim = Get("HouseTrim", "GarageTrim", new Color(0.96f, 0.95f, 0.92f), 0.25f);        // white trim, fascia, gutters
            door = Get("HouseDoor", "GarageDoor", new Color(0.8f, 0.3f, 0.14f), 0.3f);           // red-orange doors
            doorLine = Get("HouseBrick", "GarageDoorLine", new Color(0.55f, 0.26f, 0.19f), 0.05f); // darker lines on the roller door
            frame = Get("HouseFrame", "GarageFrame", new Color(0.84f, 0.31f, 0.13f), 0.25f);     // window frame
            glass = Get("HouseGlass", "GarageGlass", new Color(0.33f, 0.62f, 0.88f), 0.85f);     // light blue glass
            floor = Get("HouseStone", "GarageFloor", new Color(0.56f, 0.55f, 0.52f), 0.05f);     // floor slab
            concrete = Get("HouseConcrete", "GarageConcrete", new Color(0.5f, 0.5f, 0.5f), 0.05f); // apron, like the house's driveway grey
            dark = Get("HouseDark", "GarageDark", new Color(0.2f, 0.2f, 0.22f), 0.3f);           // door bottom bar, light cap
            lamp = Get("HouseLamp", "GarageLamp", new Color(0.98f, 0.86f, 0.5f), 0.6f);          // light, knob
        }

        Material Get(string houseName, string ownName, Color color, float gloss)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{HouseFolder}/{houseName}.mat");
            if (mat != null) { reused++; return mat; }
            string path = $"{Folder}/{ownName}.mat";
            mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) { made++; return mat; }
            EnsureFolder();
            mat = new Material(Shader.Find("Standard")) { color = color };
            mat.SetFloat("_Glossiness", gloss);
            AssetDatabase.CreateAsset(mat, path);
            made++;
            return mat;
        }

        public string Report() => made == 0
            ? $"All {reused} materials are the farmhouse's (Assets/House); the roof mesh is in {Folder}."
            : $"{reused} materials are the farmhouse's (Assets/House), {made} are the garage's own in {Folder}.";
    }
}
