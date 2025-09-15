using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    [SerializeField] private GameObject inventoryContainer; // parent with 24 slot children
    [SerializeField] private List<Item> items = new List<Item>();
    [SerializeField] private ItemDisplay itemDisplay;
    
    private ItemBox[] slots; // fixed slots

    public static Inventory Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // cache references to all slot ItemBox components
        slots = inventoryContainer.GetComponentsInChildren<ItemBox>();
        updateInventory();
    }

    public void updateInventory()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < items.Count)
            {
                Item item = items[i];
                item.itemDisplay = itemDisplay;
                slots[i].setItem(item);
            }
            else
            {
                slots[i].clearSlot(); // custom method to empty a slot
            }
        }
    }

    public void addItem(Item item)
    {
        foreach (Item unsorteditem in items)
        {
            if (item.getName() == unsorteditem.getName())
            {
                unsorteditem.increaseAmount(1);
                updateInventory();
                return; // don't add duplicate to the list
            }
        }

        if (items.Count < slots.Length)
        {
            items.Add(item);
            item.increaseAmount(1);
            updateInventory();
        }
        else
        {
            Debug.Log("Inventory full!");
        }
    }

    public void removeItem(Item item)
    {
        items.Remove(item);
        updateInventory();
    }
}
