using UnityEngine;

/// <summary>
/// What the player currently has equipped. Each slot holds an <see cref="EquipmentInstance"/>
/// rather than an <see cref="ItemDefinition"/>, so the inscriptions applied to "this sword"
/// travel with the sword and not with the shared asset.
/// </summary>
public class Equipments : MonoBehaviour
{
    [SerializeField] private GameObject inventoryContainer;
    [SerializeField] public EquipmentInstance meleeWeapon;
    [SerializeField] public EquipmentInstance rangeWeapon;
    [SerializeField] public EquipmentInstance armor;
    [SerializeField] private GameObject ItemContainer;
    [SerializeField] private ItemDisplay itemDisplay;

    public static Equipments Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Equip an instance into the slot its definition belongs to. Taking the instance (not the
    /// definition) is what preserves the item's inscriptions across equip/unequip cycles.
    /// </summary>
    public void Equip(EquipmentInstance instance)
    {
        if (instance == null || instance.definition == null)
        {
            Debug.LogWarning("Equipments.Equip called with a null instance; ignoring.");
            return;
        }

        switch (instance.definition.Kind)
        {
            case ItemKind.Weapon:
                if (instance.definition.Ranged)
                {
                    rangeWeapon = instance;
                }
                else
                {
                    meleeWeapon = instance;
                }
                break;

            case ItemKind.Armor:
                armor = instance;
                break;

            default:
                Debug.LogError($"Equipments: '{instance.DisplayName}' " +
                               $"({instance.definition.Kind}) is not equippable.");
                return;
        }

        // Stats are fully recomputed from the equipped set rather than incremented/decremented.
        // The old add/subtract bookkeeping is what let MaxHealth drift when a swap went wrong.
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.RecalculateStats();
        }
    }

    /// <summary>
    /// Reach in tiles of the currently equipped ranged weapon, or 0 when none is equipped.
    /// Range belongs to the weapon; reading it here is what stops equipping a melee weapon
    /// from zeroing the bow's reach.
    /// </summary>
    public int getRange()
    {
        return rangeWeapon != null && rangeWeapon.definition != null ? rangeWeapon.definition.Range : 0;
    }

    /// <summary>Melee damage including the weapon's own ItemStatBonus inscriptions.</summary>
    public int GetMeleeDamage()
    {
        return meleeWeapon != null ? meleeWeapon.EffectiveDamage : 0;
    }

    /// <summary>Ranged damage including the weapon's own ItemStatBonus inscriptions.</summary>
    public int GetRangedDamage()
    {
        return rangeWeapon != null ? rangeWeapon.EffectiveDamage : 0;
    }

    /// <summary>Max-health contribution of the equipped armor, including its inscriptions.</summary>
    public int GetArmorHealthBonus()
    {
        return armor != null ? armor.EffectiveHealthBonus : 0;
    }

    /// <summary>
    /// Sum of a PlayerStatBonus inscription stat across everything currently equipped. Called by
    /// PlayerStats.RecalculateStats, which is why it must include every slot.
    /// </summary>
    public int GetWearerBonus(BonusStat stat)
    {
        int total = 0;
        if (meleeWeapon != null)
        {
            total += meleeWeapon.WearerBonus(stat);
        }
        if (rangeWeapon != null)
        {
            total += rangeWeapon.WearerBonus(stat);
        }
        if (armor != null)
        {
            total += armor.WearerBonus(stat);
        }
        return total;
    }

    /// <summary>Total inscriptions currently granting an effect, for the diagnostics/UI readout.</summary>
    public int CountInscriptions()
    {
        int total = 0;
        if (meleeWeapon != null) total += meleeWeapon.Inscriptions.Count;
        if (rangeWeapon != null) total += rangeWeapon.Inscriptions.Count;
        if (armor != null) total += armor.Inscriptions.Count;
        return total;
    }
}
