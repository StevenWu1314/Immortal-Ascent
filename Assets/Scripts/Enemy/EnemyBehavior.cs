using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class EnemyBehavior : MonoBehaviour
{
    public Grid grid;
    protected GameObject player;
    public float detectionRadius;

    public Vector3 spawnPos;
    [SerializeField] protected int chaseRange;

    /// <summary>
    /// Idle (which now includes roaming), Chase, Return. The field name and the enum's member order are
    /// unchanged, so serialized <c>currentstate</c> values still mean what they meant.
    /// </summary>
    public AwarenessState currentstate;

    private int homeIsland = int.MinValue;

    /// <summary>
    /// The island this enemy spawned on. Resolved lazily because islands are built after
    /// enemies are spawned. An enemy never leaves its own island, so it can never wander
    /// across a bridge onto the player's island (which would lock the player out of leaving).
    /// </summary>
    protected int HomeIsland
    {
        get
        {
            // Only cache once the island map exists. Caching -1 while islands are still being
            // built would permanently convince this enemy it has no island.
            if (homeIsland == int.MinValue && IslandManager.IsInitialized)
            {
                homeIsland = IslandManager.GetIsland(transform.position);
            }
            return homeIsland == int.MinValue ? -1 : homeIsland;
        }
    }

    /// <summary>
    /// May this enemy step onto <paramref name="cell"/>? Combines the terrain check with the
    /// island confinement rule so both chase and return use one gate.
    /// </summary>
    protected bool CanStepOnto(Vector2Int cell)
    {
        if (grid == null)
        {
            return false;
        }
        if (!Grid.IsWalkableTerrain(grid.GetValueAtLocation(cell.x, cell.y)))
        {
            return false;
        }
        // HomeIsland < 0 means this enemy spawned on a non-walkable cell, so it has no island
        // to be confined to; fall back to the terrain check alone rather than wandering freely.
        if (HomeIsland < 0)
        {
            return true;
        }
        // Stay within the room itself. This allows pursuing the player through a doorway the
        // corridor carved open, while never stepping into the corridor beyond it.
        return IslandManager.IsPartOfRoom(cell, HomeIsland);
    }

    protected void OnEnable()
    {
        Controls.onMoveEvent += queup;
        // The level grid has exactly one owner. Read it from Grid.Current instead of
        // searching the scene for a specific generator: the world-map scene has no
        // RoomFirstDungeonGenerator at all, and its grid uses different dimensions
        // and origin from the dungeon generator's grid.
        if (grid == null)
        {
            grid = Grid.Current;
        }
        spawnPos = this.transform.position;
    }

    

    protected void OnDestroy() {
        Controls.onMoveEvent -= queup;
        // Catch-all for every destruction path (killed by the player, a regeneration sweep,
        // scene teardown). Without this the queue kept a destroyed component and TakeTurns()
        // called takeTurn() on it, throwing MissingReferenceException and aborting the
        // coroutine for every enemy still queued behind it.
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.UnregisterEnemy(this);
        }
    }
    virtual protected void Start()
    {
        player = GameObject.FindWithTag("Player");
        currentstate = AwarenessState.Idle;
        ApplyDefinition();
    }

    /// <summary>
    /// Take the awareness numbers from this enemy's definition when it has one. The prefab's serialized values
    /// are the fallback, so nothing breaks before the definitions are authored.
    /// </summary>
    protected void ApplyDefinition()
    {
        Enemy self = GetComponent<Enemy>();
        EnemyDefinition definition = self != null ? self.Definition : null;
        if (definition == null)
        {
            return;
        }
        detectionRadius = definition.detectionRadius;
        chaseRange = definition.chaseRange;
    }
    /// <summary>
    /// Re-evaluate whether this enemy notices the player (GDD 6.5). The realm decides how: Plains alerts
    /// the whole room, desert and tundra need the detection radius plus a clear line of sight.
    /// </summary>
    protected void updateAwareness()
    {
        if (NoticesPlayer())
        {
            becomeAlerted();
        }
    }

    /// <summary>Does this enemy see the player right now? Shared by the per-move check and its own turn.</summary>
    protected bool NoticesPlayer()
    {
        if (player == null)
        {
            return false;
        }

        int playerIsland = IslandManager.IsInitialized
            ? IslandManager.GetIsland(player.transform.position)
            : IslandManager.Unassigned;
        bool hasSight = LineOfSight.HasClearShot(
            grid, CellOf(transform.position), CellOf(player.transform.position));

        return Awareness.Notices(
            RunState.EnemyAggroMode,
            HomeIsland,
            playerIsland,
            Vector3.Distance(transform.position, player.transform.position),
            detectionRadius,
            hasSight);
    }

    protected static Vector2Int CellOf(Vector3 position)
    {
        return new Vector2Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
    }
    /// <summary>
    /// Start chasing. This used to unsubscribe checkForPlayerInRoom, which meant an enemy that
    /// drifted home mid-fight stopped noticing the player until it reached Idle again; queup now
    /// re-checks every move instead, so there is nothing to unsubscribe.
    /// </summary>
    public void becomeAlerted()
    {
        currentstate = AwarenessState.Chase;
    }
    /// <summary>
    /// The single always-on handler for a player move: wake up if the player is on this enemy's
    /// island, then queue for a turn if engaged. These were two subscriptions, and the alerting
    /// one unsubscribed itself the moment the enemy engaged.
    /// </summary>
    private void queup(Controls controls)
    {
        updateAwareness();

        // Every enemy takes a turn, unaware ones included: that is when they roam (GDD 6.6). The phase
        // only runs when the player actually spends a turn, so free out-of-combat travel still costs no time.
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.queupToMove(this);
        }
    }

    public virtual void takeTurn()
    {
        // Safety net: if this enemy somehow ends up off its own island (knockback, a bug in
        // the movement code), walk it home instead of leaving it stranded on the player's
        // island where it would lock the exit.
        if (IslandManager.IsInitialized && HomeIsland >= 0)
        {
            int here = IslandManager.GetIsland(transform.position);
            if (here >= 0 && here != HomeIsland)
            {
                returnToSpawn();
                return;
            }
        }

        bool notices = NoticesPlayer();
        bool atPost = transform.position == spawnPos;
        bool beyondRange = Vector3.Distance(transform.position, spawnPos) > chaseRange;
        // Radius realms also break off when the player slips out of sight. Room-wide ones do not: "in the
        // room" is already the entire condition there.
        bool lostSight = RunState.EnemyAggroMode == AggroMode.DetectionRadius && !notices;

        currentstate = Awareness.Next(currentstate, notices, beyondRange || lostSight, atPost);

        switch (currentstate)
        {
            case AwarenessState.Chase:
                chasePlayer();
                break;

            case AwarenessState.Return:
                returnToSpawn();
                break;

            default:
                // Unaware: wander rather than stand still forever (GDD 6.6).
                roamOneStep();
                break;
        }
    }

    /// <summary>
    /// One wandering step for an unaware enemy: inside its own island, never onto an occupied cell, and it
    /// never attacks. The choice itself is pure, in <see cref="Roaming"/>, so it is tested without a scene.
    /// </summary>
    protected virtual void roamOneStep()
    {
        Vector2Int here = CellOf(transform.position);
        Vector2Int step = Roaming.ChooseStep(
            here,
            UnityEngine.Random.Range(0, 4),
            CanStepOnto,
            cell => EntityManager.Instance != null && EntityManager.Instance.IsOccupied(cell));

        if (step == Vector2Int.zero)
        {
            return;   // boxed in: hold still rather than teleporting or attacking
        }

        if (EntityManager.Instance != null)
        {
            EntityManager.Instance.MoveEntity(gameObject, here, here + step);
        }
    }
    /// <summary>
    /// Preferred then fallback single step toward <paramref name="to"/>: the dominant axis first.
    /// Shared by chase and return, which each carried their own copy of this.
    /// </summary>
    protected static Vector2Int[] NextSteps(Vector2Int from, Vector2Int to)
    {
        Vector2Int dir = to - from;
        Vector2Int horizontal = new Vector2Int(Mathf.Sign(dir.x) > 0 ? 1 : -1, 0);
        Vector2Int vertical = new Vector2Int(0, Mathf.Sign(dir.y) > 0 ? 1 : -1);
        return Mathf.Abs(dir.x) > Mathf.Abs(dir.y)
            ? new[] { horizontal, vertical }
            : new[] { vertical, horizontal };
    }

    protected virtual void chasePlayer()
    {
        Vector2Int enemyCell = new Vector2Int(
            Mathf.FloorToInt(transform.position.x),
            Mathf.FloorToInt(transform.position.y)
        );
        Vector2Int playerCell = new Vector2Int(
            Mathf.FloorToInt(player.transform.position.x),
            Mathf.FloorToInt(player.transform.position.y)
        );

        Vector2Int[] steps = NextSteps(enemyCell, playerCell);

        foreach (var step in steps)
        {
            Vector2Int nextCell = enemyCell + step;
            // Step 1: walkable terrain AND still on this enemy's own island.
            if (!CanStepOnto(nextCell)) continue; // try next direction

            // Step 2: check entity layer
            if (EntityManager.Instance.IsOccupied(nextCell))
            {
                GameObject other = EntityManager.Instance.GetEntityAt(nextCell);

                if (other.CompareTag("Player"))
                {
                    this.GetComponent<Enemy>().Attack(player.GetComponent<PlayerStats>());

                }
                else
                {
                    continue;
                }
            }
            else
            {
                // Move and update entity manager
                EntityManager.Instance.MoveEntity(gameObject, enemyCell, nextCell);
            }

            return; // stop after first valid move
        }
    }
    
    protected virtual void returnToSpawn()
    {
        Vector2Int enemyCell = new Vector2Int(
            Mathf.FloorToInt(transform.position.x),
            Mathf.FloorToInt(transform.position.y)
        );
        Vector2Int spawnCell = new Vector2Int(
            Mathf.FloorToInt(spawnPos.x),
            Mathf.FloorToInt(spawnPos.y)
        );

        Vector2Int[] steps = NextSteps(enemyCell, spawnCell);

        foreach (var step in steps)
        {
            Vector2Int nextCell = enemyCell + step;
            // Step 1: walkable terrain AND still on this enemy's own island.
            if (!CanStepOnto(nextCell)) continue; // try next direction

            // Step 2: check entity layer
            if (EntityManager.Instance.IsOccupied(nextCell))
            {
                GameObject other = EntityManager.Instance.GetEntityAt(nextCell);

                if (other.CompareTag("Player"))
                {
                    this.GetComponent<Enemy>().Attack(player.GetComponent<PlayerStats>());

                }
            }
            else
            {
                // Move and update entity manager
                EntityManager.Instance.MoveEntity(gameObject, enemyCell, nextCell);
            }

            return; // stop after first valid move
        }
    }
}
