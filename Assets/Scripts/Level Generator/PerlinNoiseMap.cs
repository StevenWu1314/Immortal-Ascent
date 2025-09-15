using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;
// NOTE: Removed all Editor-only namespaces to avoid player build issues.

public class PerlinNoiseMap : MonoBehaviour
{
    // --- Serialized fields / config ---
    [Header("Tilemaps & Tiles")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private Tile[] terrainTiles;   // 0 = grass, 1 = water, 2 = deepwater
    [SerializeField] private Tile[] nonCollidablePlants; // e.g. flowers
    [SerializeField] private Tile[] collidablePlants;    // e.g. trees, bamboo
    [SerializeField] private Tile[] bridgeTiles;

    [Header("Entities")]
    [SerializeField] private GameObject[] enemies;
    [SerializeField, Range(0f, 1f)] private float enemyFrequency = 0.003f; // fraction of grass cells

    [Header("Map Size")]
    public int map_width = 120;
    public int map_height = 120;

    [Header("Water / Land Balance")]
    [Range(0f, 1f)] public float waterThreshold = 0.4f; 
    [Tooltip("Of the tiles that are water, what fraction should be deep water (0..1).")]
    [Range(0f, 1f)] public float deepWaterRatio = 0.25f;
    // Lower waterThreshold = more land; raise to create more water.

    [Header("Vegetation Density")]
    [Tooltip("Chance a grass tile becomes a collidable plant (tree/bamboo).")]
    [Range(0f, 1f)] public float treeDensity = 0.1f;   // collidable
    [Tooltip("Chance a grass tile becomes a non-collidable plant (flower).")]
    [Range(0f, 1f)] public float flowerDensity = 0.05f; // non-collidable

    [Tooltip("Bigger = smoother. Must be > 0.")]
    public float magnification = 7f; // 4-20 typically
    public bool debugMode = false;

    [Header("Bridges")]
    public int maxBridgeLength = 30;
    public int minBridgeLength = 6;
    public int minPerpendicularDepth = 2;
    public int maxBridgesPerWaterBody = 1;

    // --- Runtime data ---
    private Dictionary<int, Tile> terrainElements;
    private Dictionary<int, Tile> vegetations; // kept for compatibility (flowers earlier)
    private Dictionary<int, GameObject> tileGroups;

    // NOTE: If you also use UnityEngine.Grid in your project,
    // consider renaming your custom Grid to avoid ambiguity.
    public Grid grid;

    private int xOffset;
    private int yOffset;

    private List<List<int>> noiseGrid = new List<List<int>>();

    // --- Unity lifecycle ---
    private void Start()
    {
        grid = Manager.grid;
        GenerateMap();
    }

    // --- Public API ---
    public void GenerateMap()
    {
        // Reset previous state if regenerating
        ClearPreviousState();

        InitializeGame();

        // Randomize noise offsets
        xOffset = UnityEngine.Random.Range(-100, 100);
        yOffset = UnityEngine.Random.Range(-100, 100);

        CreateTileSet();
        CreateOrRefreshTileGroups();

        GenerateTerrain();
        PlaceBridge();
        AddPlants();
        SpawnEntities();

        if (debugMode)
        {
            try { grid.printGrid(); } catch { /* ignore if not implemented */ }
        }
    }

    // --- Setup / teardown ---
    private void ClearPreviousState()
    {
        // Clear lists
        noiseGrid.Clear();

        // Clear tilemap
        if (tilemap != null)
        {
            tilemap.ClearAllTiles();
        }

        // Remove old groups if we created any
        if (tileGroups != null)
        {
            foreach (var go in tileGroups.Values)
            {
                if (go != null) DestroyImmediate(go);
            }
            tileGroups.Clear();
        }
    }

    private void InitializeGame()
    {
        // Guard magnification
        magnification = Mathf.Max(0.0001f, magnification);

        // Construct your gameplay grid (custom class in your project)
        grid = new Grid(map_width, map_height, 1, new Vector3(0, 0, 0));

        // Wire the player to the grid if present
        var player = GameObject.Find("Player");
        if (player != null)
        {
            var controls = player.GetComponent<Controls>();
            if (controls != null) controls.grid = grid;
        }
        else
        {
            Debug.LogWarning("Player not found in scene.");
        }
    }

    private void CreateTileSet()
    {
        terrainElements = new Dictionary<int, Tile>(capacity: terrainTiles != null ? terrainTiles.Length : 0);
        if (terrainTiles != null)
        {
            for (int i = 0; i < terrainTiles.Length; i++)
            {
                if (terrainTiles[i] == null)
                    Debug.LogWarning($"terrainTiles[{i}] is null.");
                terrainElements[i] = terrainTiles[i];
            }
        }

        vegetations = new Dictionary<int, Tile>(capacity: nonCollidablePlants != null ? nonCollidablePlants.Length : 0);
        if (nonCollidablePlants != null)
        {
            for (int i = 0; i < nonCollidablePlants.Length; i++)
            {
                if (nonCollidablePlants[i] == null)
                    Debug.LogWarning($"nonCollidablePlants[{i}] is null.");
                vegetations[i] = nonCollidablePlants[i];
            }
        }
    }

    private void CreateOrRefreshTileGroups()
    {
        tileGroups = new Dictionary<int, GameObject>();

        // Terrain groups
        if (terrainElements != null)
        {
            foreach (var kv in terrainElements)
            {
                var tile = kv.Value;
                var go = new GameObject(tile != null ? tile.name : $"Terrain_{kv.Key}");
                go.transform.SetParent(transform, false);
                tileGroups.Add(kv.Key, go);
            }
        }

        // Vegetation groups after terrain (offset keys to avoid collisions)
        int offset = terrainElements != null ? terrainElements.Count : 0;
        if (vegetations != null)
        {
            foreach (var kv in vegetations)
            {
                var tile = kv.Value;
                var go = new GameObject(tile != null ? tile.name : $"Vegetation_{kv.Key}");
                go.transform.SetParent(transform, false);
                tileGroups.Add(kv.Key + offset, go);
            }
        }

        // Optional: collidablePlants don't need groups, but you can add them too if desired.
    }

    // --- Terrain generation ---
    private void GenerateTerrain()
    {
        // Precompute thresholds for water split
        float totalWater = Mathf.Clamp01(waterThreshold);
        float deepWaterPortion = Mathf.Clamp01(deepWaterRatio);
        float deepWaterThreshold = totalWater * deepWaterPortion;   // v < deepWaterThreshold => deep water
        float shallowWaterThreshold = totalWater;                   // deepWaterThreshold <= v < shallowWaterThreshold => shallow

        for (int x = 0; x < map_width; x++)
        {
            noiseGrid.Add(new List<int>(map_height));
            for (int y = 0; y < map_height; y++)
            {
                int tileId = GetIdUsingPerlinAsBiome(x, y, deepWaterThreshold, shallowWaterThreshold);

                noiseGrid[x].Add(tileId);

                // Safety: make sure the tileId exists in terrainElements
                if (!terrainElements.TryGetValue(tileId, out var tile) || tile == null)
                {
                    Debug.LogWarning($"Tile ID {tile} not found in terrainElements. Using tile 0.");
                    tile = terrainElements.ContainsKey(0) ? terrainElements[0] : null;
                }

                tilemap.SetTile(new Vector3Int(x, y, 0), tile);
                grid.SetValueAtLocation(x, y, tileId); // grid values: 0 grass, 1 shallow water, 2 deep water
            }
        }
    }

    /// <summary>
    /// Maps Perlin/simplex noise (v in [0,1]) to explicit biome tile IDs:
    /// 0 = grass, 1 = shallow water, 2 = deep water
    /// Uses the thresholds precomputed in GenerateTerrain.
    /// </summary>
    private int GetIdUsingPerlinAsBiome(int x, int y, float deepWaterThreshold, float shallowWaterThreshold)
    {
        float fx = (x - xOffset) / magnification;
        float fy = (y - yOffset) / magnification;

        // Use simplex noise, properly remapped to [0,1]
        float s = noise.snoise(new float2(fx, fy)); // [-1,1]
        float v = Mathf.Clamp01((s + 1f) * 0.5f);   // [0,1]

        // Map explicitly to biome based on thresholds:
        // v < deepWaterThreshold -> deep water (2)
        // deepWaterThreshold <= v < shallowWaterThreshold -> shallow water (1)
        // else -> grass/land (0)
        if (v < deepWaterThreshold)
            return 2;
        if (v < shallowWaterThreshold)
            return 1;
        return 0;
    }

    // --- Bridge placement (unchanged aside from defensive checks) ---
    public void PlaceBridge()
    {
        int width = map_width;
        int height = map_height;

        // Step 1: land clusters
        int[,] landCluster = new int[width, height];
        int currentClusterId = 1;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var pos = new Vector3Int(x, y, 0);
                if (IsLand(pos) && landCluster[x, y] == 0)
                {
                    FloodFill(pos, currentClusterId, landCluster, width, height, isLand: true);
                    currentClusterId++;
                }
            }
        }

        // Step 2: water bodies
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
                    var region = FloodFill(pos, currentWaterId, waterRegion, width, height, isLand: false);
                    waterBodies[currentWaterId] = region;
                    currentWaterId++;
                }
            }
        }

        // Step 3: per water body decide bridges
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

            // Rank & place
            candidates.Sort((a, b) => b.score.CompareTo(a.score));
            int bridgesPlaced = 0;
            List<Vector3Int> usedMidpoints = new List<Vector3Int>();

            foreach (var bridge in candidates)
            {
                if (bridgesPlaced >= maxBridgesPerWaterBody) break;

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

    private BoundsInt GetBoundsOfRegion(List<Vector3Int> region)
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

    private List<BridgeCandidate> FindRiverBridges(List<Vector3Int> waterTiles, int[,] landCluster)
    {
        var candidates = new List<BridgeCandidate>();
        var reservedSpots = new HashSet<Vector3Int>();

        foreach (var waterTilePos in waterTiles)
        {
            // Horizontal check (left land → right scan)
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

            // Vertical check (down land → up scan)
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

    private List<BridgeCandidate> FindLakeBridges(List<Vector3Int> waterTiles, int[,] landCluster)
    {
        var candidates = new List<BridgeCandidate>();

        // centroid
        Vector3 avg = Vector3.zero;
        foreach (var t in waterTiles) avg += (Vector3)t;
        avg /= waterTiles.Count;
        Vector3Int center = new Vector3Int(Mathf.RoundToInt(avg.x), Mathf.RoundToInt(avg.y), 0);

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
                candidates.Add(new BridgeCandidate
                {
                    start = center,
                    end = p,
                    length = length,
                    score = -length // prefer shorter
                });
            }
        }

        return candidates;
    }

    private int MeasurePerpendicularDepth(Vector3Int pos, Vector3Int perpDir)
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

    private List<Vector3Int> FloodFill(Vector3Int start, int id, int[,] regionMap, int width, int height, bool isLand)
    {
        List<Vector3Int> region = new List<Vector3Int>();
        Queue<Vector3Int> q = new Queue<Vector3Int>();
        q.Enqueue(start);
        regionMap[start.x, start.y] = id;

        // 4-neighbors
        Vector3Int[] dirs = {
            new Vector3Int( 1, 0, 0), new Vector3Int(-1, 0, 0),
            new Vector3Int( 0, 1, 0), new Vector3Int( 0,-1, 0)
        };

        while (q.Count > 0)
        {
            var p = q.Dequeue();
            region.Add(p);

            foreach (var dir in dirs)
            {
                var n = p + dir;
                if (n.x >= 0 && n.x < width && n.y >= 0 && n.y < height && regionMap[n.x, n.y] == 0)
                {
                    if (isLand ? IsLand(n) : IsWater(n))
                    {
                        regionMap[n.x, n.y] = id;
                        q.Enqueue(n);
                    }
                }
            }
        }
        return region;
    }
    private void EnsureLandMatchesBridge(Vector3Int bridgeEnd, bool horizontal, int bridgeWidth = 3)
    {
        int half = bridgeWidth / 2;

        if (horizontal)
        {
            for (int dy = -half; dy <= half; dy++)
            {
                Vector3Int pos = new Vector3Int(bridgeEnd.x, bridgeEnd.y + dy, 0);

                // Skip if this position is part of a bridge already
                if (tilemap.GetTile(pos) == bridgeTiles[0]) continue;

                if (IsWater(pos))
                {
                    tilemap.SetTile(pos, terrainTiles[0]); // land
                    grid.SetValueAtLocation(pos.x, pos.y, 0);
                }
            }
        }
        else
        {
            for (int dx = -half; dx <= half; dx++)
            {
                Vector3Int pos = new Vector3Int(bridgeEnd.x + dx, bridgeEnd.y, 0);

                if (tilemap.GetTile(pos) == bridgeTiles[0]) continue;

                if (IsWater(pos))
                {
                    tilemap.SetTile(pos, terrainTiles[0]); // land
                    grid.SetValueAtLocation(pos.x, pos.y, 0);
                }
            }
        }
    }


    private void PlaceBridgeTiles(Vector3Int start, Vector3Int end)
    {
        bool horizontal = start.y == end.y;
        bool vertical = start.x == end.x;
        
        
        if (bridgeTiles == null || bridgeTiles.Length == 0 || bridgeTiles[0] == null)
        {
            Debug.LogWarning("No bridgeTiles configured; skipping bridge placement.");
            return;
        }

        List<Vector3Int> bridgeCells = new List<Vector3Int>();

        if (start.x == end.x) // vertical bridge
        {
            int y0 = Mathf.Min(start.y, end.y);
            int y1 = Mathf.Max(start.y, end.y);
            for (int y = y0; y <= y1; y++)
            {
                var pos = new Vector3Int(start.x, y, 0);
                if (IsWater(pos) || bridgeTiles.Contains<TileBase>(tilemap.GetTile(pos)))
                {
                    tilemap.SetTile(pos, bridgeTiles[0]); // bridge floor
                    grid.SetValueAtLocation(pos.x, pos.y, 0);
                    bridgeCells.Add(pos);
                }
            }
        }
        else if (start.y == end.y) // horizontal bridge
        {
            int x0 = Mathf.Min(start.x, end.x);
            int x1 = Mathf.Max(start.x, end.x);
            for (int x = x0; x <= x1; x++)
            {
                var pos = new Vector3Int(x, start.y, 0);
                if (IsWater(pos) || bridgeTiles.Contains<TileBase>(tilemap.GetTile(pos)))
                {
                    tilemap.SetTile(pos, bridgeTiles[0]); // bridge floor
                    grid.SetValueAtLocation(pos.x, pos.y, 0);
                    bridgeCells.Add(pos);
                }
            }
        }
        else
        {
            Debug.LogWarning("Non-axis-aligned bridge requested; handling diagonals not supported.");
            return;
        }

        // Pass 2: add railings around placed bridge cells
        foreach (var pos in bridgeCells)
        {
            Vector3Int left = pos + Vector3Int.left;
            Vector3Int right = pos + Vector3Int.right;
            Vector3Int up = pos + Vector3Int.up;
            Vector3Int down = pos + Vector3Int.down;

            bool hasLeft = tilemap.GetTile(left) == bridgeTiles[0];
            bool hasRight = tilemap.GetTile(right) == bridgeTiles[0];
            bool hasUp = tilemap.GetTile(up) == bridgeTiles[0];
            bool hasDown = tilemap.GetTile(down) == bridgeTiles[0];

            // --- Vertical bridge: put railings on left & right ---
            if (hasUp || hasDown) // connected vertically
            {
                if (IsWater(left))
                {
                    tilemap.SetTile(left, bridgeTiles[1]); // left railing
                    grid.SetValueAtLocation(left.x, left.y, 1);
                }
                if (IsWater(right))
                {
                    tilemap.SetTile(right, bridgeTiles[2]); // right railing
                    grid.SetValueAtLocation(right.x, right.y, 1);
                }
            }

            // --- Horizontal bridge: put railings on top & bottom ---
            if (hasLeft || hasRight) // connected horizontally
            {
                if (IsWater(up))
                {
                    tilemap.SetTile(up, bridgeTiles[3]); // top railing
                    grid.SetValueAtLocation(up.x, up.y, 1);
                }
                if (IsWater(down))
                {
                    tilemap.SetTile(down, bridgeTiles[4]); // bottom railing
                    grid.SetValueAtLocation(down.x, down.y, 1);
                }
            }

            
        }

        //pass 3 add railings
        foreach(var pos in bridgeCells)
        {
            Vector3Int left = pos + Vector3Int.left;
            Vector3Int right = pos + Vector3Int.right;
            Vector3Int up = pos + Vector3Int.up;
            Vector3Int down = pos + Vector3Int.down;

            bool hasLeft = tilemap.GetTile(left) == bridgeTiles[0];
            bool hasRight = tilemap.GetTile(right) == bridgeTiles[0];
            bool hasUp = tilemap.GetTile(up) == bridgeTiles[0];
            bool hasDown = tilemap.GetTile(down) == bridgeTiles[0];

            if (hasUp && hasRight)
            {
                tilemap.SetTile(pos + Vector3Int.up + Vector3Int.right, bridgeTiles[9]);
                tilemap.SetTile(pos + Vector3Int.down + Vector3Int.left, bridgeTiles[5]);
                grid.SetValueAtLocation((pos + Vector3Int.up + Vector3Int.right).x, (pos + Vector3Int.up + Vector3Int.right).y, 1);
                grid.SetValueAtLocation((pos + Vector3Int.down + Vector3Int.left).x, (pos + Vector3Int.down + Vector3Int.left).y, 1);
            }

            if (hasUp && hasLeft)
            {
                tilemap.SetTile(pos + Vector3Int.up + Vector3Int.left, bridgeTiles[10]);
                tilemap.SetTile(pos + Vector3Int.down + Vector3Int.right, bridgeTiles[6]);
                grid.SetValueAtLocation((pos + Vector3Int.up + Vector3Int.left).x, (pos + Vector3Int.up + Vector3Int.left).y, 1);
                grid.SetValueAtLocation((pos + Vector3Int.down + Vector3Int.right).x, (pos + Vector3Int.down + Vector3Int.right).y, 1);
            }


            if (hasDown && hasRight)
            {
                tilemap.SetTile(pos + Vector3Int.down + Vector3Int.right, bridgeTiles[11]);
                tilemap.SetTile(pos + Vector3Int.up + Vector3Int.left, bridgeTiles[7]);
                grid.SetValueAtLocation((pos + Vector3Int.down + Vector3Int.right).x, (pos + Vector3Int.down + Vector3Int.right).y, 1);
                grid.SetValueAtLocation((pos + Vector3Int.up + Vector3Int.left).x, (pos + Vector3Int.up + Vector3Int.left).y, 1);
            }

            if (hasDown && hasLeft)
            {
                tilemap.SetTile(pos + Vector3Int.down + Vector3Int.left, bridgeTiles[12]);
                tilemap.SetTile(pos + Vector3Int.up + Vector3Int.right, bridgeTiles[8]);
                grid.SetValueAtLocation((pos + Vector3Int.down + Vector3Int.left).x, (pos + Vector3Int.down + Vector3Int.left).y, 1);
                grid.SetValueAtLocation((pos + Vector3Int.up + Vector3Int.right).x, (pos + Vector3Int.up + Vector3Int.right).y, 1);
            }  
        }
        // Widen both ends of the bridge
        if (IsLand(start)) EnsureLandMatchesBridge(start, horizontal);
        if(IsLand(end))EnsureLandMatchesBridge(end, horizontal);
    }

    private class BridgeCandidate
    {
        public Vector3Int start;
        public Vector3Int end;
        public int length;
        public float score;
    }

    // --- Vegetation ---
    /// <summary>
    /// Place plants on grass tiles using treeDensity and flowerDensity.
    /// Trees/bamboo are collidable and use grid value 4.
    /// Flowers are non-collidable and use grid value 3.
    /// </summary>
    // --- Vegetation ---
    private void AddPlants()
    {
        // If no plant arrays, just return
        bool haveNonColl = nonCollidablePlants != null && nonCollidablePlants.Length > 0;
        bool haveCollidable = collidablePlants != null && collidablePlants.Length > 0;
        if (!haveNonColl && !haveCollidable)
            return;

        // clamp requested densities
        float t = Mathf.Clamp01(treeDensity);
        float f = Mathf.Clamp01(flowerDensity);

        // IMPORTANT: if the corresponding plant arrays are missing,
        // zero out their probability so their thresholds are not used.
        if (!haveCollidable) t = 0f;
        if (!haveNonColl) f = 0f;

        // Normalize only if the combined probability would exceed 1.
        // This keeps the intended relative proportions while preventing overlap.
        float sum = t + f;
        if (sum > 1f && sum > 0f)
        {
            t = t / sum;
            f = f / sum;
            // now t + f == 1
        }

        for (int x = 0; x < map_width; x++)
        {
            for (int y = 0; y < map_height; y++)
            {
                // only consider pure grass tiles (tileId 0) for vegetation
                if (noiseGrid[x][y] != 0) continue;

                float r = UnityEngine.Random.value;

                // First attempt collidable plants (trees/bamboo) when in threshold AND available
                if (r < t && haveCollidable)
                {
                    var tile = collidablePlants[UnityEngine.Random.Range(0, collidablePlants.Length)];
                    tilemap.SetTile(new Vector3Int(x, y, 0), tile);
                    grid.SetValueAtLocation(x, y, 1); // 4 -> collidable plant (tree)
                    noiseGrid[x][y] = 4;
                }
                // Otherwise try non-collidable plants (flowers), using threshold shifted by t
                else if (r < t + f && haveNonColl)
                {
                    var tile = nonCollidablePlants[UnityEngine.Random.Range(0, nonCollidablePlants.Length)];
                    tilemap.SetTile(new Vector3Int(x, y, 0), tile);
                    grid.SetValueAtLocation(x, y, 0); // 3 -> non-collidable (flower)
                    noiseGrid[x][y] = 3;
                }
            }
        }
    }


    // --- Entities ---
    private void SpawnEntities()
    {
        var player = GameObject.Find("Player");
        var grassCoords = new List<Vector2>();

        for (int x = 0; x < map_width; x++)
        {
            for (int y = 0; y < map_height; y++)
            {
                // Treat only *pure* grass (0) as spawnable (plants are marked 3 or 4)
                if (grid.GetValueAtLocation(x, y) == 0)
                {
                    grassCoords.Add(new Vector2(x, y));
                }
            }
        }

        // Spawn enemies on grass with probability enemyFrequency
        for (int i = 0; i < grassCoords.Count; i++)
        {
            if (UnityEngine.Random.value <= enemyFrequency && enemies != null && enemies.Length > 0)
            {
                int type = UnityEngine.Random.Range(0, enemies.Length);
                var pos = grassCoords[i]; // center in cell
                var newEnemy = Instantiate(enemies[type], pos, quaternion.identity);
                EntityManager.Instance.RegisterEntity(newEnemy, Vector2Int.RoundToInt(pos));
                var eb = newEnemy.GetComponent<EnemyBehavior>();
                if (eb != null) eb.grid = grid;
            }
        }
        if (grassCoords.Count == 0)
        {
            Debug.LogWarning("No grass cells found for player spawn.");
        }
        else if (player != null)
        {
            var spawn = grassCoords[UnityEngine.Random.Range(0, grassCoords.Count)];
            player.transform.position = spawn;
            EntityManager.Instance.RegisterEntity(player, Vector2Int.FloorToInt(spawn));
        }
    }

    // --- Tile queries ---
    private bool IsLand(Vector3Int pos)
    {
        var t = tilemap.GetTile(pos);
        if (t == null) return false;

        // grass is terrainTiles[0]; bridges and collidable plants count as land for floodfill
        bool isGrass = terrainTiles != null && terrainTiles.Length > 0 && t == terrainTiles[0];
        bool isBridge = bridgeTiles != null && bridgeTiles.Any(bt => bt != null && t == bt);
        bool isCollidablePlant = collidablePlants != null && collidablePlants.Any(pt => pt != null && t == pt);

        return isGrass || isBridge || isCollidablePlant;
    }

    private bool IsWater(Vector3Int pos)
    {
        var t = tilemap.GetTile(pos);
        if (t == null) return false;

        // Safer than range-slicing for broad C# compatibility
        bool hasShallow = terrainTiles != null && terrainTiles.Length > 1 && t == terrainTiles[1];
        bool hasDeep = terrainTiles != null && terrainTiles.Length > 2 && t == terrainTiles[2];

        return hasShallow || hasDeep;
    }

    // Unused helper kept for future use
    private bool HasShoreOffset(Vector3Int pos, Vector3Int direction, int offset)
    {
        for (int i = 0; i < offset; i++)
        {
            pos += direction;
            if (!IsWater(pos)) return false;
        }
        return true;
    }
}

