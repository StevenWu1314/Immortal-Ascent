using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>How a recipe appears in the crafting panel (GDD 12.5).</summary>
public enum CraftState
{
    /// <summary>Not discovered this run: shown blacked out, never craftable.</summary>
    Locked,

    /// <summary>Discovered but missing materials or inventory room: shown greyed, with the reason.</summary>
    Greyed,

    /// <summary>Discovered, affordable and there is room: craftable.</summary>
    Enabled
}

/// <summary>
/// The crafting rules (T07, GDD 3.6/12.5). Pure wherever it can be: the recipe is taken as a material list, so
/// the maths is testable without an ItemDefinition, a scene or an Inventory.
/// </summary>
public static class CraftingRules
{
    /// <summary>
    /// GDD 3.6: crafting is out of combat only. "In combat" is T03's predicate - never a second definition.
    /// </summary>
    public static bool IsCraftingAllowed(int islandId, int livingEnemiesOnIsland)
    {
        return !TurnRules.IsTurnBased(islandId, livingEnemiesOnIsland);
    }

    public static bool IsCraftingAllowedHere()
    {
        Vector3 position = PlayerStats.Instance != null ? PlayerStats.Instance.transform.position : Vector3.zero;
        int island = IslandManager.GetIsland(position);
        return IsCraftingAllowed(island, IslandManager.CountLivingEnemies(island));
    }

    /// <summary>Does obtaining <paramref name="obtained"/> reveal a recipe that uses it as a material?</summary>
    public static bool Reveals(IReadOnlyList<ItemDefinition.Ingredient> recipe, ItemDefinition obtained)
    {
        if (recipe == null || obtained == null)
        {
            return false;
        }
        foreach (ItemDefinition.Ingredient entry in recipe)
        {
            if (entry.material == obtained)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// What is still missing, summed per material: two lines asking for 5 Iron each need 10 in stock, not two
    /// independent checks that then under-consume.
    /// </summary>
    public static Dictionary<ItemDefinition, int> Missing(
        IReadOnlyList<ItemDefinition.Ingredient> recipe, Func<ItemDefinition, int> countOf)
    {
        var missing = new Dictionary<ItemDefinition, int>();
        if (recipe == null || countOf == null)
        {
            return missing;
        }

        var demand = new Dictionary<ItemDefinition, int>();
        foreach (ItemDefinition.Ingredient entry in recipe)
        {
            if (entry.material == null)
            {
                continue;
            }
            demand.TryGetValue(entry.material, out int total);
            demand[entry.material] = total + Mathf.Max(1, entry.amount);
        }

        foreach (var pair in demand)
        {
            int have = Mathf.Max(0, countOf(pair.Key));
            if (have < pair.Value)
            {
                missing[pair.Key] = pair.Value - have;
            }
        }
        return missing;
    }

    public static bool CanAfford(IReadOnlyList<ItemDefinition.Ingredient> recipe, Func<ItemDefinition, int> countOf,
                                 out string detail)
    {
        detail = Describe(Missing(recipe, countOf));
        return string.IsNullOrEmpty(detail);
    }

    /// <summary>A readable "missing 3 Iron Ore" line, or null when nothing is missing.</summary>
    public static string Describe(Dictionary<ItemDefinition, int> missing)
    {
        if (missing == null || missing.Count == 0)
        {
            return null;
        }

        var parts = new List<string>();
        foreach (var pair in missing)
        {
            parts.Add($"{pair.Value} x {pair.Key.DisplayName}");
        }
        return "Missing " + string.Join(", ", parts);
    }

    /// <summary>The three UI states from the three inputs that decide them (GDD 12.5).</summary>
    public static CraftState StateOf(IReadOnlyList<ItemDefinition.Ingredient> recipe, bool revealed,
                                     Func<ItemDefinition, int> countOf, bool hasRoom, out string detail)
    {
        detail = null;
        if (!revealed)
        {
            detail = "Undiscovered recipe";
            return CraftState.Locked;
        }
        if (!CanAfford(recipe, countOf, out detail))
        {
            return CraftState.Greyed;
        }
        if (!hasRoom)
        {
            detail = "Inventory full";
            return CraftState.Greyed;
        }
        return CraftState.Enabled;
    }

    /// <summary>
    /// Craft one, atomically: the whole recipe and the output space are validated before anything is consumed,
    /// so a failure leaves the inventory untouched.
    ///
    /// Ammo is special. Arrows are a count rather than item stacks (T06), so crafting an Ammo definition
    /// grants arrows and adds no item.
    /// </summary>
    public static bool TryCraft(ItemDefinition recipe, Inventory inventory, out int arrowsGranted, out string failure)
    {
        arrowsGranted = 0;
        failure = null;

        if (recipe == null || !recipe.IsCraftable)
        {
            failure = "nothing to craft";
            return false;
        }
        if (inventory == null)
        {
            failure = "no inventory";
            return false;
        }

        if (recipe.Kind == ItemKind.Ammo)
        {
            if (!recipe.ConsumeMaterials())
            {
                failure = "not enough materials";
                return false;
            }
            arrowsGranted = ArrowEconomy.ArrowsPerOre;
            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.AddArrows(arrowsGranted);
            }
            return true;
        }

        if (!recipe.CanCraft())
        {
            failure = "not enough materials";
            return false;
        }
        if (!inventory.hasRoomFor(recipe, 1))
        {
            failure = "inventory full";
            return false;
        }

        recipe.Craft();
        return true;
    }
}
