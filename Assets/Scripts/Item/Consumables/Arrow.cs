using UnityEngine;
using UnityEngine.EventSystems;

public class Arrow : Item
{

    public Arrow(int amount)
    {
        this.ItemName = "Arrow";
        this.stackLimit = 100;
        this.description = "Plain old arrows";
        this.amount = amount;
        
    }


    public override void displaySelf()
    {
        itemDisplay.setFields(ItemName, description, image, "damage", 0, amount, this);
    }

    public override void onUse()
    {
        
    }
    
}