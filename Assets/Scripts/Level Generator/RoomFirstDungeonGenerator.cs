using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;


public class RoomFirstDungeonGenerator : MonoBehaviour
{
    // Start is called before the first frame update
   [SerializeField] private BoundsInt Space;
    [SerializeField] private int minWidth;
    [SerializeField] private int minHeight;
    [SerializeField] private int numberOfRooms;
    [SerializeField] private TilePlacer tilePlacer;
    [SerializeField] private GameObject spawnPoint;
    [SerializeField] private GameObject enemy;
    public static int gridSize = 500;
    public Grid grid = new Grid(gridSize, gridSize, 1, new Vector3 (-gridSize/2, -gridSize/2));
    
    void Start()
    {
        
        generateBranchingDungeon();
        
        
    }

    public void generateDungeon()
    {
        tilePlacer.clear();
        List<BoundsInt> rooms = ProceduralGenerationAlgorithm.BinarySpacePartitioning(Space, minWidth, minHeight, numberOfRooms);
        tilePlacer.PlaceTiles(rooms, grid);    
        rooms = RoomConnector.SortRoomsByDistance(rooms);
        Debug.Log(ProceduralGenerationAlgorithm.BrachingRooms(5, 10));
        tilePlacer.createCorridors(rooms);
    }

    public void generateBranchingDungeon()
    {
        tilePlacer.clear();
        List<Rooms> rooms = ProceduralGenerationAlgorithm.BrachingRooms(5, numberOfRooms);
        List<BoundsInt> roomsSpace = new List<BoundsInt>();
        for (int i = 0; i < rooms.Count; i++)
        {
            roomsSpace.Add(rooms[i].body);
            if(rooms[i].type == "Starting")
            {
                Instantiate(spawnPoint, rooms[i].body.center, quaternion.identity, gameObject.transform);
                
            }
            
        }
        tilePlacer.PlaceTiles(roomsSpace, grid);
        tilePlacer.branchingCorridors(rooms, grid);
        spawnEnemies(roomsSpace);
    }

    private void spawnEnemies(List<BoundsInt> roomsSpace)
    {
        GameObject[] oldEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject oldEnemy in oldEnemies)
        {
            DestroyImmediate(oldEnemy);
        }
        foreach (BoundsInt room in roomsSpace)
        {
            
            int amountToSpawn = Random.Range(0, 11);
            if(amountToSpawn >= 5 && amountToSpawn <= 8)
            {
                float x = Random.Range(room.xMin+3, room.xMax-3)+0.5f;
                float y = Random.Range(room.yMin+3, room.yMax-3)+0.5f;
                GameObject newEnemy = Instantiate(enemy, new Vector3(x, y), quaternion.identity);
                newEnemy.GetComponent<EnemyBehavior>().detectionRadius = room;
            }
            else if(amountToSpawn > 8)
            {
                float x = Random.Range(room.xMin+3, room.xMax-3)+0.5f;
                float y = Random.Range(room.yMin+3, room.yMax-3)+0.5f;
                GameObject newEnemy = Instantiate(enemy, new Vector3(x, y), quaternion.identity);
                newEnemy.GetComponent<EnemyBehavior>().detectionRadius = room;
                x = Random.Range(room.xMin+1, room.xMax-1);
                y = Random.Range(room.yMin+1, room.yMax-1);
                newEnemy = Instantiate(enemy, new Vector3(x, y), quaternion.identity);
                newEnemy.GetComponent<EnemyBehavior>().detectionRadius = room;
            }
        }
    }
}
