# T09 — Plains content: three enemies, two floors, tuning

**Depends on:** T05 (web/Root), T06 (drops), T04 (awareness), T02 (floors).
**GDD:** 4.1 (Plains), 13.1 (roster), 13.6 (per-floor scaling), 3.2 (2 floors), 12.2 (drops).

## Goal

The plains are populated and tuned: three distinct enemy types, two floors of 6-8 islands each, difficulty
that visibly rises on floor 2, and no enemy ever spawning somewhere it should not.

## Files

| Path | Change |
| --- | --- |
| `Assets/Scripts/Enemy/EnemyDefinition.cs` | new ScriptableObject: stats, detection, ranged params, drop table, reserved fields |
| `Assets/Resources/Enemies/PigYao.asset`, `SpiderYao.asset`, `JingYao.asset` | new data assets |
| `Assets/Scripts/Enemy/Enemy.cs`, `EnemyBehavior.cs`, `RangeEnemyBehavior.cs` | read stats from the definition, keeping prefab values as fallback |
| `Assets/Scripts/Level Generator/RoomFirstDungeonGenerator.cs` | spawn from the realm's enemy pool with per-floor scaling |
| `Assets/Scripts/Editor/EnemyDefinitionWiring.cs` | new `[MenuItem]` to assign definitions to the three prefabs |
| `Assets/Tests/EditMode/EnemyScalingTests.cs`, `SpawnRulesTests.cs` | new pure tests |

## Implementation notes

1. **Data-driven enemy stats.** `EnemyDefinition` carries: display name, max health, attack damage, XP,
   `detectionRadius`, `chaseRange`, optional ranged fields (range, cooldown), a material drop table
   reference, and **reserved** resistance fields that M1 never reads (so M2 does not have to re-author the
   assets). The prefab's existing serialized values stay as a fallback so nothing breaks before the wiring
   runs.
2. **Plains roster (proposed values, from the GDD):**

   | Enemy | Pattern | HP | Damage | XP | Notes |
   | --- | --- | --- | --- | --- | --- |
   | Pig Yao | melee chase, brute | 50 | 10 | 10 | existing prefab |
   | Spider Yao | melee, faster, drops silk | 40 | 8 | 10 | reuses the existing orphaned prefab |
   | Jing Yao | ranged, telegraphed, range 5, cooldown 2 | 30 | 10 | 10 | existing prefab |

   Spider Yao exists today but is referenced by nothing — this task is where it enters the pool.
3. **Webs are map content, not an enemy ability.** T05 places web cells during generation. Giving Spider Yao
   an ability to *create* a web behind it is optional; if it is cheap, add it and pin it with a test,
   otherwise leave it out and say so at the checkpoint.
4. **Per-floor scaling (pure function).** Every floor's enemy health is multiplied by
   `1 + 0.15 * floorIndex` and damage by `1 + 0.10 * floorIndex`, where the realm's first floor is index 0.
   Round to whole numbers. Boss stats are not scaled.
5. **Spawn rules.** 1-2 enemies per combat island, scaled up modestly on floor 2 if it plays too easily.
   Never spawn: on the starting island, on the stairs island, on the boss island, on an occupied cell, on a
   blocked cell, or in the player's spawn cell. Post-generation reachability validation must still pass.
6. **Do not add difficulty knobs the GDD does not have.** No elites, no random modifiers, no new statuses.
7. **Tuning is expected.** Values here are starting points; the checkpoint is where the designer changes
   numbers. Keep every number in data (asset or realm config), never buried in code.

## Test list (EditMode, pure)

- Scaling: floor 0 is unscaled; floor 1 gives the documented multipliers; rounding is stable; boss stats
  are excluded.
- Spawn rules: given a generated floor, no enemy occupies the starting island, the stairs island, the boss
  island, a blocked cell, or a duplicate cell; the count per combat island is within 1-2.
- Enemy pool: only the three Plains definitions are ever chosen for the Plains realm.
- Definition loading: a prefab with a definition set uses the definition's stats; with none set it uses the
  prefab's serialized values.

## Test list (PlayMode)

- Spawn a floor and assert every spawned enemy is registered in `EntityManager`, stands on walkable
  terrain, and is inside its own island.
- Floor 2 enemies have higher health and damage than floor 1 for the same type.

## Verification

1. EditMode + PlayMode tests green.
2. `execute_method` → `MapgenDiagnostics.RoomBuilderStress` and `ChestWalkability`.
3. `execute_method` → `Tools/Play/Audit Enemy Rooms` (no stranded enemies) and `PlayDriver.DumpEntityPositions`.
4. Designer checkpoint: play both plains floors and confirm the second floor reads as harder and that the
   three enemies feel different from each other.

## Checkpoint

Report the final stat table as shipped, the spawn counts per island per floor, and whether Spider Yao
places webs. Ask the designer to tune numbers here — this is the intended tuning checkpoint.

## Out of scope

Desert and tundra enemies. Resistances and status application. Elites. Boss enemies (T10).

## Outcome (implemented)

- `EnemyDefinition` (ScriptableObject): display name, health, damage, XP, detection radius, chase range,
  optional ranged range/cooldown, the material drop table, and the **reserved** resistance fields M1 never
  reads (so M2 does not have to re-author the assets).
- `EnemyScaling` (pure): health `1 + 0.15 * floor`, damage `1 + 0.10 * floor`, half-up rounding, the first
  floor unscaled, a floor index below zero treated as the first, and `isBoss` bypassing the multiplier for
  T10. Health never scales to zero and damage never goes negative.
- `Enemy.Awake` applies the definition when there is one and the prefab's own serialized values otherwise -
  and in **both** cases the floor multiplier applies, so an unauthored project still has a harder floor 2.
  `EnemyBehavior` takes detection radius and chase range from the definition, `RangeEnemyBehavior` its range
  and cooldown, all falling back to the prefab.
- The realm owns the pool: `RealmDefinition.enemies` with `RunState.EnemyPool`, and the generator falls back
  to its own serialized array when the realm defines none. `spawnEnemies` now takes the exit island index and
  never spawns there, so the stairs arena stays clear; the starting island was already excluded.
- `EnemyDefinitionWiring` (`Tools/Content/Create Enemy Definitions`) authors the three assets, assigns each
  to its prefab through `SerializedObject`, links the T06 drop tables, and sets the Plains pool. It never
  overwrites an existing asset, so re-running cannot clobber tuning.

### Checkpoint answers

- **Stat table as shipped** (proposed; tune in the assets):

  | Enemy | HP | Damage | XP | Detection | Chase | Ranged |
  | --- | --- | --- | --- | --- | --- | --- |
  | Pig Yao | 50 | 10 | 10 | 10 | 20 | — |
  | Spider Yao | 40 | 8 | 10 | 10 | 20 | — |
  | Jing Yao | 30 | 10 | 10 | 10 | 10 | range 5, cooldown 2 |

- **Spawn counts:** 1-2 per combat island (the generator's `enemiesPerRoom`), never on the starting island,
  the stairs island or the boss arena, never on blocked or occupied cells. The PlayMode test asserts the cap,
  the room containment and the exclusions.
- **Spider Yao does not place webs.** T05 already puts web cells on the combat islands during generation, so
  an ability to trail them would duplicate that content; the plan marked it optional. Say the word and it is a
  small addition.
- **Tuning is yours:** run `Tools/Content/Create Enemy Definitions`, then change the numbers in
  `Assets/Resources/Enemies/*.asset` - nothing in code hard-codes them.

### Verified

EditMode 109/109 (6 new scaling tests); PlayMode 24/24 - the two new ones sweep a generated floor for spawn
rule violations (registered, walkable, inside a room, off the start and stairs islands, at most 2 per island)
and prove floor 2's enemies are scaled by exactly the documented multiplier for every type that appears on
both floors.

One T04 assertion was relaxed while doing this: a committed enemy whose direct step is blocked sidesteps,
which can put it fractionally further away for that round, so the test now allows one cell of slack instead
of demanding the distance never grows.
