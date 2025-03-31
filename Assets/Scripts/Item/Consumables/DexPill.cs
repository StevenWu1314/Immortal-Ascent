using UnityEngine;
using UnityEngine.EventSystems;

public class DexPill : Consumables
{

    public DexPill(int amount)
    {
        this.name = "Strength Pill";
        this.stackLimit = 100;
        this.effectStrength = 10;
        this.description = "increase your strength by " + effectStrength + " for 50 turns";
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
        Manager.player.increaseStatTemp("Dexterity", 10, 50);
    }
    
}