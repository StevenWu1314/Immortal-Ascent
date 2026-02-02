using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBehavior : MonoBehaviour
{
    public Grid grid;
    protected GameObject player;
    public float detectionRadius;
    protected bool playerDetected;

    public Vector3 spawnPos;
    [SerializeField] protected int chaseRange;

    public enum state {Idle, Chase, Return}
    public state currentstate;

    protected void OnEnable()
    {
        Controls.onMoveEvent += checkForPlayerInRoom;
        Controls.onMoveEvent += takeTurn;
        grid = FindObjectOfType<PerlinNoiseMap>().grid;
        spawnPos = this.transform.position;
    }
    protected void OnDestroy() {
        Controls.onMoveEvent -= checkForPlayerInRoom;
        Controls.onMoveEvent -= takeTurn;
    }
    protected void Start()
    {

        player = GameObject.FindWithTag("Player");
        currentstate = state.Idle;

    }
    protected void checkForPlayerInRoom(Controls controls)
    {
        // Debug.Log("checking for player");
        // Debug.Log(detectionRadius + " contains: " + new Vector3Int((int)player.transform.position.x, (int)player.transform.position.y));
        // Debug.Log(detectionRadius.Contains(new Vector3Int((int)player.transform.position.x, (int)player.transform.position.y)));
        Collider2D[] objectInRange = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
        if (objectInRange.Contains(player.GetComponent<Collider2D>()))
        {
            Controls.onMoveEvent -= checkForPlayerInRoom;
            currentstate = state.Chase;
            notifyNearbyEnemies();
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
    protected virtual void takeTurn(Controls controls)
    {
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
            // Step 1: check static grid
            int terrain = grid.GetValueAtLocation(nextCell.x, nextCell.y);
            bool walkable = (terrain == 0 || terrain == 5); // grass or bridge

            if (!walkable) continue; // try next direction

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
            // Step 1: check static grid
            int terrain = grid.GetValueAtLocation(nextCell.x, nextCell.y);
            bool walkable = (terrain == 0 || terrain == 5); // grass or bridge

            if (!walkable) continue; // try next direction

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
