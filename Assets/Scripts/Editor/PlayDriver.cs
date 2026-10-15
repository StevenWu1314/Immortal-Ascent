using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only driver for hands-on testing of the running game.
///
/// The game reads the legacy <see cref="Input"/> API directly inside Controls.Update, and that
/// state is native and read-only, so a script cannot synthesise a key press. This instead drives
/// the same systems a key press drives, in the same order Controls.Update does: the island gate,
/// the Grid, EntityManager (which is what resolves melee combat on the target cell), and finally
/// the turn pipeline through Controls.onMoveEvent.
///
/// Play mode only. Pair it with the UnityAgentClient bridge:
/// <c>POST /execute_method {"type":"PlayDriver","method":"StepTowardNearestEnemy"}</c>
/// </summary>
public static class PlayDriver
{
    private const string Tag = "[PLAY]";

    /// <summary>Where the player is, how the fight is going, and how much is left standing.</summary>
    [MenuItem("Tools/Play/Status")]
    public static void Status()
    {
        if (!Ready(out Controls controls, out Vector2Int cell))
        {
            return;
        }

        PlayerStats stats = PlayerStats.Instance;
        int island = IslandManager.GetIsland(cell);
        int enemies = CountEnemies();
        int chests = CountUnopenedChests();

        Debug.Log($"{Tag} cell={cell} island={island} cleared={IslandManager.IsIslandCleared(island)} " +
                  $"hp={ReadHealth(stats)}/{stats.EffectiveMaxHealth} str={stats.Strength} dex={stats.Dexterity} " +
                  $"takingTurn={controls.takingTurn} enemies={enemies} unopenedChests={chests}");
    }

    /// <summary>One step toward the nearest living enemy. Walking into it is what triggers melee.</summary>
    [MenuItem("Tools/Play/Step Toward Nearest Enemy")]
    public static void StepTowardNearestEnemy()
    {
        if (!Ready(out Controls controls, out Vector2Int cell))
        {
            return;
        }

        if (!TryNearestEnemyCell(cell, out Vector2Int target))
        {
            Debug.Log($"{Tag} no living enemy left.");
            return;
        }

        // Prefer a real path: rooms are ringed by a solid wall, so a straight line into the wall
        // just stalls. Fall back to the dominant axis when no route exists.
        if (TryPathStep(cell, target, out Vector2Int direction))
        {
            Step(controls, cell, direction);
            return;
        }

        Vector2Int delta = target - cell;
        direction = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
            ? new Vector2Int(delta.x > 0 ? 1 : -1, 0)
            : new Vector2Int(0, delta.y > 0 ? 1 : -1);

        Step(controls, cell, direction);
    }

    /// <summary>
    /// First step of a breadth-first route from <paramref name="from"/> to <paramref name="to"/> over
    /// cells the Grid calls walkable. Occupancy is deliberately ignored: walking into an enemy is
    /// how melee happens, so a blocked cell is a valid step and not an obstacle to route around.
    /// The island gate is not modelled here either; Step applies the real one.
    /// </summary>
    private static bool TryPathStep(Vector2Int from, Vector2Int to, out Vector2Int direction)
    {
        direction = Vector2Int.zero;

        Grid grid = Grid.Current;
        if (grid == null)
        {
            return false;
        }

        var cameFrom = new Dictionary<Vector2Int, Vector2Int> { [from] = from };
        var frontier = new Queue<Vector2Int>();
        frontier.Enqueue(from);

        Vector2Int[] neighbours = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        int guard = 0;

        while (frontier.Count > 0 && guard++ < 40000)
        {
            Vector2Int current = frontier.Dequeue();
            if (current == to)
            {
                break;
            }

            foreach (Vector2Int step in neighbours)
            {
                Vector2Int next = current + step;
                if (cameFrom.ContainsKey(next) || !grid.InBounds(next.x, next.y))
                {
                    continue;
                }
                if (!Grid.IsWalkableTerrain(grid.GetValueAtLocation(next.x, next.y)))
                {
                    continue;
                }
                cameFrom[next] = current;
                frontier.Enqueue(next);
            }
        }

        if (!cameFrom.ContainsKey(to))
        {
            return false;
        }

        // Walk the chain back until the step that left `from`.
        Vector2Int cursor = to;
        while (cameFrom[cursor] != from)
        {
            cursor = cameFrom[cursor];
            if (cursor == from)
            {
                return false;
            }
        }

        direction = cursor - from;
        return direction != Vector2Int.zero;
    }

    [MenuItem("Tools/Play/Step North")]
    public static void StepNorth() => StepFree(Vector2Int.up);

    [MenuItem("Tools/Play/Step South")]
    public static void StepSouth() => StepFree(Vector2Int.down);

    [MenuItem("Tools/Play/Step East")]
    public static void StepEast() => StepFree(Vector2Int.right);

    [MenuItem("Tools/Play/Step West")]
    public static void StepWest() => StepFree(Vector2Int.left);

    /// <summary>Open the chest the player is standing next to, if any.</summary>
    [MenuItem("Tools/Play/Open Adjacent Chest")]
    public static void OpenAdjacentChest()
    {
        if (!Ready(out Controls controls, out Vector2Int cell))
        {
            return;
        }

        if (!ChestRegistry.TryGetAdjacent(cell, out Chest chest))
        {
            Debug.Log($"{Tag} no chest adjacent to {cell}.");
            return;
        }

        bool opened = chest.Open(Inventory.Instance);
        Debug.Log($"{Tag} chest at {chest.Cell} open={opened} " +
                  $"grid={Grid.Current.GetValueAtLocation(chest.Cell.x, chest.Cell.y)} " +
                  $"island={IslandManager.GetIsland(chest.Cell)}");
    }

    private static void StepFree(Vector2Int direction)
    {
        if (!Ready(out Controls controls, out Vector2Int cell))
        {
            return;
        }
        Step(controls, cell, direction);
    }

    /// <summary>
    /// Dump every registered entity's cell next to its transform. A display-versus-code offset
    /// shows up either as the derived cell disagreeing with the registry, or as a fractional
    /// position: tiles are drawn centred on cell + 0.5, and entities spawn on the integer corner,
    /// so a pose is only "clean" if derived == registered.
    /// </summary>
    [MenuItem("Tools/Play/Dump Entity Positions")]
    public static void DumpPositions()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning($"{Tag} needs Play mode.");
            return;
        }

        Grid grid = Grid.Current;
        if (grid == null || EntityManager.Instance == null)
        {
            Debug.LogWarning($"{Tag} no Grid.Current or EntityManager.");
            return;
        }

        Debug.Log($"{Tag} --- registered entities ---");
        foreach (var pair in EntityManager.Instance.AllEntities)
        {
            GameObject entity = pair.Value;
            if (entity == null)
            {
                continue;
            }

            string kind = entity.GetComponent<Enemy>() != null ? "enemy"
                        : entity.GetComponent<PlayerStats>() != null ? "player"
                        : "other";
            Vector3 p = entity.transform.position;
            Vector2Int derived = Grid.CellFromWorld(p);
            Debug.Log($"{Tag} {kind} {entity.name,-18} reg={pair.Key} " +
                      $"pos=({p.x:0.###}, {p.y:0.###}, {p.z:0.###}) derived={derived} " +
                      $"{(derived == pair.Key ? "cell-ok" : "CELL-MISMATCH")}");
        }
    }

    /// <summary>
    /// The body of one movement step, mirroring the movement branch of Controls.Update.
    /// </summary>
    private static void Step(Controls controls, Vector2Int from, Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
        {
            Debug.Log($"{Tag} zero direction; nothing to do.");
            return;
        }

        if (controls.takingTurn)
        {
            Debug.Log($"{Tag} the turn pipeline is still running; wait a moment and step again.");
            return;
        }

        Vector2Int to = from + direction;
        Vector3 fromWorld = new Vector3(from.x, from.y, 0f);

        if (!IslandManager.CanMoveTo(fromWorld, to))
        {
            Debug.Log($"{Tag} island gate blocks {from} -> {to}: {IslandManager.DescribeBlock(fromWorld)}");
            return;
        }

        Grid grid = Grid.Current;
        if (grid == null)
        {
            Debug.LogError($"{Tag} Grid.Current is null.");
            return;
        }
        if (!grid.Move(fromWorld, direction))
        {
            Debug.Log($"{Tag} terrain blocks {from} -> {to} " +
                      $"(grid={grid.GetValueAtLocation(to.x, to.y)}).");
            return;
        }

        // This is the step that resolves melee: MoveEntity attacks when the target cell holds an
        // enemy, and otherwise moves the transform itself.
        EntityManager.Instance.MoveEntity(controls.gameObject, from, to);

        // A real key press locks input first, then raises the event; the turn pipeline's finally
        // block is what clears takingTurn again.
        controls.takingTurn = true;
        RaiseMoveEvent(controls);

        int health = ReadHealth(PlayerStats.Instance);
        Vector3 pose = controls.transform.position;
        Vector2Int derived = Grid.CellFromWorld(pose);
        Debug.Log($"{Tag} stepped {from} -> {to} hp={health} str={PlayerStats.Instance.Strength} " +
                  $"island={IslandManager.GetIsland(to)} enemies={CountEnemies()} " +
                  $"pos=({pose.x:0.##},{pose.y:0.##}) " +
                  $"derived={(derived == to ? "ok" : derived.ToString())}");
    }

    /// <summary>
    /// Controls.onMoveEvent is a field-like static event, so only Controls can raise it. Reach the
    /// compiler-generated backing field so the real subscribers (TurnManager, every EnemyBehavior)
    /// run exactly as they do for a key press.
    /// </summary>
    private static void RaiseMoveEvent(Controls controls)
    {
        FieldInfo field = typeof(Controls).GetField("onMoveEvent",
            BindingFlags.Static | BindingFlags.NonPublic);
        if (field == null)
        {
            Debug.LogError($"{Tag} Controls.onMoveEvent backing field not found.");
            return;
        }

        var handler = field.GetValue(null) as System.Action<Controls>;
        if (handler == null)
        {
            Debug.Log($"{Tag} no onMoveEvent subscribers.");
            return;
        }
        handler.Invoke(controls);
    }

    /// <summary>Play mode, a player, and a registry cell for it. </summary>
    private static bool Ready(out Controls controls, out Vector2Int cell)
    {
        controls = null;
        cell = default;

        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning($"{Tag} needs Play mode.");
            return false;
        }

        PlayerStats stats = PlayerStats.Instance;
        if (stats == null || EntityManager.Instance == null)
        {
            Debug.LogWarning($"{Tag} no PlayerStats or EntityManager in the scene.");
            return false;
        }

        controls = stats.GetComponent<Controls>();
        if (controls == null)
        {
            Debug.LogWarning($"{Tag} the player has no Controls component.");
            return false;
        }

        if (!EntityManager.Instance.TryGetCell(controls.gameObject, out cell))
        {
            Debug.LogWarning($"{Tag} the player is not registered in EntityManager.");
            return false;
        }

        return true;
    }

    private static bool TryNearestEnemyCell(Vector2Int from, out Vector2Int cell)
    {
        cell = default;
        float best = float.MaxValue;
        bool found = false;

        foreach (var pair in EntityManager.Instance.AllEntities)
        {
            GameObject entity = pair.Value;
            if (entity == null || entity.GetComponent<Enemy>() == null)
            {
                continue;
            }

            float distance = Vector2Int.Distance(from, pair.Key);
            if (distance < best)
            {
                best = distance;
                cell = pair.Key;
                found = true;
            }
        }
        return found;
    }

    private static int CountEnemies()
    {
        int count = 0;
        foreach (var pair in EntityManager.Instance.AllEntities)
        {
            if (pair.Value != null && pair.Value.GetComponent<Enemy>() != null)
            {
                count++;
            }
        }
        return count;
    }

    private static int CountUnopenedChests()
    {
        int count = 0;
        foreach (Chest chest in Object.FindObjectsByType<Chest>(FindObjectsSortMode.None))
        {
            if (!chest.IsOpened)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>PlayerStats.Health is a private serialized field with no public reader.</summary>
    private static int ReadHealth(PlayerStats stats)
    {
        FieldInfo field = typeof(PlayerStats).GetField("Health",
            BindingFlags.Instance | BindingFlags.NonPublic);
        return field != null ? (int)field.GetValue(stats) : -1;
    }
}
