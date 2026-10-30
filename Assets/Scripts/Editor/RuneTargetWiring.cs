using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Sets each rune's allowed equipment. The rule was always implemented (ItemDefinition.CanApplyTo and
/// InscriptionService filter with it) but the two shipped runes never had the field serialized, so they
/// defaulted to AnyEquipment and could be slotted anywhere.
///
/// The mapping is data: edit the table below or the assets directly once it has run once.
/// </summary>
public static class RuneTargetWiring
{
    private static readonly Dictionary<string, InscriptionTarget> Targets = new Dictionary<string, InscriptionTarget>
    {
        // Designer's correction to GDD 11.3: Sharp is melee only (Strength hones an edge, a bow uses Dexterity)
        // and Sturdy is armour only (it hardens the wearer).
        { "Sharp Rune", InscriptionTarget.MeleeWeapon },
        { "Sturdy Rune", InscriptionTarget.Armor },
        { "Deft Rune", InscriptionTarget.RangedWeapon },
        { "Warding Rune", InscriptionTarget.Armor }
    };

    [MenuItem("Tools/Content/Set Rune Targets")]
    public static void SetRuneTargets()
    {
        int changed = 0;
        var missing = new List<string>(Targets.Keys);

        foreach (ItemDefinition definition in Resources.LoadAll<ItemDefinition>("ItemDefinitions"))
        {
            if (definition.Kind != ItemKind.Inscription || !Targets.TryGetValue(definition.DisplayName, out InscriptionTarget target))
            {
                continue;
            }

            SerializedObject serialized = new SerializedObject(definition);
            SerializedProperty property = serialized.FindProperty("inscriptionTarget");
            if (property == null)
            {
                Debug.LogWarning($"[RUNES] '{definition.DisplayName}' has no inscriptionTarget field to set.");
                continue;
            }

            property.enumValueIndex = (int)target;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(definition);
            missing.Remove(definition.DisplayName);
            changed++;
            Debug.Log($"[RUNES] {definition.DisplayName} -> {target}");
        }

        AssetDatabase.SaveAssets();
        if (missing.Count > 0)
        {
            Debug.LogWarning("[RUNES] no matching rune asset for: " + string.Join(", ", missing) +
                             " (fine if those runes are not authored yet).");
        }
        Debug.Log($"[RUNES] set the target on {changed} rune(s). Runes already filtered correctly by " +
                  "InscriptionService - this only fixes the data.");
    }
}
