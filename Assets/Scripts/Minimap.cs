using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The small map on the right of the screen (PLAN: minimap). A real object in the scene hierarchy ("Minimap", made by
/// Tools > Build Minimap): the roads, fields and buildings are plain UI images, north up, so they can be moved and
/// recoloured in the editor. There is no camera and no RenderTexture. This script only:
///  - shows or hides the map with <see cref="GameSettings.Minimap"/> (SETTINGS > GAME), also while a game is running;
///    when off, the map is hidden and this script does no per-frame work,
///  - moves and turns the arrow of the driven vehicle (<see cref="Vehicle.Active"/>),
///  - every few seconds copies each field's colour from the field itself and gives your own fields a gold edge.
/// It has no GraphicRaycaster and every image has Raycast Target off, so touches go through it.
/// </summary>
public class Minimap : MonoBehaviour
{
    [Serializable]
    public class FieldMark
    {
        public FieldController field;
        [Tooltip("The outline: gold on your own fields.")]
        public Image frame;
        [Tooltip("The inside: takes the colour of the field in the world.")]
        public Image fill;

        [NonSerialized] public Renderer worldRenderer;
    }

    [Header("Scene objects (set by the builder; keep them set if you rearrange)")]
    [Tooltip("The whole map (the yellow border and everything in it). Hidden while the minimap is off.")]
    public RectTransform panel;
    [Tooltip("The inside of the map; the arrow moves over this.")]
    public RectTransform mapArea;
    [Tooltip("The arrow of the driven vehicle. Anchored to the bottom left of Map Area.")]
    public RectTransform vehicleMarker;
    public List<FieldMark> fields = new();

    [Header("World")]
    [Tooltip("The world X and Z of the bottom left corner of the map.")]
    public Vector2 worldMin;
    [Tooltip("How much world X and Z the map shows.")]
    public Vector2 worldSize;

    [Header("Place on the screen (Anchored Position of the panel)")]
    [Tooltip("Without touch controls: at the right edge.")]
    public Vector2 pcPosition = new(-20f, 0f);
    [Tooltip("With touch controls: 10 left of the column of buttons (USE, CROP, GAS...), which fills the right edge.")]
    public Vector2 touchPosition = new(-340f, 0f);

    [Header("Fields")]
    [Tooltip("Seconds between updates of the field colours and owners.")]
    public float fieldRefreshSeconds = 2f;
    [Tooltip("Outline of your own fields.")]
    public Color ownedFrameColor = new Color32(255, 204, 0, 255);
    [Tooltip("Outline of the other fields.")]
    public Color otherFrameColor = new Color32(25, 18, 8, 255);

    private float nextFieldRefresh;
    private bool markerPlaced;
    private Vector2 lastPlace;
    private float lastYaw;

    void Start()
    {
        if (panel == null || mapArea == null || vehicleMarker == null || worldSize.x <= 0f || worldSize.y <= 0f)
        {
            Debug.LogError($"Minimap turned off: '{name}' is missing Panel, Map Area, Vehicle Marker or World Size. Run Tools > Build Minimap again.", this);
            if (panel != null) panel.gameObject.SetActive(false);
            enabled = false;
            return;
        }

        var input = FindFirstObjectByType<InputManager>();
        panel.anchoredPosition = input != null && input.TouchControlsActive ? touchPosition : pcPosition;

        foreach (var mark in fields)
            if (mark.field != null) mark.worldRenderer = mark.field.GetComponent<Renderer>();

        GameSettings.Changed += Apply;
        Apply();
    }

    void OnDestroy() => GameSettings.Changed -= Apply;

    private void Apply()
    {
        bool on = GameSettings.Minimap;
        if (on == enabled && on == panel.gameObject.activeSelf) return; // another setting changed
        panel.gameObject.SetActive(on);
        enabled = on;
        nextFieldRefresh = 0f; // the fields may have changed while the map was off
    }

    // LateUpdate: after the vehicle has moved, and after every field's Start on the first frame
    void LateUpdate()
    {
        MoveMarker();

        if (Time.unscaledTime < nextFieldRefresh) return;
        nextFieldRefresh = Time.unscaledTime + fieldRefreshSeconds;
        RefreshFields();
    }

    private void MoveMarker()
    {
        Vehicle vehicle = Vehicle.Active;
        bool show = vehicle != null;
        if (vehicleMarker.gameObject.activeSelf != show) vehicleMarker.gameObject.SetActive(show);
        if (!show) return;

        Transform t = vehicle.transform;
        Vector3 position = t.position;
        Vector3 forward = t.forward;
        Vector2 area = mapArea.rect.size;
        var place = new Vector2(
            Mathf.Clamp01((position.x - worldMin.x) / worldSize.x) * area.x,
            Mathf.Clamp01((position.z - worldMin.y) / worldSize.y) * area.y);
        float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg; // 0 = north (+Z), clockwise

        // a parked vehicle changes nothing, so the canvas is not redrawn for it
        if (!markerPlaced || (place - lastPlace).sqrMagnitude > 0.01f)
        {
            vehicleMarker.anchoredPosition = place;
            lastPlace = place;
        }
        if (!markerPlaced || Mathf.Abs(Mathf.DeltaAngle(yaw, lastYaw)) > 0.5f)
        {
            vehicleMarker.localRotation = Quaternion.Euler(0f, 0f, -yaw);
            lastYaw = yaw;
        }
        markerPlaced = true;
    }

    private void RefreshFields()
    {
        foreach (var mark in fields)
        {
            if (mark.field == null) continue;
            if (mark.frame != null) mark.frame.color = mark.field.IsPlayerField ? ownedFrameColor : otherFrameColor;
            if (mark.fill == null || mark.worldRenderer == null) continue;
            Material material = mark.worldRenderer.sharedMaterial;
            if (material == null) continue;
            Color color = material.color;
            color.a = 1f;
            mark.fill.color = color;
        }
    }
}
