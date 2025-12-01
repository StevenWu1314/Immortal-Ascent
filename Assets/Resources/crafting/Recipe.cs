using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using System;
using UnityEditor.Search;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "Recipe", menuName = "ScriptableObjects/Recipe", order = 2)]
public class Recipe : ScriptableObject
{
    [System.Serializable]
    public struct Pair
    {
        public Item material;
        public int amount;
    }

    public string resultName;
    public GameObject    result;
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
            sb.AppendLine($"{pair.material.getName()}: {pair.amount}");
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
                    result = FindItemByName(resultName).gameObject;

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

                    // Find material in your assets
                    Item mat = FindItemByName(matName);
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

    private Item FindItemByName(string matName)
    {
        // Fast editor-safe search:
#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:Prefab");
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                Item item = prefab.GetComponent<Item>();
                if (item != null && item.getName() == matName)
                    return item;
            }
        }
#endif
        return null;
    }

    public bool canCraft()
    {
        int Count = 0;
        List<Item> items = Inventory.Instance.getItems();
        foreach (Pair material in recipe)
        {
            foreach (Item item in items)
            {
                if (item.getName() == material.material.getName() && item.getAmount() >= material.amount)
                {
                    Count++;
                    Debug.Log($"Material found: {material.material.getName()}");
                }
                else
                {
                    Debug.Log($"Missing material: {material.material.getName()}");
                }
            }
        }
        return Count == recipe.Count;
    }

    public void craft()
    {
        List<Item> items = Inventory.Instance.getItems();
        int Count = 0;
        foreach (Pair material in recipe)
        {
            foreach (Item item in items)
            {
                if (item.getName() == material.material.getName() && item.getAmount() >= material.amount)
                {
                    Inventory.Instance.removeItem(item, material.amount);
                    Count++;
                    Debug.Log($"Material consumed: {material.material.getName()}");
                }
                else
                {
                    Debug.Log($"Missing material: {material.material.getName()}");
                }
            }
        }
        if (Count == recipe.Count)
        {
            Inventory.Instance.addItem(FindItemByName(resultName));
        }
    }
}

