using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Finds the material drop table for an enemy and hands the rolled materials to the inventory.
///
/// Tables live under <c>Resources/DropTables</c> and are matched by name, so an enemy prefab needs no
/// reference wired - which also means a new enemy type is pure data.
/// </summary>
public static class EnemyDrops
{
    private const string ResourcePath = "DropTables";

    private static Dictionary<string, MaterialDropTable> tables;

    /// <summary>The table for an enemy name, or null when the type drops no materials.</summary>
    public static MaterialDropTable For(string enemyName)
    {
        if (tables == null)
        {
            tables = new Dictionary<string, MaterialDropTable>();
            foreach (MaterialDropTable table in Resources.LoadAll<MaterialDropTable>(ResourcePath))
            {
                if (table != null && !string.IsNullOrEmpty(table.enemyName))
                {
                    tables[Normalise(table.enemyName)] = table;
                }
            }
        }

        string key = Normalise(enemyName);
        return !string.IsNullOrEmpty(key) && tables.TryGetValue(key, out MaterialDropTable found)
            ? found
            : null;
    }

    /// <summary>
    /// Roll this enemy's materials into <paramref name="inventory"/>. Returns how many units did not fit, and
    /// reports what was handed over.
    /// </summary>
    public static int Award(string enemyName, Inventory inventory, System.Random rng, out string report)
    {
        report = null;
        MaterialDropTable table = For(enemyName);
        if (table == null || inventory == null)
        {
            return 0;
        }

        int leftover = 0;
        List<string> stored = new List<string>();
        foreach (MaterialDropTable.Rolled rolled in table.Roll(rng))
        {
            int remaining = inventory.addItem(rolled.material, rolled.amount);
            int taken = rolled.amount - remaining;
            if (taken > 0)
            {
                stored.Add($"{taken} x {rolled.material.DisplayName}");
            }
            leftover += remaining;
        }

        if (stored.Count > 0)
        {
            report = string.Join(", ", stored);
        }
        return leftover;
    }

    /// <summary>Drop the cache after an asset changes (editor tooling).</summary>
    public static void ClearCache()
    {
        tables = null;
    }

    /// <summary>"Pig_yao(Clone)" and "Pig_yao" are the same enemy type.</summary>
    private static string Normalise(string enemyName)
    {
        if (string.IsNullOrEmpty(enemyName))
        {
            return null;
        }
        int clone = enemyName.IndexOf("(");
        return (clone > 0 ? enemyName.Substring(0, clone) : enemyName).Trim();
    }
}
