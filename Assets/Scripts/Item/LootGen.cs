using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Rolls a drop when an enemy dies. Replaces the old descending-threshold int[] table, whose
/// last entry was -1 so the "epic" tier could never be selected, and whose index 0 was never
/// read at all.
/// </summary>
public class LootGen : MonoBehaviour
{
    [Header("Tier chances (percent, evaluated in this order)")]
    [Range(0, 100)]
    [SerializeField] private int epicChance = 1;
    [Range(0, 100)]
    [SerializeField] private int rareChance = 30;

    [SerializeField] private ItemDefinition[] commonItem = new ItemDefinition[0];
    [SerializeField] private ItemDefinition[] rareItem = new ItemDefinition[0];
    [SerializeField] private ItemDefinition[] epicItem = new ItemDefinition[0];

    [Tooltip("Units granted per drop.")]
    [SerializeField] private int dropAmount = 1;

    [SerializeField] private Inventory playerInventory;
    public static LootGen Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        playerInventory = Inventory.Instance;
    }

    /// <summary>
    /// Roll one drop into the inventory. There is no separate chest trigger any more: the old
    /// Grid.chestOpen event had no raiser anywhere in the project, so chest loot never fired.
    /// </summary>
    public void rollTable()
    {
        if (playerInventory == null)
        {
            playerInventory = Inventory.Instance;
        }
        if (playerInventory == null)
        {
            Debug.LogWarning("LootGen: no Inventory available; skipping loot roll.");
            return;
        }

        int roll = Random.Range(0, 100);

        // Each tier falls through to the next when its pool is empty, so a single populated
        // pool still works. epicChance is evaluated first, which is what makes it reachable.
        if (roll < epicChance)
        {
            if (TryGive(epicItem) || TryGive(rareItem) || TryGive(commonItem))
            {
                return;
            }
        }
        else if (roll < epicChance + rareChance)
        {
            if (TryGive(rareItem) || TryGive(commonItem))
            {
                return;
            }
        }
        TryGive(commonItem);
    }

    private bool TryGive(ItemDefinition[] pool)
    {
        ItemDefinition pick = Pick(pool);
        if (pick == null)
        {
            return false;
        }

        int amount = Mathf.Max(1, dropAmount);
        int leftover = playerInventory.addItem(pick, amount);
        if (leftover > 0)
        {
            string message = $"Inventory full ({leftover} x {pick.DisplayName} lost)";
            Debug.LogWarning("LootGen: " + message);
            if (UIManager.Instance != null && PlayerStats.Instance != null)
            {
                UIManager.Instance.DrawFlowupText(message, PlayerStats.Instance.transform.position);
            }
        }
        return true;
    }

    private ItemDefinition Pick(ItemDefinition[] pool)
    {
        if (pool == null || pool.Length == 0)
        {
            return null;
        }
        // Retry a few times to skip empty entries rather than dereferencing one.
        for (int attempt = 0; attempt < pool.Length; attempt++)
        {
            ItemDefinition candidate = pool[Random.Range(0, pool.Length)];
            if (candidate != null)
            {
                return candidate;
            }
        }
        Debug.LogWarning("LootGen: loot pool contains no valid entries.");
        return null;
    }
}
