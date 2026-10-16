using System;
using System.Collections.Generic;

/// <summary>
/// The statuses on one entity: what is active, for how much longer, and what they block.
///
/// Pure - no MonoBehaviour, no UnityEngine - so every rule (refresh instead of stacking, tick once per
/// round, expire at zero, only Root in M1) is testable without a scene.
/// </summary>
public class StatusSet
{
    public struct Status
    {
        public StatusKind Kind;
        public int Magnitude;
        public int RoundsRemaining;
    }

    private readonly List<Status> statuses = new List<Status>();

    public int Count => statuses.Count;
    public IReadOnlyList<Status> All => statuses;

    /// <summary>GDD 9.1: Root is what stops the player moving.</summary>
    public bool BlocksMovement => Has(StatusKind.Root);

    /// <summary>
    /// Kinds this build may apply. M1 ships Root alone (T05); the four M2 statuses are refused rather than
    /// half-applied, so an unfinished one can never silently ship.
    /// </summary>
    public static bool IsImplemented(StatusKind kind)
    {
        return kind == StatusKind.Root;
    }

    public bool Has(StatusKind kind)
    {
        for (int i = 0; i < statuses.Count; i++)
        {
            if (statuses[i].Kind == kind)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Rounds left on a kind, or zero when it is not active.</summary>
    public int RoundsRemaining(StatusKind kind)
    {
        for (int i = 0; i < statuses.Count; i++)
        {
            if (statuses[i].Kind == kind)
            {
                return statuses[i].RoundsRemaining;
            }
        }
        return 0;
    }

    public int MagnitudeOf(StatusKind kind)
    {
        for (int i = 0; i < statuses.Count; i++)
        {
            if (statuses[i].Kind == kind)
            {
                return statuses[i].Magnitude;
            }
        }
        return 0;
    }

    /// <summary>
    /// Apply a status. Re-applying a kind that is already active refreshes it (GDD 9.3): the duration goes
    /// back to full and the stronger magnitude is kept, but there is never a second entry - so a web can
    /// never stack two Roots.
    ///
    /// Returns false and sets <paramref name="rejection"/> for anything this build cannot apply.
    /// </summary>
    public bool TryApply(StatusKind kind, int magnitude, int rounds, out string rejection)
    {
        rejection = null;

        if (!IsImplemented(kind))
        {
            rejection = $"{kind} is not implemented in M1; nothing was applied.";
            return false;
        }
        if (rounds <= 0)
        {
            rejection = $"{kind} needs at least one round; nothing was applied.";
            return false;
        }

        for (int i = 0; i < statuses.Count; i++)
        {
            if (statuses[i].Kind != kind)
            {
                continue;
            }

            Status refreshed = statuses[i];
            refreshed.RoundsRemaining = rounds;
            refreshed.Magnitude = Math.Max(refreshed.Magnitude, magnitude);
            statuses[i] = refreshed;
            return true;
        }

        statuses.Add(new Status
        {
            Kind = kind,
            Magnitude = magnitude,
            RoundsRemaining = rounds
        });
        return true;
    }

    /// <summary>
    /// One round has passed (GDD 9.4): decrement every entry, then drop the ones that reached zero. Called
    /// once per completed round, not once per input.
    /// </summary>
    public void Tick()
    {
        for (int i = statuses.Count - 1; i >= 0; i--)
        {
            Status status = statuses[i];
            status.RoundsRemaining--;
            if (status.RoundsRemaining <= 0)
            {
                statuses.RemoveAt(i);
            }
            else
            {
                statuses[i] = status;
            }
        }
    }

    public void Clear()
    {
        statuses.Clear();
    }
}
