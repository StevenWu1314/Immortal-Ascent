using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq.Expressions;

public abstract class Enemy : MonoBehaviour
{
    private int health;
    private readonly int damage;

    public void takeDamage(int damage)
    {
        health -= damage;
        TextMesh damageText = utilityFunction.createWorldText("-" + damage.ToString(), null, this.transform.position, 20, Color.white, TextAnchor.MiddleCenter, TextAlignment.Center, 1);
        StartCoroutine(FlowupDamageText(damageText));
        if(health <= 0)
        {
            Die();
        }
    }

    IEnumerator FlowupDamageText(TextMesh damageText)
    {
        for(int i = 0; i < 100; i++)
        {
            damageText.transform.position += new Vector3(0, 0.1f, 0);
            yield return new WaitForSeconds(0.01f);
        }
    }

    private void Die()
    {
        Destroy(gameObject);
    }

}

