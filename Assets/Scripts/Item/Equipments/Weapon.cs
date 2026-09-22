using UnityEngine;
using Unity;
public class Weapon : Item
{
    [SerializeField] int damage;
    [SerializeField] public GameObject equipedIcon;
    [SerializeField] bool ranged;
    [SerializeField] int range;
    public Weapon(int damage, bool ranged, string name, int id, string description, Sprite sprite) {
        this.damage = damage;
        this.ranged = ranged;
        this.ItemName = name;
        this.id = id;
        this.description = description;
        stackLimit = 1;
        amount = 1;
        this.image = sprite;
    }
    public Weapon(int damage, int range, bool ranged, string name, int id, string description, Sprite sprite) {
        this.damage = damage;
        this.range = range;
        this.ranged = ranged;
        this.ItemName = name;
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
        itemDisplay.setFields(ItemName, description, image, "Damage", damage, 1, this);
    }
    public override void onUse()
    {
       // equipedIcon.SetActive(!equipedIcon.activeInHierarchy);
        if(ranged)
        {
            Equipments.Instance.updateEquipment("Range Weapon", this);
            PlayerStats.Instance.setRange(range);
        }
        else
        {
            Equipments.Instance.updateEquipment("Melee Weapon", this);
            PlayerStats.Instance.setRange(range);
        }
    }

    
}