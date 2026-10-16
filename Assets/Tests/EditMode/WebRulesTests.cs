using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Spider web cells (T05, GDD 5.4). Uses a plain Grid and the registry - no scene needed, and
/// <see cref="IslandManager.MarkWalkable"/> is safe before the island map exists.
/// </summary>
public class WebRulesTests
{
    [SetUp]
    public void SetUp()
    {
        WebRegistry.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        WebRegistry.Reset();
    }

    [Test]
    public void AWebCell_IsWalkableBeforeItIsEntered()
    {
        Assert.IsTrue(Grid.IsWalkableTerrain(WebRegistry.WebCode),
            "a web has to be steppable, or it could never trigger");
    }

    [Test]
    public void Consume_DoesBothWrites()
    {
        var grid = new Grid(12, 12, 1f, Vector3.zero);
        Vector2Int cell = new Vector2Int(4, 4);
        grid.SetValueAtLocation(cell.x, cell.y, WebRegistry.WebCode);
        WebRegistry.Register(cell);

        Assert.IsTrue(WebRegistry.HasWeb(cell));
        Assert.IsTrue(WebRegistry.Consume(cell, grid));

        Assert.IsFalse(WebRegistry.HasWeb(cell), "the web is gone");
        Assert.AreEqual(0, grid.GetValueAtLocation(cell.x, cell.y),
            "the grid write is what lets the player walk through afterwards");
        Assert.IsFalse(WebRegistry.Consume(cell, grid), "a consumed web cannot be consumed twice");
    }

    [Test]
    public void Consume_WithoutAGrid_StillForgetsTheWeb()
    {
        Vector2Int cell = new Vector2Int(1, 1);
        WebRegistry.Register(cell);

        Assert.IsTrue(WebRegistry.Consume(cell, null));
        Assert.AreEqual(0, WebRegistry.Count);
    }

    [Test]
    public void RegisteredWebs_AreCounted_AndResetClearsThem()
    {
        WebRegistry.Register(new Vector2Int(2, 2));
        WebRegistry.Register(new Vector2Int(3, 3));
        WebRegistry.Register(new Vector2Int(2, 2));

        Assert.AreEqual(2, WebRegistry.Count, "registering the same cell twice is one web");
        Assert.IsTrue(WebRegistry.HasWeb(new Vector2Int(3, 3)));
        Assert.IsFalse(WebRegistry.HasWeb(new Vector2Int(9, 9)));

        WebRegistry.Reset();
        Assert.AreEqual(0, WebRegistry.Count);
    }
}
