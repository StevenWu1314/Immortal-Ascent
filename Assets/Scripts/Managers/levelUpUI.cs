using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class levelUpUI : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Button[] addButtons;
    [SerializeField] private TMP_Text str;
    [SerializeField] private TMP_Text dex;
    [SerializeField] private TMP_Text health;
    [SerializeField] private TMP_Text pointsLeft;
    [SerializeField] private int points;

    private Vector3Int pointsAssigned;
    // Start is called before the first frame update
    private void OnEnable()
    {
        pointsAssigned = new Vector3Int(0, 0, 0);
        addButtons[0] = GameObject.Find("str").GetComponentInChildren<Button>();
        addButtons[1] = GameObject.Find("dex").GetComponentInChildren<Button>();
        addButtons[2] = GameObject.Find("health").GetComponentInChildren<Button>();
        points = 5;
    }
    private void OnDisable() {
        pointsAssigned = new Vector3Int(0, 0, 0);
    }

    // Update is called once per frame
    void Update()
    {
        str.text = playerStats.getStrength().ToString();
        dex.text = playerStats.getDexterity().ToString();
        health.text = playerStats.getMaxHealth().ToString();
        pointsLeft.text = points.ToString();

        if(points <= 0)
        {
            for(int i = 0; i < addButtons.Length; i++)
            {
                addButtons[i].interactable = false;
            }
        }
        if(points > 0)
        {
            for(int i = 0; i < addButtons.Length; i++)
            {
                addButtons[i].interactable = true;
            }
        }
    }

    public void recordStr()
    {
        pointsAssigned.x++;
        points--;
    }

    public void recordDex()
    {
        pointsAssigned.y++;
        points--;
    }
    public void recordHealth()
    {
        pointsAssigned.z += 10;
        points--;
    }

    public void reAssign()
    {
        for(int i = 0; i < pointsAssigned.x; i++)
        {
            playerStats.decreaseStrength();
        }
        for(int i = 0; i < pointsAssigned.y; i++)
        {
            playerStats.decreaseDexterity();
        }
        for(int i = 0; i < pointsAssigned.z; i++)
        {
            playerStats.decreaseMaxHealth();
        }
        pointsAssigned = new Vector3Int(0, 0, 0);
        points = 5;
    }
}
