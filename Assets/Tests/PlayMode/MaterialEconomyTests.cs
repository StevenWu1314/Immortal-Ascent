using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// The material economy in the real scene (T06): mining an ore vein and a chest that cannot hand everything
/// over. Run in batchmode or from the Test Runner window, never through the bridge (see PlayModeSmokeTests).
/// </summary>
public class MaterialEconomyTests
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
    public IEnumerator MiningAnAdjacentVein_CostsATurn_YieldsOre_AndDepletesIt()
    {
        yield return DungeonSceneFixture.LoadAndWaitForGeneration();

        Controls controls = DungeonSceneFixture.Player();
        ItemDefinition iron = Resources.Load<ItemDefinition>("ItemDefinitions/Materials/Iron");
        Assert.IsTrue(iron != null, "the project should ship an Iron material");

        Vector2Int start = DungeonSceneFixture.RegistryCell(controls);
        Vector2Int direction = DungeonSceneFixture.OpenDirection(start);
        Assert.AreNotEqual(Vector2Int.zero, direction, "the spawn cell has no free neighbour for a vein");
        Vector2Int vein = start + direction;

        // Plant a vein exactly as the generator does: solid terrain plus a registry entry.
        Grid grid = Grid.Current;
        grid.SetValueAtLocation(vein.x, vein.y, OreVeinRegistry.OreCode);
        OreVeinRegistry.Register(vein, iron, 3);

        int before = Inventory.Instance != null ? Inventory.Instance.countOf(iron) : 0;
        turnEvents = 0;

        Assert.IsTrue(controls.TryMineAdjacentVein(), "standing next to a vein should mine it");
        Assert.AreEqual(1, turnEvents, "mining costs one turn (GDD 5.5)");

        int yielded = 3 - OreVeinRegistry.Remaining(vein);
        Assert.GreaterOrEqual(yielded, 1);
        Assert.LessOrEqual(yielded, 3);
        Assert.AreEqual(before + yielded, Inventory.Instance.countOf(iron), "the ore reaches the inventory");

        // Keep mining until it runs out; each swing needs the previous turn to resolve first.
        for (int attempt = 0; attempt < 6 && OreVeinRegistry.HasVein(vein); attempt++)
        {
            yield return DungeonSceneFixture.WaitForEnemyPhase();
            controls.TryMineAdjacentVein();
        }

        Assert.IsFalse(OreVeinRegistry.HasVein(vein), "three ore at 1-3 a swing cannot last forever");
        Assert.AreEqual(0, grid.GetValueAtLocation(vein.x, vein.y), "a depleted vein opens its cell");
        Assert.IsTrue(Grid.IsWalkableTerrain(grid.GetValueAtLocation(vein.x, vein.y)));

        // The cell itself is done. (An adjacent *generated* vein could still be mineable, so this asks the
        // registry about this vein rather than about whatever happens to be next to the player.)
        yield return DungeonSceneFixture.WaitForEnemyPhase();
        Assert.IsFalse(OreVeinRegistry.TryMine(vein, grid, RunState.Rng, out _, out int nothing));
        Assert.AreEqual(0, nothing);
    }

    [UnityTest]
    public IEnumerator AChest_KeepsWhatDidNotFit_UntilThereIsRoom()
    {
        yield return DungeonSceneFixture.LoadAndWaitForGeneration();

        Chest chest = null;
        foreach (Chest candidate in Object.FindObjectsByType<Chest>(FindObjectsSortMode.None))
        {
            if (!candidate.IsOpened && candidate.Contents.Count > 0)
            {
                chest = candidate;
                break;
            }
        }
        Assert.IsTrue(chest != null, "the floor should have a chest with contents");

        Inventory inventory = Inventory.Instance;
        Assert.IsTrue(inventory != null, "the scene should have an Inventory");

        // Fill every slot with iron, so the chest cannot hand anything over.
        ItemDefinition filler = Resources.Load<ItemDefinition>("ItemDefinitions/Materials/Iron");
        Assert.IsTrue(filler != null, "the project should ship an Iron material");
        for (int i = 0; i < 60 && inventory.addItem(filler, 99) == 0; i++)
        {
        }
        Assert.IsFalse(inventory.hasRoomFor(chest.Contents[0].definition, chest.Contents[0].amount),
            "the inventory has to be full for this test to mean anything");

        Assert.IsTrue(chest.Open(inventory), "a chest with leftovers is still openable");

        Assert.Greater(chest.Contents.Count, 0, "the leftovers stay in the chest");
        Assert.IsFalse(chest.IsOpened, "a chest that could not hand everything over is not spent");
        Assert.AreEqual(LevelValidation.ChestCode, Grid.Current.GetValueAtLocation(chest.Cell.x, chest.Cell.y),
            "and it stays solid, so the player can come back for it");

        // Make room and reopen: the leftovers finally change hands.
        inventory.removeItem(filler, 100000);
        Assert.IsTrue(chest.Open(inventory));
        Assert.IsTrue(chest.IsOpened, "an emptied chest is finally spent");
        Assert.AreEqual(0, chest.Contents.Count);
    }
}
