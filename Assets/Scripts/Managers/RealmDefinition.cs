using UnityEngine;

/// <summary>
/// Run-level data for one realm. Deliberately minimal: floor count is all T02 consumes. Enemy pools,
/// loot tables and visibility belong to the systems that read them (T04/T06) and are not declared here
/// until those exist, so nobody has to author fields nothing looks at.
/// </summary>
[CreateAssetMenu(menuName = "ScriptableObjects/Realm Definition", fileName = "Realm")]
public class RealmDefinition : ScriptableObject
{
    [Tooltip("Order in the run. 0 is the realm the run starts in.")]
    public int realmIndex;

    [Tooltip("Shown in the HUD, e.g. Plains.")]
    public string displayName = "Plains";

    [Tooltip("Floors in this realm, including its boss floor.")]
    public int floorsInRealm = 2;

    [Tooltip("How this realm's enemies notice the player (GDD 13.4). Plains is room-wide.")]
    public AggroMode aggroMode = AggroMode.RoomWide;

    [Tooltip("The metal ore veins in this realm yield (GDD 12.2). Plains is Iron Ore.")]
    public ItemDefinition metal;
}
