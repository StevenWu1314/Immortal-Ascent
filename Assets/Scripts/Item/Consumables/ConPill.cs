using UnityEngine;
using UnityEngine.EventSystems;

public class ConPill : Consumables
{

    public ConPill(int amount)
    {
        this.ItemName = "Constitution Pill";
        this.stackLimit = 100;
        this.effectStrength = 50;
        this.description = "increase your MaxHealth by " + effectStrength + " for 50 turns";
        this.amount = amount;
    }

    public override void displaySelf()
    {
        itemDisplay.setFields(ItemName, description, image, "Constitution", 10, amount, this);
    }

    public override void onUse()
    {
        Manager.player.increaseStatTemp("Max Health", 10, 50);
        base.onUse();
        displaySelf();
    }
    
}