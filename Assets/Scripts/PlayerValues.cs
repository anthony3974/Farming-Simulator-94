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

    public void Start()
    {
        UpdateUI();
    }
    private void UpdateUI()
    {
        txWheat.text = $"{inWheat:N0}";
        txBarley.text = $"{inBarley:N0}";
        txOat.text = $"{inOat:N0}";

    }

    public int InWheat { get => inWheat; set { inWheat = value; UpdateUI(); } }
    public int InBarley { get => inBarley; set { inBarley = value; UpdateUI(); } }
    public int InOat { get => inOat; set { inOat = value; UpdateUI(); } }
}
