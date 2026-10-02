using UnityEngine;

/// <summary>
/// A harvestable resource node. CollectableMap stores the PREFAB's component here rather than
/// a scene instance (see PerlinNoiseMap), so this class must never depend on per-instance
/// state: only the shared definition and dropRange are meaningful.
/// </summary>
public class Collectables : MonoBehaviour
{
    [SerializeField] private ItemDefinition definition;

    [Tooltip("Inclusive range of units granted per pickup (x = minimum, y = maximum).")]
    public Vector2 dropRange = new Vector2(1, 1);

    public ItemDefinition Definition => definition;

    /// <summary>
    /// Grant this node's item. addItem reports what did not fit, so a full inventory is now
    /// visible instead of silently swallowing the pickup.
    /// </summary>
    public void collectThis()
    {
        if (definition == null)
        {
            Debug.LogError($"Collectables on '{name}': no ItemDefinition assigned; nothing collected.");
            return;
        }

        int min = Mathf.Max(0, Mathf.RoundToInt(dropRange.x));
        int max = Mathf.Max(min, Mathf.RoundToInt(dropRange.y));
        int amount = Random.Range(min, max + 1);
        if (amount <= 0)
        {
            return;
        }

        Inventory inventory = Inventory.Instance;
        if (inventory == null)
        {
            Debug.LogWarning("Collectables: no Inventory instance; nothing collected.");
            return;
        }

        int leftover = inventory.addItem(definition, amount);
        if (leftover > 0)
        {
            string message = $"Inventory full ({leftover} x {definition.DisplayName} lost)";
            Debug.LogWarning("Collectables: " + message);
            // Positioned on the player: this component lives on a prefab asset, so its own
            // transform is not a meaningful place in the world.
            if (UIManager.Instance != null && PlayerStats.Instance != null)
            {
                UIManager.Instance.DrawFlowupText(message, PlayerStats.Instance.transform.position);
            }
        }
    }
}
