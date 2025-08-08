using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class Item : MonoBehaviour
{
    [SerializeField] protected int id;
    [SerializeField] protected string name;
    [SerializeField] protected string description;
    [SerializeField] protected Sprite image;
    [SerializeField] protected int amount;
    [SerializeField] protected int stackLimit;
    [SerializeField] public ItemDisplay itemDisplay;


    
    public string getName()
    {
        return name;
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
        if (amount < stackLimit)
        {
            amount += a;
        }
    }
    public abstract void displaySelf();
    public abstract void onUse();
}
