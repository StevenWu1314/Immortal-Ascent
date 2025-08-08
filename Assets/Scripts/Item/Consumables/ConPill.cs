using UnityEngine;
using UnityEngine.EventSystems;

public class ConPill : Consumables
{

    public ConPill(int amount)
    {
        this.name = "Strength Pill";
        this.stackLimit = 100;
        this.effectStrength = 50;
        this.description = "increase your MaxHealth by " + effectStrength + " for 50 turns";
        this.amount = amount;
    }
    private void Awake() {
        itemDisplay = GameObject.Find("ItemInfoDisplay").GetComponent<ItemDisplay>();
        Debug.Log("trying to find itemDisplay");
    }

    public override void displaySelf()
    {
        itemDisplay.setFields(name, description, image, "strength", 10, this);
    }

    public override void onUse()
    {
        Manager.player.increaseStatTemp("Max Health", 10, 50);
    }
    
}