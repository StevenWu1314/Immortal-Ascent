using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "Recipe", menuName = "ScriptableObjects/Recipe", order = 2)]
public class Recipe : ScriptableObject
{
    [System.Serializable]
    public struct Pair
    {
        public ItemDefinition material;
        public int amount;
    }

    public string resultName;
    public ItemDefinition result;
    public Sprite sprite;
    public List<Pair> recipe = new List<Pair>();

    private static string filePath = "Assets/Resources/crafting/Recipies.txt";

    // --------------------
    // EXPORT
    // --------------------
    public void ExportRecipe()
    {
        StringBuilder sb = new StringBuilder();

        if (File.Exists(filePath))
            sb.Append(File.ReadAllText(filePath));

        string content = sb.ToString();
        string startTag = "=== Recipe Start ===";
        string endTag = "=== Recipe End ===";
        int startIndex = content.IndexOf($"ResultName: {resultName}");

        // --- Remove existing recipe if it exists ---
        if (startIndex != -1)
        {
            int recipeStart = content.LastIndexOf(startTag, startIndex);
            int recipeEnd = content.IndexOf(endTag, startIndex);
            if (recipeEnd != -1)
                recipeEnd += endTag.Length;

            // Remove the block and any trailing newlines around it
            sb.Remove(recipeStart, recipeEnd - recipeStart);
            content = sb.ToString().TrimEnd(); // <== normalize spacing
            sb = new StringBuilder(content);
        }

        // --- Ensure there is exactly one newline before appending ---
        if (sb.Length > 0 && !sb.ToString().EndsWith("\n"))
            sb.AppendLine();

        // --- Append new recipe block ---
        sb.AppendLine("=== Recipe Start ===");
        sb.AppendLine($"ResultName: {resultName}");
        foreach (var pair in recipe)
        {
            if (pair.material == null)
            {
                Debug.LogError($"Recipe '{resultName}': skipping an empty material entry while exporting.");
                continue;
            }
            sb.AppendLine($"{pair.material.DisplayName}: {pair.amount}");
        }
        sb.AppendLine("=== Recipe End ===");

        File.WriteAllText(filePath, sb.ToString());
        Debug.Log($"Recipe for {resultName} exported!");
    }


    // --------------------
    // IMPORT
    // --------------------
    public void ImportRecipe()
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("Recipes.txt not found!");
            return;
        }

        string[] lines = File.ReadAllLines(filePath);
        bool inTargetRecipe = false;

        recipe.Clear();

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();

            if (line == "=== Recipe Start ===")
            {
                inTargetRecipe = false;
                continue;
            }

            if (line.StartsWith("ResultName:"))
            {
                string name = line.Substring("ResultName:".Length).Trim();
                inTargetRecipe = (name == resultName);
                continue;
            }

            if (line == "=== Recipe End ===")
            {
                if (inTargetRecipe)
                {
                    Debug.Log($"Loaded recipe for {resultName}");
                    ItemDefinition imported = FindDefinitionByName(resultName);
                    if (imported == null)
                    {
                        Debug.LogError($"ImportRecipe: no ItemDefinition named '{resultName}' was found; " +
                                       "'result' was left unchanged.");
                        return;
                    }
                    result = imported;

                    #if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(this);
                    UnityEditor.AssetDatabase.SaveAssets();
                    UnityEditor.AssetDatabase.Refresh();
                    #endif

                    return;
                }
                continue;
            }

            if (inTargetRecipe && !string.IsNullOrEmpty(line))
            {
                // Parse lines like "IronIngot: 3"
                string[] parts = line.Split(':');
                if (parts.Length == 2)
                {
                    string matName = parts[0].Trim();
                    int amount = int.Parse(parts[1].Trim());

                    ItemDefinition mat = FindDefinitionByName(matName);
                    if (mat != null)
                    {
                        recipe.Add(new Pair { material = mat, amount = amount });
                    }
                    else
                    {
                        Debug.LogWarning($"Could not find item '{matName}' for recipe '{resultName}'");
                    }
                }
            }
        }

        Debug.LogWarning($"Recipe for {resultName} not found in file!");
    }

    /// <summary>
    /// Editor-only lookup of an ItemDefinition asset by its display name. Player builds never
    /// call this: craft() uses the serialized <see cref="result"/> reference instead, which is
    /// what stopped crafting from consuming materials and then producing nothing in a build.
    /// </summary>
    private ItemDefinition FindDefinitionByName(string itemName)
    {
#if UNITY_EDITOR
        foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:ItemDefinition"))
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            ItemDefinition definition = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (definition != null && definition.DisplayName == itemName)
            {
                return definition;
            }
        }
#endif
        return null;
    }

    public bool canCraft()
    {
        // Quiet: LoadAvailableRecipies() calls this once per recipe, so logging every
        // missing material here would spam the console.
        return TryResolveMaterials(out _, false);
    }

    public void craft()
    {
        if (Inventory.Instance == null)
        {
            Debug.LogError($"Recipe '{name}': no Inventory instance; nothing was crafted.");
            return;
        }

        // Resolve the output BEFORE touching the inventory. If it cannot be resolved,
        // consuming materials would destroy them for no product.
        if (result == null)
        {
            Debug.LogError($"Recipe '{name}': no result ItemDefinition assigned; refusing to craft.");
            return;
        }

        // Validate the whole recipe first so a partially available one is never
        // half-consumed.
        if (!TryResolveMaterials(out List<(ItemDefinition material, int amount)> toRemove, true))
        {
            return;
        }

        // A full inventory must be detected BEFORE removing anything, otherwise the materials
        // are consumed and the crafted item is dropped on the floor (the old craft() bug).
        if (!Inventory.Instance.hasRoomFor(result, 1))
        {
            Debug.LogWarning($"Recipe '{name}': inventory has no room for {result.DisplayName}; " +
                             "materials were not consumed.");
            return;
        }

        foreach (var (material, amount) in toRemove)
        {
            Inventory.Instance.removeItem(material, amount);
        }

        Inventory.Instance.addItem(result, 1);
        Debug.Log($"Crafted: {result.DisplayName}");
    }

    /// <summary>
    /// Work out which definitions satisfy this recipe without changing anything.
    /// Entries for the same material are summed first, so two lines asking for 5 Iron each
    /// require 10 in stock instead of passing two independent "has 5" checks and then
    /// under-consuming.
    /// </summary>
    private bool TryResolveMaterials(out List<(ItemDefinition material, int amount)> toRemove, bool logMissing)
    {
        toRemove = new List<(ItemDefinition, int)>();

        if (recipe == null || recipe.Count == 0)
        {
            if (logMissing)
            {
                Debug.LogWarning($"Recipe '{name}' has no materials defined; refusing to craft.");
            }
            return false;
        }

        Inventory inventory = Inventory.Instance;
        if (inventory == null)
        {
            return false;
        }

        Dictionary<ItemDefinition, int> demand = new Dictionary<ItemDefinition, int>();
        foreach (Pair material in recipe)
        {
            if (material.material == null)
            {
                Debug.LogError($"Recipe '{name}': a material entry has no ItemDefinition assigned.");
                return false;
            }
            demand.TryGetValue(material.material, out int total);
            demand[material.material] = total + material.amount;
        }

        foreach (var entry in demand)
        {
            if (inventory.countOf(entry.Key) < entry.Value)
            {
                if (logMissing)
                {
                    Debug.Log($"Missing material: {entry.Key.DisplayName} (need {entry.Value})");
                }
                return false;
            }
            toRemove.Add((entry.Key, entry.Value));
        }

        return true;
    }
}
