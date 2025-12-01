using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Controls : MonoBehaviour
{
    Transform self;
    public Grid grid;
    Vector2Int direction;
    bool aiming = false;
    [SerializeField] private float moveCooldown = 0.1f;
    [SerializeField] private float runningCooldown = 0;
    [SerializeField] private bool MenuIsOpen;
    [SerializeField] private PlayerStats playerStats;
    Collectables collectable;
    public GameObject CollectButton;
    public static event Action<Controls> onMoveEvent;
    public static event UnityAction onShootEvent;
    // Start is called before the first frame update
    void Start()
    {
        UIManager.openMenu += detectMenu;
        self = gameObject.transform;
        grid = FindAnyObjectByType<PerlinNoiseMap>().grid;
        Debug.Log(grid);
        onMoveEvent += checkForCollectables;
        CollectButton = GameObject.Find("Collect");
        CollectButton.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (MenuIsOpen) return;

        if (aiming)
        {
            // Handle aiming mode
            if (Input.GetKeyDown(KeyCode.W)) aim(KeyCode.W);
            else if (Input.GetKeyDown(KeyCode.A)) aim(KeyCode.A);
            else if (Input.GetKeyDown(KeyCode.S)) aim(KeyCode.S);
            else if (Input.GetKeyDown(KeyCode.D)) aim(KeyCode.D);
            else if (Input.GetKeyDown(KeyCode.L)) aim(KeyCode.L);
            return;
        }

        // --- Normal movement mode ---
        if (runningCooldown <= 0)
        {
            direction = Vector2Int.zero;
            if (Input.GetKey(KeyCode.W)) direction = Vector2Int.up;
            else if (Input.GetKey(KeyCode.S)) direction = Vector2Int.down;
            else if (Input.GetKey(KeyCode.D)) direction = Vector2Int.right;
            else if (Input.GetKey(KeyCode.A)) direction = Vector2Int.left;

            if (direction != Vector2Int.zero)
            {
                runningCooldown = moveCooldown;
                Vector2Int currentCell = Vector2Int.FloorToInt(transform.position);
                Vector2Int nextCell = currentCell + direction;
                if (grid.Move(transform.position, direction, transform))
                {
                    EntityManager.Instance.MoveEntity(this.gameObject, currentCell, nextCell);
                }
                onMoveEvent(this);
                direction = Vector2Int.zero;
            }

            if (Input.GetKeyDown(KeyCode.J))
            {
                aiming = true;
                print("Entered aiming mode");
            }
        }
        else
        {
            runningCooldown -= Time.deltaTime;
        }

        // if (Input.GetKeyUp(KeyCode.Space))
        // {
        //     grid.printGrid();
        // }

    }
    IEnumerator WaitforKeyPress(Action<KeyCode> callback)
    {
        KeyCode pressedKey = KeyCode.None;

        while (pressedKey == KeyCode.None)
        {
            if (Input.GetKeyDown(KeyCode.W)) pressedKey = KeyCode.W;
            else if (Input.GetKeyDown(KeyCode.A)) pressedKey = KeyCode.A;
            else if (Input.GetKeyDown(KeyCode.S)) pressedKey = KeyCode.S;
            else if (Input.GetKeyDown(KeyCode.D)) pressedKey = KeyCode.D;
            else if (Input.GetKeyDown(KeyCode.L)) pressedKey = KeyCode.L;
            yield return null; // Wait for next frame
        }
        callback?.Invoke(pressedKey);
    }
    private void aim(KeyCode key)
    {
        if (key == KeyCode.L)
        {
            aiming = false;
            return;
        }
        GameObject target = null;
        Vector2Int offset = Vector2Int.zero;
        Vector2Int currentPos = Vector2Int.FloorToInt(transform.position);
        if (key == KeyCode.W) offset = Vector2Int.up;
        else if (key == KeyCode.S) offset = Vector2Int.down;
        else if (key == KeyCode.D) offset = Vector2Int.right;
        else if (key == KeyCode.A) offset = Vector2Int.left;

        if (offset == Vector2Int.zero) return;
        for (int i = 1; i <= 5; i++)
        {
            Vector2Int checkPos = currentPos + offset * i;
            GameObject entity = EntityManager.Instance.GetEntityAt(checkPos);
            if (entity != null && entity.GetComponent<Enemy>() != null)
            {
                target = entity;
                break; // stop at first enemy in line of sight
            }
        }
        if (target != null)
        {
            playerStats.attack("range", target.GetComponent<Enemy>());
            onMoveEvent(this);
        }
        else
        {
            print("no target found");
        }
        aiming = false;
        runningCooldown = 0.5f;
    }
    private void detectMenu(UIManager uIManager)
    {
        MenuIsOpen = !MenuIsOpen;
    }

    private void checkForCollectables(Controls controls)
    {
        Vector2Int currentCell = Vector2Int.FloorToInt(transform.position);
        if (collectable != null)
        {
            collectable = null;
        }
        collectable = CollectableMap.Instance.GetCollectableAt(currentCell);
        if (collectable != null)
        {
            collectable.collectThis();
            CollectableMap.Instance.unregisterCollectable(currentCell);
            CollectButton.SetActive(false);
            PerlinNoiseMap map = GameObject.Find("map generator").GetComponent<PerlinNoiseMap>();
            map.setToGrass(currentCell);

        }
    }

}
