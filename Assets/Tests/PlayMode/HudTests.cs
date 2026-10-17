using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// The T08 additions in the real scene: the minimap learning where the player has been, and quick use costing
/// exactly one turn through the shared item path.
/// </summary>
public class HudTests
{
    private static int turnEvents;

    private static void CountTurnEvent(Controls controls)
    {
        turnEvents++;
    }

    [SetUp]
    public void SetUp()
    {
        turnEvents = 0;
        Controls.onMoveEvent += CountTurnEvent;
    }

    [TearDown]
    public void TearDown()
    {
        Controls.onMoveEvent -= CountTurnEvent;
    }

    [UnityTest]
    public IEnumerator WalkingTheFloor_GrowsTheMinimap()
    {
        yield return DungeonSceneFixture.LoadAndWaitForGeneration();

        // Use the scene's minimap when the HUD wiring has added one, otherwise give the test its own. A second
        // Minimap component refuses itself, so Instance is the only correct handle - holding the component the
        // test just added would read a Rules object nothing ever feeds.
        GameObject host = null;
        if (Minimap.Instance == null)
        {
            host = new GameObject("MinimapTest");
            host.AddComponent<Minimap>();
        }
        Minimap minimap = Minimap.Instance;
        Assert.IsTrue(minimap != null, "the scene should have a minimap after the wiring or the fallback above");

        Controls controls = DungeonSceneFixture.Player();
        Vector2Int start = DungeonSceneFixture.RegistryCell(controls);
        Vector2Int direction = DungeonSceneFixture.OpenDirection(start);
        Assert.AreNotEqual(Vector2Int.zero, direction, "the spawn cell has no walkable neighbour");

        yield return null;
        int before = minimap.Rules.VisitedCount;
        Assert.IsFalse(minimap.Rules.IsVisited(start),
            "a minimap created after generation has not seen the spawn cell yet");

        Assert.IsTrue(controls.TryMove(direction), "the step should succeed");
        yield return null;

        Assert.Greater(minimap.Rules.VisitedCount, before, "stepping onto a new cell reveals it");
        Assert.IsTrue(minimap.Rules.IsVisited(start + direction));

        if (host != null)
        {
            Object.Destroy(host);
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator QuickUse_ConsumesThePill_AndCostsExactlyOneTurn()
    {
        yield return DungeonSceneFixture.LoadAndWaitForGeneration();

        ItemDefinition pill = null;
        foreach (ItemDefinition definition in Resources.LoadAll<ItemDefinition>("ItemDefinitions"))
        {
            if (definition.Kind == ItemKind.Consumable)
            {
                pill = definition;
                break;
            }
        }
        Assert.IsTrue(pill != null, "the project should ship at least one consumable");

        Inventory inventory = Inventory.Instance;
        Assert.IsTrue(inventory != null, "the scene should have an Inventory");
        Assert.AreEqual(0, inventory.addItem(pill, 1), "the pill should fit in the inventory");
        int before = inventory.countOf(pill);

        var host = new GameObject("QuickUseTest");
        QuickUse quickUse = host.AddComponent<QuickUse>();
        quickUse.Select(new ItemStack(pill, 1));
        Assert.IsTrue(quickUse.HasSelection, "the pill is selected");

        Controls controls = DungeonSceneFixture.Player();
        turnEvents = 0;
        bool used = quickUse.TryUse(controls);

        Assert.IsTrue(used, "the pill should be used");
        Assert.AreEqual(before - 1, inventory.countOf(pill), "exactly one pill was consumed");
        Assert.AreEqual(1, turnEvents, "using an item costs one turn (GDD 6.2)");

        yield return DungeonSceneFixture.WaitForEnemyPhase();
        Assert.IsFalse(controls.takingTurn, "and the turn pipeline releases the lock afterwards");

        Object.Destroy(host);
    }

    [UnityTest]
    public IEnumerator QuickUse_WithNothingSelected_IsFree()
    {
        yield return DungeonSceneFixture.LoadAndWaitForGeneration();

        var host = new GameObject("QuickUseEmptyTest");
        QuickUse quickUse = host.AddComponent<QuickUse>();
        Assert.IsFalse(quickUse.HasSelection);

        turnEvents = 0;
        Assert.IsFalse(quickUse.TryUse(DungeonSceneFixture.Player()), "nothing selected, nothing used");
        Assert.AreEqual(0, turnEvents, "and nothing was spent");

        Object.Destroy(host);
    }
}
