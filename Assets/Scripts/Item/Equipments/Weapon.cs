using UnityEngine;
using Unity;
public class Weapon : Item
{
    [SerializeField] int damage;
    [SerializeField] public GameObject equipedIcon;
    [SerializeField] bool range;
    public Weapon(int damage, bool range, string name, int id, string description, Sprite sprite) {
    this.damage = damage;
    this.range = range;
    this.name = name;
    this.id = id;
    this.description = description;
    stackLimit = 1;
    amount = 1;
    this.image = sprite;
    }
    public int getDamage(){
        return damage;
    }

    public override void displaySelf()
    {
        itemDisplay.setFields(name, description, image, "Damage", damage, 1, this);
    }
    public override void onUse()
    {
       // equipedIcon.SetActive(!equipedIcon.activeInHierarchy);
        if(range)
        {
            Equipments.Instance.updateEquipment("Range Weapon", this);
        }
        else
        {
            Equipments.Instance.updateEquipment("Melee Weapon", this);
            Debug.Log("attempting to update weapon");
        }
    }

    
}