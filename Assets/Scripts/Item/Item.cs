using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public abstract class Item : MonoBehaviour
{
    [SerializeField] protected int id;
    [SerializeField] protected string ItemName;
    [SerializeField] protected string description;
    [SerializeField] protected Sprite image;
    [SerializeField] protected int amount;
    [SerializeField] protected int stackLimit;
    [SerializeField] public ItemDisplay itemDisplay;


    
    public string getName()
    {
        return ItemName;
    }

    public string getDescription()
    {
        return description;
    }
    internal Sprite getSprite()
    {
        return image;
    }
    public void increaseAmount(int a)
    {
        amount += a;

    }
    public int getAmount()
    {
        return amount;
    }
    public void subtractAmount(int a)
    {
        if (amount >= a)
        {
            amount -= a;
        }
        else
        {
            Debug.Log("Trying to consume more than what you have");
        }
    }
    public abstract void displaySelf();
    public abstract void onUse();
    private void Start()
    {
        itemDisplay = GameObject.Find("ItemInfoDisplay").GetComponent<ItemDisplay>();
        Debug.Log("trying to find itemDisplay");
    }
    
    public Item clone()
    {
        return (Item)this.MemberwiseClone();
    }
}
