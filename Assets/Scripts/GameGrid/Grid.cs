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
    public delegate void genLoot();
    public static event genLoot chestOpen;

    /// <summary>
    /// Value reported for any cell outside the grid. Counts as blocked so that
    /// callers (enemy AI, the player) treat the map edge as a wall instead of
    /// reading past the end of the array.
    /// </summary>
    public const int OutOfBounds = 1;

    /// <summary>
    /// The grid currently backing the level. Exactly one component should own the
    /// level grid (see PerlinNoiseMap), and everything else should read it from
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


        // for (int x = 0; x < gridArray.GetLength(0); x++)
        // {
        //     Debug.DrawLine(GetWorldPosition(x, 0), GetWorldPosition(x, height), Color.white, 100f);
        // }
        // for (int y = 0 ; y < gridArray.GetLength(1); y++)
        // {
        //     Debug.DrawLine(GetWorldPosition(0, y), GetWorldPosition(width, y), Color.white, 100f);
                
        // }
    }

    private Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x, y) * cellSize + originPosition;
    }
    
    public int GetValueAtLocation(int x, int y)
    {
        if (!InBounds(x, y))
        {
            return OutOfBounds;
        }
        return gridArray[x, y];
    }

    public int GetValueAtWorldLocation(Vector3 worldPos)
    {
        int x, y;
        getXY(worldPos, out x, out y);
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
        //Debug.Log("x: " + x + " y: " + y);
        //utilityFunction.createWorldText(gridArray[x, y].ToString(), null, GetWorldPosition(x, y) + new Vector3(cellSize, cellSize) * 0.5f, 20, Color.white, TextAnchor.MiddleCenter, TextAlignment.Center, 1);
    }

    public bool InBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < width && y < height;
    }

    public void SetValueAtWorldLocation(Vector3 location, int value)
    {
        int worldx, worldy;
        worldx = (int)location.x;
        worldy = (int)location.y;
        int x, y;
        getXY(new Vector3(worldx, worldy), out x, out y);
        if (!InBounds(x, y))
        {
            return;
        }
        gridArray[x, y] = value;
        utilityFunction.createWorldText(gridArray[x, y].ToString(), null, new Vector3(worldx, worldy) + new Vector3(cellSize, cellSize) * 0.5f, 20, Color.white, TextAnchor.MiddleCenter, TextAlignment.Center, 1);
    }
    public int GetSize(int dimension)
    {
        return gridArray.GetLength(dimension);
    }

    /// <summary>
    /// Single definition of "an entity may stand here". Terrain codes are written by
    /// PerlinNoiseMap/TilePlacer: 0 = grass/floor/corridor, 1 = wall/shallow water,
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
    /// The <paramref name="entity"/> parameter is retained for call-site compatibility; occupancy
    /// is owned by EntityManager, not by this static terrain grid.
    /// </summary>
    public bool Move(Vector3 position, Vector2Int direction, Transform entity)
    {
        int x, y;
        getXY(position, out x, out y);
        Vector2Int target = new Vector2Int(x, y) + direction;
        // IsWalkableTerrain(OutOfBounds) is false, so the map edge blocks movement.
        return IsWalkableTerrain(GetValueAtLocation(target.x, target.y));
    }

    public void printGrid() {
        GameObject[] worldtexts = GameObject.FindGameObjectsWithTag("worldtexts");
        for(int i = 0; i < worldtexts.GetLength(0); i++)
        {
            spawnPointBehavior.Destroy(worldtexts[i]);
        }
        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int y = 0; y < gridArray.GetLength(1); y++)
            {
                if(gridArray[x, y] != 0)
                {
                    utilityFunction.createWorldText(gridArray[x, y].ToString(), null, GetWorldPosition(x, y) + new Vector3(cellSize, cellSize) * 0.5f, 20, Color.white, TextAnchor.MiddleCenter, TextAlignment.Center, 1);
                }
                
            }
        }
    }


    public Enemy detectEnemiesInALine(Vector3 position, Vector2Int direction, int range)
    {
        int x, y;
        getXY(position, out x, out y);
        for (int i = 0; i < range; i++)
        {
            x += direction.x;
            y += direction.y;
            if (!InBounds(x, y))
            {
                return null;
            }
            int value = gridArray[x, y];
            if (value == 3) {
                Collider2D[] targets = Physics2D.OverlapCircleAll(position + new Vector3(i * direction.x, i * direction.y), 1);
                Enemy target = null;
                foreach (Collider2D collider in targets)
                {
                    if (collider.gameObject.GetComponent<Enemy>() != null) {
                    
                        target = collider.gameObject.GetComponent<Enemy>();
                    }
                }
                return target;
            }     
        }
        return null;
        
    }
}
