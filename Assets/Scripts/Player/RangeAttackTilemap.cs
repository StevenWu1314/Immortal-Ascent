using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RangeAttackTilemap : MonoBehaviour
{
    [SerializeField] Tile overlayTile;
    [SerializeField] Tile highlightedTile;
    public Tilemap tilemap;
    PlayerStats player;
    bool overlaying;
    // Start is called before the first frame update
    void Start()
    {
        player = transform.parent.GetComponent<PlayerStats>();
        tilemap = transform.GetComponentInChildren<Tilemap>();
        
    }

    // Update is called once per frame
    void Update()
    {
        if(overlaying)
        {
            clearPrev();
            DisplayCurrent();
        }
        
    }

    private void DisplayCurrent()
    {
        Vector3Int center = tilemap.WorldToCell(player.transform.position);
        int range = player.getRange();
        for(int x = -range; x <= range; x++)
        {
            for(int y = -range; y <= range; y++)
            {
                if(Mathf.Abs(x) + Mathf.Abs(y) <= range)
                {
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

    private void clearPrev()
    {
        tilemap.ClearAllTiles();
    }

    public void overlay()
    {
        Debug.Log("Overlaying");
        if(overlaying)
        {
            clearPrev();
        }
        overlaying = !overlaying;
    }

    public bool InRange(Vector3Int selectedTile)
    {
        return (tilemap.GetTile(selectedTile) != null);
    }
}
