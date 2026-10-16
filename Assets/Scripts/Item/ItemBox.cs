using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemBox : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private ItemStack stack;
    private ItemDisplay itemDisplay;
    private Image image;

    /// <summary>
    /// Bind a slot to a stack. The ItemDisplay is handed in by Inventory rather than found by
    /// name, so a renamed UI object can no longer silently break the details panel.
    /// </summary>
    public void setItem(ItemStack stack, ItemDisplay display)
    {
        this.stack = stack;
        this.itemDisplay = display;
        EnsureImage();

        if (image == null)
        {
            return;
        }
        if (stack.IsEmpty || stack.definition == null)
        {
            image.sprite = null;
            image.color = Color.clear;
            return;
        }
        image.sprite = stack.definition.Icon;
        image.color = Color.white;
    }

    private void EnsureImage()
    {
        if (image != null)
        {
            return;
        }
        Transform slot = transform.Find("slot");
        Transform icon = slot != null ? slot.Find("Item") : null;
        if (icon != null)
        {
            image = icon.GetComponent<Image>();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (stack.IsEmpty || stack.definition == null)
        {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (itemDisplay != null)
            {
                itemDisplay.show(stack);
            }
            else
            {
                Debug.LogWarning("ItemBox: no ItemDisplay bound; cannot show item details.");
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            ItemUsage.Use(stack, Inventory.Instance);
            if (itemDisplay != null)
            {
                // The use may have consumed the last unit, so re-render the live count.
                itemDisplay.show(stack);
            }
        }
    }

    public void clearSlot()
    {
        stack = default;
        EnsureImage();
        if (image != null)
        {
            image.sprite = null;
            image.color = new Color(255, 255, 255, 0);
        }
    }
}
