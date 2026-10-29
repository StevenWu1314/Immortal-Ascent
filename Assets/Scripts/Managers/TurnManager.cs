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

    [Tooltip("Seconds between roaming pulses while the player explores out of combat (GDD 6.6).")]
    [SerializeField] private float roamInterval = 0.5f;

    private float lastRoamPulse;

    /// <summary>
    /// Let unaware enemies wander while the player travels for free. Free movement spends no round, so without
    /// this the whole floor stands still while the player explores and roaming is never actually seen.
    ///
    /// Paced by time on purpose: a held movement key would otherwise roam on every step and the enemies would
    /// sprint. Aware enemies are untouched - they act in the turn phase.
    /// </summary>
    public void RoamPulse()
    {
        if (Time.time - lastRoamPulse < roamInterval)
        {
            return;
        }
        lastRoamPulse = Time.time;

        foreach (EnemyBehavior enemy in FindObjectsByType<EnemyBehavior>(FindObjectsSortMode.None))
        {
            if (enemy != null && enemy.currentstate == AwarenessState.Idle)
            {
                enemy.takeTurn();
            }
        }
    }
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
            // Let every other onMoveEvent subscriber queue itself before the queue is read. TurnManager
            // subscribes in Start and the enemies subscribe in OnEnable when the generator spawns them, so the
            // order is not guaranteed: when the manager was invoked first the queue was still empty, the loop
            // below never ran, and the finally released the input lock inside the same call. The enemies then
            // queued themselves for the *next* round - which is why the player could act twice per enemy round,
            // with both melee and ranged attacks.
            yield return null;

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
                // End of round (GDD 9.4): statuses expire here, once per completed round, after the enemies
                // have acted. PlayerStats' temporary stat buffs tick on onMoveEvent instead, which is also
                // exactly once per round because only a real action raises that event.
                if (PlayerStats.Instance.Statuses != null)
                {
                    PlayerStats.Instance.Statuses.TickRound();
                }

                Controls controls = PlayerStats.Instance.gameObject.GetComponent<Controls>();
                if (controls != null)
                {
                    controls.takingTurn = false;
                }
            }
        }
        
    }
}
