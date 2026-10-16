# T04 — Enemy awareness, roaming and line of sight

**Depends on:** T03 (turn phases and the single-flight enemy phase exist).
**GDD:** section 6.5 (who acts), 6.6 (roaming), 6.7 (line of sight), 13.4 (awareness by realm).
**Fixes:** GDD gap B5 (awareness is island-only, `detectionRadius` is dead code, unaware enemies never
move) and gap B6 (line of sight is a no-op, so both sides shoot through walls).

## Goal

Enemies notice the player according to the realm's rule, wander while unaware, and can only shoot what
they can actually see.

## Files

| Path | Change |
| --- | --- |
| `Assets/Scripts/LineOfSight.cs` | new pure sight test |
| `Assets/Scripts/Enemy/EnemyBehavior.cs` | awareness states, roaming, use LOS where relevant |
| `Assets/Scripts/Enemy/RangeEnemyBehavior.cs` | shoot only with LOS; strafe when blocked |
| `Assets/Scripts/Player/Controls.cs` | aimed shots require LOS |
| `Assets/Scripts/Player/RangeAttackTilemap.cs` | highlight only tiles that are actually hittable |
| `Assets/Scripts/Managers/IslandManager.cs` | read-only use (island lookup for room-wide awareness) |
| `Assets/Tests/EditMode/LineOfSightTests.cs`, `AwarenessTests.cs` | new pure tests |
| `Assets/Tests/PlayMode/EnemyAwarenessTests.cs` | new behaviour tests |

## Implementation notes

1. **Line of sight as a pure function.** `LineOfSight.HasClearShot(ICellBlockLookup grid, Vector2Int from,
   Vector2Int to)`, using a Bresenham walk that excludes the endpoints. Blockers per GDD 5.4: walls and
   water (terrain 1 and out of bounds), rocks, huabiao pillars. Bushes, spider webs and crevasses do
   **not** block sight.
   **Important:** today `ClearLine` consults a single Tilemap reference that nothing paints. The grid is
   the authority for terrain, so query it. Obstacles are not yet represented as data; define the lookup
   interface so obstacle cells can register later, and note in the checkpoint that only terrain blocking
   is live in M1.
2. **Awareness, driven by realm data.** `RealmDefinition.aggroMode` already exists from T02:
   - `RoomWide` (Plains): the player entering an island alerts every enemy on that island.
   - `DetectionRadius` (Desert/Tundra, M2 content, but implement the path): an enemy notices the player
     within its `detectionRadius` **and** with clear line of sight. Awareness is not shared.
   Keep the state machine's existing `Idle / Chase / Return` names; add the transitions rather than
   replacing the enum, so serialized values keep working.
3. **Losing the player.** An aware enemy that exceeds `chaseRange`, or loses line of sight in radius mode,
   returns to its post and becomes unaware again (`Return` then `Idle`). Preserve the existing
   walk-home behaviour.
4. **Roaming.** Unaware enemies wander: one tile per round, inside their own island, never onto an occupied
   cell, never attacking. Extract the step choice as a pure function so it can be tested without a scene.
   Roaming stops the moment the enemy becomes aware.
5. **Only aware enemies act.** Unaware enemies that are not roaming still do nothing (no attack). Enemies
   never leave their island, aware or not.
6. **Ranged enemies respect sight.** `RangeEnemyBehavior` may only start its telegraph when it has line of
   sight. If it has no shot, keep the existing perpendicular strafe, but prefer a step that gains sight.
7. **Player shots respect sight.** The aim overlay and the actual shot must use the same
   `LineOfSight.HasClearShot`, so the highlight can never include an unhittable tile.
8. Pathfinding stays greedy dominant-axis. Do not add A* in M1. A cheap improvement is allowed: if the
   dominant axis is blocked, try the other one, and only then lose the turn.

## Test list (EditMode, pure)

- LOS: adjacent cells clear; a wall between them blocked; water blocked; out of bounds blocked; a rock or
  huabiao cell blocked; bushes, web and crevasses do **not** block; a long diagonal is symmetric
  (`A→B` equals `B→A`).
- Awareness: radius mode notices inside radius with LOS, does not notice inside radius without LOS, does
  not notice outside radius with LOS; room-wide mode alerts every enemy on the island regardless of
  distance.
- Transitions: `Idle → Chase` on alert; `Chase → Return` when the player is beyond `chaseRange`;
  `Return → Idle` on arrival; a returning enemy that re-notices the player goes back to `Chase`.
- Roaming: the chosen step is inside the island, never onto an occupied cell, and returns a "stay" step
  when boxed in.

## Test list (PlayMode)

- An enemy behind a wall does not shoot (no telegraph is created).
- An enemy in a different island does not notice the player in radius mode, but does in room-wide mode.
- An unaware enemy changes cell over several rounds (roaming is observable).
- Once aware, the enemy stops wandering and commits to the player.

## Verification

1. EditMode + PlayMode tests green.
2. `execute_method` → `MapgenDiagnostics.TurnPipeline`.
3. `execute_method` → `PlayDriver.StepTowardNearestEnemy` then `PlayDriver.Status`; and
   `Tools/Play/Audit Enemy Rooms` to confirm no enemy left its island.
4. Designer checkpoint: stand behind a wall from a ranged enemy and confirm it cannot hit you; watch an
   unaware enemy wander; step onto its island and confirm the room commits.

## Checkpoint

Report the awareness rules as implemented, the exact list of sight blockers, and the honest limitation
that obstacles are not yet data (only terrain blocks in M1). Ask the designer to verify the wall test in
play mode.

## Out of scope

Awareness sharing between enemies (not in the GDD). A* pathfinding. Statuses. Boss AI (T10).

## Outcome (implemented)

- `LineOfSight` (pure): `BlocksSight(terrain)` is the GDD 5.4 sight list - code 1 (wall, water, out of
  bounds) blocks; floor, the legacy codes, spider webs and chests do not. `HasClearShot` walks Bresenham
  exclusive of both endpoints and orders the endpoints canonically, so A->B always equals B->A. It takes a
  "is this cell blocked" delegate, which is what the tests drive, plus a Grid overload for callers.
- `Awareness` (pure): `Notices(mode, enemyIsland, playerIsland, distance, radius, hasSight)` implements
  GDD 6.5 - room-wide realms compare rooms, radius realms need range plus sight, and both fall back to the
  radius when the island map is missing (an editor-generated level). Corridors are not rooms.
  `Next(current, notices, shouldReturn, atPost)` is the Idle/Chase/Return cycle.
- `Roaming` (pure): `ChooseStep(from, rotation, canStepOnto, isOccupied)` returns one direction, or zero
  when boxed in.
- `RealmDefinition.aggroMode` (new field, default RoomWide) and `RunState.EnemyAggroMode` feed the rule, so
  the realm decides how its enemies notice the player.
- `EnemyBehavior`: the state field keeps its name and the enum keeps its member order (`AwarenessState`),
  so serialized values still mean the same thing. Awareness is re-evaluated on every player action and at
  the start of the enemy's own turn; Chase breaks off when beyond `chaseRange` - or when sight is lost in
  radius mode - and Return walks home and goes Idle. Idle enemies now roam one step per round instead of
  standing still. `queup` queues every enemy, because roaming is what Idle enemies do; the phase still only
  runs on a real player turn, so free out-of-combat travel costs no time.
- `RangeEnemyBehavior`: sight comes from the grid, the `Collidable Plants` lookup is gone, and a blocked
  archer prefers a step that opens a sight line before falling back to its strafe.
- `RangeAttackTilemap` paints only cells with a clear line, and `Controls` re-checks sight before a shot
  lands, so a wall (or an empty quiver) costs nothing.
- `playerDetected`, which nothing assigned, was deleted.

### Two cell spaces - the bug this uncovered

The range overlay has its own `Grid` under the player, so `tilemap.WorldToCell` returns player-relative
cells, while terrain lives in `Grid.Current`'s cells. The old code painted in overlay space and checked
sight in level space through a *different* Tilemap; mixing the two broke the preview outright
(`center=(17,52)` for a player at `(65,65)`). Both conversions are now explicit - and
`Controls.FindEnemyInCell`'s registry fallback converts the overlay cell to a level cell before looking an
enemy up, where it previously queried the wrong cell whenever the physics box missed.

### Verified

EditMode 56/56 (7 sight tests, 10 awareness and roaming tests); PlayMode 12/12 - the four new ones assert
that another room is not noticed in a room-wide realm, that roaming is observable and never leaves the
room, that an alerted enemy commits and closes the distance, and that the aim preview only paints cells
with a clear line.

Not verified through the bridge: `MapgenDiagnostics.TurnPipeline` and `PlayDriver.AuditEnemyRooms` want the
editor open, and the editor would not start in this session (batchmode does not need it). The PlayMode
suite drives the same pipeline - queue a phase through `TurnManager` and assert the resulting states - so
the same ground is covered, but those two harnesses should be re-run in the designer's session.
