using System.Collections;
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
        TurnManager manager = TurnManager.Instance;
        float deadline = Time.realtimeSinceStartup + 15f;
        while (manager.IsEnemyPhaseRunning && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }
        Assert.IsFalse(manager.IsEnemyPhaseRunning, "the enemy phase never finished");
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
}
