using System;
using UnityEngine;


public class PlayerStats : MonoBehaviour{
    [SerializeField] int Health;
    [SerializeField] int MaxHealth;
    [SerializeField] int strength;
    [SerializeField] int dexterity;
    [SerializeField] int[,] temporaryStatAdjustments = new int[3, 2];
    [SerializeField] int currentExperience;
    [SerializeField] int currentLevel;
    [SerializeField] int levelCap;
    [SerializeField] int experienceToNextLevel;
    [SerializeField] Equipments equipments;
    [SerializeField] AchievementManager achievementManager;
    [SerializeField] float expMultiplier = 1;
    public static event Action<PlayerStats> onLevelUpEvent;


    void OnEnable()
    {
        Controls.onMoveEvent += tickDownBuffDuration;
        ApplyAchievementBuffs();
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

    private void tickDownBuffDuration(Controls controls)
    {
        if(temporaryStatAdjustments[0, 1] > 0)
        {
            temporaryStatAdjustments[0, 1] -= 1;
            if(temporaryStatAdjustments[0, 1] == 0)
            {
                strength -= temporaryStatAdjustments[0, 0];
            }
        }
        else {
            temporaryStatAdjustments[0, 0] = 0;
        }
        if(temporaryStatAdjustments[1, 1] > 0)
        {
            temporaryStatAdjustments[1, 1] -= 1;
            if(temporaryStatAdjustments[1, 1] == 0)
            {
                dexterity -= temporaryStatAdjustments[1, 0];
            }
        }
        else {
            temporaryStatAdjustments[1, 0] = 0;
        }
        if(temporaryStatAdjustments[2, 1] > 0)
        {
            temporaryStatAdjustments[2, 1] -= 1;
            if(temporaryStatAdjustments[2, 1] == 0)
            {
                MaxHealth -= temporaryStatAdjustments[2, 0];
            }
        }
        else {
            temporaryStatAdjustments[2, 0] = 0;
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
    }

    public void heal(int amount) 
    {
        Health += amount;
        if(Health > MaxHealth)
        {
            Health = MaxHealth;
        }
    }
    private void OnDrawGizmos() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1);
    }

    public void gainExperience(int amount)
    {
        currentExperience += (int)(amount * expMultiplier);
        if(currentLevel < levelCap)
        {
            if (currentExperience >= experienceToNextLevel)
            {
                currentExperience -= experienceToNextLevel;
                currentLevel++;
                experienceToNextLevel = (int)(experienceToNextLevel * 1.1f);
                onLevelUpEvent(this);
            }
        }
        
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
    }

    public void decreaseStrength()
    {
        strength--;
    }
    public void decreaseDexterity()
    {
        dexterity--;
    }
    public void decreaseMaxHealth()
    {
        MaxHealth--;
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
    internal void increaseExp(int expValue)
    {
        throw new NotImplementedException();
    }
}
