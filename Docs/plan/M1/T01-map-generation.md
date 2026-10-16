# T01 — Map generation: floors, island counts, stairs, boss room

**Depends on:** T00.
**GDD:** section 5 (level generation), section 3.3 (floor exit), 5.2 (island sizes).
**Fixes:** GDD gap B2, corrected against measurement during T00. The generator does **not** crash and
produces **no** overlapping rooms (1200 runs across four settings: 0 threw, 0 overlapping pairs). The
real defect is **under-delivery**: the loop exits when the room queue drains instead of honouring the
request, so requesting 5 rooms yields 4-6, requesting 20 yields at most 12, and requesting 40 yields at
most 18. It also cannot express "6-8 islands per floor" as a contract. The index math is unsound even
though it has not tripped (`Random.Range(1, availableSpace.Count)` is used to index a list that can hold
one element, and a rejected candidate does not reset the `overlap` flag), so it still gets replaced.

## Goal

Generation produces a whole floor: 6-8 islands, exactly one exit-stairs island, and on the last floor of
a realm a larger boss room instead of stairs. It must be deterministic for a given seed and it must never
throw.

## Files

| Path | Change |
| --- | --- |
| `Assets/Scripts/Level Generator/ProceduralGeneraltionAlgorithm.cs` | extract a pure, seeded room-layout function; fix the crash and the overlap test |
| `Assets/Scripts/Level Generator/Rooms.cs` | room kinds; fix `body.y` → `body.yMin` in bounds checks; expose `availableDirection()` safely |
| `Assets/Scripts/Level Generator/RoomFirstDungeonGenerator.cs` | take a floor request instead of hardcoded 5 rooms; place stairs / boss room |
| `Assets/Scripts/Level Generator/TilePlacer.cs` | tile the boss/stairs islands; expose free cells for later ore-vein placement (T06 places them) |
| `Assets/Scripts/Level Generator/LevelValidation.cs` | validate stairs reachability and boss-room containment |
| `Assets/Scripts/Managers/IslandManager.cs` | no behaviour change expected; verify boss/stairs islands are registered |
| `Assets/Scripts/Level Generator/Editor/MapgenDiagnostics.cs` | extend `RoomBuilderStress` to the new counts |
| `Assets/Tests/EditMode/MapgenTests.cs` | new pure tests |

## Implementation notes

1. **Extract the layout into a pure function.** Target shape:
   `RoomLayout Generate(int seed, LayoutRequest request)` where `LayoutRequest` carries grid size,
   desired room count (6-8), min/max room size, corridor gap, and whether a boss room is required. Return
   room rects, kinds, and connections. Replace `UnityEngine.Random` inside that function with an injected
   `System.Random` so a seed reproduces a layout. Keep the MonoBehaviour call sites thin.
2. **Honour the requested room count.** Today the loop is
   `while (currentRooms < numberOfRooms && roomsQueue.Count != 0)`, so a drained queue silently shortens
   the floor (measured: request 5 -> 4-6, request 20 -> <=12, request 40 -> <=18). The loop condition is
   also evaluated once per outer iteration while an inner pass can add several rooms, which is why the
   request can be overshot. Rewrite so the count is a real contract: either produce the requested number
   or fail loudly with a reason, and never overshoot.
3. **Make the index math sound.** `numberBranch = Random.Range(1, availableSpace.Count)` followed by
   `direction = Random.Range(1, numberBranch)` and `availableSpace[direction]` uses a branch *count* as a
   list *index*; with exactly one free direction the index is out of range. This has not been observed to
   trip (0/1200 runs), but it is the reason the code is being replaced: index in `0 .. Count-1` and remove
   the entry you used.
4. **Reset the overlap flag per candidate.** `overlap` is declared outside the inner candidate loop and
   never cleared, so once one candidate is rejected every later candidate on that room is rejected too -
   a silent source of under-delivery. Test each candidate with a fresh local value.
5. **Bounds check.** `branchedRoom.body.y <= 0` is a no-op: for `BoundsInt`, `y == yMin`. Keep whichever
   form reads best; there is no bug to fix here.
5. **Fit 6-8 rooms on the grid.** With 11-19 tile rooms and a 10-tile gap, 8 rooms may not fit in
   120x120. Either raise `gridSize`, tighten the gap, or cap rooms by available space — and log clearly
   when the request cannot be met rather than silently producing fewer rooms.
6. **Room kinds.** Add `Stairs` and `Boss` to the existing `Starting` / `Combat` string field (keep the
   field type, do not rename it — serialized values are keyed by name).
7. **Stairs and boss rules.** Exactly one `Stairs` island on a non-boss floor, never the starting island.
   On a realm's last floor, one `Boss` island (larger: 15x15+ suggested in the GDD; M1 keeps it single
   tile boss but the arena is already big) and no stairs.
8. **Do not place ore veins here.** T06 owns vein spawning and mining. This task only guarantees free,
   reachable cells exist on combat islands for T06 to use.
9. **Chests.** Keep chest generation working; target 1-2 per floor rather than per room if that is cheap
   to express, otherwise leave as is and say so in the checkpoint.

## Test list (EditMode, pure)

- Same seed → identical layout (rooms, kinds, connections); different seeds → different layouts.
- **Requested count is a contract:** requesting N yields exactly N across many seeds - no silent
  undershoot (today: 5 → 4-6) and no overshoot (today: 6 when 5 was asked for). When a grid genuinely
  cannot fit N, the call fails with a reason rather than returning fewer rooms.
- No two rooms overlap (rect intersection test on every pair).
- Every room is fully inside the grid.
- All rooms are connected (BFS over the connection graph).
- A non-boss floor has exactly one `Stairs` room, and it is not the `Starting` room.
- A boss floor has exactly one `Boss` room, no `Stairs`, and the boss room meets the minimum size.
- Guard the latent index path: force a working room down to exactly one free direction and assert the
  code indexes within range and still returns a valid layout. It does not trip today (0/1200 runs), so
  this test is the proof the rewrite removed the hazard rather than hiding it.
- Characterization: keep the T00 test that pins the old generator's behaviour until the new generator
  fully replaces it, then delete it deliberately in this task's commit and say so in the checkpoint.

## Verification

1. EditMode tests green.
2. `execute_method` → `MapgenDiagnostics.RoomBuilderStress` (must generate many maps with zero thrown
   exceptions and zero overlaps; it already counts `threw`, `overlapping`, `atLeastTwo`).
3. `execute_method` → `MapgenDiagnostics.ChestWalkability` still passes.
4. `EditorDungeonGenerator` (`[MenuItem]` "Create Branching Dungeon") still produces a valid dungeon.
5. Play mode: `PlayDriver.Status` reports a spawn position; `Tools/Play/Audit Enemy Rooms` reports no
   stranded enemies.

## Checkpoint

Report: room counts produced across runs, the stress-test counters, and whether the old 5-room
characterization test was removed. Ask the designer to run the generator a few times in the editor and
confirm the maps look right (island spread, no rooms jammed against the grid edge).

## Out of scope

Ore veins and their mining (T06). Stairs *behaviour* — interaction, progression, boss gate (T02). The
boss entity itself (T10). Desert and tundra layouts.
