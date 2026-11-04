using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

// Editor tool: builds the player's farmhouse from primitives, in the place of the old House prefab.
// Use: open GameScene, then Tools > House Builder > Rebuild house, check it, save the scene (Ctrl+S).
//
// The look follows the house in the middle of the 2D map (backround.png): one tall storey, peach walls,
// an orange hip roof, a red-orange double front door between two big blue windows, and the garage on its
// right. (The 2D stepping stones and driveway are left out; the owner removed them.)
// 2D up is +X and 2D right is -Z (see DealerBuilder in git history), so in 3D:
// - the front faces -X, the SubRoad (the road under the house in 2D);
// - the 2D garage is on the -Z side and has its own tool (Tools > Garage Builder), so no garage is built here;
// - the chimney end faces +Z, the main Road; the back faces +X, the Tony fields.
//
// What it does:
// - Finds the old house (the House.prefab instance) and turns it off (not deleted), so you can compare
//   and delete it by hand later. Its plain colliders go away with it.
// - Builds a new "Farmhouse" object next to it in the hierarchy, inside the old house's footprint.
// - Running it again keeps the "Farmhouse" object (and wherever you moved it) and only rebuilds its
//   "Model" child, so it never stacks copies. Delete "Farmhouse" first to place it again from the old house.
// - Scripts or trigger zones on the old house are copied to the new one (the old House has none today).
// Materials and the roof mesh are saved in Assets/House. Placeholder primitives, a real model can replace them later.
public static class HouseBuilder
{
    const string RootName = "Farmhouse";
    const string ModelName = "Model";
    const string MaterialFolder = "Assets/House";
    const string RoofMeshPath = MaterialFolder + "/HouseRoofMesh.asset";
    const string OldPrefabGuid = "fa68d031b13a3024386c5f3836f03e86"; // Assets/Prefabs/House.prefab
    const string OldPrefabPath = "Assets/Prefabs/House.prefab";
    const string UndoName = "Rebuild house";

    // which side of the old house becomes the front, in the old house's own space: -X = the SubRoad (2D: the road below the house)
    static readonly Vector3 FrontSide = Vector3.left;

    // The owner found the first build too small (the tractor was bigger than the house) and scaled the Model
    // to 1.7923 and nudged it by hand (2026-11-03). A first build now starts at that size and offset.
    // All the layout numbers below are before this scale (so the real house is about 25 x 18 m, 14 m to the ridge).
    const float DefaultModelScale = 1.7923f;
    static readonly Vector3 DefaultModelOffset = new Vector3(7.45f, 0.08f, 1.73f);

    // ---- layout, in metres, in the new house's own space: +Z is the front, +X the chimney end, y 0 the ground
    const float Floor = 0.6f;                       // floor level (top of the foundation)
    const float BodyX0 = -7f, BodyX1 = 7f;          // 14 m wide
    const float BodyZ0 = -5f, BodyZ1 = 5f;          // 10 m deep
    const float WallH = 4f;                         // one tall storey, like the 2D house
    const float RoofRise = 3.4f, RoofOverhang = 0.6f, Fascia = 0.2f;
    const float PorchX0 = -5.4f, PorchX1 = 5.4f;    // porch over the door and both front windows
    const float PorchFront = 7.6f, PostZ = 7.45f, StepsFront = 8.5f;
    const float PorchRoofAtWall = 3.6f;             // height where the porch roof meets the wall
    const float WindowY = Floor + 1.5f;             // window centres

    // the whole layout (roof overhang, chimney, steps, shrubs), to fit it inside the old footprint
    const float LayoutMinX = -7.75f, LayoutMaxX = 8.05f, LayoutBack = -6f, LayoutFront = 8.7f;
    const float FrontGap = 3f;                      // free ground kept between the steps and the old house's front edge

    [MenuItem("Tools/House Builder/Rebuild house")]
    static void Rebuild()
    {
        if (EditorApplication.isPlaying) { Fail("Stop Play mode first. Changes made in Play mode are not saved."); return; }
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) { Fail("No terrain found in the open scene. Open GameScene first."); return; }

        var old = FindOldHouse();
        var root = FindBuild();
        if (old == null && root == null)
        {
            Fail($"Could not find the old house in the open scene (an instance of {OldPrefabPath}, or an object named 'House' " +
                 "with a 'Home' child), and there is no 'Farmhouse' from an earlier run. Open GameScene first.");
            return;
        }

        Undo.SetCurrentGroupName(UndoName);
        int undoGroup = Undo.GetCurrentGroup();
        var log = new System.Text.StringBuilder();
        var roads = RoadBounds();

        // 1. the root: reuse the one from an earlier run (keeps where it is), or place a new one from the old house
        if (root == null)
        {
            if (!OldFootprint(old, out Vector3 centre, out Vector2 size, out float yaw)) { Fail("The old house has no size to copy."); return; }
            var oldRot = Quaternion.Euler(0f, yaw, 0f);
            Vector3 front = oldRot * FrontSide;
            float halfDepth = Mathf.Abs(FrontSide.x) > 0.5f ? size.x / 2f : size.y / 2f;
            var rot = Quaternion.LookRotation(front, Vector3.up);

            // move the house toward the front inside the old footprint, leaving FrontGap before the old front edge,
            // and centre the layout from side to side
            float shift = Mathf.Max(0f, Mathf.Min(halfDepth - FrontGap - LayoutFront, halfDepth + LayoutBack));
            float side = -(LayoutMinX + LayoutMaxX) / 2f;
            Vector3 pos = centre + rot * new Vector3(side, 0f, shift);

            root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, UndoName);
            var parent = old != null ? old.transform.parent : null;
            if (parent == null) { var b = GameObject.Find("Buildings"); if (b != null) parent = b.transform; }
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(new Vector3(pos.x, 0f, pos.z), rot);
            float ground = LocalGround(terrain, root.transform, new Vector3((LayoutMinX + LayoutMaxX) / 2f, 0f, (LayoutBack + LayoutFront) / 2f),
                LayoutMaxX - LayoutMinX, LayoutFront - LayoutBack);
            root.transform.position = new Vector3(pos.x, ground, pos.z);

            float? gap = RoadAhead(centre + front * halfDepth, front, 60f, roads);
            log.Append($"\nOld house footprint: centre X {centre.x:0.0}, Z {centre.z:0.0}, size {size.x:0.0} x {size.y:0.0} m, turned {yaw:0} deg.");
            log.Append($"\nFront faces {AxisName(front)} " + (gap.HasValue ? $"(a road starts {gap.Value:0.0} m past the old house)" : "(no road found that way)") +
                       $". House moved {shift:0.0} m toward the front inside the old footprint.");
            log.Append("\n" + CarryOver(old, root.transform));
        }
        else
        {
            log.Append($"\nKept the existing '{RootName}' (position and facing as it is); rebuilt its model.");
        }

        // 2. the old house: turned off, not deleted
        if (old != null && old.activeSelf)
        {
            Undo.RecordObject(old, UndoName);
            old.SetActive(false);
            if (PrefabUtility.IsPartOfPrefabInstance(old)) PrefabUtility.RecordPrefabInstancePropertyModifications(old);
            log.Append($"\nThe old '{old.name}' is turned off (not deleted). Delete it by hand when you are happy with the new house.");
        }

        // 3. the model: delete the one from the last run, build a new one. A Model you moved or scaled by hand
        // keeps its position, rotation and scale; a first build uses the owner's size (DefaultModelScale).
        var oldModel = root.transform.Find(ModelName);
        Vector3 modelPos = DefaultModelOffset, modelScale = Vector3.one * DefaultModelScale;
        Quaternion modelRot = Quaternion.identity;
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

        log.Append("\n" + BuildModel(model, terrain, roads));
        SetStatic(model.gameObject);

        int parts = model.GetComponentsInChildren<MeshRenderer>(true).Length;
        int colliders = model.GetComponentsInChildren<Collider>(true).Length;
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Vector3 p = root.transform.position;
        Debug.Log($"House rebuilt at X {p.x:0.0}, Z {p.z:0.0} ({parts} parts, {colliders} colliders). Save the scene (Ctrl+S)." + log, root);
    }

    static void Fail(string message)
    {
        Debug.LogError("House Builder: " + message);
        EditorUtility.DisplayDialog("House Builder", message, "OK");
    }

    // ---------------------------------------------------------------- finding things

    // the old house: an instance of House.prefab, or (if it was unpacked) an object named House with a Home child
    static GameObject FindOldHouse()
    {
        string path = AssetDatabase.GUIDToAssetPath(OldPrefabGuid);
        if (string.IsNullOrEmpty(path)) path = OldPrefabPath;
        GameObject byName = null;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var go = t.gameObject;
            if (PrefabUtility.IsAnyPrefabInstanceRoot(go) && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go) == path) return go;
            if (byName == null && go.name == "House" && t.Find("Home") != null && t.Find(ModelName) == null) byName = go;
        }
        return byName;
    }

    // the house built by an earlier run
    static GameObject FindBuild()
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == RootName && !PrefabUtility.IsPartOfPrefabInstance(t.gameObject)) return t.gameObject;
        return null;
    }

    // The old house's ground plan: its "Home" box if it has one, else the box around all its meshes.
    // Works on a turned-off house too (mesh bounds, not renderer bounds).
    static bool OldFootprint(GameObject old, out Vector3 centre, out Vector2 size, out float yaw)
    {
        centre = default; size = default; yaw = 0f;
        if (old == null) return false;
        var home = old.transform.Find("Home");
        var homeMesh = home != null ? home.GetComponent<MeshFilter>() : null;
        if (homeMesh != null && homeMesh.sharedMesh != null)
        {
            var b = homeMesh.sharedMesh.bounds;
            var s = home.lossyScale;
            centre = home.TransformPoint(b.center);
            size = new Vector2(Mathf.Abs(b.size.x * s.x), Mathf.Abs(b.size.z * s.z));
            yaw = home.eulerAngles.y;
            return true;
        }

        if (MeshBox(old, out Bounds box)) { centre = box.center; size = new Vector2(box.size.x, box.size.z); return true; }

        centre = old.transform.position;
        size = new Vector2(28f, 40f);
        yaw = old.transform.eulerAngles.y;
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

    // the road strips (their boxes), to find where paths and driveways meet a road
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

    static string AxisName(Vector3 d)
    {
        if (Mathf.Abs(d.x) > Mathf.Abs(d.z)) return d.x > 0 ? "+X" : "-X";
        return d.z > 0 ? "+Z" : "-Z";
    }

    // the highest ground under a rectangle in the house's space, as a height in that space
    static float LocalGround(Terrain terrain, Transform frame, Vector3 localCentre, float sizeX, float sizeZ)
    {
        float ground = float.MinValue;
        for (int i = 0; i <= 4; i++)
            for (int k = 0; k <= 4; k++)
            {
                var w = frame.TransformPoint(localCentre + new Vector3((i / 4f - 0.5f) * sizeX, 0f, (k / 4f - 0.5f) * sizeZ));
                ground = Mathf.Max(ground, terrain.SampleHeight(w) + terrain.transform.position.y);
            }
        return ground - frame.position.y;
    }

    // Scripts and trigger zones on the old house are copied onto empty objects under the new root, at the same
    // place in the world. The old House has neither (only plain colliders), so this normally copies nothing.
    static string CarryOver(GameObject old, Transform root)
    {
        if (old == null) return "No old house to copy scripts or triggers from.";
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
            ? "The old house had no scripts or trigger zones (only plain colliders), so there was nothing to carry over."
            : $"Copied from the old house: {string.Join(", ", copied)}. Check any references in them that pointed into the old house.";
    }

    // ---------------------------------------------------------------- the house

    static string BuildModel(Transform model, Terrain terrain, List<Bounds> roads)
    {
        var m = new Mats();
        float top = Floor + WallH;              // wall top
        float bodyCx = (BodyX0 + BodyX1) / 2f, bodyW = BodyX1 - BodyX0, bodyD = BodyZ1 - BodyZ0;
        float deck = Floor + 0.02f;             // porch deck top, 2 cm over the foundation so they never share a face

        // ---- house: walls (down into the ground, so the one collider covers the foundation too), stone foundation strip, corner boards
        var house = Group(model, "House", Vector3.zero);
        Box(house, "Walls", new Vector3(bodyCx, (top - 1f) / 2f, 0f), new Vector3(bodyW, top + 1f, bodyD), m.siding, collider: true);
        Box(house, "Foundation", new Vector3(bodyCx, (Floor - 1.02f) / 2f, 0f), new Vector3(bodyW + 0.1f, Floor + 1.02f, bodyD + 0.1f), m.stone);
        foreach (float x in new[] { BodyX0, BodyX1 })
            foreach (float z in new[] { BodyZ0, BodyZ1 })
                Box(house, "Corner Board", new Vector3(x, (Floor + 0.01f + top) / 2f, z), new Vector3(0.22f, top - Floor - 0.01f, 0.22f), m.trim);

        // ---- orange hip roof (one mesh with a white fascia and soffit), 5 cm above the wall top, gutters front and back
        float roofY = top + 0.05f;
        var roof = new GameObject("Roof", typeof(MeshFilter), typeof(MeshRenderer));
        roof.transform.SetParent(house, false);
        roof.transform.localPosition = new Vector3(bodyCx, roofY, 0f);
        roof.GetComponent<MeshFilter>().sharedMesh = HipRoofMesh(bodyW, bodyD, RoofRise, RoofOverhang, Fascia);
        roof.GetComponent<MeshRenderer>().sharedMaterials = new[] { m.roof, m.trim };
        float eaveY = roofY - RoofOverhang * RoofRise / (bodyD / 2f) - Fascia; // bottom of the fascia
        foreach (float s in new[] { -1f, 1f })
            Box(house, "Gutter", new Vector3(bodyCx, eaveY + 0.07f, s * (bodyD / 2f + RoofOverhang + 0.06f)), new Vector3(bodyW + 2f * RoofOverhang - 0.1f, 0.14f, 0.16f), m.trim);

        // ---- outside brick chimney on the +X end (toward the main road), standing past the ridge
        float ridgeTop = roofY + RoofRise;
        Box(house, "Chimney Base", new Vector3(BodyX1 + 0.5f, 1.25f, 0f), new Vector3(1.1f, 3.5f, 2f), m.brick, collider: true);
        Box(house, "Chimney", new Vector3(BodyX1 + 0.43f, (2.9f + ridgeTop + 0.5f) / 2f, 0f), new Vector3(0.9f, ridgeTop + 0.5f - 2.9f, 1.3f), m.brick);
        Box(house, "Chimney Cap", new Vector3(BodyX1 + 0.43f, ridgeTop + 0.57f, 0f), new Vector3(1.05f, 0.15f, 1.45f), m.stone);

        // ---- front (+Z): red-orange double door between two big windows, like the 2D house; green shutters
        float doorX = bodyCx;
        DoubleDoor(house, new Vector3(doorX, deck, BodyZ1), 1.8f, 2.4f, m);
        Box(house, "Porch Light", new Vector3(doorX + 1.35f, deck + 1.95f, BodyZ1 + 0.09f), new Vector3(0.16f, 0.24f, 0.22f), m.lamp);
        Box(house, "Porch Light Cap", new Vector3(doorX + 1.35f, deck + 2.1f, BodyZ1 + 0.1f), new Vector3(0.24f, 0.07f, 0.26f), m.dark);
        foreach (float x in new[] { doorX - 3.35f, doorX + 3.35f })
            Window(house, "Front Window", new Vector3(x, WindowY, BodyZ1), 0f, 1.4f, 1.9f, m, shutters: true, sill: true);

        // ---- ends: +X (main road) with shutters either side of the chimney, -X (toward Garage EX) plain
        foreach (float z in new[] { -2.6f, 2.6f })
        {
            Window(house, "Side Window", new Vector3(BodyX1, WindowY, z), 90f, 1.2f, 1.7f, m, shutters: true, sill: true);
            Window(house, "Side Window", new Vector3(BodyX0, WindowY, z), -90f, 1.2f, 1.7f, m, shutters: false, sill: true);
        }

        // ---- back (-Z, toward the fields): back door with a stone step, three windows
        float backDoorX = bodyCx + 2.3f;
        Door(house, "Back Door", new Vector3(backDoorX, Floor, BodyZ0), 180f, 1f, 2.2f, m);
        Box(house, "Back Step", new Vector3(backDoorX, 0.025f, BodyZ0 - 0.45f), new Vector3(1.8f, 0.65f, 1f), m.stone);
        foreach (float x in new[] { bodyCx - 4.5f, bodyCx - 0.5f, bodyCx + 5f })
            Window(house, "Back Window", new Vector3(x, WindowY, BodyZ0), 180f, 1.2f, 1.5f, m, shutters: false, sill: true);

        // ---- front porch: deck, steps, four posts, beam, railings and a shed roof against the house
        var porch = Group(model, "Porch", Vector3.zero);
        float porchCx = (PorchX0 + PorchX1) / 2f, porchW = PorchX1 - PorchX0;
        Box(porch, "Deck", new Vector3(porchCx, (deck - 0.3f) / 2f, (BodyZ1 - 0.05f + PorchFront) / 2f), new Vector3(porchW, deck + 0.3f, PorchFront - BodyZ1 + 0.05f), m.wood);
        for (int i = 0; i < 3; i++)
        {
            // each lower step is 4 cm wider, so the side faces never line up
            float stepTop = deck * (3 - i) / 4f, stepFront = PorchFront + 0.3f * (i + 1);
            Box(porch, "Step", new Vector3(doorX, (stepTop - 0.2f) / 2f, (PorchFront - 0.05f + stepFront) / 2f),
                new Vector3(2.2f + 0.04f * i, stepTop + 0.2f, stepFront - PorchFront + 0.05f), m.wood);
        }
        float[] posts = { PorchX0 + 0.15f, doorX - 1.45f, doorX + 1.45f, PorchX1 - 0.15f };
        const float beamY = 2.85f;
        foreach (float x in posts)
            Box(porch, "Post", new Vector3(x, (deck + beamY - 0.1f) / 2f, PostZ), new Vector3(0.2f, beamY - 0.1f - deck, 0.2f), m.trim);
        Box(porch, "Beam", new Vector3(porchCx, beamY, PostZ), new Vector3(porchW, 0.2f, 0.22f), m.trim);
        for (int s = 0; s < 2; s++)
        {
            float x0 = posts[s * 2], x1 = posts[s * 2 + 1];
            Box(porch, "Rail", new Vector3((x0 + x1) / 2f, deck + 0.9f, PostZ), new Vector3(x1 - x0, 0.08f, 0.1f), m.trim);
            Box(porch, "Rail", new Vector3((x0 + x1) / 2f, deck + 0.18f, PostZ), new Vector3(x1 - x0, 0.08f, 0.1f), m.trim);
        }
        {
            // roof slab: its top meets the wall at PorchRoofAtWall and slopes 12 degrees down toward the front
            const float tilt = 12f, thick = 0.14f;
            float a = tilt * Mathf.Deg2Rad, wallZ = BodyZ1 - 0.05f, run = PostZ + 0.4f - wallZ;
            float zm = wallZ + run / 2f, ym = PorchRoofAtWall - run / 2f * Mathf.Tan(a);
            var centre = new Vector3(porchCx, ym - Mathf.Cos(a) * thick / 2f, zm - Mathf.Sin(a) * thick / 2f);
            Box(porch, "Porch Roof", centre, new Vector3(porchW + 0.4f, thick, run / Mathf.Cos(a)), m.roof, euler: new Vector3(tilt, 0f, 0f));
        }
        // one solid box for the whole porch, so vehicles stop at it instead of climbing the deck or driving through the posts
        var porchBox = porch.gameObject.AddComponent<BoxCollider>();
        porchBox.center = new Vector3(porchCx, 1.55f, (BodyZ1 + PostZ + 0.4f) / 2f);
        porchBox.size = new Vector3(porchW + 0.4f, 3.5f, PostZ + 0.4f - BodyZ1);

        // ---- yard: shrubs and a mailbox by the road. (The stepping stones and the garage driveway were
        // removed by the owner on 2026-11-03, so they are not built any more.)
        var yard = Group(model, "Yard", Vector3.zero);
        foreach (float x in new[] { doorX - 3.3f, doorX + 3.3f })
            Ball(yard, "Shrub", new Vector3(x, 0.45f, PorchFront + 0.6f), new Vector3(1.5f, 1f, 1f), m.shrub);
        foreach (float x in new[] { BodyX0 + 0.7f, BodyX1 - 0.7f })
            Ball(yard, "Shrub", new Vector3(x, 0.45f, BodyZ1 + 1f), new Vector3(1.4f, 1f, 1f), m.shrub);

        float pathEnd = RoadEnd(model, new Vector3(doorX, 0f, StepsFront), roads);
        var mailPos = new Vector3(doorX + 1.6f, 0f, Mathf.Max(StepsFront + 1f, pathEnd - 0.8f));
        mailPos.y = LocalGround(terrain, model, mailPos, 0.3f, 0.3f);
        var mail = Group(yard, "Mailbox", mailPos);
        Box(mail, "Post", new Vector3(0f, 0.45f, 0f), new Vector3(0.12f, 1.1f, 0.12f), m.wood);
        Box(mail, "Box", new Vector3(0f, 1.15f, 0f), new Vector3(0.3f, 0.3f, 0.55f), m.dark);
        Box(mail, "Flag", new Vector3(0.17f, 1.3f, -0.1f), new Vector3(0.03f, 0.22f, 0.07f), m.door);

        return "Built the yard: shrubs and a mailbox (no stepping stones or driveway).";
    }

    // where a path from a point straight to the front reaches a road (in the house's space), or a short path if there is none
    static float RoadEnd(Transform frame, Vector3 localStart, List<Bounds> roads, float stopShort = 0.1f)
    {
        float? d = RoadAhead(frame.TransformPoint(localStart), frame.forward, 30f, roads);
        return localStart.z + (d.HasValue && d.Value > 1f ? d.Value - stopShort : 4f);
    }


    // ---------------------------------------------------------------- pieces

    // A hip roof: four slopes of the same pitch meeting at a short ridge along X, with a vertical fascia
    // around the eaves and a flat soffit under it. Origin = centre of the roof's base at the wall line.
    // Sub-mesh 0 is the roof, sub-mesh 1 the fascia and soffit. Saved in Assets/House and rebuilt in place each run.
    static Mesh HipRoofMesh(float length, float span, float rise, float overhang, float fascia)
    {
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(RoofMeshPath);
        bool isNew = mesh == null;
        if (isNew) mesh = new Mesh { name = "HouseRoofMesh" };
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
        Face(trimTris, Vector3.down, fl + down, fr + down, br + down, bl + down);

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

    // A window on a wall, like the 2D one: red-orange frame, blue glass split by a centre bar, white sill.
    // pos = centre on the wall face, yaw turns it to face out of that wall (0 = +Z, 90 = +X, 180 = -Z, -90 = -X).
    // Depths from the wall face: frame -0.02..0.08, glass 0..0.10, centre bar 0.09..0.14, shutters -0.01..0.07.
    static void Window(Transform parent, string name, Vector3 pos, float yaw, float w, float h, Mats m, bool shutters, bool sill)
    {
        var g = Group(parent, name, pos, yaw);
        const float f = 0.12f; // frame border
        Box(g, "Frame", new Vector3(0f, 0f, 0.03f), new Vector3(w + 2f * f, h + 2f * f, 0.1f), m.frame);
        Box(g, "Glass", new Vector3(0f, 0f, 0.05f), new Vector3(w, h, 0.1f), m.glass);
        Box(g, "Centre Bar", new Vector3(0f, 0f, 0.115f), new Vector3(0.08f, h, 0.05f), m.frame);
        if (sill) Box(g, "Sill", new Vector3(0f, -h / 2f - f - 0.03f, 0.08f), new Vector3(w + 2f * f + 0.2f, 0.08f, 0.2f), m.trim);
        if (!shutters) return;
        float sw = w * 0.5f, sx = w / 2f + f + 0.03f + sw / 2f;
        Box(g, "Shutter", new Vector3(-sx, 0f, 0.03f), new Vector3(sw, h + 2f * f, 0.08f), m.shutter);
        Box(g, "Shutter", new Vector3(sx, 0f, 0.03f), new Vector3(sw, h + 2f * f, 0.08f), m.shutter);
    }

    // the front double door (2D: red-orange with a split in the middle), white frame, two brass knobs.
    // basePos = bottom centre on the wall face.
    static void DoubleDoor(Transform parent, Vector3 basePos, float w, float h, Mats m)
    {
        var g = Group(parent, "Front Door", basePos);
        Box(g, "Door Frame", new Vector3(0f, (h + 0.15f) / 2f + 0.005f, 0.03f), new Vector3(w + 0.3f, h + 0.15f, 0.1f), m.trim);
        Box(g, "Doors", new Vector3(0f, h / 2f + 0.01f, 0.05f), new Vector3(w, h - 0.01f, 0.1f), m.door);
        Box(g, "Door Split", new Vector3(0f, h / 2f + 0.01f, 0.105f), new Vector3(0.05f, h - 0.05f, 0.03f), m.brick);
        Box(g, "Knob", new Vector3(-0.15f, h * 0.45f, 0.13f), new Vector3(0.07f, 0.07f, 0.07f), m.lamp);
        Box(g, "Knob", new Vector3(0.15f, h * 0.45f, 0.13f), new Vector3(0.07f, 0.07f, 0.07f), m.lamp);
    }

    // a single door with a white frame and a knob; basePos = bottom centre on the wall face, yaw as for Window
    static void Door(Transform parent, string name, Vector3 basePos, float yaw, float w, float h, Mats m)
    {
        var g = Group(parent, name, basePos, yaw);
        Box(g, "Door Frame", new Vector3(0f, (h + 0.15f) / 2f + 0.005f, 0.03f), new Vector3(w + 0.3f, h + 0.15f, 0.1f), m.trim);
        Box(g, "Door", new Vector3(0f, h / 2f + 0.01f, 0.05f), new Vector3(w, h - 0.01f, 0.1f), m.door);
        Box(g, "Knob", new Vector3(w * 0.36f, h * 0.45f, 0.13f), new Vector3(0.07f, 0.07f, 0.07f), m.lamp);
    }

    static Transform Group(Transform parent, string name, Vector3 pos, float yaw = 0f)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = pos;
        t.localRotation = Quaternion.Euler(0f, yaw, 0f);
        return t;
    }

    static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, bool collider = false, Vector3 euler = default)
        => Prim(PrimitiveType.Cube, parent, name, pos, size, mat, collider, euler);

    static GameObject Ball(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        => Prim(PrimitiveType.Sphere, parent, name, pos, size, mat, false, default);

    // all positions are local to the parent; only pieces asked for keep their collider
    // (Unity's cylinder is 2 units tall, so a cylinder's Y scale is half its height)
    static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 pos, Vector3 scale, Material mat, bool collider, Vector3 euler)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
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
        if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets", "House");
    }

    // materials are saved as assets in Assets/House, so the scene keeps them; an existing one is reused as it is.
    // Colours are taken from the 2D house where it has them (walls, roof, door, frames, glass, stones, driveway).
    class Mats
    {
        public readonly Material siding = Get("HouseWall", new Color(0.97f, 0.76f, 0.5f), 0.1f);       // 2D peach walls
        public readonly Material roof = Get("HouseRoof", new Color(0.92f, 0.5f, 0.21f), 0.2f);         // 2D orange roof
        public readonly Material frame = Get("HouseFrame", new Color(0.84f, 0.31f, 0.13f), 0.25f);     // 2D window frames
        public readonly Material door = Get("HouseDoor", new Color(0.8f, 0.3f, 0.14f), 0.3f);          // 2D red-orange door
        public readonly Material glass = Get("HouseGlass", new Color(0.33f, 0.62f, 0.88f), 0.85f);     // 2D light blue glass
        public readonly Material trim = Get("HouseTrim", new Color(0.96f, 0.95f, 0.92f), 0.25f);       // white trim, fascia, gutters, posts
        public readonly Material shutter = Get("HouseShutter", new Color(0.13f, 0.33f, 0.2f), 0.2f);   // dark green
        public readonly Material stone = Get("HouseStone", new Color(0.56f, 0.55f, 0.52f), 0.05f);     // foundation, back step, chimney cap
        public readonly Material brick = Get("HouseBrick", new Color(0.55f, 0.26f, 0.19f), 0.05f);
        public readonly Material wood = Get("HouseWood", new Color(0.55f, 0.42f, 0.3f), 0.05f);        // porch deck, steps, mailbox post
        public readonly Material dark = Get("HouseDark", new Color(0.2f, 0.2f, 0.22f), 0.3f);          // mailbox, lamp cap
        public readonly Material lamp = Get("HouseLamp", new Color(0.98f, 0.86f, 0.5f), 0.6f);         // porch light, door knobs
        public readonly Material shrub = Get("HouseShrub", new Color(0.2f, 0.4f, 0.17f), 0.05f);

        static Material Get(string name, Color color, float gloss)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            EnsureFolder();
            mat = new Material(Shader.Find("Standard")) { color = color };
            mat.SetFloat("_Glossiness", gloss);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}
