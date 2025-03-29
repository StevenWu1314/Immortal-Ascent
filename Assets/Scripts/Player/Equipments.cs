using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting.ReorderableList;
using UnityEngine;
using UnityEngine.UI;

public class Equipments : MonoBehaviour
{
    [SerializeField] private GameObject inventoryContainer;
    [SerializeField] private Weapon meleeWeapon;
    [SerializeField] private Weapon rangeWeapon;
    [SerializeField] private Item Armor;
    [SerializeField] private GameObject ItemContainer;
    [SerializeField] private ItemDisplay itemDisplay;

    
    public void updateEquipment(String type, Item itemToEquip) {
        switch (type) {
            case "Melee Weapon":
                meleeWeapon = (Weapon) itemToEquip;
                break;
            case "Range Weapon":
                rangeWeapon = (Weapon) itemToEquip;
                break;
            case "Armor":
                break;
        }
    }

}
