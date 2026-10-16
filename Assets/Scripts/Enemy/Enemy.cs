using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    [SerializeField] protected int health;
    [SerializeField] protected int damage;
    [SerializeField] protected int expValue;

    [Tooltip("Optional. When set, these stats replace the prefab's own, scaled for the current floor (T09).")]
    [SerializeField] private EnemyDefinition definition;

    /// <summary>This enemy's data asset, or null when it still runs on the prefab's serialized values.</summary>
    public EnemyDefinition Definition => definition;

    private UIManager uiManager;
    SpriteRenderer spriteRenderer;

    /// <summary>
    /// Apply the floor's scaling. The definition's numbers win when there is one; otherwise the prefab's own
    /// serialized values are the base. Either way the floor multiplier applies, so an unauthored project still
    /// gets harder on floor 2.
    /// </summary>
    private void Awake()
    {
        int baseHealth = definition != null ? definition.maxHealth : health;
        int baseDamage = definition != null ? definition.damage : damage;
        if (definition != null)
        {
            expValue = definition.experience;
        }

        int floor = RunState.FloorIndex;
        health = EnemyScaling.ScaleHealth(baseHealth, floor);
        damage = EnemyScaling.ScaleDamage(baseDamage, floor);
    }

    void OnEnable()
    {
        uiManager = GameObject.Find("UIManager").GetComponent<UIManager>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    public void takeDamage(int damage)
    {
        health -= damage;
        uiManager.DrawFlowupText("-" + damage, this.transform.position);
        StartCoroutine(utilityFunction.FlashSprite(spriteRenderer,
            new[] { new Color(1f, 0f, 0f, 1f), Color.white }, 0.3f));
        if(health <= 0)
        {
            PlayerStats.Instance.gainExperience(expValue);
            Die();
        }
    }


    public void ForceDie()
    {
        // Same teardown as Die(), minus the loot roll: previously this only called
        // Destroy, so the enemy stayed registered in EntityManager (blocking that cell
        // forever) and stayed in the turn queue.
        Kill(false);
    }

    public virtual void Attack(PlayerStats target)
    {
        target.takeDamage(damage);
    }
    private void Die()
    {
        Kill(true);
    }

    /// <summary>
    /// Single teardown path shared by Die() and ForceDie(): leave the turn queue, release
    /// the grid cell, optionally roll loot, then destroy. Keeping both routes on one method
    /// stops them drifting apart again.
    /// </summary>
    private void Kill(bool awardLoot)
    {
        // Health and turn-taking live on two sibling components of the same GameObject:
        // Enemy owns damage/death, EnemyBehavior owns the TurnManager queue. Ask the
        // behaviour to leave the queue now, so a dying enemy cannot still be given a turn
        // during the frame before Destroy() actually runs. EnemyBehavior.OnDestroy() is the
        // catch-all for every other destruction path.
        if (TurnManager.Instance != null)
        {
            EnemyBehavior behavior = GetComponent<EnemyBehavior>();
            if (behavior != null)
            {
                TurnManager.Instance.UnregisterEnemy(behavior);
            }
        }
        // Instance search rather than the cell-keyed overload: this runs before Destroy,
        // but the cell is not worth trusting when the registry is the thing being fixed.
        if (EntityManager.Instance != null)
        {
            EntityManager.Instance.UnregisterEntity(gameObject);
        }
        if (awardLoot)
        {
            // Experience is granted by takeDamage() before Die() is called; only the loot
            // roll belongs here, so the two death routes cannot double-award.
            if (LootGen.Instance != null)
            {
                LootGen.Instance.rollTable();
            }

            // Per-type materials (GDD 12.2/13.7). Overflow is reported rather than silently destroyed;
            // ground drops are not built yet, which T06's checkpoint records.
            if (Inventory.Instance != null)
            {
                int leftover = EnemyDrops.Award(gameObject.name, Inventory.Instance, RunState.Rng, out string report);
                if (!string.IsNullOrEmpty(report))
                {
                    Debug.Log($"[LOOT] {gameObject.name} dropped {report}");
                }
                if (leftover > 0)
                {
                    Debug.LogWarning($"[LOOT] {leftover} unit(s) did not fit and were lost.");
                    if (UIManager.Instance != null && PlayerStats.Instance != null)
                    {
                        UIManager.Instance.DrawFlowupText($"Inventory full ({leftover} lost)",
                            PlayerStats.Instance.transform.position);
                    }
                }
            }
        }
        Destroy(gameObject);
    }

}

