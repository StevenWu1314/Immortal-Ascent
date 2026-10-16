using UnityEngine;

/// <summary>
/// The statuses on one entity, as a component. Thin wrapper: the rules live in <see cref="StatusSet"/> so
/// they are testable without a scene, and this class only adds the unity-side logging and the HUD query.
///
/// It belongs on the player and (for M2) on enemies; in M1 only the player gets one, and nothing applies
/// a status to an enemy yet.
/// </summary>
public class StatusController : MonoBehaviour
{
    /// <summary>Rounds a spider web's Root lasts (GDD 5.4).</summary>
    public const int RootRounds = 2;

    private readonly StatusSet statuses = new StatusSet();

    /// <summary>The statuses themselves, for rules-level queries.</summary>
    public StatusSet Set => statuses;

    /// <summary>True while anything is active. The HUD indicator (T08) reads this.</summary>
    public bool Active => statuses.Count > 0;

    public bool IsRooted => statuses.BlocksMovement;

    /// <summary>Apply a status; refuses and logs anything M1 does not implement.</summary>
    public bool Apply(StatusKind kind, int magnitude, int rounds)
    {
        if (statuses.TryApply(kind, magnitude, rounds, out string rejection))
        {
            return true;
        }

        Debug.LogWarning($"[STATUS] {name}: {rejection}");
        return false;
    }

    /// <summary>One round has passed. The turn pipeline calls this once per completed round.</summary>
    public void TickRound()
    {
        statuses.Tick();
    }

    public void Clear()
    {
        statuses.Clear();
    }
}
