/// <summary>
/// What a player action costs (GDD 6.2) and when the game is turn-based at all (GDD 6.1).
/// Pure and static - no MonoBehaviour, no scene - so the action economy is testable without entering
/// Play mode, and <see cref="Controls"/> stays a caller rather than the owner of the rules.
/// </summary>
public enum TurnAction
{
    Move,
    MeleeAttack,
    BowShot,
    UseItem,
    PlaceTorch,
    MineOre,
    OpenChest,
    Equip,
    ApplyRune,
    Craft,
    LevelUp,
    AimEnter,
    AimExit
}

public static class TurnRules
{
    /// <summary>
    /// Turn-based while a living enemy shares the player's room. Corridors
    /// (<see cref="IslandManager.Passage"/>) and blocked cells never are, which is what makes travelling
    /// between cleared rooms free.
    /// </summary>
    public static bool IsTurnBased(int islandId, int livingEnemiesOnIsland)
    {
        return islandId > IslandManager.Passage && livingEnemiesOnIsland > 0;
    }

    /// <summary>
    /// GDD 6.2: exactly these six actions end a turn. Looting, equipping, applying runes, crafting,
    /// levelling up and entering or leaving aim mode are free - so a free action, or an action that
    /// failed, never costs the player a round.
    /// </summary>
    public static bool ConsumesTurn(TurnAction action)
    {
        switch (action)
        {
            case TurnAction.Move:
            case TurnAction.MeleeAttack:
            case TurnAction.BowShot:
            case TurnAction.UseItem:
            case TurnAction.PlaceTorch:
            case TurnAction.MineOre:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// A step needs walkable terrain, an island gate that allows leaving, and no Root status.
    /// <paramref name="rooted"/> is the T05 hook and is false everywhere until statuses exist.
    /// </summary>
    public static bool IsMovementAllowed(bool terrainWalkable, bool islandAllows, bool rooted)
    {
        return terrainWalkable && islandAllows && !rooted;
    }
}
