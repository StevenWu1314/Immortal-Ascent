# T06 — Materials, loot rework and the arrow economy

**Depends on:** T01 (floor generation exposes free cells).
**GDD:** 12.2 (materials), 12.3 (chests and enemies never drop equipment), 12.4 (loot tables), 12.5
(recipes, arrows 1 ore = 10), 12.7 (inventory, overflow must not destroy items), 5.5 (ore veins), 13.7
(drops).
**Fixes:** GDD gap B10 — no material is obtainable at all today, so both recipes and the arrow economy are
dead ends; and gap B12 — inventory overflow destroys items.

## Goal

Materials enter the economy (mine them, or take them off corpses), chests and enemies stop handing out
equipment, arrows become a usable resource, and nothing is silently destroyed when the inventory is full.

## Files

| Path | Change |
| --- | --- |
| `Assets/Scripts/LootTable.cs` | new ScriptableObject: rarity entries, restricted kinds |
| `Assets/Resources/LootTables/Plains.asset` | new data asset (values below) |
| `Assets/Scripts/Item/LootGen.cs` | roll against the realm's `LootTable`; never return equipment |
| `Assets/Scripts/Enemy/*` | per-enemy material drop table reference |
| `Assets/Scripts/OreVein.cs` + `OreVeinRegistry.cs` | new: mineable ore cells |
| `Assets/Scripts/Level Generator/TilePlacer.cs` | place 2-4 ore veins per floor on reachable cells |
| `Assets/Scripts/Player/Controls.cs` | mine with E from an adjacent cell (costs a turn) |
| `Assets/Scripts/Player/Inventory.cs` | overflow handling; ammo becomes a counter |
| `Assets/Scripts/Item/Chest.cs` | never clear contents it could not hand over |
| `Assets/Scripts/GroundItems.cs` | new: dropped items on the floor, picked up by walking over them |
| `Assets/Tests/EditMode/LootTableTests.cs`, `ArrowEconomyTests.cs`, `InventoryOverflowTests.cs` | new |

## Implementation notes

1. **Loot table as data.** `LootTable` holds `common`, `rare`, `epic` lists of `ItemDefinition` plus the
   roll chances (keep the shipped structure: epic 1%, rare 30%, else common). It must **reject equipment**
   entries: add a guard at roll time (and a test) so a weapon or armour can never come out of a chest or a
   corpse. M1 populates `Plains.asset`:
   - common: Iron Ore bundle, Spirit Herb, Health Pill
   - rare: Sharp Rune, Sturdy Rune, Rune Shard
   - epic: Blueprint placeholder — **but see 8 below**: M1 has no blueprint items, so leave the epic list
     empty and note it, rather than inventing content.
2. **Type-specific material drops.** Each enemy type references a small drop table: Pig Yao → Pig Yao
   Tusk 30% / Pig Yao Skin 30%; Spider Yao → Spider Silk 40%; Jing Yao → Spider Silk 15% / Iron Ore 20%
   (these are proposed starting values; they live in data, not code). Rolls use the run's seeded RNG so
   they are testable.
3. **Ore veins.** 2-4 per floor, on free reachable cells, finite (they deplete). Mining costs one turn,
   requires standing adjacent, and yields 1-3 ore of the realm's metal (Plains: Iron Ore). Reuse the chest
   interaction pattern: registry plus a grid value, and the same two-write destruction path.
4. **Arrows become a count, not items.** Replace the Ammo item stack with a tracked count on the player
   (persisted for M3). Firing consumes one; the HUD shows the count (T08). Entering aim mode with zero
   arrows is refused with feedback. Recipe: 1 Iron Ore → 10 arrows.
5. **Crafting recipe changes (data only).** Iron Sword 5 Iron Ore; Composite Bow 4 Iron Ore + 3 Spider
   Silk; Arrows x10 1 Iron Ore; Studded Leather Armor 3 Pig Yao Skin + 2 Iron Ore; Pouch 3 Spider Silk.
   Do not add weapon stat requirements in M1.
6. **Overflow must not destroy anything.**
   - Chest: hand over what fits; keep whatever did not fit inside the chest so the player can come back
     with room. Do not clear contents until they are actually taken.
   - Enemy drop that does not fit: spawn a `GroundItems` drop on the enemy's cell. Walking over it picks it
     up (free, not a turn); if it still does not fit, it stays on the ground.
   This is the largest single addition in T06 — if it threatens to overrun the task, split ground drops
   into their own task and say so at the checkpoint rather than silently shipping lost loot.
7. **Seeded RNG.** All loot and drop rolls take the run's RNG from `RunState` (T02) so a test can pin a
   seed and assert exact outcomes.
8. **Do not build blueprints.** Blueprint items, the journal and permanent unlocks are M2/M3. Leaving the
   epic list empty is correct and must be recorded as intentional in the checkpoint.

## Test list (EditMode, pure)

- Roll distribution with a fixed seed is stable (same seed → same sequence).
- A loot table containing a weapon or armour entry is rejected by the guard, and no chest or corpse roll
  can ever return a `Weapon` or `Armour` kind.
- Material drop mapping: each Plains enemy's drop table yields the documented materials only.
- Arrow economy: 1 ore → 10 arrows; firing decrements by exactly 1; firing at 0 is refused.
- Ore vein: yield is within 1-3; a depleted vein yields nothing and is no longer minable.
- Inventory overflow: adding to a full inventory returns the leftover count; it is never dropped on the
  floor silently; a partial hand-over fills what fits and reports the remainder.
- Chest: contents that did not fit remain in the chest.

## Test list (PlayMode)

- Mining an adjacent vein costs a turn, grants ore, and depletes the vein.
- A full inventory + an enemy kill leaves the loot on the ground; walking over it picks it up.
- Chest with a full inventory keeps its remaining contents; reopening it after making room hands them over.

## Verification

1. EditMode + PlayMode tests green.
2. `execute_method` → `MapgenDiagnostics.ChestWalkability` and `OpenRealChest` (the chest hand-over path
   changed — these must still pass).
3. `execute_method` → `MapgenDiagnostics.ConsumableBuff` (inventory paths changed).
4. `execute_method` → `PlayDriver.OpenAdjacentChest`, then `PlayDriver.Status`.
5. Designer checkpoint: mine a vein, kill a few enemies, confirm crafting materials actually accumulate and
   that arrows are no longer capped at 10.

## Checkpoint

Report the Plains loot table as populated, the material drop percentages used, whether ground drops were
built or split out, and the explicit note that the epic list is intentionally empty (no blueprints in M1).

## Out of scope

Blueprints, the journal, permanent unlocks (M2/M3). Weapon stat requirements. Rune sources and rune
crafting. Desert and tundra material tables.
