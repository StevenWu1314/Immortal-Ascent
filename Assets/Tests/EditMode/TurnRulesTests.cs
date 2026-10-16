using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The action economy as a table (T03, GDD 6.1-6.2). Pure, so none of this needs a scene.
/// </summary>
public class TurnRulesTests
{
    [Test]
    public void IsTurnBased_RequiresALivingEnemyInARoom()
    {
        Assert.IsFalse(TurnRules.IsTurnBased(IslandManager.Unassigned, 3), "blocked cells are never turn-based");
        Assert.IsFalse(TurnRules.IsTurnBased(IslandManager.Passage, 3), "corridors are never turn-based");
        Assert.IsFalse(TurnRules.IsTurnBased(1, 0), "a cleared room is not turn-based");
        Assert.IsTrue(TurnRules.IsTurnBased(1, 1));
        Assert.IsTrue(TurnRules.IsTurnBased(7, 4));
    }

    [Test]
    public void ConsumesTurn_MatchesTheActionTable()
    {
        var expected = new Dictionary<TurnAction, bool>
        {
            { TurnAction.Move, true },
            { TurnAction.MeleeAttack, true },
            { TurnAction.BowShot, true },
            { TurnAction.UseItem, true },
            { TurnAction.PlaceTorch, true },
            { TurnAction.MineOre, true },
            { TurnAction.OpenChest, false },
            { TurnAction.Equip, false },
            { TurnAction.ApplyRune, false },
            { TurnAction.Craft, false },
            { TurnAction.LevelUp, false },
            { TurnAction.AimEnter, false },
            { TurnAction.AimExit, false }
        };

        // Every action must have a decision here: a new enum member without one fails this rather than
        // silently defaulting to free.
        foreach (TurnAction action in Enum.GetValues(typeof(TurnAction)))
        {
            Assert.IsTrue(expected.ContainsKey(action), $"{action} has no decision in the table");
            Assert.AreEqual(expected[action], TurnRules.ConsumesTurn(action), $"{action}");
        }

        Assert.AreEqual(6, expected.Count(pair => pair.Value), "GDD 6.2 lists exactly six turn-costing actions");
    }

    [Test]
    public void IsMovementAllowed_NeedsTerrainIslandAndNoRoot()
    {
        Assert.IsTrue(TurnRules.IsMovementAllowed(true, true, false), "open floor in an ungated room");
        Assert.IsFalse(TurnRules.IsMovementAllowed(false, true, false), "water, wall or out of bounds");
        Assert.IsFalse(TurnRules.IsMovementAllowed(true, false, false), "the island lock is active");
        Assert.IsFalse(TurnRules.IsMovementAllowed(true, true, true), "a Root status holds the player");
    }
}
