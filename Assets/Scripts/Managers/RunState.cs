using System;
using UnityEngine;

/// <summary>
/// The run in progress: realm and floor, the per-floor layout seed, and where this floor's stairs are.
///
/// Static rather than a scene singleton for the same reason <see cref="IslandManager"/> is: nothing
/// here needs a Unity lifecycle, and the level has to be rebuildable without rewiring a scene object.
/// <see cref="BeginRun"/> is called once per visit to the dungeon scene, which is also what resets the
/// run after a death - no separate death hook is needed.
/// </summary>
public static class RunState
{
    private const string RealmResourcePath = "Realms";

    /// <summary>Raised after a floor change. The level generator listens and rebuilds the floor.</summary>
    public static event Action FloorChanged;

    private static RunSequence sequence;
    private static RealmDefinition[] realms;
    private static int runSeed;
    private static System.Random rng;
    private static bool hasStairs;
    private static Vector2Int stairsCell;

    public static bool HasRun => sequence != null;
    public static int RealmIndex => sequence?.RealmIndex ?? 0;
    public static int FloorIndex => sequence?.FloorIndex ?? 0;
    public static bool IsComplete => sequence?.IsComplete ?? false;
    public static bool IsBossFloor => sequence?.IsBossFloor ?? false;

    public static string RealmName => CurrentRealm?.displayName ?? "Plains";

    /// <summary>How this realm's enemies notice the player (GDD 13.4).</summary>
    public static AggroMode EnemyAggroMode => CurrentRealm != null ? CurrentRealm.aggroMode : AggroMode.RoomWide;

    /// <summary>This realm's metal for ore veins, or null when the realm defines none.</summary>
    public static ItemDefinition Metal => CurrentRealm != null ? CurrentRealm.metal : null;

    /// <summary>
    /// This realm's enemy prefabs, or null when it defines none - the generator then falls back to its own
    /// serialized pool, so an unauthored realm keeps working.
    /// </summary>
    public static GameObject[] EnemyPool =>
        CurrentRealm != null && CurrentRealm.enemies != null && CurrentRealm.enemies.Length > 0
            ? CurrentRealm.enemies
            : null;

    /// <summary>
    /// The run's seeded random source. Loot, material drops and ore yields all roll here, so a test can pin
    /// a seed and assert exact outcomes instead of sampling a distribution.
    /// </summary>
    public static System.Random Rng => rng ?? (rng = new System.Random(runSeed));

    /// <summary>1-based, for the HUD. Storage stays 0-based.</summary>
    public static int DisplayFloor => FloorIndex + 1;

    public static int FloorSeed => RunSequence.FloorSeed(runSeed, RealmIndex, FloorIndex);

    /// <summary>Stairs on this floor. False on a boss floor, which ends the realm instead.</summary>
    public static bool HasStairs => hasStairs;
    public static Vector2Int StairsCell => stairsCell;

    /// <summary>Realm assets, lowest index first. Cached until the next run starts.</summary>
    private static RealmDefinition[] Realms
    {
        get
        {
            if (realms == null)
            {
                RealmDefinition[] found = Resources.LoadAll<RealmDefinition>(RealmResourcePath);
                Array.Sort(found, (a, b) => a.realmIndex.CompareTo(b.realmIndex));
                // The fallback keeps a run playable before anyone authors the asset, which is the
                // state M1 ships in; the asset wins as soon as it exists.
                realms = found.Length > 0 ? found : new[] { BuiltInPlains() };
            }

            return realms;
        }
    }

    private static RealmDefinition CurrentRealm
    {
        get
        {
            RealmDefinition[] all = Realms;
            return all[Mathf.Clamp(RealmIndex, 0, all.Length - 1)];
        }
    }

    private static RealmDefinition BuiltInPlains()
    {
        RealmDefinition plains = ScriptableObject.CreateInstance<RealmDefinition>();
        plains.realmIndex = 0;
        plains.displayName = "Plains";
        plains.floorsInRealm = 2;
        plains.aggroMode = AggroMode.RoomWide;
        // Iron ships with the project, so ore veins appear before anyone has run the content menu item.
        plains.metal = Resources.Load<ItemDefinition>("ItemDefinitions/Materials/Iron");
        return plains;
    }

    /// <summary>Start a fresh run. Re-reads the realm assets so authoring one takes effect immediately.</summary>
    public static void BeginRun(int seed)
    {
        realms = null;
        runSeed = seed;
        rng = new System.Random(seed);

        RealmDefinition[] all = Realms;
        int[] floors = new int[all.Length];
        for (int i = 0; i < all.Length; i++)
        {
            floors[i] = all[i].floorsInRealm;
        }

        sequence = new RunSequence(floors);
        ClearStairs();
        // Discovery is per run (T07): permanent blueprint unlocks are M2/M3.
        RecipeDiscovery.Reset();
    }

    /// <summary>Record where this floor's stairs are, so the interaction and the minimap agree.</summary>
    public static void SetStairs(Vector2Int cell)
    {
        hasStairs = true;
        stairsCell = cell;
    }

    public static void ClearStairs()
    {
        hasStairs = false;
        stairsCell = default;
    }

    /// <summary>
    /// Descend. Rebuilds the floor through <see cref="FloorChanged"/>; when there is no floor left the
    /// run completes instead and nothing is raised, because there is nothing to build.
    /// </summary>
    public static void AdvanceFloor()
    {
        if (sequence == null || sequence.IsComplete)
        {
            return;
        }

        sequence.AdvanceFloor();
        if (sequence.IsComplete)
        {
            return;
        }

        ClearStairs();
        FloorChanged?.Invoke();
    }
}
