using System.Collections.Generic;

/// <summary>
/// Arrow counts (GDD 12.5). Pure: the recipe turns ore into a bundle of arrows, and firing spends exactly
/// one. Arrows are a tracked count rather than item stacks, so a quiver no longer occupies ten slots.
/// </summary>
public static class ArrowEconomy
{
    /// <summary>
    /// How many arrows a set of inventory stacks is worth. Arrows live in the quiver count rather than in the
    /// bag (T06), so any ammo a starter list or an old save still hands over is folded into that count.
    /// </summary>
    public static int TotalAmmo(IEnumerable<ItemStack> stacks)
    {
        int total = 0;
        if (stacks == null)
        {
            return 0;
        }

        foreach (ItemStack stack in stacks)
        {
            if (stack.definition != null && stack.definition.Kind == ItemKind.Ammo && stack.amount > 0)
            {
                total += stack.amount;
            }
        }
        return total;
    }
}
