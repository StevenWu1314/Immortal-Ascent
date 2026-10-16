using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The crafting panel (T07, GDD 3.6/12.5). Out of combat only, every craftable definition listed in one of
/// three states - enabled, greyed with its reason, or locked because this run has not discovered it - and the
/// crafting itself goes through the pure <see cref="CraftingRules"/>, so this stays presentation.
/// </summary>
public class CraftingManager : MonoBehaviour
{
    public GameObject CraftingPanel;
    public GameObject buttonTemplate;

    private readonly List<GameObject> buttons = new List<GameObject>();

    private void OnEnable()
    {
        Controls.onMoveEvent += OnPlayerAction;
    }

    private void OnDisable()
    {
        Controls.onMoveEvent -= OnPlayerAction;
    }

    /// <summary>
    /// Rebuild the panel: every craftable definition appears, in whichever of the three states it is in.
    /// </summary>
    public void LoadAvailableRecipies()
    {
        // Rebuild instead of appending: the panel is opened repeatedly and used to stack buttons.
        removeButtons();

        if (!CraftingRules.IsCraftingAllowedHere())
        {
            // GDD 3.6: crafting is out of combat only.
            ClosePanel("Crafting is not possible in combat");
            return;
        }

        foreach (ItemDefinition item in Resources.LoadAll<ItemDefinition>("ItemDefinitions"))
        {
            if (!item.IsCraftable)
            {
                continue;
            }
            AddButton(item);
        }
    }

    private void AddButton(ItemDefinition item)
    {
        CraftState state = CraftingRules.StateOf(item.Recipe, RecipeDiscovery.IsRevealed(item),
            CountOf, HasRoomFor(item), out string detail);

        GameObject button = Instantiate(buttonTemplate, CraftingPanel.transform.Find("itemholder"));
        Image image = button.GetComponent<Image>();
        image.sprite = item.Icon;
        // Enabled keeps the template's colours; greyed dims, locked blacks out.
        image.color = state == CraftState.Enabled
            ? Color.white
            : state == CraftState.Greyed
                ? new Color(1f, 1f, 1f, 0.45f)
                : new Color(0.2f, 0.2f, 0.2f, 0.6f);

        Button click = button.GetComponent<Button>();
        click.interactable = state == CraftState.Enabled;
        if (state == CraftState.Enabled)
        {
            click.onClick.AddListener(() => Craft(item));
        }
        else
        {
            // A greyed or locked entry explains itself rather than doing nothing.
            click.onClick.AddListener(() => Explain(detail));
        }
        buttons.Add(button);
    }

    private void Craft(ItemDefinition item)
    {
        if (!CraftingRules.IsCraftingAllowedHere())
        {
            ClosePanel("Crafting is not possible in combat");
            return;
        }

        if (!CraftingRules.TryCraft(item, Inventory.Instance, out int arrows, out string failure))
        {
            Explain(failure);
            return;
        }

        Explain(arrows > 0 ? $"+{arrows} arrows" : $"Crafted {item.DisplayName}");
        LoadAvailableRecipies();
    }

    private static int CountOf(ItemDefinition material)
    {
        return Inventory.Instance != null ? Inventory.Instance.countOf(material) : 0;
    }

    private static bool HasRoomFor(ItemDefinition item)
    {
        // Ammo needs no slot: arrows are a count, not a stack (T06).
        return item.Kind == ItemKind.Ammo ||
               Inventory.Instance == null ||
               Inventory.Instance.hasRoomFor(item, 1);
    }

    private void Explain(string message)
    {
        if (string.IsNullOrEmpty(message) || UIManager.Instance == null)
        {
            return;
        }
        Vector3 at = PlayerStats.Instance != null ? PlayerStats.Instance.transform.position : transform.position;
        UIManager.Instance.DrawFlowupText(message, at);
    }

    private void ClosePanel(string reason)
    {
        if (CraftingPanel != null && CraftingPanel.activeSelf)
        {
            CraftingPanel.SetActive(false);
            Explain(reason);
        }
    }

    /// <summary>Entering combat closes the panel (GDD 3.6): crafting cannot happen mid-fight.</summary>
    private void OnPlayerAction(Controls controls)
    {
        if (CraftingPanel != null && CraftingPanel.activeSelf && !CraftingRules.IsCraftingAllowedHere())
        {
            ClosePanel("Crafting is not possible in combat");
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
