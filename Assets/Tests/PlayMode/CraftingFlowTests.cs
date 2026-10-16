using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Crafting in the real scene (T07, GDD 3.6/12.5): out of combat only, the panel closes when a fight starts,
/// and the arrow recipe feeds the quiver count rather than a stack.
/// </summary>
public class CraftingFlowTests
{
    [UnityTest]
    public IEnumerator Crafting_IsRefusedWhileAnEnemyHoldsThePlayersIsland()
    {
        yield return DungeonSceneFixture.LoadAndWaitForGeneration();

        Controls controls = DungeonSceneFixture.Player();
        int island = IslandManager.GetIsland(DungeonSceneFixture.RegistryCell(controls));
        Assert.AreEqual(0, IslandManager.CountLivingEnemies(island), "the spawn island starts clear");
        Assert.IsTrue(CraftingRules.IsCraftingAllowedHere(), "out of combat, crafting is allowed");

        // An enemy on the player's island is combat, so crafting is refused.
        EnemyBehavior enemy = Object.FindObjectsByType<EnemyBehavior>(FindObjectsSortMode.None)[0];
        Vector2Int enemyHome = new Vector2Int(
            Mathf.FloorToInt(enemy.transform.position.x), Mathf.FloorToInt(enemy.transform.position.y));
        Vector2Int moved = DungeonSceneFixture.OpenDirection(DungeonSceneFixture.RegistryCell(controls));
        Vector2Int target = DungeonSceneFixture.RegistryCell(controls) + moved;

        EntityManager.Instance.UnregisterEntity(enemy.gameObject);
        enemy.transform.position = new Vector3(target.x, target.y, enemy.transform.position.z);
        EntityManager.Instance.RegisterEntity(enemy.gameObject, target);
        yield return null;

        Assert.AreEqual(1, IslandManager.CountLivingEnemies(island), "the enemy now shares the island");
        Assert.IsFalse(CraftingRules.IsCraftingAllowedHere(), "an enemy in the room refuses crafting");

        // Clearing it allows crafting again. Put the enemy back where it came from first.
        EntityManager.Instance.UnregisterEntity(enemy.gameObject);
        enemy.transform.position = new Vector3(enemyHome.x, enemyHome.y, enemy.transform.position.z);
        EntityManager.Instance.RegisterEntity(enemy.gameObject, enemyHome);
        Object.Destroy(enemy.gameObject);
        yield return null;

        Assert.IsTrue(CraftingRules.IsCraftingAllowedHere(), "with the room clear, crafting is allowed again");
    }

    [UnityTest]
    public IEnumerator CraftingArrows_ConsumesOneOre_AndAddsTenArrows()
    {
        yield return DungeonSceneFixture.LoadAndWaitForGeneration();

        ItemDefinition iron = Resources.Load<ItemDefinition>("ItemDefinitions/Materials/Iron");
        Assert.IsTrue(iron != null, "the project should ship an Iron material");

        // Arrows ship as an Ammo definition; give that one a 1-ore recipe, because the recipe data itself is
        // still on the designer hand-over list. Setting it on the loaded asset only affects this play session.
        ItemDefinition arrows = Resources.Load<ItemDefinition>("ItemDefinitions/Consumables/Arrow");
        Assert.IsTrue(arrows != null, "the project should ship an Arrow (Ammo) definition");
        Assert.AreEqual(ItemKind.Ammo, arrows.Kind, "the arrow path keys off the item kind");
        arrows.SetRecipe(new List<ItemDefinition.Ingredient>
        {
            new ItemDefinition.Ingredient { material = iron, amount = 1 }
        });

        Inventory inventory = Inventory.Instance;
        Assert.IsTrue(inventory != null, "the scene should have an Inventory");
        while (inventory.addItem(iron, 1) != 0)
        {
            Assert.Fail("the inventory should have room for a single ore");
        }

        int oreBefore = inventory.countOf(iron);
        int arrowsBefore = PlayerStats.Instance.Arrows;
        Assert.Greater(oreBefore, 0, "the ore has to be in the inventory for this to mean anything");

        bool crafted = CraftingRules.TryCraft(arrows, inventory, out int granted, out string failure);

        Assert.IsTrue(crafted, failure);
        Assert.AreEqual(ArrowEconomy.ArrowsPerOre, granted);
        Assert.AreEqual(arrowsBefore + ArrowEconomy.ArrowsPerOre, PlayerStats.Instance.Arrows,
            "arrows land in the quiver count");
        Assert.AreEqual(oreBefore - 1, inventory.countOf(iron), "exactly one ore was consumed");
    }

    [UnityTest]
    public IEnumerator EnteringCombat_ClosesTheCraftingPanel()
    {
        yield return DungeonSceneFixture.LoadAndWaitForGeneration();

        CraftingManager crafting = Object.FindFirstObjectByType<CraftingManager>();
        Assert.IsTrue(crafting != null, "the dungeon scene should carry the crafting manager");
        Assert.IsTrue(crafting.CraftingPanel != null, "the crafting panel should be assigned");

        crafting.CraftingPanel.SetActive(true);
        Assert.IsTrue(crafting.CraftingPanel.activeSelf);

        // Put an enemy in the player's room and spend a turn: the move raises onMoveEvent, which is what the
        // manager watches.
        Controls controls = DungeonSceneFixture.Player();
        Vector2Int here = DungeonSceneFixture.RegistryCell(controls);
        Vector2Int step = DungeonSceneFixture.OpenDirection(here);
        EnemyBehavior enemy = Object.FindObjectsByType<EnemyBehavior>(FindObjectsSortMode.None)[0];
        EntityManager.Instance.UnregisterEntity(enemy.gameObject);
        enemy.transform.position = new Vector3(here.x + step.x, here.y + step.y, enemy.transform.position.z);
        EntityManager.Instance.RegisterEntity(enemy.gameObject, here + step);

        Assert.IsFalse(CraftingRules.IsCraftingAllowedHere(), "the room is now in combat");
        // Step in the opposite direction, away from the enemy in the destination cell.
        controls.TryMove(new Vector2Int(-step.x, -step.y));
        yield return DungeonSceneFixture.WaitForEnemyPhase();

        Assert.IsFalse(crafting.CraftingPanel.activeSelf, "a fight closes the crafting panel");
    }
}
