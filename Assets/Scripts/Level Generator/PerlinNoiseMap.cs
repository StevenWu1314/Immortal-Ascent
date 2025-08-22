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
using UnityEngine.Rendering;
public class PerlinNoiseMap : MonoBehaviour
{
    Dictionary<int, Tile> terrainElements;
    Dictionary<int, Tile> vegetations;
    Dictionary<int, GameObject> tileGroups;
    [SerializeField]
    Tilemap tilemap;
    [SerializeField]
    Tile[] terrainTiles; // 0 = grass, 1 = water, 2 = deepwater
    [SerializeField]
    Tile[] vegetationTiles; //0 = flower
    [SerializeField]
    Tile[] bridgeTiles;
    [SerializeField] GameObject[] enemies;
    [SerializeField] bool debugMode = false;
    public Grid grid;
    public int map_width;
    public int map_height;
    public float magnification = 7; //4-20
    int xOffset; // reduce move left
    int yOffset; // reduce move down
    public int maxBridgeLength = 30;    // Max water gap to bridge
    public int minBridgeLength = 6;    // Prevent tiny edge bridges
    public int minPerpendicularDepth = 2; // Water depth perpendicular to bridge
    public int maxBridgesPerWaterBody = 1; // Usually 1, maybe 2 for huge lakes

    [SerializeField] float enemyFrequency = 0.003f; //between 0-1, how much percent of map will be mobs
    [SerializeField] float plantRarity; // range 0-1
    List<List<int>> noiseGrid = new List<List<int>>();
    List<List<Tile>> tileGrid = new List<List<Tile>>();

    // Start is called before the first frame update
    void Start()
    {
        GenerateMap();

    }

    public void GenerateMap()
    {
        InitializeGame();
        xOffset = Random.Range(-100, 100);
        yOffset = Random.Range(-100, 100);
        CreateTileSet();
        CreateTileGroups();
        GenerateTerrain();
        PlaceBridge();
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

public void PlaceBridge()
{
    int width = map_width;
    int height = map_height;

    // --- Step 1: Identify land clusters ---
    int[,] landCluster = new int[width, height];
    int currentClusterId = 1;
    for (int x = 0; x < width; x++)
    {
        for (int y = 0; y < height; y++)
        {
            var pos = new Vector3Int(x, y, 0);
            if (IsLand(pos) && landCluster[x, y] == 0)
            {
                FloodFill(pos, currentClusterId, landCluster, width, height, true);
                currentClusterId++;
            }
        }
    }

    // --- Step 2: Identify water bodies ---
    int[,] waterRegion = new int[width, height];
    int currentWaterId = 1;
    Dictionary<int, List<Vector3Int>> waterBodies = new Dictionary<int, List<Vector3Int>>();
    for (int x = 0; x < width; x++)
    {
        for (int y = 0; y < height; y++)
        {
            var pos = new Vector3Int(x, y, 0);
            if (IsWater(pos) && waterRegion[x, y] == 0)
            {
                var region = FloodFill(pos, currentWaterId, waterRegion, width, height, false);
                waterBodies[currentWaterId] = region;
                currentWaterId++;
            }
        }
    }

    // --- Step 3: Handle each water body ---
    foreach (var kv in waterBodies)
    {
        var waterTiles = kv.Value;

        // Skip tiny puddles
        if (waterTiles.Count < 20) continue;

        BoundsInt bounds = GetBoundsOfRegion(waterTiles);
        bool isRiver = (Mathf.Max(bounds.size.x, bounds.size.y) / (float)Mathf.Min(bounds.size.x, bounds.size.y)) > 3f;

        List<BridgeCandidate> candidates = isRiver
            ? FindRiverBridges(waterTiles, landCluster)
            : FindLakeBridges(waterTiles, landCluster);

        // Pick top bridges
        candidates.Sort((a, b) => b.score.CompareTo(a.score));
        int bridgesPlaced = 0;
        List<Vector3Int> usedMidpoints = new List<Vector3Int>();

        foreach (var bridge in candidates)
        {
            if (bridgesPlaced >= maxBridgesPerWaterBody) break;

            // Reject if midpoint is too close to an existing one
            var mid = new Vector3Int(
                (bridge.start.x + bridge.end.x) / 2,
                (bridge.start.y + bridge.end.y) / 2, 0
            );
            if (usedMidpoints.Any(m => Vector3Int.Distance(m, mid) < 10))
                continue;

            PlaceBridgeTiles(bridge.start, bridge.end);
            usedMidpoints.Add(mid);
            bridgesPlaced++;
        }
    }
}

// --- New Helpers ---

BoundsInt GetBoundsOfRegion(List<Vector3Int> region)
{
    int minX = int.MaxValue, maxX = int.MinValue;
    int minY = int.MaxValue, maxY = int.MinValue;
    foreach (var p in region)
    {
        if (p.x < minX) minX = p.x;
        if (p.x > maxX) maxX = p.x;
        if (p.y < minY) minY = p.y;
        if (p.y > maxY) maxY = p.y;
    }
    return new BoundsInt(minX, minY, 0, maxX - minX + 1, maxY - minY + 1, 1);
}

List<BridgeCandidate> FindRiverBridges(List<Vector3Int> waterTiles, int[,] landCluster)
{
    List<BridgeCandidate> candidates = new List<BridgeCandidate>();
    HashSet<Vector3Int> reservedSpots = new HashSet<Vector3Int>();

    foreach (var waterTilePos in waterTiles)
    {
        // Horizontal check
        if (IsLand(waterTilePos + Vector3Int.left) && !IsLand(waterTilePos + Vector3Int.right))
        {
            Vector3Int leftLand = waterTilePos + Vector3Int.left;
            int clusterA = landCluster[leftLand.x, leftLand.y];
            int length = 0;
            var p = waterTilePos;

            while (IsWater(p) && length <= maxBridgeLength)
            {
                length++;
                p += Vector3Int.right;
            }

            if (IsLand(p))
            {
                int clusterB = landCluster[p.x, p.y];
                if (clusterA != clusterB && clusterA > 0 && clusterB > 0 &&
                    length >= minBridgeLength && length <= maxBridgeLength)
                {
                    var midPoint = waterTilePos + Vector3Int.right * (length / 2);
                    if (!reservedSpots.Contains(midPoint))
                    {
                        int perpDepth = MeasurePerpendicularDepth(midPoint, Vector3Int.up);
                        if (perpDepth >= minPerpendicularDepth)
                        {
                            candidates.Add(new BridgeCandidate
                            {
                                start = leftLand,
                                end = p,
                                length = length,
                                score = (perpDepth * 2) - (length * 0.5f)
                            });
                            reservedSpots.Add(midPoint);
                        }
                    }
                }
            }
        }

        // Vertical check
        if (IsLand(waterTilePos + Vector3Int.down) && !IsLand(waterTilePos + Vector3Int.up))
        {
            Vector3Int downLand = waterTilePos + Vector3Int.down;
            int clusterA = landCluster[downLand.x, downLand.y];
            int length = 0;
            var p = waterTilePos;

            while (IsWater(p) && length <= maxBridgeLength)
            {
                length++;
                p += Vector3Int.up;
            }

            if (IsLand(p))
            {
                int clusterB = landCluster[p.x, p.y];
                if (clusterA != clusterB && clusterA > 0 && clusterB > 0 &&
                    length >= minBridgeLength && length <= maxBridgeLength)
                {
                    var midPoint = waterTilePos + Vector3Int.up * (length / 2);
                    if (!reservedSpots.Contains(midPoint))
                    {
                        int perpDepth = MeasurePerpendicularDepth(midPoint, Vector3Int.right);
                        if (perpDepth >= minPerpendicularDepth)
                        {
                            candidates.Add(new BridgeCandidate
                            {
                                start = downLand,
                                end = p,
                                length = length,
                                score = (perpDepth * 2) - (length * 0.5f)
                            });
                            reservedSpots.Add(midPoint);
                        }
                    }
                }
            }
        }
    }

    return candidates;
}

List<BridgeCandidate> FindLakeBridges(List<Vector3Int> waterTiles, int[,] landCluster)
{
    List<BridgeCandidate> candidates = new List<BridgeCandidate>();

    // find centroid of water body
    Vector3 avg = Vector3.zero;
    foreach (var t in waterTiles) avg += (Vector3)t;
    avg /= waterTiles.Count;
    Vector3Int center = new Vector3Int(Mathf.RoundToInt(avg.x), Mathf.RoundToInt(avg.y), 0);

    // Try 4 directions
    foreach (var dir in new[] { Vector3Int.left, Vector3Int.right, Vector3Int.up, Vector3Int.down })
    {
        var p = center;
        int length = 0;

        while (IsWater(p) && length <= maxBridgeLength)
        {
            length++;
            p += dir;
        }

        if (IsLand(p) && length >= minBridgeLength && length <= maxBridgeLength)
        {
            Vector3Int end = p;
            Vector3Int start = center;
            candidates.Add(new BridgeCandidate
            {
                start = start,
                end = end,
                length = length,
                score = -length // prefer shorter
            });
        }
    }

    return candidates;
}



    int MeasurePerpendicularDepth(Vector3Int pos, Vector3Int perpDir)
    {
        int depth = 0;
        var checkPos = pos;
        while (IsWater(checkPos))
        {
            depth++;
            checkPos += perpDir;
        }
        checkPos = pos;
        while (IsWater(checkPos))
        {
            depth++;
            checkPos -= perpDir;
        }
        return depth / 2; // average both sides
    }

    List<Vector3Int> FloodFill(Vector3Int start, int id, int[,] regionMap, int width, int height, bool isLand)
    {
        List<Vector3Int> region = new List<Vector3Int>();
        Queue<Vector3Int> q = new Queue<Vector3Int>();
        q.Enqueue(start);
        regionMap[start.x, start.y] = id;

        while (q.Count > 0)
        {
            var p = q.Dequeue();
            region.Add(p);

            foreach (var dir in new Vector3Int[] {
                new Vector3Int(1,0,0), new Vector3Int(-1,0,0),
                new Vector3Int(0,1,0), new Vector3Int(0,-1,0)
            })
            {
                var n = p + dir;
                if (n.x >= 0 && n.x < width && n.y >= 0 && n.y < height && regionMap[n.x, n.y] == 0)
                {
                    if (isLand && IsLand(n))
                    {
                        regionMap[n.x, n.y] = id;
                        q.Enqueue(n);
                    }
                    else if (!isLand && IsWater(n))
                    {
                        regionMap[n.x, n.y] = id;
                        q.Enqueue(n);
                    }
                }
            }
        }
        return region;
    }

    void PlaceBridgeTiles(Vector3Int start, Vector3Int end)
    {
        if (start.x == end.x) // vertical
        {
            int y0 = Mathf.Min(start.y, end.y);
            int y1 = Mathf.Max(start.y, end.y);
            for (int y = y0; y <= y1; y++) // <= fixes missing tile
            {
                if(IsWater(new Vector3Int(start.x, y, 0)))
                    tilemap.SetTile(new Vector3Int(start.x, y, 0), bridgeTiles[0]);
            }
        }
        else if (start.y == end.y) // horizontal
        {
            int x0 = Mathf.Min(start.x, end.x);
            int x1 = Mathf.Max(start.x, end.x);
            for (int x = x0; x <= x1; x++) // <= fixes missing tile
            {
                if(IsWater(new Vector3Int(x, start.y, 0)))
                    tilemap.SetTile(new Vector3Int(x, start.y, 0), bridgeTiles[0]);
            }
        }
        else
        {
            // Optional: handle diagonal / fallback with Bresenham
            Debug.LogWarning("Non-axis-aligned bridge requested");
        }
    }

    class BridgeCandidate
    {
        public Vector3Int start;
        public Vector3Int end;
        public int length;
        public float score;
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

    bool IsLand(Vector3Int pos)
    {
        return tilemap.GetTile(pos) == terrainTiles[0] || bridgeTiles.Contains(tilemap.GetTile(pos));

    }

    bool IsWater(Vector3Int pos)
    {
        return terrainTiles[1..3].Contains(tilemap.GetTile(pos));
    }

    bool HasShoreOffset(Vector3Int pos, Vector3Int direction, int offset)
    {
        for (int i = 0; i < offset; i++)
        {
            pos += direction;
            if (!IsWater(pos))
                return false;
        }
        return true;
    }

}
