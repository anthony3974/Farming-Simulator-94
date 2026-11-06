using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerValues : MonoBehaviour
{
    private int inWheat;
    public TMP_Text txWheat;
    private int inBarley;
    public TMP_Text txBarley;
    private int inOat;
    public TMP_Text txOat;
    private float money;
    [Tooltip("Optional. Shows the money in the counters panel.")]
    public TMP_Text txMoney;

    private int seeds;
    [Tooltip("Seeds you start the game with. Planting the 23,000-size starter field costs about 17,000 to 21,000.")]
    [SerializeField] private int startSeeds = 25000;
    [Tooltip("Money you start the game with (2D game: 5,000).")]
    [SerializeField] private float startMoney = 5000f;
    [Tooltip("Optional. Shows the seeds in the Statistics panel.")]
    public TMP_Text txSeeds;
    private int fertilizer;
    [Tooltip("Fertilizer you start the game with. Buy more at the fertilizer shop.")]
    [SerializeField] private int startFertilizer = 0;
    [Tooltip("Optional. Shows the fertilizer in the Statistics panel.")]
    public TMP_Text txFertilizer;

    // Diesel: one tank shared by every vehicle, like the 2D game (tank of 100, 2 dollars per unit at Jerrys Gas Station).
    [Header("Fuel (shared by all vehicles)")]
    [Tooltip("Size of the tank, in litres. Shared by the tractor, van and harvester.")]
    [SerializeField] private float fuelCapacity = 100f;
    [Tooltip("Diesel you start the game with (2D game: 100, a full tank).")]
    [SerializeField] private float startFuel = 100f;
    [Tooltip("At or below this part of the tank a 'Low fuel' message shows once (0.2 = 20%).")]
    [Range(0f, 1f)] [SerializeField] private float lowFuelWarning = 0.2f;
    private float fuel;
    private bool lowFuelWarned, emptyWarned;

    private InputManager inputManager;

    /// <summary>The crop planted in your fields. Change it with the C key.</summary>
    public CropType SelectedCrop { get; private set; } = CropType.Wheat;

    /// <summary>Sets the crop planted next (used when loading a save).</summary>
    public void SelectCrop(CropType crop)
    {
        if (crop >= CropType.Wheat && crop <= CropType.Oat) SelectedCrop = crop;
    }

    void Awake()
    {
        // saving and loading (SaveGame.cs) lives on this object; add it if it was not added in the editor
        if (GetComponent<SaveGame>() == null && FindFirstObjectByType<SaveGame>() == null) gameObject.AddComponent<SaveGame>();
    }

    public void Start()
    {
        inputManager = GetComponent<InputManager>();
        if (inputManager == null) inputManager = FindFirstObjectByType<InputManager>();
        seeds = startSeeds;
        money = startMoney;
        fertilizer = startFertilizer;
        fuel = Mathf.Clamp(startFuel, 0f, fuelCapacity);
        UpdateUI();
    }

    void Update()
    {
        if (inputManager != null && inputManager.CycleCrop)
        {
            SelectedCrop = SelectedCrop == CropType.Oat ? CropType.Wheat : SelectedCrop + 1;
            ScreenPrompt.Instance.Flash($"Selected crop: {SelectedCrop}", 2f);
        }
    }
    private void UpdateUI()
    {
        txWheat.text = $"{inWheat:N0}";
        txBarley.text = $"{inBarley:N0}";
        txOat.text = $"{inOat:N0}";
        if (txMoney != null) txMoney.text = $"$ {money:N2}";
        if (txSeeds != null) txSeeds.text = $"{seeds:N0}";
        if (txFertilizer != null) txFertilizer.text = $"{fertilizer:N0}";
    }

    public int InWheat { get => inWheat; set { inWheat = value; UpdateUI(); } }
    public int InBarley { get => inBarley; set { inBarley = value; UpdateUI(); } }
    public float Money { get => money; set { money = value; UpdateUI(); } }
    public int Seeds { get => seeds; set { seeds = value; UpdateUI(); } }
    public int Fertilizer { get => fertilizer; set { fertilizer = value; UpdateUI(); } }
    public int InOat { get => inOat; set { inOat = value; UpdateUI(); } }

    // ---- fuel ----

    /// <summary>The first PlayerValues in the scene (the vehicles use it to burn fuel).</summary>
    public static PlayerValues Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<PlayerValues>();
            return instance;
        }
    }
    private static PlayerValues instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    public float FuelCapacity => fuelCapacity;
    public bool FuelEmpty => fuel <= 0f;
    /// <summary>At or below the "Low fuel" warning level.</summary>
    public bool FuelLow => fuel <= lowFuelWarning * fuelCapacity;

    /// <summary>Diesel in the tank (litres), kept between 0 and the capacity. Shared by every vehicle.</summary>
    public float Fuel
    {
        get => fuel;
        set
        {
            fuel = Mathf.Clamp(value, 0f, fuelCapacity);
            FuelWarnings();
        }
    }

    /// <summary>Burns fuel (from driving). Shows the low and empty messages once each time the level drops past them.</summary>
    public void UseFuel(float litres)
    {
        if (litres > 0f) Fuel = fuel - litres;
    }

    private void FuelWarnings()
    {
        float low = lowFuelWarning * fuelCapacity;
        if (fuel > low) { lowFuelWarned = emptyWarned = false; return; }
        if (fuel > 0f) emptyWarned = false;

        var prompt = ScreenPrompt.Instance;
        if (fuel <= 0f && !emptyWarned)
        {
            emptyWarned = lowFuelWarned = true;
            if (prompt != null) prompt.Flash("Out of fuel! You can only crawl.\nFill up at Jerrys Gas Station (bottom of the map)", 5f);
        }
        else if (!lowFuelWarned)
        {
            lowFuelWarned = true;
            if (prompt != null) prompt.Flash("Low fuel! Fill up at Jerrys Gas Station (bottom of the map)", 4f);
        }
    }
}
