using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// One-shot setup for the phase-1 inscription vertical slice:
///   * builds the chest prefab from the chest Tile's sprite that TilePlacer already references,
///   * creates two sample inscriptions (one ItemStatBonus, one PlayerStatBonus),
///   * wires the prefab and the loot pool onto the room-first generator in the dungeon scene.
///
/// Everything goes through Unity's API. Re-running is safe: existing assets are reused.
/// </summary>
public static class ChestSetup
{
    private const string ScenePath = "Assets/Scenes/Branching dungeon.unity";
    private const string PrefabPath = "Assets/Prefabs/Chest.prefab";
    private const string InscriptionFolder = "Assets/ItemDefinitions/Inscriptions";

    [MenuItem("Tools/Items/5 - Set Up Chests And Sample Inscriptions")]
    public static void SetUp()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        TilePlacer placer = Object.FindFirstObjectByType<TilePlacer>();
        if (placer == null)
        {
            Debug.LogError("[CHEST-SETUP] no TilePlacer in the scene; cannot find the chest sprite.");
            return;
        }

        Sprite sprite = BuildChestPrefab(placer);

        List<ItemDefinition> inscriptions = new List<ItemDefinition>
        {
            CreateInscription("Sharp Rune", InscriptionEffect.ItemStatBonus, BonusStat.Strength, 5,
                InscriptionTarget.AnyEquipment,
                "A rune that hones the edge it is set into.")
        };

        // Create a StrPill-style example for the wearer branch as well.
        inscriptions.Add(CreateInscription("Sturdy Rune", InscriptionEffect.PlayerStatBonus,
            BonusStat.MaxHealth, 20, InscriptionTarget.AnyEquipment,
            "A rune that hardens whoever carries it."));

        WireGenerator(BuildChestPrefabAsset(), inscriptions);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[CHEST-SETUP] chest prefab sprite={(sprite != null ? sprite.name : "none")}, " +
                  $"{inscriptions.Count} inscription(s) created and wired.");
    }

    private static Sprite BuildChestPrefab(TilePlacer placer)
    {
        Sprite sprite = placer.chest != null ? placer.chest.sprite : null;
        if (sprite == null)
        {
            Debug.LogWarning("[CHEST-SETUP] TilePlacer.chest has no sprite; the chest prefab will be invisible.");
        }

        GameObject root = new GameObject("Chest");

        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;

        // Sit just above the floor tilemap so the chest is not hidden behind it. Tilemap has no
        // sorting properties of its own; they live on its Renderer.
        Tilemap floor = placer.floor;
        if (floor != null)
        {
            Renderer floorRenderer = floor.GetComponent<Renderer>();
            if (floorRenderer != null)
            {
                renderer.sortingLayerName = floorRenderer.sortingLayerName;
                renderer.sortingOrder = floorRenderer.sortingOrder + 1;
            }
        }

        // Chests are solid on the Grid, so this collider exists only for future physics queries
        // (a highlight, an interaction prompt); it must not block movement itself.
        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = Vector2.one;

        Chest chest = root.AddComponent<Chest>();
        var so = new SerializedObject(chest);
        SerializedProperty rendererProp = so.FindProperty("spriteRenderer");
        if (rendererProp != null)
        {
            rendererProp.objectReferenceValue = renderer;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        Debug.Log($"[CHEST-SETUP] wrote {PrefabPath}");
        return sprite;
    }

    private static GameObject BuildChestPrefabAsset()
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
    }

    private static ItemDefinition CreateInscription(string name, InscriptionEffect effect,
                                                    BonusStat stat, int amount,
                                                    InscriptionTarget target, string description)
    {
        EnsureFolder(InscriptionFolder);
        string path = $"{InscriptionFolder}/{name}.asset";

        ItemDefinition def = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<ItemDefinition>();
            AssetDatabase.CreateAsset(def, path);
        }

        var so = new SerializedObject(def);
        so.FindProperty("id").stringValue = name;
        so.FindProperty("displayName").stringValue = name;
        so.FindProperty("description").stringValue = description;
        so.FindProperty("stackLimit").intValue = 1;
        so.FindProperty("kind").enumValueIndex = (int)ItemKind.Inscription;
        so.FindProperty("inscriptionEffect").enumValueIndex = (int)effect;
        so.FindProperty("inscriptionStat").enumValueIndex = (int)stat;
        so.FindProperty("inscriptionAmount").intValue = amount;
        so.FindProperty("inscriptionTarget").enumValueIndex = (int)target;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(def);
        Debug.Log($"[CHEST-SETUP] {name} -> {effect} {stat} +{amount} ({path})");
        return def;
    }

    private static void WireGenerator(GameObject chestPrefab, List<ItemDefinition> inscriptions)
    {
        RoomFirstDungeonGenerator generator = Object.FindFirstObjectByType<RoomFirstDungeonGenerator>();
        if (generator == null)
        {
            Debug.LogError("[CHEST-SETUP] no RoomFirstDungeonGenerator in the scene; nothing was wired.");
            return;
        }

        var so = new SerializedObject(generator);

        SerializedProperty prefabProp = so.FindProperty("chestPrefab");
        if (prefabProp != null)
        {
            prefabProp.objectReferenceValue = chestPrefab;
        }

        SerializedProperty poolProp = so.FindProperty("chestLootPool");
        if (poolProp != null && poolProp.isArray)
        {
            poolProp.arraySize = inscriptions.Count;
            for (int i = 0; i < inscriptions.Count; i++)
            {
                poolProp.GetArrayElementAtIndex(i).objectReferenceValue = inscriptions[i];
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log($"[CHEST-SETUP] wired chestPrefab and {inscriptions.Count} pool entry(s) " +
                  "onto RoomFirstDungeonGenerator");
    }

    private static void EnsureFolder(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(folder);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                AssetDatabase.CreateFolder("Assets", "ItemDefinitions");
            }
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
