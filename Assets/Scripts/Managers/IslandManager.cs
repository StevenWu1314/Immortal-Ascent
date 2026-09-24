using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the island layout of the level and the rule that the player cannot leave an island
/// until every enemy on it has been defeated.
///
/// Island assignment is EXPLICIT rather than derived from water connectivity: the generator
/// hands in the room bounds, and every room becomes its own island. That keeps the gameplay
/// rule stable no matter how far the corridors happen to reach, which matters because corridor
/// length depends on room sizes and rounding.
///
/// Corridors are a shared PASSAGE region:
///   * the player may always walk onto or off a passage cell,
///   * enemies may never enter one, so they stay inside the room they spawned in.
/// </summary>
public static class IslandManager
{
    /// <summary>Not walkable terrain, or not part of any island.</summary>
    public const int Unassigned = -1;

    /// <summary>Shared corridor region: passable by the player, closed to enemies, never gated.</summary>
    public const int Passage = 0;

    private static int[,] islandIds;
    private static int width;
    private static int height;
    private static int islandCount;

    /// <summary>Room rectangles, indexed by islandId - 1. Used for authoritative containment tests.</summary>
    private static BoundsInt[] roomBounds;

    private static bool initialized;

    public static bool IsInitialized => initialized;
    public static int IslandCount => islandCount;

    /// <summary>
    /// Assign island ids from the room layout. Cells inside a room's interior get that room's
    /// id; remaining walkable cells (corridors) become <see cref="Passage"/>; blocked terrain
    /// stays <see cref="Unassigned"/>.
    /// </summary>
    public static void Rebuild(List<BoundsInt> rooms)
    {
        islandIds = null;
        initialized = false;
        islandCount = 0;

        Grid grid = Grid.Current;
        if (grid == null)
        {
            Debug.LogWarning("IslandManager: Grid.Current is null; islands not built.");
            return;
        }
        if (rooms == null || rooms.Count == 0)
        {
            Debug.LogWarning("IslandManager: no room bounds supplied; islands not built.");
            return;
        }

        width = grid.Width;
        height = grid.Height;
        islandIds = new int[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                islandIds[x, y] = Unassigned;
            }
        }

        // One island per room, using the room's INTERIOR only. The generator's rectangle
        // includes a one-cell border ring that PlaceTiles marks as wall, but a corridor may
        // carve part of that ring open into a doorway. If those doorway cells were tagged as
        // part of the room, the gate would see "destination is still my island" and let the
        // player step one cell out before blocking. Leaving the ring untagged makes the
        // doorway and the corridor beyond it both count as passage.
        for (int i = 0; i < rooms.Count; i++)
        {
            int id = i + 1; // room ids start at 1 so 0 can mean "passage"
            BoundsInt body = rooms[i];

            int xMin = body.xMin + 1;
            int xMax = body.xMax - 1;
            int yMin = body.yMin + 1;
            int yMax = body.yMax - 1;
            if (xMin >= xMax || yMin >= yMax)
            {
                // Room too small to have an interior; fall back to the full rectangle.
                xMin = body.xMin; xMax = body.xMax;
                yMin = body.yMin; yMax = body.yMax;
            }

            for (int x = xMin; x < xMax; x++)
            {
                for (int y = yMin; y < yMax; y++)
                {
                    if (x < 0 || y < 0 || x >= width || y >= height)
                    {
                        continue;
                    }
                    if (!Grid.IsWalkableTerrain(grid.GetValueAtLocation(x, y)))
                    {
                        continue;
                    }
                    islandIds[x, y] = id;
                }
            }
        }

        islandCount = rooms.Count;
        roomBounds = rooms.ToArray();

        // Anything walkable that no room claimed is corridor -> shared passage.
        int passageCells = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (islandIds[x, y] != Unassigned)
                {
                    continue;
                }
                if (Grid.IsWalkableTerrain(grid.GetValueAtLocation(x, y)))
                {
                    islandIds[x, y] = Passage;
                    passageCells++;
                }
            }
        }

        initialized = true;
        Debug.Log($"[IslandManager] {islandCount} room island(s), {passageCells} passage cell(s) " +
                  $"on a {width}x{height} grid.");
    }

    /// <summary>
    /// Island id at a cell: a room id (>= 1), <see cref="Passage"/> (0), or
    /// <see cref="Unassigned"/> (-1) for blocked terrain.
    /// </summary>
    public static int GetIsland(Vector2Int cell)
    {
        if (!initialized || islandIds == null ||
            cell.x < 0 || cell.y < 0 || cell.x >= width || cell.y >= height)
        {
            return Unassigned;
        }
        return islandIds[cell.x, cell.y];
    }

    public static int GetIsland(Vector3 worldPos)
    {
        return GetIsland(Grid.CellFromWorld(worldPos));
    }

    /// <summary>Enemies still alive on a room island, via the EntityManager registry.</summary>
    public static int CountLivingEnemies(int islandId)
    {
        if (!initialized || islandId <= Passage || EntityManager.Instance == null)
        {
            // Passages and unknown ids never hold enemies.
            return 0;
        }

        int alive = 0;
        foreach (var pair in EntityManager.Instance.AllEntities)
        {
            GameObject go = pair.Value;
            if (go == null || go.GetComponent<Enemy>() == null)
            {
                continue;
            }
            if (GetIsland(pair.Key) == islandId)
            {
                alive++;
            }
        }
        return alive;
    }

    /// <summary>True when no living enemy stands on the room island.</summary>
    public static bool IsIslandCleared(int islandId)
    {
        return CountLivingEnemies(islandId) == 0;
    }

    /// <summary>
    /// Whether a cell belongs to the given island. Used to keep enemies inside their own room:
    /// a passage cell never matches a room id, so enemies refuse to step onto corridors.
    /// </summary>
    public static bool IsOnIsland(Vector2Int cell, int islandId)
    {
        if (!initialized)
        {
            return true; // never confine when islands are unavailable
        }
        if (islandId <= Passage)
        {
            // Enemy has no valid room island; only the terrain check applies.
            return GetIsland(cell) != Unassigned;
        }
        return GetIsland(cell) == islandId;
    }

    /// <summary>
    /// Room island id whose rectangle contains the cell (border ring included), or Unassigned.
    /// </summary>
    public static int GetRoomContaining(Vector2Int cell)
    {
        if (roomBounds == null)
        {
            return Unassigned;
        }
        for (int i = 0; i < roomBounds.Length; i++)
        {
            BoundsInt body = roomBounds[i];
            if (cell.x >= body.xMin && cell.x < body.xMax &&
                cell.y >= body.yMin && cell.y < body.yMax)
            {
                return i + 1;
            }
        }
        return Unassigned;
    }

    /// <summary>
    /// Whether the cell belongs to room <paramref name="islandId"/> itself, as opposed to the
    /// corridors that surround it. Requires BOTH:
    ///   * the cell is inside that room's rectangle, and
    ///   * the cell is not part of a corridor.
    ///
    /// Corridor cells are detected structurally: a walkable cell that does not lie inside ANY
    /// room rectangle is corridor. This lets an enemy pursue the player through a doorway the
    /// corridor carved open without ever stepping into the corridor beyond it.
    /// </summary>
    public static bool IsPartOfRoom(Vector2Int cell, int islandId)
    {
        if (!initialized)
        {
            return true;
        }
        if (islandId <= Passage || GetRoomContaining(cell) != islandId)
        {
            return false;
        }
        Grid grid = Grid.Current;
        if (grid == null)
        {
            return false;
        }
        return Grid.IsWalkableTerrain(grid.GetValueAtLocation(cell.x, cell.y));
    }

    /// <summary>
    /// Whether the player may step into <paramref name="destination"/>.
    ///
    /// The rule is one-directional and structural, with no coordinate assumptions:
    ///   * entering a room is always allowed, even one full of enemies;
    ///   * cells that belong to no room (corridors, and the border ring where it borders
    ///     corridors) are never gated, so once the current room is clear the player can step
    ///     out and choose the next room;
    ///   * while standing inside a room that still has enemies, the player may not enter a
    ///     cell that belongs to another room or to no room at all.
    /// </summary>
    public static bool CanMoveTo(Vector3 currentWorldPos, Vector2Int destination)
    {
        if (!initialized)
        {
            return true;
        }

        Vector2Int from = Grid.CellFromWorld(currentWorldPos);
        // Which room is the player standing in? Per-cell island ids are authoritative here
        // because they are assigned from terrain plus room rectangles, not from coordinates
        // I would have to guess at.
        int currentRoom = GetIsland(from);
        if (currentRoom <= Passage)
        {
            // In a corridor or outside every room: free to move.
            return true;
        }

        // Staying within this room's own cells is always allowed.
        if (GetIsland(destination) == currentRoom)
        {
            return true;
        }

        // Entering another room, or leaving into a corridor: only once this room is cleared.
        return IsIslandCleared(currentRoom);
    }

    /// <summary>Message shown when a move was blocked by the island rule.</summary>
    public static string DescribeBlock(Vector3 currentWorldPos)
    {
        int from = GetRoomContaining(Grid.CellFromWorld(currentWorldPos));
        int remaining = CountLivingEnemies(from);
        return remaining > 0
            ? $"Defeat the remaining {remaining} enemy/enemies before leaving this island."
            : "You cannot leave this island yet.";
    }
}
