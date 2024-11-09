using System;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;



public class LootGen : MonoBehaviour 
{
    [SerializeField]private int[] lootDistribution = new int[3] {100, 30, 0};
    [SerializeField]private Item[] commonItem = new Item[3];
    [SerializeField]private Item[] rareItem = new Item[3];
    [SerializeField]private Item[] epicItem = new Item[3];
    [SerializeField] private Inventory playerInventory;
    
    private void Start() {
        Grid.chestOpen += rollTable;
    }
    public void rollTable()
    {
        int loot = Random.Range(0, 100);
        if(loot <= lootDistribution[2])
        {
            playerInventory.addItem(epicItem[Random.Range(0, epicItem.Count()-1)]);
        }
        else if(loot <= lootDistribution[1])
        {
            playerInventory.addItem(epicItem[Random.Range(0, rareItem.Count()-1)]);
        }
        else
        {
            playerInventory.addItem(commonItem[Random.Range(0, commonItem.Count()-1)]);
        }
    }

}