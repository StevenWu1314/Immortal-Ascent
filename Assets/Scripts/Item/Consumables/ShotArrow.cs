using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ShotArrow : MonoBehaviour
{
    public Vector3 target;

    public float lifespan = 1;
    public float speed = 12f;

    // Update is called once per frame
    void Update()
    {
        transform.right = target - transform.position;
        float step = speed * Time.deltaTime;
        transform.position = Vector2.MoveTowards(transform.position, target, step);
        lifespan -= Time.deltaTime;
        // The test used to be ">= 0", which destroyed the arrow on its very first frame.
        // Destroy on expiry, or once the arrow has reached its target cell.
        if (lifespan <= 0f || transform.position == target)
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
