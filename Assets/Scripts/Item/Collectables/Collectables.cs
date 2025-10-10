using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Collectables : Item
{
    public Vector2 dropRange = new Vector2(1, 1); // The amount that can be dropped when collecting this item

    public void collectThis()
    {
        int amount = (int)Random.Range(dropRange[0], dropRange[1] + 1);
        Debug.Log(amount);
        for (int i = 0; i < amount; i++)
        {
            Inventory.Instance.addItem(this);
        }
    }
}
