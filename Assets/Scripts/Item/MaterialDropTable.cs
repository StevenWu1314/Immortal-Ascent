using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The materials one enemy type leaves behind (GDD 13.7). Percentages live in data, so tuning never touches
/// code; the table is matched to an enemy by its GameObject name (see <see cref="EnemyDrops"/>).
/// </summary>
[CreateAssetMenu(menuName = "ScriptableObjects/Material Drop Table", fileName = "DropTable")]
public class MaterialDropTable : ScriptableObject
{
    [Tooltip("Enemy GameObject name this table belongs to, e.g. Pig_yao. A '(Clone)' suffix is ignored.")]
    public string enemyName;

    [System.Serializable]
    public struct Drop
    {
        public ItemDefinition material;
        [Range(0, 100)] public int chancePercent;
        public int minAmount;
        public int maxAmount;
    }

    public struct Rolled
    {
        public ItemDefinition material;
        public int amount;
    }

    public List<Drop> drops = new List<Drop>();

    /// <summary>
    /// Roll every entry once. Pure apart from the supplied generator, so a test can pin a seed and assert
    /// exactly what an enemy leaves behind.
    /// </summary>
    public List<Rolled> Roll(System.Random rng)
    {
        List<Rolled> rolled = new List<Rolled>();
        if (rng == null)
        {
            return rolled;
        }

        foreach (Drop drop in drops)
        {
            if (drop.material == null)
            {
                continue;
            }
            if (rng.Next(0, 100) >= Mathf.Clamp(drop.chancePercent, 0, 100))
            {
                continue;
            }

            // minAmount is the floor: a reversed range is raised to it rather than producing an empty drop.
            int min = Mathf.Max(1, drop.minAmount);
            int max = Mathf.Max(min, drop.maxAmount);
            rolled.Add(new Rolled
            {
                material = drop.material,
                amount = rng.Next(min, max + 1)
            });
        }

        return rolled;
    }
}
