using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour{
    // ---------------------------------------------------------------------------------
    // BASE VALUES. These are the saved/levelled numbers. Level-up allocation and the
    // levelUpUI rollback write here, and nothing else.
    // ---------------------------------------------------------------------------------
    [SerializeField] int Health;
    [SerializeField] int MaxHealth;
    [SerializeField] int strength;
    [SerializeField] int dexterity;

    /// <summary>
    /// Temporary buffs: [stat, 0] = amount, [stat, 1] = turns remaining.
    /// These are NOT folded into the base values any more; the effective accessors add them.
    /// That removes the old add-now/subtract-on-expiry bookkeeping, which is what made a
    /// MaxHealth buff permanent when its revert path was skipped.
    /// </summary>
    int[,] temporaryStatAdjustments = new int[3, 2];

    [SerializeField] int currentExperience;
    [SerializeField] int currentLevel;
    [SerializeField] int levelCap;
    [SerializeField] int experienceToNextLevel;
    [SerializeField] Equipments equipments;

    /// <summary>Experience gain multiplier, applied in <see cref="gainExperience"/>.</summary>
    [SerializeField] float expMultiplier = 1;

    // ---------------------------------------------------------------------------------
    // DERIVED BONUSES. Deliberately not serialized: RecalculateStats() rebuilds them from
    // scratch, so they cannot drift and cannot be applied twice.
    // ---------------------------------------------------------------------------------
    private int bonusStrength;
    private int bonusDexterity;
    private int bonusMaxHealth;

    SpriteRenderer spriteRenderer;

    /// <summary>Guard so overlapping hits cannot stack shakes (see <see cref="DamageShake"/>).</summary>
    private bool shaking;
    private Vector3 shakeOrigin;
    public static event Action<PlayerStats> onLevelUpEvent;

    public static PlayerStats Instance;
    [SerializeField] Image HealthBar;
    [SerializeField] Image ExpBar;

    // ---------------------------------------------------------------------------------
    // EFFECTIVE VALUES. Everything outside this class should read these.
    // ---------------------------------------------------------------------------------
    public int Strength => strength + bonusStrength + TempBonus(0);
    public int Dexterity => dexterity + bonusDexterity + TempBonus(1);
    public int EffectiveMaxHealth => Mathf.Max(1, MaxHealth + bonusMaxHealth + TempBonus(2));
    /// <summary>Base strength, before equipment/inscription/achievement/buff bonuses.</summary>
    public int BaseStrength => strength;
    public int BaseDexterity => dexterity;
    public int BaseMaxHealth => MaxHealth;

    private int TempBonus(int index)
    {
        return temporaryStatAdjustments[index, 0];
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        spriteRenderer = GetComponent<SpriteRenderer>();
        RecalculateStats();
    }

    void OnEnable()
    {
        Controls.onMoveEvent += tickdownBuffs;
        // Guarded: these are found by name, and a renamed or missing bar used to throw here.
        GameObject hp = GameObject.Find("HpBar");
        if (hp != null)
        {
            HealthBar = hp.GetComponent<Image>();
        }
        GameObject exp = GameObject.Find("ExpBar");
        if (exp != null)
        {
            ExpBar = exp.GetComponent<Image>();
        }
        RecalculateStats();
    }

    void OnDestroy()
    {
        Controls.onMoveEvent -= tickdownBuffs;
    }

    /// <summary>
    /// Rebuild every derived bonus from its sources. Idempotent by construction, which is why
    /// it is safe to call from Awake, OnEnable, an equipment swap and an achievement unlock
    /// alike. This replaces ApplyAchievementBuffs(), which mutated the base fields in place.
    /// </summary>
    public void RecalculateStats()
    {
        bonusStrength = 0;
        bonusDexterity = 0;
        bonusMaxHealth = 0;

        if (equipments != null)
        {
            bonusStrength += equipments.GetWearerBonus(BonusStat.Strength);
            bonusDexterity += equipments.GetWearerBonus(BonusStat.Dexterity);
            bonusMaxHealth += equipments.GetWearerBonus(BonusStat.MaxHealth);

            // Armor's own value, plus any ItemStatBonus inscriptions on it.
            bonusMaxHealth += equipments.GetArmorHealthBonus();
        }

        ClampHealthToMax();
        updateHealthBar();
    }

    private void ClampHealthToMax()
    {
        int max = EffectiveMaxHealth;
        if (Health > max)
        {
            Health = max;
        }
        if (Health < 0)
        {
            Health = 0;
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
                // Strength is the effective value (base + bonuses + buffs), so it must not be
                // added twice. Weapon damage comes from the equipped instance so an
                // ItemStatBonus inscription is included.
                target.takeDamage(Strength + equipments.GetMeleeDamage());
                break;
            case "range":
                if (equipments.rangeWeapon == null)
                {
                    UIManager.Instance.DrawFlowupText("No ranged weapon equipped", transform.position);
                    return;
                }
                target.takeDamage(Dexterity + equipments.GetRangedDamage());
                break;        
           }
    }

    public void takeDamage(int damage)
    {
        Health -= damage;
        updateHealthBar();
        StartCoroutine(utilityFunction.FlashSprite(spriteRenderer,
            new[] { new Color(1f, 0f, 0f, 1f), Color.white, Color.white }, 0.3f));
        StartCoroutine(DamageShake());
        if (Health <= 0)
        {
            SceneManager.LoadScene(0);
        }
    }


    /// <summary>
    /// Visual hit shake.
    ///
    /// This writes transform.position, which is also the value Controls used to derive the
    /// player's grid cell from -- so it MUST always put the transform back exactly where it
    /// found it, and it must never run twice at once.
    ///
    /// It used to do neither. Two melee enemies attacking in the same turn start two shakes
    /// 0.1s apart; the second captured an ALREADY OFFSET transform as its origin, so when both
    /// finished the player was left 0.1 units off the grid. FloorToInt then reported a
    /// neighbouring cell, EntityManager.MoveEntity could not find the player there, and the
    /// player could no longer move at all.
    /// </summary>
    private IEnumerator DamageShake() {
        if (shaking)
        {
            // One shake per volley. Restarting would capture the current (offset) position as
            // the new origin, which is the exact bug this guard exists to prevent.
            yield break;
        }

        shaking = true;
        shakeOrigin = transform.position;
        float shakeAmount = 0.1f;
        float shakeInterval = 0.05f;

        try
        {
            for (int i = 0; i < 6; i++)
            {
                transform.position = shakeOrigin + new Vector3(i % 2 == 0 ? shakeAmount : -shakeAmount, 0, 0);
                yield return new WaitForSeconds(shakeInterval);
            }
        }
        finally
        {
            // Always restore, even if the coroutine is disposed mid-shake.
            transform.position = shakeOrigin;
            shaking = false;
        }
    }

    public void heal(int amount)
    {
        Health += amount;
        if (Health > EffectiveMaxHealth)
        {
            Health = EffectiveMaxHealth;
        }
        updateHealthBar();
    }

    public void updateHealthBar()
    {
        if (HealthBar == null)
        {
            return;
        }
        HealthBar.fillAmount = (float) Health / (float)EffectiveMaxHealth;
    }

    public void updateExpBar()
    {
        if (ExpBar == null)
        {
            return;
        }
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
                Health = EffectiveMaxHealth;
            }
        }
        updateExpBar();
        updateHealthBar();
    }

    public int getStrength()
    {
        return Strength;
    }
    
    public int getDexterity()
    {
        return Dexterity;
    }

    public int getMaxHealth()
    {
        return EffectiveMaxHealth;
    }

    // ---------------------------------------------------------------------------------
    // BASE MUTATORS. Only level-up allocation should use these.
    // ---------------------------------------------------------------------------------

    public void increaseStrength()
    {
        strength++;
        RecalculateStats();
    }
    public void increaseDexterity()
    {
        dexterity++;
        RecalculateStats();
    }
    public void increaseMaxHealth()
    {
        MaxHealth += 10;
        Health += 10;
        RecalculateStats();
    }

    /// <summary>Base MaxHealth change. Equipment and inscriptions must NOT use this.</summary>
    public void increaseMaxHealth(int amount)
    {
        MaxHealth += amount;
        RecalculateStats();
    }

    public void decreaseStrength()
    {
        strength--;
        RecalculateStats();
    }
    public void decreaseDexterity()
    {
        dexterity--;
        RecalculateStats();
    }

    /// <summary>Base MaxHealth change, used by the level-up rollback. Not for equipment.</summary>
    public void decreaseMaxHealth(int amount)
    {
        MaxHealth -= amount;
        RecalculateStats();
    }

    // ---------------------------------------------------------------------------------
    // TEMPORARY BUFFS. Stored in the table only; the effective accessors add them.
    // ---------------------------------------------------------------------------------

    public void increaseStatTemp(string stat, int amount, int duration)
    {
        int index = TempStatIndex(stat);
        if (index < 0)
        {
            return;
        }

        // Re-applying a buff of the same kind replaces it rather than stacking, which now
        // happens for free because the bonus is read from this single slot.
        temporaryStatAdjustments[index, 0] = amount;
        temporaryStatAdjustments[index, 1] = duration;

        ClampHealthToMax();
        updateHealthBar();
    }

    private int TempStatIndex(string stat)
    {
        switch (stat)
        {
            case "Strength":   return 0;
            case "Dexterity":  return 1;
            case "Max Health": return 2;
            default:
                Debug.LogWarning($"increaseStatTemp: unknown stat '{stat}'");
                return -1;
        }
    }

    private void ClearTempStat(int index)
    {
        temporaryStatAdjustments[index, 0] = 0;
        temporaryStatAdjustments[index, 1] = 0;
    }

    /// <summary>
    /// Remaining turns per buff: [0,*] = Strength, [1,*] = Dexterity, [2,*] = Max Health.
    /// </summary>
    void tickdownBuffs(Controls controls)
    {
        bool changed = false;
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

            // Expired. Because the bonus is read live from the table, clearing the slot is the
            // whole revert -- there is nothing to subtract, so index 2 (Max Health) can no
            // longer be left applied forever.
            ClearTempStat(stat);
            changed = true;
        }

        if (changed)
        {
            ClampHealthToMax();
            updateHealthBar();
        }
    }
}
