using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-off helpers for laying out fields. Run them from the Tools > Fields menu, look at the result,
/// then press Ctrl+S. Both can be undone with Ctrl+Z. Delete this file when you are happy with the map.
///
///  1. Split Home Field Into Four: turns the player's main field (Tony) into a 2 x 2 grid with even gaps.
///     The two fields in the row facing the house and garage belong to the player. The one nearest the
///     house starts planted with wheat; the other three start empty. The back two get AI owners when
///     the game starts.
///  2. Add Random Fields: adds fields of random sizes on free land (not on roads, buildings, other fields
///     or steep ground). Run it again to roll a new layout. Resize them in the Inspector afterwards.
/// </summary>
public static class FieldLayoutTools
{
    // ---- settings, change and run again ----
    private const float Gap = 12f;               // empty space between the four home subfields (world units)
    private const int RandomFieldCount = 6;      // how many random fields to add
    private const float MinSide = 60f;           // random field side length range (world units)
    private const float MaxSide = 180f;
    private const float MarginToObstacles = 15f; // free space kept around roads, buildings and fields
    private const float EdgeMargin = 40f;        // free space kept at the edge of the terrain
    private const float MaxHeightDifference = 0.3f; // ground must be this flat under a new field

    private const string SplitPrefix = "Field Tony ";
    private const string RandomPrefix = "Random Field ";

    // ------------------------------------------------------------------ split

    [MenuItem("Tools/Fields/Split Home Field Into Four")]
    private static void SplitHomeField()
    {
        foreach (var existing in Object.FindObjectsByType<FieldController>(FindObjectsSortMode.None))
        {
            if (existing.name.StartsWith(SplitPrefix))
            {
                EditorUtility.DisplayDialog("Split home field", "The home field is already split (found '" + existing.name + "'). Use Ctrl+Z to undo it first if you want to do it again.", "OK");
                return;
            }
        }

        var tony = FindPlayerField();
        if (tony == null)
        {
            EditorUtility.DisplayDialog("Split home field", "Could not find the player's field (a FieldController whose Owner Name equals its Player Name).", "OK");
            return;
        }

        var home = GameObject.Find("HomeArea");
        Transform house = home != null ? home.transform.Find("House") : null;
        Transform garage = home != null ? home.transform.Find("Garage EX") : null;
        if (house == null || garage == null)
        {
            EditorUtility.DisplayDialog("Split home field", "Could not find 'House' and 'Garage EX' under 'HomeArea'.", "OK");
            return;
        }

        var renderer = tony.GetComponent<MeshRenderer>();
        if (renderer == null || Quaternion.Angle(tony.transform.rotation, Quaternion.identity) > 0.5f
            || Vector3.Distance(renderer.bounds.center, tony.transform.position) > 0.5f)
        {
            EditorUtility.DisplayDialog("Split home field", "The home field must be an un-rotated, centred plane. Not changing anything.", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Split home field");

        // clone first (so the clones copy Tony's original settings), then change all four
        var bounds = renderer.bounds;
        var objs = new GameObject[4];
        objs[0] = tony.gameObject;
        for (int i = 1; i < 4; i++)
        {
            objs[i] = Object.Instantiate(tony.gameObject, tony.transform.parent);
            Undo.RegisterCreatedObjectUndo(objs[i], "Split home field");
        }

        float cellW = (bounds.size.x - Gap) / 2f;
        float cellL = (bounds.size.z - Gap) / 2f;
        float scaleX = tony.transform.localScale.x * cellW / bounds.size.x;
        float scaleZ = tony.transform.localScale.z * cellL / bounds.size.z;

        // grid cells: index = ix + 2 * iz, ix along world X, iz along world Z
        var centres = new Vector3[4];
        for (int iz = 0; iz < 2; iz++)
            for (int ix = 0; ix < 2; ix++)
                centres[ix + 2 * iz] = new Vector3(
                    bounds.min.x + cellW / 2f + ix * (cellW + Gap),
                    tony.transform.position.y,
                    bounds.min.z + cellL / 2f + iz * (cellL + Gap));

        // house and garage sit side by side along one axis; the two fields in the row that lines up
        // with them (closest across that axis) are the front row
        Vector3 hp = Centre(house), gp = Centre(garage);
        bool axisIsX = Mathf.Abs(gp.x - hp.x) >= Mathf.Abs(gp.z - hp.z);
        float buildingsAcross = axisIsX ? (hp.z + gp.z) / 2f : (hp.x + gp.x) / 2f;

        float Across(Vector3 p) => axisIsX ? p.z : p.x;
        var front = new List<int>();
        float row0 = Mathf.Abs(Across(centres[0]) - buildingsAcross);
        float row1 = Mathf.Abs(Across(centres[axisIsX ? 2 : 1]) - buildingsAcross);
        bool firstRowIsFront = row0 <= row1;
        for (int i = 0; i < 4; i++)
        {
            bool inFirstRow = axisIsX ? i < 2 : (i % 2 == 0);
            if (inFirstRow == firstRowIsFront) front.Add(i);
        }

        // of the front two, the one nearest the house starts planted
        int planted = front[0], other = front[1];
        if (Vector3.Distance(Flat(centres[other]), Flat(hp)) < Vector3.Distance(Flat(centres[planted]), Flat(hp)))
            (planted, other) = (other, planted);

        string playerName = ReadString(tony, "playerName");
        int backNumber = 3;
        for (int i = 0; i < 4; i++)
        {
            var go = objs[i];
            Undo.RecordObject(go.transform, "Split home field");
            go.transform.position = centres[i];
            go.transform.localScale = new Vector3(scaleX, go.transform.localScale.y, scaleZ);

            int number;
            string owner;
            CropType start = CropType.None;
            if (i == planted) { number = 1; owner = playerName; start = CropType.Wheat; }
            else if (i == other) { number = 2; owner = playerName; }
            else { number = backNumber++; owner = ""; } // AI owner and name are picked when the game starts

            go.name = SplitPrefix + number;
            Configure(go.GetComponent<FieldController>(), owner == "" ? "" : "Tony " + number, owner, start);
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(tony.gameObject.scene);
        Debug.Log($"Home field split into four ({cellW:0} x {cellL:0} each, gap {Gap}). House and garage are side by side along {(axisIsX ? "X" : "Z")}. " +
                  $"'{SplitPrefix}1' (nearest the house, planted) and '{SplitPrefix}2' are yours. Press Ctrl+S to save the scene.");
    }

    // ------------------------------------------------------------------ random fields

    [MenuItem("Tools/Fields/Add Random Fields")]
    private static void AddRandomFields()
    {
        var terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            EditorUtility.DisplayDialog("Random fields", "No terrain found in the scene.", "OK");
            return;
        }

        FieldController template = null;
        var oldRandom = new List<GameObject>();
        foreach (var f in Object.FindObjectsByType<FieldController>(FindObjectsSortMode.None))
        {
            if (f.name.StartsWith(RandomPrefix)) oldRandom.Add(f.gameObject);
            else if (template == null || f.name.StartsWith(SplitPrefix + "1")) template = f;
        }
        if (template == null)
        {
            EditorUtility.DisplayDialog("Random fields", "No field to copy from. Split the home field first, or keep at least one field in the scene.", "OK");
            return;
        }

        if (oldRandom.Count > 0 && !EditorUtility.DisplayDialog("Random fields",
                $"Replace the {oldRandom.Count} random fields already in the scene with a new random layout? (Any resizing you did to them is lost.)", "Replace", "Cancel"))
            return;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Add random fields");
        foreach (var go in oldRandom) Undo.DestroyObjectImmediate(go);

        // everything a new field must stay away from (rectangles on the ground plane)
        var obstacles = new List<Rect>();
        foreach (var name in new[] { "Fields", "Roads and paths", "Buildings" })
        {
            var root = GameObject.Find(name);
            if (root != null) AddObstacles(root.transform, obstacles, MarginToObstacles);
        }
        var player = Object.FindFirstObjectByType<GameCharacter>();
        if (player != null && player.characterLocation != null) AddObstacles(player.characterLocation, obstacles, MarginToObstacles);

        var tdata = terrain.terrainData;
        Vector3 tpos = terrain.transform.position;
        float x0 = tpos.x + EdgeMargin, x1 = tpos.x + tdata.size.x - EdgeMargin;
        float z0 = tpos.z + EdgeMargin, z1 = tpos.z + tdata.size.z - EdgeMargin;

        var parent = template.transform.parent;
        var meshSize = template.GetComponent<MeshFilter>().sharedMesh.bounds.size;
        Vector3 parentScale = parent != null ? parent.lossyScale : Vector3.one;
        string playerName = ReadString(template, "playerName");

        int placed = 0;
        for (int n = 1; n <= RandomFieldCount; n++)
        {
            bool done = false;
            for (int attempt = 0; attempt < 4000 && !done; attempt++)
            {
                float w = Snap(Random.Range(MinSide, MaxSide));
                float l = Snap(Random.Range(MinSide, MaxSide));
                if (x1 - x0 < w || z1 - z0 < l) continue;
                float cx = Random.Range(x0 + w / 2f, x1 - w / 2f);
                float cz = Random.Range(z0 + l / 2f, z1 - l / 2f);
                var rect = new Rect(cx - w / 2f, cz - l / 2f, w, l);

                bool blocked = false;
                foreach (var o in obstacles) if (o.Overlaps(rect)) { blocked = true; break; }
                if (blocked) continue;

                float top;
                if (!GroundIsFlat(terrain, rect, out top)) continue;

                var go = Object.Instantiate(template.gameObject, parent);
                Undo.RegisterCreatedObjectUndo(go, "Add random fields");
                go.name = RandomPrefix + n;
                go.transform.rotation = Quaternion.identity;
                go.transform.position = new Vector3(cx, top + 0.01f, cz);
                go.transform.localScale = new Vector3(w / (meshSize.x * parentScale.x), go.transform.localScale.y, l / (meshSize.z * parentScale.z));
                Configure(go.GetComponent<FieldController>(), "", "", CropType.None);

                // keep the next random fields away from this one too
                obstacles.Add(new Rect(rect.x - MarginToObstacles, rect.y - MarginToObstacles, rect.width + 2 * MarginToObstacles, rect.height + 2 * MarginToObstacles));
                placed++;
                done = true;
            }
            if (!done) Debug.LogWarning($"Could not find free flat ground for random field {n}.");
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(template.gameObject.scene);

        int aiFields = 0;
        foreach (var f in Object.FindObjectsByType<FieldController>(FindObjectsSortMode.None))
            if (ReadString(f, "ownerName") != playerName) aiFields++;
        Debug.Log($"Added {placed} random fields (all AI owned). Press Ctrl+S to save, or run the tool again for a new layout." +
                  (aiFields > 12 ? $" Warning: {aiFields} AI fields but only 12 field names, extra fields would get blank names (see PLAN.md)." : ""));
    }

    // ------------------------------------------------------------------ helpers

    private static FieldController FindPlayerField()
    {
        foreach (var f in Object.FindObjectsByType<FieldController>(FindObjectsSortMode.None))
            if (ReadString(f, "ownerName") == ReadString(f, "playerName")) return f;
        return null;
    }

    private static string ReadString(FieldController f, string property)
    {
        return new SerializedObject(f).FindProperty(property).stringValue;
    }

    private static void Configure(FieldController f, string fieldName, string owner, CropType startCrop)
    {
        var so = new SerializedObject(f);
        so.FindProperty("fieldName").stringValue = fieldName;
        so.FindProperty("ownerName").stringValue = owner;
        so.FindProperty("startCrop").enumValueIndex = (int)startCrop; // enum order: None, Wheat, Barley, Oat
        so.ApplyModifiedProperties();
    }

    private static Vector3 Centre(Transform t)
    {
        var renderers = t.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return t.position;
        var b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b.center;
    }

    private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    private static float Snap(float v) => Mathf.Round(v / 5f) * 5f;

    private static void AddObstacles(Transform root, List<Rect> list, float margin)
    {
        foreach (var r in root.GetComponentsInChildren<Renderer>())
        {
            var b = r.bounds;
            list.Add(new Rect(b.min.x - margin, b.min.z - margin, b.size.x + 2 * margin, b.size.z + 2 * margin));
        }
    }

    // the ground under the whole rectangle must be level (within MaxHeightDifference) so the field does not sink or float
    private static bool GroundIsFlat(Terrain terrain, Rect rect, out float top)
    {
        float min = float.MaxValue, max = float.MinValue;
        for (int i = 0; i <= 4; i++)
        {
            for (int j = 0; j <= 4; j++)
            {
                var p = new Vector3(rect.xMin + rect.width * i / 4f, 0f, rect.yMin + rect.height * j / 4f);
                float h = terrain.SampleHeight(p) + terrain.transform.position.y;
                min = Mathf.Min(min, h);
                max = Mathf.Max(max, h);
            }
        }
        top = max;
        return max - min <= MaxHeightDifference;
    }
}
