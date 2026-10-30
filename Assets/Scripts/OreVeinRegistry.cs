using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The mineable ore veins on the floor, keyed by cell (GDD 5.5).
///
/// A vein is solid terrain (code 7, blocked) holding a finite yield, exactly like an unopened chest - so
/// mining always happens from an adjacent cell, and depleting one performs the same two writes a chest
/// does: the grid for movement authority and the island snapshot for the gate.
/// </summary>
public static class OreVeinRegistry
{
    /// <summary>Terrain code for an intact vein: blocked until it is mined out.</summary>
    public const int OreCode = 7;

    /// <summary>
    /// Ore taken per mine attempt, inclusive (GDD 5.5). This one is a fixed rule, not a tuning knob - the
    /// per-vein total is the number that differs per floor, and it lives on the generator.
    /// </summary>
    public const int OrePerMineMin = 1;
    public const int OrePerMineMax = 3;

    private struct Vein
    {
        public ItemDefinition ore;
        public int remaining;
    }

    private static readonly Dictionary<Vector2Int, Vein> veins = new Dictionary<Vector2Int, Vein>();

    public static int Count => veins.Count;

    /// <summary>The cells of the live veins, for the minimap (T08).</summary>
    public static IEnumerable<Vector2Int> Cells => veins.Keys;

    public static void Register(Vector2Int cell, ItemDefinition ore, int remaining)
    {
        veins[cell] = new Vein { ore = ore, remaining = Mathf.Max(1, remaining) };
    }

    public static bool HasVein(Vector2Int cell)
    {
        return veins.ContainsKey(cell);
    }

    public static int Remaining(Vector2Int cell)
    {
        return veins.TryGetValue(cell, out Vein vein) ? vein.remaining : 0;
    }

    /// <summary>What a vein yields, so a test can assert the realm's metal without mining.</summary>
    public static ItemDefinition OreAt(Vector2Int cell)
    {
        return veins.TryGetValue(cell, out Vein vein) ? vein.ore : null;
    }

    /// <summary>
    /// Take one yield (GDD 5.5: 1-3 ore). The vein depletes; when it runs out the cell opens with the chest
    /// path's two writes, so the room gate stops treating it as an obstacle.
    /// </summary>
    public static bool TryMine(Vector2Int cell, Grid grid, System.Random rng, out ItemDefinition ore, out int yielded)
    {
        ore = null;
        yielded = 0;
        if (rng == null || !veins.TryGetValue(cell, out Vein vein) || vein.remaining <= 0)
        {
            return false;
        }

        int take = Mathf.Min(vein.remaining, rng.Next(OrePerMineMin, OrePerMineMax + 1));
        vein.remaining -= take;
        ore = vein.ore;
        yielded = take;

        if (vein.remaining <= 0)
        {
            veins.Remove(cell);
            if (grid != null)
            {
                grid.SetValueAtLocation(cell.x, cell.y, 0);
            }
            IslandManager.MarkWalkable(cell);
        }
        else
        {
            veins[cell] = vein;
        }

        return true;
    }

    /// <summary>A vein is solid, so it can only ever be worked from a neighbouring cell.</summary>
    public static bool TryGetAdjacent(Vector2Int from, out Vector2Int cell)
    {
        Vector2Int[] neighbours =
        {
            from + Vector2Int.up,
            from + Vector2Int.down,
            from + Vector2Int.left,
            from + Vector2Int.right
        };

        foreach (Vector2Int candidate in neighbours)
        {
            if (veins.ContainsKey(candidate))
            {
                cell = candidate;
                return true;
            }
        }

        cell = default;
        return false;
    }

    /// <summary>Forget every vein. Called before a floor is generated: veins have no objects to sweep.</summary>
    public static void Reset()
    {
        veins.Clear();
    }
}
