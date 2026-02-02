using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Tilemaps;

public class RangeEnemyBehavior : EnemyBehavior
{
    [SerializeField] protected int range;
    [SerializeField] private GameObject attackIndicator;
    private GameObject existingAttackIndicator;
    [SerializeField] private Vector3 lastAttackPos;
    private bool attacking;
    [SerializeField] int rangeAttackCooldown;
    protected override void takeTurn(Controls controls)
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
    protected override void chasePlayer()
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
            else if (math.distance(transform.position, player.transform.position) < range)
            {
                if(attacking)
                {
                    if(player.transform.position == lastAttackPos)
                    {
                        this.GetComponent<Enemy>().Attack(player.GetComponent<PlayerStats>());
                    }
                    attacking = false;
                    rangeAttackCooldown--;
                    Destroy(existingAttackIndicator);
                    lastAttackPos = new Vector3(-10000, -10000);
                    return;
                }       
                if(rangeAttackCooldown <= 0)
                {
                    Debug.Log("trying to range attack");        
                    existingAttackIndicator = Instantiate(attackIndicator, player.transform.position, quaternion.identity, transform);
                    Debug.Log(existingAttackIndicator);
                    attacking = true;
                    rangeAttackCooldown = 2;
                    lastAttackPos = player.transform.position;
                    return;
                }
                rangeAttackCooldown--;
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
    
    protected override void returnToSpawn()
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
