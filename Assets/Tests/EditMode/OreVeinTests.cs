using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Ore veins (T06, GDD 5.5). Uses a plain Grid and the registry, so no scene is needed.
/// </summary>
public class OreVeinTests
{
    private ItemDefinition iron;

    [SetUp]
    public void SetUp()
    {
        OreVeinRegistry.Reset();
        iron = ScriptableObject.CreateInstance<ItemDefinition>();
    }

    [TearDown]
    public void TearDown()
    {
        OreVeinRegistry.Reset();
    }

    [Test]
    public void AVeinIsSolidTerrain()
    {
        Assert.IsFalse(Grid.IsWalkableTerrain(OreVeinRegistry.OreCode),
            "a vein is worked from next to it, so it blocks the cell like an unopened chest");
    }

    [Test]
    public void AMinedVein_YieldsOneToThreeOre()
    {
        Vector2Int cell = new Vector2Int(4, 4);
        // Plenty in the vein: ten swings of up to three must not exhaust it mid-test.
        OreVeinRegistry.Register(cell, iron, 100);

        for (int seed = 0; seed < 10; seed++)
        {
            Assert.IsTrue(OreVeinRegistry.TryMine(cell, null, new System.Random(seed), out ItemDefinition ore, out int yielded));
            Assert.AreEqual(iron, ore);
            Assert.GreaterOrEqual(yielded, 1);
            Assert.LessOrEqual(yielded, 3);
        }
    }

    [Test]
    public void MiningDepletesTheVein_AndOpensTheCell()
    {
        var grid = new Grid(12, 12, 1f, Vector3.zero);
        Vector2Int cell = new Vector2Int(5, 5);
        grid.SetValueAtLocation(cell.x, cell.y, OreVeinRegistry.OreCode);
        OreVeinRegistry.Register(cell, iron, 2);

        int mined = 0;
        while (OreVeinRegistry.HasVein(cell) && mined < 10)
        {
            Assert.IsTrue(OreVeinRegistry.TryMine(cell, grid, new System.Random(mined), out _, out _));
            mined++;
        }

        Assert.IsFalse(OreVeinRegistry.HasVein(cell), "the vein is finite");
        Assert.AreEqual(0, grid.GetValueAtLocation(cell.x, cell.y),
            "a depleted vein opens its cell, or the room gate keeps refusing the step");

        Assert.IsFalse(OreVeinRegistry.TryMine(cell, grid, new System.Random(1), out _, out int nothing),
            "a depleted vein yields nothing further");
        Assert.AreEqual(0, nothing);
    }

    [Test]
    public void AnAdjacentVein_IsFound_AndNotADistantOne()
    {
        Vector2Int here = new Vector2Int(6, 6);
        Vector2Int vein = here + Vector2Int.right;
        OreVeinRegistry.Register(vein, iron, 3);

        Assert.IsTrue(OreVeinRegistry.TryGetAdjacent(here, out Vector2Int found));
        Assert.AreEqual(vein, found);
        Assert.AreEqual(iron, OreVeinRegistry.OreAt(vein));
        Assert.AreEqual(3, OreVeinRegistry.Remaining(vein));

        Assert.IsFalse(OreVeinRegistry.TryGetAdjacent(new Vector2Int(0, 0), out _),
            "a vein two cells away is not adjacent");
    }

    [Test]
    public void ResetForgetsEveryVein()
    {
        OreVeinRegistry.Register(new Vector2Int(1, 1), iron, 3);
        Assert.AreEqual(1, OreVeinRegistry.Count);

        OreVeinRegistry.Reset();

        Assert.AreEqual(0, OreVeinRegistry.Count);
        Assert.IsFalse(OreVeinRegistry.HasVein(new Vector2Int(1, 1)));
    }
}
