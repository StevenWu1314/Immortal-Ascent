# T00 — Test + assembly foundation, build script

**Blocks:** every other task. No gameplay behaviour changes in this task.
**GDD:** section 16 (technical design), Appendix B (the project has no test assembly today).

## Goal

Make the codebase unit-testable and give the project a reproducible desktop build entry point, without
changing any game behaviour.

## Why this is first

Unity's predefined `Assembly-CSharp` cannot be referenced by a test assembly. Until game code lives in
its own asmdef, no real unit test can compile. The project already has `com.unity.test-framework@1.1.33`
and NUnit installed; only the asmdefs and test folders are missing.

## Files to add

| Path | Purpose |
| --- | --- |
| `Assets/Scripts/RogueAsianFantasy.Runtime.asmdef` | all runtime game code |
| `Assets/Scripts/Editor/RogueAsianFantasy.Editor.asmdef` | Editor-only tools (PlayDriver) |
| `Assets/Scripts/Level Generator/Editor/RogueAsianFantasy.LevelGen.Editor.asmdef` | Editor-only generation tools |
| `Assets/Tests/EditMode/RogueAsianFantasy.Tests.EditMode.asmdef` + `GridRulesTests.cs` | pure-logic tests |
| `Assets/Tests/PlayMode/RogueAsianFantasy.Tests.PlayMode.asmdef` + `PlayModeSmokeTests.cs` | behaviour tests |
| `Assets/Scripts/Editor/TestRunnerMenu.cs` | `[MenuItem]` entry points the bridge can call |
| `Assets/Scripts/Editor/BuildMenu.cs` | desktop build + target switch |

Verified facts to rely on: the only `.cs` outside `Assets/Scripts` is `Assets/Tilesets/NewCustomRuleTile.cs`
(no script in `Assets/Scripts` references it, so leaving it in `Assembly-CSharp` is safe); no `UnityEditor`
usage exists outside the two Editor folders today.

## Implementation notes

1. Runtime asmdef covers `Assets/Scripts` and everything below it. The two Editor asmdefs override their
   folders with `includePlatforms: ["Editor"]`. The runtime asmdef must reference `Unity.TextMeshPro` and
   `UnityEngine.UI`; if compilation reports other missing package assemblies, add them explicitly.
2. Test asmdefs follow the standard Unity test-assembly shape: `overrideReferences: true`,
   `precompiledReferences: ["nunit.framework.dll"]`, `defineConstraints: ["UNITY_INCLUDE_TESTS"]`,
   `autoReferenced: false`, and references to `UnityEngine.TestRunner` + `UnityEditor.TestRunner`.
   EditMode asmdef also sets `includePlatforms: ["Editor"]`.
3. `TestRunnerMenu` exposes **static public** methods callable by type+method through the bridge:
   - `RunEditMode()` and `RunPlayMode()` and `RunAll()` — each uses `TestRunnerApi` with the proper
     filter, registers a results callback, and logs one summary line, e.g.
     `TESTRUN EditMode complete: passed=7 failed=0 skipped=0`.
   - Log each failure by name so console reading is enough.
4. `BuildMenu` exposes:
   - `SwitchToDesktop()` — switches the active target to `StandaloneOSX` or `StandaloneWindows64`
     (ask which platform the designer is on; macOS here), since Unity Hub currently records the project's
     target as WebGL.
   - `BuildMac()` / `BuildWindows()` — `BuildPipeline.BuildPlayer` into `Builds/<platform>/`, logging the
     result path and `BuildResult`.
   - Do not run these automatically; the designer runs them once and reports back.
5. Tests to write in this task (they must be genuinely useful, not placeholders):
   - EditMode `GridRulesTests`: `Grid.IsWalkableTerrain` returns true for 0 and 5, false for 1 and 99,
     and false for unknown values; out-of-bounds reads behave as blocked.
   - EditMode characterization test `MapgenCharacterizationTests`: the current generator produces a
     connected 5-room map and a reachable player spawn. This pins today's behaviour so T01 can change it
     visibly.
   - PlayMode `PlayModeSmokeTests` holds two tests: a **scene-free** one (build a grid, rebuild the island
     map, assert island ids / passage / blocked cells) and a **scene-loading** one (load `Branching
     dungeon`, assert generation, islands and player registration). The scene test is the one that proves
     the assembly split did not break scene wiring, and it **passes in batchmode**.
   - **PlayMode runs must not be driven through the bridge.** Three bridge attempts hung the editor, the
     last with the scene-free test, so the scene is not the cause: entering Play mode performs a domain
     reload that aborts the agent client's request thread and the runner never reports. Run PlayMode in
     batchmode with the editor closed, or from the Test Runner window.
   - `TestRunnerMenu` writes its summary to `Temp/testrun-<Mode>.json`. **Read that file, not the
     console**, for PlayMode results: entering and leaving Play mode performs a domain reload, and a
     domain reload clears the console buffer, so the runner's start/finish lines are gone before an
     external caller can read them.

## Test list

- `GridRulesTests.IsWalkableTerrain_*` (≥4 cases incl. unknown and out of bounds)
- `MapgenCharacterizationTests.CurrentGenerator_ProducesConnectedFiveRoomMap`
- `PlayModeSmokeTests.RuntimeGridAndIslandRules_RunInPlayMode`

## Verification

1. Editor compiles, console has no errors.
2. `TestRunnerMenu.RunEditMode` via the bridge, then read `Temp/testrun-EditMode.json` → `"failed": 0`.
   Read the file, not the console: a domain reload clears the console buffer.
3. PlayMode tests are **not** driven through the bridge. Three attempts hung the editor, the last one
   with a scene-free test, because entering Play mode aborts the agent client's request thread. Run them
   either with `-runTests -testPlatform PlayMode` in batchmode **with the editor closed**, or from the
   Test Runner window, and report the result. `Temp/testrun-PlayMode.json` is written either way.
4. All pre-existing `[MenuItem]`s still exist: `Tools/Diagnose/*`, `Tools/Play/*`,
   `Tools/Create Branching Dungeon` (EditorDungeonGenerator) — this proves the asmdef split did not
   orphan the editor tooling.
5. `Tools/Play/Status` still runs and logs a sane position.

## Checkpoint

Report the test counts, the menu items that survived, and whether `SwitchToDesktop` was run. Ask the
designer to confirm the Editor windows still open and existing menu items work.

## Out of scope

Any gameplay behaviour change. Adding namespaces. Moving `.cs` files between folders.
