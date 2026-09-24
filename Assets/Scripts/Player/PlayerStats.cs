using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour{
    [SerializeField] int Health;
    [SerializeField] int MaxHealth;
    [SerializeField] int strength;
    [SerializeField] int dexterity;
    [SerializeField] int range;
    [SerializeField] int[,] temporaryStatAdjustments = new int[3, 2];
    [SerializeField] int currentExperience;
    [SerializeField] int currentLevel;
    [SerializeField] int levelCap;
    [SerializeField] int experienceToNextLevel;
    [SerializeField] Equipments equipments;
    [SerializeField] AchievementManager achievementManager;
    [SerializeField] float expMultiplier = 1;
    SpriteRenderer spriteRenderer;
    public static event Action<PlayerStats> onLevelUpEvent;
    public static event Action<PlayerStats> onDeath;

    public static PlayerStats Instance;
    [SerializeField] Image HealthBar;
    [SerializeField] Image ExpBar;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    void OnEnable()
    {
        Controls.onMoveEvent += tickdownBuffs;
        HealthBar = GameObject.Find("HpBar").GetComponent<Image>();
        ExpBar = GameObject.Find("ExpBar").GetComponent<Image>();
        ApplyAchievementBuffs();
    }
    void OnDestroy()
    {
        Controls.onMoveEvent -= tickdownBuffs;
    }

    private void ApplyAchievementBuffs()
    {
        if(achievementManager.bowKill)
        {
            dexterity += 5;
        }
        if(achievementManager.firstFloorCleared)
        {
            expMultiplier += 0.3f;
        }
        if(achievementManager.turtleKilled)
        {
            MaxHealth += 25;
        }
    }

   
    public void attack(string form, Enemy target)
    {
        if (target == null)
        {
            Debug.LogWarning("PlayerStats.attack called with a null target; ignoring.");
            return;
        }
        if (equipments == null)
        {
            Debug.LogError("PlayerStats: no Equipments assigned.");
            return;
        }

        switch (form){
            case "melee":
                if (equipments.meleeWeapon == null)
                {
                    UIManager.Instance.DrawFlowupText("No melee weapon equipped", transform.position);
                    return;
                }
                // No temporary-adjustment term: increaseStatTemp already folds the bonus
                // into `strength`, so adding it again doubled every elixir's effect.
                target.takeDamage(strength + equipments.meleeWeapon.getDamage());
                break;
            case "range":
                if (equipments.rangeWeapon == null)
                {
                    UIManager.Instance.DrawFlowupText("No ranged weapon equipped", transform.position);
                    return;
                }
                target.takeDamage(dexterity + equipments.rangeWeapon.getDamage());
                break;        
           }
    }

    public void takeDamage(int damage)
    {
        Health -= damage;
        updateHealthBar();
        StartCoroutine(DamageIndicator());
        StartCoroutine(DamageShake());
        if (Health <= 0)
        {
            SceneManager.LoadScene(0);
        }
    }

    private IEnumerator DamageIndicator() {
        float indicatortimer = 0.3f;

        Color[] damageColors = {
            new Color(1f, 0f, 0f, 1f),
            Color.white,
            Color.white
        };

        foreach (Color color in damageColors) {
            spriteRenderer.color = color;
            yield return new WaitForSeconds(indicatortimer);
        }

        spriteRenderer.color = Color.white;
    }

    private IEnumerator DamageShake() {
        Vector3 originalPos = transform.position;
        float shakeAmount = 0.1f;
        float shakeInterval = 0.05f;

        for (int i = 0; i < 6; i++)
        {
            transform.position = originalPos + new Vector3(i % 2 == 0 ? shakeAmount : -shakeAmount, 0, 0);
            yield return new WaitForSeconds(shakeInterval);
        }

        transform.position = originalPos;
    }

    public void heal(int amount)
    {
        Health += amount;
        if (Health > MaxHealth)
        {
            Health = MaxHealth;
        }
        updateHealthBar();
    }

    public void updateHealthBar()
    {
        HealthBar.fillAmount = (float) Health / (float)MaxHealth;
    }

    public void updateExpBar()
    {
        ExpBar.fillAmount = (float) currentExperience / (float) experienceToNextLevel;
    }
    public void gainExperience(int amount)
    {
        currentExperience += (int)(amount * expMultiplier);
        if (currentLevel < levelCap)
        {
            if (currentExperience >= experienceToNextLevel)
            {
                currentExperience -= experienceToNextLevel;
                currentLevel++;
                experienceToNextLevel = (int)(experienceToNextLevel * 1.5f);
                onLevelUpEvent(this);
                Health = MaxHealth;
            }
        }
        updateExpBar();
        updateHealthBar();

    }

    public int getStrength()
    {
        return strength;
    }
    
    public int getDexterity()
    {
        return dexterity;
    }
    public int getMaxHealth()
    {
        return MaxHealth;
    }

    public void increaseStrength()
    {
        strength++;
    }
    public void increaseDexterity()
    {
        dexterity++;
    }
    public void increaseMaxHealth()
    {
        MaxHealth += 10;
        Health += 10;
    }
    public void increaseMaxHealth(int amount)
    {
        MaxHealth += amount;
        updateHealthBar();
    }
    public void decreaseStrength()
    {
        strength--;
    }
    public void decreaseDexterity()
    {
        dexterity--;
    }
    public void decreaseMaxHealth(int amount)
    {
        MaxHealth -= amount;
        if (Health > MaxHealth)
        {
            Health = MaxHealth;
        }
        updateHealthBar();
    }

    public void increaseStatTemp(string stat, int amount, int duration)
    {
        int index;
        switch (stat)
        {
            case "Strength":   index = 0; break;
            case "Dexterity":  index = 1; break;
            case "Max Health": index = 2; break;
            default:
                Debug.LogWarning($"increaseStatTemp: unknown stat '{stat}'");
                return;
        }

        // Re-applying a buff of the same kind must not stack its bonus additively.
        // Remove whatever is still active first, then apply the new value.
        RevertTempStat(index);

        temporaryStatAdjustments[index, 0] = amount;
        temporaryStatAdjustments[index, 1] = duration;

        switch (index)
        {
            case 0: strength += amount; break;
            case 1: dexterity += amount; break;
            case 2: increaseMaxHealth(amount); break;
        }
    }

    private void RevertTempStat(int index)
    {
        int applied = temporaryStatAdjustments[index, 0];
        temporaryStatAdjustments[index, 0] = 0;
        temporaryStatAdjustments[index, 1] = 0;
        if (applied == 0)
        {
            return;
        }
        switch (index)
        {
            case 0: strength -= applied; break;
            case 1: dexterity -= applied; break;
            case 2: decreaseMaxHealth(applied); break;
        }
    }

    /// <summary>
    /// Remaining turns per buff: [0,*] = Strength, [1,*] = Dexterity, [2,*] = Max Health.
    /// temporaryStatAdjustments[stat, 0] holds the amount to revert when the timer expires.
    /// </summary>
    void tickdownBuffs(Controls controls)
    {
        for (int stat = 0; stat < 3; stat++)
        {
            if (temporaryStatAdjustments[stat, 1] <= 0)
            {
                continue;
            }

            temporaryStatAdjustments[stat, 1]--;
            if (temporaryStatAdjustments[stat, 1] > 0)
            {
                continue;
            }

            // Expired: revert the amount that was actually applied. Includes index 2
            // (Max Health), which used to be skipped entirely, making that buff permanent.
            RevertTempStat(stat);
        }
    }
    internal void increaseExp(int expValue)
    {
        throw new NotImplementedException();
    }

    public int getRange()
    {
        if (equipments.rangeWeapon != null)
        {
            return range;
        }
        else
        {
            return 0;
        }
    }
    
    public void setRange(int range)
    {
        this.range = range;
    }
}
