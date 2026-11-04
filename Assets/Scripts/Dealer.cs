using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The vehicle dealer: the red L-shaped building in the top-left corner of the map. Press E / USE in its
/// lot to open the shop menu. The van and the harvester are real objects in the scene (Van.prefab and
/// Harvester.prefab, parked on "Parking" by the house); buying one makes that vehicle yours, where it stands.
/// Switch to it with 1 / 2 / 3 (CAR on touch). Vehicles are red (their paint material); there is no colour choice.
/// The shop menu is a real object in the scene hierarchy ("Dealer Menu", made by Tools > Build Dealer Menu), so
/// it can be moved, resized, restyled and reworded in the editor. This script only opens and closes it, fills
/// in the money and the BUY / OWNED buttons, and does the buying. Prices come from VehicleCatalog.cs.
/// </summary>
public class Dealer : MonoBehaviour
{
    [System.Serializable]
    public class Row
    {
        public VehicleKind kind;
        [Tooltip("The BUY button (a copy of the UI Button prefab).")]
        public Button button;
        [Tooltip("The button's text: shows the price, or OWNED.")]
        public TMP_Text label;
        [Tooltip("Optional: the button's brown Fill, turned grey once the vehicle is yours.")]
        public Image fill;
    }

    public PlayerValues playerValues;
    public InputManager inputManager;
    [Tooltip("Empty = found at runtime.")]
    public GameCharacter player;

    [Header("Menu scene objects (set by the builder; keep them set if you rearrange)")]
    [Tooltip("The part shown while open (dimmer and panel). Hidden when the game runs.")]
    public GameObject menuRoot;
    [Tooltip("Optional: the full-screen dimmer behind the panel; a tap on it closes the menu.")]
    public Button closeButton;
    public TMP_Text moneyText;
    [Tooltip("Optional: the footer shown on PC.")]
    public GameObject keyboardFooter;
    [Tooltip("Optional: the footer shown with touch controls.")]
    public GameObject touchFooter;
    [Tooltip("One per vehicle for sale.")]
    public List<Row> rows = new();

    [Header("Texts ({0} = the amount)")]
    public string moneyFormat = "Your money: $ {0:N0}";
    public string buyFormat = "BUY  $ {0:N0}";
    public string ownedText = "OWNED";

    // the old runtime menu's colours: brown fill (the UI Button prefab's) and the greyed-out grey
    private static readonly Color ButtonFill = new Color32(60, 45, 20, 255);
    private static readonly Color ButtonOff = new Color32(70, 70, 70, 255);

    private bool ready;
    private int openedFrame;

    public bool IsOpen => ready && menuRoot.activeSelf;

    void Start()
    {
        if (player == null) player = FindFirstObjectByType<GameCharacter>();
        if (playerValues == null) playerValues = FindFirstObjectByType<PlayerValues>();
        if (inputManager == null) inputManager = FindFirstObjectByType<InputManager>();

        bool rowsOk = rows.Count > 0;
        foreach (var r in rows) if (r == null || r.button == null || r.label == null) rowsOk = false;
        if (menuRoot == null || moneyText == null || !rowsOk || player == null || playerValues == null || inputManager == null)
        {
            string missing = (menuRoot == null ? " Menu Root" : "") + (moneyText == null ? " Money Text" : "")
                + (!rowsOk ? " Rows (a button or label)" : "") + (player == null ? " (no GameCharacter in the scene)" : "")
                + (playerValues == null ? " (no PlayerValues in the scene)" : "") + (inputManager == null ? " (no InputManager in the scene)" : "");
            Debug.LogError($"Dealer menu turned off: '{name}' is missing{missing}. Drag the objects back into its Dealer slots, "
                + "or run Tools > Build Dealer Menu again.", this);
            if (menuRoot != null) menuRoot.SetActive(false);
            return;
        }

        bool touch = inputManager.TouchControlsActive;
        if (keyboardFooter != null) keyboardFooter.SetActive(!touch);
        if (touchFooter != null) touchFooter.SetActive(touch);
        if (closeButton != null) closeButton.onClick.AddListener(() => Close(relockMouse: true));
        foreach (var r in rows)
        {
            var kind = r.kind;
            r.button.onClick.AddListener(() => Buy(VehicleCatalog.Get(kind)));
        }
        menuRoot.SetActive(false);
        ready = true;
    }

    void Update()
    {
        if (!IsOpen || Time.frameCount == openedFrame) return;

        // the game keys are blocked while the menu is open, so read the close keys directly
        if (Input.GetKeyDown(KeyCode.Escape)) Close(relockMouse: false);
        else if (Input.GetKeyDown(KeyCode.E)) Close(relockMouse: true);
    }

    public void OpenMenu()
    {
        if (!ready)
        {
            Debug.LogError("Dealer menu is turned off (see the error logged at the start).", this);
            return;
        }

        menuRoot.SetActive(true);
        openedFrame = Time.frameCount;
        inputManager.MenuOpen = true;
        player.SetCursorLocked(false);
        Refresh();
    }

    private void Close(bool relockMouse)
    {
        menuRoot.SetActive(false);
        inputManager.MenuOpen = false;
        if (relockMouse && !inputManager.TouchControlsActive) player.SetCursorLocked(true);
    }

    private void OnDisable()
    {
        if (IsOpen) Close(relockMouse: false);
    }

    // ---- buying ----

    private void Buy(VehicleCatalog.Info info)
    {
        var box = ScreenPrompt.Instance;
        if (player.Owns(info.kind)) return;

        if (playerValues.Money < info.price)
        {
            box.Flash($"Not enough money. The {info.name} costs $ {info.price:N0}", 3f);
            return;
        }
        var vehicle = FindParked(info.kind);
        if (vehicle == null)
        {
            Debug.LogError($"Dealer: there is no {info.name} in the scene to sell. Drag " +
                           $"Assets/Prefabs/{info.name}.prefab into the scene.", this);
            box.Flash($"Sorry, there is no {info.name} on the map right now.", 3f);
            return;
        }

        playerValues.Money -= info.price;
        player.AddVehicle(vehicle);
        Close(relockMouse: true);

        string how = inputManager.TouchControlsActive ? "tap CAR" : $"press {(int)info.kind + 1}";
        box.Flash($"Bought the {info.name} for $ {info.price:N0}.\nIt is parked by your house, {how} to drive it.", 5f);
    }

    // the van or harvester standing in the scene (you can only own one of each, so any one of that kind)
    private static Vehicle FindParked(VehicleKind kind)
    {
        foreach (var v in FindObjectsByType<Vehicle>(FindObjectsSortMode.None))
            if (v.kind == kind) return v;
        return null;
    }

    // ---- menu ----

    private void Refresh()
    {
        moneyText.text = string.Format(moneyFormat, playerValues.Money);
        foreach (var r in rows)
        {
            bool owned = player.Owns(r.kind);
            r.button.interactable = !owned;
            if (r.fill != null) r.fill.color = owned ? ButtonOff : ButtonFill;
            r.label.text = owned ? ownedText : string.Format(buyFormat, VehicleCatalog.Get(r.kind).price);
        }
    }
}
