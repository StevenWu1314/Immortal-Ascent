using System.Collections.Generic;
using UnityEngine;
using Random=UnityEngine.Random;
public static class ProceduralGenerationAlgorithm
{
    /// <summary>
    /// Side of the boss arena, forced so the fight has room. Square, odd, and the largest a room
    /// ever gets, which is what "bigger than a normal room" reduces to today.
    /// </summary>
    public const int BossRoomSize = 19;

    /// <summary>
    /// Grow a room tree until it holds a room count inside [<paramref name="minRooms"/>,
    /// <paramref name="maxRooms"/>]. The count is the contract: the loop stops the moment the
    /// target is reached, so it can neither fall short of a queue that drained early nor overshoot
    /// by adding several children in one visit.
    ///
    /// The last room added becomes the floor's <c>Stairs</c> room, or its <c>Boss</c> room when
    /// <paramref name="bossFloor"/> is set. Deterministic for a given <see cref="Random"/> state, so
    /// tests seed with <c>Random.InitState</c>.
    /// </summary>
    public static List<Rooms> BrachingRooms(int minRooms, int maxRooms, int gridSize, bool bossFloor = false)
    {
        // Never let a bad band produce zero rooms: the starting room has to exist or nothing
        // downstream (player spawn, island rebuild) has anything to work with.
        int min = Mathf.Max(1, minRooms);
        int max = Mathf.Max(min, maxRooms);
        int target = Random.Range(min, max + 1);

        List<Rooms> rooms = new List<Rooms>();
        Queue<Rooms> pending = new Queue<Rooms>();

        Rooms room = new Rooms(new BoundsInt(gridSize/2, gridSize/2, -10, 11, 11, 20), new Vector2(0, 0));
        room.setType("Starting");
        rooms.Add(room);
        pending.Enqueue(room);

        while (rooms.Count < target && pending.Count != 0)
        {
            Rooms workingRoom = pending.Dequeue();
            List<int> availableSpace = workingRoom.availableDirection();
            if (availableSpace.Count == 0)
            {
                // Every direction already branches somewhere; this room cannot host another child.
                continue;
            }

            bool placed = false;
            while (availableSpace.Count != 0 && rooms.Count < target)
            {
                // Index a valid slot, then drop it, so the list can never be read out of range.
                int pick = Random.Range(0, availableSpace.Count);
                int direction = availableSpace[pick];
                availableSpace.RemoveAt(pick);

                int forcedSize = bossFloor && rooms.Count == target - 1 ? BossRoomSize : 0;
                Rooms branchedRoom = workingRoom.Branch(direction, forcedSize);

                if (IsOutside(branchedRoom, gridSize) || Overlaps(rooms, branchedRoom))
                {
                    // Branch already claimed the direction; undo it so no corridor is carved into
                    // nothing. The direction is dropped from availableSpace, never retried in this
                    // pass: a rejected direction stays rejected, because nothing about it can change.
                    workingRoom.setConnected(direction, false);
                    continue;
                }

                rooms.Add(branchedRoom);
                pending.Enqueue(branchedRoom);
                placed = true;
                break;
            }

            // A room that placed a child can host another one later. Without this the frontier only
            // ever grows through the newest child, runs out of space in the grid's corners, and the
            // floor comes out short of the requested band - which also robs the boss room of the size
            // it is supposed to be forced to.
            if (placed && rooms.Count < target && workingRoom.availableDirection().Count != 0)
            {
                pending.Enqueue(workingRoom);
            }
        }

        if (rooms.Count < target)
        {
            // Loud, but a warning rather than an error: a small grid legitimately cannot fit the
            // requested band, and LogError would fail unrelated play-mode tests through LogAssert.
            Debug.LogWarning($"[GEN] room builder placed {rooms.Count} of {target} requested rooms on a " +
                             $"{gridSize}x{gridSize} grid; the floor is smaller than requested.");
        }

        if (rooms.Count > 1)
        {
            rooms[rooms.Count - 1].setType(bossFloor ? "Boss" : "Stairs");
        }

        return rooms;
    }

    private static bool IsOutside(Rooms room, int gridSize)
    {
        BoundsInt body = room.body;
        return body.xMin <= 0 || body.yMin <= 0 || body.xMax >= gridSize || body.yMax >= gridSize;
    }

    /// <summary>BoundsInt has no Intersects; half-open on both edges, like the diagnostics' check.</summary>
    private static bool Overlaps(List<Rooms> rooms, Rooms candidate)
    {
        foreach (Rooms other in rooms)
        {
            if (other.body.xMin < candidate.body.xMax && candidate.body.xMin < other.body.xMax &&
                other.body.yMin < candidate.body.yMax && candidate.body.yMin < other.body.yMax)
            {
                return true;
            }
        }

        return false;
    }
}
