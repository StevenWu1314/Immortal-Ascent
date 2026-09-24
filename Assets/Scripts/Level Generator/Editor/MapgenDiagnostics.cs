using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Temporary diagnostic for the room-first / island generator. Generates the level and
/// reports:
///   * the terrain-value census of the whole grid,
///   * the terrain values around the player plus what Grid.Move decides for each direction,
///   * a flood fill over walkable cells to show which room the player can actually reach.
/// Open the scene that holds the generator you want to test, then run the matching menu item.
/// </summary>
public static class MapgenDiagnostics
{
    [MenuItem("Tools/Diagnose Perlin Map Walkability")]
    public static void DiagnosePerlin()
    {
        var map = FindOrOpen<PerlinNoiseMap>("Assets/Scenes/mapgen testing.unity");
        if (map == null)
        {
            return;
        }
        WakeSingletons();
        map.GenerateMap();
        Report("PerlinNoiseMap");
    }

    [MenuItem("Tools/Diagnose Room-First Dungeon Walkability")]
    public static void DiagnoseRoomFirst()
    {
        var gen = FindOrOpen<RoomFirstDungeonGenerator>("Assets/Scenes/Branching dungeon.unity");
        if (gen == null)
        {
            return;
        }
        WakeSingletons();
        gen.generateBranchingDungeon();
        Report("RoomFirstDungeonGenerator");
    }

    private static T FindOrOpen<T>(string scenePath) where T : Component
    {
        var found = Object.FindFirstObjectByType<T>();
        if (found == null && System.IO.File.Exists(scenePath))
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
            found = Object.FindFirstObjectByType<T>();
        }
        if (found == null)
        {
            Debug.LogError($"[DIAG] No {typeof(T).Name} found, and could not open '{scenePath}'.");
        }
        return found;
    }

    private static void WakeSingletons()
    {
        if (Application.isPlaying)
        {
            return;
        }
        WakeSingleton<EntityManager>();
        WakeSingleton<CollectableMap>();
        WakeSingleton<TurnManager>();
    }

    private static void Report(string label)
    {
        Grid grid = Grid.Current;
        if (grid == null)
        {
            Debug.LogError("[DIAG] Grid.Current is null after generation.");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"[DIAG] === {label} === grid {grid.Width}x{grid.Height}");

        // 1. Terrain census.
        var counts = new Dictionary<int, int>();
        for (int x = 0; x < grid.Width; x++)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                int v = grid.GetValueAtLocation(x, y);
                counts.TryGetValue(v, out int c);
                counts[v] = c + 1;
            }
        }
        sb.Append("[DIAG] terrain census:");
        foreach (var kv in counts)
        {
            sb.Append($" value{kv.Key}={kv.Value}");
        }
        sb.AppendLine();

        // 2. Player cell + neighbours.
        var player = Object.FindFirstObjectByType<PlayerStats>();
        if (player == null)
        {
            sb.AppendLine("[DIAG] no PlayerStats found (player was never spawned).");
            Debug.Log(sb.ToString());
            return;
        }

        Vector3 p = player.transform.position;
        int px = Mathf.FloorToInt(p.x);
        int py = Mathf.FloorToInt(p.y);
        sb.AppendLine($"[DIAG] player world={p} cell=({px},{py}) " +
                      $"cellTerrain={grid.GetValueAtLocation(px, py)}");

        foreach (var (name, d) in new (string, Vector2Int)[]
                 {
                     ("up", Vector2Int.up), ("down", Vector2Int.down),
                     ("left", Vector2Int.left), ("right", Vector2Int.right)
                 })
        {
            int nx = px + d.x;
            int ny = py + d.y;
            int terr = grid.GetValueAtLocation(nx, ny);
            sb.AppendLine($"[DIAG]   {name,-5} target=({nx},{ny}) terrain={terr} " +
                          $"walkable={Grid.IsWalkableTerrain(terr)} grid.Move={grid.Move(p, d, player.transform)}");
        }

        // 3. Island id of the player's cell and of each neighbour. Island ids are assigned
        // explicitly from the room layout: >=1 is a room, 0 is a shared corridor passage.
        sb.AppendLine($"[DIAG] player island id = {IslandManager.GetIsland(new Vector2Int(px, py))} " +
                      $"(0 = passage, >=1 = room, -1 = blocked)");
        foreach (var (name, d) in new (string, Vector2Int)[]
                 {
                     ("up", Vector2Int.up), ("down", Vector2Int.down),
                     ("left", Vector2Int.left), ("right", Vector2Int.right)
                 })
        {
            int nx2 = px + d.x;
            int ny2 = py + d.y;
            sb.AppendLine($"[DIAG]   neighbour {name,-5} ({nx2},{ny2}) island=" +
                          $"{IslandManager.GetIsland(new Vector2Int(nx2, ny2))} " +
                          $"gateAllows={IslandManager.CanMoveTo(p, new Vector2Int(nx2, ny2))}");
        }

        // 4. Player stats sanity.
        sb.AppendLine($"[DIAG] player maxHealth={player.getMaxHealth()}");

        // 5. Island / gating report.
        if (IslandManager.IsInitialized)
        {
            int playerIsland = IslandManager.GetIsland(p);
            sb.AppendLine($"[DIAG] islands on map = {IslandManager.IslandCount}");
            sb.AppendLine($"[DIAG] player island = {playerIsland}, " +
                          $"enemies on it = {IslandManager.CountLivingEnemies(playerIsland)}, " +
                          $"cleared = {IslandManager.IsIslandCleared(playerIsland)}");

            // How many islands hold enemies, and can each be left?
            var enemyIslands = new Dictionary<int, int>();
            if (EntityManager.Instance != null)
            {
                foreach (var pair in EntityManager.Instance.AllEntities)
                {
                    var go = pair.Value;
                    if (go == null || go.GetComponent<Enemy>() == null)
                    {
                        continue;
                    }
                    int isl = IslandManager.GetIsland(pair.Key);
                    enemyIslands.TryGetValue(isl, out int c);
                    enemyIslands[isl] = c + 1;
                }
            }
            sb.Append("[DIAG] enemy count per island:");
            foreach (var kv in enemyIslands)
            {
                sb.Append($" island{kv.Key}={kv.Value}");
            }
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("[DIAG] IslandManager NOT initialized (island gating is inactive).");
        }

        Debug.Log(sb.ToString());
    }

    private static void WakeSingleton<T>() where T : MonoBehaviour
    {
        var inst = Object.FindFirstObjectByType<T>();
        if (inst == null)
        {
            return;
        }
        var awake = typeof(T).GetMethod("Awake",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Public);
        awake?.Invoke(inst, null);
    }
}
