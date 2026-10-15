using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    private Queue<EnemyBehavior> enemies;
    private HashSet<EnemyBehavior> enemiestable; 
    public static TurnManager Instance;

    public List<EnemyBehavior>[] AllEnemies;

    public BoundsInt[] rooms;
    private PlayerStats player;
    public int numberofRooms;
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
        player = PlayerStats.Instance;
        
    }

    public void initialize()
    {
        AllEnemies = new List<EnemyBehavior>[numberofRooms];
        for(int i = 0; i < numberofRooms; i++)
        {
            AllEnemies[i] = new List<EnemyBehavior>();
        }
        rooms = new BoundsInt[numberofRooms];
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
    /// Drop an enemy from the pending-turn queue and from the per-room alert lists.
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
        if (AllEnemies != null)
        {
            for (int i = 0; i < AllEnemies.Length; i++)
            {
                AllEnemies[i]?.Remove(enemy);
            }
        }
    }

    public void callTaketurn(Controls controls)
    {
        if (player == null)
        {
            player = PlayerStats.Instance;
        }

        // Never return early from here: Controls has already set takingTurn, and the
        // finally block in TakeTurns is the only place that releases it. An early return
        // would lock the player out of every future input.
        if (player != null && rooms != null && AllEnemies != null)
        {
            Vector3 playerpos = player.transform.position;
            for (int i = 0; i < rooms.Length && i < AllEnemies.Length; i++)
            {
                if (AllEnemies[i] == null)
                {
                    continue;
                }
                if(!rooms[i].Contains(new Vector3Int((int)playerpos.x, (int)playerpos.y)))
                {
                    continue;
                }
                foreach(EnemyBehavior enemy in AllEnemies[i])
                {
                    if (enemy != null)
                    {
                        enemy.becomeAlerted();
                    }
                }
            }
        }
        StartCoroutine("TakeTurns");
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
            if (enemies.Count != 0)
                yield return new WaitForSeconds(0.4f);
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
                yield return new WaitForSeconds(0.1f);
            }
        }
        finally
        {
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
