using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The inscription rules, with no UI dependency. The panel only decides *what* the player
/// picked; every validation and every mutation lives here so the rules can be exercised without
/// opening a panel.
/// </summary>
public static class InscriptionService
{
    /// <summary>Inscriptions one item can hold.</summary>
    public static int SlotCount => EquipmentInstance.MaxInscriptions;

    /// <summary>
    /// Every equipment instance this inscription could legally go on, equipped items first
    /// (applying to what you are holding is the common case), then the rest of the backpack.
    /// </summary>
    public static List<EquipmentInstance> FindTargets(ItemDefinition inscription,
                                                      Inventory inventory,
                                                      Equipments equipments)
    {
        var targets = new List<EquipmentInstance>();
        if (inscription == null)
        {
            return targets;
        }

        if (equipments != null)
        {
            AddIfValid(targets, equipments.meleeWeapon, inscription);
            AddIfValid(targets, equipments.rangeWeapon, inscription);
            AddIfValid(targets, equipments.armor, inscription);
        }

        if (inventory != null)
        {
            foreach (EquipmentInstance instance in inventory.getEquipmentInstances())
            {
                AddIfValid(targets, instance, inscription);
            }
        }

        return targets;
    }

    private static void AddIfValid(List<EquipmentInstance> targets, EquipmentInstance instance,
                                   ItemDefinition inscription)
    {
        if (instance == null || instance.definition == null)
        {
            return;
        }
        if (!inscription.CanApplyTo(instance.definition))
        {
            return;
        }
        // An equipped item is normally also in the backpack; do not list it twice.
        if (!targets.Contains(instance))
        {
            targets.Add(instance);
        }
    }

    /// <summary>Validate an application, returning the reason when it is not allowed.</summary>
    public static bool CanApply(ItemDefinition inscription, EquipmentInstance target, out string reason)
    {
        if (inscription == null)
        {
            reason = "no inscription";
            return false;
        }
        if (inscription.Kind != ItemKind.Inscription)
        {
            reason = $"{inscription.DisplayName} is not an inscription";
            return false;
        }
        if (target == null || target.definition == null)
        {
            reason = "no target equipment";
            return false;
        }
        if (!inscription.CanApplyTo(target.definition))
        {
            reason = $"{inscription.DisplayName} cannot be applied to {target.DisplayName}";
            return false;
        }
        if (!inscription.IsInscriptionImplemented)
        {
            // Phase 1 ships ItemStatBonus and PlayerStatBonus only. Say so rather than
            // consuming the item and doing nothing.
            reason = $"{inscription.DisplayName} uses {inscription.InscriptionEffect}, " +
                     "which is not implemented yet";
            return false;
        }

        reason = "";
        return true;
    }

    /// <summary>
    /// Apply an inscription to a specific slot, overwriting whatever was there. Inscriptions are
    /// never removed, only replaced. The item is consumed only after the slot write succeeds.
    /// </summary>
    public static bool Apply(ItemDefinition inscription, EquipmentInstance target, int slot,
                             Inventory inventory)
    {
        if (!CanApply(inscription, target, out string reason))
        {
            Debug.LogWarning($"InscriptionService: {reason}.");
            return false;
        }
        if (slot < 0 || slot >= EquipmentInstance.MaxInscriptions)
        {
            Debug.LogWarning($"InscriptionService: slot {slot} is out of range.");
            return false;
        }

        bool replaced = target.HasInscriptionAt(slot);
        if (!target.SetInscription(slot, inscription))
        {
            return false;
        }

        if (inventory != null)
        {
            inventory.removeItem(inscription, 1);
        }

        // Recompute the wearer's derived stats: a PlayerStatBonus changes them, and an
        // ItemStatBonus changes the item's own numbers which feed the same calculation.
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.RecalculateStats();
        }

        Debug.Log($"InscriptionService: applied {inscription.DisplayName} to {target.DisplayName} " +
                  $"slot {slot}{(replaced ? " (overwrote the previous inscription)" : "")}.");
        return true;
    }

    /// <summary>First free slot, or -1 when full -- the UI then asks which slot to overwrite.</summary>
    public static int NextFreeSlot(EquipmentInstance target)
    {
        return target != null ? target.FirstEmptySlot() : -1;
    }

    /// <summary>
    /// Entry point from <see cref="ItemUsage"/>. Validates that a target exists and hands off to
    /// the panel; until the panel is wired it reports instead of silently consuming the item.
    /// </summary>
    public static void BeginApply(ItemDefinition inscription, Inventory inventory)
    {
        if (inscription == null)
        {
            return;
        }

        List<EquipmentInstance> targets = FindTargets(inscription, inventory, Equipments.Instance);
        if (targets.Count == 0)
        {
            string message = $"No equipment that can take {inscription.DisplayName}";
            Debug.LogWarning($"InscriptionService: {message}.");
            if (UIManager.Instance != null && PlayerStats.Instance != null)
            {
                UIManager.Instance.DrawFlowupText(message, PlayerStats.Instance.transform.position);
            }
            return;
        }

        InscriptionPanel panel = InscriptionPanel.Instance;
        if (panel == null)
        {
            Debug.LogWarning($"InscriptionService: {targets.Count} valid target(s) for " +
                             $"{inscription.DisplayName}, but no inscription panel is present in the " +
                             "scene; nothing was applied and the item was not consumed.");
            return;
        }

        panel.Open(inscription, targets);
    }
}
