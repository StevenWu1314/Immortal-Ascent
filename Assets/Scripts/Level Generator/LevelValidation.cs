using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Post-generation invariant checks shared by the runtime generators and the editor
/// diagnostics. The key invariant is reachability: every cell the level claims is walkable
/// must actually be reachable from where the player spawned, and every chest must have a
/// reachable cell beside it.
///
/// Generation previously had no such check at all, so a corridor that failed to punch through
/// a room's wall ring produced an unreachable room that only a human walking the map could
/// find. Both the generator and MapgenDiagnostics now run this same code, so the reported
/// numbers are the ones the game asserts on.
/// </summary>
public static class LevelValidation
{
    /// <summary>
    /// Terrain code for a chest. Solid, so the player opens it from an adjacent cell rather
    /// than standing on it (see Grid.IsWalkableTerrain, which rejects this value).
    /// </summary>
    public const int ChestCode = 99;

    private static readonly Vector2Int[] Neighbours =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    /// <summary>Outcome of a single flood fill from the spawn cell.</summary>
    public readonly struct ReachabilityResult
    {
        public readonly Vector2Int Origin;
        public readonly int TotalWalkable;
        public readonly int Reachable;
        public readonly int TotalChests;
        public readonly int ReachableChests;
        public readonly bool OriginWalkable;

        public ReachabilityResult(Vector2Int origin, int totalWalkable, int reachable,
                                  int totalChests, int reachableChests, bool originWalkable)
        {
            Origin = origin;
            TotalWalkable = totalWalkable;
            Reachable = reachable;
            TotalChests = totalChests;
            ReachableChests = reachableChests;
            OriginWalkable = originWalkable;
        }

        /// <summary>Walkable cells the player cannot get to from the spawn cell.</summary>
        public int Isolated => TotalWalkable - Reachable;

        /// <summary>True when the spawn cell is walkable and nothing walkable is cut off.</summary>
        public bool Connected => OriginWalkable && Isolated == 0;

        /// <summary>True when every chest has a reachable cell next to it.</summary>
        public bool ChestsReachable => ReachableChests == TotalChests;

        /// <summary>Both invariants hold.</summary>
        public bool Ok => Connected && ChestsReachable;

        public string Describe()
        {
            return $"origin={Origin} originWalkable={OriginWalkable} " +
                   $"reachable={Reachable}/{TotalWalkable} isolated={Isolated} " +
                   $"chests={ReachableChests}/{TotalChests} connected={(Connected ? "YES" : "NO")}";
        }
    }

    /// <summary>
    /// Flood fill over walkable terrain starting at <paramref name="origin"/>. A chest counts
    /// as reachable when it touches a reachable walkable cell; the fill does not expand
    /// through it, because chest cells are solid.
    /// </summary>
    public static ReachabilityResult AnalyzeReachability(Grid grid, Vector2Int origin)
    {
        if (grid == null)
        {
            return new ReachabilityResult(origin, 0, 0, 0, 0, false);
        }

        int width = grid.Width;
        int height = grid.Height;

        int totalWalkable = 0;
        int totalChests = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int value = grid.GetValueAtLocation(x, y);
                if (Grid.IsWalkableTerrain(value))
                {
                    totalWalkable++;
                }
                if (value == ChestCode)
                {
                    totalChests++;
                }
            }
        }

        if (!Grid.IsWalkableTerrain(grid.GetValueAtLocation(origin.x, origin.y)))
        {
            // Spawning inside a wall is itself a failure worth reporting distinctly: the
            // player would be unable to move at all, so "isolated = everything" is accurate.
            return new ReachabilityResult(origin, totalWalkable, 0, totalChests, 0, false);
        }

        bool[,] visited = new bool[width, height];
        bool[,] chestTouched = new bool[width, height];
        Stack<Vector2Int> frontier = new Stack<Vector2Int>();

        visited[origin.x, origin.y] = true;
        frontier.Push(origin);

        int reachable = 0;
        int reachableChests = 0;

        while (frontier.Count > 0)
        {
            Vector2Int cell = frontier.Pop();
            reachable++;

            foreach (Vector2Int step in Neighbours)
            {
                Vector2Int next = cell + step;
                if (!grid.InBounds(next.x, next.y) || visited[next.x, next.y])
                {
                    continue;
                }

                int value = grid.GetValueAtLocation(next.x, next.y);
                if (value == ChestCode)
                {
                    if (!chestTouched[next.x, next.y])
                    {
                        chestTouched[next.x, next.y] = true;
                        reachableChests++;
                    }
                    continue;
                }

                if (!Grid.IsWalkableTerrain(value))
                {
                    continue;
                }

                visited[next.x, next.y] = true;
                frontier.Push(next);
            }
        }

        return new ReachabilityResult(origin, totalWalkable, reachable,
                                      totalChests, reachableChests, true);
    }
}
