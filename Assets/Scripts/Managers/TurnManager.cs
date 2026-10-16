using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    private Queue<EnemyBehavior> enemies;
    private HashSet<EnemyBehavior> enemiestable; 
    public static TurnManager Instance;

    [Header("Enemy turn pacing (seconds)")]
    [Tooltip("Beat before the first enemy acts, so the player sees their own move land first.")]
    [SerializeField] private float turnStartDelay = 0.15f;
    [Tooltip("Gap between enemies, so the player can follow who moved.")]
    [SerializeField] private float turnStepDelay = 0.05f;

    /// <summary>The enemy phase is in flight. See <see cref="IsEnemyPhaseRunning"/>.</summary>
    private bool phaseRunning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        enemies = new Queue<EnemyBehavior>();
        enemiestable = new HashSet<EnemyBehavior>();
        Controls.onMoveEvent += callTaketurn;
    }


    public void queupToMove(EnemyBehavior enemy)
    {
        if (enemy == null)
        {
            return;
        }
        if(enemiestable.Add(enemy))
        {
            enemies.Enqueue(enemy);
        }
    }

    /// <summary>
    /// Drop an enemy from the pending-turn queue.
    /// Enemy.ForceDie()/Die() call this on teardown: without it the queue kept a destroyed
    /// component, and TakeTurns() then called takeTurn() on it and threw
    /// MissingReferenceException, which aborted the coroutine for every enemy still queued
    /// behind it.
    /// </summary>
    public void UnregisterEnemy(EnemyBehavior enemy)
    {
        if (enemy == null)
        {
            return;
        }
        if (enemies != null && enemies.Count > 0)
        {
            // Queue<T> has no Remove(T), so rebuild it without the dead entry.
            enemies = new Queue<EnemyBehavior>(enemies.Where(e => e != null && e != enemy));
        }
        enemiestable?.Remove(enemy);
    }

    public void callTaketurn(Controls controls)
    {
        // Single-flight. A second phase used to start while the first was still running (entering aim
        // mode raised the event), and the first coroutine's finally then released the input lock
        // underneath the second one - so the player could act twice in a round.
        if (phaseRunning)
        {
            return;
        }

        // The flag is a bool rather than the Coroutine handle on purpose: StartCoroutine runs the body
        // synchronously up to its first yield, and an empty queue never yields at all - so the phase can
        // finish before an assignment to a handle would happen, leaving a handle for a dead coroutine.
        phaseRunning = true;
        StartCoroutine(TakeTurns());
    }

    /// <summary>True while an enemy phase is in flight. The input lock is takingTurn; this is the guard.</summary>
    public bool IsEnemyPhaseRunning => phaseRunning;
    public void OnDestroy()
    {
        Controls.onMoveEvent -= callTaketurn;
    }
    IEnumerator TakeTurns()
    {
        // Lazily create the queue containers: they are normally built in Start(), but the
        // editor diagnostics path never runs Start. Anything before the try block that threw
        // would skip the finally below and leave Controls.takingTurn stuck on forever.
        if (enemies == null)
        {
            enemies = new Queue<EnemyBehavior>();
        }
        if (enemiestable == null)
        {
            enemiestable = new HashSet<EnemyBehavior>();
        }

        try
        {
            if (enemies.Count != 0)
                yield return new WaitForSeconds(turnStartDelay);
            while (enemies.Count != 0)
            {
                EnemyBehavior enemy = enemies.Dequeue();
                enemiestable.Remove(enemy);
                if (enemy == null)
                {
                    // Killed after it was queued (Unity's == null is true for a destroyed
                    // object). Skip it so the enemies behind it still get their turn.
                    continue;
                }
                enemy.takeTurn();
                yield return new WaitForSeconds(turnStepDelay);
            }
        }
        finally
        {
            // Only the owning phase clears this, and only once the queue has drained, so the input
            // lock lives exactly as long as the enemy phase does.
            phaseRunning = false;

            // The player input lock must always be released, even when an enemy throws.
            if (PlayerStats.Instance != null)
            {
                Controls controls = PlayerStats.Instance.gameObject.GetComponent<Controls>();
                if (controls != null)
                {
                    controls.takingTurn = false;
                }
            }
        }
        
    }
}
