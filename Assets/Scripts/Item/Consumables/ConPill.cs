using UnityEngine;
using UnityEngine.EventSystems;

public class ConElixer : Elixers
{

    public ConElixer(int amount)
    {
        this.ItemName = "Constitution Elixer";
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
        // Was Manager.player, which is never assigned anywhere and threw before the pill
        // was consumed, leaving the item both unusable and un-consumed.
        if (PlayerStats.Instance == null)
        {
            return;
        }
        // NOTE: 10 matches this item's displayed value. The 50 written into effectStrength
        // above is dead (MonoBehaviour constructors never run) and the prefab's description
        // string says 50 — that mismatch is a data/design decision, not fixed here.
        PlayerStats.Instance.increaseStatTemp("Max Health", 10, 50);
        base.onUse();
        displaySelf();
    }
    
}