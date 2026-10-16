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
}
