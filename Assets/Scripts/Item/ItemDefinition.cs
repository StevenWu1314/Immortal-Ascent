using System.Collections.Generic;
using UnityEngine;

/// <summary>What an item is for, which decides how <see cref="ItemDefinition.Use"/> behaves.</summary>
public enum ItemKind
{
    Material,
    Weapon,
    Armor,
    Ammo,
    Consumable,
    /// <summary>Applied to a specific piece of equipment to add a permanent effect.</summary>
    Inscription
}

/// <summary>A stat an inscription can modify, on the item itself or on the wearer.</summary>
public enum BonusStat
{
    Strength,
    Dexterity,
    MaxHealth
}

/// <summary>What an inscription does once applied to a piece of equipment.</summary>
public enum InscriptionEffect
{
    /// <summary>Pure numbers: raises the item's own damage (weapons) or health bonus (armor).</summary>
    ItemStatBonus,

    /// <summary>Permanent positive effect on the wearer while the item is equipped.</summary>
    PlayerStatBonus
}

/// <summary>Which equipment an inscription may be applied to.</summary>
public enum InscriptionTarget
{
    AnyEquipment,
    MeleeWeapon,
    RangedWeapon,
    Armor
}

/// <summary>Which player effect a consumable applies.</summary>
public enum ConsumableStat
{
    Strength,
    Dexterity,
    MaxHealth,
    /// <summary>Restores health immediately instead of applying a timed buff.</summary>
    Heal
}

/// <summary>
/// Immutable, shared data for one kind of item. This is the single source of truth for what
/// an item *is*; how many of it the player holds lives in <see cref="ItemStack"/>.
///
/// It replaces the old design where <c>Item : MonoBehaviour</c> was both the asset definition
/// and the runtime inventory entry: that forced inventory copies to be made with
/// MemberwiseClone (which cannot clone a Unity object), left stackLimit with no reader, and
/// made item identity depend on a mutable display string.
/// </summary>
[CreateAssetMenu(fileName = "ItemDefinition", menuName = "ScriptableObjects/Item Definition", order = 1)]
public class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [TextArea(2, 5)]
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;

    [Header("Stacking")]
    [Tooltip("Maximum units in a single inventory stack. 1 means the item never stacks.")]
    [SerializeField] private int stackLimit = 1;

    [Header("Kind")]
    [SerializeField] private ItemKind kind = ItemKind.Material;

    [Header("Weapon")]
    [SerializeField] private int damage;
    [SerializeField] private bool ranged;
    [Tooltip("Reach in tiles. Only meaningful when Ranged is set.")]
    [SerializeField] private int range;

    [Header("Armor")]
    [SerializeField] private int healthBonus;

    [Header("Consumable")]
    [SerializeField] private ConsumableStat stat = ConsumableStat.Strength;
    [SerializeField] private int effectAmount;
    [SerializeField] private int effectDuration;

    [Header("Inscription")]
    [Tooltip("What this inscription does once applied.")]
    [SerializeField] private InscriptionEffect inscriptionEffect = InscriptionEffect.ItemStatBonus;
    [Tooltip("Which stat the bonus applies to.")]
    [SerializeField] private BonusStat inscriptionStat = BonusStat.Strength;
    [Tooltip("How much of the stat an ItemStatBonus or PlayerStatBonus grants.")]
    [SerializeField] private int inscriptionAmount;
    [Tooltip("Which equipment this inscription may be applied to.")]
    [SerializeField] private InscriptionTarget inscriptionTarget = InscriptionTarget.AnyEquipment;

    [Header("Crafting")]
    [Tooltip("Materials consumed to craft this item. Leave empty for items that cannot be crafted.")]
    [SerializeField] private List<Ingredient> recipe = new List<Ingredient>();

    /// <summary>One material plus how many units the recipe consumes of it.</summary>
    [System.Serializable]
    public struct Ingredient
    {
        public ItemDefinition material;
        public int amount;
    }

    public string Id => string.IsNullOrEmpty(id) ? name : id;

    /// <summary>Falls back to the asset name so an item can never render as an empty label.</summary>
    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

    public string Description => description;
    public Sprite Icon => icon;

    /// <summary>Never below 1: a zero or negative limit would make the item unholdable.</summary>
    public int StackLimit => Mathf.Max(1, stackLimit);

    public ItemKind Kind => kind;
    public int Damage => damage;
    public bool Ranged => ranged;

    /// <summary>Reach in tiles, or 0 for anything that is not a ranged weapon.</summary>
    public int Range => ranged ? range : 0;

    public int HealthBonus => healthBonus;
    public ConsumableStat Stat => stat;
    public int EffectAmount => effectAmount;
    public int EffectDuration => effectDuration;

    public InscriptionEffect InscriptionEffect => inscriptionEffect;
    public BonusStat InscriptionStat => inscriptionStat;
    public int InscriptionAmount => inscriptionAmount;
    public InscriptionTarget InscriptionTarget => inscriptionTarget;


    /// <summary>
    /// May this inscription be applied to the given equipment definition?
    /// </summary>
    public bool CanApplyTo(ItemDefinition equipment)
    {
        if (equipment == null)
        {
            return false;
        }
        switch (inscriptionTarget)
        {
            case InscriptionTarget.MeleeWeapon:
                return equipment.Kind == ItemKind.Weapon && !equipment.Ranged;
            case InscriptionTarget.RangedWeapon:
                return equipment.Kind == ItemKind.Weapon && equipment.Ranged;
            case InscriptionTarget.Armor:
                return equipment.Kind == ItemKind.Armor;
            default:
                return equipment.Kind == ItemKind.Weapon || equipment.Kind == ItemKind.Armor;
        }
    }

    /// <summary>
    /// Label for the inscription's effect, shown in the inscription UI.
    /// </summary>
    public string InscriptionDescription
    {
        get
        {
            string statName = inscriptionStat == BonusStat.MaxHealth
                ? "Max Health"
                : inscriptionStat.ToString();

            switch (inscriptionEffect)
            {
                case InscriptionEffect.ItemStatBonus:
                    return $"+{inscriptionAmount} {statName} to the item";
                case InscriptionEffect.PlayerStatBonus:
                    return $"+{inscriptionAmount} {statName} while equipped";
                default:
                    return "";
            }
        }
    }

    /// <summary>Row label for the item info panel; empty means the row is hidden.</summary>
    public string DisplayLabel
    {
        get
        {
            switch (kind)
            {
                case ItemKind.Weapon: return "Damage";
                case ItemKind.Armor: return "Health";
                case ItemKind.Consumable:
                    return StatLabel + " for " + effectDuration + " turns";
                default: return "";
            }
        }
    }

    /// <summary>Value paired with <see cref="DisplayLabel"/>.</summary>
    public int DisplayValue
    {
        get
        {
            switch (kind)
            {
                case ItemKind.Weapon: return damage;
                case ItemKind.Armor: return healthBonus;
                case ItemKind.Consumable: return effectAmount;
                default: return 0;
            }
        }
    }

    private string StatLabel
    {
        get
        {
            switch (stat)
            {
                case ConsumableStat.MaxHealth: return "Max Health";
                case ConsumableStat.Heal: return "Heal";
                default: return stat.ToString();
            }
        }
    }

    /// <summary>
    /// Display spelling of the consumable's stat, used by the item panel and by
    /// PlayerStats.increaseStatTemp (which keys off "Max Health", not the enum name).
    /// </summary>
    public string StatDisplayName => StatLabel;

    // ---------------------------------------------------------------------------------
    // CRAFTING. An item with an empty recipe simply cannot be crafted; everything else
    // (materials, amounts, and the craft itself) lives on this asset.
    // ---------------------------------------------------------------------------------

    /// <summary>True when this item has a recipe at all.</summary>
    public bool IsCraftable => recipe != null && recipe.Count > 0;

    /// <summary>Can this item be crafted right now, from the player's inventory?</summary>
    public bool CanCraft()
    {
        // Quiet: the crafting panel asks this once per craftable item, so a missing material
        // must not log here.
        return TryResolveMaterials(out _, false);
    }

    /// <summary>
    /// Consume the materials and put one of this item into the inventory. The whole recipe and
    /// the output space are validated before anything is removed, so a missing material or a full
    /// inventory can never eat the inputs.
    /// </summary>
    public void Craft()
    {
        Inventory inventory = Inventory.Instance;
        if (inventory == null)
        {
            Debug.LogError($"ItemDefinition '{name}': no Inventory instance; nothing was crafted.");
            return;
        }
        if (!TryResolveMaterials(out List<(ItemDefinition material, int amount)> toRemove, true))
        {
            return;
        }
        if (!inventory.hasRoomFor(this, 1))
        {
            Debug.LogWarning($"ItemDefinition '{name}': inventory has no room for {DisplayName}; " +
                             "materials were not consumed.");
            return;
        }

        foreach (var (material, amount) in toRemove)
        {
            inventory.removeItem(material, amount);
        }
        inventory.addItem(this, 1);
        Debug.Log($"Crafted: {DisplayName}");
    }

    /// <summary>
    /// Work out which definitions satisfy this recipe without changing anything. Entries for the
    /// same material are summed first, so two lines asking for 5 Iron each require 10 in stock
    /// instead of passing two independent "has 5" checks and then under-consuming.
    /// </summary>
    private bool TryResolveMaterials(out List<(ItemDefinition material, int amount)> toRemove, bool logMissing)
    {
        toRemove = new List<(ItemDefinition, int)>();

        if (!IsCraftable)
        {
            if (logMissing)
            {
                Debug.LogWarning($"ItemDefinition '{name}' has no recipe; refusing to craft.");
            }
            return false;
        }

        Inventory inventory = Inventory.Instance;
        if (inventory == null)
        {
            return false;
        }

        Dictionary<ItemDefinition, int> demand = new Dictionary<ItemDefinition, int>();
        foreach (Ingredient entry in recipe)
        {
            if (entry.material == null)
            {
                Debug.LogError($"ItemDefinition '{name}': a recipe entry has no ItemDefinition assigned.");
                return false;
            }
            demand.TryGetValue(entry.material, out int total);
            demand[entry.material] = total + entry.amount;
        }

        foreach (var entry in demand)
        {
            if (inventory.countOf(entry.Key) < entry.Value)
            {
                if (logMissing)
                {
                    Debug.Log($"Missing material: {entry.Key.DisplayName} (need {entry.Value})");
                }
                return false;
            }
            toRemove.Add((entry.Key, entry.Value));
        }
        return true;
    }
}

