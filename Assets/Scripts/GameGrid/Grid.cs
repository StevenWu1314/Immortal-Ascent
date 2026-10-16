using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class Grid
{
    private int width;
    private int height;
    private Vector3 originPosition;
    private float cellSize;
    private int[,] gridArray;

    /// <summary>
    /// Value reported for any cell outside the grid. Counts as blocked so that
    /// callers (enemy AI, the player) treat the map edge as a wall instead of
    /// reading past the end of the array.
    /// </summary>
    public const int OutOfBounds = 1;

    /// <summary>
    /// The grid currently backing the level. Exactly one component should own the
    /// level grid (see RoomFirstDungeonGenerator), and everything else should read it from
    /// here rather than creating or searching for its own instance.
    /// </summary>
    public static Grid Current { get; set; }

    public int Width => width;
    public int Height => height;

    
    public Grid(int width, int height, float cellSize, Vector3 originPosition)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;
        gridArray = new int[width, height];
        Current = this;
    }
    public int GetValueAtLocation(int x, int y)
    {
        if (!InBounds(x, y))
        {
            return OutOfBounds;
        }
        return gridArray[x, y];
    }
    public void SetValueAtLocation(int x, int y, int value)
    {
        if (!InBounds(x, y))
        {
            // Generation code legitimately probes one tile outside the map (e.g. a
            // bridge widening pass). Silently ignore instead of throwing.
            return;
        }
        gridArray[x, y] = value;
    }

    public bool InBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < width && y < height;
    }
    public int GetSize(int dimension)
    {
        return gridArray.GetLength(dimension);
    }

    /// <summary>
    /// Single definition of "an entity may stand here". Terrain codes are written by
    /// TilePlacer: 0 = floor/corridor, 1 = wall/shallow water,
    /// 2 = deep water, 5 = bridge (legacy), 99 = chest. Anything unknown is treated
    /// as blocked so a malformed map cannot silently let entities walk off it.
    /// </summary>
    public static bool IsWalkableTerrain(int terrain)
    {
        return terrain == 0 || terrain == 5;
    }

    /// <summary>Grid-aligned cell for a world position, matching the AI's FloorToInt convention.</summary>
    public static Vector2Int CellFromWorld(Vector3 worldPos)
    {
        return new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));
    }

    private void getXY(Vector3 Pos, out int x, out int y)
    {
        x = Mathf.FloorToInt(Pos.x - originPosition.x);
        y = Mathf.FloorToInt(Pos.y - originPosition.y);
    }

    /// <summary>
    /// True when the player may step from their current cell in <paramref name="direction"/>.
    /// Occupancy is owned by EntityManager, not by this static terrain grid.
    /// </summary>
    public bool Move(Vector3 position, Vector2Int direction)
    {
        int x, y;
        getXY(position, out x, out y);
        Vector2Int target = new Vector2Int(x, y) + direction;
        // IsWalkableTerrain(OutOfBounds) is false, so the map edge blocks movement.
        return IsWalkableTerrain(GetValueAtLocation(target.x, target.y));
    }
}
