using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
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

    public bool takingTurn;

    [SerializeField] GameObject shotArrow;

    public static event Action<Controls> onMoveEvent;
    public static event UnityAction onShootEvent;

    [SerializeField] private bowBehavior bow;
    // Start is called before the first frame update
    void Start()
    {
        positionDisplay = GameObject.Find("CurrentPos").GetComponent<TMP_Text>();
        UIManager.openMenu += detectMenu;
        self = gameObject.transform;
        grid = FindAnyObjectByType<PerlinNoiseMap>().grid;
        Debug.Log(grid);
        onMoveEvent += checkForCollectables;
        positionDisplay.text = $"{Vector2Int.FloorToInt(transform.position)}";
        bow = GetComponentsInChildren<bowBehavior>(true)[0];
        Debug.Log("bow: " + bow);
        FogManager.Instance.DiscoveredTiles(Vector2Int.FloorToInt(transform.position));
    }

    // Update is called once per frame
    void Update()
    {
        if (MenuIsOpen || takingTurn)
        {
            bow.restingSprite();
            return;
        }
            
        else if (aiming)
        {
            bow.aimingSprite();
            if (Input.GetKeyDown(KeyCode.J))
            {
                GameObject bowindicator = transform.GetChild(1).gameObject;
                bowindicator.SetActive(false);
                aiming = false;
                bow.restingSprite();
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
        else if (runningCooldown <= 0)
        {
            direction = Vector2Int.zero;
            if (Input.GetKey(KeyCode.W)) direction = Vector2Int.up;
            else if (Input.GetKey(KeyCode.S)) direction = Vector2Int.down;
            else if (Input.GetKey(KeyCode.D)) direction = Vector2Int.right;
            else if (Input.GetKey(KeyCode.A)) direction = Vector2Int.left;

            if (direction != Vector2Int.zero)
            {
                takingTurn = true;
                onMoveEvent(this);
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
                
                positionDisplay.text = $"{nextCell}";
                direction = Vector2Int.zero;

                if (grid.Move(transform.position, direction, transform)) {
                    EntityManager.Instance.MoveEntity(this.gameObject, currentCell, nextCell);
                }

                FogManager.Instance.DiscoveredTiles(nextCell);
                
            }

            if (Input.GetKeyDown(KeyCode.J))
            {
                takingTurn = true;
                onMoveEvent(this);
                aiming = true;
                bow.aimingSprite();
                GameObject bowindicator = transform.GetChild(1).gameObject;
                bowindicator.SetActive(true);
                transform.GetComponentInChildren<RangeAttackTilemap>().clearPrev();
                transform.GetComponentInChildren<RangeAttackTilemap>().overlay();
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
    private void shoot()
    {
       
        GameObject target = null;
        Tilemap rangeIndicator = transform.GetComponentInChildren<RangeAttackTilemap>(true).tilemap;
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -Camera.main.transform.position.z; // e.g. camera at z=-10, so this = 10
    
        Vector3 worldPoint = Camera.main.ScreenToWorldPoint(mousePos);
        worldPoint.z = 0f; // clamp to world plane just to be safe

        Vector3Int clickCell = rangeIndicator.WorldToCell(worldPoint);
        Vector3 cellCenter = rangeIndicator.GetCellCenterWorld(clickCell);
        Debug.Log($"worldPoint: {worldPoint}, clickCell: {clickCell}, cellCenter: {cellCenter}");
        if(transform.GetComponentInChildren<RangeAttackTilemap>().InRange(clickCell))
        {
            Vector2 boxSize = (Vector2)rangeIndicator.cellSize * 0.9f;
            Collider2D hit = Physics2D.OverlapBox(cellCenter, boxSize, 0f);
            if(hit != null)
            {

                Debug.Log("Target Found");
                target = hit.gameObject;
            }
        }
        if (target != null)
        {
            bool hasArrow = false;
            List<Item> items = Inventory.Instance.getItems();
            foreach(Item item in items)
            {
                if(item.getName() == "Arrow")
                {
                    hasArrow = true;
                }
            }
            if(hasArrow)
            {
                playerStats.attack("range", target.GetComponent<Enemy>());
                var temp = Instantiate(shotArrow, transform.position, quaternion.identity);
                temp.GetComponent<ShotArrow>().target = cellCenter;
                foreach(Item item in items)
                {
                    if(item.getName() == "Arrow")
                    {
                        Inventory.Instance.removeItem(item, 1);
                    }
                }
            }
            else
                UIManager.Instance.DrawFlowupText("No arrows remaining", transform.position);
            takingTurn = true;
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
