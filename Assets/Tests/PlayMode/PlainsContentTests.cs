using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Plains content rules in the real scene (T09): where enemies may spawn, and that floor 2 is measurably
/// harder than floor 1.
/// </summary>
public class PlainsContentTests
{
    [UnityTest]
    public IEnumerator SpawnedEnemies_AreRegisteredOnRoomCells_NeverOnTheStartOrExitIsland()
    {
        yield return DungeonSceneFixture.LoadAndWaitForGeneration();

        int playerIsland = IslandManager.GetIsland(
            DungeonSceneFixture.RegistryCell(DungeonSceneFixture.Player()));

        EnemyBehavior[] enemies = Object.FindObjectsByType<EnemyBehavior>(FindObjectsSortMode.None);
        Assert.Greater(enemies.Length, 0, "a Plains floor should spawn enemies");

        var perIsland = new Dictionary<int, int>();
        foreach (EnemyBehavior enemy in enemies)
        {
            Vector2Int cell;
            Assert.IsTrue(EntityManager.Instance.TryGetCell(enemy.gameObject, out cell),
                $"{enemy.name} is not registered in EntityManager");

            Assert.IsTrue(Grid.IsWalkableTerrain(Grid.Current.GetValueAtLocation(cell.x, cell.y)),
                $"{enemy.name} stands on blocked terrain at {cell}");

            int island = IslandManager.GetIsland(cell);
            Assert.Greater(island, IslandManager.Passage,
                $"{enemy.name} is in a corridor or off the map at {cell}, not in a room");
            Assert.AreNotEqual(playerIsland, island, $"{enemy.name} spawned on the player's starting island");

            if (RunState.HasStairs)
            {
                Assert.AreNotEqual(IslandManager.GetIsland(RunState.StairsCell), island,
                    $"{enemy.name} spawned on the stairs island");
            }

            perIsland.TryGetValue(island, out int count);
            perIsland[island] = count + 1;
        }

        Assert.Greater(perIsland.Count, 0, "at least one combat island should be occupied");
        foreach (var pair in perIsland)
        {
            Assert.LessOrEqual(pair.Value, 2, $"island {pair.Key} has {pair.Value} enemies; the cap is 2 per island");
        }
        Assert.IsFalse(perIsland.ContainsKey(playerIsland), "the starting island stays clear");
    }

    [UnityTest]
    public IEnumerator TheSecondFloor_ScalesEnemyHealth()
    {
        yield return DungeonSceneFixture.LoadAndWaitForGeneration();

        Dictionary<string, int> firstFloor = HealthByType();

        RunState.AdvanceFloor();
        yield return null;
        yield return null;
        Assert.AreEqual(1, RunState.FloorIndex, "the run should be on its second floor");
        Assert.IsTrue(IslandManager.IsInitialized, "the new floor should be generated");

        Dictionary<string, int> secondFloor = HealthByType();
        int compared = 0;
        foreach (var pair in secondFloor)
        {
            if (!firstFloor.TryGetValue(pair.Key, out int baseHealth))
            {
                continue;   // a type that happens not to appear on both floors proves nothing
            }
            compared++;
            Assert.AreEqual(EnemyScaling.ScaleHealth(baseHealth, 1), pair.Value,
                $"{pair.Key} should be scaled by 1.15 on floor 2");
        }

        Assert.Greater(compared, 0, "no enemy type appeared on both floors, so the comparison could not run");
    }

    private static Dictionary<string, int> HealthByType()
    {
        var health = new Dictionary<string, int>();
        foreach (Enemy enemy in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            string type = enemy.name.Replace("(Clone)", string.Empty).Trim();
            int value = DungeonSceneFixture.ReadEnemyHealth(enemy);
            if (value <= 0)
            {
                continue;
            }
            // The lowest value per type: every instance of a type shares the same base and floor.
            health[type] = health.TryGetValue(type, out int existing) ? Mathf.Min(existing, value) : value;
        }
        return health;
    }
}
