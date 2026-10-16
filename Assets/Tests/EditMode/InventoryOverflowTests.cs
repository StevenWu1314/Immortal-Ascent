using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Inventory overflow (T06, GDD 12.7): nothing may be destroyed silently. Uses a bare Inventory, so the
/// no-slots case is exercised - which is exactly the "cannot store anything" path.
/// </summary>
public class InventoryOverflowTests
{
    private GameObject host;
    private Inventory inventory;
    private ItemDefinition material;

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("InventoryOverflowTests");
        inventory = host.AddComponent<Inventory>();
        material = ScriptableObject.CreateInstance<ItemDefinition>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(host);
    }

    [Test]
    public void WithoutSlots_NothingIsStored_AndEverythingIsReported()
    {
        int leftover = inventory.addItem(material, 5);

        Assert.AreEqual(5, leftover, "the caller is told what did not fit instead of losing it silently");
    }

    [Test]
    public void ANullDefinitionOrANonPositiveAmount_IsANoOp()
    {
        Assert.AreEqual(0, inventory.addItem(null, 3));
        Assert.AreEqual(0, inventory.addItem(material, 0));
        Assert.AreEqual(0, inventory.addItem(material, -4));
    }

    [Test]
    public void HasRoomFor_WithoutSlots_IsFalseForAnything()
    {
        Assert.IsFalse(inventory.hasRoomFor(material, 1),
            "with no slots there is never room, which is what stops a mine wasting its ore");
    }
}
