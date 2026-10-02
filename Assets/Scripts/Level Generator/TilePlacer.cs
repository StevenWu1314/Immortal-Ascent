using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.VisualScripting;
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
    public Tile chest;
    public TileBase water;
    [SerializeField, Range(0f, 1f)] float flowerDensity;
    [SerializeField, Range(0f, 1f)] float TreeDensity;
    DungeonSpecifications dungeonSpecifications;

   // List of BoundsInt

    private Tile RandomFloorTile()
    {   
        return (Random.Range(0, 1) < flowerDensity) ? floorTile[Random.Range(1, floorTile.Length)] : floorTile[0];
    }
    public void PlaceTiles(List<BoundsInt> boundsList, Grid grid)
    {
        int xsize = grid.GetSize(0)/2;
        int ysize = grid.GetSize(1)/2;
        foreach (BoundsInt bound in boundsList)
        {
            for (int x = bound.xMin; x < bound.xMax; x++)
            {
                for (int y = bound.yMin; y < bound.yMax; y++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    bool alternate = (x + y) % 2 == 0; // Alternates true/false for each tile

                    if (x == bound.xMin && y == bound.yMin)
                    {
                        //walls.SetTile(pos, wallTileBottomLeft); // Bottom-left corner
                        grid.SetValueAtLocation(pos.x, pos.y, 1);
                    }
                    else if (x == bound.xMax - 1 && y == bound.yMin)
                    {
                        //walls.SetTile(pos, wallTileBottomRight); // Bottom-right corner
                        grid.SetValueAtLocation(pos.x, pos.y, 1);
                    }
                    else if (x == bound.xMin && y == bound.yMax - 1)
                    {
                        //walls.SetTile(pos, wallTileTopLeft); // Top-left corner
                        grid.SetValueAtLocation(pos.x, pos.y, 1);
                    }
                    else if (x == bound.xMax - 1 && y == bound.yMax - 1)
                    {
                        //walls.SetTile(pos, wallTileTopRight); // Top-right corner
                        grid.SetValueAtLocation(pos.x, pos.y, 1);
                    }
                    else if (x == bound.xMin)
                    {
                        //walls.SetTile(pos, alternate ? wallTileLeft1 : wallTileLeft2); // Left wall
                        grid.SetValueAtLocation(pos.x, pos.y, 1);
                    }
                    else if (x == bound.xMax - 1)
                    {
                        //walls.SetTile(pos, alternate ? wallTileRight1 : wallTileRight2); // Right wall
                        grid.SetValueAtLocation(pos.x, pos.y, 1);
                        
                    }
                    else if (y == bound.yMin)
                    {
                        //walls.SetTile(pos, alternate ? wallTileBottom1 : wallTileBottom2); // Bottom wall
                        grid.SetValueAtLocation(pos.x, pos.y, 1);
                    }
                    else if (y == bound.yMax - 1)
                    {
                        //walls.SetTile(pos, alternate ? wallTileTop1 : wallTileTop2); // Top wall
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


        
    public void createCorridors(List<BoundsInt> rooms)
    {
        for (int i = 0; i < rooms.Count-1; i++)
        {
            createCorridor(rooms[i], rooms[i+1]);

        }
    }

    public void createCorridor(BoundsInt room1, BoundsInt room2)
    {
        var position = room1.center;
        var destination = Vector3Int.FloorToInt(room2.center);
        Vector3Int current = Vector3Int.FloorToInt(position);
        bool up = false;
        bool right = false;
        Vector3Int floorPos;
        while (current.y != destination.y)
        {
            bool alternate = (current.x + current.y) % 2 == 0;
            int wallsRanInto = 0;
            floorPos = new Vector3Int((int)current.x, (int)current.y);
            /*
            if(walls.GetTile(floorPos-new Vector3Int(1, 0))==null && floor.GetTile(floorPos-new Vector3Int(1, 0))==null)
            {
                walls.SetTile(floorPos-new Vector3Int(1, 0), alternate ? wallTileLeft1 : wallTileLeft2);
            }
            if(walls.GetTile(floorPos+new Vector3Int(1, 0))==null && floor.GetTile(floorPos+new Vector3Int(1, 0))==null)
            {
                walls.SetTile(floorPos+new Vector3Int(1, 0), alternate ? wallTileRight1 : wallTileRight2);
            }
            */
            corridor.SetTile(floorPos, bridgeTile);
            if(floor.GetTile(floorPos) != null)
            {
                corridor.SetTile(floorPos, null);
                wallsRanInto++;
            }

            if (current.y > destination.y)
            {
                current -= new Vector3Int(0, 1);
            }
            else
            {
                up = true;
                current += new Vector3Int(0, 1);
            }
            if(wallsRanInto > 3)
            {
                break;
            }
        }
        while (current.x != destination.x)
        {
            bool alternate = (current.x + current.y) % 2 == 0;
            int wallsRanInto = 0;

            floorPos = new Vector3Int((int)current.x, (int)current.y);
            /*if(walls.GetTile(floorPos-new Vector3Int(0, 1))==null && floor.GetTile(floorPos-new Vector3Int(0, 1))==null )
            {
                walls.SetTile(floorPos-new Vector3Int(0, 1), alternate ? wallTileBottom1 : wallTileBottom2);
            }
            if(walls.GetTile(floorPos+new Vector3Int(0, 1))==null && floor.GetTile(floorPos+new Vector3Int(0, 1))==null)
            {
                walls.SetTile(floorPos+new Vector3Int(0, 1), alternate ? wallTileBottom1 : wallTileBottom2);
            }
            */
            
            corridor.SetTile(floorPos, bridgeTile);
            if(floor.GetTile(floorPos) != null)
            {
                wallsRanInto++;
            }
            
            if(current.x > destination.x)
            {
                
                current -= new Vector3Int(1, 0);
            }
            else
            { 
                current += new Vector3Int(1, 0);
            }
            if(wallsRanInto > 3)
            {
                break;
            }
        }
        corridor.SetTile(new Vector3Int((int)current.x, (int)current.y), bridgeTile);
        walls.SetTile(new Vector3Int((int)current.x, (int)current.y), null);
        //wallCorridor();
    }
    
    public void wallCorridor()
    {
        BoundsInt bounds = corridor.cellBounds;
        for(int i = 0; i < bounds.xMax;i++)
        {
            for(int j = 0; j < bounds.yMax; j++)
            {
                Vector3Int position = new Vector3Int(i, j);
                if(corridor.HasTile(position))
                {
        
                    for(int k = 0; k < 3; k++)
                    {
                        for (int l = 0; l < 3; l++)
                        {
                            
                            if(!walls.HasTile(position + new Vector3Int(1-k, i-l)) && !corridor.HasTile(position + new Vector3Int(1-k, i-l)) && floor.HasTile(position + new Vector3Int(1-k, i-l)))
                            {
                               bool corner = checkCornerPiece(position);
                               if(corner)
                               {
                                Debug.Log(position);
                                Vector3Int[] floorPos = new Vector3Int[2];
                                floorPos = getFloorPos(position);
                                Debug.Log("PlacingWalls");
                                placeWall(position, floorPos);
                               }
                            }
                        }
                    }
                }
            }
        }
    }

    private void placeWall(Vector3Int position, Vector3Int[] floorPos)
    {
        if(floorPos[0] == new Vector3Int(1, 0))
        {
            if(floorPos[1] == new Vector3Int(0, 1))
            {
                corridor.SetTile(position + new Vector3Int(-1, -1), bridgeTile);
                corridor.SetTile(position + new Vector3Int(1, 1), bridgeTile);
                corridor.SetTile(position + new Vector3Int(-1, 0), bridgeTile);
            }
            else
            {
                corridor.SetTile(position + new Vector3Int(-1, 1), bridgeTile);
                corridor.SetTile(position + new Vector3Int(1, -1), bridgeTile);
                corridor.SetTile(position + new Vector3Int(-1, 0), bridgeTile);
            }
        }
        else
        {
            if(floorPos[1] == new Vector3Int(0, 1))
            {
                corridor.SetTile(position + new Vector3Int(1, -1), bridgeTile);
                corridor.SetTile(position + new Vector3Int(-1, 1), bridgeTile);
                corridor.SetTile(position + new Vector3Int(1, 0), bridgeTile);
            }
            else
            {
                corridor.SetTile(position + new Vector3Int(1, 1), bridgeTile);
                corridor.SetTile(position + new Vector3Int(-1, -1), bridgeTile);
                corridor.SetTile(position + new Vector3Int(1, 0), bridgeTile);
            }   
        }
    }

    private Vector3Int[] getFloorPos(Vector3Int position)
    {
        Vector3Int[] floorPos = new Vector3Int[2];
        if(corridor.HasTile(position + new Vector3Int(1, 0)))
        {
            floorPos.Append(new Vector3Int(1, 0));
        }
        if(corridor.HasTile(position - new Vector3Int(1, 0)))
        {
            floorPos.Append(new Vector3Int(-1, 0));
        }
        if(corridor.HasTile(position + new Vector3Int(0, 1)))
        {
            floorPos.Append(new Vector3Int(0, 1));
        }
        if(corridor.HasTile(position - new Vector3Int(0, 1)))
        {
            floorPos.Append(new Vector3Int(0, -1));
        }
        return floorPos;
    }

    private bool checkCornerPiece(Vector3Int position)
    {
        if(corridor.HasTile(position + new Vector3Int(1, 0)) && corridor.HasTile(position - new Vector3Int(1, 0)) || corridor.HasTile(position + new Vector3Int(0, 1)) && corridor.HasTile(position - new Vector3Int(0, 1)))
        {
            return false;
        }
        else
        {
            return true;
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
        int xsize = grid.GetSize(0)/2;
        int ysize = grid.GetSize(1)/2;
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
                if(i != length && i != 0)
                {
                    corridor.SetTile(currentPos+new Vector3Int(0, -1), bridgeTile);
                }
                else if(i == length)
                {
                    corridor.SetTile(currentPos+new Vector3Int(0, -1), bridgeTile);
                }
                else{
                    corridor.SetTile(currentPos+new Vector3Int(0, -1), bridgeTile);
                }
                
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
                if(i != length && i != 0)
                {
                    corridor.SetTile(currentPos+new Vector3Int(0, -1), bridgeTile);
                    corridor.SetTile(currentPos+new Vector3Int(0, 1), bridgeTile);
                }
                else if(i == length)
                {
                    corridor.SetTile(currentPos+new Vector3Int(0, -1), bridgeTile);
                    corridor.SetTile(currentPos+new Vector3Int(0, 1), bridgeTile);
                }
                else{
                    corridor.SetTile(currentPos+new Vector3Int(0, -1), bridgeTile);
                    corridor.SetTile(currentPos+new Vector3Int(0, 1), bridgeTile);
                }
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
                bool alternate = i % 2 == 0;
                bool offset = false;
                corridor.SetTile(currentPos, bridgeTile);
                walls.SetTile(currentPos,null);
                
                    corridor.SetTile(currentPos+new Vector3Int(1, 0), bridgeTile);
                    corridor.SetTile(currentPos+new Vector3Int(-1, 0), bridgeTile);
                
                if(currentPos.x + xsize < 0)
                {
                    currentPos += new Vector3Int(1, 0, 0);
                    offset = true;
                }
                grid.SetValueAtLocation(currentPos.x + 1, currentPos.y, 1);
                grid.SetValueAtLocation(currentPos.x - 1, currentPos.y, 1);
                grid.SetValueAtLocation(currentPos.x, currentPos.y, 0);
                if(offset)
                {
                    currentPos += new Vector3Int(-1, 0, 0);
                    offset = false;
                }
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

    public void randomWalkerCorridors(Rooms room, Grid grid)
    {
        int xsize = grid.GetSize(0)/2;
        int ysize = grid.GetSize(1)/2;
        bool right = room.rightConnected;
        bool left = room.leftConnected;
        bool up = room.upConnected;
        bool down = room.downConnected;

        if(right)
        {

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

