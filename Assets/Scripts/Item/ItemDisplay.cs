using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemDisplay : MonoBehaviour
{
    [SerializeField] TMP_Text itemName;
    [SerializeField] TMP_Text description;
    [SerializeField] Image icon;
    [SerializeField] TMP_Text typeValue;


    public void setFields(String name, String description, Sprite sprite, String type, int value)
    {
        itemName.text = name;
        this.description.text = description;
        icon.sprite = sprite;
        this.typeValue.text = type + ": " + value;

    }


}
