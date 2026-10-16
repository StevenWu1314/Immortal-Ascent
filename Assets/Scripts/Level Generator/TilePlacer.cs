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

    /// <summary>
    /// Marker drawn on a spider web cell. Unassigned in the scene, in which case the bridge tile is reused
    /// so webs stay visible; real web art belongs to T09.
    /// </summary>
    public TileBase webTile;
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

    /// <summary>
    /// Draw the floor's stairs marker. The grid value stays walkable: the player stands on stairs,
    /// unlike a chest, which is solid until it is opened. Stairs art belongs to T08.
    /// </summary>
    public void MarkStairs(Vector2Int cell)
    {
        corridor.SetTile(new Vector3Int(cell.x, cell.y, 0), bridgeTile);
    }

    /// <summary>
    /// Sprinkle spider webs (terrain <see cref="WebRegistry.WebCode"/>) on the combat islands. A web is
    /// walkable until it is entered, which is when it roots whoever stepped in and is consumed - so it is
    /// never placed on the island the player starts on, on a chest cell, or on another web.
    /// </summary>
    public List<Vector2Int> generateWebTiles(List<BoundsInt> roomsSpace, Grid grid, int startingRoomIndex)
    {
        List<Vector2Int> webCells = new List<Vector2Int>();
        TileBase marker = webTile != null ? webTile : bridgeTile;

        for (int i = 0; i < roomsSpace.Count; i++)
        {
            if (i == startingRoomIndex)
            {
                continue;
            }

            BoundsInt room = roomsSpace[i];
            int wanted = Random.Range(0, 3);
            for (int n = 0; n < wanted; n++)
            {
                int x = Random.Range(room.xMin + 3, room.xMax - 3);
                int y = Random.Range(room.yMin + 3, room.yMax - 3);
                if (grid.GetValueAtLocation(x, y) != 0)
                {
                    continue;   // a chest or another web is already there
                }

                if (marker != null)
                {
                    corridor.SetTile(new Vector3Int(x, y, 0), marker);
                }
                grid.SetValueAtLocation(x, y, WebRegistry.WebCode);
                webCells.Add(new Vector2Int(x, y));
            }
        }

        return webCells;
    }

    /// <summary>
    /// Place 2-4 ore veins (terrain <see cref="OreVeinRegistry.OreCode"/>) on free cells. A vein is solid, so
    /// it never lands on the spawn cell, a chest, a web or another vein.
    /// </summary>
    public List<Vector2Int> generateOreVeins(List<BoundsInt> roomsSpace, Grid grid, Vector2Int reservedCell,
                                            int minVeins, int maxVeins)
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        if (roomsSpace == null || roomsSpace.Count == 0)
        {
            return cells;
        }

        int wanted = Random.Range(minVeins, maxVeins + 1);
        TileBase marker = webTile != null ? webTile : bridgeTile;
        int attempts = Mathf.Max(wanted * 8, 8);

        while (cells.Count < wanted && attempts-- > 0)
        {
            BoundsInt room = roomsSpace[Random.Range(0, roomsSpace.Count)];
            int x = Random.Range(room.xMin + 3, room.xMax - 3);
            int y = Random.Range(room.yMin + 3, room.yMax - 3);
            Vector2Int cell = new Vector2Int(x, y);
            if (cell == reservedCell || grid.GetValueAtLocation(x, y) != 0)
            {
                continue;   // the spawn cell, a chest, a web or another vein
            }

            if (marker != null)
            {
                corridor.SetTile(new Vector3Int(x, y, 0), marker);
            }
            grid.SetValueAtLocation(x, y, OreVeinRegistry.OreCode);
            cells.Add(cell);
        }

        return cells;
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

