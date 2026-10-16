using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RangeAttackTilemap : MonoBehaviour
{
    [SerializeField] Tile overlayTile;
    [SerializeField] Tile highlightedTile;
    public Tilemap tilemap;
    PlayerStats player;
    public bool overlaying;
    // The cell currently drawn with highlightedTile, or null when nothing is highlighted.
    // Using a nullable instead of a sentinel cell avoids writing a stray tile at a huge
    // coordinate (which also explodes tilemap.cellBounds).
    Vector3Int? lastHighlighted;
    // Start is called before the first frame update
    void Start()
    {
        player = transform.parent.GetComponent<PlayerStats>();
        tilemap = transform.GetComponentInChildren<Tilemap>();
    }
    void Update()
    {
        if(overlaying)
        {
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int clickCell = tilemap.WorldToCell(worldPoint);
            if(tilemap.GetTile(clickCell) != null)
            {   
                if (lastHighlighted.HasValue)
                {
                    tilemap.SetTile(lastHighlighted.Value, overlayTile);
                }
                tilemap.SetTile(clickCell, highlightedTile);
                lastHighlighted = clickCell;
            }
            else if (lastHighlighted.HasValue)
            {
                // Cursor left the in-range area: restore the tile so no stale highlight
                // is left painted where the player is no longer pointing.
                tilemap.SetTile(lastHighlighted.Value, overlayTile);
                lastHighlighted = null;
            }
        }
    }
    private void DisplayCurrent()
    {
        // Two cell spaces. The overlay has its own Grid under the player, so its cells are relative to
        // wherever that Grid sits; terrain lives in Grid.Current's cells. Painting uses the overlay's
        // space and the sight test uses the level's.
        Vector2Int overlayCenter = (Vector2Int) tilemap.WorldToCell(player.transform.position);
        Vector2Int levelCenter = Grid.CellFromWorld(player.transform.position);
        Grid level = Grid.Current;
        // Range is owned by the equipped ranged weapon, not by PlayerStats: reading it from
        // the item means equipping a sword can no longer zero out the bow's reach.
        int range = Equipments.Instance != null ? Equipments.Instance.getRange() : 0;
        if (range <= 0)
        {
            Debug.LogWarning("RangeAttackTilemap: no ranged weapon with range equipped; " +
                             "nothing to draw.");
            return;
        }
        for(int x = -range; x <= range; x++)
        {
            for(int y = -range; y <= range; y++)
            {
                if(math.distance(new Vector2(0, 0), new Vector2(x, y)) <= range)
                {
                    if (LineOfSight.HasClearShot(level, levelCenter,
                            new Vector2Int(levelCenter.x + x, levelCenter.y + y)))
                    {
                        tilemap.SetTile(new Vector3Int(x + overlayCenter.x, y + overlayCenter.y, 0), overlayTile);
                    }
                }
            }
        }
        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int clickCell = tilemap.WorldToCell(worldPoint);
        if(tilemap.GetTile(clickCell) != null)
        {
            tilemap.SetTile(clickCell, highlightedTile);
        }
    }
    public void clearPrev()
    {
        tilemap.ClearAllTiles();
        lastHighlighted = null;   // the highlight tile is gone with the rest
    }

    /// <summary>Enter aiming: clear, redraw the in-range area and start cursor tracking.</summary>
    public void ShowOverlay()
    {
        clearPrev();
        DisplayCurrent();
        overlaying = true;
    }

    /// <summary>Leave aiming: stop tracking and remove every preview tile.</summary>
    public void HideOverlay()
    {
        overlaying = false;
        clearPrev();
    }

    /// <summary>Redraw the preview without changing whether aiming is active.</summary>
    public void RefreshOverlay()
    {
        if (!overlaying)
        {
            return;
        }
        clearPrev();
        DisplayCurrent();
    }

    public bool InRange(Vector3Int selectedTile)
    {
        // Only tiles the preview actually painted count as targetable, so this stays a
        // true range test rather than "a cell inside the tilemap bounds".
        return tilemap.GetTile(selectedTile) != null;
    }
}
