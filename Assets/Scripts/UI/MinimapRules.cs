using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// What the minimap knows (T08, GDD 14.5). Pure: it tracks the visited set and the marker visibility rules,
/// and <see cref="Minimap"/> only draws what this reports.
/// </summary>
public class MinimapRules
{
    private readonly HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
    private readonly HashSet<int> enteredIslands = new HashSet<int>();
    private bool stairsFound;
    private Vector2Int stairsCell;

    public int VisitedCount => visited.Count;
    public int EnteredIslandCount => enteredIslands.Count;
    public bool StairsFound => stairsFound;
    public Vector2Int StairsCell => stairsCell;

    /// <summary>GDD 14.5: enemies are never shown, in any realm.</summary>
    public static bool ShowsEnemies => false;

    /// <summary>Mark a cell seen. Returns true only when it is new, so the renderer repaints just those.</summary>
    public bool Visit(Vector2Int cell)
    {
        return visited.Add(cell);
    }

    public bool IsVisited(Vector2Int cell)
    {
        return visited.Contains(cell);
    }

    /// <summary>
    /// GDD 3.3: the stairs marker appears when the player discovers the stairs, not when the floor starts.
    /// Returns true when this call is the discovery.
    /// </summary>
    public bool DiscoverStairs(Vector2Int cell)
    {
        if (stairsFound)
        {
            return false;
        }
        stairsFound = true;
        stairsCell = cell;
        return true;
    }

    /// <summary>Note that the player has set foot on an island. Returns true when it is new to them.</summary>
    public bool EnterIsland(int islandId)
    {
        return islandId > IslandManager.Passage && enteredIslands.Add(islandId);
    }

    /// <summary>Chest and vein markers appear once their island has been entered.</summary>
    public bool ShowsMarkersFor(int islandId)
    {
        return islandId > IslandManager.Passage && enteredIslands.Contains(islandId);
    }

    /// <summary>A new floor forgets everything: the visited set does not survive a descent (GDD 14.5).</summary>
    public void Reset()
    {
        visited.Clear();
        enteredIslands.Clear();
        stairsFound = false;
        stairsCell = default;
    }
}
