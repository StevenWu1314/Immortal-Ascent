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
    [SerializeField] Button button;


    public void setFields(String name, String description, Sprite sprite, String type, int value, Item item)
    {
        itemName.text = name;
        this.description.text = description;
        icon.sprite = sprite;
        this.typeValue.text = type + ": " + value;
        this.button.GetComponent<Button>().onClick.RemoveAllListeners();
        this.button.GetComponent<Button>().onClick.AddListener(() => item.onUse());
    }


}
