using UnityEngine;
using System.Collections;

public abstract class Enemy : MonoBehaviour
{
    [SerializeField] protected int health;
    [SerializeField] protected int damage;
    [SerializeField] protected int expValue;
    private UIManager uiManager;
    SpriteRenderer spriteRenderer;

    void OnEnable()
    {
        uiManager = GameObject.Find("UIManager").GetComponent<UIManager>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    public void takeDamage(int damage)
    {
        health -= damage;
        uiManager.DrawFlowupText("-" + damage, this.transform.position);
        StartCoroutine(DamageIndicator());
        if(health <= 0)
        {
            PlayerStats.Instance.gainExperience(expValue);
            Die();
        }
    }

    private IEnumerator DamageIndicator() {
        float indicatortimer = 0.3f;

        Color[] damageColors = {
            new Color(1f, 0f, 0f, 1f),
            new Color(1f, 1f, 1f, 1f),
        };

        foreach (Color color in damageColors) {
            spriteRenderer.color = color;
            yield return new WaitForSeconds(indicatortimer);
        }

        spriteRenderer.color = Color.white;
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
        }
        Destroy(gameObject);
    }

}

