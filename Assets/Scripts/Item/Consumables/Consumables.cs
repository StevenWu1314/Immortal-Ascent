using UnityEngine;

public abstract class Consumables : Item
{
    protected int effectStrength;

    public override void onUse()
    {
        Debug.Log("item Used");
        this.amount -= 1;
        if (this.amount <= 0)
        {
            Inventory.Instance.removeItem(this);
        }
        
    }
}