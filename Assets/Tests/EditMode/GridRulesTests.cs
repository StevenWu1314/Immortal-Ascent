using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Pure rules of the terrain grid. <see cref="Grid"/> is a plain class, so this needs no scene.
/// The grid publishes itself through the static <see cref="Grid.Current"/>, so every test restores
/// whatever was there before (the level's own grid, when the editor has one open).
/// </summary>
public class GridRulesTests
{
    private Grid previous;

    [SetUp]
    public void SetUp()
    {
        previous = Grid.Current;
    }

    [TearDown]
    public void TearDown()
    {
        Grid.Current = previous;
    }

    [TestCase(0, true, TestName = "IsWalkableTerrain_Floor_IsWalkable")]
    [TestCase(5, true, TestName = "IsWalkableTerrain_LegacyBridge_IsWalkable")]
    [TestCase(1, false, TestName = "IsWalkableTerrain_WallWaterOrEdge_IsBlocked")]
    [TestCase(2, false, TestName = "IsWalkableTerrain_LegacyDeepWater_IsBlocked")]
    [TestCase(3, false, TestName = "IsWalkableTerrain_LegacyEnemyCode_IsBlocked")]
    [TestCase(99, false, TestName = "IsWalkableTerrain_UnopenedChest_IsBlocked")]
    [TestCase(-1, false, TestName = "IsWalkableTerrain_Negative_IsBlocked")]
    [TestCase(12345, false, TestName = "IsWalkableTerrain_UnknownValue_IsBlocked")]
    public void IsWalkableTerrain_ClassifiesTerrainCodes(int terrain, bool expected)
    {
        Assert.AreEqual(expected, Grid.IsWalkableTerrain(terrain),
            $"terrain {terrain} should {(expected ? "be" : "not be")} walkable");
    }

    [Test]
    public void Constructor_PublishesItselfAsCurrent()
    {
        var grid = new Grid(4, 4, 1f, Vector3.zero);

        Assert.AreSame(grid, Grid.Current);
        Assert.AreEqual(4, grid.Width);
        Assert.AreEqual(4, grid.Height);
    }

    [Test]
    public void GetValueAtLocation_OutsideTheGrid_ReportsOutOfBounds()
    {
        var grid = new Grid(4, 4, 1f, Vector3.zero);
        grid.SetValueAtLocation(0, 0, 0);

        Assert.AreEqual(Grid.OutOfBounds, grid.GetValueAtLocation(-1, 0));
        Assert.AreEqual(Grid.OutOfBounds, grid.GetValueAtLocation(0, -1));
        Assert.AreEqual(Grid.OutOfBounds, grid.GetValueAtLocation(4, 0));
        Assert.AreEqual(Grid.OutOfBounds, grid.GetValueAtLocation(0, 4));
        Assert.IsFalse(Grid.IsWalkableTerrain(grid.GetValueAtLocation(4, 4)),
            "the map edge must read as blocked");
    }

    [Test]
    public void SetValueAtLocation_OutsideTheGrid_IsIgnoredInsteadOfThrowing()
    {
        var grid = new Grid(4, 4, 1f, Vector3.zero);

        Assert.DoesNotThrow(() => grid.SetValueAtLocation(-1, -1, 0));
        Assert.DoesNotThrow(() => grid.SetValueAtLocation(4, 4, 0));
        Assert.AreEqual(Grid.OutOfBounds, grid.GetValueAtLocation(4, 4));
    }

    [Test]
    public void CellFromWorld_FloorsToIntegerCells()
    {
        Assert.AreEqual(new Vector2Int(0, 0), Grid.CellFromWorld(new Vector3(0f, 0f, 0f)));
        Assert.AreEqual(new Vector2Int(0, 0), Grid.CellFromWorld(new Vector3(0.9f, 0.9f, 0f)));
        Assert.AreEqual(new Vector2Int(1, 2), Grid.CellFromWorld(new Vector3(1f, 2f, 0f)));
        Assert.AreEqual(new Vector2Int(-1, -1), Grid.CellFromWorld(new Vector3(-0.1f, -0.1f, 0f)),
            "negative world positions floor away from zero, matching the AI convention");
    }

    [Test]
    public void Move_AllowsFloorAndLegacyBridge_AndBlocksWallsAndTheMapEdge()
    {
        var grid = new Grid(4, 4, 1f, Vector3.zero);
        var origin = new Vector3(1f, 1f, 0f);   // cell (1,1)

        grid.SetValueAtLocation(2, 1, 0);       // floor to the east
        grid.SetValueAtLocation(0, 1, 1);       // wall to the west
        grid.SetValueAtLocation(1, 2, 5);       // legacy bridge to the north

        Assert.IsTrue(grid.Move(origin, new Vector2Int(1, 0)), "floor is walkable");
        Assert.IsTrue(grid.Move(origin, new Vector2Int(0, 1)), "legacy bridge terrain is still walkable");
        Assert.IsFalse(grid.Move(origin, new Vector2Int(-1, 0)), "a wall blocks the step");
    }

    [Test]
    public void Move_FromTheMapEdgeOffTheGrid_IsBlocked()
    {
        var grid = new Grid(4, 4, 1f, Vector3.zero);
        var edge = new Vector3(3f, 3f, 0f);     // cell (3,3)

        Assert.IsFalse(grid.Move(edge, new Vector2Int(1, 0)), "east of the map is out of bounds");
        Assert.IsFalse(grid.Move(edge, new Vector2Int(0, 1)), "north of the map is out of bounds");
    }
}
