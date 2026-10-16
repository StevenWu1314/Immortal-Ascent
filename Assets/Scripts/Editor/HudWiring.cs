using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// One-click scene setup for the T08 additions, and an honest report of what still needs an artist's or
/// designer's hand (tiles and text references).
///
/// It creates the structure - a Minimap object with its own Grid and Tilemap, and a HudReadout on the HUD
/// canvas - then lists every field it could not fill in, by name.
/// </summary>
public static class HudWiring
{
    private const string MinimapObjectName = "Minimap";

    [MenuItem("Tools/Content/Wire HUD References")]
    public static void Wire()
    {
        EnsureMinimap();

        int assigned = 0;
        int missing = 0;
        string[] textFields = { "ammoText", "weaponText", "turnText", "realmText", "rootText" };

        HudReadout readout = Object.FindFirstObjectByType<HudReadout>();
        if (readout == null)
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[HUD] no Canvas in this scene; created the minimap only. " +
                                 "Add a Canvas (or run this in the scene that has the HUD) and run it again.");
                return;
            }
            readout = canvas.gameObject.AddComponent<HudReadout>();
            Debug.Log($"[HUD] added HudReadout to '{canvas.name}'.");
        }

        SerializedObject serialized = new SerializedObject(readout);
        foreach (string fieldName in textFields)
        {
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                continue;
            }
            if (property.objectReferenceValue != null)
            {
                assigned++;
                continue;
            }

            // TMP text objects are matched by name, so the designer can create them with these names.
            GameObject found = GameObject.Find(fieldName);
            if (found != null && found.TryGetComponent(out TMPro.TMP_Text text))
            {
                property.objectReferenceValue = text;
                assigned++;
            }
            else
            {
                missing++;
            }
        }
        serialized.ApplyModifiedProperties();

        Debug.Log($"[HUD] wired {assigned} reference(s). Still to assign by hand: " +
                  $"{missing} text field(s) named {string.Join(", ", textFields)} (or create GameObjects with " +
                  "those names and re-run this menu item), plus the Minimap's visited/marker tiles.");
    }

    private static void EnsureMinimap()
    {
        Minimap minimap = Object.FindFirstObjectByType<Minimap>();
        if (minimap == null)
        {
            GameObject root = new GameObject(MinimapObjectName);
            GameObject gridObject = new GameObject("Grid");

            // A Grid parent is what a Tilemap needs to resolve cells; the minimap draws world-cell-aligned tiles.
            gridObject.transform.SetParent(root.transform);
            gridObject.AddComponent<UnityEngine.Grid>();
            GameObject tilemapObject = new GameObject("Tilemap");
            tilemapObject.transform.SetParent(gridObject.transform);
            tilemapObject.AddComponent<Tilemap>();
            tilemapObject.AddComponent<TilemapRenderer>();
            minimap = root.AddComponent<Minimap>();
            Debug.Log($"[HUD] created '{MinimapObjectName}' with a Grid and a Tilemap. " +
                      "Assign its visited and marker tiles, then scale it down for the corner of the screen.");
        }

        SerializedObject serialized = new SerializedObject(minimap);
        SerializedProperty mapProperty = serialized.FindProperty("map");
        if (mapProperty != null && mapProperty.objectReferenceValue == null)
        {
            Tilemap found = minimap.GetComponentInChildren<Tilemap>();
            if (found != null)
            {
                mapProperty.objectReferenceValue = found;
            }
        }
        serialized.ApplyModifiedProperties();
    }
}
