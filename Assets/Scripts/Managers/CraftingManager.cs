using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Crafting panel driver. Whether an item can be crafted is decided entirely by that item's own
/// <see cref="ItemDefinition"/> recipe, so this only lists the ones the player can afford now.
/// </summary>
public class CraftingManager : MonoBehaviour
{
    public GameObject CraftingPanel;
    public GameObject buttonTemplate;

    private readonly List<GameObject> buttons = new List<GameObject>();

    public void LoadAvailableRecipies()
    {
        // Rebuild instead of appending: the panel is opened repeatedly and used to stack buttons.
        removeButtons();

        foreach (ItemDefinition item in Resources.LoadAll<ItemDefinition>("ItemDefinitions"))
        {
            if (!item.IsCraftable || !item.CanCraft())
            {
                continue;
            }

            GameObject button = Instantiate(buttonTemplate, CraftingPanel.transform.Find("itemholder"));
            button.GetComponent<Image>().sprite = item.Icon;
            buttons.Add(button);
            button.GetComponent<Button>().onClick.AddListener(item.Craft);
        }
    }

    public void removeButtons()
    {
        foreach (GameObject button in buttons)
        {
            Destroy(button);
        }
        buttons.Clear();
    }
}
