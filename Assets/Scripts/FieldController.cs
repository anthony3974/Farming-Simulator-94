using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One farm field. Press the interact key inside it to plant a random crop, wait while it
/// grows through three stages, then press again to harvest.
/// </summary>
public class FieldController : MonoBehaviour
{
    public InputManager inputManager;
    public PlayerValues playerValues;
    public FieldView fieldView;

    public string fieldName;
    public string ownerName;

    [Tooltip("Fields owned by this name belong to the player and can be used. All other fields get a random AI owner.")]
    [SerializeField] private string playerName = "Anthony";

    // Short list for now, add more names later.
    private static readonly string[] AiOwners = { "Fred", "Maria", "Hank", "Dolores", "Gus" };

    [Tooltip("Seconds each growth step takes (planted > stage 1 > stage 2 > stage 3 > ready).")]
    [SerializeField] private float growTime = 7f;

    [Tooltip("Field area is scale.x * scale.z divided by this number.")]
    [SerializeField] private float sizeDivisor = 0.043029259f;

    private enum CropType { None, Wheat, Barley, Oat }

    private enum Stage { Empty, Planted, Growing1, Growing2, Growing3, Ready }

    // Colours for each crop's three growth stages and its ready state.
    // Index with (CropType - 1) and then (Stage - Growing1).
    private static readonly Color32[][] CropColors =
    {
        // Wheat
        new Color32[] { new(195, 215, 50, 255), new(215, 195, 50, 255), new(235, 200, 5, 255), new(255, 190, 0, 255) },
        // Barley
        new Color32[] { new(170, 180, 35, 255), new(210, 195, 30, 255), new(190, 140, 30, 255), new(150, 110, 25, 255) },
        // Oat
        new Color32[] { new(195, 200, 35, 255), new(200, 180, 25, 255), new(215, 160, 50, 255), new(230, 130, 40, 255) },
    };

    private static readonly Color32 DirtColor = new(130, 90, 60, 255);
    private static readonly Color32 PlantedColor = new(50, 230, 30, 255);

    // Materials are shared between fields (one per base material + colour) instead of being created per field.
    private static readonly Dictionary<(Material, int), Material> materialCache = new();

    // Fields the player is currently standing in. The last one entered is the active one,
    // so overlapping fields no longer all react to a single key press.
    private static readonly List<FieldController> fieldsInside = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        fieldsInside.Clear();
        materialCache.Clear();
    }

    private MeshRenderer meshRenderer;
    private Material baseMaterial;

    private float size;
    private float value;

    private Stage stage = Stage.Empty;
    private CropType cropType = CropType.None;
    private CropType lastCropType = CropType.None;
    private byte fertStage;

    private readonly System.Random rn = new();

    void Start()
    {
        if (inputManager == null || playerValues == null || fieldView == null)
        {
            Debug.LogError($"Field '{name}' is missing a reference (inputManager, playerValues or fieldView).", this);
            enabled = false;
            return;
        }

        if (ownerName != playerName)
            ownerName = AiOwners[UnityEngine.Random.Range(0, AiOwners.Length)];

        meshRenderer = GetComponent<MeshRenderer>();
        baseMaterial = meshRenderer.sharedMaterial;

        size = transform.localScale.x * transform.localScale.z / sizeDivisor;
        value = size * 0.5f;
    }

    void Update()
    {
        if (inputManager.Interact && IsActiveField() && ownerName == playerName)
            Interact();
    }

    private bool IsActiveField()
    {
        return fieldsInside.Count > 0 && fieldsInside[^1] == this;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            fieldsInside.Remove(this);
            fieldsInside.Add(this);
            RefreshUI();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            fieldsInside.Remove(this);

            if (fieldsInside.Count > 0) fieldsInside[^1].RefreshUI();
            else fieldView.UpdateUI(false);
        }
    }

    private void OnDisable()
    {
        fieldsInside.Remove(this);
    }

    // ---- gameplay ----

    private void Interact()
    {
        if (stage == Stage.Empty) Plant();
        else if (stage == Stage.Ready) Harvest();
        // otherwise the crop is still growing, nothing to do
    }

    private void Plant()
    {
        // pick a random crop, never the same one twice in a row on this field
        CropType picked;
        do picked = (CropType)(rn.Next(3) + 1);
        while (picked == lastCropType);

        cropType = picked;
        lastCropType = picked;
        stage = Stage.Planted;

        StartCoroutine(Grow());
        Changed();
    }

    private IEnumerator Grow()
    {
        yield return new WaitForSeconds(growTime);

        stage++;
        if (stage < Stage.Ready) StartCoroutine(Grow());
        Changed();
    }

    private void Harvest()
    {
        int amount = Convert.ToInt32(size);

        if (cropType == CropType.Wheat) playerValues.InWheat += amount;
        else if (cropType == CropType.Barley) playerValues.InBarley += amount;
        else if (cropType == CropType.Oat) playerValues.InOat += amount;

        stage = Stage.Empty;
        cropType = CropType.None;
        Changed();
    }

    // ---- visuals ----

    private void Changed()
    {
        meshRenderer.sharedMaterial = GetMaterial(CurrentColor());

        // keep the pop-up in sync while the player is standing in the field
        if (IsActiveField()) RefreshUI();
    }

    private Color32 CurrentColor()
    {
        if (stage == Stage.Empty) return DirtColor;
        if (stage == Stage.Planted) return PlantedColor;
        return CropColors[(int)cropType - 1][stage - Stage.Growing1];
    }

    private Material GetMaterial(Color32 color)
    {
        int key = (color.r << 16) | (color.g << 8) | color.b;

        if (!materialCache.TryGetValue((baseMaterial, key), out Material mat) || mat == null)
        {
            mat = new Material(baseMaterial) { color = color };
            materialCache[(baseMaterial, key)] = mat;
        }
        return mat;
    }

    // ---- pop-up text ----

    private void RefreshUI()
    {
        fieldView.UpdateUI(true, fieldName, ownerName, value.ToString("N0"), size.ToString("N0"), CropName(), StatusName(), FertilizerName());
    }

    private string CropName()
    {
        return cropType == CropType.None ? "None" : cropType.ToString();
    }

    private string StatusName()
    {
        switch (stage)
        {
            case Stage.Empty: return "Empty";
            case Stage.Planted: return "Planted";
            case Stage.Ready: return "Ready!";
            default: return $"Growing {stage - Stage.Planted}/3";
        }
    }

    private string FertilizerName()
    {
        return fertStage == 0 ? "No" : $"Stage {fertStage}";
    }
}
