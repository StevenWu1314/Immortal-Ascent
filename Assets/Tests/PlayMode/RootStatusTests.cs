using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Root and spider webs in the real scene (T05, GDD 5.4, 9.1-9.4).
///
/// Input cannot be synthesised, so these drive <see cref="Controls.TryMove"/> and the turn pipeline, and
/// they plant the web themselves rather than hunting the floor for one. Run in batchmode or from the Test
/// Runner window, never through the bridge (see PlayModeSmokeTests for why).
/// </summary>
public class RootStatusTests
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
    public IEnumerator AWeb_RootsThePlayerForTwoRounds_ThenReleasesThem()
    {
        yield return DungeonSceneFixture.LoadAndWaitForGeneration();

        Controls controls = DungeonSceneFixture.Player();
        StatusController statuses = PlayerStats.Instance.Statuses;
        Assert.IsTrue(statuses != null, "the player should carry a StatusController");

        Vector2Int start = DungeonSceneFixture.RegistryCell(controls);
        Vector2Int direction = DungeonSceneFixture.OpenDirection(start);
        Assert.AreNotEqual(Vector2Int.zero, direction, "the spawn cell has no walkable neighbour to web");
        Vector2Int web = start + direction;

        // Plant a web the way a generated floor would: terrain code plus a registry entry.
        Grid grid = Grid.Current;
        grid.SetValueAtLocation(web.x, web.y, WebRegistry.WebCode);
        WebRegistry.Register(web);
        IslandManager.MarkWalkable(web);

        Assert.IsTrue(controls.TryMove(direction), "a web is walkable");
        Assert.AreEqual(web, DungeonSceneFixture.RegistryCell(controls), "the player stepped into it");
        Assert.IsTrue(statuses.IsRooted, "entering a web roots the player");
        Assert.IsTrue(statuses.Active, "and the HUD has something to show");
        Assert.IsFalse(WebRegistry.HasWeb(web), "the web is consumed on entry");
        Assert.AreEqual(0, grid.GetValueAtLocation(web.x, web.y), "the cell is plain floor again");

        // The starting island is empty, so that step was free (T03) and no round has passed yet.
        Assert.AreEqual(StatusController.RootRounds, statuses.Set.RoundsRemaining(StatusKind.Root),
            "no round has passed, so the Root has not ticked");

        // Rooted: the step is refused, but struggling still spends the round - otherwise a web on an empty
        // island would leave the player no way to let the Root expire. One struggle per remaining round.
        for (int remaining = StatusController.RootRounds; remaining > 0; remaining--)
        {
            turnEvents = 0;
            Assert.IsFalse(controls.TryMove(direction), "a rooted player cannot move");
            Assert.AreEqual(1, turnEvents, "struggling against the root spends the round");
            yield return DungeonSceneFixture.WaitForEnemyPhase();
            Assert.AreEqual(remaining - 1, statuses.Set.RoundsRemaining(StatusKind.Root),
                "each spent round ticks the Root exactly once");
        }

        Assert.IsFalse(statuses.IsRooted, "two rounds of Root are over");
        Assert.IsFalse(statuses.Active, "nothing is left active");
        Assert.IsTrue(controls.TryMove(direction), "and the player walks again");
    }

    [UnityTest]
    public IEnumerator WhileRooted_ThePlayerCanStillAttack()
    {
        yield return DungeonSceneFixture.LoadAndWaitForGeneration();

        Controls controls = DungeonSceneFixture.Player();
        StatusController statuses = PlayerStats.Instance.Statuses;
        Vector2Int start = DungeonSceneFixture.RegistryCell(controls);
        Vector2Int intoWeb = DungeonSceneFixture.OpenDirection(start);
        Assert.AreNotEqual(Vector2Int.zero, intoWeb, "the spawn cell has no walkable neighbour to web");
        Vector2Int web = start + intoWeb;

        Grid grid = Grid.Current;
        grid.SetValueAtLocation(web.x, web.y, WebRegistry.WebCode);
        WebRegistry.Register(web);
        IslandManager.MarkWalkable(web);
        Assert.IsTrue(controls.TryMove(intoWeb), "the player steps into the web");
        Assert.IsTrue(statuses.IsRooted, "and is rooted");

        // Stand an enemy next to the webbed player, with a melee weapon in hand.
        DungeonSceneFixture.EquipFirstWeapon(ranged: false);
        Vector2Int towardEnemy = DungeonSceneFixture.OpenDirection(web);
        Assert.AreNotEqual(Vector2Int.zero, towardEnemy, "the web cell has no free neighbour for the enemy");

        Enemy enemy = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length > 0
            ? Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)[0]
            : null;
        Assert.IsTrue(enemy != null, "the floor should have spawned enemies");
        int healthBefore = DungeonSceneFixture.ReadEnemyHealth(enemy);
        Assert.Greater(healthBefore, 0, "could not read the enemy's health");

        Vector2Int enemyCell = web + towardEnemy;
        EntityManager.Instance.UnregisterEntity(enemy.gameObject);
        enemy.transform.position = new Vector3(enemyCell.x, enemyCell.y, enemy.transform.position.z);
        EntityManager.Instance.RegisterEntity(enemy.gameObject, enemyCell);

        turnEvents = 0;
        bool acted = controls.TryMove(towardEnemy);

        Assert.IsTrue(acted, "attacking is still legal while rooted (GDD 6.2)");
        Assert.AreEqual(web, DungeonSceneFixture.RegistryCell(controls), "the rooted player does not move");
        Assert.Less(DungeonSceneFixture.ReadEnemyHealth(enemy), healthBefore, "the attack still lands");
        Assert.AreEqual(1, turnEvents, "and it still costs the turn");
    }
}
