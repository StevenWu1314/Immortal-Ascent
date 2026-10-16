using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Vector2 minBounds; // bottom-left corner of the map
    [SerializeField] private Vector2 maxBounds; // top-right corner of the map

    private float halfHeight;
    private float halfWidth;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        halfHeight = cam.orthographicSize;
        halfWidth = halfHeight * cam.aspect;
    }

    void LateUpdate()
    {
        // FindObjectOfType is null while the level regenerates (the old player is destroyed before the
        // new one spawns), so the null test has to come first. It used to sit *after* the dereference,
        // which made it dead code and threw a NullReferenceException on every descend.
        PlayerStats found = FindObjectOfType<PlayerStats>();
        if (found == null)
        {
            return;
        }

        player = found.transform;
        // target position following the player
        Vector3 targetPos = player.position;

        // clamp within bounds, accounting for camera size
        float clampedX = Mathf.Clamp(targetPos.x, minBounds.x + halfWidth, maxBounds.x - halfWidth);
        float clampedY = Mathf.Clamp(targetPos.y, minBounds.y + halfHeight, maxBounds.y - halfHeight);

        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }
}