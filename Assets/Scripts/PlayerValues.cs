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

    private InputManager inputManager;

    /// <summary>The crop planted in your fields. Change it with the C key.</summary>
    public CropType SelectedCrop { get; private set; } = CropType.Wheat;

    public void Start()
    {
        inputManager = GetComponent<InputManager>();
        if (inputManager == null) inputManager = FindFirstObjectByType<InputManager>();
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

    }

    public int InWheat { get => inWheat; set { inWheat = value; UpdateUI(); } }
    public int InBarley { get => inBarley; set { inBarley = value; UpdateUI(); } }
    public float Money { get => money; set { money = value; UpdateUI(); } }
    public int InOat { get => inOat; set { inOat = value; UpdateUI(); } }
}
