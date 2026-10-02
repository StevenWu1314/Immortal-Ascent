using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Step 4 of the item migration: <c>Equipments.meleeWeapon/rangeWeapon/armor</c> changed from an
/// <see cref="ItemDefinition"/> reference to an <see cref="EquipmentInstance"/>. Unity cannot map
/// the old serialized object reference into a class field, so without this the player silently
/// starts with nothing equipped.
///
/// The orphaned reference is still present in the prefab's YAML (Unity only drops unknown fields
/// when it next re-saves), so it is read back from there and written through SerializedObject.
/// This is a read of the file, not a hand edit: every write still goes through Unity's API.
/// </summary>
public static class InscriptionMigration
{
    private const string PlayerPrefab = "Assets/Prefabs/Player.prefab";

    /// <summary>
    /// Old serialized slots, in the order they appear. The old field was capitalised "Armor";
    /// the new one is lowercase.
    /// </summary>
    private static readonly (string oldName, string newName)[] Slots =
    {
        ("meleeWeapon", "meleeWeapon"),
        ("rangeWeapon", "rangeWeapon"),
        ("Armor", "armor")
    };

    [MenuItem("Tools/Items/4 - Migrate Equipments To Instances")]
    public static void MigrateEquipments()
    {
        if (!File.Exists(PlayerPrefab))
        {
            Debug.LogError($"[INS-MIGRATE] {PlayerPrefab} not found.");
            return;
        }

        string text = File.ReadAllText(PlayerPrefab);

        GameObject contents = PrefabUtility.LoadPrefabContents(PlayerPrefab);
        if (contents == null)
        {
            Debug.LogError($"[INS-MIGRATE] could not load {PlayerPrefab}.");
            return;
        }

        int applied = 0;
        try
        {
            foreach (Component component in contents.GetComponentsInChildren<Component>(true))
            {
                if (component == null || component.GetType().Name != "Equipments")
                {
                    continue;
                }

                var so = new SerializedObject(component);

                foreach (var (oldName, newName) in Slots)
                {
                    SerializedProperty slot = so.FindProperty(newName);
                    if (slot == null)
                    {
                        Debug.LogWarning($"[INS-MIGRATE] Equipments has no property '{newName}'.");
                        continue;
                    }

                    SerializedProperty definition = slot.FindPropertyRelative("definition");
                    if (definition == null)
                    {
                        Debug.LogError($"[INS-MIGRATE] '{newName}' has no nested 'definition' field.");
                        continue;
                    }

                    // Already migrated on a previous run.
                    if (definition.objectReferenceValue != null)
                    {
                        Debug.Log($"[INS-MIGRATE] {newName} already points at " +
                                  $"'{definition.objectReferenceValue.name}'; leaving it alone.");
                        continue;
                    }

                    string guid = ExtractOrphanedGuid(text, oldName);
                    if (string.IsNullOrEmpty(guid))
                    {
                        Debug.Log($"[INS-MIGRATE] {newName}: no orphaned reference found (slot was empty).");
                        continue;
                    }

                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    ItemDefinition resolved = AssetDatabase.LoadAssetAtPath<ItemDefinition>(assetPath);
                    if (resolved == null)
                    {
                        Debug.LogWarning($"[INS-MIGRATE] {newName}: guid {guid} resolved to " +
                                         $"'{assetPath}', which is not an ItemDefinition.");
                        continue;
                    }

                    definition.objectReferenceValue = resolved;
                    applied++;
                    Debug.Log($"[INS-MIGRATE] {newName} -> {resolved.DisplayName} ({assetPath})");
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(contents, PlayerPrefab);
        }
        catch (Exception e)
        {
            Debug.LogError($"[INS-MIGRATE] failed: {e.Message}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[INS-MIGRATE] applied {applied} equipment reference(s) to {PlayerPrefab}.");
    }

    /// <summary>
    /// Pull the guid out of an orphaned line such as
    /// <c>meleeWeapon: {fileID: 11400000, guid: 039af1c7..., type: 2}</c>.
    /// </summary>
    private static string ExtractOrphanedGuid(string prefabText, string fieldName)
    {
        Match match = Regex.Match(prefabText,
            @"^\s*" + Regex.Escape(fieldName) + @":\s*\{fileID:\s*\d+,\s*guid:\s*([0-9a-fA-F]{32})",
            RegexOptions.Multiline);

        return match.Success ? match.Groups[1].Value : null;
    }
}
