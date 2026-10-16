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
