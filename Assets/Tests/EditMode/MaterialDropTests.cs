using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Material drop rolls (T06, GDD 13.7). Pure: the table rolls against a supplied generator, so a fixed seed
/// gives exact outcomes instead of a sampled distribution.
/// </summary>
public class MaterialDropTests
{
    private static MaterialDropTable Table(int chance, int min, int max)
    {
        var table = ScriptableObject.CreateInstance<MaterialDropTable>();
        table.enemyName = "Test_yao";
        table.drops.Add(new MaterialDropTable.Drop
        {
            material = ScriptableObject.CreateInstance<ItemDefinition>(),
            chancePercent = chance,
            minAmount = min,
            maxAmount = max
        });
        return table;
    }

    [Test]
    public void AZeroChanceEntry_NeverDrops()
    {
        MaterialDropTable table = Table(0, 1, 1);

        Assert.AreEqual(0, table.Roll(new System.Random(1)).Count);
        Assert.AreEqual(0, table.Roll(new System.Random(2)).Count);
    }

    [Test]
    public void AHundredChanceEntry_AlwaysDrops_WithinItsRange()
    {
        MaterialDropTable table = Table(100, 2, 4);

        for (int seed = 0; seed < 20; seed++)
        {
            List<MaterialDropTable.Rolled> rolled = table.Roll(new System.Random(seed));
            Assert.AreEqual(1, rolled.Count, $"seed {seed}");
            Assert.GreaterOrEqual(rolled[0].amount, 2);
            Assert.LessOrEqual(rolled[0].amount, 4);
        }
    }

    [Test]
    public void TheSameSeed_ProducesTheSameDrops()
    {
        MaterialDropTable table = Table(50, 1, 3);

        List<MaterialDropTable.Rolled> first = table.Roll(new System.Random(4242));
        List<MaterialDropTable.Rolled> second = table.Roll(new System.Random(4242));

        Assert.AreEqual(first.Count, second.Count);
        for (int i = 0; i < first.Count; i++)
        {
            Assert.AreEqual(first[i].material, second[i].material);
            Assert.AreEqual(first[i].amount, second[i].amount);
        }
    }

    [Test]
    public void AReversedRange_StillYieldsAtLeastOne()
    {
        // maxAmount is authored below minAmount: the min is clamped so the drop is never empty or negative.
        MaterialDropTable table = Table(100, 5, 1);

        List<MaterialDropTable.Rolled> rolled = table.Roll(new System.Random(7));

        Assert.AreEqual(1, rolled.Count);
        Assert.AreEqual(5, rolled[0].amount);
    }

    [Test]
    public void AnEmptyEntry_IsSkipped()
    {
        var table = ScriptableObject.CreateInstance<MaterialDropTable>();
        table.drops.Add(new MaterialDropTable.Drop { material = null, chancePercent = 100, minAmount = 1, maxAmount = 1 });

        Assert.AreEqual(0, table.Roll(new System.Random(1)).Count);
    }

    [Test]
    public void EnemiesAreMatchedByName_IgnoringTheCloneSuffix()
    {
        // No tables ship in the test environment, so this only pins the lookup's null-safety and trimming.
        Assert.IsNull(EnemyDrops.For(null));
        Assert.IsNull(EnemyDrops.For(""));
        Assert.IsNull(EnemyDrops.For("Nothing_yao(Clone)"));
    }
}
