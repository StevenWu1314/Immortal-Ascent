using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class Controls : MonoBehaviour
{
    Transform self;
    public Grid grid;
    Vector2Int direction;
    bool aiming = false;
    [SerializeField] private float moveCooldown = 0.1f;
    [SerializeField] private float runningCooldown = 0;
    [SerializeField] public bool MenuIsOpen;
    [SerializeField] private PlayerStats playerStats;
    public TMP_Text positionDisplay;
    Collectables collectable;
    public GameObject CollectButton;
    internal Vector3Int portalPos;

    public static event Action<Controls> onMoveEvent;
    public static event UnityAction onShootEvent;
    // Start is called before the first frame update
    void Start()
    {
        positionDisplay = GameObject.Find("CurrentPos").GetComponent<TMP_Text>();
        UIManager.openMenu += detectMenu;
        self = gameObject.transform;
        grid = FindAnyObjectByType<PerlinNoiseMap>().grid;
        Debug.Log(grid);
        onMoveEvent += checkForCollectables;
        CollectButton = GameObject.Find("Collect");
        CollectButton.SetActive(false);
        positionDisplay.text = $"{Vector2Int.FloorToInt(transform.position)}";
    }

    // Update is called once per frame
    void Update()
    {
        if (MenuIsOpen) return;

        if (aiming)
        {
            if (Input.GetKeyDown(KeyCode.J))
            {
                GameObject bowindicator = transform.GetChild(1).gameObject;
                bowindicator.SetActive(false);
                aiming = false;
                transform.GetComponentInChildren<RangeAttackTilemap>().clearPrev();
                transform.GetComponentInChildren<RangeAttackTilemap>().overlaying = false;
                print("exiting aiming mode");
                return;
            }
            else if(Input.GetMouseButtonDown(0))
            {
                Debug.Log("leftClickDetected");
                shoot();
            }
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
                if(nextCell == (Vector2Int) portalPos)
                {
                    FindFirstObjectByType<PerlinNoiseMap>().GenerateMap();
                }
                if (grid.Move(transform.position, direction, transform))
                {
                    EntityManager.Instance.MoveEntity(this.gameObject, currentCell, nextCell);
                }
                onMoveEvent(this);
                positionDisplay.text = $"{nextCell}";
                direction = Vector2Int.zero;
            }

            if (Input.GetKeyDown(KeyCode.J))
            {
                aiming = true;
                GameObject bowindicator = transform.GetChild(1).gameObject;
                bowindicator.SetActive(true);
                transform.GetComponentInChildren<RangeAttackTilemap>().overlay();
                print("Entered aiming mode");
                onMoveEvent(this);
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
    private void shoot()
    {
       
        GameObject target = null;
        Tilemap rangeIndicator = transform.GetComponentInChildren<RangeAttackTilemap>(true).tilemap;
        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int clickCell = rangeIndicator.WorldToCell(worldPoint);
        Vector3 cellcenter = rangeIndicator.GetCellCenterWorld(clickCell);
        if(transform.GetComponentInChildren<RangeAttackTilemap>().InRange(clickCell))
        {
            Collider2D hit = Physics2D.OverlapCircle(cellcenter, 0.1f);
            Debug.Log(cellcenter);
            if(hit != null)
            {
                Debug.Log("Target Found");
                target = hit.gameObject;
            }
        }
        if (target != null)
        {
            playerStats.attack("range", target.GetComponent<Enemy>());
            onMoveEvent(this);
        }
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

    private void OnDestroy()
    {
        onMoveEvent -= checkForCollectables;
        UIManager.openMenu -= detectMenu;
    }
}
