# T07 — Portable crafting and recipe discovery

**Depends on:** T06 (materials actually exist, recipes updated).
**GDD:** 3.6 (crafting access), 12.5 (recipes and unlocking), 12.1 (item kinds).
**Note:** this supersedes the earlier "crafting stations / safe islands" idea — there are no stations in M1.

## Goal

The player crafts from the inventory anywhere out of combat, and recipes appear as they are discovered by
handling new materials.

## Files

| Path | Change |
| --- | --- |
| `Assets/Scripts/Crafting/CraftingRules.cs` | new pure rules: availability, discovery, affordability, atomic craft |
| `Assets/Scripts/Crafting/RecipeDiscovery.cs` | new pure: discovered-recipe set, unlock on first acquisition |
| `Assets/Scripts/Managers/CraftingManager.cs` | list recipes via `CraftingRules`, not by walking all definitions |
| `Assets/Scripts/Item/ItemDefinition.cs` | keep `recipe`; move the validate-then-consume logic behind the pure rules if practical |
| `Assets/Scripts/Managers/UI Manager.cs` | crafting panel states: enabled / greyed (unaffordable) / locked (undiscovered) |
| `Assets/Scripts/Player/Controls.cs` | crafting is refused in combat; an open panel closes when combat starts |
| `Assets/Tests/EditMode/CraftingRulesTests.cs` | new pure tests |
| `Assets/Tests/PlayMode/CraftingFlowTests.cs` | new behaviour tests |

## Implementation notes

1. **Availability rule.** Crafting is allowed exactly when the player is *not* in turn-based mode — i.e.
   no living enemy shares the player's island. Reuse the same predicate T03 introduced; do not write a
   second definition of "in combat".
2. **Combat interrupts crafting.** If an enemy becomes aware or the player enters an occupied island while
   the crafting panel is open, close the panel. Crafting never costs a turn because it can only happen out
   of combat.
3. **Discovery.** `RecipeDiscovery` tracks which recipes this run has revealed. A recipe is revealed the
   first time the player obtains **any** material its recipe uses. Revealed recipes stay revealed for the
   run. Undiscovered recipes are shown as locked (blacked out), per the GDD, rather than hidden.
4. **Three UI states.** Enabled (discovered + affordable + inventory has room), greyed (discovered but
   missing materials or room), locked (undiscovered). Show the missing materials on greyed entries.
5. **Atomic crafting.** Validate the *entire* recipe and the inventory space before consuming anything. A
   failed craft consumes nothing. This already works in `ItemDefinition.Craft`; keep the behaviour and pin
   it with tests.
6. **Feedback.** Reuse the floating-text helper for "not enough materials" / "inventory full" and show a
   one-time "new recipe unlocked" message when discovery fires.
7. **Persistence note.** Discovery is per run in M1. Do not build permanent blueprint unlocks — that is
   M2/M3. Say so in the checkpoint so the omission is deliberate.

## Test list (EditMode, pure)

- Discovery: obtaining Iron Ore reveals the Iron Sword and Arrow recipes; a recipe whose materials are all
  unknown stays locked; re-obtaining a material does not change the set.
- Affordability: exactly-enough materials is affordable; one short is not; recipe list ordering is stable.
- Requirements: a recipe with an unmet requirement (should one exist later) is greyed and not craftable —
  keep the code path even though M1 has no weapon requirements.
- Atomicity: a craft that fails on missing materials or a full inventory consumes nothing and returns a
  failure result.
- Success: a valid craft consumes exactly the recipe amounts and produces the item.

## Test list (PlayMode)

- Crafting is refused while an enemy is alive on the player's island.
- Opening the crafting panel out of combat, then stepping onto an occupied island, closes it.
- Crafting arrows out of combat with 1 ore grants 10 arrows and consumes the ore.

## Verification

1. EditMode + PlayMode tests green.
2. `execute_method` → `MapgenDiagnostics.TurnPipeline` (gating reuses the turn predicate).
3. `execute_method` → `PlayDriver.Status`.
4. Designer checkpoint: mine ore, watch the recipe appear, craft arrows and a sword out of combat, and
   confirm the panel cannot be opened during a fight.

## Checkpoint

Report the discovery rule as implemented, the three UI states, and confirm that nothing was persisted
(per-run discovery only).

## Out of scope

Permanent blueprint unlocks, the journal, rune crafting, crafting stations, currency.

## Outcome (implemented)

- `CraftingRules` (pure): `IsCraftingAllowed(island, livingEnemies)` is literally
  `!TurnRules.IsTurnBased(...)` - there is no second definition of "in combat". Plus `Reveals`,
  `Missing`/`CanAfford` (demand is summed per material, so two lines asking for 5 Iron need 10 in stock),
  `StateOf` (the three states) and `TryCraft` (validate the whole recipe and the output space, then consume).
- `RecipeDiscovery` (per-run): a recipe is revealed the first time the player handles *any* material it
  uses, and stays revealed. `Inventory.addItem` is the single observation point, so mining, chests, corpse
  drops and starter items all feed it, and each unlock is announced once. `RunState.BeginRun` resets it.
- `CraftingManager` lists every craftable definition with a state: **Enabled** (discovered, affordable and
  there is room), **Greyed** (discovered but missing materials - the entry spells out what is missing - or
  no room), **Locked** (undiscovered, blacked out). Non-enabled entries are non-interactable but still
  explain themselves when clicked.
- Crafting is refused in combat with feedback, and the panel closes when a fight starts: the manager watches
  `Controls.onMoveEvent` and closes it as soon as the player's island holds a living enemy.
- The ammo path: crafting an `ItemKind.Ammo` definition consumes the recipe and grants
  `ArrowEconomy.ArrowsPerOre` arrows via `PlayerStats.AddArrows` instead of adding a stack, so the arrow
  recipe stays "1 ore" and the quiver stays a number (T06).

### Checkpoint answers

- **Discovery rule:** revealed on first handling of any material the recipe uses, kept for the run.
- **Three states:** enabled / greyed (with the missing-materials or "Inventory full" reason) / locked.
- **Nothing is persisted:** discovery is per run and reset at `BeginRun`. There are no blueprint items and
  no journal, deliberately - that is M2/M3.

### Notes

- `ItemDefinition` gained three small members for this: `Recipe` (read accessor), `ConsumeMaterials()`
  (validate and consume without granting, used by the ammo path) and `SetRecipe` for content tooling and
  tests. Gameplay never rewrites a definition.
- The shipped `Arrow` definition has **no recipe yet** - authoring the recipes is the hand-over item - so the
  PlayMode test sets one at runtime to exercise the path. Until those exist the panel lists nothing, because
  nothing is craftable.
- The crafting panel does exist in the dungeon scene (`CraftingManager` plus an assigned `CraftingPanel`);
  its open button calls `LoadAvailableRecipies` from a scene-assigned `onClick`.

### Verified

EditMode 97/97 (12 new crafting tests: summed demand, the three states, discovery predicates, failed-craft
reporting); PlayMode 19/19 - the three new ones assert crafting is refused while an enemy shares the island
and allowed again once it is gone, that a fight closes the panel, and that the arrow recipe consumes exactly
one ore and adds exactly ten arrows.
