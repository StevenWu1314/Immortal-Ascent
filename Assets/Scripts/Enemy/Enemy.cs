using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq.Expressions;
using UnityEngine.Rendering;

public abstract class Enemy : MonoBehaviour
{
    [SerializeField] protected int health;
    protected int damage;
    [SerializeField] protected int expValue;
    private UIManager uiManager;

    void OnEnable()
    {
        uiManager = GameObject.Find("Manager").GetComponent<UIManager>();
    }
    public void takeDamage(int damage)
    {
        health -= damage;
        uiManager.DrawFlowupDamageText(damage, this.transform.position);
        if(health <= 0)
        {
            Manager.player.gainExperience(expValue);
            Die();
        }
    }

    public void attack(PlayerStats target)
    {
        target.takeDamage(damage);
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

    private void Die()
    {
        EntityManager.Instance.UnregisterEntity(this.gameObject, Vector2Int.FloorToInt(transform.position));
        LootGen.Instance.rollTable();
        Debug.Log("enemy eliminated");
        Destroy(gameObject);
        
    }

}

