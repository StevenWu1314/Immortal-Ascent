using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HealthPill : Consumables
{

    public HealthPill(int amount)
    {
        this.ItemName = "Health Pill";
        this.stackLimit = 10;
        this.description = "Heals you 10 hp";
        this.amount = amount;
    }

    public override void displaySelf()
    {
        itemDisplay.setFields(ItemName, description, image, "Heal", 10, amount, this);
    }

    public override void onUse()
    {
        PlayerStats.Instance.heal(10);
        base.onUse();
        displaySelf();
    }
    
}