using System;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Run progression (T02). <see cref="RunSequence"/> is pure, so the whole realm/floor walk is tested
/// without a scene; <see cref="RunState"/> wraps it with the seed and the floor's stairs.
/// </summary>
public class RunStateTests
{
    private static int notifications;

    [SetUp]
    public void SetUp()
    {
        notifications = 0;
        RunState.BeginRun(1234);
    }

    [TearDown]
    public void TearDown()
    {
        RunState.FloorChanged -= CountNotification;
    }

    private static void CountNotification()
    {
        notifications++;
    }

    // ---------------------------------------------------------------- RunSequence

    [Test]
    public void NewSequence_StartsAtTheFirstRealmAndFloor()
    {
        var sequence = new RunSequence(new[] { 2, 3 });

        Assert.AreEqual(0, sequence.RealmIndex);
        Assert.AreEqual(0, sequence.FloorIndex);
        Assert.IsFalse(sequence.IsComplete);
        Assert.AreEqual(2, sequence.RealmCount);
    }

    [Test]
    public void AdvanceFloor_StaysInTheRealmUntilItsLastFloor()
    {
        var sequence = new RunSequence(new[] { 3, 2 });

        sequence.AdvanceFloor();

        Assert.AreEqual(0, sequence.RealmIndex, "the realm should not change mid-realm");
        Assert.AreEqual(1, sequence.FloorIndex);
    }

    [Test]
    public void AdvanceFloor_WrapsIntoTheNextRealm()
    {
        var sequence = new RunSequence(new[] { 1, 2 });

        sequence.AdvanceFloor();

        Assert.AreEqual(1, sequence.RealmIndex);
        Assert.AreEqual(0, sequence.FloorIndex, "a new realm starts on its first floor");
    }

    [Test]
    public void IsBossFloor_IsTrueOnlyOnARealmsLastFloor()
    {
        var sequence = new RunSequence(new[] { 3, 2 });

        Assert.IsFalse(sequence.IsBossFloor, "floor 0 of 3 is not the boss floor");
        sequence.AdvanceFloor();
        Assert.IsFalse(sequence.IsBossFloor, "floor 1 of 3 is not the boss floor");
        sequence.AdvanceFloor();
        Assert.IsTrue(sequence.IsBossFloor, "floor 2 of 3 is the boss floor");

        sequence.AdvanceFloor();
        Assert.AreEqual(1, sequence.RealmIndex);
        Assert.IsFalse(sequence.IsBossFloor, "the next realm starts on a normal floor");
        Assert.IsTrue(sequence.IsLastRealm);
    }

    [Test]
    public void AdvancingPastTheFinalBossFloor_CompletesTheRun()
    {
        var sequence = new RunSequence(new[] { 2 });

        sequence.AdvanceFloor();                       // floor 1: the boss floor
        Assert.IsTrue(sequence.IsBossFloor);
        Assert.IsFalse(sequence.IsComplete);

        sequence.AdvanceFloor();

        Assert.IsTrue(sequence.IsComplete);
        Assert.AreEqual(0, sequence.RealmIndex, "the indices stay put once the run is over");
        Assert.AreEqual(1, sequence.FloorIndex);
    }

    [Test]
    public void AdvanceFloor_AfterCompletion_ChangesNothing()
    {
        var sequence = new RunSequence(new[] { 1 });
        sequence.AdvanceFloor();                       // completes

        sequence.AdvanceFloor();
        sequence.AdvanceFloor();

        Assert.IsTrue(sequence.IsComplete);
        Assert.AreEqual(0, sequence.RealmIndex);
        Assert.AreEqual(0, sequence.FloorIndex);
    }

    [Test]
    public void Reset_ReturnsToTheStartOfTheRun()
    {
        var sequence = new RunSequence(new[] { 2, 2 });
        sequence.AdvanceFloor();
        sequence.AdvanceFloor();

        sequence.Reset();

        Assert.AreEqual(0, sequence.RealmIndex);
        Assert.AreEqual(0, sequence.FloorIndex);
        Assert.IsFalse(sequence.IsComplete);
    }

    [Test]
    public void Sequence_WithNoRealms_Throws()
    {
        Assert.Throws<ArgumentException>(() => new RunSequence(new int[0]));
    }

    [Test]
    public void WalkingAThreeRealmRun_VisitsEveryFloorExactlyOnce()
    {
        var sequence = new RunSequence(new[] { 2, 3, 2 });
        var visited = new System.Collections.Generic.HashSet<string>();

        while (!sequence.IsComplete)
        {
            Assert.IsTrue(visited.Add($"{sequence.RealmIndex}:{sequence.FloorIndex}"),
                $"floor {sequence.RealmIndex}:{sequence.FloorIndex} was visited twice");
            sequence.AdvanceFloor();
        }

        Assert.AreEqual(7, visited.Count, "2 + 3 + 2 floors");
    }

    [Test]
    public void FloorSeed_IsStable_AndConsecutiveFloorsDiffer()
    {
        Assert.AreEqual(RunSequence.FloorSeed(7, 0, 0), RunSequence.FloorSeed(7, 0, 0), "same inputs, same seed");
        Assert.AreNotEqual(RunSequence.FloorSeed(7, 0, 0), RunSequence.FloorSeed(7, 0, 1),
            "consecutive floors must not share a layout seed");
        Assert.AreNotEqual(RunSequence.FloorSeed(7, 0, 0), RunSequence.FloorSeed(7, 1, 0),
            "a floor in another realm must not share a layout seed");
        Assert.AreNotEqual(RunSequence.FloorSeed(7, 0, 0), RunSequence.FloorSeed(8, 0, 0),
            "another run must not share a layout seed");
    }

    // ---------------------------------------------------------------- RunState

    [Test]
    public void BeginRun_StartsAtTheFirstRealmAndFloor()
    {
        Assert.IsTrue(RunState.HasRun);
        Assert.AreEqual(0, RunState.RealmIndex);
        Assert.AreEqual(0, RunState.FloorIndex);
        Assert.IsFalse(RunState.IsComplete);
        Assert.AreEqual(1, RunState.DisplayFloor, "the HUD shows floors 1-based");
        Assert.IsFalse(string.IsNullOrEmpty(RunState.RealmName));
    }

    [Test]
    public void BeginRun_UsesTheRunSeedForTheFirstFloor()
    {
        RunState.BeginRun(4242);

        Assert.AreEqual(RunSequence.FloorSeed(4242, 0, 0), RunState.FloorSeed);
    }

    [Test]
    public void AdvanceFloor_MovesTheRun_AndClearsTheFloorsStairs()
    {
        RunState.SetStairs(new Vector2Int(5, 5));
        Assert.IsTrue(RunState.HasStairs);

        RunState.AdvanceFloor();

        // Requires a realm with at least two floors (GDD 3.2); the built-in and authored plains both
        // have two, so landing on floor 1 rather than completing is the contract.
        Assert.AreEqual(1, RunState.FloorIndex, "descending advances the floor");
        Assert.IsFalse(RunState.HasStairs, "the new floor's stairs are not known until it is generated");
    }

    [Test]
    public void AdvanceFloor_NotifiesListeners_ButNotWhenTheRunCompletes()
    {
        RunState.FloorChanged += CountNotification;

        RunState.AdvanceFloor();                       // floor 1 of the 2-floor plains
        Assert.AreEqual(1, notifications, "a floor change rebuilds the level exactly once");

        RunState.AdvanceFloor();                       // past the boss floor: the run is over
        Assert.IsTrue(RunState.IsComplete);
        Assert.AreEqual(1, notifications, "there is no floor to build once the run is complete");
    }
}
