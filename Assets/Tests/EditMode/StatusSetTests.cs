using NUnit.Framework;

/// <summary>
/// Status rules (T05, GDD 9.1/9.3/9.4). Pure: no scene, no MonoBehaviour.
/// </summary>
public class StatusSetTests
{
    [Test]
    public void ApplyRoot_MakesItActive_AndBlocksMovement()
    {
        var statuses = new StatusSet();

        Assert.IsTrue(statuses.TryApply(StatusKind.Root, 0, 2, out string rejection), rejection);
        Assert.IsTrue(statuses.Has(StatusKind.Root));
        Assert.IsTrue(statuses.BlocksMovement);
        Assert.AreEqual(2, statuses.RoundsRemaining(StatusKind.Root));
        Assert.AreEqual(1, statuses.Count);
    }

    [Test]
    public void ReapplyingRoot_RefreshesInsteadOfStacking()
    {
        var statuses = new StatusSet();
        statuses.TryApply(StatusKind.Root, 0, 2, out _);
        statuses.Tick();

        statuses.TryApply(StatusKind.Root, 0, 2, out _);

        Assert.AreEqual(1, statuses.Count, "there is never a second entry for the same kind");
        Assert.AreEqual(2, statuses.RoundsRemaining(StatusKind.Root), "the duration goes back to full");
    }

    [Test]
    public void ReapplyingRoot_KeepsTheStrongerMagnitude()
    {
        var statuses = new StatusSet();
        statuses.TryApply(StatusKind.Root, 3, 1, out _);

        statuses.TryApply(StatusKind.Root, 1, 4, out _);
        Assert.AreEqual(3, statuses.MagnitudeOf(StatusKind.Root), "the weaker application must not weaken it");

        statuses.TryApply(StatusKind.Root, 5, 1, out _);
        Assert.AreEqual(5, statuses.MagnitudeOf(StatusKind.Root), "a stronger application wins");
    }

    [Test]
    public void Tick_ExpiresRootWhenItsRoundsRunOut()
    {
        var statuses = new StatusSet();
        statuses.TryApply(StatusKind.Root, 0, 2, out _);

        statuses.Tick();
        Assert.IsTrue(statuses.BlocksMovement, "one round of two is not the end of it");
        Assert.AreEqual(1, statuses.RoundsRemaining(StatusKind.Root));

        statuses.Tick();
        Assert.IsFalse(statuses.Has(StatusKind.Root));
        Assert.IsFalse(statuses.BlocksMovement);
        Assert.AreEqual(0, statuses.Count);
    }

    [Test]
    public void Tick_DoesNotExpireAStatusEarly()
    {
        var statuses = new StatusSet();
        statuses.TryApply(StatusKind.Root, 0, 3, out _);

        statuses.Tick();
        statuses.Tick();

        Assert.IsTrue(statuses.Has(StatusKind.Root), "three rounds need three ticks");
        Assert.AreEqual(1, statuses.RoundsRemaining(StatusKind.Root));
    }

    [Test]
    public void OnlyRootCanBeAppliedInM1()
    {
        var statuses = new StatusSet();
        StatusKind[] m2Kinds =
        {
            StatusKind.Fire, StatusKind.Poison, StatusKind.Enfeeblement, StatusKind.Explosive
        };

        foreach (StatusKind kind in m2Kinds)
        {
            Assert.IsFalse(StatusSet.IsImplemented(kind), $"{kind} is M2 content");
            Assert.IsFalse(statuses.TryApply(kind, 5, 3, out string rejection), $"{kind} must be refused");
            Assert.IsFalse(string.IsNullOrEmpty(rejection), "a refusal has to say why");
        }

        Assert.AreEqual(0, statuses.Count, "a refused status leaves the set untouched");
        Assert.IsTrue(StatusSet.IsImplemented(StatusKind.Root));
    }

    [Test]
    public void AStatusNeedsAtLeastOneRound()
    {
        var statuses = new StatusSet();

        Assert.IsFalse(statuses.TryApply(StatusKind.Root, 0, 0, out _));
        Assert.IsFalse(statuses.TryApply(StatusKind.Root, 0, -3, out _));
        Assert.AreEqual(0, statuses.Count);
    }

    [Test]
    public void Clear_DropsEverything()
    {
        var statuses = new StatusSet();
        statuses.TryApply(StatusKind.Root, 0, 5, out _);

        statuses.Clear();

        Assert.AreEqual(0, statuses.Count);
        Assert.IsFalse(statuses.BlocksMovement);
    }

    [Test]
    public void TheKindOrder_StaysStableForSerializedData()
    {
        // The enum carries all five kinds from the start so serialized values never need renumbering.
        Assert.AreEqual(0, (int)StatusKind.Root);
        Assert.AreEqual(1, (int)StatusKind.Fire);
        Assert.AreEqual(2, (int)StatusKind.Poison);
        Assert.AreEqual(3, (int)StatusKind.Enfeeblement);
        Assert.AreEqual(4, (int)StatusKind.Explosive);
    }
}
