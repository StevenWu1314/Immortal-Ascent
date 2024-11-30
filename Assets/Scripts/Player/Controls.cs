using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Controls : MonoBehaviour
{
    Transform self;
    Grid grid;
    Vector2Int direction;
    [SerializeField] private float moveCooldown = 0.1f;
    [SerializeField] private float runningCooldown = 0;
    public static event Action<Controls> onMoveEvent;
    
    // Start is called before the first frame update
    void Start()
    {
        self = gameObject.transform;
        grid = GameObject.Find("Dungeon Generator").GetComponent<RoomFirstDungeonGenerator>().grid;
        Debug.Log(-0.5);
        Debug.Log((int)-0.5);
        Debug.Log((int)0.5);
        Debug.Log(math.floor(-0.5));
        
        
    }

    // Update is called once per frame
    void Update()
    {
        if(runningCooldown <= 0)
        {
            runningCooldown = moveCooldown;
            if(Input.GetKey(KeyCode.W)) 
            {
                direction = new Vector2Int(0, 1);
                grid.Move(transform.position, direction, gameObject.transform);
                onMoveEvent(this);
                
            }
            else if(Input.GetKey(KeyCode.S))
            {
                direction = new Vector2Int(0, -1);
                grid.Move(transform.position, direction, gameObject.transform);
                onMoveEvent(this);
                
            }
            else if(Input.GetKey(KeyCode.D))
            {
                direction = new Vector2Int(1, 0);
                grid.Move(transform.position, direction, gameObject.transform);
                onMoveEvent(this);
                
            }
            else if(Input.GetKey(KeyCode.A))
            {
                direction = new Vector2Int(-1, 0);
                grid.Move(transform.position, direction, gameObject.transform);
                onMoveEvent(this);
                
            }   

            
            
        }
        else
        {
            runningCooldown -= Time.deltaTime;
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            grid.printGrid();
        }
    }   

    
}
