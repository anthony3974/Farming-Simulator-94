using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FieldView : MonoBehaviour
{
    public TMP_Text txtName;
    public TMP_Text txtOwner;
    public TMP_Text txtValue;
    public TMP_Text txtSize;
    public TMP_Text txtType;
    public TMP_Text txtStatus;
    public TMP_Text txtFertilized;

    // colour of the labels ("Size:", "Owner:", ...); the values stay white
    private const string LabelColor = "#FFC933";

    void Awake()
    {
        // long field names shrink to fit instead of running into the next column
        txtName.textWrappingMode = TextWrappingModes.NoWrap;
        txtName.overflowMode = TextOverflowModes.Ellipsis;
        txtName.enableAutoSizing = true;
        txtName.fontSizeMax = txtName.fontSize;
        txtName.fontSizeMin = 12;

        // the panel is active in the scene so Awake runs; start hidden, a field shows it when the player enters
        gameObject.SetActive(false);
    }

    private static string Line(string label, string value)
    {
        return $"<color={LabelColor}>{label}:</color> {value}";
    }

    public void UpdateUI(bool isPlayerInside, string name = "", string owner = "", string value = "", string size = "", string type = "", string status = "", string fertilized = "")
    {
        if (isPlayerInside)
        {
            gameObject.SetActive(isPlayerInside);
            txtName.text = Line("Field", name);
            txtOwner.text = Line("Owner", owner);
            txtValue.text = Line("Value", value);
            txtSize.text = Line("Size", size);
            txtType.text = Line("Crop", type);
            txtStatus.text = Line("Status", status);
            txtFertilized.text = Line("Fertilized", fertilized);
        }
        else
        {
            gameObject.SetActive(isPlayerInside);
        }
    }
}
