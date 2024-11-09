using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class TilePlacer : MonoBehaviour
{
    public Tilemap walls; // Reference to the walls Tilemap component
    public Tilemap floor; // Reference to the floor Tilemap component
    public Tilemap corridor;
    public TileBase floorTile; // Reference to the floor tile
    public TileBase flowerFloorTile; // Reference to the flower tile
    public TileBase wallTileTop1; // Reference to the first top wall tile
    public TileBase wallTileTop2; // Reference to the second top wall tile
    public TileBase wallTileBottom1; // Reference to the first bottom wall tile
    public TileBase wallTileBottom2; // Reference to the second bottom wall tile
    public TileBase wallTileLeft1; // Reference to the first left wall tile
    public TileBase wallTileLeft2; // Reference to the second left wall tile
    public TileBase wallTileRight1; // Reference to the first right wall tile
    public TileBase wallTileRight2; // Reference to the second right wall tile
    public TileBase wallTileTopLeft; // Reference to the top-left wall tile
    public TileBase wallTileTopRight; // Reference to the top-right wall tile
    public TileBase wallTileBottomLeft; // Reference to the bottom-left wall tile
    public TileBase wallTileBottomRight; // Reference to the bottom-right wall tile
    public TileBase wallLefttoDown; // Reference to the leftside->Downside connection tile
    public TileBase wallRighttoDown; // Reference to the rightside->Downside connection tile
    public TileBase wallDowntoLeft;
    public TileBase wallDowntoRight;
    public TileBase chest;
    DungeonSpecifications dungeonSpecifications;

   // List of BoundsInt

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
                        walls.SetTile(pos, wallTileBottomLeft); // Bottom-left corner
                        grid.SetValueAtLocation(pos.x + xsize, pos.y + ysize, 1);
                    }
                    else if (x == bound.xMax - 1 && y == bound.yMin)
                    {
                        walls.SetTile(pos, wallTileBottomRight); // Bottom-right corner
                        grid.SetValueAtLocation(pos.x + xsize, pos.y + ysize, 1);
                    }
                    else if (x == bound.xMin && y == bound.yMax - 1)
                    {
                        walls.SetTile(pos, wallTileTopLeft); // Top-left corner
                        grid.SetValueAtLocation(pos.x + xsize, pos.y + ysize, 1);
                    }
                    else if (x == bound.xMax - 1 && y == bound.yMax - 1)
                    {
                        walls.SetTile(pos, wallTileTopRight); // Top-right corner
                        grid.SetValueAtLocation(pos.x + xsize, pos.y + ysize, 1);
                    }
                    else if (x == bound.xMin)
                    {
                        walls.SetTile(pos, alternate ? wallTileLeft1 : wallTileLeft2); // Left wall
                        grid.SetValueAtLocation(pos.x + xsize, pos.y + ysize, 1);
                    }
                    else if (x == bound.xMax - 1)
                    {
                        walls.SetTile(pos, alternate ? wallTileRight1 : wallTileRight2); // Right wall
                        grid.SetValueAtLocation(pos.x + xsize, pos.y + ysize, 1);
                        
                    }
                    else if (y == bound.yMin)
                    {
                        walls.SetTile(pos, alternate ? wallTileBottom1 : wallTileBottom2); // Bottom wall
                        grid.SetValueAtLocation(pos.x + xsize, pos.y + ysize, 1);
                    }
                    else if (y == bound.yMax - 1)
                    {
                        walls.SetTile(pos, alternate ? wallTileTop1 : wallTileTop2); // Top wall
                        grid.SetValueAtLocation(pos.x + xsize, pos.y + ysize, 1);
                    }
                    else
                    {
                        int rand = Random.Range(0, 100);
                        if(rand < 97)
                        {
                            floor.SetTile(pos, floorTile); // Floor
                        }
                        else
                        {
                            floor.SetTile(pos, flowerFloorTile);
                        }
                        
                    }
                }
            }
        }
    }


    /*
    public void createCorridors(List<BoundsInt> rooms)
    {
        for(int i = 0; i < rooms.Count-1; i++)
        {
            int[] closestwalls = findClosestWalls(rooms[i], rooms[i+1]);
            buildpath(rooms[i], rooms[i+1], closestwalls);
        }

    }

    private void buildpath(BoundsInt room1, BoundsInt room2, int[] closestwalls)
    {
        Vector3Int start = new Vector3Int();
        Vector3Int end = new Vector3Int();;
        switch(closestwalls[0])
        {
            case 1:
                start = new Vector3Int((int)room1.center.x,room1.yMax);
                break;
            case 2:
                start = new Vector3Int(room1.xMax, (int)room1.center.y);
                break;
            case 3:
                start = new Vector3Int((int)room1.center.x,room1.yMin);
                break;
            case 4:
                start = new Vector3Int(room1.xMin, (int)room1.center.y);
                break;
        }
        switch(closestwalls[1])
        {
            case 1:
                end = new Vector3Int((int)room2.center.x,room2.yMax);
                break;
            case 2:
                end = new Vector3Int(room2.xMax, (int)room2.center.y);
                break;
            case 3:
                end = new Vector3Int((int)room2.center.x,room2.yMin);
                break;
            case 4:
                end = new Vector3Int(room2.xMin, (int)room2.center.y);
                break;
        }

        bool right = closestwalls[0] == 2;
        bool up = closestwalls[0] == 1;
        int amountHori;
        
        if(right)
        {
            amountHori = (int)(room2.center.x - room1.xMax);
            for(int i = 0; i < amountHori; i++)
            {
                corridor.SetTile(start+new Vector3Int(i, 0), floorTile);
            }
            Debug.Log("Right: "+amountHori);
        }
        else
        {
            amountHori = (int)(room2.center.x - room1.xMin);
            for(int i = amountHori; i < 0; i++)
            {
                corridor.SetTile(end+new Vector3Int(-i, 0), floorTile);
            }
            Debug.Log("Left: "+amountHori);
            
        }
        if (up)
        {
            int amountVori = (int)(room2.yMin - room1.center.y);
            for(int i = 0; i < amountVori; i++)
            {
                corridor.SetTile(start+new Vector3Int(amountHori, i), floorTile);
            }
            Debug.Log("up: " + amountVori);
        }
        else
        {
            int amountVori = (int)( room1.center.y - room2.yMax);
            for(int i = amountVori; i > 0; i--)
            {
                corridor.SetTile(end+new Vector3Int(amountHori, i), floorTile);
            }
            Debug.Log("down: " + amountVori);
        }
    }

    public int[] findClosestWalls(BoundsInt room1, BoundsInt room2)
    {
        // using array to represent wall sides first entry is room 1 second is room 2
        // up = 1, right = 2, down = 3, left = 4
        int[] walls = new int[2];
        float xdist = room1.center.x - room2.center.x;
        float ydist = room1.center.y - room2.center.y;
        //check if second room is to the right of first
        bool right = xdist < 0;
        //check if second room is above the first
        bool up = ydist < 0;
        //check if the x distance between the two rooms is small enough to be overlapping. 
        //(room1.size.x + room2.size.x)/2 would be the maximum distance for any overlap
        // - 2 at the end because we only want the following logic to apply when it's better 
        //to connect top to bottom or vice versa
        /*
                    17
            /////////////////
            /   8   /   8   /
            /////////////////       17
                            /////////////////
                            /   8   /   8   /
                            /////////////////

                    |_______________|
                            17
        
        bool xoverlap = math.abs(xdist) < (room1.size.x + room2.size.x)/2 - right ? (math.abs(room2.x)): ;
        //same logic to the xoverlap for the y overlap
        bool yoverlap = math.abs(ydist) < (room1.size.y + room2.size.y)/2 - 2;
        bool xBigY = math.abs(xdist) > math.abs(ydist);

        if(xoverlap)
        {
            walls[0] = up ? 1 : 3;
            walls[1] = up ? 3 : 1;
            return walls;
        }

        if(yoverlap)
        {
            walls[0] = right ? 2 : 4;
            walls[1] = right ? 4 : 2;
            return walls;
        }
        //following is just a decision tree to determin which two walls are the closest
        if (right)
        {
            
                walls[0] = 2;
                walls[1] = up ? 3 : 1;
            
        }
        else
        {
            if (xoverlap)
            {
                walls[0] = xBigY ? 4 : (up ? 1 : 3);
                walls[1] = xBigY ? 2 : (up ? 3 : 1);
            }
            else
            {
                walls[0] = 4;
                walls[1] = up ? 3 : 1;
            }
        }
            return walls;
    }
    */
        
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
        while (current.y != destination.y)
        {
            bool alternate = (current.x + current.y) % 2 == 0;
            Vector3Int floorPos = new Vector3Int((int)current.x, (int)current.y);
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
            if(floor.GetTile(floorPos) == null)
            {
                corridor.SetTile(floorPos, floorTile);
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
        }
        while (current.x != destination.x)
        {
            bool alternate = (current.x + current.y) % 2 == 0;
            Vector3Int floorPos = new Vector3Int((int)current.x, (int)current.y);
            /*if(walls.GetTile(floorPos-new Vector3Int(0, 1))==null && floor.GetTile(floorPos-new Vector3Int(0, 1))==null )
            {
                walls.SetTile(floorPos-new Vector3Int(0, 1), alternate ? wallTileBottom1 : wallTileBottom2);
            }
            if(walls.GetTile(floorPos+new Vector3Int(0, 1))==null && floor.GetTile(floorPos+new Vector3Int(0, 1))==null)
            {
                walls.SetTile(floorPos+new Vector3Int(0, 1), alternate ? wallTileBottom1 : wallTileBottom2);
            }
            */
            if(floor.GetTile(floorPos) == null)
            {
                corridor.SetTile(floorPos, floorTile);
            }
            if(current.x > destination.x)
            {
                
                current -= new Vector3Int(1, 0);
            }
            else
            { 
                current += new Vector3Int(1, 0);
            }
        }
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
                walls.SetTile(position + new Vector3Int(-1, -1), wallTileBottomLeft);
                walls.SetTile(position + new Vector3Int(1, 1), wallTileBottom2);
                walls.SetTile(position + new Vector3Int(-1, 0), wallTileLeft1);
            }
            else
            {
                walls.SetTile(position + new Vector3Int(-1, 1), wallLefttoDown);
                walls.SetTile(position + new Vector3Int(1, -1), wallTileTop2);
                walls.SetTile(position + new Vector3Int(-1, 0), wallTileLeft1);
            }
        }
        else
        {
            if(floorPos[1] == new Vector3Int(0, 1))
            {
                walls.SetTile(position + new Vector3Int(1, -1), wallTileBottomRight);
                walls.SetTile(position + new Vector3Int(-1, 1), wallTileBottom2);
                walls.SetTile(position + new Vector3Int(1, 0), wallTileRight1);
            }
            else
            {
                walls.SetTile(position + new Vector3Int(1, 1), wallRighttoDown);
                walls.SetTile(position + new Vector3Int(-1, -1), wallRighttoDown);
                walls.SetTile(position + new Vector3Int(1, 0), wallTileRight1);
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

    public void branchingCorridors(List<Rooms> Rooms, Grid grid)
    {
        foreach (Rooms room in Rooms)
        {
            branchingCorridors(room, grid);
        }
    }

    private void branchingCorridors(Rooms room, Grid grid)
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
            for(int i = 0; i < 21; i++)
            {
                bool alternate = i % 2 == 0;
                corridor.SetTile(currentPos, floorTile);
                walls.SetTile(currentPos,null);
                walls.SetTile(currentPos+new Vector3Int(0, 1), alternate ? wallTileTop1: wallTileTop2);
                grid.SetValueAtLocation(currentPos.x + xsize, currentPos.y + ysize+1, 1);
                grid.SetValueAtLocation(currentPos.x + xsize, currentPos.y + ysize-1, 1);
                grid.SetValueAtLocation(currentPos.x + xsize, currentPos.y + ysize, 0);
                if(i != 21 && i != 0)
                {
                    walls.SetTile(currentPos+new Vector3Int(0, -1), alternate ? wallTileBottom1: wallTileBottom2);
                }
                else if(i == 21)
                {
                    walls.SetTile(currentPos+new Vector3Int(0, -1), wallRighttoDown);
                }
                else{
                    walls.SetTile(currentPos+new Vector3Int(0, -1), wallLefttoDown);
                }
                
                currentPos += new Vector3Int(1, 0, 0);
            }
        }
        if(left)
        {
            currentPos = new Vector3Int(room.body.xMin, (int)room.body.center.y, 0);
            for(int i = 0; i < 21; i++)
            {
                bool alternate = i % 2 == 0;
                
                grid.SetValueAtLocation(currentPos.x + xsize, currentPos.y + ysize + 1, 1);
                grid.SetValueAtLocation(currentPos.x + xsize, currentPos.y + ysize - 1, 1);
                grid.SetValueAtLocation(currentPos.x + xsize, currentPos.y + ysize, 0);
                corridor.SetTile(currentPos, floorTile);
                walls.SetTile(currentPos,null);
                if(i != 21 && i != 0)
                {
                    walls.SetTile(currentPos+new Vector3Int(0, -1), alternate ? wallTileBottom1: wallTileBottom2);
                    walls.SetTile(currentPos+new Vector3Int(0, 1), alternate ? wallTileBottom1 : wallTileBottom2);
                }
                else if(i == 21)
                {
                    walls.SetTile(currentPos+new Vector3Int(0, -1), wallLefttoDown);
                    walls.SetTile(currentPos+new Vector3Int(0, 1), wallDowntoLeft);
                }
                else{
                    walls.SetTile(currentPos+new Vector3Int(0, -1), wallRighttoDown);
                    walls.SetTile(currentPos+new Vector3Int(0, 1), wallDowntoRight);
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
            for(int i = 0; i < 22; i++)
            {
                bool alternate = i % 2 == 0;
                bool offset = false;
                corridor.SetTile(currentPos, floorTile);
                walls.SetTile(currentPos,null);
                if(i != 21 && i != 0)
                {
                    walls.SetTile(currentPos+new Vector3Int(1, 0), alternate ? wallTileRight1: wallTileRight2);
                    walls.SetTile(currentPos+new Vector3Int(-1, 0), alternate ? wallTileLeft1: wallTileLeft2);
                }
                else if(i == 21)
                {
                    walls.SetTile(currentPos+new Vector3Int(-1, 0), wallRighttoDown);
                    walls.SetTile(currentPos+new Vector3Int(1, 0), wallLefttoDown);
                }
                if(currentPos.x + xsize < 0)
                {
                    currentPos += new Vector3Int(1, 0, 0);
                    offset = true;
                }
                grid.SetValueAtLocation(currentPos.x + xsize + 1, currentPos.y + ysize, 1);
                grid.SetValueAtLocation(currentPos.x + xsize - 1, currentPos.y + ysize, 1);
                grid.SetValueAtLocation(currentPos.x + xsize, currentPos.y + ysize, 0);
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
            grid.SetValueAtLocation(currentPos.x + xsize, currentPos.y + ysize, 0);
            for(int i = 0; i < 21; i++)
            {
                //corridor.SetTile(currentPos, floorTile);
                walls.SetTile(currentPos,null);
                currentPos += new Vector3Int(0, -1, 0);
            }
        }
    }

    public void generateLootTiles(List<BoundsInt> roomsSpace, Grid grid)
    {
        foreach (BoundsInt room in roomsSpace)
        {
            int spawnLoot = Random.Range(1, 11);
            if(spawnLoot > 5)
            {
                int x = Random.Range(room.xMin+3, room.xMax-3);
                int y = Random.Range(room.yMin+3, room.yMax-3);
                Vector3 pos = new Vector3(x, y);
                grid.SetValueAtWorldLocation(pos, 99);
                floor.SetTile(new Vector3Int(x, y), chest);
            }
        }
    }
}

