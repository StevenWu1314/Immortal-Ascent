"""GDD v2 content, part 4: UI, art/audio, technical design, milestones, appendices."""
from helpers import h1, h2, h3, p, b, n, note, t, kv, tag, page_break


def build():
    # ---------------------------------------------------------------- 14 UI/UX
    page_break()
    h1("14. UI and UX")
    h2("14.1 HUD")
    t(["Element", "Status", "Notes"], [
        ["HP bar", "Exists", "Keep; add current/max numbers"],
        ["XP bar + level", "Exists", "Add the level number beside the bar"],
        ["Minimap", "New", "Explored terrain, discovered stairs, known chests. Enemies always hidden"],
        ["Arrow counter", "New", "Current arrow count; turns red at 0"],
        ["Equipped weapon indicator", "New", "Shows sword or bow, and the draw state"],
        ["Status effect icons", "New", "Icon plus turns remaining, for the player's own statuses"],
        ["Realm + floor indicator", "New", "e.g. 'Desert - Floor 2'"],
        ["Quick-use pill slot", "New", "One slot for the selected pill, used with a key"],
        ["Turn indicator", "New", "Shows whose turn it is and that input is locked during the enemy turn"],
    ])
    h2("14.2 Screens")
    t(["Screen", "Status", "Contents"], [
        ["Inventory + item detail", "Exists", "16 slots; detail pane shows name, description, stats, runes and Use button"],
        ["Crafting", "Exists", "Recipe list, greyed when unaffordable or locked; shows requirements"],
        ["Level-up allocation", "Exists", "5 points across Strength, Dexterity, MaxHealth, with reset"],
        ["Rune application", "Exists", "Target list (equipped first), then slot chooser for the chosen item"],
        ["Pause + settings", "Partial", "Volume exists; add keybind reference"],
        ["Controls / how to play", "New", "One page listing every binding and the turn rules"],
        ["Death screen (run summary)", "New", "Realm and floor reached, kills, time, materials gathered, unlocks earned"],
        ["Victory / ending screen", "New", "Ending text for defeating the Ashen Immortal"],
        ["Blueprint journal", "New", "Every permanently unlocked weapon, rune and recipe, per realm"],
    ])
    h2("14.3 Controls")
    t(["Input", "Action"], [
        ["W A S D", "Move one tile (turn-based) or walk continuously (free movement)"],
        ["1", "Draw the melee weapon"],
        ["2", "Draw the bow / enter aim mode (free)"],
        ["Left click", "Fire at the highlighted tile while aiming"],
        ["Right click", "Cancel aim mode"],
        ["E", "Interact: open chest, mine ore, take stairs"],
        ["I / Tab", "Inventory"],
        ["C", "Crafting"],
        ["Esc", "Pause / close the current panel"],
        ["Number keys in panels", "Quick-use slots for pills"],
    ])
    b("Input stays keyboard + mouse: WASD plus mouse aim. Keyboard-only aiming and gamepad support "
      "are out of scope for v1.")
    b("The player is locked out of movement while any full-screen panel is open, and panels cannot "
      "be opened during the enemy turn.")
    h2("14.4 Feedback rules")
    b("Damage numbers float up from the victim (already implemented).")
    b("The telegraphed enemy shot is drawn as a low-opacity red overlay on the target tile.")
    b("The player flashes and shakes briefly when hit (already implemented).")
    b("Full inventory and blocked actions report through the floating text system rather than the "
      "console.")
    b("Picking up a material for the first time shows a 'new recipe unlocked' toast, which is the "
      "in-run legibility for recipe unlocking.")
    h2("14.5 Minimap rules")
    b("Terrain is revealed as the player explores, and stays revealed for the floor.")
    b("Stairs appear when discovered; the marker persists after discovery.")
    b("Chests and ore veins appear once their island has been entered.")
    b("Enemies are never shown on the minimap, in any realm.")
    b("The minimap is per floor and is cleared on descent.")

    # ------------------------------------------------------------ 15 art audio
    page_break()
    h1("15. Art and audio")
    h2("15.1 Art scope")
    kv([
        ("Style", "32x32 pixel art, matching the current pipeline"),
        ("Tilesets", "One per realm (plains, desert, tundra), including water/dune/snow variants and hazard tiles"),
        ("Enemies", "9 regular enemies plus 3 bosses. Bosses are multi-tile: 2x2 (Tusk Sovereign), a 3-tile body (Mirage Serpent) and 3x3 (Ashen Immortal), each with idle, move, attack and death animation"),
        ("Player", "Sword, bow, aim, hit and death animations; the GDD's 'player holding weapon' requirement"),
        ("Obstacles", "Bushes, rocks, huabiao pillars, spider webs"),
        ("Hazards", "Slippery ice, crevasses, rubble, and the final boss's Ashen Ground tiles"),
        ("Items", "Icons for 7 weapons, 3 armour pieces, 12 materials, 5 consumables, 12 runes"),
        ("UI", "HUD frames, status icons, minimap frame, panel art, journal art"),
    ])
    b("Reuse existing tiles with per-realm palette work where a full new set is not ready; the "
      "palette pass is what makes a realm read differently at first glance.")
    h2("15.2 Art requirements carried over from the original GDD")
    n("Player sprite visibly holding the current weapon (sword or bow).")
    n("A visual for skipping a turn, so the player can see an enemy's turn was consumed.")
    n("The telegraphed ranged attack: low-opacity red on the threatened tiles.")
    n("Torch placement art, and a visible lit radius on the tundra.")
    n("Boss telegraph art: the charge lane, the frost ring and the Ashen Ground tiles must all read "
      "clearly one full turn before they hurt the player.")
    h2("15.3 Audio scope")
    kv([
        ("Music", "One ambient track per realm, plus one boss theme per realm (6 tracks)"),
        ("Ambience", "Water, wind/sandstorm, blizzard, torch crackle"),
        ("SFX", "Sword hit, bow fire, arrow hit, enemy death, level up, craft, rune apply, chest open, ore mine, UI clicks, player hurt"),
        ("Mixing", "Master volume plus separate music and SFX sliders"),
        ("Darkness", "The tundra's audio should narrow with vision; wind masks distant enemy audio"),
    ])

    # -------------------------------------------------------- 16 technical
    page_break()
    h1("16. Technical design")
    h2("16.1 Constraints to keep")
    b("Unity 2022.3.62f2, URP 2D, Tilemap, TextMeshPro, legacy Input, no namespaces, no asmdef.")
    b("ItemDefinition stays a shared, immutable ScriptableObject. Per-item state (runes) lives on "
      "the equipment instance, never on the definition.")
    b("Item definitions stay under Assets/Resources/ItemDefinitions/<Kind>/ so the crafting loader "
      "keeps working.")
    b("The grid remains the single authority for movement. Chests must update both the grid value "
      "and the island map when opened.")
    b("Scenes: ui.unity is build index 0 (the death screen loads it) and 'Branching dungeon.unity' "
      "is index 1.")
    h2("16.2 Systems to add")
    t(["System", "Purpose", "Depends on"], [
        ["RunState", "Realm, floor, seed, RNG state; drives generation and saving", "Nothing"],
        ["FloorGenerator changes", "6-8 islands per floor, multiple floors, stairs and boss islands", "RunState"],
        ["Status system", "Apply, tick, expire, resist, skip-turn for all 5 statuses", "Enemy behaviour refactor"],
        ["Enemy awareness", "Detection radius, line of sight, roaming for unaware enemies", "Line of sight"],
        ["Line of sight", "Blocking check used by player aim and enemy ranged attacks", "Obstacle tilemap"],
        ["Multi-tile entities", "Bosses occupying 2x2 / 3x3 / a 3-tile body, with movement, collision and targeting rules", "EntityManager, line of sight"],
        ["Loot tables", "Per-realm common/rare/epic pools, materials and runes only (no equipment)", "Realm data"],
        ["Blueprint/Unlock store", "Permanent unlocks and the journal", "Save system"],
        ["Save system", "Run autosave at floor transitions; unlocks and settings", "RunState"],
        ["Minimap", "Explored-tile reveal and markers", "FloorGenerator"],
        ["Localization tables", "All player-facing strings", "Nothing"],
    ])
    h2("16.3 Save format")
    b("Run state (autosave at each floor transition): realm index, floor index, player level and "
      "stats, current health, inventory contents including equipment instances and their slotted "
      "runes, unlocked-this-run recipes, arrows, and remaining placed torches.")
    b("Permanent data (JSON, in the persistent data path): unlocked blueprints, journal entries, "
      "lifetime stats.")
    b("Settings (PlayerPrefs, as today): master/music/SFX volume, any keybind overrides.")
    b("A run save is deleted on death or victory; unlocks survive both.")
    h2("16.4 Performance and platform")
    b("Ship on Windows and macOS from the same project. Both builds are first-class; the game must "
      "be playable on Apple Silicon natively, not only through translation.")
    b("Target 60 fps at 1080p on a mid-range Windows PC and on Apple Silicon macOS.")
    b("Save data, unlock data and settings all use Application.persistentDataPath and PlayerPrefs, "
      "which behave the same on both platforms; no Windows-only file APIs.")
    b("Verify input and fullscreen behaviour on both platforms, since the game is keyboard + mouse "
      "only.")
    b("120x120 grid with 6-8 islands and up to 12 enemies is well within budget; the risk is "
      "per-tile work in the fog-of-war and lighting pass, which must be limited to the visible "
      "radius rather than the whole map.")
    b("Render the tundra light pass through a single Tilemap or a small mesh, not one GameObject "
      "per lit tile.")

    # ---------------------------------------------------------- 17 milestones
    page_break()
    h1("17. Milestones")
    p("The original week-by-week table (dated May 2026 with placeholder tasks) is replaced by "
      "milestone phases with no hard dates. Each milestone is playable and testable.")
    h2("M1 - Plains, finished end to end")
    b("Scope: one realm, complete. Player starts a run, clears 2 plains floors of 6-8 islands, "
      "finds the stairs, descends, fights the Tusk Sovereign, and reaches a victory placeholder.")
    b("Run structure: RunState, floor generation with 6-8 islands, exit stairs, boss island, "
      "descend transition, death and victory screens.")
    b("Combat: all five statuses with tick and resist rules; enemy awareness (room-wide in plains) "
      "and roaming; line of sight; blocked moves are free; aim mode is free to enter.")
    b("Boss: the Tusk Sovereign as a 2x2 multi-tile entity in a 15x15-plus open arena, obeying one "
      "action per turn, with the telegraphed charge lane and its phase 2.")
    b("Items: materials obtainable (ore veins plus Pig Yao and Spider Yao drops), full recipe list "
      "for the plains, arrows in batches of 10, rune slots scaling with tier, and the on-hit rune "
      "effects that plains needs.")
    b("UI: minimap, arrow counter, weapon indicator, status icons, realm/floor indicator, turn "
      "indicator, quick-use pill slot, death summary, blueprint journal.")
    b("Exit criteria: a full plains run is playable in one sitting, and no known crash remains.")
    h2("M2 - All three realms")
    b("Desert and tundra generation, sandstorm vision, darkness and torches, ice and crevasses.")
    b("Nine regular enemies plus three bosses with their phases and resistances.")
    b("Complete weapon, armour, rune and material tables for all three realms.")
    b("Per-floor scaling, full loot tables with filled epic pools, boss drops.")
    h2("M3 - Meta, save and polish")
    b("Run autosave and resume at floor transitions; permanent unlock JSON; journal completeness.")
    b("Localization string tables and a full text pass.")
    b("Balance pass across the whole run: experience curve, enemy health scaling, pill and status "
      "numbers, weapon requirements.")
    b("Audio pass, UI polish, control reference screen, and the tuning items listed in Appendix A.")
    h2("18. Out of scope for v1")
    b("Fire spreading to terrain, destructible obstacles, and burning tiles.")
    b("Accessory, ring, offhand and light equipment slots.")
    b("NPCs, dialogue system, shops, currency and a hub scene.")
    b("Gamepad and keyboard-only aiming.")
    b("Mid-floor saving, extra lives, and endless or daily-run modes.")
    b("A bestiary screen (resistance display is proposed but optional; see Appendix A item A8).")

    # ------------------------------------------------------------ appendix A
    page_break()
    h1("Appendix A - Open decisions")
    p("Each item below is undecided. A recommendation is given for each; if the designer accepts "
      "them, they can be folded into the body text as decisions.")
    t(["#", "Question", "Recommendation"], [
        ["A1", "Should the desert's sandstorm also reduce how far enemies notice the player, or only the player's vision?", "Vision only. Reducing both makes the desert unfair rather than tense."],
        ["A2", "Flat additive stats plus 5 points per level will outgrow weapon damage by the tundra. How should the curve be controlled?", "Scale enemy health per floor as specified, and review weapon tier damage after the first full-run playtest."],
        ["A3", "Does the tundra's permanent torch light persist across floors?", "No. Torches placed are consumed, but light does not carry between floors."],
        ["A4", "Can the player place a torch in combat?", "Yes, at the cost of one turn. It is a legitimate tactical choice."],
        ["A5", "Are chests visible on the minimap before their island is entered?", "No. They appear when the island is entered, matching the exploration intent."],
        ["A6", "Do ore veins run out, and are there enough of them?", "2-4 per floor, finite, so a player who skips mining feels the shortage later."],
        ["A7", "Does slippery ice affect enemies as well as the player?", "Yes. It makes the ice a battlefield the player can exploit with knockback and Root."],
        ["A8", "Is the enemy resistance display in scope (bestiary or tooltip)?", "Tooltip on the enemy, revealed after the first kill. Keeps the information in-world and avoids a new screen."],
        ["A9", "Does each boss arena get its own tileset and hazard?", "Unique hazard for the final boss only; the first two reuse the realm tileset."],
        ["A10", "Do runes have rarity, and do rarer runes need more slots?", "Runes have rarity for loot purposes only; every rune costs one slot."],
        ["A11", "Can the player hold more than one of the same rune and choose?", "Yes; runes stack in the inventory as separate items with a count, and applying consumes one."],
        ["A12", "Is there a use for spare materials late in a run?", "Sell-less economy: spare ore converts to arrows and pills, which keeps it useful."],
        ["A13", "How big is a pouch craft, and what caps it?", "Two pouches maximum, +4 slots each, crafted from realm cloth/leather."],
        ["A14", "Should the player be able to drop items on the ground?", "Yes, dropping is needed to make room and to fix the current overflow-destroys-items behaviour."],
        ["A15", "Do enemies respawn on a floor the player has left?", "No. Floors are one-way, so respawning has no purpose."],
        ["A16", "What exactly does the run summary show?", "Realm and floor reached, kills, time, materials gathered, recipes unlocked."],
        ["A17", "Confirm the Tusk Sovereign's charge: lane width, telegraph length, cooldown, and the recovery turn.", "2-tile-wide lane, telegraphed for one full round, recovery turn after the charge; phase 2 drops the recovery so it can charge every other turn. Never more than one action per turn."],
        ["A18", "How many runes should a full run deliver?", "10-14 found, so a 3-slot weapon is usually full by the tundra. Runes are build currency, not trophies."],
        ["A19", "What exactly does Ashen Ground do, and does it interact with torches?", "Applies Fire to anything standing on it, persists for the fight, and cannot itself be lit. It does not extinguish torches that are already placed."],
        ["A20", "Do multi-tile bosses block line of sight, or only movement?", "Both. A body that large reading as transparent would make the ranged fights confusing."],
    ])

    # ------------------------------------------------------------ appendix B
    page_break()
    h1("Appendix B - Known issues and code gaps")
    p("Verified against the codebase while writing this document. Each item is a real gap between "
      "the design above and the current build.")
    t(["#", "Gap", "Where"], [
        ["B1", "No run structure: one endless map, exactly 5 rooms, only Starting and Combat room types, no stairs, boss, win state or save.", "Level generator"],
        ["B2", "Mapgen crash when a room has exactly one free direction (index out of range); room count is hardcoded to 5.", "ProceduralGeneraltionAlgorithm"],
        ["B3", "The game is turn-based everywhere; free movement outside combat does not exist.", "Controls / TurnManager"],
        ["B4", "Blocked moves consume a turn (the turn event fires before validation), and entering aim mode triggers an enemy turn without locking input, which can start a second concurrent enemy-turn coroutine.", "Controls, TurnManager"],
        ["B5", "Enemy awareness is island-based only; detection radius is dead code; unaware enemies never move, so roaming does not exist.", "EnemyBehavior"],
        ["B6", "Line of sight is a no-op: the check tests a tilemap nothing paints, so both sides shoot through walls and rocks.", "RangeAttackTilemap, RangeEnemyBehavior"],
        ["B7", "Damage-over-time kills must use the normal death path or they bypass drops and leave the enemy registered in the turn queue.", "To build with the status system"],
        ["B8", "No unequip, and equipped items stay in the backpack, so an item is both equipped and carried.", "Equipments, Inventory"],
        ["B9", "Only item and player stat bonuses exist; on-hit and utility rune effects need new effect types.", "InscriptionEffect"],
        ["B10", "No material is obtainable at all, so both recipes and the arrow economy are dead ends.", "LootGen, chest pools"],
        ["B11", "Asset text contradicts data: Strength Elixir says 50 turns but is 10; Constitution Pill maps to MaxHealth; Pig Yao Skin and Tusk both describe themselves as 'Iron'.", "ItemDefinitions"],
        ["B12", "Inventory overflow destroys items instead of dropping them.", "Inventory, LootGen, Chest"],
        ["B13", "No status effect system of any kind exists.", "To build"],
        ["B14", "Armour only adds MaxHealth, and there is no unequip or swapping logic, so the armour rules in 8.5 cannot be verified yet.", "Equipments, PlayerStats"],
        ["B15", "Level-up screen has dangling listener entries pointing at null targets alongside the real handler, and a free heal call on the health button.", "Branching dungeon.unity, levelUpUI"],
        ["B16", "No rarity field on items, and every epic loot pool is empty, so the 1% epic roll yields nothing.", "LootGen, scenes"],
        ["B17", "Unused leftovers: Spider Yao prefab referenced by nothing, orphaned gameHUD scene, empty CastVisualizer folder, achievement art with no code.", "Assets"],
        ["B18", "The 'sword drawn' flag is always true and never modified; the real melee gate is having a melee weapon equipped.", "Controls"],
        ["B19", "Six correctness bugs documented but unfixed in the refactoring plan (flower density, combat inside MoveEntity, branching-room range errors, ranged cooldown going negative, hardcoded death scene index, string-based coroutine start).", "See Docs/Refactoring-Plan.md"],
        ["B20", "Documentation drift: ItemSystem.md points at a folder that does not exist and its data tables are stale; InscriptionSystem.md is superseded by shipped code.", "Docs/"],
        ["B21", "Multi-tile bosses do not exist. Entity, movement, collision, line of sight and the enemy turn queue all assume one entity per cell.", "EntityManager, Grid, EnemyBehaviour"],
        ["B22", "Hazard terrain (slippery ice, crevasses, rubble, Ashen Ground) has no representation; terrain codes 2 and 5 are unused legacy values with no behaviour.", "Grid terrain codes"],
        ["B23", "The build has never shipped for macOS. Input, fullscreen, save paths and the build pipeline need verifying on Apple Silicon.", "Build pipeline"],
    ])

    # ------------------------------------------------------------ appendix C
    page_break()
    h1("Appendix C - Changes from the original GDD")
    t(["Original GDD", "This document", "Why"], [
        ["Sections described only as 'section 2 = Dusk', 'section 3 = Midnight'", "Three realms (plains, desert, tundra), each with its own visibility gimmick", "Designer decision: biomes are independent, not a time-of-day ladder"],
        ["No run structure stated", "3 realms x 2-3 floors, 6-8 islands per floor, one-way descent, 30-45 minute runs", "Needed to be buildable; pacing measured in testing"],
        ["'Fog of war' with discovered areas faintly lit, then fully dark with torches", "Desert: static reduced vision. Tundra: 3-tile vision plus permanent torches", "Simpler, testable, and biome-specific"],
        ["Engravings applied to armour/weapons", "Inscriptions (runes), max 3 slots scaling with tier", "Matches shipped code; naming confirmed by the designer"],
        ["Weapons differ by damage", "Weapons differ by rune slots and traits first, damage second", "Designer decision: avoid boring damage upgrades"],
        ["Status buffs listed as Fire, Poison, Enfeeblement, Explosive", "Same four plus Root; 3-turn damage over time, 5-8 turn buffs; bosses immune to skip", "Root added for the shipped web obstacle; durations resolved against code"],
        ["Movement: moving a tile takes one turn, always", "Free movement outside combat, one action per turn in combat", "Designer decision, matches the original intent for empty rooms"],
        ["Pills last 3-5 turns", "Buffs last 5-8 turns (6 proposed); damage over time lasts 3", "Resolved against the shipped 10-turn value"],
        ["Crafting unlocked by picking up a specific part", "Material discovery in-run plus permanent blueprint unlocks from chests and bosses", "Designer decision: blueprint meta progression"],
        ["Week-by-week timeline dated May 2026", "Milestone phases M1-M3 with no dates", "The dated table was already stale and had placeholder tasks"],
        ["Armour heals to max only if at full health on the equip turn", "Armour adds MaxHealth, never heals", "Original rule was confusing and created an equip-swap exploit"],
        ["'Boss islands will always be between 9 and 12'", "Boss arenas are 15x15 to 19x19 and open", "Bosses occupy several tiles and need charge lanes, so the old cap is too small"],
        ["Bosses implied to be single-tile like other enemies", "Bosses are multi-tile: 2x2, a 3-tile body, and 3x3", "Designer correction: a boss should be visibly bigger than one tile"],
        ["Loot was the source of weapons and armour", "Chests and enemies never drop equipment; it is crafted. Boss signature weapons are the only dropped equipment", "Designer correction: equipment drops diluted the crafting loop"],
        ["Rune supply unspecified", "Roughly one chest in three holds a rune; a run should deliver 10-14 runes", "Designer correction: runes must not be so rare that a run ends with empty slots"],
        ["No platform stated", "Windows and macOS, both first-class", "Designer correction"],
        ["Boss actions unspecified (the first draft allowed two charges per round)", "Every boss takes exactly one action per turn, with telegraphed lanes, rings and hazards doing the pressure instead", "Two actions per round contradicts the game's own turn system"],
    ])
    p()
    note("End of document. Generated from an interview with the designer plus a survey of the "
         "existing codebase; all decisions are traceable to one of the two.")
