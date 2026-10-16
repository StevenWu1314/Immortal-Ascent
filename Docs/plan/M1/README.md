# M1 Plan — Plains vertical slice

**Audience:** the AI agent executing the work, plus the designer who verifies each checkpoint.
**Source of truth for behaviour:** `Docs/Immortal-Ascent-GDD-v2.md` (sections cited per task).
**Scope:** everything below is M1. If a task tries to do something not listed here, it is out of scope.

---

## 1. What M1 is

One realm, finished end to end: start a run, clear 2 plains floors of 6-8 islands each, find the
stairs, descend, fight a simplified Tusk Sovereign, win or die, restart. Placeholder art is fine.

**M1 is done when** a full plains run is playable in one sitting, every task's tests are green, and
macOS + Windows builds exist.

---

## 2. Scope: in and out

### In scope
| # | System |
| --- | --- |
| 1 | Level flow: 6-8 islands per floor, multiple floors, exit stairs, boss gate, death/victory restart |
| 2 | Turn state machine and action economy: free movement vs turn-based, blocked moves free, aim mode free, reliable input lock |
| 3 | Enemy awareness (room vs radius), roaming, line-of-sight fix |
| 4 | Minimal status system: **Root only** (the spider web), built to extend |
| 5 | Materials and loot rework: ore veins, type-specific drops, no equipment from chests/enemies, arrows 1 ore = 10 |
| 6 | Portable crafting + recipe discovery |
| 7 | Minimap + new HUD elements |
| 8 | Plains content: Pig Yao, Spider Yao, Jing Yao, 2 floors |
| 9 | Tusk Sovereign (single-tile simplified) — **last task in M1** |
| 10 | Build script + macOS/Windows target |

### Out of scope (do not build in M1)
- The other four statuses (Fire, Poison, Enfeeblement, Explosive), resistances, status stacking beyond
  refresh, damage-over-time ticks. Only Root exists in M1.
- Rune changes: on-hit/utility effect types, slot scaling by tier, rune shards, blueprint items and the
  journal. Runes stay exactly as shipped (3 global slots, Sharp/Sturdy only).
- Save/resume and permanent unlock persistence (M3).
- Localization string tables (M2/M3).
- Multi-tile bosses (M2). M1's boss is single-tile.
- Weapon stat requirements, accessory slots, Desert and Tundra content, shops, NPCs, minimap extras
  beyond the rules below.

---

## 3. Working agreement

### 3.1 Assembly + test architecture (created by T00)
- `Assets/Scripts/RogueAsianFantasy.Runtime.asmdef` — all runtime game code.
- `Assets/Scripts/Editor/RogueAsianFantasy.Editor.asmdef` — Editor platform only (PlayDriver).
- `Assets/Scripts/Level Generator/Editor/RogueAsianFantasy.LevelGen.Editor.asmdef` — Editor only.
- `Assets/Tests/EditMode/` — pure-logic tests (asmdef, Editor platform, references Runtime).
- `Assets/Tests/PlayMode/` — behaviour tests (asmdef, references Runtime).
- `Assets/Tilesets/NewCustomRuleTile.cs` stays in Assembly-CSharp; that is fine, nothing in
  `Assets/Scripts` references it.
- Runtime asmdef references at least `Unity.TextMeshPro` and `UnityEngine.UI` (confirm by compiling).

### 3.2 Tests are part of every task
- A task is not done until its tests pass. Tests ship in the same commit as the implementation.
- **EditMode tests** cover pure rules: generation, reachability, statistics, loot rolls, turn ordering,
  LOS, status timing, minimap reveal.
- **PlayMode tests** cover behaviour: grid movement, blocked moves, enemy turn pipeline, chest
  interaction, awareness transitions in a live scene.
- Before changing behaviour that already ships, add a **characterization test** that pins the current
  behaviour, then change it and update the test deliberately.
- If a rule is trapped inside a MonoBehaviour and cannot be tested, **extract a pure function** and test
  that. The extraction is part of the task, not a follow-up.

### 3.3 Editor and scene wiring
- Never hand-edit `*.unity`, `*.prefab`, `*.asset`, `*.meta`, `*.controller`, `*.anim`.
- When a task needs scene/prefab wiring, ship the C# plus a `[MenuItem]` script that performs the wiring
  through `SerializedObject` (pattern: `Assets/Scripts/Item/Editor/ChestSetup.cs`), and list it in the
  task's checkpoint for the designer to run once.
- Never rename or retype an existing serialized field without saying so in the checkpoint.

### 3.4 Git
- One commit per task, after the designer's checkpoint passes. Stay on branch `Testing`.
- Stage files **by name**. Never `git add -A` (untracked `Assets/Plugins/` and
  `Packages/com.nuskey.unity-agent-client/` would be committed into the game repo).
- Commit message: `M1 Txx: <system> — <what changed>`.

### 3.5 Editor access
- The agent may read the Unity console, run `[MenuItem]` diagnostics, and enter/leave Play mode through
  the bridge. Leave the editor idle and not in Play mode when a checkpoint ends.
- Editor must be idle before running anything: `curl -s http://localhost:57123/editor_state`.
  `{"busy":true}` means compiling/reloading/modal — wait, do not force it.
- `Library/PlayerDataCache` shows macOS and WebGL builds exist from earlier work; that does not mean the
  current target is correct.

### 3.6 Verification ladder (every task, in order)
1. Editor compiles with no errors.
2. The task's EditMode tests are green.
3. Its PlayMode tests are green (if the task has any).
4. The relevant `[MenuItem]` diagnostic still passes (see the task file).
5. Designer plays the affected slice and signs off the checkpoint.

---

## 4. Checkpoint protocol

Each task ends with a **checkpoint**: the agent stops, posts a short report, and waits.

Report format:
```
Task: Txx <name>
Status: checkpoint
Changed: <files>
Tests: EditMode x/y green, PlayMode x/y green  (or "no PlayMode tests")
Diagnostic: <MenuItem name> — <result>
Editor checklist for you: <steps, or "none">
Commit: <proposed message>
Risks/notes: <anything uncertain>
```
The designer replies `pass` (agent commits and continues) or lists corrections (agent fixes, re-runs
verification, checkpoints again). Do not start the next task before `pass`.

---

## 5. Task order and dependencies

```
T00 tests+asmdef+build script   (no gameplay change; blocks everything)
 └─ T01 map generation: floors, island counts, stairs, boss room
     ├─ T02 RunState + floor progression + death/victory
     │    ├─ T08 minimap + HUD
     │    └─ T10 Tusk Sovereign (single-tile)   [LAST task in M1]
     ├─ T03 turn state machine + action economy
     │    └─ T04 awareness + roaming + line of sight
     │         └─ T05 minimal status system (Root only)
     │              └─ T09 plains content (3 enemies, 2 floors)
     └─ T06 materials + loot rework
          └─ T07 portable crafting + recipe discovery
               └─ T09 plains content
T11 integration + full-run playtest + desktop builds   (after T10 and T09)
```

| Task | File | Status |
| --- | --- | --- |
| T00 | [T00-tests-asmdef-build.md](T00-tests-asmdef-build.md) | not started |
| T01 | [T01-map-generation.md](T01-map-generation.md) | not started |
| T02 | [T02-run-state-floors.md](T02-run-state-floors.md) | not started |
| T03 | [T03-turn-state-machine.md](T03-turn-state-machine.md) | not started |
| T04 | [T04-awareness-roaming-los.md](T04-awareness-roaming-los.md) | not started |
| T05 | [T05-status-root.md](T05-status-root.md) | not started |
| T06 | [T06-materials-loot.md](T06-materials-loot.md) | not started |
| T07 | [T07-portable-crafting.md](T07-portable-crafting.md) | not started |
| T08 | [T08-minimap-hud.md](T08-minimap-hud.md) | not started |
| T09 | [T09-plains-content.md](T09-plains-content.md) | not started |
| T10 | [T10-boss-tusk-sovereign.md](T10-boss-tusk-sovereign.md) | not started |
| T11 | [T11-integration-builds.md](T11-integration-builds.md) | not started |

---

## 6. Shared verification commands

```bash
# is the editor idle? (busy:true means compiling/reloading/modal — wait)
curl -s http://localhost:57123/editor_state

# available bridge tools
curl -s http://localhost:57123/tools

# run a static [MenuItem] method and read its Debug.Log output from the console
curl -s -X POST http://localhost:57123/execute_method \
  -H 'Content-Type: application/json' \
  -d '{"type":"TestRunnerMenu","method":"RunEditMode"}'
```

The agent also has the MCP tool `read_unity_console` for reading results.

Existing diagnostics that must keep passing (do not break these):
`MapgenDiagnostics.ChestWalkability`, `OpenRealChest`, `ConsumableBuff`, `TurnPipeline`,
`RoomBuilderStress`, and `PlayDriver.Status` / `StepTowardNearestEnemy` / `OpenAdjacentChest`.

---

## 7. Risks carried into M1

| Risk | Handling |
| --- | --- |
| The asmdef split changes every compile boundary at once | T00 is standalone, no gameplay change, verified before anything else |
| Turn-state rework touches Controls, TurnManager, EnemyBehaviour together | T03 is its own task with PlayMode tests and a designer playtest checkpoint |
| Switching the active build target triggers a long reimport | T00 adds the script; the switch happens once, in T11, and is expected to be slow |
| Multi-tile bosses are deliberately deferred | M1 boss is single-tile; the multi-tile upgrade is an M2 task and a known code gap (GDD B21) |
| Moving combat out of `EntityManager.MoveEntity` could change bump-attack feel | T03 keeps bump-to-attack behaviour identical and pins it with PlayMode tests |
