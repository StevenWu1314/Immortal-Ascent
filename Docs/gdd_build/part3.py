"""GDD v2 content, part 3: weapons, runes, items/crafting, enemies and bosses."""
from helpers import h1, h2, h3, p, b, n, note, t, kv, tag, page_break


def build():
    # ------------------------------------------------------ 10 weapons
    page_break()
    h1("10. Weapons and equipment")
    h2("10.1 Equipment slots")
    t(["Slot", "Holds", "Notes"], [
        ["Melee", "One melee weapon", "Sword, cleaver. Uses Strength."],
        ["Ranged", "One bow", "Uses Dexterity, requires arrows. Range in tiles."],
        ["Armour", "One armour piece", "MaxHealth only, no damage reduction."],
    ])
    b("There are no accessory, ring, offhand or light slots in v1.")
    b("Equipping removes the item from the backpack. Unequipping returns it. An item is never "
      "simultaneously equipped and carried.")
    note("Gap B8: the current build has no unequip, and equipped items stay in the bag. Both "
         "behaviours must change.")

    h2("10.2 Weapon traits")
    p("Traits are what separates one tier from the next, together with rune slots. Each weapon has "
      "at most one trait, and a trait is a property of the weapon definition, so it cannot be "
      "moved between weapons.")
    t(["Trait", "Effect", "Applies to"], [
        ["Cleave", "A melee attack also hits every other enemy orthogonally adjacent to the target", "Melee"],
        ["Pierce", "An arrow continues through the first enemy it hits and hits the next one in line", "Ranged"],
        ["Multishot", "Firing also shoots the two tiles flanking the target cell, if occupied", "Ranged"],
        ["Knockback", "A hit pushes the target one tile directly away, if that tile is free", "Melee"],
        ["Bleed", "The hit applies Fire-style damage over time with a bleed theme (3 turns)", "Melee"],
        ["Extended range", "+2 tiles of range", "Ranged"],
    ])

    h2("10.3 Weapon progression")
    p("One craftable sword and one craftable bow per realm, plus one signature weapon dropped by "
      "each realm's boss. Rune slots scale with tier: tier 1 has one slot, tier 2 has two, tier 3 "
      "has three.")
    t(["Weapon", "Realm", "Tier", "Source", "Damage", "Req.", "Slots", "Trait"], [
        ["Rusty Sword", "Plains", "0", "Starting kit", "10", "-", "1", "None"],
        ["Wooden Bow", "Plains", "0", "Starting kit", "10", "-", "1", "None"],
        ["Iron Sword", "Plains", "1", "Craft (5 Iron Ore)", "15", "5 STR", "1", "None"],
        ["Composite Bow", "Plains", "1", "Craft (4 Iron Ore, 3 Spider Silk)", "12", "5 DEX", "1", "None"],
        ["Tusk Cleaver", "Plains", "1", "Tusk Sovereign drop", "18", "8 STR", "2", "Cleave"],
        ["Bronze Sword", "Desert", "2", "Craft (5 Bronze Ore)", "25", "12 STR", "2", "Bleed"],
        ["Recurve Bow", "Desert", "2", "Craft (4 Bronze Ore, 3 Scorpion Carapace)", "20", "12 DEX", "2", "Extended range"],
        ["Mirage Bow", "Desert", "2", "Mirage Serpent drop", "22", "14 DEX", "3", "Multishot"],
        ["Frost Iron Sword", "Tundra", "3", "Craft (6 Frost Iron Ore)", "35", "20 STR", "3", "Knockback"],
        ["Longbow", "Tundra", "3", "Craft (5 Frost Iron Ore, 3 Wolf Pelt)", "28", "20 DEX", "3", "Pierce"],
        ["Fallen Immortal's Blade", "Tundra", "3", "Ashen Immortal drop", "45", "25 STR", "3", "Cleave"],
    ], widths=[1.3, 0.62, 0.4, 1.4, 0.72, 0.65, 0.55, 0.85])
    note("All damage, requirement and slot values are proposed starting points. The intent is that "
         "a weapon upgrade is felt through slots and traits first, damage second.")

    h2("10.4 Armour")
    t(["Armour", "Realm", "Source", "MaxHealth bonus"], [
        ["Studded Leather Armor", "Plains", "Craft (3 Pig Yao Skin, 2 Iron Ore)", "+50"],
        ["Bronze Scale", "Desert", "Craft (4 Bronze Ore, 2 Scorpion Carapace)", "+75"],
        ["Frost Iron Plate", "Tundra", "Craft (5 Frost Iron Ore, 2 Wolf Pelt)", "+100"],
    ])
    h2("10.5 Arrows")
    b("Arrows are a tracked count, not an inventory slot each. The HUD shows the current count.")
    b("Firing consumes exactly one arrow.")
    b("Crafting converts 1 metal ore into 10 arrows (proposed ratio; the current build makes 1 "
      "arrow per ore, which caps the bow at the 10 starting arrows).")
    b("Starting arrows: 10.")
    b("A bow cannot fire with zero arrows; the aim overlay shows the player as out of ammo instead.")

    # --------------------------------------------------------- 11 inscriptions
    page_break()
    h1("11. Inscriptions (runes)")
    h2("11.1 Naming and model")
    b("The system is called Inscription. The items are Runes.")
    b("A rune is an item that is applied to one specific piece of equipment, not to a slot or to "
      "the player.")
    b("Runes live on the equipment instance, so two Iron Swords can carry different runes.")
    b("This matches the shipped implementation (EquipmentInstance, InscriptionService).")
    h2("11.2 Slots and applying")
    b("Slot count comes from the weapon tier (1, 2 or 3) - see 10.3.")
    b("Applying a rune consumes it from the inventory and fills the chosen slot.")
    b("Runes are permanent while slotted: they cannot be removed or recovered. Applying a rune to a "
      "full item overwrites the slot the player chooses, destroying the old rune.")
    b("Applying a rune is free and can be done any time outside combat.")
    b("Runes cannot be applied to arrows, materials or consumables.")
    h2("11.3 Rune effects")
    t(["Rune", "Effect type", "Effect (proposed)", "Valid target"], [
        ["Sharp Rune", "Item stat", "+5 Strength to the weapon's damage", "Any weapon"],
        ["Deft Rune", "Item stat", "+5 Dexterity to the bow's damage", "Ranged"],
        ["Sturdy Rune", "Player stat", "+20 MaxHealth while equipped", "Any equipment"],
        ["Ember Rune", "On hit", "Attacks apply Fire (3 turns)", "Any weapon"],
        ["Venom Rune", "On hit", "Attacks apply Poison (3 turns)", "Any weapon"],
        ["Binding Rune", "On hit", "Attacks apply Root (2 turns)", "Any weapon"],
        ["Thunder Rune", "On hit", "Attacks apply Explosive splash", "Any weapon"],
        ["Warding Rune", "Player stat", "+15 MaxHealth and immune to Poison", "Armour"],
        ["Frugal Rune", "Utility", "25% chance not to consume an arrow", "Ranged"],
        ["Lantern Rune", "Utility", "+2 tiles of vision radius", "Any equipment"],
        ["Bounty Rune", "Utility", "+15% chance of a material drop from kills", "Any equipment"],
        ["Insight Rune", "Utility", "+20% experience from kills", "Any equipment"],
    ])
    note("Only item stat bonuses and player stat bonuses exist in code today (InscriptionEffect: "
         "ItemStatBonus, PlayerStatBonus). On-hit and utility effects need new effect types. "
         "Gap B9.")
    h2("11.4 Rune sources and supply")
    b("Chests: the main source. About one in three chests holds a rune.")
    b("Enemies: a small chance (about 5%) from any normal kill, so long fights still pay.")
    b("Bosses: every boss drops one guaranteed rune, biased to its realm.")
    b("Crafting: 3 rune shards craft the rune of the player's choice; shards come from chests and "
      "bosses.")
    b("Blueprint unlocking applies to runes too: a rune found or crafted once enters the permanent "
      "pool.")
    b("Supply target: a full run should hand the player 10-14 runes, so a 2-3 slot weapon is "
      "normally filled by the tundra. Runes must not be scarce - they are the main build currency, "
      "not a trophy.")
    b("Designer note: if playtesting shows players finishing runs with unspent empty slots, raise "
      "the chest rune chance before adding new rune types.")

    # ---------------------------------------------- 12 items, loot and crafting
    page_break()
    h1("12. Items, loot and crafting")
    h2("12.1 Item kinds")
    t(["Kind", "Usable", "Purpose"], [
        ["Material", "No", "Crafting input only"],
        ["Weapon", "Equip", "Melee or ranged, chosen by the ranged flag"],
        ["Armour", "Equip", "Adds MaxHealth"],
        ["Ammo", "No", "Tracked arrow count, consumed per shot"],
        ["Consumable", "Yes", "Pills: stat buffs, healing, cleansing"],
        ["Inscription", "Yes", "Runes, applied to equipment"],
        ["Blueprint", "Yes", "Permanent recipe unlock (proposed new kind)"],
    ])
    h2("12.2 Materials")
    t(["Material", "Realm", "Source", "Used for"], [
        ["Iron Ore", "Plains", "Ore veins, chests", "Iron Sword, Composite Bow, arrows"],
        ["Pig Yao Tusk", "Plains", "Pig Yao kill (30% chance)", "Tusk Cleaver, weapon handles"],
        ["Pig Yao Skin", "Plains", "Pig Yao kill (30% chance)", "Studded Leather Armor, pouches"],
        ["Spider Silk", "Plains", "Spider Yao kill (40% chance)", "Composite Bow, pouches"],
        ["Bronze Ore", "Desert", "Ore veins, chests", "Bronze Sword, Recurve Bow, Bronze Scale"],
        ["Scorpion Carapace", "Desert", "Sand Scorpion kill (35% chance)", "Bronze Scale, Recurve Bow"],
        ["Venom Sac", "Desert", "Sand Scorpion kill (25% chance)", "Venom Rune, poison pills"],
        ["Frost Iron Ore", "Tundra", "Ore veins, chests", "Frost Iron Sword, Longbow, Frost Iron Plate"],
        ["Wolf Pelt", "Tundra", "Frost Wolf kill (40% chance)", "Frost Iron Plate, Longbow"],
        ["Everfrost Crystal", "Tundra", "Ice Wraith kill (20% chance)", "Lantern Rune, rune crafting"],
        ["Rune Shard", "Any", "Chests, boss drops", "Crafting specific runes"],
        ["Spirit Herb", "Any", "Chests", "Pills"],
    ])
    b("Ore veins are the guaranteed material income; monster parts are chance-based.")
    b("Bosses drop a guaranteed signature weapon plus 1 rune and 2-3 rare materials.")
    b("Material drops are type-specific: the enemy that drops it is the enemy you had to kill.")
    note("Gap B10: in the current build no material is obtainable at all - Iron, Web, Tusk and Skin "
         "have no loot pool, chest pool or drop source. Both craftable recipes are therefore dead "
         "and the player never gets more than the 10 starting arrows.")
    h2("12.3 Rarity and what can drop")
    b("Chests and ordinary enemies never drop weapons or armour. Equipment comes from crafting. "
      "The only dropped equipment in the game is the three boss signature weapons.")
    b("Loot rolls keep the current structure: epic 1%, rare 30%, otherwise common, rolled per kill. "
      "With equipment removed from the pools, these tiers now carry runes, materials, pills and "
      "blueprints.")
    b("Rarity is a property of the loot table entry, not of the item definition (matching the "
      "current implementation), so the same item can appear at different rarities in different realms.")
    b("Every realm needs a filled epic pool. The current epic pools are empty, so the 1% epic roll "
      "can never produce anything.")
    h2("12.4 Loot tables per realm")
    t(["Realm", "Common", "Rare", "Epic"], [
        ["Plains", "Iron Ore bundle, Spirit Herb, Health Pill", "Sharp Rune, Sturdy Rune, Rune Shard", "Blueprint: Iron Sword or Composite Bow"],
        ["Desert", "Bronze Ore bundle, Venom Sac, Health Pill", "Ember Rune, Venom Rune, Rune Shard", "Blueprint: Bronze Sword or Recurve Bow"],
        ["Tundra", "Frost Iron Ore bundle, Everfrost Crystal, Health Pill", "Lantern Rune, Insight Rune, Rune Shard", "Blueprint: Frost Iron Sword or Longbow"],
    ])
    p("Chest pools use the same tables with a rune weight of roughly one in three. Ore bundles are "
      "2-3 units.")
    h2("12.5 Recipes and unlocking")
    b("Crafting is the only way to obtain weapons and armour. Chests and enemies supply materials, "
      "runes, pills and blueprints; they never supply equipment.")
    b("A recipe is a list of materials and amounts attached to the item definition; an empty "
      "recipe means the item cannot be crafted (current model, kept).")
    b("In-run unlocking: obtaining a material for the first time this run reveals every recipe that "
      "uses it. Recipes the player cannot afford are shown greyed out.")
    b("Permanent unlocking: blueprint items found in chests or dropped by bosses unlock a recipe "
      "for every future run. Blueprints are the meta progression.")
    b("Crafting validates the whole recipe and inventory space before consuming anything.")
    t(["Item", "Recipe (proposed)", "Requirement"], [
        ["Iron Sword", "5 Iron Ore", "5 Strength"],
        ["Composite Bow", "4 Iron Ore, 3 Spider Silk", "5 Dexterity"],
        ["Arrows x10", "1 Iron Ore", "-"],
        ["Studded Leather Armor", "3 Pig Yao Skin, 2 Iron Ore", "-"],
        ["Pouch", "3 Spider Silk (or Pelt)", "-"],
        ["Bronze Sword", "5 Bronze Ore", "12 Strength"],
        ["Recurve Bow", "4 Bronze Ore, 3 Scorpion Carapace", "12 Dexterity"],
        ["Bronze Scale", "4 Bronze Ore, 2 Scorpion Carapace", "-"],
        ["Frost Iron Sword", "6 Frost Iron Ore", "20 Strength"],
        ["Longbow", "5 Frost Iron Ore, 3 Wolf Pelt", "20 Dexterity"],
        ["Frost Iron Plate", "5 Frost Iron Ore, 2 Wolf Pelt", "-"],
        ["Specific rune", "3 Rune Shards", "-"],
    ])
    h2("12.6 Consumables")
    t(["Item", "Effect", "Duration"], [
        ["Health Pill", "Heals 25 (proposed; currently 10)", "Instant"],
        ["Strength Pill", "+10 Strength", "6 turns"],
        ["Dexterity Pill", "+10 Dexterity", "6 turns"],
        ["Constitution Pill", "+10 MaxHealth", "6 turns"],
        ["Cleanse Pill", "Removes all status effects", "Instant"],
    ])
    b("Buff durations sit in the 5-8 turn band; 6 is the starting value.")
    b("Re-applying a pill replaces the existing buff rather than stacking it (current behaviour, kept).")
    note("Gap B11: asset text is inconsistent with the data. The Strength Elixir description says "
         "50 turns while the value is 10; the Constitution Pill says 'constitution' but maps to "
         "MaxHealth, and no Constitution stat exists; Pig Yao Skin and Pig Yao Tusk both describe "
         "themselves as 'Iron'.")
    h2("12.7 Inventory")
    b("16 slots, matching the shipped UI.")
    b("Pouches add 4 slots each, to a maximum of two pouches (24 slots).")
    b("Equipment never stacks, and one slot holds one piece of equipment.")
    b("Materials and consumables stack to their stack limit (99 for materials, 100 for consumables, "
      "arrows tracked separately).")
    b("When the inventory is full, overflow must be dropped on the ground rather than destroyed.")
    note("Gap B12: today overflow is silently destroyed with a 'inventory full (N lost)' message. "
         "With 16 slots that will happen constantly.")
    h2("12.8 Chests")
    b("1-2 per floor. Contents are rolled at generation time from the realm's loot table.")
    b("Opened with E from an adjacent cell. Free action.")
    b("Opening sets the cell to walkable and updates the island map, so the chest tile does not "
      "block the room - both writes are required (current implementation is correct here).")
    b("Chest contents: material bundles, runes, pills, Spirit Herbs, Rune Shards and blueprints. "
      "Never equipment.")

    # ------------------------------------------------ 13 enemies and bosses
    page_break()
    h1("13. Enemies and bosses")
    h2("13.1 Roster")
    p("Three regular enemy types per realm, plus one boss per realm. Nine regular enemies and three "
      "bosses in total.")
    t(["Enemy", "Realm", "Pattern", "HP", "Damage", "XP", "Resists", "Applies"], [
        ["Pig Yao", "Plains", "Melee chase, brute", "50", "10", "10", "Root", "-"],
        ["Spider Yao", "Plains", "Melee, fast, ranged web", "40", "8", "10", "-", "Root (web)"],
        ["Jing Yao", "Plains", "Ranged, telegraphed, range 5", "30", "10", "10", "Poison", "-"],
        ["Sand Scorpion", "Desert", "Melee, fast", "60", "12", "18", "Poison", "Root (sting)"],
        ["Desiccated Monk", "Desert", "Melee, slow, armoured", "80", "14", "22", "Root, Enfeeblement", "-"],
        ["Sand Wraith", "Desert", "Ranged, range 6, hides in storm", "45", "12", "20", "Fire", "-"],
        ["Frost Wolf", "Tundra", "Melee, very fast", "55", "13", "25", "Root", "Enfeeblement"],
        ["Snow Yao", "Tundra", "Melee brute, applies cold", "90", "15", "30", "Fire", "Root"],
        ["Ice Wraith", "Tundra", "Ranged, range 6", "50", "14", "28", "Root, Fire", "-"],
    ], widths=[0.92, 0.65, 1.06, 0.4, 0.72, 0.45, 1.12, 1.1])
    note("All stats are proposed. Grounding: Pig Yao, Spider Yao and Jing Yao already exist in the "
         "build; the six desert and tundra enemies are new content. Names are subject to the "
         "designer's approval.")
    h2("13.2 Boss rules")
    b("Every boss occupies more than one tile. The Tusk Sovereign is 2x2, Shen's surfaced body "
      "spans 3 tiles, and the Ashen Immortal is 3x3. A hit on any occupied tile damages the boss.")
    b("A multi-tile boss blocks movement and blocks line of sight through every tile it occupies.")
    b("Bosses obey the turn system exactly: exactly one action per turn. No boss ever takes two "
      "actions in a round, and no boss action is ever free. Any design that would give a boss a "
      "second action is invalid.")
    b("Boss arenas are larger than the original GDD's 9-12 limit: 15x15 to 19x19, open, with no "
      "interior walls, so multi-tile movement and charge lanes have room.")
    b("The arena locks when the player enters. Bosses never leave it.")
    b("Each boss has two phases and is immune to Enfeeblement's skip. All three drop a signature "
      "weapon, one guaranteed rune and rare materials.")
    h2("13.3 Boss stat blocks and behaviour")
    t(["Boss", "Realm", "Size", "HP", "Phase 1 (one action per turn)", "Phase 2 (below 50%)"], [
        ["The Tusk Sovereign", "Plains", "2x2", "400",
         "Charge: spends its action marking a 2-tile-wide lane across the arena. On its next turn it charges that lane as its action for 20 damage and knocks back everything hit, then must spend its following turn recovering. Alternatively Gore (15, adjacent) or Roar (alerts the arena).",
         "Drops the recovery turn, so it can charge every other turn. Gains Summon Boar (one Pig Yao, costs its action). Its charge leaves rubble in the tiles it crossed (impassable). Still only one action per turn."],
        ["Shen, the Mirage Serpent", "Desert", "3 tiles of body when surfaced", "600",
         "Burrow or Surface: one action. Untargetable while burrowed; resurfaces in a new position. While surfaced its body occupies 3 tiles that block player movement and line of sight, so the player must route around it or break through it. Also Bite (18, adjacent) and Sand Blast (15, ranged line, telegraphed one full turn ahead).",
         "Creates one mirage copy per turn as its action. Copies block movement, have 1 HP and deal no damage. The real serpent's attacks hit for double. The sandstorm tightens to 3 tiles."],
        ["The Ashen Immortal", "Tundra", "3x3", "900",
         "Frost Ring: telegraphs an expanding ring of ice across the arena, which detonates on its next action. Also Teleport (blink to a new position, one action) and Strike (20, adjacent).",
         "Ashen Ground: may instead spend its action corrupting a 3x3 patch of floor into a hazard tile. Ashen Ground applies Fire to anything standing on it, persists for the rest of the fight, and cannot be lit by torches. It keeps Frost Ring and Teleport as alternatives. It no longer extinguishes torches; the spreading hazards are the pressure instead."],
    ], widths=[1.05, 0.6, 0.6, 0.48, 1.97, 1.95])
    note("Open item A9: whether boss arenas get unique tilesets (recommended: unique tileset and "
         "hazard art for the final boss only, to keep art scope down).")
    note("Gap B21: multi-tile bosses are new. The current entity, movement, line-of-sight and turn "
         "queue code all assume one entity per cell, so this touches EntityManager, movement "
         "validation, range targeting and the enemy turn queue.")
    h2("13.4 Enemy awareness, restated")
    t(["Realm", "Awareness", "Reason"], [
        ["Plains", "Room-wide on entering the island", "Islands define rooms, so the boundary is readable"],
        ["Desert", "Detection radius with line of sight", "Open field; lets the player route around threats"],
        ["Tundra", "Detection radius with line of sight", "Open field; pairs with limited vision"],
    ])
    h2("13.5 Resistances")
    b("A resistance means the status cannot be applied at all. There is no partial resistance.")
    b("Every enemy resists at least one status, which is what makes rune choice matter per realm "
      "(for example, desert enemies resist Fire but not Poison).")
    b("Resistances are listed in the table in 13.1 and are shown in the UI once the enemy has been "
      "killed at least once.")
    h2("13.6 Per-floor scaling")
    b("Within a realm, each floor multiplies enemy health by (1 + 0.15 x floor index) and enemy "
      "damage by (1 + 0.10 x floor index), where the realm's first floor is index 0.")
    b("Across realms, the enemy roster changes rather than the multipliers jumping, so the player "
      "reads difficulty as new threats, not bigger numbers.")
    b("Boss health and damage are fixed values, not scaled, because their floors are fixed.")
    h2("13.7 Drops and experience")
    b("Every kill gives experience and rolls the realm's loot table once.")
    b("Chance-based, type-specific material drops as listed in 12.2.")
    b("Damage-over-time kills give full credit.")
