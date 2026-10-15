using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class TilePlacer : MonoBehaviour
{
    public Tilemap walls; // Reference to the walls Tilemap component
    public Tilemap floor; // Reference to the floor Tilemap component
    public Tilemap corridor;
    public Tile[] floorTile; // Reference to the floor tile
    public RuleTile bridgeTile;
    public TileBase water;
    [SerializeField, Range(0f, 1f)] float flowerDensity;

   // List of BoundsInt

    private Tile RandomFloorTile()
    {   
        // Random.Range(int, int) is [min, max), so Range(0, 1) was always 0 and this read
        // "0 < flowerDensity": every floor tile got a flower whenever the density was non-zero,
        // and the density itself did nothing. Random.value restores the intended fraction.
        return (Random.value < flowerDensity) ? floorTile[Random.Range(1, floorTile.Length)] : floorTile[0];
    }
    public void PlaceTiles(List<BoundsInt> boundsList, Grid grid)
    {
        foreach (BoundsInt bound in boundsList)
        {
            for (int x = bound.xMin; x < bound.xMax; x++)
            {
                for (int y = bound.yMin; y < bound.yMax; y++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);

                    // Every ring cell is solid terrain. The branches this replaced were eight
                    // copies of the same grid write; only the long-commented-out per-edge wall
                    // tiles differed, and the wall Tilemap no longer draws them.
                    if (x == bound.xMin || x == bound.xMax - 1 ||
                        y == bound.yMin || y == bound.yMax - 1)
                    {
                        grid.SetValueAtLocation(pos.x, pos.y, 1);
                    }
                    else
                    {
                        floor.SetTile(pos, RandomFloorTile()); // Floor
                    }
                }
            }
        }
    }
    public void clear()
    {
        floor.ClearAllTiles();
        walls.ClearAllTiles();
        corridor.ClearAllTiles();
    }

    public void branchingCorridors(List<Rooms> Rooms, Grid grid, int length)
    {
        foreach (Rooms room in Rooms)
        {
            branchingCorridors(room, grid, length);
        }
    }

    private void branchingCorridors(Rooms room, Grid grid, int length)
    {
        bool right = room.rightConnected;
        bool left = room.leftConnected;
        bool up = room.upConnected;
        bool down = room.downConnected;
        Vector3Int currentPos = new Vector3Int(0, 0, 0);
        if(right)
        {
            
            
            currentPos = new Vector3Int(room.body.xMax-1, (int)room.body.center.y, 0);
            for(int i = 0; i < length; i++)
            {
                bool alternate = i % 2 == 0;
                corridor.SetTile(currentPos, bridgeTile);
                corridor.SetTile(currentPos+new Vector3Int(0, 1), bridgeTile);
                grid.SetValueAtLocation(currentPos.x, currentPos.y+1, 1);
                grid.SetValueAtLocation(currentPos.x, currentPos.y-1, 1);
                grid.SetValueAtLocation(currentPos.x, currentPos.y, 0);
                corridor.SetTile(currentPos+new Vector3Int(0, -1), bridgeTile);
                
                currentPos += new Vector3Int(1, 0, 0);
            }
        }
        if(left)
        {
            currentPos = new Vector3Int(room.body.xMin, (int)room.body.center.y, 0);
            for(int i = 0; i < length; i++)
            {
                bool alternate = i % 2 == 0;
                
                grid.SetValueAtLocation(currentPos.x, currentPos.y + 1, 1);
                grid.SetValueAtLocation(currentPos.x, currentPos.y - 1, 1);
                grid.SetValueAtLocation(currentPos.x, currentPos.y, 0);
                corridor.SetTile(currentPos, bridgeTile);
                walls.SetTile(currentPos,null);
                corridor.SetTile(currentPos+new Vector3Int(0, -1), bridgeTile);
                corridor.SetTile(currentPos+new Vector3Int(0, 1), bridgeTile);
                currentPos += new Vector3Int(-1, 0, 0);
            }
        }
        if(up)
        {
            currentPos = new Vector3Int((int)room.body.center.x, room.body.yMax-1, 0);
            if(currentPos.x < 0)
            {
                currentPos = new Vector3Int(currentPos.x-1, currentPos.y);
            }
            for(int i = 0; i < length+2; i++)
            {
                corridor.SetTile(currentPos, bridgeTile);
                walls.SetTile(currentPos,null);
                
                    corridor.SetTile(currentPos+new Vector3Int(1, 0), bridgeTile);
                    corridor.SetTile(currentPos+new Vector3Int(-1, 0), bridgeTile);
                
                grid.SetValueAtLocation(currentPos.x + 1, currentPos.y, 1);
                grid.SetValueAtLocation(currentPos.x - 1, currentPos.y, 1);
                grid.SetValueAtLocation(currentPos.x, currentPos.y, 0);
                currentPos += new Vector3Int(0, 1, 0);
            }
        }
        if(down)
        {
            currentPos = new Vector3Int((int)room.body.center.x, room.body.yMin, 0);
            grid.SetValueAtLocation(currentPos.x, currentPos.y, 0);
            for(int i = 0; i < length; i++)
            {
                // This branch only cleared the wall tile and never wrote the grid, so the
                // corridor stayed blocked on the terrain grid (the authority for movement)
                // and the room below was unreachable. Mirror the "up" branch.
                corridor.SetTile(currentPos, bridgeTile);
                walls.SetTile(currentPos, null);
                corridor.SetTile(currentPos + new Vector3Int(1, 0), bridgeTile);
                corridor.SetTile(currentPos + new Vector3Int(-1, 0), bridgeTile);
                grid.SetValueAtLocation(currentPos.x + 1, currentPos.y, 1);
                grid.SetValueAtLocation(currentPos.x - 1, currentPos.y, 1);
                grid.SetValueAtLocation(currentPos.x, currentPos.y, 0);
                currentPos += new Vector3Int(0, -1, 0);
            }
        }
    }
/// <summary>
    /// Place chest cells and report where they are so the generator can spawn an entity at each.
    ///
    /// The cell is marked with terrain code 99 (solid) and given a FLOOR tile rather than the
    /// chest tile: the chest is an entity now, so drawing the tile as well would double up the
    /// sprite. The floor tile still has to be written, or fillWater would flood the cell.
    /// </summary>
    public List<Vector2Int> generateLootTiles(List<BoundsInt> roomsSpace, Grid grid)
    {
        List<Vector2Int> chestCells = new List<Vector2Int>();
        foreach (BoundsInt room in roomsSpace)
        {
            int spawnLoot = Random.Range(1, 11);
            if(spawnLoot > 5)
            {
                int x = Random.Range(room.xMin+3, room.xMax-3);
                int y = Random.Range(room.yMin+3, room.yMax-3);
                floor.SetTile(new Vector3Int(x, y), RandomFloorTile());
                grid.SetValueAtLocation(x, y, LevelValidation.ChestCode);
                chestCells.Add(new Vector2Int(x, y));
            }
        }
        return chestCells;
    }

    public void fillWater(Grid grid)
    {
        for(int x = 0; x < grid.GetSize(0); x++)
        {
            for(int y = 0; y < grid.GetSize(1); y++)
            {
                Vector3Int cell = new Vector3Int(x, y);
                if (floor.GetTile(cell) == null && corridor.GetTile(cell) == null)
                {
                    floor.SetTile(cell, water);
                    // The grid is the authority for movement, so water must be BLOCKED on it.
                    // Previously only the tile was drawn, leaving the value at 0 (walkable),
                    // which made every gap between rooms traversable.
                    grid.SetValueAtLocation(x, y, 1);
                }
            }
        }
        corridor.RefreshAllTiles();
    }
}

