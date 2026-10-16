using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Awareness and roaming (T04, GDD 6.5-6.6, 13.4). Pure: no scene, no MonoBehaviour.
/// </summary>
public class AwarenessTests
{
    // ------------------------------------------------------------------ who notices

    [Test]
    public void RoomWide_NoticesOnlyOnTheSameIsland()
    {
        Assert.IsTrue(Awareness.Notices(AggroMode.RoomWide, 3, 3, distance: 99f, detectionRadius: 0f, hasClearShot: false),
            "the same island is the whole condition, however far apart and with no sight line");
        Assert.IsFalse(Awareness.Notices(AggroMode.RoomWide, 3, 4, distance: 1f, detectionRadius: 10f, hasClearShot: true),
            "another island is not noticed, even right next to the player");
        Assert.IsFalse(Awareness.Notices(AggroMode.RoomWide, IslandManager.Passage, IslandManager.Passage,
            distance: 1f, detectionRadius: 10f, hasClearShot: true),
            "corridors are not rooms");
    }

    [Test]
    public void Radius_NeedsRangeAndSight()
    {
        Assert.IsTrue(Awareness.Notices(AggroMode.DetectionRadius, 1, 2, distance: 4f, detectionRadius: 5f, hasClearShot: true),
            "inside the radius with a clear line");
        Assert.IsFalse(Awareness.Notices(AggroMode.DetectionRadius, 1, 2, distance: 4f, detectionRadius: 5f, hasClearShot: false),
            "inside the radius but behind a wall");
        Assert.IsFalse(Awareness.Notices(AggroMode.DetectionRadius, 1, 2, distance: 6f, detectionRadius: 5f, hasClearShot: true),
            "outside the radius");
        Assert.IsFalse(Awareness.Notices(AggroMode.DetectionRadius, 1, 2, distance: 1f, detectionRadius: 0f, hasClearShot: true),
            "a zero radius notices nothing");
    }

    [Test]
    public void WhenTheIslandMapIsMissing_RoomWideFallsBackToTheRadius()
    {
        Assert.IsTrue(Awareness.Notices(AggroMode.RoomWide, IslandManager.Unassigned, IslandManager.Unassigned,
            distance: 2f, detectionRadius: 5f, hasClearShot: true),
            "an editor-generated level with no islands must still be able to wake enemies");
        Assert.IsFalse(Awareness.Notices(AggroMode.RoomWide, IslandManager.Unassigned, IslandManager.Unassigned,
            distance: 9f, detectionRadius: 5f, hasClearShot: true),
            "and the fallback still respects the radius");
    }

    // ------------------------------------------------------------------ the cycle

    [Test]
    public void Idle_OnlyLeavesForAChase()
    {
        Assert.AreEqual(AwarenessState.Chase, Awareness.Next(AwarenessState.Idle, notices: true, shouldReturn: false, atPost: true));
        Assert.AreEqual(AwarenessState.Idle, Awareness.Next(AwarenessState.Idle, notices: false, shouldReturn: false, atPost: true));
    }

    [Test]
    public void Chase_BreaksOffWhenToldTo()
    {
        Assert.AreEqual(AwarenessState.Chase, Awareness.Next(AwarenessState.Chase, notices: true, shouldReturn: false, atPost: false));
        Assert.AreEqual(AwarenessState.Return, Awareness.Next(AwarenessState.Chase, notices: true, shouldReturn: true, atPost: false),
            "beyond chaseRange, or out of sight in radius mode");
    }

    [Test]
    public void Return_GoesBackToChaseOrHome()
    {
        Assert.AreEqual(AwarenessState.Chase, Awareness.Next(AwarenessState.Return, notices: true, shouldReturn: true, atPost: false),
            "re-noticing the player interrupts the walk home");
        Assert.AreEqual(AwarenessState.Idle, Awareness.Next(AwarenessState.Return, notices: false, shouldReturn: true, atPost: true),
            "arriving home ends the return");
        Assert.AreEqual(AwarenessState.Return, Awareness.Next(AwarenessState.Return, notices: false, shouldReturn: true, atPost: false));
    }

    // ------------------------------------------------------------------ roaming

    [Test]
    public void Roaming_StepsToAFreeNeighbour()
    {
        Vector2Int from = new Vector2Int(5, 5);
        Vector2Int step = Roaming.ChooseStep(from, rotation: 0,
            canStepOnto: cell => cell.x >= 5, isOccupied: cell => false);

        Assert.AreEqual(Vector2Int.up, step, "rotation 0 tries up first");
        Assert.AreEqual(from + step, new Vector2Int(5, 6));
    }

    [Test]
    public void Roaming_NeverStepsOntoABlockedOrOccupiedCell()
    {
        Vector2Int from = new Vector2Int(5, 5);
        Vector2Int step = Roaming.ChooseStep(from, rotation: 0,
            canStepOnto: cell => cell == new Vector2Int(5, 4),
            isOccupied: cell => false);

        Assert.AreEqual(Vector2Int.down, step);
    }

    [Test]
    public void Roaming_HoldsStillWhenBoxedIn()
    {
        Vector2Int step = Roaming.ChooseStep(new Vector2Int(5, 5), rotation: 0,
            canStepOnto: cell => true, isOccupied: cell => true);

        Assert.AreEqual(Vector2Int.zero, step, "every neighbour is taken");
    }

    [Test]
    public void Roaming_RotationChangesTheChoice_ButNeverTheRule()
    {
        Vector2Int from = new Vector2Int(0, 0);
        Vector2Int first = Roaming.ChooseStep(from, rotation: 0, canStepOnto: cell => true, isOccupied: cell => false);
        Vector2Int second = Roaming.ChooseStep(from, rotation: 1, canStepOnto: cell => true, isOccupied: cell => false);

        Assert.AreNotEqual(first, second, "rotation is what makes repeated roaming not a straight line");
        Assert.AreEqual(1, Mathf.Abs(first.x) + Mathf.Abs(first.y), "still exactly one cell");
        Assert.AreEqual(1, Mathf.Abs(second.x) + Mathf.Abs(second.y));
    }
}
