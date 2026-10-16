using System;
using UnityEngine;

/// <summary>
/// GDD 6.7 / 5.4: whether the straight line between two cells is clear.
///
/// Pure and static, and it walks a "is this cell blocked" delegate rather than a Tilemap: the grid is the
/// authority for terrain, and obstacle cells can plug in later without touching this file. The old
/// <c>utilityFunction.ClearLine</c> read a Tilemap that nothing ever painted, so every shot was clear.
/// </summary>
public static class LineOfSight
{
    /// <summary>
    /// Terrain that blocks sight. Walls, water and out of bounds all share code 1.
    ///
    /// This is deliberately *not* the walkability test: bushes, spider webs (the reserved code 6) and
    /// crevasses are on the GDD 5.4 non-blocking list, and an unopened chest is not on the blocking list.
    /// Rocks and huabiao pillars do not exist as data yet; when they do, add their codes here.
    /// </summary>
    public static bool BlocksSight(int terrain)
    {
        return terrain == 1;
    }

    /// <summary>Sight test for callers that own a Grid. Without a grid nothing is known to block.</summary>
    public static bool HasClearShot(Grid grid, Vector2Int from, Vector2Int to)
    {
        if (grid == null)
        {
            // No terrain data: match the old unpainted-tilemap behaviour and report a clear line rather
            // than disabling every shot in a scene that has no generator.
            return true;
        }
        return HasClearShot(cell => BlocksSight(grid.GetValueAtLocation(cell.x, cell.y)), from, to);
    }

    /// <summary>
    /// Bresenham walk from <paramref name="from"/> to <paramref name="to"/>, exclusive of both endpoints,
    /// so a wall in your own cell (or the target's) is not "between" you.
    ///
    /// The endpoints are ordered canonically before walking, which makes the result identical in both
    /// directions: integer Bresenham breaks ties differently depending on which end you start from.
    /// </summary>
    public static bool HasClearShot(Func<Vector2Int, bool> isBlocked, Vector2Int from, Vector2Int to)
    {
        if (isBlocked == null)
        {
            return false;
        }

        if (to.x < from.x || (to.x == from.x && to.y < from.y))
        {
            Vector2Int swap = from;
            from = to;
            to = swap;
        }

        int x = from.x;
        int y = from.y;
        int dx = Math.Abs(to.x - from.x);
        int dy = Math.Abs(to.y - from.y);
        int sx = to.x > from.x ? 1 : (to.x < from.x ? -1 : 0);
        int sy = to.y > from.y ? 1 : (to.y < from.y ? -1 : 0);
        int err = dx - dy;

        while (x != to.x || y != to.y)
        {
            if (!(x == from.x && y == from.y) && isBlocked(new Vector2Int(x, y)))
            {
                return false;
            }

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }

        return true;
    }
}
