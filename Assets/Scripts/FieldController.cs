using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FieldController : MonoBehaviour
{
    public InputManager inputManager;
    public PlayerValues playerValues;
    public FieldView fieldView;

    public string fieldName;
    public string ownerName;

    private MeshRenderer meshRenderer;
    private Material dirt;
    private Material planted;
    private Material wheat1;
    private Material wheat2;
    private Material wheat3;
    private Material wheatReady;
    private Material barley1;
    private Material barley2;
    private Material barley3;
    private Material barleyReady;
    private Material oat1;
    private Material oat2;
    private Material oat3;
    private Material oatReady;
    private Material fert1;
    private Material fert2;

    private float size;
    private float value;

    private enum Crops
    {
        Dirt,
        Planted,
        TypeWheat,
        Wheat1,
        Wheat2,
        Wheat3,
        WheatReady,
        TypeBarley,
        Barley1,
        Barley2,
        Barley3,
        BarleyReady,
        TypeOat,
        Oat1,
        Oat2,
        Oat3,
        OatReady,
        Fert1,
        Fert2,
    }

    private Crops crop;
    private Crops cropType;
    private byte fertStage;

    private bool isPlayerInside;

    // Fields the player is currently standing in. The last one entered is the active one,
    // so overlapping fields no longer all react to a single key press.
    private static readonly List<FieldController> fieldsInside = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => fieldsInside.Clear();

    System.Random rn = new();
    private int lastRandom = -1;

    // Start is called before the first frame update
    void Start()
    {
        meshRenderer = gameObject.GetComponent<MeshRenderer>();

        Material currMat = meshRenderer.material;

        dirt = new Material(currMat)
        {
            color = new Color32(130, 90, 60, 255)
        };
        planted = new Material(currMat)
        {
            color = new Color32(50, 230, 30, 255)
        };
        wheat1 = new Material(currMat)
        {
            color = new Color32(195, 215, 50, 255)
        };
        wheat2 = new Material(currMat)
        {
            color = new Color32(215, 195, 50, 255)
        };
        wheat3 = new Material(currMat)
        {
            color = new Color32(235, 200, 5, 255)
        };
        wheatReady = new Material(currMat)
        {
            color = new Color32(255, 190, 0, 255)
        };
        barley1 = new Material(currMat)
        {
            color = new Color32(170, 180, 35, 255)
        };
        barley2 = new Material(currMat)
        {
            color = new Color32(210, 195, 30, 255)
        };
        barley3 = new Material(currMat)
        {
            color = new Color32(190, 140, 30, 255)
        };
        barleyReady = new Material(currMat)
        {
            color = new Color32(150, 110, 25, 255)
        };
        oat1 = new Material(currMat)
        {
            color = new Color32(195, 200, 35, 255)
        };
        oat2 = new Material(currMat)
        {
            color = new Color32(200, 180, 25, 255)
        };
        oat3 = new Material(currMat)
        {
            color = new Color32(215, 160, 50, 255)
        };
        oatReady = new Material(currMat)
        {
            color = new Color32(230, 130, 40, 255)
        };
        fert1 = new Material(currMat)
        {
            color = new Color32(100, 185, 235, 255)
        };
        fert2 = new Material(currMat)
        {
            color = new Color32(80, 155, 235, 255)
        };

        size = gameObject.transform.localScale.x * gameObject.transform.localScale.z / 0.043029259f;
        value = size * 0.5f;
    }

    // Update is called once per frame
    void Update()
    {
        InputKey();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            fieldsInside.Remove(this);
            fieldsInside.Add(this);
            RefreshUI();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            fieldsInside.Remove(this);

            if (fieldsInside.Count > 0) fieldsInside[^1].RefreshUI();
            else fieldView.UpdateUI(false);
        }
    }

    private void OnDisable()
    {
        fieldsInside.Remove(this);
    }

    private void RefreshUI()
    {
        fieldView.UpdateUI(true, fieldName, ownerName, value.ToString("N0"), size.ToString("N0"), CropName(), StatusName(), FertilizerName());
    }

    // Readable text for the pop-up (the enum names contain "Type", "Wheat1", ...)
    private string CropName()
    {
        switch (cropType)
        {
            case Crops.TypeWheat: return "Wheat";
            case Crops.TypeBarley: return "Barley";
            case Crops.TypeOat: return "Oat";
            default: return "None";
        }
    }

    private string StatusName()
    {
        switch (crop)
        {
            case Crops.Dirt: return "Empty";
            case Crops.Planted: return "Planted";
            case Crops.Wheat1:
            case Crops.Barley1:
            case Crops.Oat1: return "Growing 1/3";
            case Crops.Wheat2:
            case Crops.Barley2:
            case Crops.Oat2: return "Growing 2/3";
            case Crops.Wheat3:
            case Crops.Barley3:
            case Crops.Oat3: return "Growing 3/3";
            case Crops.WheatReady:
            case Crops.BarleyReady:
            case Crops.OatReady: return "Ready!";
            default: return crop.ToString();
        }
    }

    private string FertilizerName()
    {
        return fertStage == 0 ? "No" : $"Stage {fertStage}";
    }

    private void InputKey()
    {
        if (inputManager.Interact && fieldsInside.Count > 0 && fieldsInside[^1] == this)
            FieldStageChanger();
    }

    private IEnumerator Grow(int time)
    {
        yield return new WaitForSeconds(time);
        FieldStageChanger(true);
    }

    private void FieldStageChanger(bool isStageDone = false)
    {
        if (crop == Crops.Dirt)
        {
            crop = Crops.Planted;

            int cropTypeRandom = rn.Next(3);
            while (cropTypeRandom == lastRandom) cropTypeRandom = rn.Next(3);
            lastRandom = cropTypeRandom;

            if (cropTypeRandom == 0) cropType = Crops.TypeWheat;
            else if (cropTypeRandom == 1) cropType = Crops.TypeBarley;
            else if (cropTypeRandom == 2) cropType = Crops.TypeOat;

            StartCoroutine(Grow(7));
        }
        else if (crop == Crops.Planted && isStageDone)
        {
            if (cropType == Crops.TypeWheat) crop = Crops.Wheat1;
            else if (cropType == Crops.TypeBarley) crop = Crops.Barley1;
            else if (cropType == Crops.TypeOat) crop = Crops.Oat1;
            StartCoroutine(Grow(7));
        }
        else if ((crop == Crops.Wheat1 || crop == Crops.Barley1 || crop == Crops.Oat1) && isStageDone)
        {
            if (cropType == Crops.TypeWheat) crop = Crops.Wheat2;
            else if (cropType == Crops.TypeBarley) crop = Crops.Barley2;
            else if (cropType == Crops.TypeOat) crop = Crops.Oat2;
            StartCoroutine(Grow(7));
        }
        else if ((crop == Crops.Wheat2 || crop == Crops.Barley2 || crop == Crops.Oat2) && isStageDone)
        {
            if (cropType == Crops.TypeWheat) crop = Crops.Wheat3;
            else if (cropType == Crops.TypeBarley) crop = Crops.Barley3;
            else if (cropType == Crops.TypeOat) crop = Crops.Oat3;
            StartCoroutine(Grow(7));
        }
        else if ((crop == Crops.Wheat3 || crop == Crops.Barley3 || crop == Crops.Oat3) && isStageDone)
        {
            if (cropType == Crops.TypeWheat) crop = Crops.WheatReady;
            else if (cropType == Crops.TypeBarley) crop = Crops.BarleyReady;
            else if (cropType == Crops.TypeOat) crop = Crops.OatReady;
        }
        else if ((crop == Crops.WheatReady || crop == Crops.BarleyReady || crop == Crops.OatReady) && isStageDone == false)
        {
            crop = Crops.Dirt;
            HarvestField();
        }

        SetColor(crop);

        // keep the pop-up in sync while the player is standing in the field
        if (fieldsInside.Count > 0 && fieldsInside[^1] == this) RefreshUI();
    }

    private void SetColor(Crops mats)
    {
        if (mats == Crops.Dirt) meshRenderer.material = dirt;
        else if (mats == Crops.Planted) meshRenderer.material = planted;
        else if (mats == Crops.Wheat1) meshRenderer.material = wheat1;
        else if (mats == Crops.Wheat2) meshRenderer.material = wheat2;
        else if (mats == Crops.Wheat3) meshRenderer.material = wheat3;
        else if (mats == Crops.WheatReady) meshRenderer.material = wheatReady;
        else if (mats == Crops.Barley1) meshRenderer.material = barley1;
        else if (mats == Crops.Barley2) meshRenderer.material = barley2;
        else if (mats == Crops.Barley3) meshRenderer.material = barley3;
        else if (mats == Crops.BarleyReady) meshRenderer.material = barleyReady;
        else if (mats == Crops.Oat1) meshRenderer.material = oat1;
        else if (mats == Crops.Oat2) meshRenderer.material = oat2;
        else if (mats == Crops.Oat3) meshRenderer.material = oat3;
        else if (mats == Crops.OatReady) meshRenderer.material = oatReady;
        else if (mats == Crops.Fert1) meshRenderer.material = fert1;
        else if (mats == Crops.Fert2) meshRenderer.material = fert2;
    }

    private void HarvestField()
    {
        if (cropType == Crops.TypeWheat) playerValues.InWheat += Convert.ToInt32(size);
        else if (cropType == Crops.TypeBarley) playerValues.InBarley += Convert.ToInt32(size);
        else if (cropType == Crops.TypeOat) playerValues.InOat += Convert.ToInt32(size);
    }
}
