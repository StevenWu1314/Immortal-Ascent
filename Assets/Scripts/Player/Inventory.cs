using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [System.Serializable]
    public struct StarterItem
    {
        public ItemDefinition definition;
        public int amount;
    }

    [SerializeField] private List<ItemStack> items = new List<ItemStack>();
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
        GameObject inventoryContainer = GameObject.Find("UIManager").transform.Find("Inventory").transform.Find("itemholder").gameObject;
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

    /// <summary>
    /// Seed the inventory from the serialized starter list. This used to Instantiate a copy of
    /// each item prefab and then throw it away (addItem cloned it instead), leaking roughly 17
    /// GameObjects on every scene start. ItemStack needs no GameObject at all.
    /// </summary>
    private void addStarterItems()
    {
        foreach (StarterItem entry in StarterItems)
        {
            if (entry.definition == null || entry.amount <= 0)
            {
                continue;
            }
            int leftover = addItem(entry.definition, entry.amount);
            if (leftover > 0)
            {
                Debug.LogWarning($"Inventory: no room for {leftover} x {entry.definition.DisplayName} " +
                                 "from StarterItems.");
            }
        }
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
            if (i < items.Count && !items[i].IsEmpty)
            {
                slots[i].setItem(items[i], itemDisplay);
            }
            else
            {
                slots[i].clearSlot(); // custom method to empty a slot
            }
        }
    }

    /// <summary>
    /// Add <paramref name="amount"/> units, topping up existing stacks as far as their
    /// stackLimit allows and opening new stacks only when needed.
    ///
    /// Returns how many units could NOT be stored, so the caller can surface a message
    /// instead of silently destroying them. The old addItem() ignored the incoming amount
    /// entirely and did a hardcoded +1 per call, so picking up N units stored 1.
    /// </summary>
    public int addItem(ItemDefinition definition, int amount)
    {
        if (definition == null)
        {
            Debug.LogWarning("Inventory.addItem called with a null definition; ignoring.");
            return 0;
        }
        if (amount <= 0)
        {
            return 0;
        }

        // Discovery (T07): handling a material for the first time reveals the recipes that use it. Every
        // acquisition path funnels through here - mining, chests, corpse drops and starter items.
        foreach (ItemDefinition unlocked in RecipeDiscovery.Observe(definition))
        {
            Debug.Log($"[CRAFT] new recipe unlocked: {unlocked.DisplayName}");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.DrawFlowupText($"New recipe: {unlocked.DisplayName}",
                    PlayerStats.Instance != null ? PlayerStats.Instance.transform.position : transform.position);
            }
        }

        if (slots == null || slots.Length == 0)
        {
            Debug.LogWarning("Inventory has no slots; cannot add " + definition.DisplayName);
            return amount;
        }

        // Equipment takes one instance per unit and never merges. Inscriptions live on the
        // instance, so folding a second Iron Sword into the first one's stack would silently
        // fuse two different sets of inscriptions.
        if (IsEquipmentKind(definition))
        {
            int equipmentRemaining = amount;
            while (equipmentRemaining > 0 && items.Count < slots.Length)
            {
                items.Add(new ItemStack(new EquipmentInstance(definition)));
                equipmentRemaining--;
            }
            updateInventory();
            if (equipmentRemaining > 0)
            {
                Debug.Log($"Inventory full! {equipmentRemaining} x {definition.DisplayName} could not be stored.");
            }
            return equipmentRemaining;
        }

        int limit = definition.StackLimit;
        int remaining = amount;

        // Top up stacks of the same definition first.
        for (int i = 0; i < items.Count && remaining > 0; i++)
        {
            ItemStack stack = items[i];
            if (stack.definition != definition)
            {
                continue;
            }
            int space = limit - stack.amount;
            if (space <= 0)
            {
                continue;
            }
            int moved = Mathf.Min(space, remaining);
            stack.amount += moved;
            items[i] = stack;   // ItemStack is a struct: write the copy back
            remaining -= moved;
        }

        // Then fill empty slots, which is what actually bounds the inventory.
        while (remaining > 0 && items.Count < slots.Length)
        {
            int moved = Mathf.Min(limit, remaining);
            items.Add(new ItemStack(definition, moved));
            remaining -= moved;
        }

        updateInventory();

        if (remaining > 0)
        {
            Debug.Log($"Inventory full! {remaining} x {definition.DisplayName} could not be stored.");
        }
        return remaining;
    }

    /// <summary>Remove <paramref name="amount"/> units of a definition, dropping empty stacks.</summary>
    public void removeItem(ItemDefinition definition, int amount)
    {
        if (definition == null || amount <= 0)
        {
            return;
        }

        int remaining = amount;
        for (int i = items.Count - 1; i >= 0 && remaining > 0; i--)
        {
            if (items[i].definition != definition)
            {
                continue;
            }
            ItemStack stack = items[i];
            int taken = Mathf.Min(stack.amount, remaining);
            stack.amount -= taken;
            remaining -= taken;

            if (stack.amount <= 0)
            {
                items.RemoveAt(i);
            }
            else
            {
                items[i] = stack;
            }
        }

        if (remaining > 0)
        {
            Debug.Log($"Inventory: tried to remove {remaining} more {definition.DisplayName} than held.");
        }
        updateInventory();
    }

    /// <summary>How many units of a definition the inventory currently holds.</summary>
    public int countOf(ItemDefinition definition)
    {
        if (definition == null)
        {
            return 0;
        }
        int total = 0;
        foreach (ItemStack stack in items)
        {
            if (stack.definition == definition)
            {
                total += stack.amount;
            }
        }
        return total;
    }

    /// <summary>
    /// Could <paramref name="amount"/> more units be stored right now? Callers that consume
    /// something first (crafting) must ask this beforehand, otherwise a full inventory eats the
    /// inputs and drops the output.
    /// </summary>
    public bool hasRoomFor(ItemDefinition definition, int amount)
    {
        if (definition == null || amount <= 0)
        {
            return true;
        }

        int limit = definition.StackLimit;
        int space = 0;

        foreach (ItemStack stack in items)
        {
            if (stack.definition == definition)
            {
                space += Mathf.Max(0, limit - stack.amount);
            }
        }

        int freeSlots = (slots != null ? slots.Length : 0) - items.Count;
        space += Mathf.Max(0, freeSlots) * limit;

        return space >= amount;
    }

    public void setItemDisplay(ItemDisplay itemDisplay)
    {
        this.itemDisplay = itemDisplay;
        updateInventory();
    }
    public List<ItemStack> getItems()
    {
        return items;
    }

    /// <summary>Equipment is tracked per instance, so it never participates in stacking.</summary>
    public static bool IsEquipmentKind(ItemDefinition definition)
    {
        return definition != null &&
               (definition.Kind == ItemKind.Weapon || definition.Kind == ItemKind.Armor);
    }

    /// <summary>
    /// Every equipment instance the player carries, in slot order. Inscriptions are applied to
    /// these, so the inscription UI needs instances rather than shared definitions.
    /// </summary>
    public List<EquipmentInstance> getEquipmentInstances()
    {
        List<EquipmentInstance> result = new List<EquipmentInstance>();
        foreach (ItemStack stack in items)
        {
            if (stack.instance != null)
            {
                result.Add(stack.instance);
            }
        }
        return result;
    }

    /// <summary>
    /// Remove one specific equipment instance, matched by reference. Removing "any Iron Sword"
    /// would be wrong here: two swords can carry different inscriptions.
    /// </summary>
    public bool removeItem(EquipmentInstance instance)
    {
        if (instance == null)
        {
            return false;
        }
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].instance == instance)
            {
                items.RemoveAt(i);
                updateInventory();
                return true;
            }
        }
        return false;
    }
}
