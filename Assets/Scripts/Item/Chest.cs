using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A chest the player opens from an adjacent cell.
///
/// The chest cell keeps terrain code 99 (solid) because the Grid remains the single authority
/// for movement -- this component only supplies the visual and the interaction. Its contents are
/// rolled once at generation time, so a level can be inspected (and reproduced from a seed) long
/// before anyone opens it.
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

        IsOpened = true;

        int lost = 0;
        foreach (ItemStack stack in contents)
        {
            if (stack.IsEmpty || stack.definition == null)
            {
                continue;
            }
            lost += inventory.addItem(stack.definition, stack.amount);
        }
        contents.Clear();

        if (lost > 0)
        {
            string message = $"Inventory full ({lost} item(s) left behind)";
            Debug.LogWarning("Chest: " + message);
            if (UIManager.Instance != null && PlayerStats.Instance != null)
            {
                UIManager.Instance.DrawFlowupText(message, PlayerStats.Instance.transform.position);
            }
        }

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
/// Cell -> chest lookup so the player can find a chest to open without a physics query. Mirrors
/// CollectableMap; rebuilt every generation.
/// </summary>
public static class ChestRegistry
{
    private static readonly Dictionary<Vector2Int, Chest> chests = new Dictionary<Vector2Int, Chest>();

    public static int Count => chests.Count;

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
    /// The unopened chest directly next to <paramref name="from"/>, if any. Chests are solid, so
    /// "adjacent" is the only way the player can ever be near one.
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
