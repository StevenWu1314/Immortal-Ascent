using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
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

    public bool takingTurn;

    [SerializeField] GameObject shotArrow;

    public static event Action<Controls> onMoveEvent;

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
        // Fog is optional: FogManager ships as code with no scene object and nobody calls its
        // Initialize, so an unguarded call threw a NullReferenceException on every player spawn.
        if (FogManager.Instance != null)
        {
            FogManager.Instance.DiscoveredTiles(Vector2Int.FloorToInt(transform.position));
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

                // Ask the registry where the player is rather than rounding transform.position.
                // The damage shake moves the transform, and a cell that disagrees with the
                // registry made MoveEntity drop the move entirely.
                Vector2Int currentCell;
                if (EntityManager.Instance == null ||
                    !EntityManager.Instance.TryGetCell(gameObject, out currentCell))
                {
                    // Not registered (or no registry): fall back to the transform.
                    currentCell = Vector2Int.FloorToInt(transform.position);
                }
                Vector2Int nextCell = currentCell + direction;

                // Island rule: while inside a room that still holds enemies you may not step
                // outside that room. Corridors themselves are never gated, so once the room is
                // clear you can walk out and choose the next one.
                //
                // The registry cell is passed as a world position on purpose: CanMoveTo rounds
                // its argument, and transform.position can be mid-shake.
                Vector3 currentWorld = new Vector3(currentCell.x, currentCell.y, 0f);
                if (!IslandManager.CanMoveTo(currentWorld, nextCell))
                {
                    UIManager.Instance.DrawFlowupText(IslandManager.DescribeBlock(currentWorld), currentWorld);
                    direction = Vector2Int.zero;
                    return;
                }

                // Terrain is checked from the registry cell for the same reason currentCell is:
                // Grid.Move rounds its position, and the hit shake leaves transform.position up to
                // 0.1 off the cell, which is enough to round to a neighbour.
                if (grid != null && grid.Move(currentWorld, direction))
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

                // Fog reveal for the cell the player just stepped into. The 3-argument grid.Move
                // block that sat here was a merge artifact from the fog branch: it duplicated the
                // movement above, ran after `direction` was zeroed, and had no matching overload.
                if (FogManager.Instance != null)
                {
                    FogManager.Instance.DiscoveredTiles(nextCell);
                }
                
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                TryEnterAimMode();
            }

            // An unopened chest is solid terrain, so the player can only ever be next to one.
            // Opening is deliberately free (it does not fire onMoveEvent), so looting is not a turn.
            if (Input.GetKeyDown(KeyCode.E))
            {
                TryOpenAdjacentChest();
                TryDescendStairs();
            }
        }
        else
        {
            runningCooldown -= Time.deltaTime;
        }

    }
    /// <summary>
    /// The player's cell, preferring the registry over the shaken transform - the same rule the
    /// movement path uses, because a hit shake leaves transform.position up to 0.1 off the cell.
    /// </summary>
    private Vector2Int PlayerCell()
    {
        Vector2Int cell;
        if (EntityManager.Instance != null && EntityManager.Instance.TryGetCell(gameObject, out cell))
        {
            return cell;
        }
        return Vector2Int.FloorToInt(transform.position);
    }

    /// <summary>
    /// E while standing on the stairs descends to the next floor. Free, like opening a chest: the
    /// rebuild is the cost, and the run state refuses once the run is complete.
    /// </summary>
    private void TryDescendStairs()
    {
        if (!RunState.HasRun || !RunState.HasStairs)
        {
            return;
        }
        if (PlayerCell() != RunState.StairsCell)
        {
            return;
        }
        RunState.AdvanceFloor();
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

    private ItemDefinition FindArrowDefinition()
    {
        if (Inventory.Instance == null)
        {
            return null;
        }
        foreach (ItemStack stack in Inventory.Instance.getItems())
        {
            if (stack.definition != null && stack.definition.Kind == ItemKind.Ammo && stack.amount > 0)
            {
                return stack.definition;
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

        ItemDefinition arrow = FindArrowDefinition();
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

        // Remove exactly one arrow by definition, so this never mutates the inventory list
        // while it is being enumerated.
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
        onMoveEvent(this);
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

    /// <summary>
    /// Open the chest next to the player, if any. Reports when there is nothing to open so the
    /// key never feels broken.
    /// </summary>
    private void TryOpenAdjacentChest()
    {
        // Registry cell for the same reason as movement: a mid-shake transform would look for a
        // chest next to the wrong cell.
        Vector2Int cell;
        if (EntityManager.Instance == null ||
            !EntityManager.Instance.TryGetCell(gameObject, out cell))
        {
            cell = Vector2Int.FloorToInt(transform.position);
        }
        if (ChestRegistry.TryGetAdjacent(cell, out Chest chest))
        {
            chest.Open(Inventory.Instance);
            return;
        }
        if (UIManager.Instance != null)
        {
            UIManager.Instance.DrawFlowupText("No chest nearby", transform.position);
        }
    }

    private void OnDestroy()
    {
        UIManager.openMenu -= detectMenu;
    }
}
