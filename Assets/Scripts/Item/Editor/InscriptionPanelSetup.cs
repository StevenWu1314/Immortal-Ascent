using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds the three-slot inscription panel prefab and places it in the dungeon scene.
///
/// The hierarchy is assembled with layout groups rather than hand-placed anchors, so it stays
/// readable and re-runnable. Visual polish (fonts, colours, sizes) is expected to be tuned by
/// hand afterwards; this tool only guarantees the structure and the component wiring.
/// </summary>
public static class InscriptionPanelSetup
{
    private const string PrefabPath = "Assets/Prefabs/UI/InscriptionPanel.prefab";
    private const string ScenePath = "Assets/Scenes/Branching dungeon.unity";

    [MenuItem("Tools/Items/6 - Build Inscription Panel UI")]
    public static void Build()
    {
        GameObject root = BuildHierarchy();

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            AssetDatabase.DeleteAsset(PrefabPath);
        }
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        Debug.Log($"[PANEL-SETUP] wrote {PrefabPath}");

        PlaceInScene(prefab);
    }

    private static GameObject BuildHierarchy()
    {
        // --- root canvas -----------------------------------------------------------------
        GameObject root = new GameObject("InscriptionPanel", typeof(RectTransform));
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        root.AddComponent<GraphicRaycaster>();

        // --- window ----------------------------------------------------------------------
        GameObject window = CreateUI("Window", root.transform);
        Stretch(window.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(660f, 680f));
        Image windowImage = window.AddComponent<Image>();
        windowImage.color = new Color(0.07f, 0.07f, 0.09f, 0.96f);

        VerticalLayoutGroup windowLayout = window.AddComponent<VerticalLayoutGroup>();
        windowLayout.padding = new RectOffset(24, 24, 20, 20);
        windowLayout.spacing = 12f;
        windowLayout.childControlHeight = false;
        windowLayout.childControlWidth = true;
        windowLayout.childForceExpandHeight = false;
        windowLayout.childForceExpandWidth = true;

        TMP_Text title = CreateText(window.transform, "Title", "Inscription", 32f, TextAlignmentOptions.Center);
        PreferredHeight(title.gameObject, 44f);

        TMP_Text inscriptionName = CreateText(window.transform, "InscriptionName", "", 26f, TextAlignmentOptions.Center);
        PreferredHeight(inscriptionName.gameObject, 36f);

        TMP_Text inscriptionEffect = CreateText(window.transform, "InscriptionEffect", "", 20f,
                                                TextAlignmentOptions.Center);
        PreferredHeight(inscriptionEffect.gameObject, 32f);

        // --- target list ------------------------------------------------------------------
        GameObject targetList = CreateUI("TargetList", window.transform);
        VerticalLayoutGroup listLayout = targetList.AddComponent<VerticalLayoutGroup>();
        listLayout.spacing = 6f;
        listLayout.childControlHeight = false;
        listLayout.childControlWidth = true;
        listLayout.childForceExpandHeight = false;
        listLayout.childForceExpandWidth = true;
        ContentSizeFitter listFitter = targetList.AddComponent<ContentSizeFitter>();
        listFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // The template stays in the prefab, inactive, and is cloned per target at runtime.
        Button template = CreateButton(targetList.transform, "TargetButtonTemplate", "Target");
        template.gameObject.SetActive(false);

        // --- slot section -----------------------------------------------------------------
        GameObject slotSection = CreateUI("SlotSection", window.transform);
        VerticalLayoutGroup slotLayout = slotSection.AddComponent<VerticalLayoutGroup>();
        slotLayout.spacing = 8f;
        slotLayout.childControlHeight = false;
        slotLayout.childControlWidth = true;
        slotLayout.childForceExpandHeight = false;
        slotLayout.childForceExpandWidth = true;
        ContentSizeFitter slotFitter = slotSection.AddComponent<ContentSizeFitter>();
        slotFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TMP_Text slotPrompt = CreateText(slotSection.transform, "SlotPrompt", "", 20f, TextAlignmentOptions.Center);
        PreferredHeight(slotPrompt.gameObject, 32f);

        GameObject slotRow = CreateUI("SlotRow", slotSection.transform);
        HorizontalLayoutGroup rowLayout = slotRow.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 8f;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = false;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = false;

        var slotButtons = new Button[EquipmentInstance.MaxInscriptions];
        var slotLabels = new TMP_Text[EquipmentInstance.MaxInscriptions];
        for (int i = 0; i < EquipmentInstance.MaxInscriptions; i++)
        {
            Button slot = CreateButton(slotRow.transform, $"Slot{i + 1}", $"{i + 1}. (empty)");
            slotButtons[i] = slot;
            slotLabels[i] = slot.GetComponentInChildren<TMP_Text>();
        }

        // --- buttons ----------------------------------------------------------------------
        GameObject buttonRow = CreateUI("ButtonRow", window.transform);
        HorizontalLayoutGroup buttonRowLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
        buttonRowLayout.spacing = 12f;
        buttonRowLayout.childControlWidth = true;
        buttonRowLayout.childControlHeight = false;
        buttonRowLayout.childForceExpandWidth = true;
        buttonRowLayout.childForceExpandHeight = false;

        Button backButton = CreateButton(buttonRow.transform, "BackButton", "Back");
        Button closeButton = CreateButton(buttonRow.transform, "CloseButton", "Close");

        // --- component + wiring -----------------------------------------------------------
        InscriptionPanel panel = root.AddComponent<InscriptionPanel>();
        var so = new SerializedObject(panel);
        so.FindProperty("window").objectReferenceValue = window;
        so.FindProperty("inscriptionName").objectReferenceValue = inscriptionName;
        so.FindProperty("inscriptionEffect").objectReferenceValue = inscriptionEffect;
        so.FindProperty("targetList").objectReferenceValue = targetList.transform;
        so.FindProperty("targetButtonTemplate").objectReferenceValue = template;
        so.FindProperty("slotSection").objectReferenceValue = slotSection;
        so.FindProperty("slotPrompt").objectReferenceValue = slotPrompt;
        so.FindProperty("backButton").objectReferenceValue = backButton;
        so.FindProperty("closeButton").objectReferenceValue = closeButton;

        SerializedProperty slotButtonsProp = so.FindProperty("slotButtons");
        slotButtonsProp.arraySize = slotButtons.Length;
        SerializedProperty slotLabelsProp = so.FindProperty("slotLabels");
        slotLabelsProp.arraySize = slotLabels.Length;
        for (int i = 0; i < slotButtons.Length; i++)
        {
            slotButtonsProp.GetArrayElementAtIndex(i).objectReferenceValue = slotButtons[i];
            slotLabelsProp.GetArrayElementAtIndex(i).objectReferenceValue = slotLabels[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        return root;
    }

    private static void PlaceInScene(GameObject prefab)
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // Replace any previous instance so re-running does not stack panels.
        foreach (InscriptionPanel existing in Object.FindObjectsByType<InscriptionPanel>(
                     FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = "InscriptionPanel";

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[PANEL-SETUP] placed '{instance.name}' in {ScenePath}");
    }

    // ---------------------------------------------------------------------------------------
    // helpers
    // ---------------------------------------------------------------------------------------

    private static GameObject CreateUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, float size,
                                       TextAlignmentOptions alignment)
    {
        GameObject go = CreateUI(name, parent);
        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.alignment = alignment;
        label.enableWordWrapping = true;
        return label;
    }

    private static Button CreateButton(Transform parent, string name, string label)
    {
        GameObject go = CreateUI(name, parent);
        Image background = go.AddComponent<Image>();
        background.color = new Color(0.20f, 0.20f, 0.26f, 1f);

        Button button = go.AddComponent<Button>();
        button.targetGraphic = background;

        TMP_Text text = CreateText(go.transform, "Label", label, 20f, TextAlignmentOptions.Center);
        Stretch(text.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), true);

        LayoutElement element = go.AddComponent<LayoutElement>();
        element.preferredHeight = 48f;
        element.minHeight = 40f;
        return button;
    }

    private static void PreferredHeight(GameObject go, float height)
    {
        LayoutElement element = go.GetComponent<LayoutElement>();
        if (element == null)
        {
            element = go.AddComponent<LayoutElement>();
        }
        element.preferredHeight = height;
        element.minHeight = height;
    }

    /// <summary>Anchor a rect to the centre with a fixed size, or stretch it to fill its parent.</summary>
    private static void Stretch(RectTransform rect, Vector2 anchor, Vector2 size, bool fill = false)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);

        if (fill)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return;
        }

        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
    }
}
