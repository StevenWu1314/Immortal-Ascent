using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A chest the player opens from an adjacent cell.
///
/// The cell starts as terrain code 99 (solid) so the chest is opened from a neighbouring cell
/// rather than stood on. Opening it makes the cell passable: the Grid stays the single authority
/// for movement, and this component is what tells the Grid (and IslandManager) that it changed.
/// Contents are rolled once at generation time, so a level can be inspected (and reproduced from a
/// seed) long before anyone opens it.
/// </summary>
public class Chest : MonoBehaviour
{
    [SerializeField] private List<ItemStack> contents = new List<ItemStack>();
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("Optional: sprite to swap to once opened. Falls back to a tint.")]
    [SerializeField] private Sprite openedSprite;
    [SerializeField] private Color openedTint = new Color(1f, 1f, 1f, 0.45f);

    public bool IsOpened { get; private set; }
    public IReadOnlyList<ItemStack> Contents => contents;

    /// <summary>Grid cell this chest occupies, matching how EntityManager indexes the world.</summary>
    public Vector2Int Cell => Vector2Int.FloorToInt(transform.position);

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void OnEnable()
    {
        ChestRegistry.Register(this);
    }

    private void OnDisable()
    {
        ChestRegistry.Unregister(this);
    }

    /// <summary>Fill the chest. Called by the generator immediately after instantiation.</summary>
    public void SetContents(List<ItemStack> rolled)
    {
        contents = rolled ?? new List<ItemStack>();
    }

    /// <summary>
    /// Grant everything inside. An item that does not fit is reported rather than destroyed, and
    /// the chest still opens -- otherwise a full inventory would trap the player in a loop of
    /// re-opening the same chest.
    /// </summary>
    public bool Open(Inventory inventory)
    {
        if (IsOpened)
        {
            return false;
        }
        if (inventory == null)
        {
            Debug.LogWarning("Chest.Open: no Inventory available.");
            return false;
        }

        // IsOpened is set further down, and only once the chest is actually empty: a chest that still holds
        // what did not fit stays openable.

        // Keep whatever did not fit so the player can come back with room (GDD 12.7): clearing the chest
        // outright destroyed the leftovers the moment the inventory filled up.
        List<ItemStack> remaining = new List<ItemStack>();
        int lost = 0;
        foreach (ItemStack stack in contents)
        {
            if (stack.IsEmpty || stack.definition == null)
            {
                continue;
            }

            int leftover = inventory.addItem(stack.definition, stack.amount);
            lost += leftover;
            if (leftover > 0)
            {
                // Equipment carries its inscriptions on the instance, so a leftover instance is kept whole.
                // Equipment never stacks, so a partial hand-over can only happen between instances.
                remaining.Add(stack.instance != null
                    ? new ItemStack(stack.instance)
                    : new ItemStack(stack.definition, leftover));
            }
        }
        contents.Clear();
        contents.AddRange(remaining);

        if (lost > 0)
        {
            string message = $"Inventory full ({lost} item(s) left in the chest)";
            Debug.LogWarning("Chest: " + message);
            if (UIManager.Instance != null && PlayerStats.Instance != null)
            {
                UIManager.Instance.DrawFlowupText(message, PlayerStats.Instance.transform.position);
            }
        }

        // Anything still inside means the chest is not spent: it stays registered, solid and re-openable,
        // which is what lets the player come back for the leftovers.
        if (contents.Count > 0)
        {
            return true;
        }

        IsOpened = true;

        // An opened chest stops being an obstacle. The Grid is the authority for movement, and
        // IslandManager snapshotted this cell as blocked, so both have to be updated or the room
        // gate keeps refusing the step.
        Vector2Int cell = Cell;
        if (Grid.Current != null)
        {
            Grid.Current.SetValueAtLocation(cell.x, cell.y, 0);
        }
        IslandManager.MarkWalkable(cell);

        // Visual feedback that this one is spent.
        if (spriteRenderer != null)
        {
            if (openedSprite != null)
            {
                spriteRenderer.sprite = openedSprite;
            }
            else
            {
                spriteRenderer.color = openedTint;
            }
        }

        ChestRegistry.Unregister(this);
        return true;
    }
}

/// <summary>
/// Cell -> chest lookup so the player can find a chest to open without a physics query.
/// Rebuilt every generation.
/// </summary>
public static class ChestRegistry
{
    private static readonly Dictionary<Vector2Int, Chest> chests = new Dictionary<Vector2Int, Chest>();

    /// <summary>The cells of the unopened chests, for the minimap (T08).</summary>
    public static IEnumerable<Vector2Int> Cells => chests.Keys;

    public static void Register(Chest chest)
    {
        if (chest == null)
        {
            return;
        }
        chests[chest.Cell] = chest;
    }

    public static void Unregister(Chest chest)
    {
        if (chest == null)
        {
            return;
        }
        Vector2Int cell = chest.Cell;
        if (chests.TryGetValue(cell, out Chest existing) && existing == chest)
        {
            chests.Remove(cell);
        }
    }

    public static void Reset()
    {
        chests.Clear();
    }

    /// <summary>
    /// The unopened chest directly next to <paramref name="from"/>, if any. An unopened chest is
    /// solid, so "adjacent" is the only way the player can ever be near one.
    /// </summary>
    public static bool TryGetAdjacent(Vector2Int from, out Chest chest)
    {
        chest = null;
        Vector2Int[] neighbours =
        {
            from + Vector2Int.up,
            from + Vector2Int.down,
            from + Vector2Int.left,
            from + Vector2Int.right
        };

        foreach (Vector2Int cell in neighbours)
        {
            if (chests.TryGetValue(cell, out Chest candidate) && candidate != null && !candidate.IsOpened)
            {
                chest = candidate;
                return true;
            }
        }
        return false;
    }
}
