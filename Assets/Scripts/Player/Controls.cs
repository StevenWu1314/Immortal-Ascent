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
    [SerializeField] private float moveCooldown = 0.1f;
    [SerializeField] private float runningCooldown = 0;
    [SerializeField] public bool MenuIsOpen;
    [SerializeField] private PlayerStats playerStats;
    public TMP_Text positionDisplay;

    public bool takingTurn;

    /// <summary>True while the bow is drawn. Selecting a weapon is free; only firing costs a turn.</summary>
    public bool IsAiming => aiming;

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

            // Leaving aim mode must always be possible, independently of the turn lock, so a failed
            // shot can never strand the player in aiming mode. Key 1 selects melee, key 2 the bow.
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Alpha1))
            {
                LeaveAimMode();
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                Vector3Int clickCell = MouseCell();
                if (!TryFireAt(clickCell))
                {
                    // Nothing targetable under the cursor: leave aiming so the player is never stuck, but do
                    // not burn a turn. A refused *shot* (no line, no arrows) keeps the player aiming, because
                    // LeaveAimMode is only right when there was nothing to shoot at.
                    if (!IsValidShootTarget(clickCell))
                    {
                        LeaveAimMode();
                        runningCooldown = moveCooldown;
                    }
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
                // The cooldown paces a held key. Whether the step costs a turn is TryMove's decision,
                // and a step that was refused is free.
                runningCooldown = moveCooldown;
                TryMove(direction);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                TryEnterAimMode();
            }
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                LeaveAimMode();
            }

            // An unopened chest is solid terrain, so the player can only ever be next to one.
            // Opening is deliberately free (it does not fire onMoveEvent), so looting is not a turn.
            if (Input.GetKeyDown(KeyCode.E))
            {
                // Mining first: a vein is solid, so it is always worked from a neighbouring cell, and the
                // chest message must not fire on the same press.
                if (!TryMineAdjacentVein())
                {
                    TryOpenAdjacentChest();
                }
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
    /// Take one step, or attack an enemy standing in the destination cell. Returns true when the action
    /// happened at all.
    ///
    /// A refused step is free: no turn event, no enemy phase, no status tick. That is the whole point -
    /// wall bumps and gated exits used to consume a turn because the event was raised before anything
    /// was validated.
    ///
    /// Public because this is the entire movement path: <see cref="Update"/> feeds it the keys and the
    /// editor PlayDriver drives it, so both exercise the same validation.
    /// </summary>
    public bool TryMove(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
        {
            return false;
        }

        // The registry cell, not the transform: the damage shake moves the transform up to 0.1, which
        // is enough to round to a neighbouring cell.
        Vector2Int currentCell = PlayerCell();
        Vector2Int nextCell = currentCell + direction;
        // IslandManager rounds its argument, and transform.position can be mid-shake, so the registry
        // cell is passed as a world position.
        Vector3 currentWorld = new Vector3(currentCell.x, currentCell.y, 0f);

        bool rooted = playerStats != null && playerStats.Statuses != null && playerStats.Statuses.IsRooted;

        // An enemy in the destination is attacked rather than walked into, and attacking stays legal while
        // rooted (GDD 6.2) - so the bump is resolved before the movement gate. This used to live inside
        // EntityManager.MoveEntity, which made the registry the owner of an action-economy decision.
        GameObject occupant = EntityManager.Instance != null ? EntityManager.Instance.GetEntityAt(nextCell) : null;
        Enemy enemy = occupant != null ? occupant.GetComponent<Enemy>() : null;
        if (enemy != null)
        {
            if (Equipments.Instance == null || Equipments.Instance.meleeWeapon == null)
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.DrawFlowupText("No melee weapon equipped", transform.position);
                }
                return false;   // nothing happened, so it costs nothing
            }

            playerStats.attack("melee", enemy);
            ConsumeTurn(TurnAction.MeleeAttack);
            return true;
        }

        bool islandAllows = IslandManager.CanMoveTo(currentWorld, nextCell);
        bool terrainWalkable = grid != null && grid.Move(currentWorld, direction);
        if (!TurnRules.IsMovementAllowed(terrainWalkable, islandAllows, rooted))
        {
            if (rooted)
            {
                // A rooted player who tries to walk is struggling in the web rather than failing at a wall,
                // so the round passes. Otherwise a web on an otherwise empty island would leave them no way
                // to spend the turn that Root needs in order to expire - a soft lock.
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.DrawFlowupText("Rooted", transform.position);
                }
                ConsumeTurn(TurnAction.Move);
                return false;
            }

            // Only the island gate explains itself; being blocked by a wall needs no caption.
            if (!islandAllows && UIManager.Instance != null)
            {
                UIManager.Instance.DrawFlowupText(IslandManager.DescribeBlock(currentWorld), currentWorld);
            }
            return false;
        }

        if (EntityManager.Instance == null ||
            !EntityManager.Instance.MoveEntity(gameObject, currentCell, nextCell))
        {
            return false;
        }

        // Only report the destination once the move actually happened.
        positionDisplay.text = $"{nextCell}";

        // The minimap learns where the player has been (T08); guarded like FogManager, since a scene with no
        // minimap is perfectly valid.
        if (Minimap.Instance != null)
        {
            Minimap.Instance.Reveal(nextCell);
        }

        // Entering a web roots the player and consumes it (GDD 5.4). Consume owns both writes: the grid for
        // movement authority and the island snapshot for the gate.
        if (WebRegistry.HasWeb(nextCell))
        {
            WebRegistry.Consume(nextCell, grid);
            if (playerStats != null && playerStats.Statuses != null)
            {
                playerStats.Statuses.Apply(StatusKind.Root, 0, StatusController.RootRounds);
            }
            if (UIManager.Instance != null)
            {
                UIManager.Instance.DrawFlowupText("Rooted", transform.position);
            }
        }

        // Fog reveal for the cell the player just stepped into. Guarded: FogManager ships with no
        // scene object, so an unguarded call threw on every move.
        if (FogManager.Instance != null)
        {
            FogManager.Instance.DiscoveredTiles(nextCell);
        }

        // Free movement out of combat: while no living enemy shares the room, travelling costs nothing.
        if (IsTurnBasedHere())
        {
            ConsumeTurn(TurnAction.Move);
        }
        else if (TurnManager.Instance != null)
        {
            // Free travel spends no round, so the world would otherwise stand still while the player explores
            // and no unaware enemy would ever be seen roaming (GDD 6.6). The pulse is paced by time.
            TurnManager.Instance.RoamPulse();
        }
        return true;
    }

    /// <summary>
    /// Fire at a cell in the aim overlay's own space: validate the target, re-check the line of sight, spend an
    /// arrow and resolve the shot.
    ///
    /// Public for the same reason <see cref="TryMove"/> is - this is the whole shooting path, so the PlayDriver
    /// and the tests drive the real code rather than a copy.
    /// </summary>
    public bool TryFireAt(Vector3Int clickCell)
    {
        if (rangeOverlay == null || rangeOverlay.tilemap == null)
        {
            return false;
        }

        // Resolve the target BEFORE the overlay changes: InRange reads the overlay tiles, so it is only
        // meaningful while the preview is painted.
        Enemy target = IsValidShootTarget(clickCell) ? FindEnemyInCell(clickCell) : null;
        if (target == null)
        {
            return false;
        }

        Vector3 cellCenter = rangeOverlay.tilemap.GetCellCenterWorld(clickCell);

        // The overlay's cells are player-relative, so they must be converted before they are compared with the
        // terrain grid - passing an overlay cell straight in tested a cell nowhere near the target, which is
        // what made every shot report "no line of sight".
        Vector2Int levelTarget = Grid.CellFromWorld(cellCenter);
        if (!LineOfSight.HasClearShot(grid, PlayerCell(), levelTarget))
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.DrawFlowupText("No line of sight", transform.position);
            }
            return false;
        }

        // Only a shot that actually left the bow costs a turn; an empty quiver is a no-op.
        if (!shoot(target, cellCenter))
        {
            return false;
        }

        runningCooldown = 0.5f;
        // Stay in aiming mode so the player can keep firing; re-paint the preview, which also clears the
        // highlight left on the old cursor cell.
        lastAimCell = clickCell;
        RefreshAimOverlay();
        ConsumeTurn(TurnAction.BowShot);
        return true;
    }

    /// <summary>
    /// Spend the turn for an action that already resolved (using an item, for example). Movement and attacks go
    /// through <see cref="TryMove"/>; everything else comes here, so the turn table stays the single gate.
    /// </summary>
    public void SpendTurn(TurnAction action)
    {
        ConsumeTurn(action);
    }

    /// <summary>True while a living enemy shares the player's room (GDD 6.1).</summary>
    private bool IsTurnBasedHere()
    {
        int island = IslandManager.GetIsland(PlayerCell());
        return TurnRules.IsTurnBased(island, IslandManager.CountLivingEnemies(island));
    }

    /// <summary>
    /// End the player's action: lock input and let the turn pipeline run. Only a real action reaches
    /// here, and the table is the guard, so a free action can never sneak an enemy phase in.
    /// </summary>
    private void ConsumeTurn(TurnAction action)
    {
        if (!TurnRules.ConsumesTurn(action))
        {
            return;
        }
        takingTurn = true;
        onMoveEvent(this);
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

        // Fallback: the cell may be registered to an enemy even when no collider matched. The overlay's
        // cells are relative to its own Grid under the player, so convert to a level cell first - looking
        // the overlay cell up directly queried the wrong cell.
        if (EntityManager.Instance != null)
        {
            Vector2Int levelCell = rangeOverlay != null && rangeOverlay.tilemap != null
                ? Grid.CellFromWorld(rangeOverlay.tilemap.GetCellCenterWorld(cell))
                : new Vector2Int(cell.x, cell.y);
            GameObject occupant = EntityManager.Instance.GetEntityAt(levelCell);
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

        // Arrows are a count on the player (T06), spent here so a shot that cannot be paid for never lands.
        if (playerStats == null || !playerStats.TryConsumeArrow())
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

        // The arrow was already spent above, one per shot.
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

        // Arrows are a count now (T06), so an empty quiver is refused up front instead of after the player
        // has lined up a shot they cannot take.
        if (playerStats == null || playerStats.Arrows <= 0)
        {
            UIManager.Instance.DrawFlowupText("No arrows remaining", transform.position);
            return;
        }

        SetBowSprite(true);
        if (bowIndicator != null)
        {
            bowIndicator.SetActive(true);
        }
        // Free: entering aim mode ends no turn, locks no input and lets no enemy act. It used to raise
        // the turn event, which handed the enemies a free round while leaving input unlocked - so the
        // player could act again during that phase and two enemy phases could run at once.
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

    /// <summary>
    /// Mine an adjacent ore vein (GDD 5.5): one turn, 1-3 ore, and the vein depletes. Returns false only when
    /// there is nothing to mine, so the caller can fall through to the chest interaction.
    ///
    /// Room is checked for the largest possible yield first, and a vein is not touched when the ore would not
    /// fit - ground drops are not built yet, and silently destroying a finite resource is worse than refusing.
    /// </summary>
    public bool TryMineAdjacentVein()
    {
        if (!OreVeinRegistry.TryGetAdjacent(PlayerCell(), out Vector2Int cell))
        {
            return false;
        }

        ItemDefinition ore = OreVeinRegistry.OreAt(cell);
        if (Inventory.Instance != null && ore != null && !Inventory.Instance.hasRoomFor(ore, 3))
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.DrawFlowupText("No room for ore", transform.position);
            }
            return true;   // handled: the press explained itself, and no turn was spent
        }

        if (!OreVeinRegistry.TryMine(cell, grid, RunState.Rng, out ore, out int yielded))
        {
            return false;
        }

        int leftover = Inventory.Instance != null ? Inventory.Instance.addItem(ore, yielded) : 0;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.DrawFlowupText(leftover > 0 ? $"Inventory full ({leftover} lost)" : $"+{yielded} {ore.DisplayName}",
                transform.position);
        }
        if (leftover > 0)
        {
            Debug.LogWarning($"[MINE] {leftover} x {ore.DisplayName} did not fit and were lost.");
        }

        ConsumeTurn(TurnAction.MineOre);
        return true;
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
