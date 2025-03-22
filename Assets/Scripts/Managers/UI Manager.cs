using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using TMPro.EditorUtilities;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject levelUpMenu;
    
    private void Awake() {
        PlayerStats.onLevelUpEvent += toggleLevelUpMenu;
    }

    private void toggleLevelUpMenu(PlayerStats stats)
    {
        levelUpMenu.SetActive(true);
    }

    private void Update() {
        
    }

    public void DrawFlowupDamageText(int damage, Vector3 position)
    {
        TextMesh damageText = utilityFunction.createWorldText("-" + damage.ToString(), null, position, 20, Color.white, TextAnchor.MiddleCenter, TextAlignment.Center, 1);
        StartCoroutine(FlowupDamageText(damageText));
    }
    IEnumerator FlowupDamageText(TextMesh damageText)
    {
        for(int i = 0; i < 100; i++)
        {
            damageText.transform.position += new Vector3(0, 0.1f, 0);
            yield return new WaitForSeconds(0.01f);
        }
        Destroy(damageText);
    }
}
