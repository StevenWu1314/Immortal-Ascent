using System;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;



public class LootGen : MonoBehaviour 
{
    [SerializeField]private int[] lootDistribution = new int[3] {100, 30, -1};
    [SerializeField]private Item[] commonItem = new Item[0];
    [SerializeField]private Item[] rareItem = new Item[0];
    [SerializeField]private Item[] epicItem = new Item[0];
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
        Grid.chestOpen += rollTable;
        playerInventory = Inventory.Instance;
    }
    void OnDestroy()
    {
        Grid.chestOpen -= rollTable;
    }
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

        int loot = Random.Range(0, 100);

        // Each tier is validated before indexing: the arrays default to empty and can also
        // contain null entries, either of which previously threw during a chest roll.
        if (lootDistribution.Length > 2 && loot <= lootDistribution[2])
        {
            Item epic = Pick(epicItem);
            if (epic != null) { playerInventory.addItem(epic); return; }
        }
        if (lootDistribution.Length > 1 && loot <= lootDistribution[1])
        {
            Item rare = Pick(rareItem);
            if (rare != null) { playerInventory.addItem(rare); return; }
        }
        Item common = Pick(commonItem);
        if (common != null)
        {
            playerInventory.addItem(common);
        }
    }

    private Item Pick(Item[] pool)
    {
        if (pool == null || pool.Length == 0)
        {
            return null;
        }
        // Retry a few times to skip null entries rather than dereferencing one.
        for (int attempt = 0; attempt < pool.Length; attempt++)
        {
            Item candidate = pool[Random.Range(0, pool.Length)];
            if (candidate != null)
            {
                return candidate;
            }
        }
        Debug.LogWarning("LootGen: loot pool contains no valid entries.");
        return null;
    }

}