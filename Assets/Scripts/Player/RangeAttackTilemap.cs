using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RangeAttackTilemap : MonoBehaviour
{
    [SerializeField] Tile overlayTile;
    [SerializeField] Tile highlightedTile;
    Tilemap collidableMap;
    public Tilemap tilemap;
    PlayerStats player;
    public bool overlaying;
    Vector3Int prevPos;
    // The cell currently drawn with highlightedTile, or null when nothing is highlighted.
    // Using a nullable instead of a sentinel cell avoids writing a stray tile at a huge
    // coordinate (which also explodes tilemap.cellBounds).
    Vector3Int? lastHighlighted;
    // Start is called before the first frame update
    void Start()
    {
        player = transform.parent.GetComponent<PlayerStats>();
        tilemap = transform.GetComponentInChildren<Tilemap>();
        collidableMap = GameObject.Find("Collidable Plants").GetComponent<Tilemap>();
        prevPos = new Vector3Int(-999999, 0, 0);
    }
    void Update()
    {
        if(overlaying)
        {
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int clickCell = tilemap.WorldToCell(worldPoint);
            if(tilemap.GetTile(clickCell) != null)
            {   
                tilemap.SetTile(prevPos, overlayTile);
                tilemap.SetTile(clickCell, highlightedTile);
                prevPos = clickCell;
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
        Vector2Int center = (Vector2Int) tilemap.WorldToCell(player.transform.position);
        Vector3Int rayCenter = collidableMap.WorldToCell(player.transform.position);
        Debug.Log(center);
        Debug.Log(player.transform.position);
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
                    if(ClearLine((Vector2Int) rayCenter, new Vector2Int(rayCenter.x + x, rayCenter.y + y)))
                        tilemap.SetTile(new Vector3Int(x + center.x, y + center.y, 0), overlayTile);
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

    public bool ClearLine(Vector2Int start, Vector2Int target)
    {
        if(math.abs(target.y - start.y) < math.abs(target.x - start.x))
        {
            if(start.x > target.x)
            {
                return ClearLineLow(target, start);
            }
            else
            {
                return ClearLineLow(start, target);
            }
        }
        else
        {
            if(start.y > target.y)
            {
                return ClearLineHigh(target, start);
            }
            else
            {
                return ClearLineHigh(start, target);
            }
        }
    }

    public bool ClearLineLow(Vector2Int start, Vector2Int target)
    {
        Vector2Int current = start;
        if(collidableMap.GetTile((Vector3Int) current) != null)
        {
            Debug.Log(collidableMap.GetTile((Vector3Int) current));
            return false;
        }
        int dy = target.y - start.y;
        int dx = target.x - start.x;
        int yi = 1;
        if(dy < 0)
        {
            yi = -1;
            dy = -dy;
        }
        int d = 2*dy - dx;
        while (current.x != target.x)
        {
            if(d > 0)
            {
                current.y += yi;
                d += 2 * (dy - dx);
            }
            else
            {
                d += 2*dy;
            }
            current.x++;
            if(collidableMap.GetTile((Vector3Int) current) != null)
            {
                Debug.Log(collidableMap.GetTile((Vector3Int) current));
                return false;
            }
        }
        return true;
    }

    public bool ClearLineHigh(Vector2Int start, Vector2Int target)
    {
        Vector2Int current = start;
        if(collidableMap.GetTile((Vector3Int) current) != null)
        {
            Debug.Log(collidableMap.GetTile((Vector3Int) current));
            return false;
        }
        int dy = target.y - start.y;
        int dx = target.x - start.x;
        int xi = 1;
        if(dx < 0)
        {
            xi = -1;
            dx = -dx;
        }
        int d = 2*dx - dy;
        
        while (current.y != target.y)
        {
            if(d > 0)
            {
                current.x += xi;
                d += 2 * (dx - dy);
            }
            else
            {
                d += 2*dx;
            }
            current.y++;
            if(collidableMap.GetTile((Vector3Int) current) != null)
            {
                Debug.Log(collidableMap.GetTile((Vector3Int) current));
                return false;
            }
        }
        return true;
        
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
