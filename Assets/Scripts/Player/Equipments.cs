using System;
using UnityEngine;

public class Equipments : MonoBehaviour
{
    [SerializeField] private GameObject inventoryContainer;
    [SerializeField] public Weapon meleeWeapon;
    [SerializeField] public Weapon rangeWeapon;
    [SerializeField] public Armor Armor;
    [SerializeField] private GameObject ItemContainer;
    [SerializeField] private ItemDisplay itemDisplay;

    public static Equipments Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    
    public void updateEquipment(String type, Item itemToEquip)
    {
        switch (type)
        {
            case "Melee Weapon":
                //if (meleeWeapon != null) meleeWeapon.equipedIcon.SetActive(false);
                meleeWeapon = (Weapon)itemToEquip;
                Debug.Log(meleeWeapon.getName());
                break;
            case "Range Weapon":
                //if (rangeWeapon != null) rangeWeapon.equipedIcon.SetActive(false);
                rangeWeapon = (Weapon)itemToEquip;
                break;
            case "Armor":
                if (Armor != null)
                {
                    Manager.player.decreaseMaxHealth(Armor.getHealth());
                }
                Armor = (Armor)itemToEquip;
                PlayerStats.Instance.increaseMaxHealth(Armor.getHealth());
                break;
        }
    }

}
