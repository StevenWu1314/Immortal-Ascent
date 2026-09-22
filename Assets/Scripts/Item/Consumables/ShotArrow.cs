using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ShotArrow : MonoBehaviour
{
    public Vector3 target;

    public float lifespan = 1;
    public float speed;
    
    // Update is called once per frame
    void Update()
    {
        transform.right = target - transform.position;
        float step = speed * Time.deltaTime;
        transform.position = Vector2.MoveTowards(transform.position, target, step);
        lifespan -= Time.deltaTime;
        if(lifespan >= 0)
        {
            Destroy(gameObject);
        }
    }
    void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.tag != "Player")
            Destroy(gameObject);
    }
}
