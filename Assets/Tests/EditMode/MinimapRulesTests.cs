using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Minimap reveal rules (T08, GDD 14.5/3.3). Pure: no tilemap, no scene.
/// </summary>
public class MinimapRulesTests
{
    private MinimapRules rules;

    [SetUp]
    public void SetUp()
    {
        rules = new MinimapRules();
    }

    [Test]
    public void ACellBecomesVisited_AndStaysVisited()
    {
        Vector2Int cell = new Vector2Int(3, 4);

        Assert.IsFalse(rules.IsVisited(cell));
        Assert.IsTrue(rules.Visit(cell), "the first visit is news to the map");
        Assert.IsTrue(rules.IsVisited(cell));
        Assert.IsFalse(rules.Visit(cell), "a repeat visit is not news, so nothing repaints");
        Assert.AreEqual(1, rules.VisitedCount);
    }

    [Test]
    public void VisitingOneCell_DoesNotRevealUnrelatedCells()
    {
        rules.Visit(new Vector2Int(0, 0));

        Assert.IsFalse(rules.IsVisited(new Vector2Int(0, 1)));
        Assert.IsFalse(rules.IsVisited(new Vector2Int(1, 0)));
        Assert.IsFalse(rules.IsVisited(new Vector2Int(-1, -1)));
        Assert.AreEqual(1, rules.VisitedCount);
    }

    [Test]
    public void TheStairsMarker_AppearsOnlyOnDiscovery()
    {
        Assert.IsFalse(rules.StairsFound, "the floor starts without the stairs on the map");

        Assert.IsTrue(rules.DiscoverStairs(new Vector2Int(65, 65)));
        Assert.IsTrue(rules.StairsFound);
        Assert.AreEqual(new Vector2Int(65, 65), rules.StairsCell);
        Assert.IsFalse(rules.DiscoverStairs(new Vector2Int(1, 1)), "discovery happens once");
        Assert.AreEqual(new Vector2Int(65, 65), rules.StairsCell, "and does not move afterwards");
    }

    [Test]
    public void ChestAndVeinMarkers_AppearOnceTheirIslandIsEntered()
    {
        Assert.IsFalse(rules.ShowsMarkersFor(3), "an island the player has not set foot on shows nothing");

        Assert.IsTrue(rules.EnterIsland(3));
        Assert.IsTrue(rules.ShowsMarkersFor(3));

        Assert.IsFalse(rules.EnterIsland(3), "entering twice is not news");
        Assert.IsFalse(rules.ShowsMarkersFor(1), "a different island is still dark");
        Assert.IsFalse(rules.ShowsMarkersFor(IslandManager.Passage), "corridors are not islands");
        Assert.IsFalse(rules.ShowsMarkersFor(IslandManager.Unassigned), "nor is blocked terrain");
    }

    [Test]
    public void EnemiesAreNeverShown()
    {
        rules.Visit(new Vector2Int(1, 1));
        rules.EnterIsland(2);
        rules.DiscoverStairs(new Vector2Int(9, 9));

        Assert.IsFalse(MinimapRules.ShowsEnemies, "GDD 14.5: enemies never appear on the minimap");
    }

    [Test]
    public void Reset_ForgetsTheFloor()
    {
        rules.Visit(new Vector2Int(1, 1));
        rules.EnterIsland(4);
        rules.DiscoverStairs(new Vector2Int(2, 2));

        rules.Reset();

        Assert.AreEqual(0, rules.VisitedCount);
        Assert.AreEqual(0, rules.EnteredIslandCount);
        Assert.IsFalse(rules.StairsFound);
        Assert.IsFalse(rules.IsVisited(new Vector2Int(1, 1)));
        Assert.IsFalse(rules.ShowsMarkersFor(4));
    }
}
