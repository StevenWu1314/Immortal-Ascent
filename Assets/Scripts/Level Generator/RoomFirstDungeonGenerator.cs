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
    
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject[] enemies;
    public int gridSize = 500;
    public Grid grid;
    
    void Start()
    {
        // Origin MUST stay (0,0): every writer in TilePlacer calls
        // grid.SetValueAtLocation(x, y, ...) with raw tilemap cell coordinates (0..gridSize),
        // and entities index the grid with floor(transform.position). A non-zero origin makes
        // Grid's world->cell conversion (getXY) subtract it, so the player would read terrain
        // from a cell gridSize/2 away and every move would be blocked by a wall.
        grid = new Grid(gridSize, gridSize, 1, new Vector3(0, 0, 0));
        generateBranchingDungeon();     
        
    }

    public void generateDungeon()
    {
        tilePlacer.clear();
        List<BoundsInt> rooms = ProceduralGenerationAlgorithm.BinarySpacePartitioning(Space, minWidth, minHeight, numberOfRooms);
        tilePlacer.PlaceTiles(rooms, grid);    
        rooms = RoomConnector.SortRoomsByDistance(rooms);
        //Debug.Log(ProceduralGenerationAlgorithm.BrachingRooms(5, 10));
        tilePlacer.createCorridors(rooms);
    }

    public void generateBranchingDungeon()
    {
        tilePlacer.clear();
        List<Rooms> rooms = ProceduralGenerationAlgorithm.BrachingRooms(5, numberOfRooms, gridSize);
        List<BoundsInt> roomsSpace = new List<BoundsInt>();
        TurnManager.Instance.numberofRooms = rooms.ToArray().Length;
        TurnManager.Instance.initialize();
        
        for (int i = 0; i < rooms.Count; i++)
        {
            Debug.Log(rooms[i].body);
            roomsSpace.Add(rooms[i].body);
            TurnManager.Instance.rooms[i] = rooms[i].body;
            if(rooms[i].type == "Starting")
            {
                GameObject instance = Instantiate(player, rooms[i].body.center-new Vector3(0.5f, 0.5f), quaternion.identity);
                instance.GetComponent<Controls>().grid = grid;
                EntityManager.Instance.RegisterEntity(instance, new Vector2Int((int) rooms[i].body.center.x, (int) rooms[i].body.center.y));
            }
            
        }
        
        tilePlacer.PlaceTiles(roomsSpace, grid);
        tilePlacer.generateLootTiles(roomsSpace, grid);
        // Corridors must span the gap between rooms (Rooms.distanceApart) plus enough to
        // punch through the neighbour's wall ring. A length shorter than the gap leaves the
        // corridor as a dead end and the player can never reach the next island.
        int corridorLength = (rooms.Count > 0 ? rooms[0].distanceApart : 10);
        tilePlacer.branchingCorridors(rooms, grid, corridorLength);
        tilePlacer.fillWater(grid);

        spawnEnemies(roomsSpace);

        // Islands are assigned explicitly from the room layout rather than inferred from water
        // connectivity, so they stay correct however far the corridors happen to reach.
        // Corridor cells become a shared passage: the player may cross them, enemies may not.
        IslandManager.Rebuild(roomsSpace);
    }

    private void spawnEnemies(List<BoundsInt> roomsSpace)
    {
        int n = 0;
        GameObject[] oldEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject oldEnemy in oldEnemies)
        {
            DestroyImmediate(oldEnemy);
        }
        foreach (BoundsInt room in roomsSpace)
        {

            int amountToSpawn = Random.Range(0, 2);
            for(int i = 0; i < amountToSpawn; i++)
            {
                int type = Random.Range(0, enemies.Length);
                int x = Random.Range(room.xMin+3, room.xMax-3);
                int y = Random.Range(room.yMin+3, room.yMax-3);
                GameObject newEnemy = Instantiate(enemies[type], new Vector3(x, y), quaternion.identity);
                EnemyBehavior enemy =  newEnemy.GetComponent<EnemyBehavior>();
                enemy.room = room;
                EntityManager.Instance.RegisterEntity(newEnemy, new Vector2Int(x, y));
                TurnManager.Instance.AllEnemies[n].Add(enemy);
            }
            n++;
        }
    }
}
