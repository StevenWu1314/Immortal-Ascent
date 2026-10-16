using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Authors the M1 content assets the run reads from Resources. Run once; every item is skipped when it
/// already exists, so re-running is safe.
/// </summary>
public static class CreateContentMenu
{
    private const string DropTableFolder = "Assets/Resources/DropTables";

    [MenuItem("Tools/Content/Create Material Drop Tables")]
    public static void CreateDropTables()
    {
        // GDD 13.7 / 12.2. Percentages are the M1 starting values and live in the assets afterwards.
        Create("Pig_yao", new[]
        {
            new DropSpec("PigYaoTusk", "Pig Yao Tusk", 30, 1, 1),
            new DropSpec("Pig Yao Skin", null, 30, 1, 1)
        });
        Create("Spider_yao", new[]
        {
            new DropSpec("Web", "Spider Silk", 40, 1, 2)
        });
        Create("Jing_yao", new[]
        {
            new DropSpec("Web", "Spider Silk", 15, 1, 1),
            new DropSpec("Iron", "Iron Ore", 20, 1, 2)
        });
    }

    private struct DropSpec
    {
        public readonly string AssetName;
        public readonly string DisplayName;
        public readonly int Chance;
        public readonly int Min;
        public readonly int Max;

        public DropSpec(string assetName, string displayName, int chance, int min, int max)
        {
            AssetName = assetName;
            DisplayName = displayName;
            Chance = chance;
            Min = min;
            Max = max;
        }
    }

    private static void Create(string enemyName, DropSpec[] specs)
    {
        string path = $"{DropTableFolder}/{enemyName}.asset";
        if (AssetDatabase.LoadAssetAtPath<MaterialDropTable>(path) != null)
        {
            Debug.Log($"[CONTENT] {path} already exists; nothing to do.");
            return;
        }

        System.IO.Directory.CreateDirectory(DropTableFolder);

        MaterialDropTable table = ScriptableObject.CreateInstance<MaterialDropTable>();
        table.enemyName = enemyName;

        List<string> missing = new List<string>();
        foreach (DropSpec spec in specs)
        {
            ItemDefinition material = FindMaterial(spec);
            if (material == null)
            {
                missing.Add(spec.AssetName);
                continue;
            }
            table.drops.Add(new MaterialDropTable.Drop
            {
                material = material,
                chancePercent = spec.Chance,
                minAmount = spec.Min,
                maxAmount = spec.Max
            });
        }

        AssetDatabase.CreateAsset(table, path);
        AssetDatabase.SaveAssets();

        if (missing.Count > 0)
        {
            Debug.LogWarning($"[CONTENT] created {path}, but these materials were not found and are missing " +
                             $"from the table: {string.Join(", ", missing)}. Assign them by hand.");
        }
        else
        {
            Debug.Log($"[CONTENT] created {path}.");
        }
    }

    /// <summary>
    /// Find a material by asset name, then by display name, so a renamed asset still resolves. Materials live
    /// under Resources/ItemDefinitions/Materials.
    /// </summary>
    private static ItemDefinition FindMaterial(DropSpec spec)
    {
        ItemDefinition byPath = Resources.Load<ItemDefinition>($"ItemDefinitions/Materials/{spec.AssetName}");
        if (byPath != null)
        {
            return byPath;
        }

        foreach (ItemDefinition candidate in Resources.LoadAll<ItemDefinition>("ItemDefinitions/Materials"))
        {
            if (candidate.name == spec.AssetName ||
                (spec.DisplayName != null && candidate.DisplayName == spec.DisplayName))
            {
                return candidate;
            }
        }
        return null;
    }

    [MenuItem("Tools/Content/Set Plains Realm Metal")]
    public static void SetPlainsMetal()
    {
        RealmDefinition plains = AssetDatabase.LoadAssetAtPath<RealmDefinition>("Assets/Resources/Realms/Plains.asset");
        if (plains == null)
        {
            Debug.LogWarning("[CONTENT] Resources/Realms/Plains.asset does not exist yet. " +
                             "Run Tools/Content/Create Plains Realm Asset first.");
            return;
        }

        ItemDefinition iron = FindMaterial(new DropSpec("Iron", "Iron Ore", 0, 1, 1));
        if (iron == null)
        {
            Debug.LogWarning("[CONTENT] could not find the Iron material under ItemDefinitions/Materials.");
            return;
        }

        plains.metal = iron;
        EditorUtility.SetDirty(plains);
        AssetDatabase.SaveAssets();
        Debug.Log($"[CONTENT] {plains.name} metal set to {iron.DisplayName}.");
    }
}
