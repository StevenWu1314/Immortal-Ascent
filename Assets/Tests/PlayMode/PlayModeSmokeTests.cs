using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Proves the runtime assembly works under Play mode.
///
/// Two tests with different jobs:
///
/// 1. <see cref="RuntimeGridAndIslandRules_RunInPlayMode"/> builds its own grid and island map - no scene
///    loading, no prefab wiring, nothing that can stall. It is the cheap always-runnable assertion.
///
/// 2. <see cref="DungeonScene_GeneratesLevel_WithIslandsAndARegisteredPlayer"/> loads the real
///    <c>Branching dungeon</c> scene and asserts generation, island building and player registration. This
///    is the test that actually proves the assembly split did not break the game's scene wiring, so it is
///    worth the extra weight.
///
/// Run these in <b>batchmode</b> (<c>-runTests -testPlatform PlayMode</c>, editor closed) or from the Test
/// Runner window. They must <b>not</b> be driven through the UnityAgentClient bridge: entering Play mode
/// performs a domain reload that aborts the bridge's request thread, and three bridge attempts hung the
/// editor (including one with a scene-free test, so the scene is not the cause).
/// </summary>
public class PlayModeSmokeTests
{
    [UnityTest]
    public IEnumerator RuntimeGridAndIslandRules_RunInPlayMode()
    {
        Grid previousGrid = Grid.Current;
        var grid = new Grid(24, 24, 1f, Vector3.zero);

        try
        {
            // Two 6x6 rooms joined by a one-tile corridor at y = 3.
            for (int x = 1; x <= 6; x++)
            {
                for (int y = 1; y <= 6; y++)
                {
                    grid.SetValueAtLocation(x, y, 0);
                }
            }

            for (int x = 12; x <= 17; x++)
            {
                for (int y = 1; y <= 6; y++)
                {
                    grid.SetValueAtLocation(x, y, 0);
                }
            }

            for (int x = 7; x <= 11; x++)
            {
                grid.SetValueAtLocation(x, 3, 0);
            }

            // A fresh Grid's array defaults to terrain 0, which is *walkable floor* - not a wall. A cell
            // only becomes Unassigned if its terrain is explicitly blocking, so paint one.
            grid.SetValueAtLocation(20, 20, 1);

            var rooms = new List<BoundsInt>
            {
                new BoundsInt(1, 1, 0, 6, 6, 1),
                new BoundsInt(12, 1, 0, 6, 6, 1)
            };

            IslandManager.Rebuild(rooms);
            yield return null;   // survive a frame, proving this is live Play mode code

            Assert.IsTrue(IslandManager.IsInitialized, "IslandManager did not initialise in Play mode");
            Assert.AreEqual(1, IslandManager.GetIsland(new Vector2Int(3, 3)), "first room should be island 1");
            Assert.AreEqual(2, IslandManager.GetIsland(new Vector2Int(14, 3)), "second room should be island 2");
            Assert.AreEqual(IslandManager.Passage, IslandManager.GetIsland(new Vector2Int(9, 3)),
                "the corridor between rooms is a passage, not a room");
            Assert.AreEqual(IslandManager.Unassigned, IslandManager.GetIsland(new Vector2Int(20, 20)),
                "a blocked cell is unassigned");

            Assert.IsTrue(Grid.IsWalkableTerrain(grid.GetValueAtLocation(9, 3)), "corridor floor is walkable");
            Assert.IsFalse(Grid.IsWalkableTerrain(grid.GetValueAtLocation(20, 20)), "wall terrain is blocked");
            Assert.AreEqual(new Vector2Int(3, 3), Grid.CellFromWorld(new Vector3(3.7f, 3.2f, 0f)),
                "world positions floor to grid cells");
        }
        finally
        {
            Grid.Current = previousGrid;
        }
    }

    [UnityTest]
    public IEnumerator DungeonScene_GeneratesLevel_WithIslandsAndARegisteredPlayer()
    {
        yield return DungeonSceneFixture.LoadAndWaitForGeneration();

        Assert.IsNotNull(EntityManager.Instance, "EntityManager is missing from the scene");
        Assert.IsNotNull(PlayerStats.Instance, "PlayerStats is missing; the player prefab did not spawn");
        Assert.Greater(EntityManager.Instance.AllEntities.Count, 0,
            "the entity registry is empty; the player and enemies were not registered");

        bool playerRegistered = false;
        foreach (var pair in EntityManager.Instance.AllEntities)
        {
            if (pair.Value != null && pair.Value.GetComponent<PlayerStats>() != null)
            {
                playerRegistered = true;
                break;
            }
        }

        Assert.IsTrue(playerRegistered, "the player entity is not in the entity registry");

        // The floor must actually use the 6-8 island band. This is the check that catches the new
        // serialized band fields deserializing as 0: a one-room floor still spawns a player and still
        // initialises islands, so every assertion above would pass while the game was broken.
        int islands = CountIslands();
        Assert.That(islands, Is.InRange(6, 8),
            $"the generated floor has {islands} islands, expected the 6-8 band from the GDD");
    }

    /// <summary>
    /// Descending has to build a whole new floor without losing the run or the player. This is the
    /// test that catches a regeneration that leaves the old player behind, or a new player that
    /// deletes itself because the previous one is still registered.
    /// </summary>
    [UnityTest]
    public IEnumerator AdvancingTheFloor_RebuildsTheLevel_WithExactlyOnePlayer()
    {
        yield return DungeonSceneFixture.LoadAndWaitForGeneration();

        Assert.IsTrue(RunState.HasRun, "entering the dungeon scene should start a run");
        Assert.AreEqual(0, RunState.FloorIndex, "a fresh run starts on floor 0");
        Assert.IsTrue(RunState.HasStairs, "a normal floor must offer stairs to descend");

        Vector2Int stairs = RunState.StairsCell;
        Assert.IsTrue(Grid.IsWalkableTerrain(Grid.Current.GetValueAtLocation(stairs.x, stairs.y)),
            $"the stairs at {stairs} must be a cell the player can stand on");

        RunState.AdvanceFloor();
        yield return null;
        yield return null;

        Assert.AreEqual(1, RunState.FloorIndex, "descending advances the floor");
        Assert.IsTrue(IslandManager.IsInitialized, "the new floor should be built");

        int playerBefore = PlayerStats.Instance.GetInstanceID();

        RunState.AdvanceFloor();
        yield return null;
        yield return null;

        Assert.AreEqual(1, RunState.FloorIndex, "descending advances the floor");
        Assert.IsTrue(IslandManager.IsInitialized, "the new floor should be built");
        Assert.IsNotNull(PlayerStats.Instance, "the new floor should have a player");

        int players = Object.FindObjectsByType<PlayerStats>(FindObjectsSortMode.None).Length;
        Assert.AreEqual(1, players, "descending must not leave a second player behind");
        Assert.AreEqual(playerBefore, PlayerStats.Instance.GetInstanceID(),
            "descending must keep the same player, so health, level and inventory carry over");
    }

    /// <summary>
    /// The chest pass used to be able to drop a chest (terrain 99, solid) on the cell the player spawns
    /// on, which left the player standing in blocked terrain and made the whole floor report as
    /// unreachable from the spawn. It hit roughly one floor in thirty, so this walks 30 seeded floors.
    /// </summary>
    [UnityTest]
    public IEnumerator GeneratedFloors_NeverPutAChestUnderThePlayer()
    {
        yield return DungeonSceneFixture.LoadAndWaitForGeneration();

        RoomFirstDungeonGenerator generator =
            Object.FindFirstObjectByType<RoomFirstDungeonGenerator>();
        Assert.IsTrue(generator != null, "the dungeon scene should have a generator");

        for (int i = 0; i < 30; i++)
        {
            int seed = 1000 + i;
            RunState.BeginRun(seed);
            generator.generateBranchingDungeon(RunState.IsBossFloor);
            yield return null;

            Vector2Int cell;
            bool registered = EntityManager.Instance.TryGetCell(PlayerStats.Instance.gameObject, out cell);
            Assert.IsTrue(registered, $"floor seed {seed} left the player unregistered");

            int terrain = Grid.Current.GetValueAtLocation(cell.x, cell.y);
            Assert.IsTrue(Grid.IsWalkableTerrain(terrain),
                $"floor seed {seed} left the player standing on terrain {terrain} at {cell}");
        }
    }

    /// <summary>Distinct island ids over the walkable terrain: one per room, corridors excluded.</summary>
    private static int CountIslands()
    {
        var ids = new HashSet<int>();
        Grid grid = Grid.Current;
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                if (!Grid.IsWalkableTerrain(grid.GetValueAtLocation(x, y)))
                {
                    continue;
                }

                int id = IslandManager.GetIsland(new Vector2Int(x, y));
                if (id > 0)
                {
                    ids.Add(id);
                }
            }
        }

        return ids.Count;
    }
}
