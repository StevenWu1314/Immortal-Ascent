# Item System

Rogue Asian Fantasy keeps all item data in **`ItemDefinition`** ScriptableObject assets. An
inventory entry is an **`ItemStack`** — a definition plus a count — so item data is shared and
immutable, and only the count lives in the save/UI layer.

This replaced an older design where `Item : MonoBehaviour` served as both the asset definition
and the runtime inventory entry. That design could not work correctly: inventory copies were
made with `MemberwiseClone` (which cannot clone a Unity object), `stackLimit` had no reader
anywhere, and item identity depended on a mutable display string.

| Concern | Lives in |
|---|---|
| What an item *is* | `ItemDefinition` asset (`Assets/ItemDefinitions/`) |
| How many the player holds | `ItemStack.amount` inside `Inventory` |
| What using it does | `ItemUsage.Use()` |
| Damage / reach / stacking rules | `PlayerStats`, `Equipments`, `Inventory` |

---

## 1. Creating an item

**Project window → right-click → Create → ScriptableObjects → Item Definition**

Save it under `Assets/ItemDefinitions/`. This folder is *not* in `Resources`; definitions are
referenced directly, so moving one is safe as long as you keep the `.meta` file with it.

> Renaming the **asset** is safe (references are by GUID). Renaming a **serialized field** in
> `ItemDefinition.cs` is not — see [Gotchas](#7-gotchas).

---

## 2. Field reference

### Identity

| Field | Meaning |
|---|---|
| `id` | **Currently read by nothing** — `ItemDefinition.Id` has no callers. Reserved for future save data / documentation. Safe to leave empty. |
| `displayName` | Shown in the UI. Falls back to the asset filename when empty, so an item can never render blank. |
| `description` | Multi-line description shown in the item panel. |
| `icon` | Sprite used by the inventory slot and the info panel. |

### Stacking

| Field | Meaning |
|---|---|
| `stackLimit` | Maximum units in one stack. Clamped by code to a minimum of 1, so **1 means "never stacks"**. |

### Kind

`kind` is what decides behaviour. Every other field is ignored unless the kind uses it.

| `ItemKind` | Behaviour |
|---|---|
| `Material` | Not usable. Crafting input only (e.g. Iron). |
| `Weapon` | Equippable. `ranged` decides which slot: ranged → ranged slot, otherwise melee slot. |
| `Armor` | Equippable. Adds `healthBonus` to MaxHealth. |
| `Ammo` | Not usable directly. Consumed one-per-shot when firing a bow. |
| `Consumable` | Usable. Applies `stat` and consumes one unit. |

### Weapon section

| Field | Meaning |
|---|---|
| `damage` | Added to the player's strength (melee) or dexterity (ranged) in `PlayerStats.attack`. |
| `ranged` | Marks this as a ranged weapon, which is what puts it in the ranged slot. |
| `range` | Reach in tiles. **`ItemDefinition.Range` returns 0 unless `ranged` is set**, so a melee weapon can never contribute range. |

### Armor section

| Field | Meaning |
|---|---|
| `healthBonus` | Added to MaxHealth while equipped. Equipping new armor subtracts the outgoing armor's bonus first, so swapping cannot accumulate health. |

### Consumable section

| Field | Meaning |
|---|---|
| `stat` | `Strength`, `Dexterity`, `MaxHealth`, or `Heal`. |
| `effectAmount` | Magnitude of the effect. |
| `effectDuration` | Duration in turns. **Ignored by `Heal`**, which restores health immediately via `PlayerStats.heal()`. The other three apply a timed buff through `PlayerStats.increaseStatTemp()`. |

> **All sections are shown in the Inspector regardless of `kind`.** Nothing validates that you
> filled in the right one. A `damage` value on an `Armor` item is silently never read.

---

## 3. Where items are wired in

### a) Starting inventory

`Assets/Prefabs/Player.prefab` → `Inventory` component → **`StarterItems`** array.
Each entry is `{ definition, amount }`.

Amounts that do not fit are reported in the Console as a warning rather than silently dropped.

### b) Enemy loot

Each scene's **`LootGen`** component (on the `UIManager` / `Manager` object):

| Field | Meaning |
|---|---|
| `epicChance` | Percent chance of the epic tier. Evaluated **first**. |
| `rareChance` | Percent chance of the rare tier, evaluated second. |
| `commonItem` / `rareItem` / `epicItem` | `ItemDefinition` pools. |
| `dropAmount` | Units granted per drop. |

Evaluation order: `roll < epicChance` → epic; else `roll < epicChance + rareChance` → rare;
else common. **An empty pool falls through to the next tier**, so a drop is never lost.

> ⚠️ **There are three separate `LootGen` components** — one each in `Branching dungeon.unity`,
> `mapgen testing.unity`, and `SampleScene.unity`. Editing one does not affect the others.
> All three currently have **empty `epicItem` pools**, so the epic tier is reachable in
> structure but has nothing to grant.

### c) World resource nodes

`Assets/Prefabs/Collectables/Iron.prefab` → `Iron` component (derived from `Collectables`):

| Field | Meaning |
|---|---|
| `definition` | The item granted when harvested. |
| `dropRange` | Inclusive `{x, y}` range of units granted per pickup. |

> ⚠️ `PerlinNoiseMap` registers the **component on the prefab asset** into `CollectableMap`,
> not a scene instance. `definition` must therefore be set on the prefab itself — setting it on
> a scene instance would have no effect.

### d) Crafting recipes

The recipe lives on the item itself. `ItemDefinition` has an optional `recipe` list; an item with
an empty recipe simply cannot be crafted.

| Field | Meaning |
|---|---|
| `recipe` | List of `{ material, amount }`. Empty = this item is not craftable. Each `material` is an `ItemDefinition`. |

`CraftingManager.LoadAvailableRecipies()` walks `Resources.LoadAll<ItemDefinition>("ItemDefinitions")`
and shows a button for every craftable item the player can currently afford. **Item definitions must
stay under `Assets/Resources/ItemDefinitions/`** or the crafting panel will not see them.

> `ItemDefinition.Craft()` validates the entire recipe and checks `hasRoomFor()` **before** removing
> anything, so a partially available recipe or a full inventory cannot eat the inputs.

The button icon is `ItemDefinition.Icon` and the recipe is edited in the normal Inspector — there is
no separate recipe asset and no text import/export step.

---

## 4. Player-facing behaviour

| Action | Result |
|---|---|
| Left-click an inventory slot | Shows the item panel. The count is read live from `Inventory.countOf()`, so using the last unit refreshes immediately. |
| Right-click an inventory slot | Uses the item. |
| Click the button in the item panel | Uses the item. |

Using a consumable removes exactly one unit; the stack disappears when it reaches zero.

---

## 5. Code API

```csharp
// Add items. Returns how many units could NOT be stored (0 means everything fit).
int leftover = Inventory.Instance.addItem(definition, 5);
if (leftover > 0)
{
    UIManager.Instance.DrawFlowupText(
        $"Inventory full ({leftover} x {definition.DisplayName} lost)",
        PlayerStats.Instance.transform.position);
}

// Remove / query.
Inventory.Instance.removeItem(definition, 2);
int held   = Inventory.Instance.countOf(definition);
bool fits  = Inventory.Instance.hasRoomFor(definition, 3);

// Iterate stacks.
foreach (ItemStack stack in Inventory.Instance.getItems())
{
    Debug.Log($"{stack.definition.DisplayName} x{stack.amount}");
}

// Equip / use directly.
ItemUsage.Use(definition, Inventory.Instance);
Equipments.Instance.Equip(definition);
int range = Equipments.Instance.getRange();   // 0 when no ranged weapon is equipped
```

Full `Inventory` surface: `addItem`, `removeItem`, `countOf`, `hasRoomFor`, `getItems`,
`updateInventory`, `setItemDisplay`, `getItemDisplay`.

---

## 6. Extending the system

**Adding a new item kind** (for example, a throwable):

1. Add a value to the `ItemKind` enum in `Assets/Scripts/Item/ItemDefinition.cs`.
2. Add a branch to the `switch` in `ItemUsage.Use()`.
3. Decide whether `DisplayLabel` / `DisplayValue` should show a row for it.
4. If it needs new data, add a `[SerializeField]` field to `ItemDefinition`.

Adding fields is safe: existing assets simply keep the field's default value.

---

## 7. Gotchas

- **Never rename or change the type of an existing `[SerializeField]` field.** Values are
  stored in scene/prefab/asset files keyed by field name; renaming silently resets every asset
  to its default. (Adding fields is fine.)
- **`kind` drives everything.** Mismatched sections are silently ignored, with no warning.
- **`Heal` ignores `effectDuration`.**
- **`range` is ignored unless `ranged` is set.**
- `stackLimit` is clamped to a minimum of 1 — you cannot make an item unholdable.
- Crafting, looting, and harvesting all report a full inventory through
  `UIManager.DrawFlowupText`; nothing is silently destroyed any more. If you add a new item
  source, use the `addItem` return value and do the same.

---

## 8. Current data state

The ten existing items were migrated mechanically from the old prefabs by
`Assets/Scripts/Item/Editor/ItemMigration.cs`. The captured data is at
`Library/ItemMigrationMap.json`.

Consumable values were taken from the **behaviour the old code actually executed**, not from
the old constructors — those constructors never ran (MonoBehaviour constructors are not
invoked by Unity), so their `effectStrength` values were never serialized into the prefabs.
Migrated consumables therefore behave exactly as before the refactor.

| Asset | Kind | Values as migrated |
|---|---|---|
| `Iron.asset` | Material | `stackLimit` 99. Also a world pickup via `Iron.prefab`. |
| `Arrow.asset` | Ammo | `stackLimit` 100. Recipe: 1 `Iron`. |
| `ConPill.asset` | Consumable | Max Health +10 for 50 turns. |
| `DexPill.asset` | Consumable | Dexterity +10 for 50 turns. |
| `StrPill.asset` | Consumable | Strength +10 for 50 turns. |
| `healthPill.asset` | Consumable | Heal 10 (immediate; duration ignored). |
| `normal sword.asset` | Weapon | damage 15, melee. Recipe: 5 `Iron`. |
| `rusty sword.asset` | Weapon | damage 10, melee. |
| `Wooden Bow.asset` | Weapon | damage 10, ranged, range 10. |
| `Studded Leather Armor.asset` | Armor | `healthBonus` 50. |

> Note that `Iron` needs 5 units per Iron Sword recipe but stacks up to 99, while equipment
> stacks are capped at 1. These are inherited values from the old prefabs, not deliberate
> balance — adjust freely in the Inspector.

---

## 9. Leftovers and cleanup

- **`Tools/Items/` menu items are now dead.** Step 1 identifies items by the old `ItemName`
  property, and those scripts have been deleted, so re-running it captures nothing. Steps 2 and
  3 are idempotent but already applied. `ItemMigration.cs` can be deleted, along with
  `Library/ItemMigrationMap.json`.
- **Nine equipment/consumable prefabs are now empty GameObjects** — their data moved into
  `ItemDefinition` assets. They were deliberately *not* deleted, because deleting them could
  break any scene instance placed by hand. They can be removed once you confirm nothing
  references them.
- **`epicItem` pools are empty in all three scenes.** The epic tier is structurally reachable
  but grants nothing until you populate them.
