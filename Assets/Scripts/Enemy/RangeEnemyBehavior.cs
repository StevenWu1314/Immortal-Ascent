using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class RangeEnemyBehavior : EnemyBehavior
{
    [SerializeField] protected int range;
    [SerializeField] private GameObject attackIndicator;
    private GameObject existingAttackIndicator;
    [SerializeField] private Vector3 lastAttackPos;
    private bool attacking;
    [SerializeField] int rangeAttackCooldown;

    protected override void Start()
    {
        base.Start();

        Enemy self = GetComponent<Enemy>();
        if (self != null && self.Definition != null)
        {
            range = self.Definition.range;
            rangeAttackCooldown = self.Definition.rangedCooldown;
        }
    }

    /// <summary>
    /// The first step from which the player would be in line of sight, dominant axis first, or zero when no
    /// neighbour gains it. This is what stops a blocked archer from strafing on the spot forever (GDD 6.5).
    /// </summary>
    private Vector2Int StepGainingSight(Vector2Int enemyCell, Vector2Int playerCell)
    {
        foreach (Vector2Int step in NextSteps(enemyCell, playerCell))
        {
            Vector2Int candidate = enemyCell + step;
            if (!CanStepOnto(candidate) || EntityManager.Instance.IsOccupied(candidate))
            {
                continue;
            }
            if (LineOfSight.HasClearShot(grid, candidate, playerCell))
            {
                return step;
            }
        }
        return Vector2Int.zero;
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

        Vector2Int[] steps = NextSteps(enemyCell, playerCell);

        foreach (var step in steps)
        {
            Vector2Int nextCell = enemyCell + step;
            // Step 1: check static grid
            // Step 1: walkable terrain AND still on this enemy's own island.
            if (!CanStepOnto(nextCell)) continue; // try next direction

            // Step 2: check entity layer
            if (EntityManager.Instance.IsOccupied(nextCell))
            {
                GameObject other = EntityManager.Instance.GetEntityAt(nextCell);

                if (other.CompareTag("Player"))
                {
                    this.GetComponent<Enemy>().Attack(player.GetComponent<PlayerStats>());
                    return;

                }
            }
            else if (math.distance(transform.position, player.transform.position) < range || attacking)
            {
                if(attacking)
                {
                    if(player.transform.position == lastAttackPos)
                    {
                        this.GetComponent<Enemy>().Attack(player.GetComponent<PlayerStats>());
                    }
                    attacking = false;
                    // Clamp: the check below is "<= 0", so a cooldown driven far negative was
                    // meaningless state rather than a longer wait.
                    rangeAttackCooldown = Mathf.Max(0, rangeAttackCooldown - 1);
                    Destroy(existingAttackIndicator);
                    // lastAttackPos is only ever read while attacking, and starting an attack is what
                    // set it, so there was no sentinel to reset here.
                    return;
                }       
                if(rangeAttackCooldown <= 0 && LineOfSight.HasClearShot(grid, enemyCell, playerCell))
                {
                    existingAttackIndicator = Instantiate(attackIndicator, player.transform.position, quaternion.identity, transform);
                    attacking = true;
                    rangeAttackCooldown = 2;
                    lastAttackPos = player.transform.position;
                    return;
                }
                else
                {
                    // No shot from here: prefer a step that opens one, then fall back to the strafe.
                    Vector2Int sightStep = StepGainingSight(enemyCell, playerCell);
                    Vector2Int strafeDir;
                    bool horizontal = math.abs(transform.position.x - player.transform.position.x) 
                                    < math.abs(transform.position.y - player.transform.position.y);
                    bool direction = Random.Range(0, 2) == 0;

                    if (horizontal)
                        strafeDir = new Vector2Int(direction ? 1 : -1, 0);
                    else
                        strafeDir = new Vector2Int(0, direction ? 1 : -1);

                    Vector2Int candidate = enemyCell + strafeDir;
                    Vector2Int opposite  = enemyCell - strafeDir;

                    bool candWalkable = CanStepOnto(candidate);

                    if (sightStep != Vector2Int.zero)
                    {
                        nextCell = enemyCell + sightStep;
                    }
                    else if (candWalkable && !EntityManager.Instance.IsOccupied(candidate))
                    {
                        nextCell = candidate;
                        
                    }
                    else
                    {
                        bool oppWalkable = CanStepOnto(opposite);
                        if (oppWalkable && !EntityManager.Instance.IsOccupied(opposite))
                            nextCell = opposite;
                        else
                            return; // nowhere to strafe, skip turn
                    }
                    rangeAttackCooldown = Mathf.Max(0, rangeAttackCooldown - 1);
                    EntityManager.Instance.MoveEntity(gameObject, enemyCell, nextCell);
                    return; // stop after first valid move
                }
           
            }
            else {
                // Move and update entity manager
                EntityManager.Instance.MoveEntity(gameObject, enemyCell, nextCell);
                return; // stop after first valid move
            }
        }
    }
}
