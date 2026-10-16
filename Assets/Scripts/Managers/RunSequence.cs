using System;

/// <summary>
/// Where a run is: which realm, which floor, and whether it is over. Pure - no Unity types, no scene,
/// no time - so the whole progression can be tested without entering Play mode.
/// </summary>
public class RunSequence
{
    private readonly int[] floorsPerRealm;

    public RunSequence(int[] floorsPerRealm)
    {
        if (floorsPerRealm == null || floorsPerRealm.Length == 0)
        {
            throw new ArgumentException("a run needs at least one realm", nameof(floorsPerRealm));
        }

        this.floorsPerRealm = (int[])floorsPerRealm.Clone();
        for (int i = 0; i < this.floorsPerRealm.Length; i++)
        {
            this.floorsPerRealm[i] = Math.Max(1, this.floorsPerRealm[i]);
        }

        Reset();
    }

    public int RealmIndex { get; private set; }
    public int FloorIndex { get; private set; }
    public bool IsComplete { get; private set; }

    public int RealmCount => floorsPerRealm.Length;
    public int FloorsInCurrentRealm => floorsPerRealm[RealmIndex];
    public bool IsLastRealm => RealmIndex == floorsPerRealm.Length - 1;

    /// <summary>True on the floor that ends the realm, which is where the boss arena goes.</summary>
    public bool IsBossFloor => FloorIndex == FloorsInCurrentRealm - 1;

    public int FloorsInRealm(int realmIndex)
    {
        return floorsPerRealm[realmIndex];
    }

    public void Reset()
    {
        RealmIndex = 0;
        FloorIndex = 0;
        IsComplete = false;
    }

    /// <summary>
    /// Step down one floor. Past the final realm's boss floor the run completes and the indices stay
    /// put, so completion is the only thing that changes and a second call does nothing.
    /// </summary>
    public void AdvanceFloor()
    {
        if (IsComplete)
        {
            return;
        }

        if (FloorIndex + 1 < FloorsInCurrentRealm)
        {
            FloorIndex++;
            return;
        }

        if (RealmIndex + 1 < floorsPerRealm.Length)
        {
            RealmIndex++;
            FloorIndex = 0;
            return;
        }

        IsComplete = true;
    }

    /// <summary>
    /// Layout seed for one floor of one run. Stable across calls, and it must differ between
    /// consecutive floors or every floor of a run comes out identical.
    /// </summary>
    public static int FloorSeed(int runSeed, int realmIndex, int floorIndex)
    {
        unchecked
        {
            return (runSeed * 73856093) ^ ((realmIndex + 1) * 19349663) ^ ((floorIndex + 1) * 83492791);
        }
    }
}
