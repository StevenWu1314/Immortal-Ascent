using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Crafting rules (T07, GDD 3.6/12.5). Pure: recipes are built by hand, so no assets or scene are needed.
/// </summary>
public class CraftingRulesTests
{
    private ItemDefinition iron;
    private ItemDefinition silk;
    private ItemDefinition sword;

    [SetUp]
    public void SetUp()
    {
        iron = ScriptableObject.CreateInstance<ItemDefinition>();
        silk = ScriptableObject.CreateInstance<ItemDefinition>();
        sword = ScriptableObject.CreateInstance<ItemDefinition>();
        RecipeDiscovery.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        RecipeDiscovery.Reset();
    }

    private static List<ItemDefinition.Ingredient> Recipe(params (ItemDefinition material, int amount)[] entries)
    {
        var recipe = new List<ItemDefinition.Ingredient>();
        foreach (var (material, amount) in entries)
        {
            recipe.Add(new ItemDefinition.Ingredient { material = material, amount = amount });
        }
        return recipe;
    }

    // ------------------------------------------------------------------ availability

    [Test]
    public void Crafting_IsOutOfCombatOnly()
    {
        Assert.IsFalse(CraftingRules.IsCraftingAllowed(1, 3), "a living enemy on the island is combat");
        Assert.IsTrue(CraftingRules.IsCraftingAllowed(1, 0), "a cleared room is out of combat");
        Assert.IsTrue(CraftingRules.IsCraftingAllowed(IslandManager.Passage, 0), "corridors are never combat");
    }

    // ------------------------------------------------------------------ affordability

    [Test]
    public void ExactlyEnoughMaterials_IsAffordable()
    {
        List<ItemDefinition.Ingredient> recipe = Recipe((iron, 5));

        Assert.IsTrue(CraftingRules.CanAfford(recipe, _ => 5, out string detail), detail);
        Assert.IsNull(detail);
    }

    [Test]
    public void OneShort_IsNotAffordable_AndSaysWhatIsMissing()
    {
        List<ItemDefinition.Ingredient> recipe = Recipe((iron, 5));

        Assert.IsFalse(CraftingRules.CanAfford(recipe, _ => 4, out string detail));
        StringAssert.Contains("1", detail);
    }

    [Test]
    public void TwoEntriesForTheSameMaterial_AreSummed()
    {
        // 3 + 4 Iron needs 7 in stock: two independent "has 3" checks would let 5 pass and under-consume.
        List<ItemDefinition.Ingredient> recipe = Recipe((iron, 3), (iron, 4));

        Assert.IsFalse(CraftingRules.CanAfford(recipe, _ => 5, out _));
        Assert.IsTrue(CraftingRules.CanAfford(recipe, _ => 7, out _));

        Dictionary<ItemDefinition, int> missing = CraftingRules.Missing(recipe, _ => 5);
        Assert.AreEqual(2, missing[iron], "5 of 7 leaves 2 missing");
    }

    // ------------------------------------------------------------------ the three states

    [Test]
    public void AnUndiscoveredRecipe_IsLocked()
    {
        CraftState state = CraftingRules.StateOf(Recipe((iron, 1)), revealed: false, _ => 99, hasRoom: true,
            out string detail);

        Assert.AreEqual(CraftState.Locked, state);
        Assert.IsFalse(string.IsNullOrEmpty(detail));
    }

    [Test]
    public void ADiscoveredButUnownedRecipe_IsGreyed()
    {
        CraftState state = CraftingRules.StateOf(Recipe((iron, 5)), revealed: true, _ => 1, hasRoom: true,
            out string detail);

        Assert.AreEqual(CraftState.Greyed, state);
        StringAssert.Contains("Missing", detail);
    }

    [Test]
    public void ADiscoveredAffordableRecipeWithNoRoom_IsGreyed()
    {
        CraftState state = CraftingRules.StateOf(Recipe((iron, 1)), revealed: true, _ => 5, hasRoom: false,
            out string detail);

        Assert.AreEqual(CraftState.Greyed, state);
        StringAssert.Contains("full", detail);
    }

    [Test]
    public void ADiscoveredAffordableRecipeWithRoom_IsEnabled()
    {
        CraftState state = CraftingRules.StateOf(Recipe((iron, 1)), revealed: true, _ => 5, hasRoom: true,
            out string detail);

        Assert.AreEqual(CraftState.Enabled, state);
        Assert.IsNull(detail);
    }

    // ------------------------------------------------------------------ discovery

    [Test]
    public void ObtainingAMaterial_RevealsTheRecipesThatUseIt()
    {
        Assert.IsTrue(CraftingRules.Reveals(Recipe((iron, 5)), iron));
        Assert.IsFalse(CraftingRules.Reveals(Recipe((iron, 5)), silk), "an unrelated material reveals nothing");
        Assert.IsFalse(CraftingRules.Reveals(null, iron), "a recipe-less item cannot reveal anything");
    }

    [Test]
    public void Discovery_IsPerRunOnly()
    {
        Assert.AreEqual(0, RecipeDiscovery.Count);
        Assert.IsFalse(RecipeDiscovery.IsRevealed(sword));

        RecipeDiscovery.Reset();

        Assert.AreEqual(0, RecipeDiscovery.Count, "nothing is persisted between runs");
    }

    // ------------------------------------------------------------------ atomicity

    [Test]
    public void AFailedCraft_ReportsWhy_AndGrantsNothing()
    {
        // No Inventory instance in this test, which is itself a failure path, and no recipe at all.
        bool crafted = CraftingRules.TryCraft(sword, null, out int arrows, out string failure);

        Assert.IsFalse(crafted);
        Assert.AreEqual(0, arrows);
        Assert.IsFalse(string.IsNullOrEmpty(failure));
    }

    [Test]
    public void ARecipeLessItem_CannotBeCrafted()
    {
        Assert.IsFalse(CraftingRules.TryCraft(sword, null, out _, out string failure));
        StringAssert.Contains("nothing to craft", failure);
    }
}
