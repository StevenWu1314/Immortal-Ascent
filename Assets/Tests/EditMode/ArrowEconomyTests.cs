using NUnit.Framework;

/// <summary>
/// Arrow counts (T06, GDD 12.5): one ore is a bundle, and nothing can make arrows negative.
/// </summary>
public class ArrowEconomyTests
{
    [Test]
    public void OneOre_IsABundleOfTen()
    {
        Assert.AreEqual(10, ArrowEconomy.ArrowsFromOre(1));
        Assert.AreEqual(30, ArrowEconomy.ArrowsFromOre(3));
        Assert.AreEqual(ArrowEconomy.ArrowsPerOre, ArrowEconomy.ArrowsFromOre(1));
    }

    [Test]
    public void NoOreOrLess_IsWorthNothing()
    {
        Assert.AreEqual(0, ArrowEconomy.ArrowsFromOre(0));
        Assert.AreEqual(0, ArrowEconomy.ArrowsFromOre(-5));
    }
}
