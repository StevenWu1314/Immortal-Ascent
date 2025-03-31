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
    [SerializeField] private List<GameObject> oldContainers;


    public void updateInventory() {
        foreach (GameObject container in oldContainers) {
            Destroy(container);
            
            Debug.Log("destroyed a container");
        }
        oldContainers.Clear();
        int i = 0;
        foreach (Item item in items){
            item.itemDisplay = itemDisplay;
            GameObject itemBox = Instantiate(ItemContainer, new Vector3(0, 0, 0), quaternion.identity, inventoryContainer.transform);
            Image[] itemHolder = itemBox.GetComponentsInChildren<Image>();
            foreach (Image holder in itemHolder){
                if(holder.sprite == null)
                {
                    holder.sprite = item.getSprite();
                }
            }
            itemBox.GetComponent<ItemBox>().setItem(item);
            itemBox.GetComponent<ItemBox>().itemBox = itemBox;
            oldContainers.Add(itemBox);
        }
    }

    public void addItem(Item item) {
    
        updateInventory();
    }

    private void Start() {
        updateInventory();
    }
}
