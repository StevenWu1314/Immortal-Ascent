using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Headless assertions for the generation-side invariants, in the project's `[DIAG]` style.
/// Run from Tools &gt; Diagnose, or drive them from outside the editor through the
/// UnityAgentClient bridge:
/// <c>POST /execute_method {"type":"MapgenDiagnostics","method":"ChestWalkability"}</c>.
///
/// These build a synthetic grid rather than driving a real generation run on purpose: generation
/// destroys and re-instantiates the player, enemies and chests, so invoking it from a menu item
/// would throw away whatever the user has open in the scene.
/// </summary>
public static class MapgenDiagnostics
{
    private static int failures;

    private static void Check(string label, bool ok, string detail)
    {
        if (ok)
        {
            Debug.Log($"[DIAG] PASS  {label}  ({detail})");
            return;
        }

        failures++;
        Debug.LogError($"[DIAG] FAIL  {label}  ({detail})");
    }

    /// <summary>
    /// The contract <see cref="Chest.Open"/> relies on. A chest cell starts as solid terrain and is
    /// therefore stored as <see cref="IslandManager.Unassigned"/>, because the island map is a
    /// snapshot of walkability taken at generation time. Opening the chest must update BOTH the grid
    /// and the island map:
    ///   * grid only -> the exit gate still reads the cell as "another room" and refuses the step
    ///     while the room holds enemies;
    ///   * island only -> the cell stays solid.
    /// The negative control below fails if <see cref="IslandManager.MarkWalkable"/> is ever removed,
    /// which is the whole point of the check.
    /// </summary>
    [MenuItem("Tools/Diagnose/Opened Chest Walkability")]
    public static void ChestWalkability()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[DIAG] stop Play mode first: this replaces Grid.Current and the island map.");
            return;
        }

        failures = 0;

        Grid previousGrid = Grid.Current;
        var grid = new Grid(12, 12, 1, Vector3.zero);
        var rooms = new List<BoundsInt> { new BoundsInt(1, 1, 0, 8, 8, 1) };
        var chest = new Vector2Int(4, 4);
        var corridor = new Vector2Int(10, 10);

        try
        {
            grid.SetValueAtLocation(chest.x, chest.y, LevelValidation.ChestCode);
            IslandManager.Rebuild(rooms);

            Check("chest cell is solid before opening",
                  !Grid.IsWalkableTerrain(grid.GetValueAtLocation(chest.x, chest.y)), "grid=99");
            Check("solid chest cell is Unassigned",
                  IslandManager.GetIsland(chest) == IslandManager.Unassigned,
                  $"island={IslandManager.GetIsland(chest)}");

            // Exactly what Chest.Open does.
            grid.SetValueAtLocation(chest.x, chest.y, 0);
            IslandManager.MarkWalkable(chest);

            Check("chest cell is walkable after opening",
                  Grid.IsWalkableTerrain(grid.GetValueAtLocation(chest.x, chest.y)), "grid=0");
            Check("opened chest cell belongs to its room",
                  IslandManager.GetIsland(chest) == 1, $"island={IslandManager.GetIsland(chest)}");
            Check("walkable cells are never left Unassigned",
                  IslandManager.GetIsland(chest) != IslandManager.Unassigned, "invariant holds");

            // Negative control: clearing the grid alone must NOT be enough.
            grid.SetValueAtLocation(chest.x, chest.y, LevelValidation.ChestCode);
            IslandManager.Rebuild(rooms);
            grid.SetValueAtLocation(chest.x, chest.y, 0);
            Check("grid-only change leaves the island id stale",
                  IslandManager.GetIsland(chest) == IslandManager.Unassigned,
                  $"island={IslandManager.GetIsland(chest)} - this is why MarkWalkable is required");

            // A cell outside every room belongs to the shared passage, not to a room.
            grid.SetValueAtLocation(corridor.x, corridor.y, LevelValidation.ChestCode);
            IslandManager.Rebuild(rooms);
            grid.SetValueAtLocation(corridor.x, corridor.y, 0);
            IslandManager.MarkWalkable(corridor);
            Check("cell outside every room becomes Passage",
                  IslandManager.GetIsland(corridor) == IslandManager.Passage,
                  $"island={IslandManager.GetIsland(corridor)}");
        }
        finally
        {
            // The Grid constructor republishes Grid.Current, so put the level's own grid back.
            Grid.Current = previousGrid;
        }

        Debug.Log(failures == 0
            ? "[DIAG] chest walkability: all checks passed"
            : $"[DIAG] chest walkability: {failures} check(s) FAILED");
        Debug.Log("[DIAG] note: IslandManager now holds this synthetic map. It is rebuilt by the next " +
                  "generation run, so Play (or the generator's Create Branching Dungeon button) before " +
                  "trusting island behaviour in the editor.");
    }

    /// <summary>
    /// End-to-end version of the check above, against a chest the generator actually produced, going
    /// through the real <see cref="Chest.Open"/>. Play mode only, because chests are instantiated at
    /// runtime. Everything it changes is discarded when Play mode stops, so it is safe on a real level.
    /// </summary>
    [MenuItem("Tools/Diagnose/Open A Real Chest (Play mode)")]
    public static void OpenRealChest()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("[DIAG] needs Play mode: open the dungeon scene and press Play first.");
            return;
        }

        failures = 0;

        Grid grid = Grid.Current;
        if (grid == null)
        {
            Debug.LogError("[DIAG] Grid.Current is null; generation has not run.");
            return;
        }

        Chest[] chests = Object.FindObjectsByType<Chest>(FindObjectsSortMode.None);
        Check("a chest was generated", chests.Length > 0, $"found {chests.Length}");
        if (chests.Length == 0)
        {
            Debug.Log("[DIAG] real chest: nothing to open");
            return;
        }

        Chest chest = chests[0];
        Vector2Int cell = chest.Cell;

        int gridBefore = grid.GetValueAtLocation(cell.x, cell.y);
        int islandBefore = IslandManager.GetIsland(cell);
        Check("real chest cell is solid terrain",
              gridBefore == LevelValidation.ChestCode, $"grid={gridBefore}");
        Check("real chest cell starts Unassigned in the island map",
              islandBefore == IslandManager.Unassigned, $"island={islandBefore}");

        bool opened = chest.Open(Inventory.Instance);

        int gridAfter = grid.GetValueAtLocation(cell.x, cell.y);
        int islandAfter = IslandManager.GetIsland(cell);
        Check("Chest.Open reported success", opened, $"opened={opened}");
        Check("real chest cell is walkable after opening",
              Grid.IsWalkableTerrain(gridAfter), $"grid={gridAfter}");
        Check("real chest cell is no longer Unassigned",
              islandAfter != IslandManager.Unassigned, $"island={islandAfter}");
        Check("real chest cell joined its room",
              islandAfter > IslandManager.Passage, $"island={islandAfter}");
        Check("enemies still treat that cell as part of the room",
              IslandManager.IsPartOfRoom(cell, islandAfter),
              $"IsPartOfRoom={IslandManager.IsPartOfRoom(cell, islandAfter)}");

        Debug.Log(failures == 0
            ? "[DIAG] real chest: all checks passed"
            : $"[DIAG] real chest: {failures} check(s) FAILED");
    }
}
