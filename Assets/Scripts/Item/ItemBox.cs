using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;



public class ItemBox : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] Item item;
    private Image image;

    public void setItem(Item item)
    {
        if (image == null)
        {
            image = transform.Find("slot").transform.Find("Item").GetComponent<Image>();
        }
        this.item = item;
        // null must be handled before touching item, otherwise this throws.
        if (item == null)
        {
            image.sprite = null;
            image.color = Color.clear;
            return;
        }
        image.sprite = item.getSprite();
        image.color = Color.white;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (item != null)
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
        else
        {
            Debug.Log("No item");
        }
        
    }

    public void clearSlot()
    {
        this.item = null;
        if (image != null)
        {
            image.sprite = null;
            image.color = new Color(255, 255, 255, 0);
        }
        
    }
}