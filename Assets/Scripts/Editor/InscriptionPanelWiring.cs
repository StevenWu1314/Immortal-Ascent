using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Points the inscription panel's board at the art the designer picked (AGENTS.md: the scene owns UI, a
/// [MenuItem] performs the wiring).
/// </summary>
public static class InscriptionPanelWiring
{
    private const string ArtPath = "Assets/Sprites/Inscryption page.png";

    [MenuItem("Tools/Content/Assign Inscription Page Art")]
    public static void AssignBoardArt()
    {
        InscriptionPanel panel = Object.FindFirstObjectByType<InscriptionPanel>(FindObjectsInactive.Include);
        if (panel == null)
        {
            Debug.LogWarning("[UI] no InscriptionPanel in the open scene.");
            return;
        }

        Sprite art = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath);
        if (art == null)
        {
            Debug.LogWarning($"[UI] '{ArtPath}' is not imported as a Sprite. Select it and set Texture Type = " +
                             "Sprite (2D and UI), then run this again.");
            return;
        }

        // The board is the window's own Image, or the first Image under it.
        SerializedObject serialized = new SerializedObject(panel);
        SerializedProperty boardProperty = serialized.FindProperty("board");
        if (boardProperty == null)
        {
            Debug.LogWarning("[UI] InscriptionPanel has no 'board' field. Add one (Image) and run this again.");
            return;
        }

        boardProperty.objectReferenceValue = boardProperty.objectReferenceValue != null
            ? boardProperty.objectReferenceValue
            : FindBoardImage(panel);
        if (boardProperty.objectReferenceValue == null)
        {
            Debug.LogWarning("[UI] could not find an Image on the panel to use as the board.");
            return;
        }

        serialized.ApplyModifiedProperties();

        SerializedObject boardSo = new SerializedObject(boardProperty.objectReferenceValue);
        SerializedProperty sprite = boardSo.FindProperty("m_Sprite");
        if (sprite != null)
        {
            sprite.objectReferenceValue = art;
            boardSo.ApplyModifiedProperties();
        }

        EditorUtility.SetDirty(panel);
        Debug.Log($"[UI] inscription board now uses '{ArtPath}'. Save the scene.");
    }

    private static Object FindBoardImage(InscriptionPanel panel)
    {
        Image own = panel.GetComponent<Image>();
        if (own != null)
        {
            return own;
        }
        return panel.GetComponentInChildren<Image>(true);
    }
}
