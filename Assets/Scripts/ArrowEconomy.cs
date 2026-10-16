/// <summary>
/// Arrow counts (GDD 12.5). Pure: the recipe turns ore into a bundle of arrows, and firing spends exactly
/// one. Arrows are a tracked count rather than item stacks, so a quiver no longer occupies ten slots.
/// </summary>
public static class ArrowEconomy
{
    public const int ArrowsPerOre = 10;

    /// <summary>Arrows a batch of ore is worth. Zero ore is worth nothing, never a negative count.</summary>
    public static int ArrowsFromOre(int ore)
    {
        return ore > 0 ? ore * ArrowsPerOre : 0;
    }
}
