using UnityEngine;
using Unity;
public class Armor : Item
{
    [SerializeField] int Health;
    [SerializeField] public GameObject equipedIcon;
    public Armor(int health, bool range, string name, int id, string description, Sprite sprite) {
    this.Health = health;
    this.ItemName = name;
    this.id = id;
    this.description = description;
    stackLimit = 1;
    amount = 1;
    this.image = sprite;
    }
    public int getHealth(){
        return Health;
    }

    public override void displaySelf()
    {
        itemDisplay.setFields(ItemName, description, image, "Health", Health, 1, this);
    }
    public override void onUse()
    {
        Equipments.Instance.updateEquipment("Armor", this);
        Debug.Log("attempting to update Armor");
        
    }

    
}
