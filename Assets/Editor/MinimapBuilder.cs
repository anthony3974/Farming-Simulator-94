using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Editor tool: builds the minimap (PLAN: minimap) as real UI objects in GameScene.
// Use: open GameScene, Tools > Build Minimap, save the scene (Ctrl+S).
//
// - The "Minimap" canvas (sorted with the gauges, under the touch buttons and the menus) holds "Border" (the panel, in
//   the middle of the left edge) and inside it "Map Area" with Roads, Fields, Buildings, the N and the Vehicle Marker.
// - Map Area is turned 90 degrees (north points left), inside an unturned Border. The letters are turned back so they
//   read upright, and the arrow turns with the map by itself.
// - The world area shown is the Map Border fence (the whole terrain if there is no fence). Roads and fields are placed
//   with their anchors, so they follow if the panel is resized. Fields and roads use their world bounds, so a rotated
//   one would show as its bounding box (all of them are square to the map now).
// - Buildings are a letter in a box, fixed size: W B O sell that crop, S seeds, F fertilizer, G gas, D dealer, H home.
//   Boxes that would overlap are pushed apart a little. The letters can be edited in the scene.
// - The panel's place works with and without touch controls: between the fuel gauge and the Settings button, on the
//   narrowest phones too. That gap is why it sits a little above the exact middle.
// - The Minimap script gets its references here.
// - Running it again only adds what is missing (a new field, say) and sets the references again, so hand changes stay.
//   To place something again, delete it and run the tool.
public static class MinimapBuilder
{
    const string Title = "Build Minimap";
    const string SceneName = "GameScene";
    const string RootName = "Minimap";
    const string ArrowPath = "Assets/Images/MinimapArrow.png";
    const string UndoName = "Build minimap";

    const int SortingOrder = 40; // like the speedometer and the fuel gauge
    const float BorderWidth = 4f;
    const float FieldFrameWidth = 2f;
    const float BoxSize = 26f;   // a building's box
    const float BoxGap = 2f;     // kept between two boxes
    const float BoxBorder = 2f;
    const float ArrowSize = 30f;
    const int PushPasses = 20;

    static readonly Vector2 PanelSize = new(280f, 240f); // the shape of the map on its side, about 1166 x 1000 m
    const float MapRotation = 90f;
    static readonly Vector2 PanelPosition = new(20f, 30f);

    static readonly Color Yellow = new Color32(255, 204, 0, 255);
    static readonly Color Brown = new Color32(60, 45, 20, 255);
    static readonly Color Gold = new Color32(255, 201, 51, 255);
    static readonly Color RoadColor = new Color32(150, 140, 120, 255);
    static readonly Color FieldFrame = new Color32(25, 18, 8, 255);
    static readonly Color FieldStart = new Color32(130, 90, 60, 255); // bare dirt; the script copies the real colour in Play
    static readonly Color ArrowColor = new Color32(235, 50, 35, 255);

    class BuildingEntry
    {
        public string name;
        public string letter;
        public Vector2 anchor;
        public Vector2 place;
    }

    [MenuItem("Tools/Build Minimap")]
    static void Build()
    {
        if (!CanEdit(out Scene scene, out Terrain terrain)) return;

        var arrow = AssetDatabase.LoadAssetAtPath<Sprite>(ArrowPath);
        if (arrow == null)
        {
            EditorUtility.DisplayDialog(Title, $"The arrow sprite {ArrowPath} is missing or is not imported as a Sprite.", "OK");
            return;
        }

        Undo.SetCurrentGroupName(UndoName);
        int undoGroup = Undo.GetCurrentGroup();

        GameObject root = Root(scene);
        Minimap minimap = root.GetComponent<Minimap>();
        if (minimap == null) minimap = Undo.AddComponent<Minimap>(root);
        Undo.RecordObject(minimap, UndoName);

        if (minimap.worldSize.x <= 0f || minimap.worldSize.y <= 0f)
            WorldArea(terrain, out minimap.worldMin, out minimap.worldSize);
        Vector2 worldMin = minimap.worldMin, worldSize = minimap.worldSize;

        Image border = NewImage(root.transform, "Border", Yellow, out bool created);
        RectTransform panel = border.rectTransform;
        if (created || panel.anchorMin.x > 0.5f) // also moves a panel built on the right side, where it first was
        {
            Undo.RecordObject(panel, UndoName);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0f, 0.5f);
            panel.sizeDelta = PanelSize;
            panel.anchoredPosition = PanelPosition;
        }

        Image ground = NewImage(panel, "Map Area", Brown, out created);
        RectTransform mapArea = ground.rectTransform;
        bool turning = created || !Mathf.Approximately(Mathf.DeltaAngle(mapArea.localEulerAngles.z, MapRotation), 0f); // new, or built at another angle
        if (turning) TurnMap(panel, mapArea);

        RectTransform roads = Group(mapArea, "Roads");
        RectTransform fields = Group(mapArea, "Fields");
        RectTransform buildings = Group(mapArea, "Buildings");

        AddRoads(roads, worldMin, worldSize);
        var marks = AddFields(fields, worldMin, worldSize);
        AddBuildings(buildings, terrain, mapArea.rect.size, worldMin, worldSize);

        TMP_Text north = Letter(mapArea, "North", "N", 18f, out created);
        if (created || turning)
        {
            RectTransform rt = north.rectTransform;
            Undo.RecordObject(rt, UndoName);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); // the map's north edge, the middle of the left side on screen
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(20f, 22f);
            rt.anchoredPosition = new Vector2(0f, -13f);
        }
        Upright(north);

        Image marker = NewImage(mapArea, "Vehicle Marker", ArrowColor, out created);
        if (created)
        {
            RectTransform rt = marker.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(ArrowSize, ArrowSize);
            marker.sprite = arrow;
        }

        // drawn in this order: the arrow on top of everything
        Order(roads, 0);
        Order(fields, 1);
        Order(buildings, 2);
        Order(north.transform, 3);
        Order(marker.transform, 4);

        minimap.panel = panel;
        minimap.mapArea = mapArea;
        minimap.vehicleMarker = marker.rectTransform;
        minimap.fields = marks;
        EditorUtility.SetDirty(minimap);

        // the map never takes a touch, whatever was added to it by hand
        foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            if (!graphic.raycastTarget) continue;
            Undo.RecordObject(graphic, UndoName);
            graphic.raycastTarget = false;
            EditorUtility.SetDirty(graphic);
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root;
        EditorUtility.DisplayDialog(Title, $"Minimap ready with {marks.Count} fields. Save the scene (Ctrl+S).", "OK");
    }

    static bool CanEdit(out Scene scene, out Terrain terrain)
    {
        scene = SceneManager.GetActiveScene();
        terrain = Terrain.activeTerrain;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(Title, "Stop Play mode first. Changes made in Play mode are lost.", "OK");
            return false;
        }
        if (scene.name != SceneName)
        {
            EditorUtility.DisplayDialog(Title, $"Open {SceneName} first (the open scene is '{scene.name}').", "OK");
            return false;
        }
        if (terrain == null)
        {
            EditorUtility.DisplayDialog(Title, "No terrain found in the open scene.", "OK");
            return false;
        }
        return true;
    }

    // the root canvas, set up like the Fuel Gauge canvas: no GraphicRaycaster, so it can never catch a touch
    static GameObject Root(Scene scene)
    {
        GameObject root = Array.Find(scene.GetRootGameObjects(), g => g.name == RootName);
        if (root == null)
        {
            root = new GameObject(RootName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(root, UndoName);
        }

        if (root.GetComponent<Canvas>() == null)
        {
            var canvas = Undo.AddComponent<Canvas>(root);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent; // for TextMeshPro
        }
        if (root.GetComponent<CanvasScaler>() == null)
        {
            var scaler = Undo.AddComponent<CanvasScaler>(root);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
        return root;
    }

    // the fence around the map, or the whole terrain without one
    static void WorldArea(Terrain terrain, out Vector2 min, out Vector2 size)
    {
        var fence = GameObject.Find("Map Border");
        Renderer[] parts = fence != null ? fence.GetComponentsInChildren<Renderer>() : Array.Empty<Renderer>();
        if (parts.Length > 0)
        {
            Bounds bounds = parts[0].bounds;
            foreach (var part in parts) bounds.Encapsulate(part.bounds);
            min = new Vector2(bounds.min.x, bounds.min.z);
            size = new Vector2(bounds.size.x, bounds.size.z);
            return;
        }

        Debug.LogWarning("Minimap: no 'Map Border' found, the map shows the whole terrain.");
        Vector3 position = terrain.transform.position, terrainSize = terrain.terrainData.size;
        min = new Vector2(position.x, position.z);
        size = new Vector2(terrainSize.x, terrainSize.z);
    }

    static void AddRoads(RectTransform parent, Vector2 worldMin, Vector2 worldSize)
    {
        var group = GameObject.Find("Roads and paths");
        if (group == null)
        {
            Debug.LogWarning("Minimap: no 'Roads and paths' found, the map has no roads.");
            return;
        }
        foreach (var road in group.GetComponentsInChildren<Renderer>())
        {
            Image image = NewImage(parent, road.name, RoadColor, out bool created);
            if (created) PlaceBounds(image.rectTransform, road.bounds, worldMin, worldSize);
        }
    }

    static List<Minimap.FieldMark> AddFields(RectTransform parent, Vector2 worldMin, Vector2 worldSize)
    {
        var marks = new List<Minimap.FieldMark>();
        var worldFields = UnityEngine.Object.FindObjectsByType<FieldController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Array.Sort(worldFields, (a, b) => string.CompareOrdinal(a.name, b.name));
        foreach (var field in worldFields)
        {
            var renderer = field.GetComponent<Renderer>();
            if (renderer == null)
            {
                Debug.LogWarning($"Minimap: field '{field.name}' has no Renderer, left off the map.", field);
                continue;
            }

            Image frame = NewImage(parent, field.name, FieldFrame, out bool created);
            if (created) PlaceBounds(frame.rectTransform, renderer.bounds, worldMin, worldSize);
            Image fill = NewImage(frame.transform, "Fill", FieldStart, out created);
            if (created) Stretch(fill.rectTransform, FieldFrameWidth);
            marks.Add(new Minimap.FieldMark { field = field, frame = frame, fill = fill });
        }
        return marks;
    }

    static void AddBuildings(RectTransform parent, Terrain terrain, Vector2 area, Vector2 worldMin, Vector2 worldSize)
    {
        var entries = new List<BuildingEntry>();
        foreach (var building in UnityEngine.Object.FindObjectsByType<Building>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string name = building.transform.parent != null ? building.transform.parent.name : building.name;
            entries.Add(Entry(name, BuildingLetter(building), building.transform.position, area, worldMin, worldSize));
        }
        Transform home = terrain.transform.Find("Buildings/HomeArea");
        if (home != null) entries.Add(Entry(home.name, "H", home.position, area, worldMin, worldSize));
        entries.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

        PushApart(entries, area);

        foreach (var entry in entries)
        {
            Image box = NewImage(parent, entry.name, Yellow, out bool created);
            if (created)
            {
                RectTransform rt = box.rectTransform;
                rt.anchorMin = rt.anchorMax = entry.anchor;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(BoxSize, BoxSize);
                rt.anchoredPosition = entry.place - Vector2.Scale(entry.anchor, area);
            }
            Image fill = NewImage(box.transform, "Fill", Brown, out created);
            if (created) Stretch(fill.rectTransform, BoxBorder);
            TMP_Text letter = Letter(box.transform, "Letter", entry.letter, 20f, out created);
            if (created) Stretch(letter.rectTransform, 0f);
            Upright(letter);
        }
    }

    // Border stays square to the screen, so its size and place are simple; only what is inside it turns.
    // Map Area keeps the map's own shape (taller than wide), so the arrow's sums don't change.
    static void TurnMap(RectTransform panel, RectTransform mapArea)
    {
        Undo.RecordObject(panel, UndoName);
        panel.sizeDelta = PanelSize;

        Undo.RecordObject(mapArea, UndoName);
        mapArea.anchorMin = mapArea.anchorMax = mapArea.pivot = new Vector2(0.5f, 0.5f);
        mapArea.anchoredPosition = Vector2.zero;
        mapArea.sizeDelta = new Vector2(PanelSize.y - 2f * BorderWidth, PanelSize.x - 2f * BorderWidth);
        mapArea.localRotation = Quaternion.Euler(0f, 0f, MapRotation);
    }

    static void Upright(TMP_Text label)
    {
        Undo.RecordObject(label.rectTransform, UndoName);
        label.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -MapRotation);
    }

    static BuildingEntry Entry(string name, string letter, Vector3 world, Vector2 area, Vector2 worldMin, Vector2 worldSize)
    {
        Vector2 anchor = Normalized(world, worldMin, worldSize);
        return new BuildingEntry { name = name, letter = letter, anchor = anchor, place = Vector2.Scale(anchor, area) };
    }

    static string BuildingLetter(Building building)
    {
        switch (building.type)
        {
            case Building.BuildingType.Sell: return building.crop.ToString().Substring(0, 1); // W, B, O
            case Building.BuildingType.Buy: return "S";
            case Building.BuildingType.Fertilizer: return "F";
            case Building.BuildingType.Gas: return "G";
            case Building.BuildingType.Dealer: return "D";
            case Building.BuildingType.Paint: return "P";
            default: return "?";
        }
    }

    // spreads out the boxes of buildings that stand close together, and keeps them all inside the map
    static void PushApart(List<BuildingEntry> entries, Vector2 area)
    {
        float reach = BoxSize + BoxGap;
        for (int pass = 0; pass < PushPasses; pass++)
        {
            for (int i = 0; i < entries.Count; i++)
                for (int j = i + 1; j < entries.Count; j++)
                {
                    Vector2 offset = entries[j].place - entries[i].place;
                    float overlapX = reach - Mathf.Abs(offset.x), overlapY = reach - Mathf.Abs(offset.y);
                    if (overlapX <= 0f || overlapY <= 0f) continue;
                    Vector2 push = overlapX < overlapY
                        ? new Vector2((offset.x < 0f ? -overlapX : overlapX) / 2f, 0f)
                        : new Vector2(0f, (offset.y < 0f ? -overlapY : overlapY) / 2f);
                    entries[j].place += push;
                    entries[i].place -= push;
                }

            float half = BoxSize / 2f;
            foreach (var entry in entries)
                entry.place = new Vector2(Mathf.Clamp(entry.place.x, half, area.x - half), Mathf.Clamp(entry.place.y, half, area.y - half));
        }
    }

    static Vector2 Normalized(Vector3 world, Vector2 worldMin, Vector2 worldSize) => new(
        Mathf.Clamp01((world.x - worldMin.x) / worldSize.x),
        Mathf.Clamp01((world.z - worldMin.y) / worldSize.y));

    static void PlaceBounds(RectTransform rt, Bounds bounds, Vector2 worldMin, Vector2 worldSize)
    {
        rt.anchorMin = Normalized(bounds.min, worldMin, worldSize);
        rt.anchorMax = Normalized(bounds.max, worldMin, worldSize);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void Stretch(RectTransform rt, float inset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }

    static void Order(Transform t, int index)
    {
        if (t.GetSiblingIndex() != index) Undo.SetSiblingIndex(t, index, UndoName);
    }

    static RectTransform Child(Transform parent, string name, out bool created)
    {
        Transform existing = parent.Find(name);
        created = existing == null;
        if (!created) return (RectTransform)existing;

        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, UndoName);
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static RectTransform Group(Transform parent, string name)
    {
        RectTransform rt = Child(parent, name, out bool created);
        if (created) Stretch(rt, 0f);
        return rt;
    }

    static Image NewImage(Transform parent, string name, Color color, out bool created)
    {
        RectTransform rt = Child(parent, name, out created);
        var image = rt.GetComponent<Image>();
        if (image != null) return image;

        image = Undo.AddComponent<Image>(rt.gameObject);
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static TMP_Text Letter(Transform parent, string name, string text, float size, out bool created)
    {
        RectTransform rt = Child(parent, name, out created);
        var label = rt.GetComponent<TextMeshProUGUI>();
        if (label != null) return label;

        label = Undo.AddComponent<TextMeshProUGUI>(rt.gameObject);
        label.font = TMP_Settings.defaultFontAsset;
        label.text = text;
        label.fontSize = size;
        label.fontStyle = FontStyles.Bold;
        label.color = Gold;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        return label;
    }
}
