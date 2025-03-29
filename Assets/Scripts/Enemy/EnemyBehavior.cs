using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBehavior : MonoBehaviour
{
    private Grid grid;
    private GameObject player;
    public BoundsInt detectionRadius;
    private bool playerDetected;
    

    
    private void OnEnable() {
        Controls.onMoveEvent += checkForPlayerInRoom;
        
    }
    private void OnDestroy() {
        Controls.onMoveEvent -= checkForPlayerInRoom;
        Controls.onMoveEvent -= chasePlayer;
        grid.SetValueAtWorldLocation(transform.position, 0);
    }
    void Start()
    {
        player = GameObject.FindWithTag("Player");
        grid = GameObject.Find("Dungeon Generator").GetComponent<RoomFirstDungeonGenerator>().grid;
        gridSetup(grid);

    }

    private void gridSetup(Grid grid)
    {
        grid.SetValueAtWorldLocation(transform.position, 3);
    }

    private void checkForPlayerInRoom(Controls controls)
    {
        // Debug.Log("checking for player");
        // Debug.Log(detectionRadius + " contains: " + new Vector3Int((int)player.transform.position.x, (int)player.transform.position.y));
        // Debug.Log(detectionRadius.Contains(new Vector3Int((int)player.transform.position.x, (int)player.transform.position.y)));
        if(detectionRadius.Contains(new Vector3Int((int)player.transform.position.x, (int)player.transform.position.y)))
        {
            Controls.onMoveEvent -= checkForPlayerInRoom;
            //Debug.Log("player detected");
            Controls.onMoveEvent += chasePlayer;
        }
    }

    private void chasePlayer(Controls controls)
    {
        
        grid.pathFind(transform.position, player.transform.position, gameObject.transform);
    }
    
}
