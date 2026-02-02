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
        Debug.Log("rolling lootable");
        int loot = Random.Range(0, 100);
        if(loot <= lootDistribution[2])
        {
            playerInventory.addItem(epicItem[Random.Range(0, epicItem.Count())]);
        }
        else if(loot <= lootDistribution[1])
        {
            playerInventory.addItem(rareItem[Random.Range(0, rareItem.Count())]);
        }
        else
        {
            playerInventory.addItem(commonItem[Random.Range(0, commonItem.Count())]);
        }
    }

}