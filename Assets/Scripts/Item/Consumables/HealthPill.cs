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
    private void Start() {
        itemDisplay = FindObjectOfType<ItemDisplay>();
        Debug.Log("trying to find itemDisplay");
    }

    public override void displaySelf()
    {
        itemDisplay.setFields(name, description, image, "Heal", 10, amount, this);
    }

    public override void onUse()
    {
        PlayerStats.Instance.heal(10);
        base.onUse();
        displaySelf();
    }
    
}