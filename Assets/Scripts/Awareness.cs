using System;
using UnityEngine;

/// <summary>
/// The awareness cycle of one enemy (GDD 6.5). The names and order match the old
/// <c>EnemyBehavior.state</c> enum, so serialized <c>currentstate</c> values keep their meaning.
/// </summary>
public enum AwarenessState
{
    Idle,
    Chase,
    Return
}

/// <summary>How a realm's enemies pick up the player (GDD 13.4).</summary>
public enum AggroMode
{
    /// <summary>Plains: setting foot on the island alerts every enemy standing on it.</summary>
    RoomWide,

    /// <summary>Desert and tundra: notice inside the detection radius, with line of sight, unshared.</summary>
    DetectionRadius
}

/// <summary>
/// Who notices the player and how the cycle moves. Pure and static - no scene, no MonoBehaviour - so the
/// rules are testable on their own.
/// </summary>
public static class Awareness
{
    /// <summary>
    /// Does this enemy notice the player right now?
    ///
    /// Room-wide realms compare islands, which is what T03's turn-based rule already uses. When the island
    /// map is unavailable (an editor-generated level with no islands) even a room-wide realm falls back to
    /// the radius, so enemies can still wake up.
    /// </summary>
    public static bool Notices(AggroMode mode, int enemyIsland, int playerIsland,
                               float distance, float detectionRadius, bool hasClearShot)
    {
        bool islandsKnown = enemyIsland != IslandManager.Unassigned && playerIsland != IslandManager.Unassigned;

        if (mode == AggroMode.RoomWide && islandsKnown)
        {
            // Only a real room counts. Corridors (<see cref="IslandManager.Passage"/>) are not rooms, and a
            // room-wide realm does not fall back to the radius when the island map is present.
            return enemyIsland == playerIsland && enemyIsland > IslandManager.Passage;
        }

        return detectionRadius > 0f && distance <= detectionRadius && hasClearShot;
    }

    /// <summary>
    /// One step of the cycle. <paramref name="shouldReturn"/> is the caller's "beyond chaseRange", or
    /// "lost line of sight" in radius mode; <paramref name="atPost"/> is only read while returning.
    /// </summary>
    public static AwarenessState Next(AwarenessState current, bool notices, bool shouldReturn, bool atPost)
    {
        switch (current)
        {
            case AwarenessState.Idle:
                return notices ? AwarenessState.Chase : AwarenessState.Idle;

            case AwarenessState.Chase:
                return shouldReturn ? AwarenessState.Return : AwarenessState.Chase;

            default:
                if (notices)
                {
                    return AwarenessState.Chase;
                }
                return atPost ? AwarenessState.Idle : AwarenessState.Return;
        }
    }
}

/// <summary>
/// Where an unaware enemy steps (GDD 6.6). Pure so the rule - stay on the island, never onto an occupied
/// cell, hold still when boxed in - is testable without a scene.
/// </summary>
public static class Roaming
{
    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
    };

    /// <summary>
    /// One wandering step, or <see cref="Vector2Int.zero"/> when every neighbour is blocked or occupied.
    /// <paramref name="rotation"/> rotates the direction order so a caller with a random source still gets
    /// a deterministic choice.
    /// </summary>
    public static Vector2Int ChooseStep(Vector2Int from, int rotation,
                                        Func<Vector2Int, bool> canStepOnto,
                                        Func<Vector2Int, bool> isOccupied)
    {
        if (canStepOnto == null || isOccupied == null)
        {
            return Vector2Int.zero;
        }

        for (int i = 0; i < Directions.Length; i++)
        {
            Vector2Int direction = Directions[((rotation % Directions.Length) + Directions.Length + i) % Directions.Length];
            Vector2Int cell = from + direction;
            if (canStepOnto(cell) && !isOccupied(cell))
            {
                return direction;
            }
        }

        return Vector2Int.zero;
    }
}
