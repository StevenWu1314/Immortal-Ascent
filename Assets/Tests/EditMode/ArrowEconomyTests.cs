using NUnit.Framework;

/// <summary>
/// Arrow counts (T06, GDD 12.5): one ore is a bundle, and nothing can make arrows negative.
/// </summary>
public class ArrowEconomyTests
{


    [Test]
    public void AmmoStacks_AreWorthTheirTotal()
    {
        // The real Arrow asset: a CreateInstance definition defaults to ItemKind.Material, which is not ammo.
        ItemDefinition arrow = UnityEngine.Resources.Load<ItemDefinition>("ItemDefinitions/Consumables/Arrow");
        ItemDefinition ore = UnityEngine.Resources.Load<ItemDefinition>("ItemDefinitions/Materials/Iron");
        NUnit.Framework.Assert.IsNotNull(arrow, "the project should ship the Arrow (Ammo) definition");
        NUnit.Framework.Assert.IsNotNull(ore, "the project should ship an Iron material");
        NUnit.Framework.Assert.AreEqual(ItemKind.Ammo, arrow.Kind);

        var stacks = new System.Collections.Generic.List<ItemStack>
        {
            new ItemStack(arrow, 7),
            new ItemStack(ore, 99),          // not ammo, so it is not arrows
            new ItemStack(arrow, 3)
        };

        Assert.AreEqual(10, ArrowEconomy.TotalAmmo(stacks));
        Assert.AreEqual(0, ArrowEconomy.TotalAmmo(null));
        Assert.AreEqual(0, ArrowEconomy.TotalAmmo(new System.Collections.Generic.List<ItemStack>()));
    }
}
