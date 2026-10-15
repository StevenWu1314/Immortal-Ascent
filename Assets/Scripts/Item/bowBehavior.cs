using UnityEngine;

public class bowBehavior : MonoBehaviour
{
    public Sprite[] states;
    public SpriteRenderer sprite;

    void Start()
    {
        sprite = GetComponentInChildren<SpriteRenderer>();
    }
    // Update is called once per frame
    void Update()
    {
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
