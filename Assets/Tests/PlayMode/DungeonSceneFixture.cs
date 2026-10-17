using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Shared setup for the PlayMode suites that need the real dungeon scene generated. Kept in one place
/// so the scene name and the timeout cannot drift between suites.
/// </summary>
internal static class DungeonSceneFixture
{
    public const string SceneName = "Branching dungeon";
    private const float GenerationTimeoutSeconds = 20f;

    /// <summary>Load the dungeon scene and wait until its generator has built a floor.</summary>
    public static IEnumerator LoadAndWaitForGeneration()
    {
        SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
        yield return null;

        float deadline = Time.realtimeSinceStartup + GenerationTimeoutSeconds;
        while ((Grid.Current == null || !IslandManager.IsInitialized) && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        Assert.IsNotNull(Grid.Current,
            $"Grid.Current is null after {GenerationTimeoutSeconds}s - generation did not run in '{SceneName}'");
        Assert.IsTrue(IslandManager.IsInitialized,
            "IslandManager was never rebuilt; generation did not complete");
    }

    /// <summary>The player's Controls. Asserts the scene actually produced a driveable player.</summary>
    public static Controls Player()
    {
        Assert.IsTrue(PlayerStats.Instance != null, "the scene should have spawned a player");
        Controls controls = PlayerStats.Instance.GetComponent<Controls>();
        Assert.IsTrue(controls != null, "the player should have a Controls component");
        return controls;
    }

    /// <summary>The cell the registry says the player is in, falling back to the transform.</summary>
    public static Vector2Int RegistryCell(Controls controls)
    {
        Vector2Int cell;
        if (EntityManager.Instance != null && EntityManager.Instance.TryGetCell(controls.gameObject, out cell))
        {
            return cell;
        }
        return Vector2Int.FloorToInt(controls.transform.position);
    }

    /// <summary>Wait for the enemy phase to drain, then assert it really finished.</summary>
    public static IEnumerator WaitForEnemyPhase()
    {
        // Re-read the instance each frame: a test that ends by reloading the scene (or by dying) leaves the
        // captured TurnManager destroyed, and its flag would read whatever it held at teardown - which made this
        // helper fail with "the enemy phase never finished" once in a full run and pass in isolation.
        float deadline = Time.realtimeSinceStartup + 15f;
        while (Time.realtimeSinceStartup < deadline)
        {
            TurnManager manager = TurnManager.Instance;
            if (manager == null || !manager.IsEnemyPhaseRunning)
            {
                yield break;   // finished, or the scene that owned the phase is gone
            }
            yield return null;
        }

        // A phase that is still running after 15s on a live manager is a real hang, not a flake.
        Assert.IsFalse(TurnManager.Instance != null && TurnManager.Instance.IsEnemyPhaseRunning,
            "the enemy phase never finished");
    }

    /// <summary>
    /// Run one full enemy phase for a single enemy, the way a player turn would: queue it, take the input
    /// lock, start the phase and wait it out.
    /// </summary>
    public static IEnumerator DriveEnemyPhase(EnemyBehavior enemy)
    {
        TurnManager manager = TurnManager.Instance;
        Controls controls = Player();
        manager.queupToMove(enemy);
        controls.takingTurn = true;
        manager.callTaketurn(controls);
        yield return WaitForEnemyPhase();
    }

    /// <summary>Equip the first definition of a kind, for tests that need a weapon in hand.</summary>
    public static void EquipFirstWeapon(bool ranged)
    {
        ItemDefinition found = null;
        foreach (ItemDefinition definition in Resources.LoadAll<ItemDefinition>("ItemDefinitions"))
        {
            if (definition.Kind == ItemKind.Weapon && definition.Ranged == ranged)
            {
                found = definition;
                break;
            }
        }
        Assert.IsTrue(found != null,
            $"no {(ranged ? "ranged" : "melee")} weapon definition exists under Resources/ItemDefinitions");
        Equipments.Instance.Equip(new EquipmentInstance(found));
    }

    /// <summary>
    /// A direction whose neighbour is walkable and on the same island, so a test that blocks or webs it
    /// afterwards is not fighting the island gate. Zero when there is no such neighbour.
    /// </summary>
    public static Vector2Int OpenDirection(Vector2Int from)
    {
        Grid grid = Grid.Current;
        int island = IslandManager.GetIsland(from);
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (Vector2Int direction in directions)
        {
            Vector2Int cell = from + direction;
            if (!Grid.IsWalkableTerrain(grid.GetValueAtLocation(cell.x, cell.y)))
            {
                continue;
            }
            if (IslandManager.GetIsland(cell) != island)
            {
                continue;
            }
            if (EntityManager.Instance != null && EntityManager.Instance.IsOccupied(cell))
            {
                continue;
            }
            return direction;
        }
        return Vector2Int.zero;
    }

    /// <summary>Enemy.health is a protected serialized field with no public getter.</summary>
    public static int ReadEnemyHealth(Enemy enemy)
    {
        FieldInfo field = typeof(Enemy).GetField("health", BindingFlags.Instance | BindingFlags.NonPublic);
        return field != null ? (int)field.GetValue(enemy) : -1;
    }
}
