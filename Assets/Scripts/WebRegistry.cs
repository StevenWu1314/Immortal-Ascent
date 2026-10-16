using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The spider webs on the current floor, keyed by cell.
///
/// A web lives in two places, exactly like a chest: terrain (code 6, walkable, triggers on entry) and a
/// registry entry. It needs no GameObject, so unlike <see cref="ChestRegistry"/> this is cells only.
/// </summary>
public static class WebRegistry
{
    /// <summary>Terrain code for an intact web: walkable until it is entered, then consumed (GDD 5.4).</summary>
    public const int WebCode = 6;

    private static readonly HashSet<Vector2Int> webs = new HashSet<Vector2Int>();

    public static int Count => webs.Count;

    public static void Register(Vector2Int cell)
    {
        webs.Add(cell);
    }

    public static bool HasWeb(Vector2Int cell)
    {
        return webs.Contains(cell);
    }

    /// <summary>
    /// Consume the web at <paramref name="cell"/>. This is the chest path's two writes: terrain alone is
    /// not enough, because <see cref="IslandManager"/> keeps its own walkability snapshot and a cell left
    /// <c>Unassigned</c> there keeps <c>CanMoveTo</c> refusing the step.
    /// </summary>
    public static bool Consume(Vector2Int cell, Grid grid)
    {
        if (!webs.Remove(cell))
        {
            return false;
        }

        if (grid != null)
        {
            grid.SetValueAtLocation(cell.x, cell.y, 0);
        }
        IslandManager.MarkWalkable(cell);
        return true;
    }

    /// <summary>Forget every web. Called before a floor is generated, since webs have no objects to sweep.</summary>
    public static void Reset()
    {
        webs.Clear();
    }
}
