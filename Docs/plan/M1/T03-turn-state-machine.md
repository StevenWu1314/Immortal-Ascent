# T03 — Turn state machine and action economy

**Depends on:** T01 (floors exist), T02 (a run exists).
**GDD:** section 6 (turn system and action economy) — specifically 6.1-6.4.
**Fixes:** GDD gap B4 — blocked moves consume a turn, entering aim mode grants a free enemy turn without
locking input, and two enemy-turn coroutines can run at once. Also removes combat from inside
`EntityManager.MoveEntity`.

## Goal

Turns are predictable: free movement out of combat, exactly one action per turn in combat, failed actions
cost nothing, and the input lock is never lost or double-held.

## Files

| Path | Change |
| --- | --- |
| `Assets/Scripts/Player/Controls.cs` | validate before consuming a turn; aim mode free; sword/bow swap; free movement |
| `Assets/Scripts/Managers/TurnManager.cs` | single-flight enemy phase; ownership-checked lock release; end-of-round phase |
| `Assets/Scripts/Managers/EntityManager.cs` | stop resolving combat inside `MoveEntity` |
| `Assets/Scripts/Enemy/EnemyBehavior.cs` | keep behaviour, adapt to the new phase contract |
| `Assets/Scripts/Enemy/RangeEnemyBehavior.cs` | same |
| `Assets/Scripts/Player/PlayerStats.cs` | buff/status duration decrement moves to end of round |
| `Assets/Scripts/TurnRules.cs` | new pure rules helper |
| `Assets/Tests/EditMode/TurnRulesTests.cs` | new pure tests |
| `Assets/Tests/PlayMode/TurnFlowTests.cs` | new behaviour tests |

## Implementation notes

1. **Pure extraction first.** Add `TurnRules` (static, no MonoBehaviour) with at least:
   - `bool IsTurnBased(int islandId, int livingEnemiesOnIsland)` — turn-based mode is simply "a living
     enemy shares the player's island".
   - `bool ConsumesTurn(TurnAction action)` — move / melee attack / bow shot / use item / place torch /
     mine ore consume a turn; opening a chest, equipping, applying runes, crafting, levelling up and
     entering or leaving aim mode do not.
   - `bool IsMovementAllowed(...)` — blocked by terrain, island lock, or a Root status (T05 adds the
     status hook; leave the parameter in place now).
   These are what the tests target; `Controls` becomes a thin caller.
2. **Validate before consuming.** Current code sets `takingTurn = true` and raises the turn event *before*
   checking terrain or the island gate. Reverse it: only a successful action raises the turn event. A
   blocked move must be silent and free — no enemy phase, no duration ticks.
3. **Free movement out of combat.** When `IsTurnBased` is false, movement does not raise the turn event
   and enemies do not act. Hold-to-move is acceptable (the existing `moveCooldown` can pace it). Entering
   an occupied island switches to turn-based immediately.
4. **Aim mode is free.** Entering/exiting aim mode must not raise the turn event. Only firing consumes a
   turn. Firing with no arrows is a no-op and costs nothing.
5. **Single-flight enemy phase.** Add an explicit owner/guard so a second `TakeTurns` cannot start while
   one is running, and release the input lock only if the releasing coroutine is the current owner. Today
   the first coroutine's `finally` releases the lock while a second coroutine is still running.
6. **End-of-round phase.** After every enemy has acted: tick statuses and buff durations down by one
   round, then clear the lock. Player stat buff durations currently decrement on every turn event,
   including blocked moves and aim-mode entry — move that decrement here so a buff lasts a fixed number of
   *rounds*. Keep a single place that does this so T05 can add damage-over-time to the same phase.
7. **Remove combat from the registry.** `EntityManager.MoveEntity` currently calls `PlayerStats.attack`
   when the destination is occupied. Move bump-attack resolution into the caller (Controls / a small
   `PlayerAttack` path). Behaviour must stay identical: walking into an adjacent enemy with a melee weapon
   equipped attacks it and costs the turn.
8. **Sword/bow selection.** Key 1 selects melee, key 2 selects ranged/aim. Selection is free. Melee
   requires an equipped melee weapon; bow requires an equipped bow. The vestigial always-true
   `swordDrawn` flag is replaced by real state — if the serialized field must be renamed, say so
   explicitly in the checkpoint (serialized values are keyed by field name).
9. Do not change enemy AI here — that is T04.

## Test list (EditMode, pure)

- `IsTurnBased` is false with zero living enemies on the island, true with one or more.
- `ConsumesTurn` returns true for the six actions and false for chest, equip, rune, craft, level-up, aim
  entry/exit.
- Movement allowance: open floor allowed; water/wall/OOB blocked; blocked while the island lock is active;
  allowed again once the island is cleared.
- Duration accounting: a 6-round buff loses exactly one round per completed round, not per input.

## Test list (PlayMode)

- Bumping a wall 3 times in turn-based mode does not let enemies act and does not shorten a buff.
- Entering aim mode does not let enemies act; firing does.
- Firing with zero arrows does not consume a turn.
- Only one enemy phase runs at a time (trigger aim entry during an enemy phase; assert enemy actions for
  that round did not double).
- The input lock is released after the enemy phase and not before.
- Walking into an adjacent enemy attacks it and ends the turn (bump-attack preserved after the
  `MoveEntity` cleanup).
- Out of combat, movement is free and enemies do not act.

## Verification

1. EditMode + PlayMode tests green.
2. `execute_method` → `MapgenDiagnostics.TurnPipeline` still passes.
3. `execute_method` → `PlayDriver.StepTowardNearestEnemy`, then `PlayDriver.Status`: the player closes
   distance and the enemy phase resolves once per player action.
4. Designer checkpoint: this task changes feel more than any other in M1. Walk one combat island and one
   empty island and confirm: free walking when clear, one action per turn in combat, no lost turns on wall
   bumps, aim mode feels free.

## Checkpoint

Report the turn rules table as implemented, whether `swordDrawn` was renamed, and the exact keys used for
sword/bow. Ask the designer to playtest before the commit — feel regressions here are expensive later.

## Out of scope

Enemy awareness, roaming and line of sight (T04). Statuses other than the hook parameter (T05). Pathfinding.

## Outcome (implemented)

- `TurnRules` (pure): `IsTurnBased(islandId, livingEnemies)` - a living enemy in the player's room;
  `ConsumesTurn(action)`; `IsMovementAllowed(terrain, island, rooted)`, with `rooted` left as the T05 hook.
- The table as implemented:

| Action | Costs a turn |
| --- | --- |
| Move | yes, while an enemy shares the room |
| Melee attack / bow shot | yes, when the action actually lands |
| Use item / place torch / mine ore | yes (the call sites arrive with T05/T06) |
| Open chest / equip / apply rune / craft / level up | no |
| Enter or leave aim mode | no |

- `Controls.TryMove(direction)` is now the whole movement path - `Update` feeds it the keys and
  `PlayDriver` drives it: validate, resolve, then consume. A refused step moves nothing and raises nothing.
- Bump-attack moved out of `EntityManager.MoveEntity` into `TryMove`: an enemy in the destination is
  attacked with the equipped melee weapon and that costs the turn; without a melee weapon it is a free
  no-op. `MoveEntity` now only moves, and refuses an occupied destination.
- `onMoveEvent` has exactly one raiser, `Controls.ConsumeTurn`, which consults the table first. That is
  what makes a free action structurally unable to start an enemy phase.
- `TurnManager` is single-flight (`IsEnemyPhaseRunning`) and the phase owns the input lock. The flag is a
  bool rather than the coroutine handle on purpose: `StartCoroutine` runs the body up to its first yield,
  and an empty queue never yields, so a handle could be stored for an already-finished coroutine.
- Aim-mode entry and exit raise nothing, so they neither grant the enemies a round nor lock input. Firing
  costs a turn only when a shot actually left the bow (an empty quiver is free).

### Deviations and notes

- `swordDrawn` was **deleted**, not renamed. Nothing read it once combat left `MoveEntity`, and the only
  real selection state is `aiming`: key 1 selects melee (`LeaveAimMode`), key 2 selects the bow
  (`TryEnterAimMode`). The Player prefab still stores `swordDrawn: 1`; Unity ignores the orphaned field.
- Buff durations stay on `onMoveEvent` instead of moving into a TurnManager end-of-round phase. Once the
  event fires exactly once per real turn, one tick per round is already true, and `MapgenDiagnostics`
  drives the tick through that subscription. T05 can hang damage-over-time on the same seam.
- `PlayDriver` no longer reimplements movement: it calls `Controls.TryMove`, so the harness exercises the
  real validation and bump-attack instead of a copy that silently drifted.
- Input cannot be synthesised, so the PlayMode suite drives `TryMove` and the pipeline's public API.

### A T02 mystery resolved

T02 reported one unreproducible `originWalkable=False` generation. It was a chest on the player's spawn
cell: `generateLootTiles` writes chest terrain (99, solid) at a random cell in each room without knowing
where the player stands, so roughly one floor in thirty spawned the player inside a chest - which also
made the whole floor report as unreachable from the spawn. The generator now reserves the spawn cell, and
`GeneratedFloors_NeverPutAChestUnderThePlayer` walks 30 seeded floors to keep it that way.

### Verified

EditMode 39/39 (3 new rule tests); PlayMode 8/8 (4 new turn-flow tests, plus the 30-floor generation
sweep with no reachability error); bridge play test: `MapgenDiagnostics.TurnPipeline` and
`ConsumableBuff` all checks passed, and `PlayDriver`'s steps logged `takingTurn=False` out of combat.
