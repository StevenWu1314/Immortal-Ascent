using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public class Inventory : MonoBehaviour
{
    [System.Serializable]
    public struct StarterItem
    {
        public Item item;
        public int amount;
    }
    [SerializeField] private GameObject inventoryContainer; // parent with 24 slot children
    [SerializeField] private List<Item> items = new List<Item>();
    [SerializeField] private List<StarterItem> StarterItems;
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
        // cache references to all slot ItemBox components.
        // includeInactive: true is required — the Inventory panel starts disabled, so a
        // default search would find zero slots and every pickup would report "full".
        items.Clear();
        slots = inventoryContainer.GetComponentsInChildren<ItemBox>(true);
        if (slots == null || slots.Length == 0)
        {
            Debug.LogError("Inventory: no ItemBox slots found under " + inventoryContainer.name +
                           ". The inventory will not be able to hold anything.");
        }
        addStarterItems();
        updateInventory();
    }

    private void addStarterItems()
    {
        foreach (StarterItem item in StarterItems)
        {
            for(int i = 0; i < item.amount; i++)
            {
                addItem((Instantiate(item.item)));
            }
        }
        updateInventory();
    }

    public void updateInventory()
    {
        if (slots == null)
        {
            return;
        }
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                continue;
            }
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
        if (item == null)
        {
            Debug.LogWarning("Inventory.addItem called with null; ignoring.");
            return;
        }
        if (slots == null || slots.Length == 0)
        {
            Debug.LogWarning("Inventory has no slots; cannot add " + item.getName());
            return;
        }

        foreach (Item unsorteditem in items)
        {
            if (unsorteditem != null && item.getName() == unsorteditem.getName())
            {
                unsorteditem.increaseAmount(1);
                updateInventory();
                return; // don't add duplicate to the list
            }
        }

        if (items.Count < slots.Length)
        {

            items.Add(item.clone());
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
        if (item == null || items == null)
        {
            return;
        }
        int index = items.IndexOf(item);
        if (index < 0)
        {
            // The item is not in the inventory (already consumed or a stale reference).
            // Bail out instead of indexing items[-1].
            return;
        }

        items[index].subtractAmount(amount);
        if (items[index].getAmount() <= 0)
        {
            items.RemoveAt(index);
        }
        // Refresh on both paths: previously a surviving stack kept displaying its old count.
        updateInventory();
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
