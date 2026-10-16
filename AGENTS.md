# AGENTS.md — Rogue Asian Fantasy ("Immortal Ascent")

Instructions for AI agents working in this repository. Read this before touching code.
Deeper detail lives in `Tools/dsh-unity-preset/skills/unity-rogue-asian-fantasy/SKILL.md`
(exhaustive runtime contracts and verification notes) — this file is the entry point and the
rule set; that file is the reference when you need the full call-site detail.

## 1. What this project is

A turn-based, grid-based roguelike in Unity **2022.3.62f2** (URP 2D, Tilemap, TextMeshPro,
legacy `Input`, no namespaces). One run = three realms (Plains → Desert →
Tundra), 2-3 floors each, 6-8 islands per floor, one action per turn in combat, permadeath,
30-45 minutes.

Code is split into asmdefs (added by T00): `RogueAsianFantasy.Runtime` (`Assets/Scripts`),
`RogueAsianFantasy.Editor`, `RogueAsianFantasy.LevelGen.Editor`, `RogueAsianFantasy.Tests.EditMode`,
`RogueAsianFantasy.Tests.PlayMode`. `Assembly-CSharp` now holds only `Assets/Tilesets/NewCustomRuleTile.cs`.

| Thing | Where |
| --- | --- |
| Design spec (source of truth for behaviour) | `Docs/Immortal-Ascent-GDD-v2.md` |
| The same spec as Word | `Docs/Immortal-Ascent-GDD-v2.docx` (generated — edit the `.md`/generator, never the `.docx`) |
| GDD generator | `Docs/gdd_build/` — `build.py` writes both `.docx` and `.md` |
| Current work plan and checkpoint protocol | `Docs/plan/M1/README.md` |
| Item system detail | `Docs/ItemSystem.md` |
| Refactor backlog (partly executed) | `Docs/Refactoring-Plan.md` |
| Runtime code | `Assets/Scripts/` (41 `.cs`) |
| Item data | `Assets/Resources/ItemDefinitions/{Consumables,Equipments,Inscriptions,Materials}/` |

`Docs/InscriptionSystem.md` is **superseded** (its design shipped as `EquipmentInstance` +
`InscriptionService`) and is currently deleted in the working tree. Do not resurrect it.

Scenes: `Assets/Scenes/ui.unity` is build index **0** (the death screen loads it),
`Assets/Scenes/Branching dungeon.unity` is index **1**. `gameHUD.unity` is not in the build.

## 2. Current state — do not assume features exist

Verified against the code, not the docs:

- **Run structure exists; its content does not.** T02 added realm/floor progression (`RunState` +
  pure `RunSequence` + `RealmDefinition`), a per-floor seed and stairs that rebuild the floor in place.
  A run completes when the last realm's boss floor is left, but the boss entity (T10), the victory UI
  and saving (M3) do not exist yet.
- **Statuses exist, but only Root** (T05). `StatusKind` carries all five GDD kinds so serialized values
  never renumber, while `StatusSet.IsImplemented` admits Root alone and refuses the rest out loud.
  `StatusController` lives on the player (auto-attached by `PlayerStats.Awake` if the prefab lacks one);
  `TurnManager` ticks it once per completed round.
- **Materials exist now** (T06). Ore veins (terrain `7`, solid, mined with `E` from an adjacent cell for
  1-3 ore, then opened with the chest two-write path) are the primary source, and enemy types drop
  materials from `Resources/DropTables/*` via `EnemyDrops`. Arrows are a count on `PlayerStats`, not item
  stacks. Ground drops are **not** built: overflow is reported and lost (chests keep their leftovers).
- **Line of sight is real** (T04). `LineOfSight.HasClearShot` walks the Grid: walls, water and out of
  bounds block, while webs, bushes and chests do not. `utilityFunction.ClearLine` is dead code (marked as
  such, no callers) - it read a Tilemap nothing painted.
- **Awareness is realm-driven** (T04). `RealmDefinition.aggroMode` selects room-wide (Plains) or
  detection-radius-plus-line-of-sight (desert, tundra, M2 content); `RunState.EnemyAggroMode` is what the
  enemies read. Unaware enemies roam one step per round inside their own room.
- **Test assemblies exist** (T00): `Assets/Tests/EditMode` (pure rules, runnable through the bridge) and
  `Assets/Tests/PlayMode` (batchmode or the Test Runner window only - never the bridge).
- **Fog is code-only and unwired.** `FogManager` has serialized Tilemap/TileBase fields, but no scene
  object references it and nothing calls its `Initialize`/`ClearFog`. Both of its calls in `Controls` are
  null-guarded; the feature does nothing until someone wires it.

`Docs/Immortal-Ascent-GDD-v2.md` Appendix B is the full gap list. Appendix A is the open
decisions list — check it before treating a number as settled.

## 3. Workflow: one plan task at a time

1. **Check the tree before trusting it.** `git log --oneline -3` and compile. A collaborator merges into
   this branch from the remote, and one such merge left a duplicated movement block in `Controls.cs` that
   did not compile, plus an unguarded call into an unwired system that threw on every spawn. HEAD is not
   guaranteed green.
2. Read `Docs/plan/M1/README.md` (working agreement, task order, dependencies).
3. Open **exactly one** task file from `Docs/plan/M1/`.
4. Implement it, write its tests, run the verification ladder in that file.
5. Stop at the task's **checkpoint** and post the report it asks for. Do not start the next
   task, and do not commit, until the designer replies `pass`.
6. Only then commit and continue.

Values in the GDD marked **proposed** are starting points for playtesting, not balance. Never
"fix" a number to match a doc without saying so.

## 4. Hard rules

### Never hand-edit Unity-authored files
| Path | Why |
| --- | --- |
| `*.unity`, `*.prefab`, `*.asset`, `*.controller`, `*.anim` | editor-authored YAML; hand edits corrupt GUID/fileID references |
| `*.meta` | Unity owns GUIDs; never create, edit or delete one by hand |
| `Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/` | generated scratch, gitignored |
| `*.csproj`, `*.sln` | regenerated by Unity/IDE |
| `Assets/TextMesh Pro/` | vendored; `ui.unity` depends on `Examples & Extras/.../Electronic Highway Sign SDF.asset`, so that folder cannot be deleted wholesale |
| `Packages/com.nuskey.unity-agent-client/`, `Assets/Plugins/` | local agent tooling, **untracked and not gitignored** |

When a task needs scene/prefab/inspector wiring: write the complete C# side, then add an Editor
`[MenuItem]` script that performs the wiring through `SerializedObject`, and hand the designer a
one-line instruction to run it. Never wire it by editing YAML.

There is **no surviving wiring script to copy** — the refactor deleted `ChestSetup.cs` and
`InscriptionPanelSetup.cs`, and `Assets/Scripts/Item/Editor/` is now an empty folder. For the
menu-item style, copy the two that survive: `Assets/Scripts/Level Generator/Editor/EditorDungeonGenerator.cs`
(creates content) and `Assets/Scripts/Level Generator/Editor/MapgenDiagnostics.cs` (asserts and logs
`[DIAG] PASS/FAIL`). Do not copy patterns out of `Packages/com.nuskey.unity-agent-client/` — that is
tooling, not game code.

### Never rename without telling the designer
Renaming a serialized **field**, or a **GameObject/transform** that code finds by name, silently
resets data or disables a feature. Both are load-bearing (see contracts below). Renaming a file
or a non-serialized member is fine — the cosmetic typos
(`ProceduralGeneraltionAlgorithm.cs`, `BrachingRooms`, `levelUpUI.cs`, `UI Manager.cs`) are
deliberately left alone.

### Scope
Make the smallest change that satisfies the task. Do not refactor toward a preferred style, add
packages, or reorganise folders unless the task says so. `Packages/manifest.json` changes force a
full reimport for the designer — ask first.

### Honesty
Never report that something works because it "looks correct". Never conclude a serialized field
is empty from a filtered `grep` of a `.unity`/`.prefab` file — nested fields are indented deeper
and list entries start with `-`, so a `^  \w` filter hides every value. Read the block verbatim.
Say plainly what you did not verify.

## 5. Runtime contracts you must not break

Read the call sites before changing any of these.

**1. The grid is terrain only; entities are separate.** `Grid` (`Assets/Scripts/GameGrid/Grid.cs`)
holds terrain codes; `RoomFirstDungeonGenerator.EnsureGrid()` is the single owner and publishes
`Grid.Current`. Everything reads `Grid.Current`. Terrain codes: `0` floor/corridor (walkable),
`1` wall + water + **out of bounds** (blocked), `6` spider web (walkable until entered, T05),
`7` ore vein (blocked until mined out, T06), `99` unopened chest (blocked). `2`, `3`, `5` are legacy
values nobody writes (`5` is still accepted as walkable). Route movement checks through
`Grid.IsWalkableTerrain(int)` and cell lookups through `Grid.CellFromWorld(Vector3)` — never compare codes
inline.

**Entity occupancy is not in `Grid`.** `EntityManager` owns a one-entity-per-cell dictionary.
Register, move and unregister every moving entity through it or collision/combat silently ignores
it. `MoveEntity` only moves, and refuses an occupied destination: the caller decides what stepping
into an entity means, and since T03 bump-attack lives in `Controls.TryMove`.

**Chests change terrain at runtime, and it takes two writes:**
```csharp
Grid.Current.SetValueAtLocation(cell.x, cell.y, 0);   // movement authority
IslandManager.MarkWalkable(cell);                     // the island map's snapshot
```
Only the first leaves the cell `Unassigned`, so `CanMoveTo` keeps refusing the step and
`CountLivingEnemies` misses an enemy standing there (a room can read as cleared while one is
alive). Any new destructible cell (webs, ore veins) must copy this path.

**2. Turn order flows through a static event, and only a real action raises it.** The player action is
validated and resolved first, then `Controls.ConsumeTurn` - the single raiser of `onMoveEvent` - checks
`TurnRules.ConsumesTurn` and fires; `TurnManager` receives it, alerts enemies, runs the single-flight
`TakeTurns` coroutine, and clears `Controls.takingTurn` in `finally`. `takingTurn` is the input lock;
`TurnManager.IsEnemyPhaseRunning` is the phase guard. A refused action (wall bump, gated exit, empty
quiver, aim-mode entry) is free: nothing moves, nothing fires. `TurnManager` subscribes in `Start` and
unsubscribes in `OnDestroy` — keep static event subscriptions symmetric or reloads leak handlers.

**3. `IslandManager` is a static snapshot, not a singleton.** `Rebuild(roomsSpace)` snapshots
walkability once per generation; anything walkable later needs `MarkWalkable`. `CanMoveTo` gates
room exit while enemies live on the island; `IsPartOfRoom` keeps enemies in their own room.
`Unassigned == -1`, `Passage == 0`, room ids from 1.

**4. Managers are scene-local singletons.** `TurnManager`, `EntityManager`, `PlayerStats`,
`Inventory`, `Equipments`, `LootGen`, `UIManager`, `CraftingManager`, `InscriptionPanel` expose
`public static X Instance;` assigned in `Awake` with a duplicate-destroy guard. Match that
pattern; prefer these and `Grid.Current`/`IslandManager` over `FindObjectOfType` in gameplay code.

**5. Items.** `ItemDefinition` is an immutable shared ScriptableObject and the single source of
truth (identity, stacking, kind, weapon/armour numbers, consumable effect, inscription effect, and
an optional `recipe`). An empty `recipe` means "not craftable"; there is no `Recipe` asset type.
Definitions must stay under `Assets/Resources/ItemDefinitions/` — `CraftingManager` finds them with
`Resources.LoadAll<ItemDefinition>("ItemDefinitions")`. Per-item state (inscriptions) lives on
`EquipmentInstance` carried by `ItemStack.instance`; equipment never stacks. **Never put per-instance
state on `ItemDefinition`.**

**6. Scene wiring is name-based.** These lookups are null-guarded, so renames fail *quietly*:

| Looked up | By |
| --- | --- |
| `UIManager` | `Enemy.OnEnable`, `Inventory.Start` |
| `UIManager/Inventory/itemholder` | `Inventory.Start` |
| `CurrentPos` | `Controls.Start` |
| `HpBar`, `ExpBar` | `PlayerStats.OnEnable` |
| `Level Up UI` | `UIManager.toggleLevelUpMenu` |
| `str`, `dex`, `health` | `levelUpUI.OnEnable` |
| `slot` → `Item` | `ItemBox.EnsureImage` |
| `Player` tag | `EnemyBehavior.Start`, `UIManager.CloseMenu` |

**7. Regenerating a floor requires `Grid.Clear()`.** The `Grid` outlives a generation: `tilePlacer.clear()`
clears the tilemaps but not the terrain, so the previous floor's room rings (`1`) and chest codes (`99`)
stay underneath the new floor and cut through it - isolated cells, chests walled in. Clear before placing.

**8. The player is reused across floors, not respawned.** `RoomFirstDungeonGenerator` repositions and
re-registers the existing `PlayerStats`. Instantiating a fresh one while the old is still alive
(`Destroy` is deferred) makes the player-side singleton guards - `PlayerStats`, `Equipments`, `Inventory`,
which all `Destroy(gameObject)` - delete the whole new player; and it would reset health, level and
inventory, which the GDD carries between floors.

**9. The range overlay has its own Grid, so its cells are player-relative.** `RangeAttackTilemap.tilemap`
is a child of the player: `tilemap.WorldToCell` returns overlay cells, while terrain and the entity
registry are addressed in `Grid.Current` cells (and `Grid.CellFromWorld` gives those). Convert before any
grid, sight or registry lookup - mixing the two silently paints and queries the wrong cells.

**10. Root costs the round; terrain and island refusals do not.** A rooted player who tries to step spends
the turn - and so the round, which is what lets Root expire. M1 has no wait key, so a free refusal would
soft-lock the run on an island with nothing to attack. Attacking stays legal while rooted, which is why
`Controls.TryMove` resolves the bump-attack *before* the movement gate.

## 6. Coding conventions

Match the file you are editing; the codebase style is inconsistent and local consistency wins.

- No namespaces; `public class Foo : MonoBehaviour` at file scope. 4 spaces, no tabs.
- Designer-tunable fields are `public` or `[SerializeField] private`; runtime state is private.
- Coroutines (`IEnumerator` + `StartCoroutine`), not `async`/`await`.
- Spatial queries use `Vector2Int` + `Grid`/`EntityManager`, not raycasts.
- `Debug.Log` is used liberally, including hot paths. Keep new logging sparse; use
  `Debug.LogWarning`/`LogError` for real faults.
- XML `<summary>` on new **public** APIs; don't retrofit older files.
- Prefer extracting a pure, testable function over burying rules in a MonoBehaviour — the plan
  requires unit tests for rules.

## 7. Verification

T00 added the test assemblies - EditMode (`Assets/Tests/EditMode`) and PlayMode (`Assets/Tests/PlayMode`) -
driven by `TestRunnerMenu`. Verify with the tools below and report which step you actually ran.

**The designer usually has the editor open, and batchmode cannot share the project.** A batchmode
run then aborts with exit code **134** (SIGABRT) and
"It looks like another Unity instance is running with this project open". Do **not** kill the
editor — that risks unsaved scene work.

**1. Is the editor up, and is it idle?**
```bash
curl -s http://localhost:57123/editor_state     # {"busy":false,"isPlaying":...} is what you want
curl -s http://localhost:57123/tools
```
`busy:true` means compiling, reloading, or a modal dialog — wait, do not force it.

**2. Compile check while the editor is open.** The open editor compiles on import. Confirm the
recompile landed by comparing timestamps against the assembly for the code you touched:
`RogueAsianFantasy.Runtime.dll` for gameplay code, `RogueAsianFantasy.Editor.dll` or
`RogueAsianFantasy.LevelGen.Editor.dll` for Editor scripts, `RogueAsianFantasy.Tests.*` for tests.
```bash
find Assets -name '*.cs' -newer Library/ScriptAssemblies/RogueAsianFantasy.Runtime.dll
```
Then read the bridge console for `error CS` - **not** `Editor.log`, which is buffered and can be hours
stale (see the traps below).
Weaker fallback (catches syntax/type errors only against the last import, and needs stale
`<Compile Include>` entries pruned if you added/moved/deleted a file):
```bash
dotnet build "Assembly-CSharp.csproj" -nologo -v q 2>&1 | grep -E "error CS" | head -40
```
If the editor is closed, batchmode works — but it **exits 0 even when compilation fails**, so read
the log, not the exit code. After any batchmode run confirm nothing tracked changed:
`git status --porcelain -- Assets ProjectSettings`.

**3. Run diagnostics / tests through the bridge** (static methods on the editor main thread):
```bash
curl -s -X POST http://localhost:57123/execute_method \
  -H 'Content-Type: application/json' \
  -d '{"type":"MapgenDiagnostics","method":"ChestWalkability"}'
```
Then read the console. Existing harnesses that must keep passing:
`MapgenDiagnostics.ChestWalkability`, `OpenRealChest`, `ConsumableBuff`,
`TurnPipeline`, `RoomBuilderStress`; and `PlayDriver.Status`, `StepTowardNearestEnemy`,
`OpenAdjacentChest`, `DumpEntityPositions`, `Audit Bridge Tiles`, `Audit Enemy Rooms`. Diagnostic
methods log `[DIAG] PASS/FAIL`. After T00 lands, `TestRunnerMenu.RunEditMode` / `RunPlayMode` /
`RunAll` are the test entry points.

Traps, all learned the hard way:

- The console buffer is cleared by every domain reload, and a reload drops the in-flight response. Make
  the call, then read the console.
- **For test runs, read `Temp/testrun-<Mode>.json`, not the console.** `TestRunnerMenu` writes the
  summary there. Entering and leaving Play mode performs a domain reload, which wipes the console, so a
  PlayMode run's `[TESTRUN]` start and finish lines are erased before you can read them - the console
  looks like the run never happened. The file survives; `Temp/` is gitignored.
- **PlayMode tests only run in batchmode or the Test Runner window - never through the bridge.** Three
  bridge attempts hung the editor: twice with a scene-loading test, and once with a **scene-free** test
  that only builds a Grid and an island map. Entering Play mode performs a domain reload, which aborts the
  agent client's request thread (`Thread was being aborted`), and the runner never reports afterwards. The
  scene was not the cause: the same scene-loading test **passes** in batchmode (`-runTests
  -testPlatform PlayMode`, exit 0, XML report), so the hang is specific to bridge-driven Play mode.
- `Editor.log` is buffered and can be hours stale (it stops growing mid-session), so do not rely on it as
  the record of what just happened. The bridge console and `Temp/` files are the observable channels.
- These methods call `IslandManager.Rebuild`, which replaces the editor's island map until the next
  generation.
- **The designer may leave the editor open when they hand over.** EditMode suites still run through the
  bridge (`TestRunnerMenu.RunEditMode`, then read `Temp/testrun-EditMode.json`), and play testing works
  through `EnterPlaymode` + `PlayDriver`. The **PlayMode** suite cannot: batchmode aborts with 134 while
  another instance holds the project. Confirm `activeSceneDirty:false` from `/editor_state`, then quit
  the editor gracefully (`osascript -e 'tell application "Unity" to quit'`) instead of killing it.

**4. Run play tests through `EnterPlaymode` + `PlayDriver`. This path works, unlike bridge-driven
`TestRunnerApi` PlayMode runs (see the traps above).** The game reads legacy `Input` inside
`Controls.Update`, which a script cannot synthesise, so `PlayDriver` drives the same systems in the
same order. Verified sequence, every call returning in milliseconds:

```bash
# 1. enter play mode, then poll /editor_state until isPlaying:true and busy:false (a few seconds)
curl -s -X POST http://localhost:57123/execute_method -H 'Content-Type: application/json' \
  -d '{"type":"UnityEditor.EditorApplication","method":"EnterPlaymode","timeoutMs":20000}'
# 2. expect [IslandManager] N room island(s) and [GEN] reachability OK, and no errors
curl -s -X POST http://localhost:57123/read_unity_console -H 'Content-Type: application/json' -d '{"maxCount":20}'
# 3. drive the real game: Status reports cell/island/hp/enemies, Step* moves the player
curl -s -X POST http://localhost:57123/execute_method -H 'Content-Type: application/json' \
  -d '{"type":"PlayDriver","method":"Status"}'
curl -s -X POST http://localhost:57123/execute_method -H 'Content-Type: application/json' \
  -d '{"type":"PlayDriver","method":"StepTowardNearestEnemy"}'
# 4. then the play-mode diagnostics: MapgenDiagnostics.OpenRealChest, ConsumableBuff, TurnPipeline
# 5. leave play mode
curl -s -X POST http://localhost:57123/execute_method -H 'Content-Type: application/json' \
  -d '{"type":"UnityEditor.EditorApplication","method":"ExitPlaymode","timeoutMs":20000}'
```

One step per call; poll `Status` for `takingTurn` instead of sleeping. Entering play mode flips
`runInBackground` to 1 in `ProjectSettings/ProjectSettings.asset` - revert that before committing.

**5. After editing any `.cs`, restart Play mode — never assume it survived.** Recompiling tears
down the domain under the running game, leaving `isPlaying:true` with the scene objects gone
(`PlayerStats.Instance`/`EntityManager.Instance` null, bridge timeouts). Reliable sequence: write
code → `open -a "/Applications/Unity/Hub/Editor/2022.3.62f2/Unity.app"` (macOS throttles a
background editor; AppleScript `activate` is not enough) → poll until no `.cs` is newer than
`Library/ScriptAssemblies/RogueAsianFantasy.Runtime.dll` (or `...Editor.dll` for Editor scripts) → poll
`/editor_state` until `busy:false` and
`isPlaying:false` → `UnityEditor.EditorApplication.EnterPlaymode` → confirm generation in the
console (`[GEN]`, `[IslandManager]`) before trusting anything.

**6. What you cannot verify.** Input, physics, visual layout, balance and feel. Say so and hand the
designer a short manual checklist instead of implying you played it. Check `activeSceneDirty` before
anything destructive (opening a scene, generating into the current one) and ask first.

## 8. Git

- Branch: **`Testing`**. One commit per plan task, **only after the designer replies `pass`** at a
  checkpoint; otherwise leave changes in the working tree.
- **Never `git add -A`** — `Assets/Plugins/` and `Packages/com.nuskey.unity-agent-client/` are
  untracked tooling that must not enter the game repo. Stage files by name.
- Commit message: `M1 Txx: <system> — <what changed>`.
- Do not commit generated `.csproj`/`.sln`, `Library/`, `Temp/`, `Logs/` or `.DS_Store` (all
  repo-ignored). If Unity writes Burst debug output (`*_BurstDebugInformation_DoNotShip/`), do not
  commit that either — it is **not** covered by the repo's `.gitignore`, so it would otherwise show
  up as untracked.
- `Docs/Immortal-Ascent-GDD-v2.docx`, `Docs/Immortal-Ascent-GDD-v2.md`, `Docs/gdd_build/` and
  `Docs/plan/` are currently **untracked** (created after the last commit). Commit them with the
  plan when the designer approves, and regenerate the GDD with
  `python3 Docs/gdd_build/build.py` (writes both files) instead of editing either by hand.
