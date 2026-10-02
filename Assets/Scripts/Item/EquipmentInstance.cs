using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One physical piece of equipment. <see cref="ItemDefinition"/> is a shared, immutable asset,
/// so it cannot carry per-item state: two Iron Swords in the inventory are the same asset.
/// Inscriptions belong to a specific sword, which means they need this instance layer.
///
/// Equipment never stacks (its stackLimit is 1), so one instance == one inventory slot.
/// </summary>
[Serializable]
public class EquipmentInstance
{
    /// <summary>How many inscriptions one piece of equipment can hold.</summary>
    public const int MaxInscriptions = 3;

    public ItemDefinition definition;

    /// <summary>
    /// Applied inscriptions, in the order they were applied. Never removed -- applying a fourth
    /// overwrites an existing slot.
    ///
    /// Read this through <see cref="Inscriptions"/>, never directly: Unity's deserializer does
    /// not run field initialisers for a class it constructs without a constructor, so a
    /// deserialized instance can hold a NULL list here despite the initialiser below.
    /// </summary>
    [SerializeField] private List<ItemDefinition> inscriptions = new List<ItemDefinition>();

    /// <summary>
    /// Parameterless constructor for Unity's serialiser.
    ///
    /// This is not optional: without it Unity cannot run the field initialisers above, and a
    /// deserialised instance comes back with a null <c>inscriptions</c> list. That is what
    /// crashed ItemDisplay.DescribeInscriptions in play.
    /// </summary>
    public EquipmentInstance()
    {
        inscriptions = new List<ItemDefinition>();
    }

    public EquipmentInstance(ItemDefinition definition) : this()
    {
        this.definition = definition;
    }

    /// <summary>
    /// The inscription list, repaired on first access. Covers instances Unity built without
    /// running a constructor, not just the ones this code creates with <c>new</c>.
    /// </summary>
    public List<ItemDefinition> Inscriptions
    {
        get
        {
            if (inscriptions == null)
            {
                inscriptions = new List<ItemDefinition>();
            }
            return inscriptions;
        }
    }

    public string DisplayName => definition != null ? definition.DisplayName : "(empty equipment)";

    public bool IsFull => Inscriptions.Count >= MaxInscriptions;

    /// <summary>Number of slots the UI should draw: always MaxInscriptions, filled or not.</summary>
    public int SlotCount => MaxInscriptions;

    public bool HasInscriptionAt(int slot)
    {
        return slot >= 0 && slot < Inscriptions.Count && Inscriptions[slot] != null;
    }

    public ItemDefinition GetInscriptionAt(int slot)
    {
        return HasInscriptionAt(slot) ? Inscriptions[slot] : null;
    }

    /// <summary>
    /// Apply an inscription to a specific slot, overwriting whatever was there. This is the only
    /// mutation path: inscriptions are never removed, only replaced.
    /// Returns false when the slot index is out of range or the inscription is null.
    /// </summary>
    public bool SetInscription(int slot, ItemDefinition inscription)
    {
        if (inscription == null || slot < 0 || slot >= MaxInscriptions)
        {
            return false;
        }

        // Grow to the slot being written so the list index always matches the UI slot index.
        while (Inscriptions.Count <= slot)
        {
            Inscriptions.Add(null);
        }

        Inscriptions[slot] = inscription;
        return true;
    }

    /// <summary>
    /// The first empty slot, or -1 when every slot is taken. Callers that want to overwrite
    /// explicitly must pick a slot themselves -- silently replacing an inscription would lose
    /// the player's investment without asking.
    /// </summary>
    public int FirstEmptySlot()
    {
        for (int i = 0; i < MaxInscriptions; i++)
        {
            if (!HasInscriptionAt(i))
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Sum of one bonus stat across this item's inscriptions, for effects that boost the item's
    /// own numbers (a sharper blade).
    /// </summary>
    public int ItemBonus(BonusStat stat)
    {
        int total = 0;
        foreach (ItemDefinition inscription in Inscriptions)
        {
            if (inscription != null
                && inscription.InscriptionEffect == InscriptionEffect.ItemStatBonus
                && inscription.InscriptionStat == stat)
            {
                total += inscription.InscriptionAmount;
            }
        }
        return total;
    }

    /// <summary>
    /// Sum of one bonus stat across this item's inscriptions, for effects that buff the wearer
    /// while the item is equipped.
    /// </summary>
    public int WearerBonus(BonusStat stat)
    {
        int total = 0;
        foreach (ItemDefinition inscription in Inscriptions)
        {
            if (inscription != null
                && inscription.InscriptionEffect == InscriptionEffect.PlayerStatBonus
                && inscription.InscriptionStat == stat)
            {
                total += inscription.InscriptionAmount;
            }
        }
        return total;
    }

    /// <summary>Damage contribution: the item's own damage plus any ItemStatBonus inscriptions.</summary>
    public int EffectiveDamage
    {
        get
        {
            if (definition == null)
            {
                return 0;
            }
            return definition.Damage + ItemBonus(BonusStat.Strength);
        }
    }

    /// <summary>Armor contribution: the item's own health bonus plus any ItemStatBonus inscriptions.</summary>
    public int EffectiveHealthBonus
    {
        get
        {
            if (definition == null)
            {
                return 0;
            }
            return definition.HealthBonus + ItemBonus(BonusStat.MaxHealth);
        }
    }

    public override string ToString()
    {
        return $"{DisplayName} ({Inscriptions.Count}/{MaxInscriptions} inscriptions)";
    }
}
