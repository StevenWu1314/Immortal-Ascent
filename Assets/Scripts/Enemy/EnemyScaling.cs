using UnityEngine;

/// <summary>
/// Per-floor enemy scaling (T09, GDD 13.6). Pure: the realm's first floor is index 0 and is unscaled, and
/// boss stats are authored rather than scaled.
/// </summary>
public static class EnemyScaling
{
    public const float HealthPerFloor = 0.15f;
    public const float DamagePerFloor = 0.10f;

    public static float HealthMultiplier(int floorIndex)
    {
        return 1f + HealthPerFloor * Mathf.Max(0, floorIndex);
    }

    public static float DamageMultiplier(int floorIndex)
    {
        return 1f + DamagePerFloor * Mathf.Max(0, floorIndex);
    }

    /// <summary>
    /// Scale health for a floor. Rounds half up rather than to even, so the numbers are predictable when the
    /// designer reads them off the prefab.
    /// </summary>
    public static int ScaleHealth(int baseHealth, int floorIndex, bool isBoss = false)
    {
        if (isBoss)
        {
            return baseHealth;
        }
        return Mathf.Max(1, Mathf.FloorToInt(baseHealth * HealthMultiplier(floorIndex) + 0.5f));
    }

    public static int ScaleDamage(int baseDamage, int floorIndex, bool isBoss = false)
    {
        if (isBoss)
        {
            return baseDamage;
        }
        return Mathf.Max(0, Mathf.FloorToInt(baseDamage * DamageMultiplier(floorIndex) + 0.5f));
    }
}
