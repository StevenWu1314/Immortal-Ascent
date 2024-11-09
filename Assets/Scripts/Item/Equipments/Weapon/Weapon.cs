using Microsoft.Unity.VisualStudio.Editor;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;


public class Weapon : Item
{
    [SerializeField] int damage;
    [SerializeField] GameObject equipedIcon;
    public Weapon(int damage, string name, int id, string description, Sprite sprite) {
    this.damage = damage;
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
        itemDisplay.setFields(name, description, image, "Damage", damage);
    }
    public override void onUse()
    {
        equipedIcon.SetActive(!equipedIcon.activeInHierarchy);
    }

    
}