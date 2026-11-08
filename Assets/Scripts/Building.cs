using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// A building with a trigger zone. Press the interact key (E / USE) inside the zone to run
/// the building's action. Same pattern as FieldController: only the last zone the driven vehicle
/// entered reacts.
/// </summary>
public class Building : MonoBehaviour
{
    // the scene stores these as numbers: add new types at the end only
    public enum BuildingType { Sell, Buy, Paint, Dealer, Gas, Fertilizer }
    public enum Crop { Wheat, Barley, Oat }

    public InputManager inputManager;
    public PlayerValues playerValues;

    public BuildingType type = BuildingType.Sell;
    [Tooltip("Which crop this station buys (Sell type only).")]
    public Crop crop = Crop.Wheat;

    [Tooltip("Price per unit is random between these two values on each sale (2D game: 0.65 to 0.75).")]
    public float minPrice = 0.65f;
    public float maxPrice = 0.75f;

    // Owner, 2026-11-03: both shops sell 2,000 at a time, at the same price per unit as before. The fields were
    // renamed (from seedsPerPurchase, minSeedCost, ...) so the old values saved in the scene are ignored and
    // these defaults apply to every shop.
    [Header("Seed shop (Buy type only)")]
    [Tooltip("Seeds you get per purchase. The 2D game sold 1,000 for 450 to 550 dollars (about 0.5 dollars per seed).")]
    public int seedsPerBuy = 2000;
    [Tooltip("Cost of one purchase is random between these two values (about 0.5 dollars per seed).")]
    public float minSeedPrice = 900f;
    public float maxSeedPrice = 1100f;

    [Header("Fertilizer shop (Fertilizer type only)")]
    [Tooltip("Fertilizer you get per purchase. The 2D game sold 1,000 compost for 400 to 500 dollars (about 0.45 dollars per unit).")]
    public int fertilizerPerBuy = 2000;
    [Tooltip("Cost of one purchase is random between these two values (about 0.45 dollars per unit).")]
    public float minFertilizerPrice = 800f;
    public float maxFertilizerPrice = 1000f;

    [Header("Gas station (Gas type only)")]
    [Tooltip("Dollars per litre of diesel (2D game: 2).")]
    public float fuelPrice = 2f;
    [Tooltip("Litres pumped per second. E / USE starts filling the shared tank slowly, like the 2D game; E / USE again stops. " +
             "It also stops when the tank is full, the money runs out, or the vehicle drives off.")]
    public float fuelPerSecond = 4f;
    [Tooltip("The pump displays: they show the price, and the litres while filling. Empty = every text named " +
             "'Display Numbers' under the gas station (the Gas Station Builder's pump displays).")]
    public List<TMP_Text> pumpDisplays = new();
    [Tooltip("What the pump displays show when not filling. {0} = the price per litre.")]
    public string pumpPriceFormat = "$ {0:0.00} / L";

    [Header("Dealer (Dealer type only)")]
    [Tooltip("The dealer that opens its shop menu here. Empty = the Dealer on this object or a parent.")]
    public Dealer dealer;

    [Tooltip("Optional. Text object used for the short sale message.")]
    public TMP_Text messageText;
    public float messageSeconds = 3f;

    // filling up at the gas station
    private bool pumping;
    private float pumpedLitres, pumpedCost;
    private Vehicle pumpingVehicle;
    private const float DriveOffKmh = 2f;

    private static ScreenPrompt prompt;
    private string promptText; // built once per visit, not every frame

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { prompt = null; }

    // the driven vehicle is in this zone, and it is the last building zone it entered
    private bool IsCurrent => Vehicle.Active != null && Vehicle.Active.CurrentZone<Building>() == this;

    void Start()
    {
        if (inputManager == null || playerValues == null)
        {
            Debug.LogError($"Building '{name}' is missing a reference (inputManager or playerValues).", this);
            enabled = false;
            return;
        }
        if (messageText != null) messageText.text = "";
        if (prompt == null) prompt = ScreenPrompt.Instance;
        if (type == BuildingType.Gas) SetupPumpDisplays();
    }

    void Update()
    {
        if (inputManager.Interact && IsCurrent)
            Interact();

        if (pumping) { Pump(); return; } // the pump shows its own message while filling

        UpdatePrompt();
    }

    private void UpdatePrompt()
    {
        if (prompt == null || !IsCurrent) return;

        // a message (from here or from a field) is on screen: leave it alone until it ends
        if (prompt.IsFlashing) return;

        promptText ??= PromptText();
        if (!prompt.Visible || prompt.CurrentText != promptText) prompt.Show(promptText);
    }

    private string PromptText()
    {
        string key = inputManager.TouchControlsActive ? "USE" : "E";
        switch (type)
        {
            case BuildingType.Sell: return $"Press {key} to sell {crop}";
            case BuildingType.Buy: return $"Press {key} to buy {seedsPerBuy:N0} seeds (about $ {(minSeedPrice + maxSeedPrice) / 2f:N0})";
            case BuildingType.Dealer: return $"Press {key} to buy vehicles";
            case BuildingType.Fertilizer: return $"Press {key} to buy {fertilizerPerBuy:N0} fertilizer (about $ {(minFertilizerPrice + maxFertilizerPrice) / 2f:N0})";
            case BuildingType.Gas: return $"Press {key} to fill up with diesel ($ {fuelPrice:0.00} per litre)";
            default: return $"{type} (coming soon)";
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Vehicle.TryGet(other, out var vehicle)) return;
        vehicle.EnterZone(this);
        promptText = null;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!Vehicle.TryGet(other, out var vehicle)) return;
        vehicle.ExitZone(this);
        HideIfNone();
    }

    private void OnEnable() => Vehicle.ActiveChanged += OnVehicleSwitched;

    private void OnDisable()
    {
        Vehicle.ActiveChanged -= OnVehicleSwitched;
        if (pumping) { pumping = false; SetPumpDisplays(PumpPriceText); }
        HideIfNone();
    }

    private void OnVehicleSwitched()
    {
        promptText = null;
        HideIfNone();
    }

    // the driven vehicle is in no building zone: hide the box, but never cut off a message that is still showing
    private void HideIfNone()
    {
        bool inside = Vehicle.Active != null && Vehicle.Active.CurrentZone<Building>() != null;
        if (!inside && prompt != null && !prompt.IsFlashing) prompt.Hide();
    }

    private void Interact()
    {
        switch (type)
        {
            case BuildingType.Sell: Sell(); break;
            case BuildingType.Buy: BuySeeds(); break;
            case BuildingType.Dealer: OpenDealer(); break;
            case BuildingType.Fertilizer: BuyFertilizer(); break;
            case BuildingType.Gas: if (pumping) StopPumping("Stopped"); else StartPumping(); break;
            default: Debug.Log($"{type} building is not implemented yet."); break;
        }
    }

    private void OpenDealer()
    {
        if (dealer == null) dealer = GetComponentInParent<Dealer>();
        if (dealer != null) dealer.OpenMenu();
        else Debug.LogWarning($"Dealer building '{name}' has no Dealer to open.", this);
    }

    private void Sell()
    {
        int amount;
        switch (crop)
        {
            case Crop.Wheat: amount = playerValues.InWheat; playerValues.InWheat = 0; break;
            case Crop.Barley: amount = playerValues.InBarley; playerValues.InBarley = 0; break;
            default: amount = playerValues.InOat; playerValues.InOat = 0; break;
        }

        if (amount <= 0)
        {
            ShowError($"No {crop} to sell");
            return;
        }

        float profit = amount * Random.Range(minPrice, maxPrice);
        playerValues.Money += profit;
        GameSounds.Play(GameSounds.Sound.MoneyIn);
        ShowMessage($"{crop} Sold, Profit ({profit:N0}$)");
    }

    private void BuySeeds()
    {
        int cost = Mathf.RoundToInt(Random.Range(minSeedPrice, maxSeedPrice));
        if (playerValues.Money < cost)
        {
            ShowError($"Not enough money. {seedsPerBuy:N0} seeds cost $ {cost:N0}");
            return;
        }

        playerValues.Money -= cost;
        playerValues.Seeds += seedsPerBuy;
        GameSounds.Play(GameSounds.Sound.MoneyOut);
        ShowMessage($"Bought {seedsPerBuy:N0} seeds for $ {cost:N0}");
    }

    private void BuyFertilizer()
    {
        int cost = Mathf.RoundToInt(Random.Range(minFertilizerPrice, maxFertilizerPrice));
        if (playerValues.Money < cost)
        {
            ShowError($"Not enough money. {fertilizerPerBuy:N0} fertilizer costs $ {cost:N0}");
            return;
        }

        playerValues.Money -= cost;
        playerValues.Fertilizer += fertilizerPerBuy;
        GameSounds.Play(GameSounds.Sound.MoneyOut);
        ShowMessage($"Bought {fertilizerPerBuy:N0} fertilizer for $ {cost:N0}");
    }

    // ---- gas station: fills the shared tank slowly (every vehicle uses the same diesel) ----

    private string PumpPriceText => string.Format(pumpPriceFormat, fuelPrice);

    private void SetupPumpDisplays()
    {
        pumpDisplays.RemoveAll(t => t == null);
        if (pumpDisplays.Count == 0)
        {
            var root = transform.parent != null ? transform.parent : transform;
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
                if (t.name == "Display Numbers") pumpDisplays.Add(t);
        }
        SetPumpDisplays(PumpPriceText);
    }

    private void SetPumpDisplays(string text)
    {
        foreach (var t in pumpDisplays)
            if (t != null) t.text = text;
    }

    private void StartPumping()
    {
        if (playerValues.FuelCapacity - playerValues.Fuel < 0.05f) { ShowError("The tank is already full"); return; }
        if (playerValues.Money < fuelPrice * 0.1f) { ShowError($"Not enough money. Diesel costs $ {fuelPrice:0.00} per litre"); return; }

        pumping = true;
        pumpingVehicle = Vehicle.Active;
        pumpedLitres = pumpedCost = 0f;
    }

    // one frame of filling; ends itself when the tank is full, the money runs out or the vehicle leaves
    private void Pump()
    {
        if (!IsCurrent || Vehicle.Active != pumpingVehicle) { StopPumping("Stopped"); return; }
        if (Mathf.Abs(pumpingVehicle.SpeedKmh) > DriveOffKmh) { StopPumping("You drove off. Stopped"); return; }

        float space = playerValues.FuelCapacity - playerValues.Fuel;
        float affordable = playerValues.Money / fuelPrice;
        float litres = Mathf.Min(fuelPerSecond * Time.deltaTime, space, affordable);
        if (litres > 0f)
        {
            playerValues.Fuel += litres;
            playerValues.Money -= litres * fuelPrice;
            pumpedLitres += litres;
            pumpedCost += litres * fuelPrice;
        }

        if (space - litres <= 0.001f) { StopPumping("Tank full"); return; }
        if (affordable - litres <= 0.001f) { StopPumping("Out of money"); return; }

        string key = inputManager.TouchControlsActive ? "USE" : "E";
        if (prompt != null) prompt.Show($"Filling up... {pumpedLitres:0.0} L   $ {pumpedCost:N2}\nPress {key} to stop");
        SetPumpDisplays($"{pumpedLitres:0.0} L");
    }

    private void StopPumping(string reason)
    {
        pumping = false;
        SetPumpDisplays(PumpPriceText);
        promptText = null;
        if (pumpedLitres > 0.05f) GameSounds.Play(GameSounds.Sound.MoneyOut); // paid once, when filling ends
        ShowMessage(pumpedLitres > 0.05f ? $"{reason}: {pumpedLitres:0.0} L of diesel for $ {pumpedCost:N2}" : reason);
    }

    private void ShowError(string text)
    {
        GameSounds.Play(GameSounds.Sound.Error);
        ShowMessage(text);
    }

    private void ShowMessage(string text)
    {
        Debug.Log(text);
        if (prompt != null) prompt.Flash(text, messageSeconds);
        if (messageText == null) return;
        StopAllCoroutines();
        StartCoroutine(MessageRoutine(text));
    }

    private IEnumerator MessageRoutine(string text)
    {
        messageText.text = text;
        yield return new WaitForSeconds(messageSeconds);
        messageText.text = "";
    }
}
