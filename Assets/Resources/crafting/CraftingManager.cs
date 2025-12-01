using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class CraftingManager : MonoBehaviour
{
    public ScriptableObject[] recipes;
    public static CraftingManager Instance { get; private set; }
    public GameObject CraftingPanel;
    public GameObject buttonTemplate;
    List<GameObject> buttons;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }
    // Start is called before the first frame update
    void Start()
    {
        recipes = Resources.LoadAll<ScriptableObject>("crafting");
        buttons = new List<GameObject>();
    }

    public void LoadAvailableRecipies()
    {
        foreach (Recipe recipe in recipes)
        {
            if (recipe.canCraft())
            {
                GameObject button = Instantiate(buttonTemplate, CraftingPanel.transform.Find("itemholder"));
                button.transform.Find("Text (TMP)").gameObject.GetComponent<TMP_Text>().text = recipe.resultName;
                buttons.Add(button);
                button.GetComponent<Button>().onClick.AddListener(recipe.craft);
            }
            Debug.Log($"Attempting to check available Recipe: {recipe.resultName}");
        }
    }

    public void removeButtons()
    {
        foreach (GameObject button in buttons)
        {
            Destroy(button);
        }
        buttons.Clear();
    }

    public void ImportAllRecipies()
    {
        recipes = Resources.LoadAll<ScriptableObject>("crafting");
        foreach (Recipe recipe in recipes)
        {
            recipe.ImportRecipe();
        }
    }
    public void ExportAllRecipies()
    {
        recipes = Resources.LoadAll<ScriptableObject>("crafting");
        foreach (Recipe recipe in recipes)
        {
            recipe.ExportRecipe();
        }
    }
}
