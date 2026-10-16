using UnityEngine;

/// <summary>
/// One enemy type's numbers (T09, GDD 13.1). The prefab keeps its own serialized values as a fallback, so
/// nothing breaks before the definitions are authored and wired.
///
/// Resistances are declared but never read in M1: they exist so the M2 status work does not have to re-author
/// every enemy asset.
/// </summary>
[CreateAssetMenu(menuName = "ScriptableObjects/Enemy Definition", fileName = "Enemy")]
public class EnemyDefinition : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "Enemy";

    [Header("Stats")]
    public int maxHealth = 30;
    public int damage = 5;
    public int experience = 10;

    [Header("Awareness")]
    public float detectionRadius = 10f;
    public int chaseRange = 20;

    [Header("Ranged (leave range at 0 for a melee enemy)")]
    public int range;
    public int rangedCooldown = 2;

    [Header("Drops")]
    public MaterialDropTable dropTable;

    [Header("Reserved for M2 - never read in M1")]
    public int resistancePhysical;
    public int resistanceFire;
    public int resistancePoison;
    public int resistanceEnfeeblement;
}
