using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemDisplay : MonoBehaviour
{
    [SerializeField] TMP_Text itemName;
    [SerializeField] TMP_Text description;
    [SerializeField] Image icon;
    [SerializeField] TMP_Text typeValue;
    [SerializeField] TMP_Text amount;
    [SerializeField] Button button;

    /// <summary>
    /// Render an inventory stack. Takes the whole stack rather than a definition because
    /// equipment carries per-item state: the panel has to show the inscriptions on *this*
    /// sword, which a shared definition cannot tell us.
    /// </summary>
    public void show(ItemStack stack)
    {
        if (stack.IsEmpty || stack.definition == null)
        {
            Clear();
            return;
        }

        ItemDefinition definition = stack.definition;

        if (itemName != null) itemName.text = definition.DisplayName;
        if (this.description != null) this.description.text = definition.Description;
        if (icon != null) icon.sprite = definition.Icon;

        if (typeValue != null)
        {
            if (stack.instance != null)
            {
                typeValue.text = DescribeInscriptions(stack.instance);
            }
            else
            {
                string label = definition.DisplayLabel;
                typeValue.text = string.IsNullOrEmpty(label)
                    ? ""
                    : label + ": " + definition.DisplayValue;
            }
        }

        // amount is not assigned in every scene's UI copy (SampleScene never had it), so this
        // must not be dereferenced blindly.
        if (amount != null)
        {
            amount.text = stack.instance != null
                ? $"Inscriptions: {stack.instance.Inscriptions.Count}/{EquipmentInstance.MaxInscriptions}"
                : "Amount: " + stack.amount;
        }

        if (button != null)
        {
            // Copy the struct: closing over the parameter would still be fine here, but the
            // listener outlives this call and must not read a mutated stack.
            ItemStack captured = stack;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ItemUsage.Use(captured, Inventory.Instance));
        }
    }

    private static string DescribeInscriptions(EquipmentInstance instance)
    {
        if (instance == null || instance.Inscriptions.Count == 0)
        {
            return "No inscriptions";
        }

        var sb = new StringBuilder();
        for (int i = 0; i < EquipmentInstance.MaxInscriptions; i++)
        {
            if (i > 0)
            {
                sb.Append('\n');
            }
            ItemDefinition inscription = instance.GetInscriptionAt(i);
            sb.Append(inscription != null ? $"{i + 1}. {inscription.DisplayName}" : $"{i + 1}. (empty)");
        }
        return sb.ToString();
    }

    public void Clear()
    {
        if (itemName != null) itemName.text = "";
        if (description != null) description.text = "";
        if (icon != null) icon.sprite = null;
        if (typeValue != null) typeValue.text = "";
        if (amount != null) amount.text = "";
        if (button != null) button.onClick.RemoveAllListeners();
    }

    void OnEnable()
    {
        if (Inventory.Instance != null)
        {
            Inventory.Instance.setItemDisplay(this);
        }
    }
}
