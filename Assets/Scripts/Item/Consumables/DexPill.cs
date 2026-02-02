using UnityEngine;
using UnityEngine.EventSystems;

public class DexPill : Consumables
{

    public DexPill(int amount)
    {
        this.ItemName = "Strength Pill";
        this.stackLimit = 100;
        this.effectStrength = 10;
        this.description = "increase your strength by " + effectStrength + " for 50 turns";
        this.amount = amount;
        
    }


    public override void displaySelf()
    {
        itemDisplay.setFields(ItemName, description, image, "Dexterity", 10, amount, this);
    }

    public override void onUse()
    {
        PlayerStats.Instance.increaseStatTemp("Dexterity", 10, 50);
        base.onUse();
        displaySelf();
    }
    
}