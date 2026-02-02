using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public class Inventory : MonoBehaviour
{
    [SerializeField] private GameObject inventoryContainer; // parent with 24 slot children
    [SerializeField] private List<Item> items = new List<Item>();
    [SerializeField] private List<Item> StarterItems;
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
        inventoryContainer = GameObject.Find("UIManager").transform.Find("Inventory").transform.Find("itemholder").gameObject;
        // cache references to all slot ItemBox components
        items.Clear();
        slots = inventoryContainer.GetComponentsInChildren<ItemBox>();
        addStarterItems();
        updateInventory();
    }

    private void addStarterItems()
    {
        foreach (Item item in StarterItems)
        {
            addItem((Instantiate(item)));
        }
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

            items.Add(item.clone());
            Debug.Log("add: " + item.name);
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

    public void removeItem(Item item, int amount)
    {
        items[items.IndexOf(item)].subtractAmount(amount);
        if(items[items.IndexOf(item)].getAmount() <= 0)
        {
            items.Remove(item);
            updateInventory();
        }
    }
    public void setItemDisplay(ItemDisplay itemDisplay)
    {
        this.itemDisplay = itemDisplay;
        updateInventory();
    }

    public List<Item> getItems()
    {
        return items;
    }
}
