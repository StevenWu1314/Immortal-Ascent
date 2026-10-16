using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

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
                      $"pos=({p.x:R}, {p.y:R}, {p.z:R}) derived={derived} " +
                      $"{(derived == pair.Key ? "cell-ok" : "CELL-MISMATCH")}");
        }
    }

    /// <summary>
    /// Is any enemy standing outside the room island it spawned on? Records each enemy's spawn
    /// island on first sight and compares, so an enemy that got alerted out of its room, or that
    /// spawned before IslandManager existed, shows up instead of being invisible.
    /// </summary>
    private static readonly Dictionary<int, int> spawnIslandByEnemy = new Dictionary<int, int>();

    /// <summary>
    /// How is the doorway a corridor carves through a room's wall classified? The corridor is drawn
    /// with bridgeTile, and the doorway cell sits at the room's own wall column -- inside its
    /// rectangle. If it carries the room's island id then the confinement rule, and the player's
    /// island gate, both treat a bridge tile as part of the room.
    /// </summary>
    [MenuItem("Tools/Play/Audit Bridge Tiles")]
    public static void AuditBridgeTiles()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning($"{Tag} needs Play mode.");
            return;
        }

        TilePlacer[] placers = Object.FindObjectsByType<TilePlacer>(FindObjectsSortMode.None);
        if (placers.Length == 0 || placers[0].corridor == null)
        {
            Debug.LogWarning($"{Tag} no TilePlacer with a corridor tilemap.");
            return;
        }
        Tilemap corridor = placers[0].corridor;
        Grid grid = Grid.Current;

        int total = 0, inRoom = 0, inPassage = 0, unassigned = 0, stillAllowed = 0;
        string examples = "";
        foreach (Vector3Int tile in corridor.cellBounds.allPositionsWithin)
        {
            if (!corridor.HasTile(tile))
            {
                continue;
            }
            total++;
            Vector2Int cell = new Vector2Int(tile.x, tile.y);
            int island = IslandManager.GetIsland(cell);
            // IsPartOfRoom keys off GetRoomContaining, not off the island id, so that is the
            // predicate that matters here: a bridge tile inside a room rectangle is a doorway, and
            // before the strict-interior rule an enemy was allowed to stand on it.
            int containing = IslandManager.GetRoomContaining(cell);
            if (containing >= 1)
            {
                inRoom++;
                if (IslandManager.IsPartOfRoom(cell, containing))
                {
                    stillAllowed++;
                }
                if (examples.Length < 90)
                {
                    examples += $"{cell}=room{containing} ";
                }
            }
            else if (island == IslandManager.Passage) inPassage++;
            else unassigned++;
        }

        Debug.Log($"{Tag} bridge tiles: {total} total, {inRoom} classified as part of a ROOM, " +
                  $"{inPassage} passage, {unassigned} unassigned; " +
                  $"{stillAllowed} of the {inRoom} inside a room rectangle still accepted by IsPartOfRoom " +
                  $"(must be 0; it was the doorway count before the strict-interior rule)");
        if (examples.Length > 0)
        {
            Debug.Log($"{Tag} bridge cells that count as room cells: {examples}");
        }
    }

    [MenuItem("Tools/Play/Audit Enemy Rooms")]
    public static void AuditEnemyRooms()
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

        // The doorway a corridor carves through a room's wall sits INSIDE the room rectangle, so its
        // island id is the room's and an island test cannot see it. Ask the tilemap instead: a tile
        // in the corridor tilemap is a bridge, and nothing should be standing on one.
        Tilemap corridor = null;
        TilePlacer[] placers = Object.FindObjectsByType<TilePlacer>(FindObjectsSortMode.None);
        if (placers.Length > 0)
        {
            corridor = placers[0].corridor;
        }

        int enemies = 0, violations = 0;
        foreach (var pair in EntityManager.Instance.AllEntities)
        {
            GameObject entity = pair.Value;
            if (entity == null || entity.GetComponent<Enemy>() == null)
            {
                continue;
            }
            enemies++;

            int id = entity.GetInstanceID();
            int island = IslandManager.GetIsland(pair.Key);
            if (!spawnIslandByEnemy.ContainsKey(id))
            {
                spawnIslandByEnemy[id] = island;
            }
            int home = spawnIslandByEnemy[id];

            bool walkable = Grid.IsWalkableTerrain(grid.GetValueAtLocation(pair.Key.x, pair.Key.y));
            bool onBridge = corridor != null && corridor.HasTile(new Vector3Int(pair.Key.x, pair.Key.y, 0));

            // island <= 0 is Unassigned (-1) or Passage (0). A room enemy on either has escaped the
            // confinement rule, and one that SPAWNED there has HomeIsland < 0, which makes CanStepOnto
            // skip the rule entirely -- the old version of this audit recorded that as its baseline and
            // so could never report it.
            if (!walkable || island <= 0 || island != home || onBridge)
            {
                violations++;
                Debug.LogWarning($"{Tag} ENEMY OUT OF PLACE {entity.name} cell={pair.Key} island={island} " +
                                 $"spawnIsland={home} walkable={walkable} " +
                                 $"spawnedOutsideARoom={home <= 0} onBridgeTile={onBridge}");
            }
        }

        string islands = string.Join(",", spawnIslandByEnemy.Values);
        Debug.Log($"{Tag} enemy audit: {enemies} alive, {violations} out of place; " +
                  $"spawn islands [{islands}]; player island {IslandManager.GetIsland(Grid.CellFromWorld(PlayerStats.Instance.transform.position))}");
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

        // The driver has no sense of self-preservation and used to fight until the player died, which
        // ends the Play session by loading the UI scene. Stop short of that.
        int currentHealth = ReadHealth(PlayerStats.Instance);
        if (currentHealth >= 0 && currentHealth <= 30)
        {
            Debug.Log($"{Tag} hp={currentHealth}: stopping the drive rather than dying. Heal, then carry on.");
            return;
        }

        Vector2Int to = from + direction;

        // Drive the same path the keyboard drives: TryMove validates the island gate and the terrain,
        // resolves a bump-attack and decides the turn cost. The driver used to reimplement those checks
        // and call MoveEntity directly, which stopped resolving melee the moment combat left the registry.
        if (!controls.TryMove(direction))
        {
            Debug.Log($"{Tag} {from} -> {to} refused (island gate or terrain); nothing consumed.");
            return;
        }

        int health = ReadHealth(PlayerStats.Instance);
        Vector3 pose = controls.transform.position;
        Vector2Int poseCell = Grid.CellFromWorld(pose);
        Debug.Log($"{Tag} {(poseCell == to ? "stepped" : $"stayed at {poseCell} (attacked)")} {from} -> {to} " +
                  $"hp={health} str={PlayerStats.Instance.Strength} " +
                  $"island={IslandManager.GetIsland(poseCell)} enemies={CountEnemies()} " +
                  $"pos=({pose.x:R},{pose.y:R}) " +
                  $"derived={(poseCell == to ? "ok" : poseCell.ToString())}");
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
