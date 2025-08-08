using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Mathematics;
using Random = UnityEngine.Random;
using System.Linq;
using UnityEditor;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.UIElements;
public class perlinnoisemap : MonoBehaviour
{
    Dictionary<int, Tile> terrainElements;
    Dictionary<int, Tile> vegetations;
    Dictionary<int, GameObject> tileGroups;
    [SerializeField]
    Tilemap tilemap;
    [SerializeField]
    Tile[] terrainTiles; // 0 = forest, 1 = grass, 2 = water
    [SerializeField]
    Tile[] vegetationTiles; //0 = tree
    [SerializeField] GameObject[] enemies;
    [SerializeField] bool debugMode = false;
    public Grid grid;
    public int map_width;
    public int map_height;
    public float magnification = 7; //4-20
    int xOffset; // reduce move left
    int yOffset; // reduce move down

    [SerializeField] float enemyFrequency = 0.003f; //between 0-1, how much percent of map will be mobs
    [SerializeField] float plantRarity; // range 0-1
    List<List<int>> noiseGrid = new List<List<int>>();
    List<List<Tile>> tileGrid = new List<List<Tile>>();
    
    // Start is called before the first frame update
    void Start()
    {
        InitializeGame();
        xOffset = Random.Range(-100, 100);
        yOffset = Random.Range(-100, 100);
        CreateTileSet();
        CreateTileGroups();
        GenerateTerrain();
        AddPlants();
        spawnEntities();
        if (debugMode)
            grid.printGrid();

    }

    private void InitializeGame()
    {
        grid = new Grid(map_width, map_height, 1, new Vector3(0, 0, 0));
        GameObject player = GameObject.Find("Player");
        player.GetComponent<Controls>().grid = grid;
    }

    private void spawnEntities()
    {
        GameObject player = GameObject.Find("Player");
        List<Vector2> grassCoords = new List<Vector2>();
        for (int x = 0; x < map_width; x++)
        {
            for (int y = 0; y < map_height; y++)
            {
                if (grid.GetValueAtLocation(x, y) == 0)
                {
                    grassCoords.Add(new Vector2(x, y));
                }
            }
        }
        player.transform.position = grassCoords[Random.Range(0, grassCoords.Count)] + new Vector2(0.5f, 0.5f);
        Debug.Log(grassCoords.Count);
        for (int x = 0; x < grassCoords.Count; x++)
        {
            if ((float)Random.Range(0, 1000) / 1000 <= enemyFrequency)
            {
                
                int type = Random.Range(0, enemies.Length);
                GameObject newEnemy = Instantiate(enemies[type], grassCoords[x], quaternion.identity);
                newEnemy.GetComponent<EnemyBehavior>().grid = grid;
            }
        }
    }
    private void CreateTileGroups()
    {
        tileGroups = new Dictionary<int, GameObject>();
        foreach (KeyValuePair<int, Tile> tilepair in terrainElements)
        {
            GameObject tileGroup = new GameObject(tilepair.Value.name);
            tileGroup.transform.parent = gameObject.transform;
            tileGroup.transform.localPosition = new Vector3();
            tileGroups.Add(tilepair.Key, tileGroup);

        }
        foreach (KeyValuePair<int, Tile> tilepair in vegetations)
        {
            GameObject tileGroup = new GameObject(tilepair.Value.name);
            tileGroup.transform.parent = gameObject.transform;
            tileGroup.transform.localPosition = new Vector3();
            tileGroups.Add(tilepair.Key + terrainElements.Count, tileGroup);

        }
    }

    private void CreateTileSet()
    {
        terrainElements = new Dictionary<int, Tile>();
        for (int i = 0; i < terrainTiles.Length; i++)
        {
            terrainElements.Add(i, terrainTiles[i]);
        }

        vegetations = new Dictionary<int, Tile>();
        for (int i = 0; i < vegetationTiles.Length; i++)
        {
            vegetations.Add(i, vegetationTiles[i]);
        }

    }

    void GenerateTerrain()
    {
        for (int x = 0; x < map_width; x++)
        {
            noiseGrid.Add(new List<int>());
            tileGrid.Add(new List<Tile>());
            for (int y = 0; y < map_height; y++)
            {
                int tileId = GetIdUsingPerlin(x, y);

                noiseGrid[x].Add(tileId);
                tilemap.SetTile(new Vector3Int(x, y, 0), terrainElements[tileId]);
                grid.SetValueAtLocation(x, y, tileId);

            }
        }
    }

    void AddPlants()
    {
        for (int x = 0; x < map_width; x++)
        {
            for (int y = 0; y < map_height; y++)
            {
                if (noiseGrid[x][y] == 0)
                {
                    int plant = Random.Range(0, (int)(1 / plantRarity));
                    if (plant == Random.Range(0, (int)(1 / plantRarity)))
                    {
                        //Replace original tile with plant tile
                        noiseGrid[x][y] = 3;
                        tilemap.SetTile(new Vector3Int(x, y, 0), vegetations[0]);
                    }
                }
                
            }
        }
    }
    private int GetIdUsingPerlin(int x, int y)
    {
        float rawPerlin = Mathf.PerlinNoise(
            (x - xOffset) / magnification,
            (y - yOffset) / magnification
        );

        float simpleNoise = noise.snoise(new float2(
            (x - xOffset) / magnification,
            (y - yOffset) / magnification)
        );
        float clampPerlin = Mathf.Clamp(simpleNoise, 0, 1);
        float scalePerlin = clampPerlin * terrainElements.Count;
        if (scalePerlin == terrainElements.Count)
        {
            scalePerlin = terrainElements.Count - 1;
        }
        return Mathf.FloorToInt(scalePerlin);
    }
}
