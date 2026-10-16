# T08 — Minimap and HUD additions

**Depends on:** T02 (floor/realm for the indicator), T03 (turn state, weapon state), T05 (root status),
T06 (ammo count).
**GDD:** 14.1 (HUD elements), 14.5 (minimap rules), 3.3 (stairs discovery).

## Goal

The player can see where they have been, where the stairs are once found, and the state of the run:
ammo, weapon, floor, whose turn it is, and their active status.

## Files

| Path | Change |
| --- | --- |
| `Assets/Scripts/UI/MinimapRules.cs` | new pure: visited set, marker rules, per-floor reset |
| `Assets/Scripts/UI/Minimap.cs` | new: draws the visited set cheaply |
| `Assets/Scripts/Managers/UI Manager.cs` | HUD elements and their data sources |
| `Assets/Scripts/Player/Controls.cs` | quick-use key for the selected pill |
| `Assets/Scripts/UI/QuickUse.cs` | new: selected pill slot, uses one item (costs a turn) |
| `Assets/Scripts/Editor/HudWiring.cs` | new: `[MenuItem]` that wires the new HUD references |
| `Assets/Tests/EditMode/MinimapRulesTests.cs` | new pure tests |
| `Assets/Tests/PlayMode/HudTests.cs` | new behaviour tests |

## Implementation notes

1. **Minimap rules as pure logic.** `MinimapRules` tracks a visited set per floor and answers which markers
   are visible:
   - Terrain becomes visible as the player explores it and stays visible for the rest of the floor.
   - The stairs marker appears **when the player discovers the stairs**, not at floor start.
   - Chests and ore veins appear once their island has been entered.
   - **Enemies are never shown, in any realm.**
   - The visited set resets when the floor changes.
2. **Rendering.** Keep it cheap: one small texture (or a single Tilemap) updated only on newly revealed
   cells, never per-frame full-map work. Do not add a GameObject per minimap pixel.
3. **HUD elements to add** (all data already exists after the earlier tasks):
   - ammo count (turns red at 0),
   - equipped weapon indicator (melee / ranged / aim state),
   - turn indicator (whose turn, and that input is locked),
   - realm + floor indicator (e.g. "Plains — Floor 2"),
   - quick-use pill slot,
   - root status icon with rounds remaining.
   HP and XP bars already exist — keep them. **Status icons for the other four statuses are M2**; only Root
   exists in M1.
4. **Quick use.** One slot holding the selected pill, activated by a key; using it costs a turn and goes
   through the same `ItemUsage` path as the inventory button, so there is one code path for using pills.
   Confirm the exact key with the designer at the checkpoint (proposal: `Q`).
5. **Wiring.** New HUD elements need scene references. Ship `HudWiring` as a `[MenuItem]` that finds and
   assigns the components via `SerializedObject`, and list it in the checkpoint for the designer to run
   once. Never edit the scene YAML.
6. **Do not break existing UI.** Inventory, crafting, level-up and inscription panels must keep working;
   `InscriptionPanel` is a prefab instance in the dungeon scene and locks the player while open.

## Test list (EditMode, pure)

- A cell becomes visited when the player enters it and stays visited.
- Visiting another cell does not reveal unrelated cells.
- The stairs marker is absent before discovery and present after.
- Chest and vein markers appear only once their island is entered.
- Enemies never appear, whatever their state.
- `Reset` clears the visited set and all markers for the new floor.

## Test list (PlayMode)

- Walking across a floor grows the minimap data (assert on `MinimapRules`, not pixels).
- The ammo counter follows the arrow count from T06 and firing decrements it.
- The turn indicator is shown during the enemy phase and cleared after.
- Quick-use consumes one pill, applies its buff, and costs exactly one turn.

## Verification

1. EditMode + PlayMode tests green.
2. `execute_method` → `PlayDriver.Status` and `Tools/Play/Audit Bridge Tiles` (movement unchanged).
3. `execute_method` → `MapgenDiagnostics.TurnPipeline` (quick-use goes through the turn pipeline).
4. Designer checkpoint: play a floor and confirm the minimap reveals sensibly, the stairs appear only when
   found, enemies are absent from it, and the new HUD elements are legible at 1080p.

## Checkpoint

Report the minimap reveal rules, the quick-use key proposal, and the exact `HudWiring` menu item the
designer must run. Ask for a legibility check on the HUD (this is the one task where "does it read well"
matters more than the tests).

## Out of scope

Status icons beyond Root (M2). Tooltips, damage log, bestiary. Localization of the new strings.
