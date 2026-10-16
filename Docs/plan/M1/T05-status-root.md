# T05 — Minimal status system (Root only)

**Depends on:** T03 (end-of-round phase) and T04 (enemy contract).
**GDD:** 9.1 (Root), 9.3 (stacking = refresh, never stack), 9.4 (tick at end of round), 5.4 (spider web),
6.2 (a Rooted player cannot move but can still attack).
**Scope note:** M1 implements **only Root**. Fire, Poison, Enfeeblement and Explosive are M2. This task
exists now because the spider web needs it and because the status architecture should be proven by tests
before four more statuses depend on it.

## Goal

A status can be applied to the player, refreshes instead of stacking, blocks movement while active,
expires after a fixed number of rounds, and can be applied by walking into a spider web which is then
consumed.

## Files

| Path | Change |
| --- | --- |
| `Assets/Scripts/Status/StatusKind.cs` | new: enum `Root, Fire, Poison, Enfeeblement, Explosive` |
| `Assets/Scripts/Status/StatusSet.cs` | new: pure apply / refresh / tick / expire / query |
| `Assets/Scripts/Status/StatusController.cs` | new: MonoBehaviour wrapper for one entity |
| `Assets/Scripts/WebRegistry.cs` | new: web cells, following the `ChestRegistry` pattern |
| `Assets/Scripts/Level Generator/TilePlacer.cs` | sprinkle web cells on combat islands |
| `Assets/Scripts/Player/Controls.cs` | entering a web cell applies Root and clears the web; movement blocked while Rooted |
| `Assets/Scripts/Managers/TurnManager.cs` | call the end-of-round tick on the player's statuses |
| `Assets/Tests/EditMode/StatusSetTests.cs`, `WebRulesTests.cs` | new pure tests |
| `Assets/Tests/PlayMode/RootStatusTests.cs` | new behaviour tests |

## Implementation notes

1. **Pure core.** `StatusSet` holds entries of `(StatusKind kind, int magnitude, int roundsRemaining)`.
   Rules:
   - Applying a kind that is already present sets `roundsRemaining` back to the new duration and keeps the
     stronger magnitude. It never adds a second entry.
   - `Tick()` decrements every entry by one round and removes entries at zero. This is called once per
     round from the end-of-round phase, after enemies have acted.
   - `BlocksMovement` is true when Root is present.
   - Magnitude is meaningful for the damage statuses (M2); Root uses magnitude 0 and duration 2.
2. **Extensible but guarded.** The enum carries all five values so serialized data does not need
   renumbering later. In M1 only Root may be applied: reject any other kind with a clear warning and a
   test that pins that behaviour, so an unfinished status can never silently ship.
3. **Web cells.** Represent a web as terrain code `6` (walkable, triggers on entry) plus a `WebRegistry`
   entry for generation and rendering, mirroring how chests work today. Entering the cell applies Root
   (2 rounds) and destroys the web — which, exactly like opening a chest, requires **two writes**:
   `grid.SetValueAtLocation(x, y, 0)` and `IslandManager.MarkWalkable(cell)`. Doing only the first leaves
   the cell `Unassigned` and breaks `CanMoveTo`.
4. **Movement vs attack while rooted.** A rooted player cannot move. Attacking, using an item, aiming and
   shooting remain legal. Wire this through `TurnRules.IsMovementAllowed` from T03 rather than adding a
   second gate.
5. **Feedback.** The status must be visible: expose `StatusController.Active` for the HUD icon (drawn in
   T08) and reuse the existing floating-text helper for the "Rooted" message.
6. **Enemies do not get statuses yet.** The `StatusController` belongs on both player and enemies so M2 can
   use it, but in M1 nothing applies a status to an enemy. Do not add Apply-on-hit plumbing.

## Test list (EditMode, pure)

- Apply Root → present with the given duration; `BlocksMovement` true.
- Re-apply Root → still exactly one entry, duration reset to full, no stacking.
- Tick twice with duration 2 → Root expires; `BlocksMovement` false.
- Tick does not remove a status early (duration 3 needs 3 ticks).
- Applying Fire/Poison/Enfeeblement/Explosive in M1 is rejected and leaves the set unchanged.
- Web rules: entering a web cell applies Root; a web cell counts as walkable before entry; after
  destruction the grid value is 0.

## Test list (PlayMode)

- Walk into a web: the player cannot move for 2 rounds, then can.
- The web cell is gone afterwards and the player can walk through it.
- While rooted, the player can still attack an adjacent enemy.
- The rendered root icon state follows the status (assert via the controller, not pixels).

## Verification

1. EditMode + PlayMode tests green.
2. `execute_method` → `MapgenDiagnostics.ChestWalkability` (the web destroy path copies the chest path —
   prove the chest path still works) and `TurnPipeline`.
3. `execute_method` → `PlayDriver.StepTowardNearestEnemy` still resolves turns normally.
4. Designer checkpoint: get webbed on purpose and confirm the 2-round stop reads clearly and is not
   frustrating.

## Checkpoint

Report the status rules as implemented, the web terrain code chosen, and confirm the enum already carries
all five kinds. Ask the designer to confirm the Root duration (2 rounds) feels right.

## Out of scope

Fire, Poison, Enfeeblement, Explosive. Damage over time. Resistances and immunities. Enemy-applied
statuses. On-hit rune effects. Status icons beyond a minimal indicator (T08 draws it).

## Outcome (implemented)

- `StatusKind` carries all five kinds from the start (Root, Fire, Poison, Enfeeblement, Explosive), so
  serialized values never need renumbering. `StatusSet.IsImplemented` admits **Root only**: the other four
  are refused with a reason the caller logs, and a test pins that down.
- `StatusSet` (pure) implements GDD 9.3/9.4 - re-applying refreshes the duration and keeps the stronger
  magnitude without ever adding a second entry, `Tick()` decrements every entry and drops what reaches
  zero, and `BlocksMovement` is Root.
- `StatusController` wraps one entity's set, logs refusals, and exposes `Active`/`IsRooted` for the HUD
  (T08) plus `TickRound()`. `PlayerStats.Awake` attaches one when the prefab has none, so the scene needs
  no wiring; moving it onto the prefab later changes nothing.
- A web cell is terrain `6` (walkable, `WebRegistry.WebCode`) plus a `WebRegistry` entry - the chest split
  of terrain versus registry, minus the GameObject. `WebRegistry.Consume` performs the chest path's **two
  writes**, so the island snapshot keeps up and the cell can be walked through afterwards.
- `TilePlacer.generateWebTiles` sprinkles 0-2 webs per combat island: never the starting island, never a
  chest cell, never the same cell twice. The new `webTile` marker is optional and falls back to the bridge
  tile, so webs are visible until T09 supplies art.
- `Controls.TryMove` routes Root through the `TurnRules.IsMovementAllowed(..., rooted)` gate the plan asked
  for, and entering a web applies Root (2 rounds) and consumes it.
- `TurnManager` ticks the player's statuses in its `finally`, so a status expires once per completed round,
  after the enemies have acted.

### The one behaviour worth a second look

A rooted player who tries to walk **spends the round** (with a "Rooted" float) rather than getting a free
refusal. Otherwise a web on an otherwise empty island would leave the player unable to spend the turn Root
needs in order to expire - a soft lock, since M1 has no wait key. Terrain and island refusals stay free as
T03 specified; only Root costs. If a wait key arrives later, this can go back to a free refusal.

The bump-attack also had to move *before* the movement gate: attacking has to stay legal while rooted
(GDD 6.2), and the first cut refused it - the PlayMode test caught that.

### Verified

EditMode 69/69 (9 status-set tests, 4 web tests); PlayMode 14/14 - the two new ones assert that a web roots
for exactly two spent rounds and then releases, and that a rooted player still attacks an adjacent enemy.

Not verified through the bridge: `ChestWalkability` and `TurnPipeline` want the editor open, and it would
not start this session. The web destroy path is the same two writes as the chest path and is covered by
`Consume_DoesBothWrites`; the chest path itself was last seen green in the T03-era bridge run.
