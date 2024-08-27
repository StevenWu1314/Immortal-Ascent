using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.PlasticSCM.Editor.WebApi;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using Random=UnityEngine.Random;
public static class ProceduralGenerationAlgorithm
{
    public static List<BoundsInt> BinarySpacePartitioning(BoundsInt spaceToSplit, int minWidth, int minHeight, int numberOfRooms)
    {
        Queue<BoundsInt> roomsQue = new Queue<BoundsInt>();
        List<BoundsInt> roomsList = new List<BoundsInt>();
        roomsQue.Enqueue(spaceToSplit);
        while(roomsList.Count <= numberOfRooms)
        {
            var room = roomsQue.Dequeue();
            if(room.size.y >= minHeight  && room.size.x >= minWidth)
            {
                roomsList.Add(room);
                if(Random.value < 0.5f)
                {
                    if(room.size.y >= minHeight *2)
                    {
                        splitHorizontally(minHeight,roomsQue, room, roomsList);
                    }
                    else if(room.size.x >= minWidth *2)
                    {
                        splitVertically(minWidth, roomsQue, room, roomsList);
                    }
                }
                else
                {
                    if(room.size.x >= minWidth *2)
                    {
                        splitVertically(minWidth, roomsQue, room, roomsList);
                    }
                    else if(room.size.y >= minHeight *2)
                    {
                        splitHorizontally(minHeight,roomsQue, room, roomsList);
                    }
                }
                
            }
        }
        roomsList = resizeSpace(roomsList, 4);
        return roomsList;

    }

    private static void splitVertically(int minWidth, Queue<BoundsInt> roomsQue, BoundsInt room, List<BoundsInt> roomsList)
    {
        var xSplit = Random.Range(1, room.size.x);
        roomsList.Remove(room);
        BoundsInt room1 = new BoundsInt(room.min, new Vector3Int(xSplit, room.size.y, room.size.z));
        BoundsInt room2 = new BoundsInt(new Vector3Int(room.xMin + xSplit, room.yMin, room.zMin), 
        new Vector3Int(room.size.x - xSplit, room.size.y, room.size.z));
        roomsQue.Enqueue(room1);
        roomsQue.Enqueue(room2);
    }

    private static void splitHorizontally(int minHeight, Queue<BoundsInt> roomsQue, BoundsInt room, List<BoundsInt> roomsList)
    {
        roomsList.Remove(room);
        var ySplit = Random.Range(1, room.size.y);
        BoundsInt room1 = new BoundsInt(room.min, new Vector3Int(room.size.x, ySplit));
        BoundsInt room2 = new BoundsInt(new Vector3Int(room.xMin, room1.yMax), new Vector3Int(room.size.x, room.size.y-ySplit));    
        roomsQue.Enqueue(room1);
        roomsQue.Enqueue(room2);
    }

    private static List<BoundsInt> resizeSpace(List<BoundsInt> rooms, int shrink)
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            BoundsInt bound = rooms[i];

            // Shrink the size by 2 in both width and height
            bound.size = new Vector3Int(bound.size.x - shrink, bound.size.y - shrink, bound.size.z);

            // Reposition the bounds to keep it centered
            int variance = Random.Range(-shrink/2, shrink/2);
            variance = 0;
            bound.position = new Vector3Int(bound.position.x + variance, bound.position.y + variance, bound.position.z);

            // Update the boundsList with the new values
            rooms[i] = bound;
        }
        return rooms;


    }

    public static List<Rooms> BrachingRooms(int minRooms, int maxRooms)
    {
        int numberOfRooms = Random.Range(minRooms,  maxRooms);

        List<Rooms> rooms = new List<Rooms>();
        Queue<Rooms> roomsQue = new Queue<Rooms>();
        Rooms room = new Rooms(new BoundsInt(0, 0, -10, 11, 11, 20), new Vector2(0, 0));
        
        room.setType("Starting");
        rooms.Add(room);
        int direction = Random.Range(1, 4);
        room = room.Branch(direction);
        room.setType("Combat");
        int currentRooms = 2;
        roomsQue.Enqueue(room);
        rooms.Add(room);
        while(currentRooms < numberOfRooms && roomsQue.Count != 0)
        {
            Rooms workingRoom = roomsQue.Dequeue();
            bool overlap = false;
            
                List<int> availableSpace = workingRoom.availableDirection();
                int numberBranch = Random.Range(1, availableSpace.Count);
                
                for (int i = numberBranch; i > 0; i--)
                {
                    direction = Random.Range(1, numberBranch); 
                    Rooms branchedRoom = workingRoom.Branch(availableSpace[direction]);
                    foreach(Rooms test in rooms)
                    {
                        if(test != branchedRoom)
                        {
                            overlap = test.position == branchedRoom.position;
                            
                        }
                        
                        if(overlap)
                        {
                            workingRoom.setConnected(availableSpace[direction], false);
                            break;
                        }
                    
                    }
                    availableSpace.Remove(availableSpace[direction]);
                    if(!overlap)
                    {
                        roomsQue.Enqueue(branchedRoom);
                        rooms.Add(branchedRoom);
                        currentRooms++;  
                    }
                    
                }
            
            
        }
        
        return rooms;
    }

}
