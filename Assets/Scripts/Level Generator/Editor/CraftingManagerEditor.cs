using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CraftingManager))]
public class CraftingManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        CraftingManager manager = (CraftingManager)target;

        if (GUILayout.Button("Export"))
        {
            manager.ExportAllRecipies();
        }
        if (GUILayout.Button("Import"))
        {
            manager.ImportAllRecipies();
        }
    }
}
