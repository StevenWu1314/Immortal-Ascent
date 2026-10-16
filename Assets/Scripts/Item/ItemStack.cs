using System;

/// <summary>
/// One inventory slot: a shared <see cref="ItemDefinition"/> plus how many units this slot
/// holds. This is the runtime instance the old design lacked -- it used a MemberwiseClone of a
/// MonoBehaviour as a stand-in, which is why stackLimit never worked and why equipping could
/// hand out a reference that no longer existed in the inventory.
/// </summary>
[Serializable]
public struct ItemStack
{
    public ItemDefinition definition;
    public int amount;

    /// <summary>
    /// Non-null only for equipment. Equipment cannot stack precisely because this instance
    /// carries per-item state (its inscriptions), which a shared definition must not hold.
    /// </summary>
    public EquipmentInstance instance;

    public ItemStack(ItemDefinition definition, int amount)
    {
        this.definition = definition;
        this.amount = amount;
        this.instance = null;
    }

    public ItemStack(EquipmentInstance instance)
    {
        this.instance = instance;
        this.definition = instance != null ? instance.definition : null;
        this.amount = instance != null ? 1 : 0;
    }

    public bool IsEmpty => definition == null || amount <= 0;

    public string DisplayName => definition != null ? definition.DisplayName : "(empty)";

    public override string ToString()
    {
        return $"{DisplayName} x{amount}";
    }
}
