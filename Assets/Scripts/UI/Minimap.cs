using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws what <see cref="MinimapRules"/> has revealed (T08, GDD 14.5).
///
/// One small texture, one pixel per revealed cell, shown by a <see cref="RawImage"/> in the corner of the HUD -
/// never a GameObject per cell, and never a per-frame full redraw. It deliberately does **not** use a tilemap:
/// tiles would have to be drawn at the level's own world coordinates, which is where the level already is.
/// </summary>
public class Minimap : MonoBehaviour
{
    [Tooltip("The UI image that shows the map. Assign it with Tools/Content/Wire HUD References.")]
    [SerializeField] private RawImage display;

    [Tooltip("Texture resolution. One pixel per cell is enough for a 120x120 floor.")]
    [SerializeField] private int textureSize = 128;

    [Header("Colours")]
    [SerializeField] private Color unexplored = new Color(0f, 0f, 0f, 0f);
    [SerializeField] private Color explored = new Color(0.28f, 0.32f, 0.36f, 1f);
    [SerializeField] private Color wallOrVoid = new Color(0.12f, 0.13f, 0.15f, 1f);
    [SerializeField] private Color marker = new Color(1f, 0.85f, 0.2f, 1f);

    public static Minimap Instance { get; private set; }

    public MinimapRules Rules { get; } = new MinimapRules();

    /// <summary>The map texture, or null before Awake.</summary>
    public Texture2D Texture { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        EnsureTexture();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        if (Texture != null)
        {
            Destroy(Texture);
            Texture = null;
        }
    }

    /// <summary>
    /// Reveal the cell the player is standing in, and - the first time an island is entered - its known chest
    /// and vein markers.
    /// </summary>
    public void Reveal(Vector2Int cell)
    {
        bool fresh = Rules.Visit(cell);
        bool painted = fresh && Paint(cell, explored);

        int island = IslandManager.GetIsland(cell);
        if (Rules.EnterIsland(island))
        {
            painted |= PaintIslandMarkers(island);

            // The stairs are discovered by walking into their room, not at floor start (GDD 3.3).
            if (RunState.HasStairs && IslandManager.GetIsland(RunState.StairsCell) == island)
            {
                painted |= RevealStairs(RunState.StairsCell);
            }
        }

        if (painted)
        {
            Apply();
        }
    }

    /// <summary>Show the stairs marker, once, when the player finds them (GDD 3.3).</summary>
    public bool RevealStairs(Vector2Int cell)
    {
        if (!Rules.DiscoverStairs(cell))
        {
            return false;
        }
        return Paint(cell, marker);
    }

    /// <summary>A new floor clears the map; the visited set does not survive a descent (GDD 14.5).</summary>
    public void ResetForFloor()
    {
        Rules.Reset();
        if (Texture == null)
        {
            return;
        }

        Color[] pixels = new Color[Texture.width * Texture.height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = unexplored;
        }
        Texture.SetPixels(pixels);
        Texture.Apply(false);
    }

    private bool PaintIslandMarkers(int island)
    {
        bool painted = false;
        foreach (Vector2Int cell in ChestRegistry.Cells)
        {
            if (IslandManager.GetIsland(cell) == island)
            {
                painted |= Paint(cell, marker);
            }
        }
        foreach (Vector2Int cell in OreVeinRegistry.Cells)
        {
            if (IslandManager.GetIsland(cell) == island)
            {
                painted |= Paint(cell, marker);
            }
        }
        return painted;
    }

    /// <summary>
    /// Light up one cell. Blocked terrain is drawn in its own colour so the map reads as rooms rather than
    /// confetti; anything outside the grid is left alone.
    /// </summary>
    private bool Paint(Vector2Int cell, Color colour)
    {
        if (Texture == null || !TryPixelOf(cell, out int x, out int y))
        {
            return false;
        }

        Grid grid = Grid.Current;
        bool walkable = grid != null && Grid.IsWalkableTerrain(grid.GetValueAtLocation(cell.x, cell.y));
        Texture.SetPixel(x, y, walkable ? colour : wallOrVoid);
        return true;
    }

    private void Apply()
    {
        if (Texture != null)
        {
            Texture.Apply(false);
        }
    }

    /// <summary>Map a level cell onto the texture, so the whole floor fits however large the grid is.</summary>
    private bool TryPixelOf(Vector2Int cell, out int x, out int y)
    {
        x = 0;
        y = 0;
        Grid grid = Grid.Current;
        if (grid == null || grid.Width <= 0 || grid.Height <= 0)
        {
            return false;
        }
        if (cell.x < 0 || cell.y < 0 || cell.x >= grid.Width || cell.y >= grid.Height)
        {
            return false;
        }

        x = Mathf.Clamp(cell.x * textureSize / grid.Width, 0, textureSize - 1);
        y = Mathf.Clamp(cell.y * textureSize / grid.Height, 0, textureSize - 1);
        return true;
    }

    private void EnsureTexture()
    {
        if (Texture != null)
        {
            return;
        }

        int size = Mathf.Clamp(textureSize, 16, 1024);
        Texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            name = "MinimapTexture"
        };
        textureSize = size;
        ResetForFloor();

        if (display != null)
        {
            display.texture = Texture;
        }
    }
}
