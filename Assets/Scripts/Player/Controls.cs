using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

public class Controls : MonoBehaviour
{
    Transform self;
    public Grid grid;
    Vector2Int direction;
    [SerializeField] private float moveCooldown = 0.1f;
    [SerializeField] private float runningCooldown = 0;
    [SerializeField] private bool MenuIsOpen;
    [SerializeField] private PlayerStats playerStats;
    public static event Action<Controls> onMoveEvent;
    public static event UnityAction onShootEvent;
    // Start is called before the first frame update
    void Start()
    {
        UIManager.openMenu += detectMenu;
        self = gameObject.transform;
        Debug.Log(grid);
    }

    // Update is called once per frame
    void Update()
    {
        if (MenuIsOpen == false)
        {
            if (runningCooldown <= 0)
            {
                direction = Vector2Int.zero;
                if (Input.GetKey(KeyCode.W))
                {
                    direction = Vector2Int.up;
                }
                else if (Input.GetKey(KeyCode.S))
                {
                    direction = Vector2Int.down;
                }
                else if (Input.GetKey(KeyCode.D))
                {
                    direction = Vector2Int.right;
                }
                else if (Input.GetKey(KeyCode.A))
                {
                    direction = Vector2Int.left;
                }

                if (direction != Vector2Int.zero)
                {
                    runningCooldown = moveCooldown;
                    Vector2Int currentCell = Vector2Int.FloorToInt(transform.position);
                    Vector2Int nextCell = currentCell + direction;
                    if (grid.Move(transform.position, direction, transform))
                    {
                        EntityManager.Instance.MoveEntity(this.gameObject, currentCell, nextCell);
                    }
                    onMoveEvent(this);
                    direction = Vector2Int.zero;
                }
            }
            else
            {
                runningCooldown -= Time.deltaTime;
            }
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            grid.printGrid();
        }
        // if(Input.GetKey(KeyCode.J))
        // {   
        //     aim();
        //     runningCooldown = 0.3f;
        // }
    }

    private void aim()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            print("up");
            Enemy target = grid.detectEnemiesInALine(transform.position, new Vector2Int(0, 1), 5);
            if (target != null)
            {
                playerStats.attack("range", target);
            }
            else
            {
                print("no target found");
            }
        }
        else if (Input.GetKey(KeyCode.S))
        {
            print("down");
            Enemy target = grid.detectEnemiesInALine(transform.position, new Vector2Int(0, -1), 5);
            if (target != null)
            {
                playerStats.attack("range", target);
            }
            else
            {
                print("no target found");
            }
        }
        else if (Input.GetKey(KeyCode.D))
        {
            print("right");
            Enemy target = grid.detectEnemiesInALine(transform.position, new Vector2Int(1, 0), 5);
            if (target != null)
            {
                playerStats.attack("range", target);
            }
            else
            {
                print("no target found");
            }
        }
        else if (Input.GetKey(KeyCode.A))
        {
            print("left");
            Enemy target = grid.detectEnemiesInALine(transform.position, new Vector2Int(-1, 0), 5);
            if (target != null)
            {
                playerStats.attack("range", target);
            }
            else
            {
                print("no target found");
            }
        }

    }

    private void detectMenu(UIManager uIManager)
    {
        MenuIsOpen = !MenuIsOpen;
    }

}
