using UnityEngine;

/// <summary>
/// Applies an item's behaviour. Kept separate from <see cref="ItemDefinition"/> so the
/// definition stays a pure data asset, and separate from the old MonoBehaviours so nothing has
/// to be instantiated just to use an item.
/// </summary>
public static class ItemUsage
{
    /// <summary>
    /// Use an inventory stack. This takes the whole stack rather than just the definition
    /// because equipment carries per-item state: equipping must pass the very same
    /// <see cref="EquipmentInstance"/> the stack holds, or the item's inscriptions would be
    /// left behind on a discarded instance.
    /// </summary>
    public static void Use(ItemStack stack, Inventory inventory)
    {
        if (stack.IsEmpty || stack.definition == null)
        {
            return;
        }

        ItemDefinition definition = stack.definition;

        switch (definition.Kind)
        {
            case ItemKind.Weapon:
            case ItemKind.Armor:
                if (Equipments.Instance == null)
                {
                    Debug.LogError($"ItemUsage: no Equipments instance; cannot equip '{definition.DisplayName}'.");
                    return;
                }
                // Defensive fallback: a stack that entered the inventory before the instance
                // layer existed would have no instance attached.
                EquipmentInstance instance = stack.instance ?? new EquipmentInstance(definition);
                Equipments.Instance.Equip(instance);
                break;

            case ItemKind.Inscription:
                InscriptionService.BeginApply(definition, inventory);
                break;

            case ItemKind.Consumable:
                ApplyConsumable(definition);
                if (inventory != null)
                {
                    inventory.removeItem(definition, 1);
                }
                break;

            default:
                Debug.Log($"ItemUsage: '{definition.DisplayName}' cannot be used directly.");
                break;
        }
    }

    private static void ApplyConsumable(ItemDefinition definition)
    {
        if (PlayerStats.Instance == null)
        {
            Debug.LogError($"ItemUsage: no PlayerStats instance; '{definition.DisplayName}' had no effect.");
            return;
        }

        if (definition.Stat == ConsumableStat.Heal)
        {
            PlayerStats.Instance.heal(definition.EffectAmount);
            return;
        }

        PlayerStats.Instance.increaseStatTemp(definition.Stat,
                                              definition.EffectAmount,
                                              definition.EffectDuration);
    }
}
