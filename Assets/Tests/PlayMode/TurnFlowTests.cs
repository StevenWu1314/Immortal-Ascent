using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Turn flow in the real scene (T03, GDD 6.1-6.3): what costs a turn, what is free, and that the input
/// lock lives exactly as long as the enemy phase does.
///
/// Input cannot be synthesised, so these drive the same entry points the keyboard drives -
/// <see cref="Controls.TryMove"/> for a step and the turn pipeline's public API - rather than
/// reimplementing the rules in the test. Run in batchmode or from the Test Runner window, never through
/// the bridge (see PlayModeSmokeTests for why).
/// </summary>
public class TurnFlowTests
{
    private static int turnEvents;

    private static void CountTurnEvent(Controls controls)
    {
        turnEvents++;
    }

    [SetUp]
    public void SetUp()
    {
        turnEvents = 0;
        Controls.onMoveEvent += CountTurnEvent;
    }

    [TearDown]
    public void TearDown()
    {
        Controls.onMoveEvent -= CountTurnEvent;
    }

    [UnityTest]
    public IEnumerator BlockedStep_IsFree_AndRaisesNoTurnEvent()
    {
        yield return DungeonSceneFixture.LoadAndWaitForGeneration();

        Controls controls = DungeonSceneFixture.Player();
        Vector2Int start = DungeonSceneFixture.RegistryCell(controls);
        Vector2Int direction = DungeonSceneFixture.OpenDirection(start);
        Assert.AreNotEqual(Vector2Int.zero, direction, "the spawn cell has no walkable neighbour to test");
        Vector2Int blockedCell = start + direction;

        // Refuse it on terrain, so the refusal is not about the island gate. The grid is restored below
        // rather than regenerated, so the rest of the scene is untouched.
        Grid grid = Grid.Current;
        int original = grid.GetValueAtLocation(blockedCell.x, blockedCell.y);
        grid.SetValueAtLocation(blockedCell.x, blockedCell.y, 1);
        try
        {
            bool moved = controls.TryMove(direction);

            Assert.IsFalse(moved, "a wall must refuse the step");
            Assert.AreEqual(start, DungeonSceneFixture.RegistryCell(controls), "the player must not have moved");
            Assert.AreEqual(0, turnEvents, "a refused step must not start the enemy phase");
            Assert.IsFalse(controls.takingTurn, "a refused step must not lock input");
        }
        finally
        {
            grid.SetValueAtLocation(blockedCell.x, blockedCell.y, original);
        }
    }

    [UnityTest]
    public IEnumerator SteppingInAClearedRoom_IsFree()
    {
        yield return DungeonSceneFixture.LoadAndWaitForGeneration();

        Controls controls = DungeonSceneFixture.Player();
        Vector2Int start = DungeonSceneFixture.RegistryCell(controls);
        int island = IslandManager.GetIsland(start);
        Assert.Greater(island, IslandManager.Passage, "the player should spawn inside a room");
        Assert.AreEqual(0, IslandManager.CountLivingEnemies(island),
            "the generator never spawns an enemy into the starting room, so the run starts out of combat");

        Vector2Int direction = DungeonSceneFixture.OpenDirection(start);
        Assert.AreNotEqual(Vector2Int.zero, direction, "the spawn cell has no walkable neighbour to test");

        bool moved = controls.TryMove(direction);

        Assert.IsTrue(moved, "a walkable neighbour in a cleared room should be steppable");
        Assert.AreEqual(start + direction, DungeonSceneFixture.RegistryCell(controls), "the player should have stepped one cell");
        Assert.AreEqual(0, turnEvents, "out of combat, movement must not start the enemy phase");
        Assert.IsFalse(controls.takingTurn, "out of combat, movement must not lock input");
    }

    [UnityTest]
    public IEnumerator BumpingAnEnemy_AttacksIt_AndCostsTheTurn()
    {
        yield return DungeonSceneFixture.LoadAndWaitForGeneration();

        Controls controls = DungeonSceneFixture.Player();
        Vector2Int start = DungeonSceneFixture.RegistryCell(controls);
        Vector2Int direction = DungeonSceneFixture.OpenDirection(start);
        Assert.AreNotEqual(Vector2Int.zero, direction, "the spawn cell has no walkable neighbour to test");

        DungeonSceneFixture.EquipFirstWeapon(ranged: false);
        Enemy enemy = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length > 0
            ? Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)[0]
            : null;
        Assert.IsTrue(enemy != null, "the floor should have spawned enemies");
        int healthBefore = DungeonSceneFixture.ReadEnemyHealth(enemy);
        Assert.Greater(healthBefore, 0, "could not read the enemy's health");

        // Stand it next to the player: that is also what makes the room turn-based.
        Vector2Int target = start + direction;
        EntityManager.Instance.UnregisterEntity(enemy.gameObject);
        enemy.transform.position = new Vector3(target.x, target.y, enemy.transform.position.z);
        EntityManager.Instance.RegisterEntity(enemy.gameObject, target);
        Assert.Greater(IslandManager.CountLivingEnemies(IslandManager.GetIsland(start)), 0,
            "the placed enemy should make the room turn-based");

        bool acted = controls.TryMove(direction);

        Assert.IsTrue(acted, "walking into an enemy is an action");
        Assert.AreEqual(start, DungeonSceneFixture.RegistryCell(controls), "the player attacks instead of stepping into it");
        Assert.Less(DungeonSceneFixture.ReadEnemyHealth(enemy), healthBefore, "the bump must damage the enemy");
        Assert.AreEqual(1, turnEvents, "a melee attack ends the turn");
        Assert.IsTrue(controls.takingTurn, "the input lock is held while the enemy phase runs");

        yield return DungeonSceneFixture.WaitForEnemyPhase();

        Assert.IsFalse(controls.takingTurn, "the enemy phase must release the input lock when it finishes");
    }

    [UnityTest]
    public IEnumerator EnemyPhase_IsSingleFlight_AndReleasesTheLockWhenItEnds()
    {
        yield return DungeonSceneFixture.LoadAndWaitForGeneration();

        Controls controls = DungeonSceneFixture.Player();
        TurnManager manager = TurnManager.Instance;
        Assert.IsTrue(manager != null, "the scene should have a TurnManager");

        EnemyBehavior[] behaviours = Object.FindObjectsByType<EnemyBehavior>(FindObjectsSortMode.None);
        Assert.Greater(behaviours.Length, 0, "the floor should have spawned enemies");

        // Queue one enemy so the phase has work and cannot complete inside the call.
        manager.queupToMove(behaviours[0]);
        controls.takingTurn = true;
        manager.callTaketurn(controls);
        Assert.IsTrue(manager.IsEnemyPhaseRunning, "the first call starts a phase");

        // A second call while a phase is in flight must not start another one. Two phases used to run
        // at once, and the first coroutine's finally released the lock underneath the second.
        manager.callTaketurn(controls);

        yield return DungeonSceneFixture.WaitForEnemyPhase();

        Assert.IsFalse(controls.takingTurn, "the lock must be released when the phase ends, not left stuck");
    }

    // ------------------------------------------------------------------ helpers


}
