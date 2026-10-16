using UnityEditor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the T08 HUD in the open scene: a readout column (realm/floor, whose turn, weapon, ammo, root status)
/// and the corner minimap, with every reference assigned.
///
/// It creates what is missing and reuses what is already there, so it is safe to run again after moving things
/// around. Positions are plain placeholders - nudge them once you can see them.
/// </summary>
public static class HudWiring
{
    private const string ReadoutName = "HudReadout";
    private const string MinimapName = "Minimap";
    private const float RowHeight = 30f;
    private const float LeftPadding = 16f;

    [MenuItem("Tools/Content/Wire HUD References")]
    public static void Wire()
    {
        Canvas canvas = FindHudCanvas();
        if (canvas == null)
        {
            Debug.LogWarning("[HUD] no Canvas in the open scene. Open the scene that has the HUD and run this again.");
            return;
        }

        HudReadout readout = Object.FindFirstObjectByType<HudReadout>(FindObjectsInactive.Include);
        if (readout == null)
        {
            readout = canvas.gameObject.AddComponent<HudReadout>();
        }

        RectTransform column = EnsureChild(canvas.transform, ReadoutName);
        AnchorTopLeft(column);
        column.sizeDelta = new Vector2(420f, 5f * RowHeight);

        SerializedObject serialized = new SerializedObject(readout);
        Assign(serialized, "realmText", EnsureText(column, "realmText", "Plains - Floor 1", 0));
        Assign(serialized, "turnText", EnsureText(column, "turnText", "Your turn", 1));
        Assign(serialized, "weaponText", EnsureText(column, "weaponText", "Melee: none", 2));
        Assign(serialized, "ammoText", EnsureText(column, "ammoText", "Arrows 0", 3));

        RectTransform rootRow = EnsureChild(column, "rootRow");
        AnchorTopLeft(rootRow);
        rootRow.anchoredPosition = new Vector2(0f, -(4f * RowHeight));
        rootRow.sizeDelta = new Vector2(420f, 22f);
        Assign(serialized, "rootIcon", EnsureIcon(rootRow));
        Assign(serialized, "rootText", EnsureText(rootRow, "rootText", string.Empty, 0, 240f));
        serialized.ApplyModifiedProperties();

        RawImage map = EnsureMinimapImage(canvas.transform);
        Minimap minimap = Object.FindFirstObjectByType<Minimap>(FindObjectsInactive.Include);
        if (minimap == null)
        {
            minimap = new GameObject(MinimapName).AddComponent<Minimap>();
        }

        SerializedObject minimapSerialized = new SerializedObject(minimap);
        SerializedProperty display = minimapSerialized.FindProperty("display");
        if (display != null)
        {
            display.objectReferenceValue = map;
        }
        minimapSerialized.ApplyModifiedProperties();

        EditorUtility.SetDirty(readout);
        EditorUtility.SetDirty(minimap);
        Debug.Log("[HUD] wired " + ReadoutName + " and " + MinimapName + " under '" + canvas.name + "'. " +
                  "The rows are piled at the top-left as placeholders; move them where they read best and save " +
                  "the scene. The minimap texture only fills while the game is running. If " + MinimapName +
                  " still carries a Grid/Tilemap child from the earlier wiring, it is unused now.");
    }

    private static Canvas FindHudCanvas()
    {
        // A screen-space canvas with a scaler is the HUD; the others are panels.
        foreach (Canvas candidate in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (candidate.isActiveAndEnabled && candidate.renderMode == RenderMode.ScreenSpaceOverlay &&
                candidate.GetComponent<CanvasScaler>() != null)
            {
                return candidate;
            }
        }
        return Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
    }

    private static void Assign(SerializedObject serialized, string field, Object value)
    {
        SerializedProperty property = serialized.FindProperty(field);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
        else
        {
            Debug.LogWarning("[HUD] HudReadout has no '" + field + "' field to assign.");
        }
    }

    private static RectTransform EnsureChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            return existing as RectTransform != null ? (RectTransform)existing : existing.gameObject.AddComponent<RectTransform>();
        }

        var created = new GameObject(name, typeof(RectTransform));
        created.transform.SetParent(parent, false);
        return created.GetComponent<RectTransform>();
    }

    private static void AnchorTopLeft(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
    }

    private static TextMeshProUGUI EnsureText(Transform parent, string name, string initial, int row, float width = 420f)
    {
        Transform existing = parent.Find(name);
        TextMeshProUGUI text;
        if (existing != null && existing.TryGetComponent(out text))
        {
            return text;
        }

        var created = new GameObject(name, typeof(RectTransform));
        created.transform.SetParent(parent, false);
        text = created.AddComponent<TextMeshProUGUI>();
        text.text = initial;
        text.fontSize = 22f;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;
        if (text.font == null && TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }

        RectTransform rect = text.rectTransform;
        AnchorTopLeft(rect);
        rect.anchoredPosition = new Vector2(LeftPadding, -(row * RowHeight));
        rect.sizeDelta = new Vector2(width, RowHeight - 4f);
        return text;
    }

    private static Image EnsureIcon(Transform parent)
    {
        Transform existing = parent.Find("rootIcon");
        Image found;
        if (existing != null && existing.TryGetComponent(out found))
        {
            return found;
        }

        var created = new GameObject("rootIcon", typeof(RectTransform));
        created.transform.SetParent(parent, false);
        Image icon = created.AddComponent<Image>();
        icon.color = new Color(0.55f, 0.85f, 0.45f, 1f);   // placeholder colour; assign art later
        icon.raycastTarget = false;

        RectTransform rect = icon.rectTransform;
        AnchorTopLeft(rect);
        rect.sizeDelta = new Vector2(20f, 20f);
        rect.anchoredPosition = new Vector2(LeftPadding, -1f);
        return icon;
    }

    private static RawImage EnsureMinimapImage(Transform canvas)
    {
        Transform existing = canvas.Find(MinimapName + "Image");
        RawImage found;
        if (existing != null && existing.TryGetComponent(out found))
        {
            return found;
        }

        var created = new GameObject(MinimapName + "Image", typeof(RectTransform));
        created.transform.SetParent(canvas, false);
        RawImage image = created.AddComponent<RawImage>();
        image.raycastTarget = false;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-16f, -16f);
        rect.sizeDelta = new Vector2(180f, 180f);
        return image;
    }
}
