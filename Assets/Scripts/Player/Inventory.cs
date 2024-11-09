using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting.ReorderableList;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    [SerializeField] private GameObject inventoryContainer;
    [SerializeField] private List<Item> items= new List<Item>();
    [SerializeField] private GameObject ItemContainer;
    [SerializeField] private ItemDisplay itemDisplay;
    public void updateInventory() {
        foreach (Item item in items){
            item.itemDisplay = itemDisplay;
            Debug.Log(item.itemDisplay);
            GameObject itemBox = Instantiate(ItemContainer, new Vector3(0, 0, 0), quaternion.identity, inventoryContainer.transform);
            itemBox.GetComponentInChildren<TMP_Text>().text = item.getName();
            itemBox.GetComponent<Image>().sprite = item.getSprite();
            itemBox.GetComponent<ItemBox>().setItem(item);
            itemBox.GetComponent<ItemBox>().itemBox = itemBox;
        }
    }

    public void addItem(Item item) {
        items.Add(item);
        updateInventory();
    }

    private void Start() {
        updateInventory();
    }
}
