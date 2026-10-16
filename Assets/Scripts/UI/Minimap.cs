using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Draws what <see cref="MinimapRules"/> has revealed (T08, GDD 14.5).
///
/// Cheap on purpose: one tilemap, and a tile is written only for a cell that has just been revealed - never a
/// per-frame full-map redraw, and never a GameObject per pixel.
/// </summary>
public class Minimap : MonoBehaviour
{
    [SerializeField] private Tilemap map;
    [Tooltip("Tile used for explored terrain.")]
    [SerializeField] private TileBase visitedTile;
    [Tooltip("Tile used for the stairs, chest and vein markers.")]
    [SerializeField] private TileBase markerTile;

    public static Minimap Instance { get; private set; }

    public MinimapRules Rules { get; } = new MinimapRules();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Reveal the cell the player is standing in, and - the first time an island is entered - its known chest
    /// and vein markers.
    /// </summary>
    public void Reveal(Vector2Int cell)
    {
        bool fresh = Rules.Visit(cell);

        int island = IslandManager.GetIsland(cell);
        if (Rules.EnterIsland(island))
        {
            PaintIslandMarkers(island);

            // The stairs are discovered by walking into their room, not at floor start (GDD 3.3).
            if (RunState.HasStairs && IslandManager.GetIsland(RunState.StairsCell) == island)
            {
                RevealStairs(RunState.StairsCell);
            }
        }

        if (fresh)
        {
            Paint(cell, visitedTile);
        }
    }

    /// <summary>Show the stairs marker, once, when the player finds them (GDD 3.3).</summary>
    public void RevealStairs(Vector2Int cell)
    {
        if (Rules.DiscoverStairs(cell))
        {
            Paint(cell, markerTile);
        }
    }

    /// <summary>A new floor clears the map; the visited set does not survive a descent (GDD 14.5).</summary>
    public void ResetForFloor()
    {
        Rules.Reset();
        if (map != null)
        {
            map.ClearAllTiles();
        }
    }

    private void PaintIslandMarkers(int island)
    {
        if (markerTile == null)
        {
            return;
        }
        foreach (Vector2Int cell in ChestRegistry.Cells)
        {
            if (IslandManager.GetIsland(cell) == island)
            {
                Paint(cell, markerTile);
            }
        }
        foreach (Vector2Int cell in OreVeinRegistry.Cells)
        {
            if (IslandManager.GetIsland(cell) == island)
            {
                Paint(cell, markerTile);
            }
        }
    }

    private void Paint(Vector2Int cell, TileBase tile)
    {
        if (map != null && tile != null)
        {
            map.SetTile(new Vector3Int(cell.x, cell.y, 0), tile);
        }
    }
}
