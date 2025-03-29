using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;



public class ItemBox : MonoBehaviour, IPointerClickHandler
{
    public GameObject itemBox;
    [SerializeField] Item item;


    void Start()
    {
    }


    public void setItem(Item item)
    {
        this.item = item;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            item.displaySelf();
            
        }
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            item.onUse();
        }
    }
}