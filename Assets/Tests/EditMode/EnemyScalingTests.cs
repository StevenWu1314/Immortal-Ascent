using NUnit.Framework;

/// <summary>
/// Per-floor scaling (T09, GDD 13.6). Pure.
/// </summary>
public class EnemyScalingTests
{
    [Test]
    public void TheFirstFloor_IsUnscaled()
    {
        Assert.AreEqual(1f, EnemyScaling.HealthMultiplier(0));
        Assert.AreEqual(1f, EnemyScaling.DamageMultiplier(0));

        Assert.AreEqual(50, EnemyScaling.ScaleHealth(50, 0));
        Assert.AreEqual(10, EnemyScaling.ScaleDamage(10, 0));
    }

    [Test]
    public void TheSecondFloor_UsesTheDocumentedMultipliers()
    {
        Assert.AreEqual(1.15f, EnemyScaling.HealthMultiplier(1), 0.0001f);
        Assert.AreEqual(1.10f, EnemyScaling.DamageMultiplier(1), 0.0001f);

        Assert.AreEqual(58, EnemyScaling.ScaleHealth(50, 1), "50 * 1.15 = 57.5, rounded half up");
        Assert.AreEqual(11, EnemyScaling.ScaleDamage(10, 1));
        Assert.AreEqual(35, EnemyScaling.ScaleHealth(30, 1), "30 * 1.15 = 34.5, rounded half up");
    }

    [Test]
    public void LaterFloors_KeepScaling()
    {
        Assert.AreEqual(65, EnemyScaling.ScaleHealth(50, 2), "1 + 0.15 * 2 = 1.30");
        Assert.AreEqual(12, EnemyScaling.ScaleDamage(10, 2), "1 + 0.10 * 2 = 1.20");
    }

    [Test]
    public void BossStats_AreNeverScaled()
    {
        Assert.AreEqual(200, EnemyScaling.ScaleHealth(200, 5, isBoss: true));
        Assert.AreEqual(25, EnemyScaling.ScaleDamage(25, 5, isBoss: true));
    }

    [Test]
    public void AFloorBelowTheFirst_IsTreatedAsTheFirst()
    {
        Assert.AreEqual(1f, EnemyScaling.HealthMultiplier(-3));
        Assert.AreEqual(50, EnemyScaling.ScaleHealth(50, -3));
    }

    [Test]
    public void HealthNeverReachesZero_AndDamageNeverGoesNegative()
    {
        Assert.AreEqual(1, EnemyScaling.ScaleHealth(0, 0), "an enemy with 1 hp is better than an unkillable zero");
        Assert.AreEqual(0, EnemyScaling.ScaleDamage(0, 0));
        Assert.AreEqual(0, EnemyScaling.ScaleDamage(-5, 3));
    }
}
