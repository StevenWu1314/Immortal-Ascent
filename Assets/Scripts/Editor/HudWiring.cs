using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the HUD in the open scene (AGENTS.md: UI lives in the scene, never built at runtime).
///
/// It creates a "HUD" root under the canvas with the readout rows, the two weapon slots and the corner
/// minimap, then assigns every reference. Run it once; afterwards the scene owns the layout and you can move,
/// restyle or delete any of it.
/// </summary>
public static class HudWiring
{
    private const float Margin = 16f;
    private const float Row = 26f;
    private const float Slot = 56f;
    private const float SlotGap = 10f;
    private const float SlotBase = Margin;
    private const float RowBase = Margin + Slot + SlotGap + 8f;

    [MenuItem("Tools/Content/Wire HUD References")]
    public static void Wire()
    {
        Canvas canvas = FindCanvas();
        if (canvas == null)
        {
            Debug.LogWarning("[HUD] no Canvas in the open scene; open the scene with the HUD and run this again.");
            return;
        }

        GameObject hud = Child(canvas.transform, "HUD");
        Stretch(hud.GetComponent<RectTransform>());

        HudReadout readout = Object.FindFirstObjectByType<HudReadout>();
        if (readout == null)
        {
            readout = canvas.gameObject.AddComponent<HudReadout>();
        }

        TextMeshProUGUI realm = Text(hud.transform, "HudRealm", "Plains - Floor 1", 0);
        TextMeshProUGUI turn = Text(hud.transform, "HudTurn", "Your turn", 3);
        TextMeshProUGUI rootText = Text(hud.transform, "HudRootText", string.Empty, 1, 240f);
        GameObject rootIcon = Icon(hud.transform);

        RectTransform slotRoot = Child(hud.transform, "WeaponSlots").GetComponent<RectTransform>();
        Stretch(slotRoot);
        Image meleeSlot = SlotImage(slotRoot, "MeleeSlot", 0f);
        Image meleeIcon = Icon(meleeSlot.transform, "Icon");
        Image bowSlot = SlotImage(slotRoot, "BowSlot", Slot + SlotGap);
        Image bowIcon = Icon(bowSlot.transform, "Icon");
        TextMeshProUGUI badge = Badge(bowSlot.transform);

        WeaponSlots slots = canvas.gameObject.GetComponent<WeaponSlots>();
        if (slots == null)
        {
            slots = canvas.gameObject.AddComponent<WeaponSlots>();
        }

        SerializedObject so = new SerializedObject(readout);
        Set(so, "realmText", realm);
        Set(so, "turnText", turn);
        Set(so, "rootText", rootText);
        Set(so, "rootIcon", rootIcon);
        Set(so, "hudRoot", hud);
        so.ApplyModifiedProperties();

        SerializedObject slotSo = new SerializedObject(slots);
        Set(slotSo, "meleeSlot", meleeSlot);
        Set(slotSo, "meleeIcon", meleeIcon);
        Set(slotSo, "rangedSlot", bowSlot);
        Set(slotSo, "rangedIcon", bowIcon);
        Set(slotSo, "ammoBadge", badge);
        slotSo.ApplyModifiedProperties();

        RawImage map = EnsureMinimapImage(hud.transform);
        Minimap minimap = Object.FindFirstObjectByType<Minimap>();
        if (minimap == null)
        {
            minimap = new GameObject("Minimap").AddComponent<Minimap>();
        }
        SerializedObject mapSo = new SerializedObject(minimap);
        Set(mapSo, "display", map);
        mapSo.ApplyModifiedProperties();

        EditorUtility.SetDirty(readout);
        EditorUtility.SetDirty(slots);
        EditorUtility.SetDirty(minimap);
        Debug.Log("[HUD] built under '" + canvas.name + "': a 'HUD' root with realm/turn/root rows, two weapon " +
                  "slots (MeleeSlot, BowSlot) and a corner minimap. Move, restyle or delete any of it - and if a " +
                  "duplicate 'Your turn' banner already exists, delete one. Save the scene.");
    }

    private static Canvas FindCanvas()
    {
        foreach (Canvas candidate in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (candidate.isActiveAndEnabled && candidate.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return candidate;
            }
        }
        return Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
    }

    private static GameObject Child(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            return existing.gameObject;
        }
        var created = new GameObject(name, typeof(RectTransform));
        created.transform.SetParent(parent, false);
        return created;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static TextMeshProUGUI Text(Transform parent, string name, string initial, int row, float width = 720f)
    {
        GameObject go = Child(parent, name);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = go.AddComponent<TextMeshProUGUI>();
        }
        text.text = initial;
        text.fontSize = 22f;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        if (text.font == null && TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }
        Place(text.rectTransform, Margin, RowBase + row * Row, width, Row - 4f);
        return text;
    }

    private static GameObject Icon(Transform parent)
    {
        GameObject go = Child(parent, "HudRootIcon");
        Image image = go.GetComponent<Image>();
        if (image == null)
        {
            image = go.AddComponent<Image>();
        }
        image.color = new Color(0.55f, 0.85f, 0.45f, 1f);
        image.raycastTarget = false;
        Place(image.rectTransform, Margin, RowBase + 2f * Row, 18f, 18f);
        return go;
    }

    private static Image Icon(Transform parent, string name)
    {
        GameObject go = Child(parent, name);
        Image image = go.GetComponent<Image>();
        if (image == null)
        {
            image = go.AddComponent<Image>();
        }
        image.raycastTarget = false;
        image.preserveAspect = true;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(4f, 4f);
        rect.offsetMax = new Vector2(-4f, -4f);
        return image;
    }

    private static Image SlotImage(Transform parent, string name, float offsetX)
    {
        GameObject go = Child(parent, name);
        Image image = go.GetComponent<Image>();
        if (image == null)
        {
            image = go.AddComponent<Image>();
        }
        image.raycastTarget = false;
        image.color = new Color(0.12f, 0.13f, 0.15f, 0.65f);
        Place(image.rectTransform, Margin + offsetX, SlotBase, Slot, Slot);
        return image;
    }

    private static TextMeshProUGUI Badge(Transform slot)
    {
        GameObject go = Child(slot, "AmmoBadge");
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = go.AddComponent<TextMeshProUGUI>();
        }
        text.fontSize = 18f;
        text.alignment = TextAlignmentOptions.TopRight;
        text.raycastTarget = false;
        if (text.font == null && TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(6f, 2f);
        rect.sizeDelta = new Vector2(44f, 22f);
        return text;
    }

    private static RawImage EnsureMinimapImage(Transform parent)
    {
        GameObject go = Child(parent, "MinimapImage");
        RawImage image = go.GetComponent<RawImage>();
        if (image == null)
        {
            image = go.AddComponent<RawImage>();
        }
        image.raycastTarget = false;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-16f, -16f);
        rect.sizeDelta = new Vector2(180f, 180f);
        return image;
    }

    private static void Set(SerializedObject serialized, string field, Object value)
    {
        SerializedProperty property = serialized.FindProperty(field);
        if (property == null)
        {
            Debug.LogWarning("[HUD] no '" + field + "' field to assign.");
            return;
        }
        property.objectReferenceValue = value;
    }
}
