using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Pure rules of the room builder (T01). No scene, no MonoBehaviours: <c>BrachingRooms</c> is static
/// code over <see cref="Random"/>, so a seed makes a layout reproducible.
///
/// This replaces the T00 characterization suite, which pinned the old behaviour (a request of 5
/// rooms yielded 4-6). The count is now a contract, and the last room is the floor's Stairs or Boss
/// room.
/// </summary>
public class MapgenTests
{
    private const int GridSize = 120;
    private const int MinRooms = 6;
    private const int MaxRooms = 8;
    private const int Seeds = 200;

    private static readonly Vector2Int[] Steps =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0),
        new Vector2Int(0, 1), new Vector2Int(0, -1)
    };

    private static List<Rooms> Generate(int seed, bool bossFloor = false,
                                        int gridSize = GridSize, int min = MinRooms, int max = MaxRooms)
    {
        Random.InitState(seed);
        return ProceduralGenerationAlgorithm.BrachingRooms(min, max, gridSize, bossFloor);
    }

    private static Vector2Int Key(Rooms room)
    {
        return new Vector2Int((int)room.position.x, (int)room.position.y);
    }

    [Test]
    public void RequestedCount_IsHonoured_OnEverySeed()
    {
        for (int seed = 1; seed <= Seeds; seed++)
        {
            List<Rooms> rooms = Generate(seed);
            Assert.That(rooms.Count, Is.InRange(MinRooms, MaxRooms),
                $"seed {seed} produced {rooms.Count} rooms, outside the requested {MinRooms}-{MaxRooms}");
        }
    }

    [Test]
    public void Rooms_NeverOverlap_AndStayInsideTheGrid()
    {
        for (int seed = 1; seed <= Seeds; seed++)
        {
            List<Rooms> rooms = Generate(seed);
            for (int a = 0; a < rooms.Count; a++)
            {
                BoundsInt first = rooms[a].body;
                Assert.GreaterOrEqual(first.xMin, 0, $"seed {seed}: room {a} escapes the grid");
                Assert.GreaterOrEqual(first.yMin, 0, $"seed {seed}: room {a} escapes the grid");
                Assert.LessOrEqual(first.xMax, GridSize, $"seed {seed}: room {a} escapes the grid");
                Assert.LessOrEqual(first.yMax, GridSize, $"seed {seed}: room {a} escapes the grid");

                for (int b = a + 1; b < rooms.Count; b++)
                {
                    BoundsInt second = rooms[b].body;
                    bool overlaps = first.xMin < second.xMax && second.xMin < first.xMax &&
                                    first.yMin < second.yMax && second.yMin < first.yMax;
                    Assert.IsFalse(overlaps, $"seed {seed}: rooms {a} and {b} overlap");
                }
            }
        }
    }

    [Test]
    public void Rooms_FormOneConnectedLayout()
    {
        for (int seed = 1; seed <= Seeds; seed++)
        {
            List<Rooms> rooms = Generate(seed);
            var byPosition = new Dictionary<Vector2Int, Rooms>();
            foreach (Rooms room in rooms)
            {
                byPosition[Key(room)] = room;
            }

            var visited = new HashSet<Vector2Int> { Key(rooms[0]) };
            var frontier = new Queue<Vector2Int>();
            frontier.Enqueue(Key(rooms[0]));
            while (frontier.Count > 0)
            {
                Vector2Int cell = frontier.Dequeue();
                foreach (Vector2Int step in Steps)
                {
                    Vector2Int next = cell + step;
                    if (byPosition.ContainsKey(next) && visited.Add(next))
                    {
                        frontier.Enqueue(next);
                    }
                }
            }

            Assert.AreEqual(rooms.Count, visited.Count,
                $"seed {seed}: only {visited.Count} of {rooms.Count} rooms are reachable from the starting room");
        }
    }

    [Test]
    public void NormalFloor_HasExactlyOneStairsRoom_ThatIsNotTheStartingRoom()
    {
        for (int seed = 1; seed <= 50; seed++)
        {
            List<Rooms> rooms = Generate(seed);
            int stairs = 0;
            int starting = 0;
            foreach (Rooms room in rooms)
            {
                if (room.type == "Stairs")
                {
                    stairs++;
                }
                if (room.type == "Starting")
                {
                    starting++;
                }
            }

            Assert.AreEqual(1, stairs, $"seed {seed}: expected exactly one Stairs room");
            Assert.AreEqual(1, starting, $"seed {seed}: expected exactly one Starting room");
            Assert.AreEqual("Stairs", rooms[rooms.Count - 1].type,
                $"seed {seed}: the Stairs room should be the last one placed");
        }
    }

    [Test]
    public void BossFloor_HasOneLargeBossRoom_AndNoStairs()
    {
        for (int seed = 1; seed <= 50; seed++)
        {
            List<Rooms> rooms = Generate(seed, bossFloor: true);
            Assert.That(rooms.Count, Is.InRange(MinRooms, MaxRooms), $"seed {seed}: short floor on a boss floor");

            int boss = 0;
            foreach (Rooms room in rooms)
            {
                Assert.AreNotEqual("Stairs", room.type, $"seed {seed}: a boss floor must not have stairs");
                if (room.type == "Boss")
                {
                    boss++;
                    Assert.GreaterOrEqual(room.body.size.x, 15,
                        $"seed {seed}: the boss arena is {room.body.size.x} wide, expected at least 15");
                    Assert.GreaterOrEqual(room.body.size.y, 15,
                        $"seed {seed}: the boss arena is {room.body.size.y} tall, expected at least 15");
                }
            }

            Assert.AreEqual(1, boss, $"seed {seed}: expected exactly one Boss room");
        }
    }

    [Test]
    public void SameSeed_ProducesTheSameLayout()
    {
        List<Rooms> first = Generate(4242);
        List<Rooms> second = Generate(4242);

        Assert.AreEqual(first.Count, second.Count, "same seed must give the same room count");
        for (int i = 0; i < first.Count; i++)
        {
            Assert.AreEqual(first[i].body, second[i].body, $"room {i} differs between identical seeds");
            Assert.AreEqual(first[i].position, second[i].position, $"room {i} position differs");
            Assert.AreEqual(first[i].type, second[i].type, $"room {i} type differs");
        }
    }

    [Test]
    public void SingleFreeDirection_StillPlacesARoom()
    {
        // The old builder indexed the free-direction list with a branch *count*, so a room with
        // exactly one direction left read past the end. It never tripped in 1200 stress runs, so this
        // pins the primitive directly rather than hoping a seed finds it.
        var room = new Rooms(new BoundsInt(10, 10, 0, 11, 11, 1), Vector2.zero);
        room.setConnected(1);
        room.setConnected(2);
        room.setConnected(3);

        List<int> free = room.availableDirection();
        Assert.AreEqual(1, free.Count, "this test needs a room with exactly one free direction");

        Rooms child = room.Branch(free[0]);
        Assert.Greater(child.body.size.x, 0, "branching on the last free direction should place a room");
        Assert.AreEqual(new Vector2(0, -1), child.position, "the only free direction was down");
    }

    [Test]
    public void TightGrid_ShortfallDoesNotThrow()
    {
        // A 40x40 grid cannot hold a 6-8 room band of 11-19 tile rooms, so every seed here takes the
        // rejection and shortfall paths. It must warn, not throw, and still return the starting room.
        for (int seed = 1; seed <= Seeds; seed++)
        {
            Random.InitState(seed);
            List<Rooms> rooms = null;
            Assert.DoesNotThrow(() => rooms = ProceduralGenerationAlgorithm.BrachingRooms(MinRooms, MaxRooms, 40),
                $"seed {seed} threw on a tight grid");
            Assert.IsNotNull(rooms);
            Assert.GreaterOrEqual(rooms.Count, 1, $"seed {seed}: at least the starting room must exist");
            Assert.LessOrEqual(rooms.Count, MaxRooms, $"seed {seed}: overshot the requested band");
        }
    }
}
