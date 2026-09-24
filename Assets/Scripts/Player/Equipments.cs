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
        if (PlayerStats.Instance == null)
        {
            Debug.LogError("Equipments: PlayerStats.Instance is null; cannot equip.");
            return;
        }

        switch (type)
        {
            case "Melee Weapon":
                if (!(itemToEquip is Weapon melee))
                {
                    Debug.LogError($"Equipments: '{itemToEquip}' is not a Weapon; cannot equip as melee.");
                    return;
                }
                meleeWeapon = melee;
                break;
            case "Range Weapon":
                if (!(itemToEquip is Weapon ranged))
                {
                    Debug.LogError($"Equipments: '{itemToEquip}' is not a Weapon; cannot equip as ranged.");
                    return;
                }
                rangeWeapon = ranged;
                break;
            case "Armor":
                if (!(itemToEquip is Armor newArmor))
                {
                    Debug.LogError($"Equipments: '{itemToEquip}' is not an Armor; cannot equip.");
                    return;
                }
                // Remove the old armour's contribution with PlayerStats.Instance rather
                // than Manager.player, which was never assigned and threw on every swap.
                if (Armor != null)
                {
                    PlayerStats.Instance.decreaseMaxHealth(Armor.getHealth());
                }
                Armor = newArmor;
                PlayerStats.Instance.increaseMaxHealth(Armor.getHealth());
                break;
            default:
                Debug.LogWarning($"Equipments: unknown equipment type '{type}'");
                break;
        }
    }

}
