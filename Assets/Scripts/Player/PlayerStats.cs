using System;
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
        Debug.Log(target);
        switch (form){
            case "melee":
                target.takeDamage(strength + equipments.meleeWeapon.getDamage() + temporaryStatAdjustments[0, 0]);
                break;
            case "range":
                target.takeDamage(dexterity + equipments.rangeWeapon.getDamage() + temporaryStatAdjustments[1, 0]);
                break;        
           }
    }

    public void takeDamage(int damage)
    {
        Health -= damage;
        updateHealthBar();
        if (Health <= 0)
        {
            SceneManager.LoadScene(0);
        }
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
        switch (stat)
        {
            case "Strength":
                temporaryStatAdjustments[0, 0] = amount;
                temporaryStatAdjustments[0, 1] = duration;
                strength += amount;
                break;
            case "Dexterity":
                temporaryStatAdjustments[1, 0] = amount;
                temporaryStatAdjustments[1, 1] = duration;
                dexterity += amount;
                break;
            case "Max Health":
                temporaryStatAdjustments[2, 0] = amount;
                temporaryStatAdjustments[2, 1] = duration;
                MaxHealth += amount;
                break;
        }
        
    }

    void tickdownBuffs(Controls controls)
    {
        if (temporaryStatAdjustments[0, 1] > 0)
        {
            temporaryStatAdjustments[0, 1]--;
            if (temporaryStatAdjustments[0, 1] == 0)
            {
                strength -= temporaryStatAdjustments[0, 0];
            }
        }
        if (temporaryStatAdjustments[1, 1] > 0)
        {
            temporaryStatAdjustments[1, 1]--;
            if (temporaryStatAdjustments[1, 1] == 0)
            {
                dexterity -= temporaryStatAdjustments[1, 0];
            }
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
}
