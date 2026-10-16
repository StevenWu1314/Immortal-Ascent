using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Characterization tests for the room builder as it behaves <b>today</b>, before the M1 T01 rewrite.
///
/// Verified behaviour (measured with the suite plus MapgenDiagnostics.RoomBuilderStress, 1200 runs):
/// the generator does <b>not</b> throw, and it produces <b>no</b> overlapping rooms. What it does do
/// is under-deliver: requesting 5 rooms yields 4-6, and requesting up to 40 never yields more than 18,
/// because the loop exits when the queue drains instead of honouring the request. The loop condition is
/// also checked once per outer iteration while an inner pass can add several rooms, which is where the
/// overshoot to 6 comes from.
///
/// The index math is nevertheless unsound - <c>Random.Range(1, availableSpace.Count)</c> is used to
/// index a list that can have one element, and a candidate that is rejected does not reset the
/// <c>overlap</c> flag for the next candidate - so T01 replaces it. These tests pin the properties that
/// hold today so that rewrite is visibly a change, and they deliberately do not assert a room-count
/// contract that the current generator cannot meet.
///
/// When T01 lands, this file should be replaced by the stronger generator suite.
/// </summary>
public class MapgenCharacterizationTests
{
    private const int GridSize = 120;
    private const int RequestedRooms = 5;
    private const int SeedsToScan = 500;

    private struct ScanResult
    {
        public int threw;
        public int fiveRooms;
        public int maxRoomsSeen;
        public int seedWithFiveRooms;
        public List<Rooms> layout;
        public string firstError;
    }

    private static ScanResult Scan()
    {
        var result = new ScanResult
        {
            seedWithFiveRooms = -1,
            layout = null,
            firstError = null,
            maxRoomsSeen = 0
        };

        for (int seed = 1; seed <= SeedsToScan; seed++)
        {
            UnityEngine.Random.InitState(seed);
            List<Rooms> rooms;
            try
            {
                rooms = ProceduralGenerationAlgorithm.BrachingRooms(RequestedRooms, RequestedRooms, GridSize);
            }
            catch (System.Exception e)
            {
                result.threw++;
                if (result.firstError == null)
                {
                    result.firstError = $"{e.GetType().Name}: {e.Message}";
                }
                continue;
            }

            result.maxRoomsSeen = Mathf.Max(result.maxRoomsSeen, rooms.Count);
            if (rooms.Count == RequestedRooms)
            {
                result.fiveRooms++;
                if (result.seedWithFiveRooms == -1)
                {
                    result.seedWithFiveRooms = seed;
                    result.layout = rooms;
                }
            }
        }

        return result;
    }

    [Test]
    public void Generator_CanProduceAFiveRoomLayout()
    {
        ScanResult scan = Scan();

        Debug.Log($"[TEST] mapgen characterization: {scan.threw}/{SeedsToScan} seeds threw, " +
                  $"{scan.fiveRooms} produced {RequestedRooms} rooms, max rooms seen {scan.maxRoomsSeen}" +
                  (scan.firstError != null ? $", first error {scan.firstError}" : ""));

        Assert.AreNotEqual(-1, scan.seedWithFiveRooms,
            $"no seed in 1..{SeedsToScan} produced a {RequestedRooms}-room layout without throwing");
    }

    [Test]
    public void Generator_FiveRoomLayout_StaysInsideTheGrid_WithOneStartingRoom()
    {
        ScanResult scan = Scan();
        Assert.AreNotEqual(-1, scan.seedWithFiveRooms, "needs a working seed; see the test above");

        List<Rooms> rooms = scan.layout;
        Assert.AreEqual(RequestedRooms, rooms.Count);

        int starting = 0;
        foreach (Rooms room in rooms)
        {
            if (room.type == "Starting")
            {
                starting++;
            }

            Assert.GreaterOrEqual(room.body.xMin, 0, $"room at {room.position} escapes the grid");
            Assert.GreaterOrEqual(room.body.yMin, 0, $"room at {room.position} escapes the grid");
            Assert.LessOrEqual(room.body.xMax, GridSize, $"room at {room.position} escapes the grid");
            Assert.LessOrEqual(room.body.yMax, GridSize, $"room at {room.position} escapes the grid");
        }

        Assert.AreEqual(1, starting, "exactly one room should be the starting room");
    }

    [Test]
    public void Generator_IsDeterministic_ForAGivenSeed()
    {
        ScanResult scan = Scan();
        Assert.AreNotEqual(-1, scan.seedWithFiveRooms, "needs a working seed; see the tests above");
        int seed = scan.seedWithFiveRooms;

        UnityEngine.Random.InitState(seed);
        List<Rooms> first = ProceduralGenerationAlgorithm.BrachingRooms(RequestedRooms, RequestedRooms, GridSize);
        UnityEngine.Random.InitState(seed);
        List<Rooms> second = ProceduralGenerationAlgorithm.BrachingRooms(RequestedRooms, RequestedRooms, GridSize);

        Assert.AreEqual(first.Count, second.Count, "same seed must give the same room count");
        for (int i = 0; i < first.Count; i++)
        {
            Assert.AreEqual(first[i].body, second[i].body, $"room {i} differs between identical seeds");
            Assert.AreEqual(first[i].position, second[i].position, $"room {i} position differs");
            Assert.AreEqual(first[i].type, second[i].type, $"room {i} type differs");
        }
    }
}
