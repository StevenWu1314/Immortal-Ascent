using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using Vector3 = UnityEngine.Vector3;

public class EnemyBehavior : MonoBehaviour
{
    public Grid grid;
    protected GameObject player;
    public float detectionRadius;
    protected bool playerDetected;

    public BoundsInt room;

    public Vector3 spawnPos;
    [SerializeField] protected int chaseRange;

    public enum state {Idle, Chase, Return}
    public state currentstate;

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
        Controls.onMoveEvent += checkForPlayerInRoom;
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
        Controls.onMoveEvent -= checkForPlayerInRoom;
        Controls.onMoveEvent -= queup;
    }
    virtual protected void Start()
    {

        player = GameObject.FindWithTag("Player");
        currentstate = state.Idle;

    }
    protected void checkForPlayerInRoom(Controls controls)
    {
        if (currentstate != state.Idle || player == null)
        {
            return;
        }

        // Islands are the authority when available: this enemy engages only when the player
        // has actually set foot on the island this enemy spawned on.
        if (IslandManager.IsInitialized)
        {
            int home = HomeIsland;
            if (home >= 0)
            {
                if (IslandManager.GetIsland(player.transform.position) == home)
                {
                    becomeAlerted();
                }
                return;
            }
        }

        // Fallback used only when islands are unavailable, so a level generated without them
        // still has some way to wake enemies up.
        if (detectionRadius > 0f &&
            Vector3.Distance(transform.position, player.transform.position) <= detectionRadius)
        {
            becomeAlerted();
        }
    }

    protected void notifyNearbyEnemies()
    {
        Collider2D[] objectInRange = Physics2D.OverlapCircleAll(transform.position, detectionRadius/2);
        List<EnemyBehavior> others = (from unit in objectInRange where unit.GetComponent<EnemyBehavior>() != null select unit.GetComponent<EnemyBehavior>()).ToList();
        foreach(EnemyBehavior other in others)
        {
            other.becomeAlerted();
            
        }
    }

    public void becomeAlerted()
    {
        Controls.onMoveEvent -= checkForPlayerInRoom;
        currentstate = state.Chase;
    }
    private void queup(Controls controls)
    {
        if(currentstate != state.Idle)
            TurnManager.Instance.queupToMove(this);
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

        if (currentstate == state.Idle)
        {
            return;
        }
        else if (currentstate == state.Chase)
        {
            chasePlayer();
            if (Vector3.Distance(transform.position, spawnPos) > chaseRange)
            {
                currentstate = state.Return;
            }
        }
        else if (currentstate == state.Return)
        {
            returnToSpawn();
            if(transform.position == spawnPos)
            {
                currentstate = state.Idle;
                Controls.onMoveEvent += checkForPlayerInRoom;
            }
        }
    }
    protected virtual void chasePlayer()
    {
        Debug.Log("chasing player");
        Vector2Int enemyCell = new Vector2Int(
            Mathf.FloorToInt(transform.position.x),
            Mathf.FloorToInt(transform.position.y)
        );
        Vector2Int playerCell = new Vector2Int(
            Mathf.FloorToInt(player.transform.position.x),
            Mathf.FloorToInt(player.transform.position.y)
        );

        Vector2Int dir = playerCell - enemyCell;

    // Try primary axis first, then fallback axis
        Vector2Int[] steps;
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            steps = new Vector2Int[]
            {
                new Vector2Int(Mathf.Sign(dir.x) > 0 ? 1 : -1, 0),
                new Vector2Int(0, Mathf.Sign(dir.y) > 0 ? 1 : -1)
            };
        }
        else
        {
            steps = new Vector2Int[]
            {
                new Vector2Int(0, Mathf.Sign(dir.y) > 0 ? 1 : -1),
                new Vector2Int(Mathf.Sign(dir.x) > 0 ? 1 : -1, 0)
            };
        }

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
                transform.position = new Vector3(nextCell.x, nextCell.y, 0f);
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

        Vector2Int dir = spawnCell - enemyCell;

    // Try primary axis first, then fallback axis
        Vector2Int[] steps;
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            steps = new Vector2Int[]
            {
                new Vector2Int(Mathf.Sign(dir.x) > 0 ? 1 : -1, 0),
                new Vector2Int(0, Mathf.Sign(dir.y) > 0 ? 1 : -1)
            };
        }
        else
        {
            steps = new Vector2Int[]
            {
                new Vector2Int(0, Mathf.Sign(dir.y) > 0 ? 1 : -1),
                new Vector2Int(Mathf.Sign(dir.x) > 0 ? 1 : -1, 0)
            };
        }

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
                transform.position = new Vector3(nextCell.x, nextCell.y, 0f);
            }

            return; // stop after first valid move
        }
    }
}
