using System;
using UnityEngine;


public class PlayerStats : MonoBehaviour{
    [SerializeField] int Health;
    [SerializeField] int MaxHealth;
    [SerializeField] int strength;
    [SerializeField] int dexterity;
    [SerializeField] int[,] temporaryStatAdjustments = new int[2, 2];
    [SerializeField] int currentExperience;
    [SerializeField] int currentLevel;
    [SerializeField] int levelCap;
    [SerializeField] int experienceToNextLevel;
    [SerializeField] Weapon weapon;
    [SerializeField] Weapon rangedWeapon;
    public static event Action<PlayerStats> onLevelUpEvent;


    void OnEnable()
    {
        Controls.onMoveEvent += tickDownBuffDuration;
    }

    private void tickDownBuffDuration(Controls controls)
    {
        if(temporaryStatAdjustments[0, 1] > 0)
        {
            temporaryStatAdjustments[0, 1] -= 1;
        }
        else {
            temporaryStatAdjustments[0, 0] = 0;
        }
        if(temporaryStatAdjustments[1, 1] > 0)
        {
            temporaryStatAdjustments[1, 1] -= 1;
        }
        else {
            temporaryStatAdjustments[1, 0] = 0;
        }
    }

   
    public void attack(string form, Enemy target)
    {
        Debug.Log(target);
        switch (form){
            case "melee":
                target.takeDamage(strength + weapon.getDamage() + temporaryStatAdjustments[0, 0]);
                target.attack(this);
                break;
            case "range":
                target.takeDamage(dexterity + weapon.getDamage() + temporaryStatAdjustments[1, 0]);
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
        currentExperience += amount;
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
                break;
            case "Dexterity":
                temporaryStatAdjustments[1, 0] = amount;
                temporaryStatAdjustments[1, 1] = duration;
                break;
        }
        
    }
    internal void increaseExp(int expValue)
    {
        throw new NotImplementedException();
    }
}
