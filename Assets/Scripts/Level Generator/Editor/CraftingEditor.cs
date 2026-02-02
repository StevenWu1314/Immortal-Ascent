using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Recipe))]
public class RecipeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        Recipe recipe = (Recipe)target;

        if (GUILayout.Button("Export"))
        {
            recipe.ExportRecipe();
        }
        if (GUILayout.Button("Import"))
        {
            recipe.ImportRecipe();
        }
    }
}