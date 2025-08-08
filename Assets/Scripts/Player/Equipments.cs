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
    [SerializeField] public Weapon meleeWeapon;
    [SerializeField] public Weapon rangeWeapon;
    [SerializeField] private Item Armor;
    [SerializeField] private GameObject ItemContainer;
    [SerializeField] private ItemDisplay itemDisplay;

    
    public void updateEquipment(String type, Weapon itemToEquip) {
        switch (type) {
            case "Melee Weapon":
                if (meleeWeapon != null) //meleeWeapon.equipedIcon.SetActive(false);
                meleeWeapon = itemToEquip;
                Debug.Log(meleeWeapon.getName());
                break;
            case "Range Weapon":
            if (rangeWeapon != null) //rangeWeapon.equipedIcon.SetActive(false);
                rangeWeapon = (Weapon) itemToEquip;
                break;
            case "Armor":
                break;
        }
    }

}
