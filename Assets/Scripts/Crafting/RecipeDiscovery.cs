using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Which recipes this run has revealed (T07, GDD 12.5).
///
/// A recipe is revealed the first time the player handles any material it uses, and stays revealed for the
/// rest of the run. Nothing is persisted: permanent blueprint unlocks are M2/M3, so a new run starts with
/// every recipe undiscovered.
/// </summary>
public static class RecipeDiscovery
{
    private static readonly HashSet<ItemDefinition> revealed = new HashSet<ItemDefinition>();
    private static List<ItemDefinition> cachedDefinitions;

    public static int Count => revealed.Count;

    public static bool IsRevealed(ItemDefinition recipe)
    {
        return recipe != null && revealed.Contains(recipe);
    }

    /// <summary>
    /// Note that the player obtained <paramref name="obtained"/>. Returns the recipes this revealed, so the
    /// caller can announce them.
    /// </summary>
    public static List<ItemDefinition> Observe(ItemDefinition obtained)
    {
        if (cachedDefinitions == null)
        {
            cachedDefinitions = new List<ItemDefinition>();
            foreach (ItemDefinition definition in Resources.LoadAll<ItemDefinition>("ItemDefinitions"))
            {
                if (definition != null && definition.IsCraftable)
                {
                    cachedDefinitions.Add(definition);
                }
            }
        }
        return Observe(obtained, cachedDefinitions);
    }

    /// <summary>The testable shape: hand it the candidate recipes instead of letting it scan Resources.</summary>
    public static List<ItemDefinition> Observe(ItemDefinition obtained, IEnumerable<ItemDefinition> candidates)
    {
        var newlyRevealed = new List<ItemDefinition>();
        if (obtained == null || candidates == null)
        {
            return newlyRevealed;
        }

        foreach (ItemDefinition candidate in candidates)
        {
            if (candidate == null || !candidate.IsCraftable || revealed.Contains(candidate))
            {
                continue;
            }
            if (!CraftingRules.Reveals(candidate.Recipe, obtained))
            {
                continue;
            }
            revealed.Add(candidate);
            newlyRevealed.Add(candidate);
        }

        return newlyRevealed;
    }

    /// <summary>Forget everything: a new run starts undiscovered, and editor tooling can force a re-scan.</summary>
    public static void Reset()
    {
        revealed.Clear();
        cachedDefinitions = null;
    }
}
