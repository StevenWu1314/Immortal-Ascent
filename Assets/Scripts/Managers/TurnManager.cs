using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [SerializeField] private Queue<EnemyBehavior> enemies;
    private HashSet<EnemyBehavior> enemiestable; 
    public static TurnManager Instance;
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
        if(enemiestable.Add(enemy))
        {
            enemies.Enqueue(enemy);
            Debug.Log("queued up: " + enemy.name);
        }
    }
    public void callTaketurn(Controls controls)
    {
        StartCoroutine("TakeTurns");
    }
    public void OnDestroy()
    {
        Controls.onMoveEvent -= callTaketurn;
    }
    IEnumerator TakeTurns()
    {
        Debug.Log("taking turns: " + enemies.Count);
        try
        {
            if (enemies.Count != 0)
                yield return new WaitForSeconds(1f);
            while (enemies.Count != 0)
            {
                Debug.Log("looping: " + enemies.Count + " left");
                EnemyBehavior enemy = enemies.Dequeue();
                enemiestable.Remove(enemy);
                enemy.takeTurn();
                yield return new WaitForSeconds(0.1f);
            }
        }
        finally
        {
            PlayerStats.Instance.gameObject.GetComponent<Controls>().takingTurn = false;
            Debug.Log("Turn Manager Finished: Player Unlocked");
        }
        
    }
}
