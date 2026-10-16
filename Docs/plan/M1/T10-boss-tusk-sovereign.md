# T10 — Tusk Sovereign (single-tile simplified)

**Depends on:** T02 (boss floor, gate, run completion), T03 (one action per turn), T06 (drops), T09 (enemy
data patterns).
**GDD:** 13.2 (boss rules), 13.3 (Tusk Sovereign row), 3.3 (boss gate).
**This is the last task in M1.** Multi-tile bosses are explicitly M2.

## Goal

A real boss fight at the end of the plains: an open arena, a telegraphed charge, a phase change at 50%, a
strict one-action-per-turn budget, and a proper reward.

## Scope boundary (read this first)

M1 ships the boss as a **single-tile entity**. The GDD's multi-tile body (2x2), the rubble it leaves behind,
and Enfeeblement immunity are **M2**. Everything in this task must be built so the M2 upgrade changes the
body size, not the turn logic.

## Files

| Path | Change |
| --- | --- |
| `Assets/Scripts/Enemy/BossController.cs` | new: boss state machine on top of the enemy contract |
| `Assets/Scripts/Enemy/BossTurnPlanner.cs` | new pure: chooses the single action for a round |
| `Assets/Scripts/Level Generator/RoomFirstDungeonGenerator.cs` | spawn the boss in the boss room, no other enemies |
| `Assets/Scripts/Item/LootGen.cs` or boss drop path | boss-specific rewards |
| `Assets/Resources/Items/Equipments/Tusk Cleaver.asset` | new item definition (drop-only, not craftable) |
| `Assets/Tests/EditMode/BossTurnPlannerTests.cs`, `ChargeLaneTests.cs` | new pure tests |
| `Assets/Tests/PlayMode/BossFightTests.cs` | new behaviour tests |

## Implementation notes

1. **One action per turn, absolutely.** The boss takes exactly one action per round, like every other
   entity. No design or implementation may give it a second action. Add an explicit counter assertion in the
   PlayMode test: over N rounds, the number of boss actions is exactly N (minus recovery rounds, which
   still count as the round's action).
2. **Action set.**
   - `Charge`: spends the round marking a **2-tile-wide lane** across the arena. On its next action it
     charges that lane: 20 damage and knockback to everything in the lane, then it must spend its following
     action recovering.
   - `Gore`: 15 damage to an adjacent player, when the player is already close.
   - `Roar`: alerts the arena (in a boss room this is mostly flavour; keep it cheap or omit it).
3. **Phase 2 at 50% health, once.** Transition fires exactly once. It removes the post-charge recovery, so
   the boss can charge every other round, and adds `Summon Boar`: summon one Pig Yao, which costs its
   action for that round.
4. **Pure planner.** `BossTurnPlanner.Decide(state, playerPosition, phase, cooldowns)` returns the single
   action for the round. All decisions live here so they are testable without a scene; `BossController` only
   executes.
5. **Charge lane as pure geometry.** `ChargeLane.Cells(origin, direction, length)` returns the affected
   cells; tests cover horizontal, vertical and wall-clipped lanes. The charge stops at a wall or the arena
   edge.
6. **Arena and gate.** The arena comes from T01 (large, open, no interior walls). The room locks when the
   player enters; the gate/run-completion hook already exists from T02 — this task makes the boss death the
   thing that opens it.
7. **Rewards, exactly once.** On death: the Tusk Cleaver (a drop-only weapon — it must **not** appear in any
   loot table or crafting list), one guaranteed rune, and 2-3 rare materials. Guard against double
   granting.
8. **Telegraph readability.** The marked lane must be visible for a full round before it hurts, reusing the
   existing low-opacity red overlay approach from the ranged enemy telegraph. Art is a placeholder in M1.
9. **Do not build** rubble terrain, multi-tile collision, Enfeeblement immunity, or unique boss-arena art.

## Test list (EditMode, pure)

- Planner: at 100% health with the player far away → `Charge` (mark); with the mark already placed → charge
  execution; with recovery pending → recovery (no attack); with the player adjacent → `Gore`.
- Planner: at ≤50% health the recovery state is never produced, and `Summon Boar` is available.
- Phase transition happens exactly once, even if health drops far below 50% in one hit.
- Charge lane geometry: length, width of 2, clipping at walls and arena edges, symmetry.
- Damage application: only entities inside the lane take damage; an entity that stepped out is not hit.
- Rewards: the reward set is granted once; the Tusk Cleaver is not craftable and is absent from the loot
  table.

## Test list (PlayMode)

- Entering the boss room locks it; the boss takes exactly one action per round across at least 6 rounds.
- The marked lane is cleared after the charge resolves.
- Killing the boss opens the gate and completes the run (victory placeholder from T02).
- The player can still die during the fight and the run ends correctly.

## Verification

1. EditMode + PlayMode tests green.
2. `execute_method` → `MapgenDiagnostics.RoomBuilderStress` (boss room generation) and `TurnPipeline`.
3. `execute_method` → `PlayDriver.Status`; `Tools/Play/Audit Enemy Rooms` (no stray enemies in the boss
   room).
4. Designer checkpoint: fight the boss. This is the acceptance test that matters — confirm the charge is
   readable and dodgeable, that it never acts twice in a round, and that phase 2 feels different but fair.

## Checkpoint

Report the action set as implemented, the round-by-round action counts observed in the PlayMode test, and
the exact reward list. Ask the designer to confirm the boss's difficulty and the charge telegraph window
before committing.

## Out of scope

Multi-tile boss body, rubble terrain, summoned adds beyond the single Pig Yao, Enfeeblement immunity, unique
boss tileset, the other two bosses.
