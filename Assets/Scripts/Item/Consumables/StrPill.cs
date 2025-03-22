using UnityEngine;
using UnityEngine.EventSystems;

public class StrPill : Consumables
{

    public StrPill(int amount, int effectStrength)
    {
        this.name = "Strength Pill";
        this.stackLimit = 0;
        this.description = "increase your strength by " + effectStrength + " for 50 turns";
        this.amount = amount;
        this.effectStrength = effectStrength;
    }
    private void Awake() {
        itemDisplay = GameObject.Find("ItemInfoDisplay").GetComponent<ItemDisplay>();
        Debug.Log("trying to find itemDisplay");
    }

    public override void displaySelf()
    {
        itemDisplay.setFields(name, description, image, "strength", 10);
    }

    public override void onUse()
    {
        Manager.player.increaseStatTemp("Strength", 10, 50);
    }
    
}