using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Awareness, roaming and sight in the real scene (T04, GDD 6.5-6.7).
///
/// Uses the same entry points the game does - the turn pipeline's public API and the range overlay -
/// because input cannot be synthesised. Run in batchmode or from the Test Runner window, never through
/// the bridge (see PlayModeSmokeTests for why).
/// </summary>
public class EnemyAwarenessTests
{
    [UnityTest]
    public IEnumerator APlainsRoomWideRealm_DoesNotNoticeThePlayerInAnotherRoom()
    {
        yield return DungeonSceneFixture.LoadAndWaitForGeneration();

        Assert.AreEqual(AggroMode.RoomWide, RunState.EnemyAggroMode, "M1 ships the Plains realm");

        Controls controls = DungeonSceneFixture.Player();
        int playerIsland = IslandManager.GetIsland(DungeonSceneFixture.RegistryCell(controls));

        EnemyBehavior enemy = FirstEnemyOffIsland(playerIsland);
        Assert.IsTrue(enemy != null, "the floor should have an enemy outside the player's room");

        yield return DungeonSceneFixture.DriveEnemyPhase(enemy);

        Assert.AreEqual(AwarenessState.Idle, enemy.currentstate,
            "room-wide awareness must not reach across rooms, however close the enemy is");
    }

    [UnityTest]
    public IEnumerator AnUnawareEnemy_Roams()
    {
        yield return DungeonSceneFixture.LoadAndWaitForGeneration();

        EnemyBehavior roamer = null;
        foreach (EnemyBehavior candidate in Object.FindObjectsByType<EnemyBehavior>(FindObjectsSortMode.None))
        {
            if (HasFreeNeighbour(candidate))
            {
                roamer = candidate;
                break;
            }
        }
        Assert.IsTrue(roamer != null, "no unaware enemy on this floor has anywhere to roam");

        Vector2Int start = CellOf(roamer);
        int startIsland = IslandManager.GetIsland(start);
        bool moved = false;
        for (int round = 0; round < 8 && !moved; round++)
        {
            yield return DungeonSceneFixture.DriveEnemyPhase(roamer);
            moved = CellOf(roamer) != start;
        }

        Assert.IsTrue(moved, $"an unaware enemy with a free neighbour should wander; it is still at {start}");
        Assert.AreEqual(startIsland, IslandManager.GetIsland(CellOf(roamer)),
            "roaming must stay inside the enemy's own room - an enemy loose in a corridor locks the player in");
    }

    [UnityTest]
    public IEnumerator AnAwareEnemy_CommitsToThePlayer_InsteadOfWandering()
    {
        yield return DungeonSceneFixture.LoadAndWaitForGeneration();

        EnemyBehavior enemy = FirstEnemyOffIsland(IslandManager.GetIsland(
            DungeonSceneFixture.RegistryCell(DungeonSceneFixture.Player())));
        Assert.IsTrue(enemy != null, "the floor should have an enemy outside the player's room");

        float before = Vector3.Distance(enemy.transform.position, DungeonSceneFixture.Player().transform.position);
        enemy.becomeAlerted();

        yield return DungeonSceneFixture.DriveEnemyPhase(enemy);

        float after = Vector3.Distance(enemy.transform.position, DungeonSceneFixture.Player().transform.position);
        Assert.AreEqual(AwarenessState.Chase, enemy.currentstate, "an alerted enemy stays committed");
        // One cell of slack: the direct step toward the player can be blocked, and NextSteps then sidesteps -
        // which can be slightly further away for that round. What must not happen is the enemy wandering off.
        Assert.LessOrEqual(after, before + 2f,
            "a committed enemy closes the distance, or is stopped by its own room - it never wanders away");
    }

    [UnityTest]
    public IEnumerator TheAimOverlay_OnlyPaintsCellsWithLineOfSight()
    {
        yield return DungeonSceneFixture.LoadAndWaitForGeneration();

        DungeonSceneFixture.EquipFirstWeapon(ranged: true);

        RangeAttackTilemap overlay = PlayerStats.Instance.GetComponentInChildren<RangeAttackTilemap>(true);
        Assert.IsTrue(overlay != null, "the player should carry the range overlay");
        Assert.IsTrue(overlay.tilemap != null, "the overlay should have resolved its tilemap");

        Vector2Int levelFrom = DungeonSceneFixture.RegistryCell(DungeonSceneFixture.Player());
        Vector2Int overlayFrom = (Vector2Int) overlay.tilemap.WorldToCell(
            DungeonSceneFixture.Player().transform.position);
        int reach = Equipments.Instance.getRange();
        Assert.Greater(reach, 0, "the equipped bow should have reach");

        overlay.ShowOverlay();
        try
        {
            // The overlay's cells are player-relative (its own Grid), so read tiles in overlay space while
            // the sight test runs in level space. Iterating the expected disc beats tilemap.cellBounds,
            // whose bounds are recomputed lazily and can still describe the previous contents.
            int painted = 0;
            for (int x = -reach; x <= reach; x++)
            {
                for (int y = -reach; y <= reach; y++)
                {
                    if (overlay.tilemap.GetTile(new Vector3Int(overlayFrom.x + x, overlayFrom.y + y, 0)) == null)
                    {
                        continue;
                    }
                    painted++;
                    Assert.IsTrue(
                        LineOfSight.HasClearShot(Grid.Current, levelFrom,
                            new Vector2Int(levelFrom.x + x, levelFrom.y + y)),
                        $"the preview painted an offset ({x},{y}) with no line of sight from {levelFrom}");
                }
            }

            Assert.Greater(painted, 0, "with a bow equipped the overlay must paint cells around the player");
        }
        finally
        {
            overlay.HideOverlay();
        }
    }

    // ------------------------------------------------------------------ helpers

    private static EnemyBehavior FirstEnemyOffIsland(int island)
    {
        foreach (EnemyBehavior enemy in Object.FindObjectsByType<EnemyBehavior>(FindObjectsSortMode.None))
        {
            if (IslandManager.GetIsland(CellOf(enemy)) != island)
            {
                return enemy;
            }
        }
        return null;
    }

    private static Vector2Int CellOf(EnemyBehavior enemy)
    {
        return new Vector2Int(
            Mathf.FloorToInt(enemy.transform.position.x),
            Mathf.FloorToInt(enemy.transform.position.y));
    }

    private static bool HasFreeNeighbour(EnemyBehavior enemy)
    {
        Vector2Int here = CellOf(enemy);
        int island = IslandManager.GetIsland(here);
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (Vector2Int direction in directions)
        {
            Vector2Int cell = here + direction;
            if (!Grid.IsWalkableTerrain(Grid.Current.GetValueAtLocation(cell.x, cell.y)))
            {
                continue;
            }
            if (island >= 0 && IslandManager.GetIsland(cell) != island)
            {
                continue;   // enemies stay in their own room
            }
            if (EntityManager.Instance.IsOccupied(cell))
            {
                continue;
            }
            return true;
        }
        return false;
    }
}
