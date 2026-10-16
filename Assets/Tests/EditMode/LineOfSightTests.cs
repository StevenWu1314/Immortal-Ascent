using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Line of sight (T04, GDD 6.7 / 5.4). Pure: the walk takes a "is this cell blocked" delegate, so the
/// rule is tested without a Grid or a scene.
/// </summary>
public class LineOfSightTests
{
    private static Func<Vector2Int, bool> WallsAt(params Vector2Int[] cells)
    {
        var walls = new HashSet<Vector2Int>(cells);
        return cell => walls.Contains(cell);
    }

    [Test]
    public void AdjacentAndOpenCells_AreClear()
    {
        Func<Vector2Int, bool> open = WallsAt();

        Assert.IsTrue(LineOfSight.HasClearShot(open, new Vector2Int(0, 0), new Vector2Int(1, 0)));
        Assert.IsTrue(LineOfSight.HasClearShot(open, new Vector2Int(0, 0), new Vector2Int(1, 1)));
        Assert.IsTrue(LineOfSight.HasClearShot(open, new Vector2Int(0, 0), new Vector2Int(9, 4)));
        Assert.IsTrue(LineOfSight.HasClearShot(open, new Vector2Int(3, 3), new Vector2Int(3, 3)),
            "a cell can always see itself");
    }

    [Test]
    public void ABlockerBetween_BlocksSight()
    {
        Func<Vector2Int, bool> wall = WallsAt(new Vector2Int(2, 0));

        Assert.IsFalse(LineOfSight.HasClearShot(wall, new Vector2Int(0, 0), new Vector2Int(4, 0)));
        Assert.IsFalse(LineOfSight.HasClearShot(wall, new Vector2Int(4, 0), new Vector2Int(0, 0)),
            "blocking must not depend on which end the walk starts from");
    }

    [Test]
    public void TheEndpoints_AreExcluded()
    {
        // A wall in the shooter's own cell, and in the target's: neither is "between" them.
        Func<Vector2Int, bool> wall = WallsAt(new Vector2Int(0, 0), new Vector2Int(4, 0));

        Assert.IsTrue(LineOfSight.HasClearShot(wall, new Vector2Int(0, 0), new Vector2Int(4, 0)));
    }

    [Test]
    public void Sight_IsSymmetric()
    {
        Func<Vector2Int, bool> wall = WallsAt(new Vector2Int(3, 2), new Vector2Int(4, 3));

        Vector2Int[] pairs =
        {
            new Vector2Int(0, 0), new Vector2Int(7, 5), new Vector2Int(2, 9),
            new Vector2Int(5, 1), new Vector2Int(8, 8), new Vector2Int(1, 6)
        };

        for (int i = 0; i < pairs.Length; i++)
        {
            for (int j = 0; j < pairs.Length; j++)
            {
                Assert.AreEqual(
                    LineOfSight.HasClearShot(wall, pairs[i], pairs[j]),
                    LineOfSight.HasClearShot(wall, pairs[j], pairs[i]),
                    $"{pairs[i]} <-> {pairs[j]} disagreed");
            }
        }
    }

    [Test]
    public void OnlyWallsWaterAndOutOfBounds_BlockSight()
    {
        Assert.IsTrue(LineOfSight.BlocksSight(1), "wall, water and out of bounds all share code 1");
        Assert.IsFalse(LineOfSight.BlocksSight(0), "floor");
        Assert.IsFalse(LineOfSight.BlocksSight(5), "a legacy walkable code");
        Assert.IsFalse(LineOfSight.BlocksSight(6), "spider webs are on the non-blocking list");
        Assert.IsFalse(LineOfSight.BlocksSight(99), "the GDD 5.4 blocking list does not include chests");
    }

    [Test]
    public void TheGridOverload_ReadsTerrain_AndOutOfBoundsBlocks()
    {
        var grid = new Grid(8, 8, 1f, Vector3.zero);
        grid.SetValueAtLocation(3, 3, 1);

        Assert.IsFalse(LineOfSight.HasClearShot(grid, new Vector2Int(0, 3), new Vector2Int(6, 3)),
            "a wall in the grid blocks");
        Assert.IsTrue(LineOfSight.HasClearShot(grid, new Vector2Int(0, 0), new Vector2Int(0, 6)),
            "the same column away from the wall is clear");
        Assert.IsFalse(LineOfSight.HasClearShot(grid, new Vector2Int(0, 0), new Vector2Int(-4, 0)),
            "out of bounds is blocked terrain");
    }

    [Test]
    public void WithoutAGrid_NothingIsKnownToBlock()
    {
        // Documented choice: a scene with no generator must not have every shot silently disabled. This
        // matches the behaviour of the unpainted Tilemap this replaced.
        Assert.IsTrue(LineOfSight.HasClearShot((Grid)null, new Vector2Int(0, 0), new Vector2Int(9, 9)));
    }
}
