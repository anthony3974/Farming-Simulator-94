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

    public void UpdateUI(bool isPlayerInside, string name = "", string owner = "", string value = "", string size = "", string type = "", string status = "", string fertilized = "")
    {
        if (isPlayerInside)
        {
            gameObject.SetActive(isPlayerInside);
            txtName.text = name;
            txtOwner.text = owner;
            txtValue.text = value;
            txtSize.text = size;
            txtType.text = type;
            txtStatus.text = status;
            txtFertilized.text = fertilized;
        }
        else
        {
            gameObject.SetActive(isPlayerInside);
        }
    }
}
