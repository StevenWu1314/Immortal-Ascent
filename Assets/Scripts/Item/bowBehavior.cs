using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.UI;

public class bowBehavior : MonoBehaviour
{
    public float mousez;
    public Sprite[] states;
    public SpriteRenderer sprite;

    void Start()
    {
        sprite = GetComponentInChildren<SpriteRenderer>();
    }
    // Update is called once per frame
    void Update()
    {
        // Vector3 dif = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        // dif.Normalize();
        // float rotz = Mathf.Atan2(dif.y, dif.x) * Mathf.Rad2Deg;
        // transform.rotation = quaternion.Euler(0, 0, rotz - 90);
        var target = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        target.z = 0;
        transform.right = target - transform.position;
    }

    public void aimingSprite()
    {
        sprite.sprite = states[1];
    }

    public void restingSprite()
    {
        sprite.sprite = states[0];
    }
}
