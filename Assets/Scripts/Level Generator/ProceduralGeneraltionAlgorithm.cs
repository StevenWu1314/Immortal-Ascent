using System.Collections.Generic;
using UnityEngine;
using Random=UnityEngine.Random;
public static class ProceduralGenerationAlgorithm
{
    public static List<Rooms> BrachingRooms(int minRooms, int maxRooms, int gridSize)
    {
        int numberOfRooms = Random.Range(minRooms,  maxRooms);

        List<Rooms> rooms = new List<Rooms>();
        Queue<Rooms> roomsQue = new Queue<Rooms>();
        Rooms room = new Rooms(new BoundsInt(gridSize/2, gridSize/2, -10, 11, 11, 20), new Vector2(0, 0));
        
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
                    if(branchedRoom.body.xMin <= 0 || branchedRoom.body.y <= 0 || branchedRoom.body.xMax >= gridSize || branchedRoom.body.yMax >= gridSize)
                    {
                        workingRoom.setConnected(availableSpace[direction], false);
                        break;
                    }
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
