"""GDD v2 content, part 1: overview, setting, run structure, biomes, level generation."""
from helpers import h1, h2, h3, p, b, n, note, t, kv, tag, page_break


def build():
    # ------------------------------------------------------------------ front
    h1("How to read this document")
    p("This is a full replacement for the original mind-map GDD (Immortal Ascent Overhaul.pdf). "
      "It records the decisions taken in a structured interview with the designer, plus the "
      "content needed to actually build each system.")
    b("Decision - a rule the designer has confirmed. Treat these as the spec.")
    b("Proposed - a starting number or piece of content written to make the system buildable. "
      "Not balance; expect to change it after playtesting.")
    b("Open - still undecided. Every open item is collected in Appendix A with a recommendation.")
    b("Gap - something the current build does not do yet, or does incorrectly. Collected in Appendix B.")
    note("Sections are written so a programmer can implement from them without asking follow-up "
         "questions, and so an artist can see exactly what content is required. All player-facing "
         "strings are written as localizable text; see Section 2.6.")

    # ------------------------------------------------------------- 1 overview
    page_break()
    h1("1. Overview")
    h2("1.1 High concept")
    p("A turn-based, grid-based roguelike. A cultivator climbs through three realms toward "
      "immortality, clearing enemy-held islands one deliberate action at a time, looting the dead, "
      "forging better weapons, and slotting runes into them to build a character that can survive "
      "the final ascent.")
    h2("1.2 Facts")
    kv([
        ("Genre", "Turn-based tactical roguelike with a room-clear (Binding of Isaac style) map"),
        ("Perspective", "Top-down 2D, 32x32 pixel art"),
        ("Platform", "PC: Windows and macOS, keyboard + mouse"),
        ("Engine", "Unity 2022.3.62f2, URP 2D, Tilemap, TextMeshPro, legacy Input"),
        ("Players", "Single player"),
        ("Run length", "30-45 minutes, start to victory or death"),
        ("Death", "Permadeath. The run resets; permanent unlocks are kept."),
        ("Meta", "Blueprint unlocking plus a journal of everything discovered"),
        ("Team", "Steven (design + code), Dewei (art), Jo (UI)"),
    ])
    h2("1.3 Design pillars")
    n("One action per turn. Movement is a decision, not a formality. Every turn spent walking is a "
      "turn the enemy gets to use.")
    n("Escalating biome hazards. Each realm removes a safety net: plains is readable, the desert "
      "takes your vision, the tundra takes your light and your footing.")
    n("Build through runes, not just numbers. Better weapons earn more rune slots and traits, so an "
      "upgrade opens options instead of only adding damage.")
    n("Readable numbers. Damage is flat, stats are small whole numbers, and the player can always "
      "work out what a fight will cost.")
    n("Legible risk. Chests, ore and stairs are worth a detour, and the player chooses when to "
      "spend turns on them.")

    # -------------------------------------------------------------- 2 setting
    page_break()
    h1("2. Setting and narrative")
    h2("2.1 Premise")
    p("A cultivator attempts the ascent: three realms, three trials, and the gate to immortality at "
      "the summit. Each realm is guarded by a named beast that has already ended other climbers. "
      "The player is not told why they climb; the climb is the answer.")
    h2("2.2 Folklore basis")
    p("Chinese mythology and xianxia, matching what is already built: yao (Pig Yao, Jing Yao, "
      "Spider Yao), huabiao pillars, and immortal/jade imagery. Names are proposed in Section 13; "
      "the designer approves the final wording.")
    h2("2.3 Cast and dialogue")
    b("The protagonist is a silent avatar and is never named in dialogue.")
    b("There are no NPCs and no dialogue system in v1. Story is delivered by environment art, "
      "boss intro lines, and item/rune lore text.")
    b("Each boss gets a name splash and exactly one line of flavour text before the fight.")
    h2("2.4 Tone")
    b("Austere and quiet, not comedic. The horror is scale and inevitability, not gore.")
    b("Item descriptions carry dry humour where the original GDD had it (e.g. the Rusty Sword's "
      "'Why would you use this thing').")
    h2("2.5 Realm identities")
    t(["Realm", "Identity", "What it takes from the player"], [
        ["Plains (Realm of the Wild)", "Water, reeds, huabiao stone, wild yao", "Nothing - this is the readable baseline"],
        ["Desert (Realm of Sand)", "Dunes, ruined walls, buried things", "Vision - a standing sandstorm"],
        ["Tundra (Realm of Frost)", "Open ice field, crevasses, whiteout", "Light and footing"],
    ])
    h2("2.6 Localization")
    b("Launch language: English.")
    b("All player-facing text lives in string tables from the first commit, so a second language can "
      "be added without touching code.")
    b("No text baked into textures or prefabs.")

    # --------------------------------------------------------- 3 run structure
    page_break()
    h1("3. Core loop and run structure")
    h2("3.1 The loop")
    n("Enter a floor. The minimap is blank except for the tile the player stands on.")
    n("Explore islands. Fight what is in them; loot chests, mine ore, take materials off corpses.")
    n("Spend. Level up (5 points), craft from the inventory while out of combat, apply runes at any "
      "time outside combat.")
    n("Find the exit stairs, revealed on the minimap once discovered.")
    n("Descend. Leaving a floor is permanent.")
    n("On the last floor of a realm, clear the boss. Its death opens the gate to the next realm.")
    n("Repeat for the desert and the tundra. Killing the third boss wins the run.")
    h2("3.2 Run structure at a glance")
    t(["Property", "Value"], [
        ["Realms per run", "3 (Plains, Desert, Tundra)"],
        ["Floors per realm", "2-3"],
        ["Floors per run", "6-9"],
        ["Islands per floor", "6-8"],
        ["Boss", "One per realm, on the last floor of that realm"],
        ["Bosses per run", "3; the third ends the run"],
        ["Measured pacing", "2-5 minutes per floor in current testing"],
        ["Target run length", "30-45 minutes"],
        ["Backtracking", "None - floors are one-way"],
    ])
    h2("3.3 Floor exit")
    b("The exit stairs spawn somewhere on the floor at generation time; they are not marked at the start.")
    b("Walking a floor reveals it on the minimap. Once discovered, the stair marker persists.")
    b("The player is never forced to clear the floor; enemies left alive stay behind.")
    b("On the last floor of a realm, the boss room replaces the stairs. The room locks on entry and "
      "the gate opens only when the boss dies.")
    h2("3.4 Permadeath, victory and meta progression")
    b("Death ends the run immediately. The player returns to the menu.")
    b("Victory: kill the third boss (the Ashen Immortal) at the end of the tundra.")
    b("Meta progression is blueprint unlocking: the first time an item is found or crafted, it is "
      "added to the permanent pool and can appear in later runs' loot and crafting lists.")
    b("The blueprint journal (Section 14.2) is the visibility for this system. Without it, unlocks "
      "are invisible.")
    h2("3.5 Saving")
    b("The run autosaves at every floor transition. Quitting mid-floor forfeits progress on that floor.")
    b("Resuming restores: realm and floor index, player stats and level, inventory, equipment with "
      "its runes, unlocked recipes, and remaining HP.")
    b("Permanent unlocks and settings are stored separately from run state (Section 16.3).")
    h2("3.6 Crafting access")
    b("Crafting is portable. The player opens the crafting screen from the inventory while out of "
      "combat - that is, while no living enemy is on their island, and never during the enemy turn.")
    b("There are no crafting stations and no dedicated safe-island type in v1. This supersedes the "
      "earlier 'crafting stations / safe islands' decision.")
    b("Crafting costs no turn because it cannot happen in combat. If combat starts while the crafting "
      "screen is open, it closes.")

    # ---------------------------------------------------------------- 4 biomes
    page_break()
    h1("4. Realms (biomes)")
    p("Each realm changes the map layout, the way enemies notice the player, what the player can see, "
      "and which materials and weapons are available. Difficulty also rises per floor inside a realm "
      "(Section 13.5).")
    t(["", "Plains", "Desert", "Tundra"], [
        ["Layout", "Islands in water, bridges between them", "Open dunes with rock and ruin walls", "Open ice field with crevasses"],
        ["Vision", "Unlimited", "Reduced to 5 tiles (sandstorm)", "Reduced to 3 tiles; torches light 5"],
        ["Aggro", "Room-wide: entering an occupied island alerts all of it", "Radius-based: enemies notice within their detection radius", "Radius-based"],
        ["Standout hazard", "None", "Sandstorm vision loss", "Slippery ice and crevasses"],
        ["Enemies", "Pig Yao, Spider Yao, Jing Yao", "Sand Scorpion, Desiccated Monk, Sand Wraith", "Frost Wolf, Snow Yao, Ice Wraith"],
        ["Materials", "Iron Ore, Pig Yao Tusk, Pig Yao Skin, Spider Silk", "Bronze Ore, Scorpion Carapace, Venom Sac", "Frost Iron Ore, Wolf Pelt, Everfrost Crystal"],
        ["Craftable weapons", "Iron Sword, Composite Bow", "Bronze Sword, Recurve Bow", "Frost Iron Sword, Longbow"],
        ["Boss", "The Tusk Sovereign", "Shen, the Mirage Serpent", "The Ashen Immortal"],
    ], widths=[1.1, 1.8, 1.8, 1.8])

    h2("4.1 Plains - Realm of the Wild")
    b("Realm 1, floors 1-2 (proposed: 2 floors). No visibility gimmick; this is where the player "
      "learns the turn economy, runes and crafting.")
    b("Layout: islands in water joined by narrow bridges, exactly as the current generator produces.")
    b("Aggro is room-wide, which makes the island boundary the combat boundary.")
    b("Threats: melee brutes that close distance, ranged yao that punish standing still, and spiders "
      "whose webs Root the player.")

    h2("4.2 Desert - Realm of Sand")
    b("Realm 2. A sandstorm holds vision at 5 tiles for the entire floor. The minimap keeps what the "
      "player has already seen, so exploring is how the map gets made.")
    b("Layout: an open field broken up by rock outcrops and ruined walls. Ruins create partial "
      "cover and short sight lines, not sealed rooms.")
    b("Aggro is radius-based, so the player can walk around an enemy they have not been noticed by.")
    b("Threats: poisoners, slow armoured bruisers, and ranged spirits that shoot out of the storm.")
    note("Open item A6: whether the sandstorm also muffles sound (i.e. enemies notice the player "
         "later) or purely limits vision.")

    h2("4.3 Tundra - Realm of Frost")
    b("Realm 3, the final realm. Base vision 3 tiles; placed torches light a 5-tile radius "
      "permanently. The floor is an open field, not a cave.")
    b("Layout: open snowfield with two ground hazards - slippery ice and crevasses.")
    b("Slippery ice: stepping onto an ice tile forces an extra tile of slide in the same direction. "
      "If the slide is blocked, the player stops (no damage). Slide never triggers an attack.")
    b("Crevasses: impassable holes in the field. Falling is not possible; they are walls the player "
      "can see through, which makes ranged enemies behind a crevasse dangerous.")
    b("Torches are the counterplay to both the darkness and the layout: the player chooses where "
      "light exists for the rest of the floor.")
    b("Aggro is radius-based.")

    note("Open item A7: whether ice slides enemies as well as the player (recommended: yes, which "
         "makes ice a battlefield the player can exploit).")

    # ------------------------------------------------------ 5 level generation
    page_break()
    h1("5. Level generation")
    h2("5.1 Grid and terrain")
    kv([
        ("Grid", "120 x 120 cells (current scene value), 1 cell = 1 world unit, origin (0,0)"),
        ("Terrain 0", "Floor and corridor - walkable"),
        ("Terrain 1", "Wall, water, out of bounds - blocked"),
        ("Terrain 5", "Bridge - legacy, still treated as walkable; nothing writes it"),
        ("Terrain 99", "Unopened chest - blocked until opened"),
        ("Cells", "Entities stay on integer coordinates; cell size is 1"),
    ])
    h2("5.2 Island sizes")
    t(["Island class", "Size", "Notes"], [
        ["Smallest", "3x3", "One enemy or one reward; used as connective filler"],
        ["Average", "6x6 to 8x8", "The bulk of a floor"],
        ["Largest", "9x9 to 12x12", "Used for dense encounters"],
        ["Boss arena", "15x15 to 19x19", "Open, no interior walls. Bigger than the original GDD's 9-12 because bosses occupy several tiles and need charge room"],
    ])
    h2("5.3 Island and room types")
    t(["Type", "Contents", "v1 priority"], [
        ["Starting", "Player spawn, no enemies", "Required"],
        ["Combat", "1-2 enemies (scaled per floor); may hold a chest or ore", "Required"],
        ["Stairs", "Exit stairs, plus possible chest or ore", "Required"],
        ["Boss arena", "One multi-tile boss; the room locks on entry", "Required"],
        ["Treasure", "Chest, no enemies or light enemies", "Nice to have; can be folded into Combat"],
    ])
    note("Current build only ever assigns 'Starting' and 'Combat', and always generates exactly 5 "
         "rooms. Gap B1.")
    h2("5.4 Obstacles")
    t(["Obstacle", "Effect", "Status"], [
        ["Bushes", "Block movement; hide the player (and enemies) behind them", "Art partially exists"],
        ["Rocks", "Block movement; block ranged line of sight", "Planned"],
        ["Huabiao pillar", "Block movement; tall - always blocks line of sight", "Planned"],
        ["Spider web", "Roots the player for 2 turns when walked into, then is destroyed", "Planned"],
    ])
    note("Decision: Fire does not spread to obstacles or terrain. Burning bushes, spreading grass "
         "fires and destructible terrain are explicitly out of scope for v1.")
    h2("5.5 Chests, ore and stairs")
    kv([
        ("Chests", "1-2 per floor. Open with E from an adjacent cell. Free action. Contents rolled at generation."),
        ("Ore veins", "2-4 per floor (proposed). Mined with E from an adjacent cell; costs one turn. Yields 1-3 ore of the realm's metal."),
        ("Exit stairs", "Exactly one per non-boss floor, on a non-starting island. Revealed on the minimap when discovered."),
        ("Boss gate", "Replaces the stairs on a realm's last floor. Opens when the boss dies."),
    ])
    h2("5.6 Generation rules that must hold")
    n("Every walkable cell is reachable from the player spawn (flood fill). Already implemented and "
      "must stay implemented.")
    n("Every chest, ore vein and the exit stairs have at least one reachable adjacent cell.")
    n("No enemy spawns in the starting island, and no two entities share a cell.")
    n("The boss island spawns only on the last floor of a realm and contains no other enemies.")
    n("Island count per floor is 6-8, excluding the boss island.")
    note("Gap B2: the current generator can crash when a room has exactly one free direction "
         "(index out of range), and always produces exactly 5 rooms. Both must be fixed before "
         "floors can be sized per realm.")
