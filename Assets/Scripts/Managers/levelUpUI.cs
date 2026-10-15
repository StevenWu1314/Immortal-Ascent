using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class levelUpUI : MonoBehaviour
{
    private PlayerStats playerStats;
    private Button[] addButtons = new Button[3];
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
        playerStats = PlayerStats.Instance;
    }
    private void OnDisable() {
        pointsAssigned = new Vector3Int(0, 0, 0);
    }

    // Update is called once per frame
    void Update()
    {
        str.text = playerStats.Strength.ToString();
        dex.text = playerStats.Dexterity.ToString();
        health.text = playerStats.EffectiveMaxHealth.ToString();
        pointsLeft.text = points.ToString();

        for (int i = 0; i < addButtons.Length; i++)
        {
            addButtons[i].interactable = points > 0;
        }
    }

    public void recordStr()
    {
        if (points <= 0) return;
        pointsAssigned.x++;
        playerStats.increaseStrength();
        points--;
    }

    public void recordDex()
    {
        if (points <= 0) return;
        pointsAssigned.y++;
        playerStats.increaseDexterity();
        points--;
    }
    public void recordHealth()
    {
        if (points <= 0) return;
        pointsAssigned.z += 10;
        playerStats.increaseMaxHealth();
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
        // pointsAssigned.z is a health AMOUNT (10 per point), not an iteration count.
        // Looping it as a count ran 10x too many times and also depended on the magic 10
        // appearing in two places.
        if (pointsAssigned.z > 0)
        {
            playerStats.decreaseMaxHealth(pointsAssigned.z);
        }
        pointsAssigned = new Vector3Int(0, 0, 0);
        points = 5;
    }
}
