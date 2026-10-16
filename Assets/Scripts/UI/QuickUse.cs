using UnityEngine;

/// <summary>
/// The quick-use pill slot (T08, GDD 14.1).
///
/// One selected consumable, activated by a key. It goes through <see cref="ItemUsage"/> - the same path the
/// inventory button uses - so there is exactly one place that applies a pill, and using it costs a turn.
/// </summary>
public class QuickUse : MonoBehaviour
{
    /// <summary>Proposed key. Confirm at the T08 checkpoint; changing it is this one constant.</summary>
    public const KeyCode UseKey = KeyCode.Q;

    public static QuickUse Instance { get; private set; }

    [SerializeField] private ItemStack selected;

    public ItemStack Selected => selected;
    public bool HasSelection => !selected.IsEmpty && selected.definition != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>Select what the key will use. Selecting is free; using is what costs the turn.</summary>
    public void Select(ItemStack stack)
    {
        selected = stack;
    }

    public void ClearSelection()
    {
        selected = default;
    }

    /// <summary>
    /// The key check, split out so a test (and the PlayDriver) can drive the action without synthesising input.
    /// </summary>
    public bool TryUse(Controls controls)
    {
        if (!HasSelection || Inventory.Instance == null)
        {
            return false;
        }

        // Only a use that actually consumed the item costs a turn: a failed use must stay free.
        int before = Inventory.Instance.countOf(selected.definition);
        ItemUsage.Use(selected, Inventory.Instance);
        if (Inventory.Instance.countOf(selected.definition) >= before)
        {
            return false;
        }

        if (selected.amount <= 0 || Inventory.Instance.countOf(selected.definition) <= 0)
        {
            ClearSelection();
        }

        if (controls != null)
        {
            controls.SpendTurn(TurnAction.UseItem);
        }
        return true;
    }

    private void Update()
    {
        if (!Input.GetKeyDown(UseKey))
        {
            return;
        }

        PlayerStats player = PlayerStats.Instance;
        Controls controls = player != null ? player.GetComponent<Controls>() : null;
        if (controls == null || controls.takingTurn || controls.MenuIsOpen)
        {
            return;   // the input lock and open menus win; the item is not consumed
        }
        TryUse(controls);
    }
}
