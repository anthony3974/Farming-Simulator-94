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

    public void Start()
    {
        UpdateUI();
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
