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
    public bool swordDrawn = true;
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

    // Cached so aim-mode entry/exit does not repeat a per-frame component search.
    RangeAttackTilemap rangeOverlay;
    Vector3Int lastAimCell;
    GameObject bowIndicator;

    // Start is called before the first frame update
    void Start()
    {
        positionDisplay = GameObject.Find("CurrentPos").GetComponent<TMP_Text>();
        UIManager.openMenu += detectMenu;
        self = gameObject.transform;
        onMoveEvent += checkForCollectables;
        positionDisplay.text = $"{Vector2Int.FloorToInt(transform.position)}";

        var bows = GetComponentsInChildren<bowBehavior>(true);
        if (bows.Length > 0)
        {
            bow = bows[0];
        }
        else
        {
            Debug.LogError("Controls: no bowBehavior found under the player; bow sprites disabled.");
        }

        rangeOverlay = GetComponentInChildren<RangeAttackTilemap>(true);
        if (rangeOverlay == null)
        {
            Debug.LogError("Controls: no RangeAttackTilemap found under the player; ranged attacks disabled.");
        }

        if (transform.childCount > 1)
        {
            bowIndicator = transform.GetChild(1).gameObject;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (MenuIsOpen || takingTurn)
        {
            SetBowSprite(false);
            return;
        }
            
        else if (aiming)
        {
            SetBowSprite(true);

            // Leaving aim mode must always be possible, independently of the turn lock,
            // so a failed shot can never strand the player in aiming mode.
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                LeaveAimMode();
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                Vector3Int clickCell = MouseCell();
                Vector3 cellCenter = rangeOverlay.tilemap.GetCellCenterWorld(clickCell);
                // Resolve the target BEFORE the overlay changes: InRange reads the overlay
                // tiles, so it is only meaningful while the preview is painted.
                Enemy target = IsValidShootTarget(clickCell) ? FindEnemyInCell(clickCell) : null;

                if (target != null)
                {
                    shoot(target, cellCenter);
                    runningCooldown = 0.5f;
                    // Stay in aiming mode so the player can keep firing; re-paint the
                    // preview, which also clears the highlight left on the old cursor cell.
                    lastAimCell = clickCell;
                    RefreshAimOverlay();
                    takingTurn = true;
                    onMoveEvent(this);   // resolve the shot, then let enemies act
                }
                else
                {
                    // Nothing targetable under the cursor: leave aiming so the player is
                    // never stuck, but do not burn a turn.
                    LeaveAimMode();
                    runningCooldown = moveCooldown;
                }
                return;
            }

            // Track the cursor so the preview follows it without leaving stale tiles.
            Vector3Int hovered = MouseCell();
            if (hovered != lastAimCell)
            {
                lastAimCell = hovered;
                RefreshAimOverlay();
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

                if (nextCell == (Vector2Int) portalPos)
                {
                    // Generating the map teleports the player to a new spawn and resets
                    // EntityManager, so the cells computed above become meaningless.
                    // Do the move bookkeeping for the portal cell first, then regenerate
                    // and stop: continuing would move the player a second time from the
                    // new spawn using pre-generation coordinates.
                    EntityManager.Instance.MoveEntity(this.gameObject, currentCell, nextCell);
                    PerlinNoiseMap map = FindFirstObjectByType<PerlinNoiseMap>();
                    if (map != null)
                    {
                        map.GenerateMap();
                    }
                    positionDisplay.text = $"{Vector2Int.FloorToInt(transform.position)}";
                    direction = Vector2Int.zero;
                    return;
                }

                // Island rule: while inside a room that still holds enemies you may not step
                // outside that room. Corridors themselves are never gated, so once the room is
                // clear you can walk out and choose the next one.
                if (!IslandManager.CanMoveTo(transform.position, nextCell))
                {
                    UIManager.Instance.DrawFlowupText(IslandManager.DescribeBlock(transform.position), transform.position);
                    direction = Vector2Int.zero;
                    return;
                }

                if (grid != null && grid.Move(transform.position, direction, transform))
                {
                    EntityManager.Instance.MoveEntity(this.gameObject, currentCell, nextCell);
                    // Only report the destination once the move actually happened.
                    positionDisplay.text = $"{nextCell}";
                }
                else
                {
                    // Terrain blocked the step. Use Tools > Diagnose Map Walkability to
                    // inspect the grid values around the player if this is unexpected.
                }
                direction = Vector2Int.zero;
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                TryEnterAimMode();
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
    /// <summary>Cell currently under the mouse, in range-indicator tilemap space.</summary>
    private Vector3Int MouseCell()
    {
        Camera cam = Camera.main;
        if (cam == null || rangeOverlay == null)
        {
            return new Vector3Int(int.MinValue, int.MinValue, 0);
        }
        Vector3 screen = Input.mousePosition;
        screen.z = -cam.transform.position.z;
        Vector3 world = cam.ScreenToWorldPoint(screen);
        world.z = 0f;
        return rangeOverlay.tilemap.WorldToCell(world);
    }

    /// <summary>
    /// A shot is only legal if the cell is painted as in-range and actually holds an enemy.
    /// Checking the Enemy component here keeps PlayerStats.attack from receiving a null
    /// target (e.g. the cell only contains the player's own collider).
    /// </summary>
    private bool IsValidShootTarget(Vector3Int cell)
    {
        if (rangeOverlay == null || !rangeOverlay.InRange(cell))
        {
            return false;
        }
        return FindEnemyInCell(cell) != null;
    }

    private Item FindArrowStack()
    {
        if (Inventory.Instance == null)
        {
            return null;
        }
        foreach (Item item in Inventory.Instance.getItems())
        {
            if (item == null)
            {
                continue;
            }
            if (item is Arrow && item.getAmount() > 0)
            {
                return item;
            }
        }
        return null;
    }

    /// <summary>
    /// Find the enemy occupying a cell. Prefers a physics hit, but falls back to the
    /// EntityManager registry so the shot does not silently fail if the collider query
    /// misses (collider smaller than the cell, off-centre pivot, etc.).
    /// </summary>
    private Enemy FindEnemyInCell(Vector3Int cell)
    {
        if (rangeOverlay != null && rangeOverlay.tilemap != null)
        {
            Tilemap indicator = rangeOverlay.tilemap;
            Vector3 center = indicator.GetCellCenterWorld(cell);
            Vector2 boxSize = (Vector2)indicator.cellSize * 0.9f;
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, boxSize, 0f);
            foreach (Collider2D hit in hits)
            {
                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy != null)
                {
                    return enemy;
                }
            }
        }

        // Fallback: the cell may be registered to an enemy even when no collider matched.
        if (EntityManager.Instance != null)
        {
            GameObject occupant = EntityManager.Instance.GetEntityAt(new Vector2Int(cell.x, cell.y));
            if (occupant != null)
            {
                return occupant.GetComponent<Enemy>();
            }
        }
        return null;
    }

    /// <summary>
    /// Fire a shot: apply damage, spawn the arrow, consume exactly one arrow.
    /// Returns false when the shot could not be taken at all (no weapon / no ammo).
    /// </summary>
    private bool shoot(Enemy target, Vector3 cellCenter)
    {
        if (target == null)
        {
            return false;
        }

        // Guarded here as well as in IsValidShootTarget: the aiming overlay already
        // requires a ranged weapon, but PlayerStats.attack dereferences it directly.
        if (Equipments.Instance == null || Equipments.Instance.rangeWeapon == null)
        {
            UIManager.Instance.DrawFlowupText("No ranged weapon equipped", transform.position);
            return false;
        }

        Item arrow = FindArrowStack();
        if (arrow == null)
        {
            UIManager.Instance.DrawFlowupText("No arrows remaining", transform.position);
            return false;
        }

        playerStats.attack("range", target);

        // Spawn the projectile. If the reference is somehow unset, fall back to loading the
        // prefab from Resources so the shot is still visible instead of silently missing.
        GameObject arrowPrefab = shotArrow;
        if (arrowPrefab == null)
        {
            arrowPrefab = Resources.Load<GameObject>("ShotArrow");
        }
        if (arrowPrefab == null)
        {
            Debug.LogWarning("Controls: no shotArrow prefab assigned; arrow will not be spawned.");
        }
        else
        {
            var temp = Instantiate(arrowPrefab, transform.position, quaternion.identity);
            ShotArrow projectile = temp.GetComponent<ShotArrow>();
            if (projectile != null)
            {
                projectile.target = cellCenter;
            }
        }

        // Remove exactly one arrow. FindArrowStack returns a single stack, so this never
        // mutates the inventory list while it is being enumerated.
        Inventory.Instance.removeItem(arrow, 1);
        return true;
    }

    private void SetBowSprite(bool isAiming)
    {
        if (bow == null)
        {
            return;
        }
        if (isAiming)
        {
            bow.aimingSprite();
        }
        else
        {
            bow.restingSprite();
        }
    }

    private void RefreshAimOverlay()
    {
        if (rangeOverlay != null)
        {
            rangeOverlay.RefreshOverlay();
        }
    }

    /// <summary>
    /// Enter aiming mode only if a ranged weapon is actually equipped, so the player is
    /// never put into a mode they cannot shoot out of.
    /// </summary>
    private void TryEnterAimMode()
    {
        if (rangeOverlay == null)
        {
            return;
        }
        if (Equipments.Instance == null || Equipments.Instance.rangeWeapon == null)
        {
            UIManager.Instance.DrawFlowupText("No ranged weapon equipped", transform.position);
            return;
        }

        SetBowSprite(true);
        if (bowIndicator != null)
        {
            bowIndicator.SetActive(true);
        }
        aiming = true;
        lastAimCell = MouseCell();
        rangeOverlay.ShowOverlay();
    }

    private void LeaveAimMode()
    {
        aiming = false;
        SetBowSprite(false);
        if (bowIndicator != null)
        {
            bowIndicator.SetActive(false);
        }
        if (rangeOverlay != null)
        {
            rangeOverlay.HideOverlay();
        }
        lastAimCell = new Vector3Int(int.MinValue, int.MinValue, 0);
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
