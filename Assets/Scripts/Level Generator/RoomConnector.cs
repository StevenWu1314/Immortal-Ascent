using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public static class RoomConnector 
{
    public static List<BoundsInt> SortRoomsByDistance(List<BoundsInt> rooms)
    {
    /*
        List<BoundsInt> sortedRooms = new List<BoundsInt>();
        //add the first room to the list
        sortedRooms.Add(rooms[0]);
        for (int i = 0; i < rooms.Count-1; i++)
        {
            var room = rooms[i];
            //sets an inital closets room for comparison
            var currentClosestRoom = rooms[i+1];
            for (int j = i; j < rooms.Count; j++)
            {
                //makes sure we're not checking the distance between the same room and that we're not checking it against the rooms before it
                if(room != rooms[j] && !sortedRooms.Contains(rooms[j]))
                {
                    //checks the distance between the centers of two rooms and compare it against the distance of the current closest room
                    if(Vector3.Distance(room.center, rooms[j].center) < Vector3.Distance(room.center, currentClosestRoom.center))
                    {
                        //updates the closest room
                        currentClosestRoom = rooms[j];  
                    }
                }
            }
            //add the closets room to the sorted list after the comparison is done
            sortedRooms.Add(currentClosestRoom);
        }
        foreach (var room in rooms)
        if(!sortedRooms.Contains(room))
        {
            sortedRooms.Add(room);
        }
        sortedRooms = sortedRooms.Distinct().ToList();
        
        return sortedRooms;
    } */
    List<BoundsInt> sortedRooms = new List<BoundsInt>();
    var room = rooms[Random.Range(0, rooms.Count)];
    rooms.Remove(room);
    sortedRooms.Add(room);
    while (rooms.Count != 0)
    {
        BoundsInt closest = FindClosestPointTo(room, rooms);
        rooms.Remove(closest);
        sortedRooms.Add(closest);
        room = closest;
    }
    return sortedRooms;
    }

    private static BoundsInt FindClosestPointTo(BoundsInt room, List<BoundsInt> rooms)
    {
        BoundsInt closest = new BoundsInt();
        float distance = float.MaxValue;
        foreach (BoundsInt roomI in rooms)
        {
            float currentDistance = Vector2.Distance(room.center, roomI.center);
            if (currentDistance < distance)
            {
                closest = roomI;
                distance = currentDistance;
            }
        }
        return closest;
    }
}
