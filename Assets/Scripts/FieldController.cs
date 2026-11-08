using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CropType { None, Wheat, Barley, Oat }

/// <summary>
/// One farm field. Press the interact key inside it to plant the selected crop, wait while it
/// grows through three stages, then press again to harvest. While it grows, the same key (E / USE)
/// fertilizes it: up to two levels, one per growth stage, each +50% harvest (like the 2D game).
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
    // names handed out to the fields in the loaded scene; a field gives its name back when it is destroyed
    private static readonly List<string> usedNames = new();
    private string pickedName; // the name this field took from usedNames (null = it keeps its scene name)

    // Short list for now, add more names later.
    private static readonly string[] AiOwners = { "Fred", "Maria", "Hank", "Dolores", "Gus" };

    // Was 7 s ("growTime"), then 9.45 s ("secondsPerStage"), doubled to 18.9 s ("secondsPerGrowthStage") by the owner
    // on 2026-11-03, then +10% to 20.79 s (about 83 s per crop) by the owner on 2026-11-05.
    // Each time the field was renamed so the value saved in the scene is ignored and this default applies.
    [Tooltip("Seconds each growth step takes (planted > stage 1 > stage 2 > stage 3 > ready). 4 steps in total.")]
    [SerializeField] private float growthStageSeconds = 20.79f;

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

    // 2D game: fertilizing cost 250 to 350 compost per size, against a harvest of 550 to 650 per size,
    // so about 0.42 to 0.58 fertilizer per harvested unit. Two levels, one per growth stage, +50% each.
    [Tooltip("Fertilizing costs this much fertilizer per unit of harvest, each time (random between the two values).")]
    [SerializeField] private float minFertilizerPerYield = 0.42f;
    [SerializeField] private float maxFertilizerPerYield = 0.58f;
    [Tooltip("Extra harvest for each fertilizer level: 0.5 = +50% (2D game).")]
    [SerializeField] private float fertilizerBonusPerLevel = 0.5f;
    private const int MaxFertilizerLevel = 2;

    // AI owners work their own fields (owner, 2026-11-06): they plant an empty field after a while, let it grow
    // (same speed as yours), sometimes fertilize it, and harvest it when ready. A new game starts every AI field
    // at a random point of that, so the map looks alive. Buying a field gets you whatever grows on it.
    [Header("AI owners")]
    [Tooltip("An AI owner plants an empty field after this many seconds (random between the two).")]
    [SerializeField] private float minAiPlantDelay = 15f;
    [SerializeField] private float maxAiPlantDelay = 60f;
    [Tooltip("An AI owner harvests a ready field after this many seconds (random between the two).")]
    [SerializeField] private float minAiHarvestDelay = 10f;
    [SerializeField] private float maxAiHarvestDelay = 45f;
    [Tooltip("Chance an AI owner fertilizes in each growth stage (0 = never, 1 = always; still two levels at most).")]
    [SerializeField, Range(0f, 1f)] private float aiFertilizeChance = 0.25f;

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
    // fertilized fields turn blue like the 2D game (Fert1 / Fert2), until the crop grows into its next stage
    private static readonly Color32[] FertilizedColors = { new(100, 185, 235, 255), new(80, 155, 235, 255) };

    // Materials are shared between fields (one per base material + colour) instead of being created per field.
    private static readonly Dictionary<(Material, int), Material> materialCache = new();

    // Each vehicle remembers the fields it is in (Vehicle.CurrentZone). The last one the driven vehicle
    // entered is the active one, so overlapping fields do not all react to a single key press.

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        usedNames.Clear();
        materialCache.Clear();
    }

    private MeshRenderer meshRenderer;
    private Material baseMaterial;

    private float size;
    private float value;

    private Stage stage = Stage.Empty;
    private CropType cropType = CropType.None;
    private byte fertStage;               // fertilizer levels on the current crop (0 to MaxFertilizerLevel)
    private Stage fertilizedAt = Stage.Empty; // growth stage of the last fertilizing (one per stage)
    private int fertilizerQuote;
    private float growSeconds;            // time spent in the current growth stage (saved, so loading keeps it)
    private float aiWait = -1f;           // seconds until the AI owner plants / harvests (-1 = not waiting)
    private Stage aiFertilizeRolled = Stage.Empty; // the growth stage the AI owner already decided on fertilizing

    void Start()
    {
        if (inputManager == null || playerValues == null || fieldView == null)
        {
            Debug.LogError($"Field '{name}' is missing a reference (inputManager, playerValues or fieldView).", this);
            enabled = false;
            return;
        }

        if (!IsPlayerField)
            fieldName = pickedName = PickAiFieldName();

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
            growSeconds = 0f;
            Changed();
        }
        else if (!IsPlayerField)
        {
            AiRandomStart();
        }
    }

    void Update()
    {
        Grow();
        AiWork();

        if (!IsActiveField()) return;

        // drop an old "press B again" before reading the key, so a stale confirmation can never be used
        if (confirmTrade && Time.time > confirmUntil)
            confirmTrade = false;

        // buying and selling fields is paperwork: every vehicle can do it, the van too (owner, 2026-11-05)
        if (inputManager.Trade)
            Trade();

        // the van is for the road: it cannot plant, harvest or fertilize (owner, 2026-11-03)
        if (Vehicle.Active.kind == VehicleKind.Van)
        {
            if (inputManager.Interact)
                ScreenPrompt.Instance.Error(inputManager.TouchControlsActive
                    ? "The van can't work fields. Switch to the tractor or the harvester (CAR)."
                    : "The van can't work fields. Use the tractor (1) or the harvester (3).", 3f);
            return;
        }

        if (inputManager.Interact)
        {
            if (IsPlayerField) Interact();
            else NotYours();
        }
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
                box.Error($"Not enough money. {fieldName} costs $ {price}", 3f);
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
            GameSounds.Play(GameSounds.Sound.MoneyIn);
            // the crop keeps growing, so you can buy the field back and harvest it
            ownerName = AiOwners[UnityEngine.Random.Range(0, AiOwners.Length)];
            RefreshUI();
            box.Flash($"Sold {fieldName} for $ {price}", 3f);
        }
        else
        {
            if (playerValues.Money < value) { box.Error("Not enough money", 3f); return; }
            playerValues.Money -= value;
            GameSounds.Play(GameSounds.Sound.MoneyOut);
            ownerName = playerName;
            RefreshUI();
            box.Flash($"Bought {fieldName} for $ {price}", 3f);
        }
    }

    private bool IsActiveField()
    {
        return Vehicle.Active != null && Vehicle.Active.CurrentZone<FieldController>() == this;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Vehicle.TryGet(other, out var vehicle)) return;
        vehicle.EnterZone(this);
        if (vehicle.IsActive) RefreshUI();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!Vehicle.TryGet(other, out var vehicle)) return;
        vehicle.ExitZone(this);
        if (vehicle.IsActive) Left();
    }

    private void OnEnable() => Vehicle.ActiveChanged += OnVehicleSwitched;
    private void OnDisable() => Vehicle.ActiveChanged -= OnVehicleSwitched;

    // give the name back, so the next game (Main Menu > New Game again) can use the base names again
    // instead of "Lone Oak 2": usedNames is static, so it outlives the scene
    private void OnDestroy()
    {
        if (pickedName != null) usedNames.Remove(pickedName);
    }

    // the player switched vehicles: show the pop-up for the field the new vehicle is parked in, or hide it
    private void OnVehicleSwitched()
    {
        confirmTrade = false;
        seedQuote = 0;
        fertilizerQuote = 0;
        if (IsActiveField()) RefreshUI();
        else if (Vehicle.Active == null || Vehicle.Active.CurrentZone<FieldController>() == null) fieldView.UpdateUI(false);
    }

    // the driven vehicle left this field: show the field it is still in (overlap), or hide the pop-up
    private void Left()
    {
        confirmTrade = false;
        seedQuote = 0;
        fertilizerQuote = 0;

        var current = Vehicle.Active.CurrentZone<FieldController>();
        if (current != null) current.RefreshUI();
        else fieldView.UpdateUI(false);
    }

    // A random unused name from the 2D game's list. Once all of them are taken (12 AI fields), a random base
    // name gets the next free number ("Dahl Ranch 2", "Dahl Ranch 3", ...), so adding fields never runs out.
    // A base name nobody uses yet; only when all of them are taken (more AI fields than names), a base name
    // with the first free number: "Lone Oak 2", "Lone Oak 3", ...
    private static string PickAiFieldName()
    {
        var free = new List<string>(FieldNames);
        free.RemoveAll(usedNames.Contains);

        string picked;
        if (free.Count > 0)
        {
            picked = free[UnityEngine.Random.Range(0, free.Count)];
        }
        else
        {
            string baseName = FieldNames[UnityEngine.Random.Range(0, FieldNames.Length)];
            int number = 2;
            while (usedNames.Contains($"{baseName} {number}")) number++;
            picked = $"{baseName} {number}";
        }
        usedNames.Add(picked);
        return picked;
    }

    // ---- gameplay ----

    // E / USE on someone else's field: say whose it is and what it costs (no key listed, owner 2026-11-08)
    private void NotYours()
    {
        ScreenPrompt.Instance.Error($"{fieldName} belongs to {ownerName}.\nBuy it ($ {value:N0}) to use it.", 3f);
    }

    private void Interact()
    {
        if (stage == Stage.Empty) Plant();
        else if (stage == Stage.Ready) Harvest();
        // growing: E / USE fertilizes (owner 2026-11-06: no F key, no FERT touch button)
        else Fertilize();
    }

    private void Plant()
    {
        // the price is rolled once and kept until you plant or leave the field, so a failed attempt
        // shows the same number every time (and a tiny field can never plant for free)
        if (seedQuote <= 0)
            seedQuote = Mathf.Max(1, Mathf.RoundToInt(size * UnityEngine.Random.Range(minSeedsPerYield, maxSeedsPerYield)));
        if (playerValues.Seeds < seedQuote)
        {
            ScreenPrompt.Instance.Error($"Not enough seeds. Planting {fieldName} needs {seedQuote:N0}, you have {playerValues.Seeds:N0}", 3f);
            return;
        }
        playerValues.Seeds -= seedQuote;
        int used = seedQuote;
        seedQuote = 0;

        // plant whatever the player picked with the C key
        cropType = playerValues.SelectedCrop;
        stage = Stage.Planted;
        growSeconds = 0f;

        GameSounds.Play(GameSounds.Sound.Planting);
        Changed();
        ScreenPrompt.Instance.Flash($"Planted {CropName()} on {fieldName}. Used {used:N0} seeds.", 3f);
    }

    // E / USE while growing: one fertilizer level per growth stage, up to two. Costs fertilizer from the Statistics panel.
    private void Fertilize()
    {
        var box = ScreenPrompt.Instance;
        if (!IsPlayerField) { NotYours(); return; }
        if (stage == Stage.Empty) { box.Error("Plant the field first, then fertilize it while it grows.", 3f); return; }
        if (stage == Stage.Ready) { box.Error("The crop is ready. Harvest it first.", 3f); return; }
        if (fertStage >= MaxFertilizerLevel)
        {
            box.Error($"{fieldName} is fully fertilized ({MaxFertilizerLevel}/{MaxFertilizerLevel}).", 3f);
            return;
        }
        if (fertilizedAt == stage)
        {
            box.Error("Already fertilized in this growth stage. Wait for the next stage.", 3f);
            return;
        }

        // rolled once and kept until you fertilize or leave the field, like the seed price
        if (fertilizerQuote <= 0)
            fertilizerQuote = Mathf.Max(1, Mathf.RoundToInt(size * UnityEngine.Random.Range(minFertilizerPerYield, maxFertilizerPerYield)));
        if (playerValues.Fertilizer < fertilizerQuote)
        {
            box.Error($"Not enough fertilizer. {fieldName} needs {fertilizerQuote:N0}, you have {playerValues.Fertilizer:N0}.\nBuy more at the fertilizer shop.", 3f);
            return;
        }

        playerValues.Fertilizer -= fertilizerQuote;
        int used = fertilizerQuote;
        fertilizerQuote = 0;
        fertStage++;
        fertilizedAt = stage;
        GameSounds.Play(GameSounds.Sound.Fertilizing);
        box.Flash($"Fertilized {fieldName} ({fertStage}/{MaxFertilizerLevel}): +{fertStage * fertilizerBonusPerLevel * 100f:0}% harvest. Used {used:N0} fertilizer.", 3f);
        Changed();
    }

    // planted > stage 1 > stage 2 > stage 3 > ready, one step every growthStageSeconds (game time: stops while paused)
    private void Grow()
    {
        if (stage < Stage.Planted || stage >= Stage.Ready) return;

        growSeconds += Time.deltaTime;
        if (growSeconds < growthStageSeconds) return;

        growSeconds = 0f;
        stage++;
        Changed();
    }

    // ---- AI owners ----

    private static CropType RandomCrop() => (CropType)UnityEngine.Random.Range((int)CropType.Wheat, (int)CropType.Oat + 1);

    // a new game: the field is somewhere in the AI owner's year (empty, growing or ready), with a random crop
    private void AiRandomStart()
    {
        stage = (Stage)UnityEngine.Random.Range((int)Stage.Empty, (int)Stage.Ready + 1);
        cropType = stage == Stage.Empty ? CropType.None : RandomCrop();
        growSeconds = stage is > Stage.Empty and < Stage.Ready ? UnityEngine.Random.Range(0f, growthStageSeconds) : 0f;
        fertStage = 0;
        fertilizedAt = Stage.Empty;
        // fertilized in an earlier stage sometimes (an earlier stage: the field does not start blue)
        if (stage >= Stage.Growing1 && UnityEngine.Random.value < aiFertilizeChance)
        {
            fertStage = 1;
            fertilizedAt = Stage.Planted;
        }
        aiFertilizeRolled = stage; // no extra roll for the stage it starts in
        aiWait = -1f;
        Changed();
    }

    // the AI owner plants an empty field and harvests a ready one, each after a random wait (game time,
    // so it stops while paused); while it grows, it may fertilize once per stage
    private void AiWork()
    {
        if (IsPlayerField || ownerName == null) { aiWait = -1f; return; }

        if (stage == Stage.Empty || stage == Stage.Ready)
        {
            if (aiWait < 0f)
                aiWait = stage == Stage.Empty
                    ? UnityEngine.Random.Range(minAiPlantDelay, maxAiPlantDelay)
                    : UnityEngine.Random.Range(minAiHarvestDelay, maxAiHarvestDelay);
            aiWait -= Time.deltaTime;
            if (aiWait > 0f) return;
            aiWait = -1f;
            if (stage == Stage.Empty) AiPlant();
            else ClearCrop(); // the AI owner's harvest goes to the AI owner, not to your counters
            return;
        }

        aiWait = -1f;
        if (stage == aiFertilizeRolled) return;
        aiFertilizeRolled = stage;
        if (fertStage < MaxFertilizerLevel && fertilizedAt != stage && UnityEngine.Random.value < aiFertilizeChance)
        {
            fertStage++;
            fertilizedAt = stage;
            Changed();
        }
    }

    private void AiPlant()
    {
        cropType = RandomCrop();
        stage = Stage.Planted;
        growSeconds = 0f;
        fertStage = 0;
        fertilizedAt = Stage.Empty;
        Changed();
    }

    // back to an empty field (after a harvest)
    private void ClearCrop()
    {
        stage = Stage.Empty;
        cropType = CropType.None;
        fertStage = 0;
        fertilizedAt = Stage.Empty;
        growSeconds = 0f;
        Changed();
    }

    private void Harvest()
    {
        // the harvester brings in more (2D game: +60%), and each fertilizer level +50%
        var vehicle = Vehicle.Active;
        float vehicleBonus = vehicle != null ? vehicle.harvestBonus : 0f;
        float fertilizerBonus = fertStage * fertilizerBonusPerLevel;
        int amount = Convert.ToInt32(size * (1f + vehicleBonus + fertilizerBonus));
        if (vehicleBonus > 0f || fertilizerBonus > 0f)
        {
            var parts = new List<string>();
            if (vehicleBonus > 0f) parts.Add($"+{vehicleBonus * 100f:0}% with the {vehicle.displayName}");
            if (fertilizerBonus > 0f) parts.Add($"+{fertilizerBonus * 100f:0}% from fertilizer");
            ScreenPrompt.Instance.Flash($"Harvested {amount:N0} {cropType} ({string.Join(", ", parts)})", 3f);
        }

        if (cropType == CropType.Wheat) playerValues.InWheat += amount;
        else if (cropType == CropType.Barley) playerValues.InBarley += amount;
        else if (cropType == CropType.Oat) playerValues.InOat += amount;

        GameSounds.Play(GameSounds.Sound.Harvesting);
        ClearCrop();
    }

    // ---- save / load (SaveGame.cs) ----

    /// <summary>This field's state for the save file.</summary>
    public FieldSave GetSave(string id)
    {
        return new FieldSave
        {
            id = id,
            name = fieldName,
            owner = ownerName,
            crop = cropType,
            stage = (int)stage,
            growSeconds = growSeconds,
            fertilizer = fertStage,
            fertilizedAt = (int)fertilizedAt,
        };
    }

    /// <summary>Puts the saved state back (after Start, which gave the field a new-game name, owner and crop).</summary>
    public void ApplySave(FieldSave save)
    {
        if (save == null) return;

        if (!string.IsNullOrEmpty(save.name) && save.name != fieldName)
        {
            // an AI field: swap the random name it took this game for the saved one, so new names stay unique
            if (pickedName != null)
            {
                usedNames.Remove(pickedName);
                usedNames.Add(save.name);
                pickedName = save.name;
            }
            fieldName = save.name;
        }
        if (!string.IsNullOrEmpty(save.owner)) ownerName = save.owner;

        stage = (Stage)Mathf.Clamp(save.stage, (int)Stage.Empty, (int)Stage.Ready);
        cropType = save.crop >= CropType.Wheat && save.crop <= CropType.Oat ? save.crop : CropType.None;
        if (cropType == CropType.None) stage = Stage.Empty;
        if (stage == Stage.Empty) cropType = CropType.None;

        growSeconds = Mathf.Clamp(save.growSeconds, 0f, growthStageSeconds);
        fertStage = (byte)Mathf.Clamp(stage == Stage.Empty ? 0 : save.fertilizer, 0, MaxFertilizerLevel);
        fertilizedAt = fertStage == 0 ? Stage.Empty : (Stage)Mathf.Clamp(save.fertilizedAt, (int)Stage.Empty, (int)Stage.Ready);

        seedQuote = 0;
        fertilizerQuote = 0;
        confirmTrade = false;
        aiWait = -1f;              // an AI owner's wait starts again (not saved)
        aiFertilizeRolled = stage; // and it does not roll again for the stage it was saved in
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
        // fertilized in this stage: level 1 light blue, level 2 deeper blue (2D game); the next stage shows the crop again
        if (fertStage > 0 && fertilizedAt == stage) return FertilizedColors[Mathf.Min(fertStage, FertilizedColors.Length) - 1];
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
        return fertStage == 0 ? "No" : $"+{fertStage * fertilizerBonusPerLevel * 100f:0}%";
    }
}
