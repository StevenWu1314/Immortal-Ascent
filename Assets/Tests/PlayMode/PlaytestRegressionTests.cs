using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Regression tests for the three defects the designer hit in playtest:
/// arrows stuck as items while the bow reported an empty quiver, no roaming out of combat, and every shot
/// reporting "no line of sight" because the aim overlay's player-relative cells were fed to a grid-space query.
/// </summary>
public class PlaytestRegressionTests
{
    [UnityTest]
    public IEnumerator TheQuiver_TakesOverTheStarterArrows()
    {
        yield return DungeonSceneFixture.LoadAndWaitForGeneration();

        Inventory inventory = Inventory.Instance;
        Assert.IsTrue(inventory != null, "the scene should have an Inventory");

        foreach (ItemStack stack in inventory.getItems())
        {
            if (stack.definition == null)
            {
                continue;
            }
            Assert.AreNotEqual(ItemKind.Ammo, stack.definition.Kind,
                $"'{stack.definition.DisplayName}' is still an item stack; arrows belong in the quiver count");
        }

        Assert.Greater(PlayerStats.Instance.Arrows, 0,
            "the arrows the player starts with have to reach the quiver, or the bow is unusable");
        Assert.IsTrue(PlayerStats.Instance.TryConsumeArrow(), "and the quiver can pay for a shot");
    }

    [UnityTest]
    public IEnumerator FiringAtAnEnemyWithAClearLine_ActuallyFires()
    {
        yield return DungeonSceneFixture.LoadAndWaitForGeneration();

        Controls controls = DungeonSceneFixture.Player();
        DungeonSceneFixture.EquipFirstWeapon(ranged: true);
        PlayerStats.Instance.AddArrows(ArrowEconomy.ArrowsPerOre);

        RangeAttackTilemap overlay = PlayerStats.Instance.GetComponentInChildren<RangeAttackTilemap>(true);
        Assert.IsTrue(overlay != null, "the player should carry the aim overlay");
        Assert.IsTrue(overlay.tilemap != null, "the overlay should have resolved its tilemap");

        Vector2Int start = DungeonSceneFixture.RegistryCell(controls);
        Vector2Int direction = DungeonSceneFixture.OpenDirection(start);
        Assert.AreNotEqual(Vector2Int.zero, direction, "no walkable neighbour to place the target on");
        Vector2Int target = start + direction;

        Enemy enemy = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length > 0
            ? Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)[0]
            : null;
        Assert.IsTrue(enemy != null, "the floor should have spawned enemies");
        int healthBefore = DungeonSceneFixture.ReadEnemyHealth(enemy);
        Assert.Greater(healthBefore, 0, "could not read the enemy's health");

        EntityManager.Instance.UnregisterEntity(enemy.gameObject);
        enemy.transform.position = new Vector3(target.x, target.y, enemy.transform.position.z);
        EntityManager.Instance.RegisterEntity(enemy.gameObject, target);

        overlay.ShowOverlay();
        try
        {
            // The overlay's cells are player-relative, which is exactly what the fire path has to convert.
            Vector3Int overlayCell = overlay.tilemap.WorldToCell(new Vector3(target.x, target.y, 0f));
            int arrowsBefore = PlayerStats.Instance.Arrows;

            bool fired = controls.TryFireAt(overlayCell);

            Assert.IsTrue(fired, "an adjacent enemy with a clear line has to be shootable");
            Assert.Less(DungeonSceneFixture.ReadEnemyHealth(enemy), healthBefore, "the shot lands");
            Assert.AreEqual(arrowsBefore - 1, PlayerStats.Instance.Arrows, "and it spends exactly one arrow");
        }
        finally
        {
            overlay.HideOverlay();
        }
    }

    [UnityTest]
    public IEnumerator UnawareEnemies_RoamWhileThePlayerExploresOutOfCombat()
    {
        yield return DungeonSceneFixture.LoadAndWaitForGeneration();

        EnemyBehavior roamer = null;
        foreach (EnemyBehavior candidate in Object.FindObjectsByType<EnemyBehavior>(FindObjectsSortMode.None))
        {
            if (candidate.currentstate == AwarenessState.Idle && HasFreeNeighbour(candidate))
            {
                roamer = candidate;
                break;
            }
        }
        Assert.IsTrue(roamer != null, "the floor should have an unaware enemy with somewhere to wander");

        Vector2Int before = CellOf(roamer);

        // The pulse is paced by time so a held movement key cannot make the enemies sprint.
        yield return new WaitForSeconds(1f);
        TurnManager.Instance.RoamPulse();
        yield return null;

        Assert.AreNotEqual(before, CellOf(roamer), "an unaware enemy should have wandered");
    }

    private static Vector2Int CellOf(EnemyBehavior enemy)
    {
        return new Vector2Int(Mathf.FloorToInt(enemy.transform.position.x),
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
                continue;
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
