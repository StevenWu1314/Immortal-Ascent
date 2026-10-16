using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class RoomFirstDungeonGenerator : MonoBehaviour
{
    // Start is called before the first frame update
    [Tooltip("Islands per floor, inclusive band (GDD 5: 6-8). Replaces the old string of a single " +
             "numberOfRooms, which the builder could undershoot or overshoot.")]
    [SerializeField] private int minRoomsPerFloor = 6;
    [SerializeField] private int maxRoomsPerFloor = 8;
    /// <summary>
    /// Inclusive enemy count per room (x = minimum, y = maximum). Replaces the hardcoded
    /// Random.Range(0, 2), whose exclusive upper bound made "0 or 1" an accident rather than
    /// a decision, and which left some rooms empty. Tunable in the Inspector.
    /// </summary>
    [SerializeField] private Vector2Int enemiesPerRoom = new Vector2Int(1, 2);

    [Header("Chests")]
    [Tooltip("Entity spawned at every chest cell. Leave empty to keep chest cells as bare terrain.")]
    [SerializeField] private GameObject chestPrefab;
    [Tooltip("Items a chest may contain. Chest-only; craft-only inscriptions simply stay out of this list.")]
    [SerializeField] private ItemDefinition[] chestLootPool;
    [Tooltip("Inclusive number of items rolled per chest (x = minimum, y = maximum).")]
    [SerializeField] private Vector2Int chestItemCount = new Vector2Int(1, 2);
    [SerializeField] private TilePlacer tilePlacer;
    
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject[] enemies;
    public int gridSize = 500;
    public Grid grid;

    /// <summary>The player instance this generator created, used for the reachability check.</summary>
    private GameObject spawnedPlayer;
    
    void Start()
    {
        // Origin MUST stay (0,0): every writer in TilePlacer calls
        // grid.SetValueAtLocation(x, y, ...) with raw tilemap cell coordinates (0..gridSize),
        // and entities index the grid with floor(transform.position). A non-zero origin makes
        // Grid's world->cell conversion (getXY) subtract it, so the player would read terrain
        // from a cell gridSize/2 away and every move would be blocked by a wall.
        EnsureGrid();

        // Entering the dungeon scene starts a run, which is also what resets it after a death:
        // the death path reloads the menu, and the next visit starts a fresh run from here.
        RunState.BeginRun(System.Environment.TickCount);
        RunState.FloorChanged += OnFloorChanged;

        generateBranchingDungeon(RunState.IsBossFloor);     
        
    }

    private void OnDestroy()
    {
        // A static event: an unbalanced subscription would keep this destroyed generator alive and
        // rebuilding floors through it after a scene change.
        RunState.FloorChanged -= OnFloorChanged;
    }

    /// <summary>Rebuild the level for the floor the run just moved to.</summary>
    private void OnFloorChanged()
    {
        generateBranchingDungeon(RunState.IsBossFloor);
    }

    /// <summary>
    /// Return the grid this generator writes into, creating it on first use.
    /// Creation lives here rather than only in <see cref="Start"/> so an editor tool can
    /// drive the same pipeline without a running scene: MapgenDiagnostics.DiagnoseRoomFirst
    /// used to call generateBranchingDungeon() with a null grid, and TilePlacer.PlaceTiles
    /// dereferenced it immediately. Grid.Current is set by the constructor, so
    /// IslandManager.Rebuild() and the diagnostics reporter find it too.
    /// </summary>
    public Grid EnsureGrid()
    {
        if (grid == null)
        {
            grid = new Grid(gridSize, gridSize, 1, new Vector3(0, 0, 0));
        }
        return grid;
    }
    /// <summary>
    /// Build one floor. <paramref name="bossFloor"/> makes the last island a large Boss arena instead
    /// of the Stairs room; the run state (T02) decides which floors those are, so the default keeps
    /// <see cref="Start"/> and the editor tools on a normal floor.
    /// </summary>
    public void generateBranchingDungeon(bool bossFloor = false)
    {
        // Self-heal the grid: at runtime Start() has already built it, but the editor
        // diagnostics entry point reaches this method directly.
        EnsureGrid();

        // Same run, same floor, same layout: (run seed, realm, floor) decides the seed.
        if (RunState.HasRun)
        {
            Random.InitState(RunState.FloorSeed);
        }

        // A repeated generation (editor diagnostics today, a restart later) must not inherit
        // the previous run's registrations. A stale player entry made EntityManager.MoveEntity
        // fail its TryGetValue(oldCell) lookup, leaving the player unable to move at all.
        if (EntityManager.Instance != null)
        {
            EntityManager.Instance.Reset();
        }
        spawnedPlayer = null;

        // Reuse the player across floors. A fresh instance would reset health, level and inventory -
        // the GDD carries all three between floors - and the old instance is still alive while the new
        // one wakes, so the player's singleton components (Equipments, Inventory) would destroy the new
        // player through their duplicate guards, leaving the floor with no player at all.
        PlayerStats[] existingPlayers = UnityEngine.Object.FindObjectsByType<PlayerStats>(FindObjectsSortMode.None);
        PlayerStats existingPlayer = existingPlayers.Length > 0 ? existingPlayers[0] : null;

        tilePlacer.clear();

        // The Grid outlives a generation. Clearing the tilemaps is not enough: the previous floor's
        // terrain (room rings = 1, chest cells = 99) stayed underneath the new floor and cut through
        // it, which showed up as isolated cells and unreachable chests on every descend.
        grid.Clear();
        // A new floor forgets what the minimap knew (GDD 14.5).
        if (Minimap.Instance != null)
        {
            Minimap.Instance.ResetForFloor();
        }
        List<Rooms> rooms = ProceduralGenerationAlgorithm.BrachingRooms(
            minRoomsPerFloor, maxRoomsPerFloor, gridSize, bossFloor);
        List<BoundsInt> roomsSpace = new List<BoundsInt>();
        int startingRoomIndex = -1;
        Vector2Int spawnCell = default;
        for (int i = 0; i < rooms.Count; i++)
        {
            roomsSpace.Add(rooms[i].body);
            if(rooms[i].type == "Starting")
            {
                startingRoomIndex = i;
                Vector3 spawn = rooms[i].body.center - new Vector3(0.5f, 0.5f);
                GameObject instance;
                if (existingPlayer != null)
                {
                    instance = existingPlayer.gameObject;
                    instance.transform.position = spawn;
                }
                else
                {
                    instance = Instantiate(player, spawn, quaternion.identity);
                }

                instance.GetComponent<Controls>().grid = grid;
                spawnCell = new Vector2Int((int) rooms[i].body.center.x, (int) rooms[i].body.center.y);
                EntityManager.Instance.RegisterEntity(instance, spawnCell);
                spawnedPlayer = instance;
            }
            
        }
        
        tilePlacer.PlaceTiles(roomsSpace, grid);
        List<Vector2Int> chestCells = tilePlacer.generateLootTiles(roomsSpace, grid);

        // A chest on the spawn cell is solid terrain (99) underneath the player, which made the whole
        // floor read as unreachable from the spawn - the "isolated, chest walled in" report that showed
        // up on roughly one floor in thirty. The loot pass does not know where the player stands, so the
        // cell is reserved here. Its floor tile stays; only the chest terrain and the chest go.
        if (chestCells.Remove(spawnCell))
        {
            grid.SetValueAtLocation(spawnCell.x, spawnCell.y, 0);
        }

        // Spider webs (T05) live on the combat islands. They have no objects to sweep, so the registry is
        // cleared with the floor rather than by destroying entities.
        WebRegistry.Reset();
        foreach (Vector2Int webCell in tilePlacer.generateWebTiles(roomsSpace, grid, startingRoomIndex))
        {
            WebRegistry.Register(webCell);
        }

        // Ore veins (T06) are the realm's metal, 2-4 to a floor. Like webs they have no objects to sweep, so
        // the registry is cleared with the floor.
        OreVeinRegistry.Reset();
        ItemDefinition metal = RunState.Metal;
        if (metal == null)
        {
            Debug.LogWarning("[GEN] the realm defines no metal; no ore veins were placed.");
        }
        else
        {
            foreach (Vector2Int veinCell in tilePlacer.generateOreVeins(roomsSpace, grid, spawnCell, 2, 4))
            {
                // The yield is rolled at generation time so a vein is finite, and so a save can hold it later.
                OreVeinRegistry.Register(veinCell, metal, Random.Range(3, 9));
            }
        }
        // Corridors must span the gap between rooms (Rooms.distanceApart) plus enough to
        // punch through the neighbour's wall ring. A length shorter than the gap leaves the
        // corridor as a dead end and the player can never reach the next island.
        int corridorLength = (rooms.Count > 0 ? rooms[0].distanceApart : 10);
        tilePlacer.branchingCorridors(rooms, grid, corridorLength);
        tilePlacer.fillWater(grid);

        // Stairs live in the room T01 marked as Stairs. A boss floor has none: its arena ends the
        // realm instead, so the run state keeps no exit and the interaction cannot fire.
        RunState.ClearStairs();
        Rooms stairsRoom = rooms.Find(room => room.type == "Stairs");
        if (!bossFloor && stairsRoom != null)
        {
            Vector2Int stairsCell = new Vector2Int((int)stairsRoom.body.center.x, (int)stairsRoom.body.center.y);
            tilePlacer.MarkStairs(stairsCell);
            RunState.SetStairs(stairsCell);
        }

        spawnEnemies(roomsSpace, startingRoomIndex);
        spawnChests(chestCells);

        // Islands are assigned explicitly from the room layout rather than inferred from water
        // connectivity, so they stay correct however far the corridors happen to reach.
        // Corridor cells become a shared passage: the player may cross them, enemies may not.
        IslandManager.Rebuild(roomsSpace);

        ValidateReachability();
    }

    /// <summary>
    /// Spawn enemies only on cells that are genuinely free: walkable per the Grid and not
    /// already registered in EntityManager. Previously a random cell was used unchecked, so
    /// an enemy could land inside a wall -- unable to move, and with the island gate demanding
    /// it be killed before the player could leave.
    /// </summary>
    private void spawnEnemies(List<BoundsInt> roomsSpace, int startingRoomIndex)
    {
        if (enemies == null || enemies.Length == 0)
        {
            Debug.LogError("[GEN] no enemy prefabs assigned to RoomFirstDungeonGenerator; nothing spawned.");
            return;
        }

        GameObject[] oldEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject oldEnemy in oldEnemies)
        {
            // Destroy() is deferred and is a no-op in edit mode, so the editor diagnostics
            // path needs DestroyImmediate.
            if (Application.isPlaying)
            {
                Destroy(oldEnemy);
            }
            else
            {
                DestroyImmediate(oldEnemy);
            }
        }

        int minPerRoom = Mathf.Max(0, enemiesPerRoom.x);
        int maxPerRoom = Mathf.Max(minPerRoom, enemiesPerRoom.y);

        for (int n = 0; n < roomsSpace.Count; n++)
        {
            if (n == startingRoomIndex)
            {
                // Never place an enemy on the room the player spawns in.
                continue;
            }

            List<Vector2Int> candidates = FindFreeCells(roomsSpace[n]);
            int amountToSpawn = Random.Range(minPerRoom, maxPerRoom + 1);
            if (candidates.Count == 0 && amountToSpawn > 0)
            {
                Debug.LogWarning($"[GEN] room {n} {roomsSpace[n]} has no free walkable cell; " +
                                 "no enemies spawned there.");
                continue;
            }

            for (int i = 0; i < amountToSpawn && candidates.Count > 0; i++)
            {
                int pick = Random.Range(0, candidates.Count);
                Vector2Int cell = candidates[pick];
                candidates.RemoveAt(pick);   // one entity per cell

                int type = Random.Range(0, enemies.Length);
                GameObject newEnemy = Instantiate(enemies[type], new Vector3(cell.x, cell.y), quaternion.identity);
                EnemyBehavior enemy = newEnemy.GetComponent<EnemyBehavior>();
                if (enemy == null)
                {
                    Debug.LogError($"[GEN] enemy prefab '{enemies[type].name}' has no EnemyBehavior component; skipping.");
                    if (Application.isPlaying)
                    {
                        Destroy(newEnemy);
                    }
                    else
                    {
                        DestroyImmediate(newEnemy);
                    }
                    continue;
                }

                EntityManager.Instance.RegisterEntity(newEnemy, cell);
            }
        }
    }

    /// <summary>
    /// Spawn an entity chest on every cell generateLootTiles reported. The cell starts as terrain
    /// code 99 (solid), so the chest blocks movement until it is opened and the player opens it
    /// from a neighbour. Contents are rolled here, once, rather than at open time, so the level's
    /// loot is fully determined the moment it is generated.
    /// </summary>
    private void spawnChests(List<Vector2Int> cells)
    {
        // Clear chests left over from a previous generation, then the registry, so a stale
        // entry cannot answer for a cell that no longer holds one.
        Chest[] existing = UnityEngine.Object.FindObjectsByType<Chest>(FindObjectsSortMode.None);
        foreach (Chest old in existing)
        {
            if (old == null)
            {
                continue;
            }
            if (Application.isPlaying)
            {
                Destroy(old.gameObject);
            }
            else
            {
                DestroyImmediate(old.gameObject);
            }
        }
        ChestRegistry.Reset();

        if (cells == null || cells.Count == 0)
        {
            return;
        }
        if (chestPrefab == null)
        {
            Debug.LogError("[GEN] chest cells were generated but no chest prefab is assigned; " +
                           "they will be empty terrain.");
            return;
        }
        if (chestLootPool == null || chestLootPool.Length == 0)
        {
            Debug.LogWarning("[GEN] the chest loot pool is empty; every chest will open empty.");
        }

        int min = Mathf.Max(0, chestItemCount.x);
        int max = Mathf.Max(min, chestItemCount.y);

        foreach (Vector2Int cell in cells)
        {
            GameObject go = Instantiate(chestPrefab, new Vector3(cell.x+0.5f, cell.y+0.5f, 0f), Quaternion.identity);
            go.name = $"Chest {cell}";

            Chest chest = go.GetComponent<Chest>();
            if (chest == null)
            {
                Debug.LogError($"[GEN] chest prefab '{chestPrefab.name}' has no Chest component; skipping.");
                if (Application.isPlaying)
                {
                    Destroy(go);
                }
                else
                {
                    DestroyImmediate(go);
                }
                continue;
            }

            chest.SetContents(RollChestContents(Random.Range(min, max + 1)));

            // Register explicitly instead of relying on Chest.OnEnable: Unity does not invoke
            // OnEnable in edit mode, so the editor diagnostics path would generate chest cells
            // with no reachable entity. Chest's own OnEnable/OnDisable remain for chests created
            // at runtime by other means; re-registering the same chest is idempotent.
            ChestRegistry.Register(chest);
        }
    }

    /// <summary>Roll one chest's contents from the serialized pool. Duplicates are allowed.</summary>
    private List<ItemStack> RollChestContents(int count)
    {
        var rolled = new List<ItemStack>();
        if (chestLootPool == null || chestLootPool.Length == 0 || count <= 0)
        {
            return rolled;
        }

        for (int i = 0; i < count; i++)
        {
            ItemDefinition pick = chestLootPool[Random.Range(0, chestLootPool.Length)];
            if (pick == null)
            {
                continue;
            }
            rolled.Add(new ItemStack(pick, 1));
        }
        return rolled;
    }

    /// <summary>
    /// Every cell inside <paramref name="room"/> that the Grid calls walkable and that no
    /// entity occupies yet. The room rectangle includes a one-cell wall ring, so the interior
    /// is scanned from xMin+1 up to (not including) xMax-1; the walkability filter is what
    /// actually keeps enemies out of the ring, which is why this no longer needs the old
    /// hardcoded +3/-3 inset.
    /// </summary>
    private List<Vector2Int> FindFreeCells(BoundsInt room)
    {
        List<Vector2Int> free = new List<Vector2Int>();
        for (int x = room.xMin + 1; x < room.xMax - 1; x++)
        {
            for (int y = room.yMin + 1; y < room.yMax - 1; y++)
            {
                if (!Grid.IsWalkableTerrain(grid.GetValueAtLocation(x, y)))
                {
                    continue;
                }
                Vector2Int cell = new Vector2Int(x, y);
                if (EntityManager.Instance.IsOccupied(cell))
                {
                    continue;
                }
                free.Add(cell);
            }
        }
        return free;
    }

    /// <summary>
    /// Post-generation invariant check (BUG-14): every walkable cell must be reachable from
    /// where the player actually spawned, and every chest must have a reachable cell beside
    /// it. Reports instead of throwing, because an exception here would abort Start() and
    /// leave the level half-built; the log is what a human diagnoses from.
    /// </summary>
    private void ValidateReachability()
    {
        if (grid == null)
        {
            return;
        }
        if (spawnedPlayer == null)
        {
            Debug.LogWarning("[GEN] reachability not checked: no player was spawned (no 'Starting' room).");
            return;
        }

        Vector2Int origin = Grid.CellFromWorld(spawnedPlayer.transform.position);
        LevelValidation.ReachabilityResult result = LevelValidation.AnalyzeReachability(grid, origin);

        if (result.Ok)
        {
            Debug.Log($"[GEN] reachability OK: {result.Describe()}");
            return;
        }

        // Two different problems, two different severities. An unreachable walkable cell is by
        // definition one the player can never enter, so it cannot affect play - it is map noise, and
        // deserves a warning. A chest in such a pocket is real content the player can never open.
        // The player's own cell and terrain are in the message because a floor that is unreachable from
        // the spawn means the spawn is the thing to look at, not the corridors.
        string spawnState = $"player world={spawnedPlayer.transform.position} cell={origin} " +
                            $"terrain={grid.GetValueAtLocation(origin.x, origin.y)}";

        if (!result.ChestsReachable)
        {
            Debug.LogError($"[GEN] a chest is walled in: {result.Describe()}; {spawnState}. " +
                           "A corridor failed to reach it, so its loot is unreachable.");
            return;
        }

        Debug.LogWarning($"[GEN] floor is not fully connected: {result.Describe()}; {spawnState}. " +
                         "Those cells cannot be entered, so play is unaffected.");
    }
}
