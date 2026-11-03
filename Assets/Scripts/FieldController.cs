using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CropType { None, Wheat, Barley, Oat }

/// <summary>
/// One farm field. Press the interact key inside it to plant the selected crop, wait while it
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

    // Field names taken from the 2D game. Fields not owned by the player get a random unused one.
    private static readonly string[] FieldNames =
    {
        "Dahl Ranch", "Alpine Farming", "John Deer", "Erlengrat", "Welker Farms", "Goldcrest Valley",
        "Lone Oak", "Estancia Lapacho", "Felsbrunn", "Minibrunn", "No Man's Land", "Ravenport",
    };
    private static readonly List<string> usedNames = new();

    // Short list for now, add more names later.
    private static readonly string[] AiOwners = { "Fred", "Maria", "Hank", "Dolores", "Gus" };

    // Was 7 s ("growTime", saved in the scene). Raised 35% to 9.45 s. The field was renamed so the old
    // saved 7 is ignored and this default applies to every field.
    [Tooltip("Seconds each growth step takes (planted > stage 1 > stage 2 > stage 3 > ready). 4 steps in total.")]
    [SerializeField] private float secondsPerStage = 9.45f;

    [Tooltip("The player's own field starts the game planted with this crop (the Planted stage, just like after pressing E). None = starts empty.")]
    [SerializeField] private CropType startCrop = CropType.Wheat;

    [Tooltip("Field area is scale.x * scale.z divided by this number.")]
    [SerializeField] private float sizeDivisor = 0.043029259f;

    // Economy, in units of one harvest (the Size shown in the pop-up is also the harvest amount).
    // 2D game ratios: planting 450 to 550 seeds per size and harvest 550 to 650 per size, so about 0.83 seeds
    // per harvested unit. Land cost 1,250 per size, so about 2.1 dollars per harvested unit.
    [Header("Economy")]
    [Tooltip("Planting costs this many seeds per unit of harvest (random between the two values, 2D ratio about 0.83).")]
    [SerializeField] private float minSeedsPerYield = 0.75f;
    [SerializeField] private float maxSeedsPerYield = 0.9f;

    [Tooltip("Buy price of the field = size x this (2D ratio about 2.1). Selling it back pays Sell Back Fraction of this.")]
    [SerializeField] private float landPricePerYield = 2.1f;

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
        usedNames.Clear();
        materialCache.Clear();
    }

    private MeshRenderer meshRenderer;
    private Material baseMaterial;

    private float size;
    private float value;

    private Stage stage = Stage.Empty;
    private CropType cropType = CropType.None;
    private byte fertStage;

    void Start()
    {
        if (inputManager == null || playerValues == null || fieldView == null)
        {
            Debug.LogError($"Field '{name}' is missing a reference (inputManager, playerValues or fieldView).", this);
            enabled = false;
            return;
        }

        if (!IsPlayerField)
        {
            var free = new List<string>(FieldNames);
            free.RemoveAll(usedNames.Contains);
            if (free.Count > 0)
            {
                fieldName = free[UnityEngine.Random.Range(0, free.Count)];
                usedNames.Add(fieldName);
            }
        }

        if (!IsPlayerField)
            ownerName = AiOwners[UnityEngine.Random.Range(0, AiOwners.Length)];

        meshRenderer = GetComponent<MeshRenderer>();
        baseMaterial = meshRenderer.sharedMaterial;

        size = transform.localScale.x * transform.localScale.z / sizeDivisor;
        value = size * landPricePerYield;

        // the player's field starts already growing (free, no seeds)
        if (IsPlayerField && startCrop != CropType.None)
        {
            cropType = startCrop;
            stage = Stage.Planted;
            StartCoroutine(Grow());
            Changed();
        }
    }

    void Update()
    {
        if (!IsActiveField()) return;

        // drop an old "press B again" before reading the key, so a stale confirmation can never be used
        if (confirmTrade && Time.time > confirmUntil)
            confirmTrade = false;

        if (inputManager.Interact && IsPlayerField)
            Interact();

        if (inputManager.Trade)
            Trade();
    }

    [Tooltip("Selling a field pays this fraction of its value (buy at 100%, sell back at 80%).")]
    [SerializeField, Range(0f, 1f)] private float sellBackFraction = 0.8f;

    private bool IsPlayerField => ownerName == playerName;

    // B key: first press asks, second press (within a few seconds) confirms.
    private int seedQuote;
    private bool confirmTrade;
    private float confirmUntil;
    private const float ConfirmSeconds = 4f;

    private void Trade()
    {
        var box = ScreenPrompt.Instance;
        float sellPrice = value * sellBackFraction;
        string price = (IsPlayerField ? sellPrice : value).ToString("N0");

        if (!confirmTrade)
        {
            if (!IsPlayerField && playerValues.Money < value)
            {
                box.Flash($"Not enough money. {fieldName} costs $ {price}", 3f);
                return;
            }

            confirmTrade = true;
            confirmUntil = Time.time + ConfirmSeconds;
            string question = IsPlayerField
                ? $"Sell {fieldName} for $ {price}?" + (stage != Stage.Empty ? " (the crop stays on the field)" : "")
                : $"Buy {fieldName} from {ownerName} for $ {price}?";
            box.Flash($"{question}\nPress again to confirm", ConfirmSeconds);
            return;
        }

        confirmTrade = false;

        if (IsPlayerField)
        {
            playerValues.Money += sellPrice;
            // the crop keeps growing, so you can buy the field back and harvest it
            ownerName = AiOwners[UnityEngine.Random.Range(0, AiOwners.Length)];
            RefreshUI();
            box.Flash($"Sold {fieldName} for $ {price}", 3f);
        }
        else
        {
            if (playerValues.Money < value) { box.Flash("Not enough money", 3f); return; }
            playerValues.Money -= value;
            ownerName = playerName;
            RefreshUI();
            box.Flash($"Bought {fieldName} for $ {price}", 3f);
        }
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
            confirmTrade = false;
            seedQuote = 0;

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
        // the price is rolled once and kept until you plant or leave the field, so a failed attempt
        // shows the same number every time (and a tiny field can never plant for free)
        if (seedQuote <= 0)
            seedQuote = Mathf.Max(1, Mathf.RoundToInt(size * UnityEngine.Random.Range(minSeedsPerYield, maxSeedsPerYield)));
        if (playerValues.Seeds < seedQuote)
        {
            ScreenPrompt.Instance.Flash($"Not enough seeds. Planting {fieldName} needs {seedQuote:N0}, you have {playerValues.Seeds:N0}", 3f);
            return;
        }
        playerValues.Seeds -= seedQuote;
        seedQuote = 0;

        // plant whatever the player picked with the C key
        cropType = playerValues.SelectedCrop;
        stage = Stage.Planted;

        StartCoroutine(Grow());
        Changed();
    }

    private IEnumerator Grow()
    {
        yield return new WaitForSeconds(secondsPerStage);

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
