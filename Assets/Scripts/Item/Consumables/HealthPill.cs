using UnityEngine;
using UnityEngine.EventSystems;

public class HealthPill : Consumables
{
    
    public HealthPill(int amount)
    {
        this.name = "Health Pill";
        this.stackLimit = 10;
        this.description = "Heals you 10 hp";
        this.amount = amount;
    }
    private void Awake() {
        itemDisplay = GameObject.Find("ItemInfoDisplay").GetComponent<ItemDisplay>();
        Debug.Log("trying to find itemDisplay");
    }

    public override void displaySelf()
    {
        itemDisplay.setFields(name, description, image, "buff", 10, this);
    }

    public override void onUse()
    {
        Manager.player.heal(10);
        Debug.Log("item Used");
    }
    
}