using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class spawnPointBehavior : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject Player;
    public Grid grid;
    void Start()
    {
        Player = GameObject.FindWithTag("Player");
        Player.transform.position = transform.position;
        grid = transform.parent.GetComponent<RoomFirstDungeonGenerator>().grid;
        grid.SetValueAtWorldLocation(Player.transform.position, 2);
        
        //Destroy(gameObject);
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
