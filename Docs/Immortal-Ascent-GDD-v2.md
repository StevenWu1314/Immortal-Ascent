---
title: Immortal Ascent
subtitle: Game Design Document v2 - Rogue Asian Fantasy
version: "2.0 (draft)"
platform: [Windows, macOS]
engine: Unity 2022.3.62f2
genre: turn-based tactical roguelike, room-clear map, permadeath
run_length: 30-45 minutes
realms: [Plains, Desert, Tundra]
floors_per_realm: 2-3
islands_per_floor: 6-8
bosses: 3
win_condition: defeat the Ashen Immortal at the end of the Tundra
meta_progression: blueprint unlocking, persisted in JSON
status: decisions locked; every value marked "proposed" still needs playtesting
conventions: decisions are "Decision"; unverified starting numbers are "proposed"; unresolved items are "Open" and collected in Appendix A; code gaps are "Gap" and collected in Appendix B
---

# Immortal Ascent

Game Design Document v2 - Rogue Asian Fantasy

Draft for team review

This document is a full replacement for the original mind-map GDD (Immortal Ascent Overhaul.pdf). It contains the design as decided in a structured interview with the designer, written out in enough detail to implement from, plus two appendices covering what is still undecided and what the current build does not yet do.

| Field | Value |
| --- | --- |
| Version | 2.0 (draft) |
| Date | 2026-10-15 |
| Prepared for | Steven (design + code), Dewei (art), Jo (UI) |
| Source | Immortal Ascent Overhaul.pdf, Docs/ItemSystem.md, Docs/InscriptionSystem.md, Docs/Refactoring-Plan.md, and a read-only survey of Assets/Scripts |
| Project | Rogue Asian Fantasy, Unity 2022.3.62f2 |
| Status | Decisions locked; values marked 'proposed' need playtesting |

## How to read this document

This is a full replacement for the original mind-map GDD (Immortal Ascent Overhaul.pdf). It records the decisions taken in a structured interview with the designer, plus the content needed to actually build each system.

- Decision - a rule the designer has confirmed. Treat these as the spec.
- Proposed - a starting number or piece of content written to make the system buildable. Not balance; expect to change it after playtesting.
- Open - still undecided. Every open item is collected in Appendix A with a recommendation.
- Gap - something the current build does not do yet, or does incorrectly. Collected in Appendix B.

> Sections are written so a programmer can implement from them without asking follow-up questions, and so an artist can see exactly what content is required. All player-facing strings are written as localizable text; see Section 2.6.

## 1. Overview

### 1.1 High concept

A turn-based, grid-based roguelike. A cultivator climbs through three realms toward immortality, clearing enemy-held islands one deliberate action at a time, looting the dead, forging better weapons, and slotting runes into them to build a character that can survive the final ascent.

### 1.2 Facts

| Field | Value |
| --- | --- |
| Genre | Turn-based tactical roguelike with a room-clear (Binding of Isaac style) map |
| Perspective | Top-down 2D, 32x32 pixel art |
| Platform | PC: Windows and macOS, keyboard + mouse |
| Engine | Unity 2022.3.62f2, URP 2D, Tilemap, TextMeshPro, legacy Input |
| Players | Single player |
| Run length | 30-45 minutes, start to victory or death |
| Death | Permadeath. The run resets; permanent unlocks are kept. |
| Meta | Blueprint unlocking plus a journal of everything discovered |
| Team | Steven (design + code), Dewei (art), Jo (UI) |

### 1.3 Design pillars

1. One action per turn. Movement is a decision, not a formality. Every turn spent walking is a turn the enemy gets to use.
2. Escalating biome hazards. Each realm removes a safety net: plains is readable, the desert takes your vision, the tundra takes your light and your footing.
3. Build through runes, not just numbers. Better weapons earn more rune slots and traits, so an upgrade opens options instead of only adding damage.
4. Readable numbers. Damage is flat, stats are small whole numbers, and the player can always work out what a fight will cost.
5. Legible risk. Chests, ore and stairs are worth a detour, and the player chooses when to spend turns on them.

## 2. Setting and narrative

### 2.1 Premise

A cultivator attempts the ascent: three realms, three trials, and the gate to immortality at the summit. Each realm is guarded by a named beast that has already ended other climbers. The player is not told why they climb; the climb is the answer.

### 2.2 Folklore basis

Chinese mythology and xianxia, matching what is already built: yao (Pig Yao, Jing Yao, Spider Yao), huabiao pillars, and immortal/jade imagery. Names are proposed in Section 13; the designer approves the final wording.

### 2.3 Cast and dialogue

- The protagonist is a silent avatar and is never named in dialogue.
- There are no NPCs and no dialogue system in v1. Story is delivered by environment art, boss intro lines, and item/rune lore text.
- Each boss gets a name splash and exactly one line of flavour text before the fight.

### 2.4 Tone

- Austere and quiet, not comedic. The horror is scale and inevitability, not gore.
- Item descriptions carry dry humour where the original GDD had it (e.g. the Rusty Sword's 'Why would you use this thing').

### 2.5 Realm identities

| Realm | Identity | What it takes from the player |
| --- | --- | --- |
| Plains (Realm of the Wild) | Water, reeds, huabiao stone, wild yao | Nothing - this is the readable baseline |
| Desert (Realm of Sand) | Dunes, ruined walls, buried things | Vision - a standing sandstorm |
| Tundra (Realm of Frost) | Open ice field, crevasses, whiteout | Light and footing |

### 2.6 Localization

- Launch language: English.
- All player-facing text lives in string tables from the first commit, so a second language can be added without touching code.
- No text baked into textures or prefabs.

## 3. Core loop and run structure

### 3.1 The loop

1. Enter a floor. The minimap is blank except for the tile the player stands on.
2. Explore islands. Fight what is in them; loot chests, mine ore, take materials off corpses.
3. Spend. Level up (5 points), craft from the inventory while out of combat, apply runes at any time outside combat.
4. Find the exit stairs, revealed on the minimap once discovered.
5. Descend. Leaving a floor is permanent.
6. On the last floor of a realm, clear the boss. Its death opens the gate to the next realm.
7. Repeat for the desert and the tundra. Killing the third boss wins the run.

### 3.2 Run structure at a glance

| Property | Value |
| --- | --- |
| Realms per run | 3 (Plains, Desert, Tundra) |
| Floors per realm | 2-3 |
| Floors per run | 6-9 |
| Islands per floor | 6-8 |
| Boss | One per realm, on the last floor of that realm |
| Bosses per run | 3; the third ends the run |
| Measured pacing | 2-5 minutes per floor in current testing |
| Target run length | 30-45 minutes |
| Backtracking | None - floors are one-way |

### 3.3 Floor exit

- The exit stairs spawn somewhere on the floor at generation time; they are not marked at the start.
- Walking a floor reveals it on the minimap. Once discovered, the stair marker persists.
- The player is never forced to clear the floor; enemies left alive stay behind.
- On the last floor of a realm, the boss room replaces the stairs. The room locks on entry and the gate opens only when the boss dies.

### 3.4 Permadeath, victory and meta progression

- Death ends the run immediately. The player returns to the menu.
- Victory: kill the third boss (the Ashen Immortal) at the end of the tundra.
- Meta progression is blueprint unlocking: the first time an item is found or crafted, it is added to the permanent pool and can appear in later runs' loot and crafting lists.
- The blueprint journal (Section 14.2) is the visibility for this system. Without it, unlocks are invisible.

### 3.5 Saving

- The run autosaves at every floor transition. Quitting mid-floor forfeits progress on that floor.
- Resuming restores: realm and floor index, player stats and level, inventory, equipment with its runes, unlocked recipes, and remaining HP.
- Permanent unlocks and settings are stored separately from run state (Section 16.3).

### 3.6 Crafting access

- Crafting is portable. The player opens the crafting screen from the inventory while out of combat - that is, while no living enemy is on their island, and never during the enemy turn.
- There are no crafting stations and no dedicated safe-island type in v1. This supersedes the earlier 'crafting stations / safe islands' decision.
- Crafting costs no turn because it cannot happen in combat. If combat starts while the crafting screen is open, it closes.

## 4. Realms (biomes)

Each realm changes the map layout, the way enemies notice the player, what the player can see, and which materials and weapons are available. Difficulty also rises per floor inside a realm (Section 13.5).

|  | Plains | Desert | Tundra |
| --- | --- | --- | --- |
| Layout | Islands in water, bridges between them | Open dunes with rock and ruin walls | Open ice field with crevasses |
| Vision | Unlimited | Reduced to 5 tiles (sandstorm) | Reduced to 3 tiles; torches light 5 |
| Aggro | Room-wide: entering an occupied island alerts all of it | Radius-based: enemies notice within their detection radius | Radius-based |
| Standout hazard | None | Sandstorm vision loss | Slippery ice and crevasses |
| Enemies | Pig Yao, Spider Yao, Jing Yao | Sand Scorpion, Desiccated Monk, Sand Wraith | Frost Wolf, Snow Yao, Ice Wraith |
| Materials | Iron Ore, Pig Yao Tusk, Pig Yao Skin, Spider Silk | Bronze Ore, Scorpion Carapace, Venom Sac | Frost Iron Ore, Wolf Pelt, Everfrost Crystal |
| Craftable weapons | Iron Sword, Composite Bow | Bronze Sword, Recurve Bow | Frost Iron Sword, Longbow |
| Boss | The Tusk Sovereign | Shen, the Mirage Serpent | The Ashen Immortal |

### 4.1 Plains - Realm of the Wild

- Realm 1, floors 1-2 (proposed: 2 floors). No visibility gimmick; this is where the player learns the turn economy, runes and crafting.
- Layout: islands in water joined by narrow bridges, exactly as the current generator produces.
- Aggro is room-wide, which makes the island boundary the combat boundary.
- Threats: melee brutes that close distance, ranged yao that punish standing still, and spiders whose webs Root the player.

### 4.2 Desert - Realm of Sand

- Realm 2. A sandstorm holds vision at 5 tiles for the entire floor. The minimap keeps what the player has already seen, so exploring is how the map gets made.
- Layout: an open field broken up by rock outcrops and ruined walls. Ruins create partial cover and short sight lines, not sealed rooms.
- Aggro is radius-based, so the player can walk around an enemy they have not been noticed by.
- Threats: poisoners, slow armoured bruisers, and ranged spirits that shoot out of the storm.

> Open item A6: whether the sandstorm also muffles sound (i.e. enemies notice the player later) or purely limits vision.

### 4.3 Tundra - Realm of Frost

- Realm 3, the final realm. Base vision 3 tiles; placed torches light a 5-tile radius permanently. The floor is an open field, not a cave.
- Layout: open snowfield with two ground hazards - slippery ice and crevasses.
- Slippery ice: stepping onto an ice tile forces an extra tile of slide in the same direction. If the slide is blocked, the player stops (no damage). Slide never triggers an attack.
- Crevasses: impassable holes in the field. Falling is not possible; they are walls the player can see through, which makes ranged enemies behind a crevasse dangerous.
- Torches are the counterplay to both the darkness and the layout: the player chooses where light exists for the rest of the floor.
- Aggro is radius-based.

> Open item A7: whether ice slides enemies as well as the player (recommended: yes, which makes ice a battlefield the player can exploit).

## 5. Level generation

### 5.1 Grid and terrain

| Field | Value |
| --- | --- |
| Grid | 120 x 120 cells (current scene value), 1 cell = 1 world unit, origin (0,0) |
| Terrain 0 | Floor and corridor - walkable |
| Terrain 1 | Wall, water, out of bounds - blocked |
| Terrain 5 | Bridge - legacy, still treated as walkable; nothing writes it |
| Terrain 99 | Unopened chest - blocked until opened |
| Cells | Entities stay on integer coordinates; cell size is 1 |

### 5.2 Island sizes

| Island class | Size | Notes |
| --- | --- | --- |
| Smallest | 3x3 | One enemy or one reward; used as connective filler |
| Average | 6x6 to 8x8 | The bulk of a floor |
| Largest | 9x9 to 12x12 | Used for dense encounters |
| Boss arena | 15x15 to 19x19 | Open, no interior walls. Bigger than the original GDD's 9-12 because bosses occupy several tiles and need charge room |

### 5.3 Island and room types

| Type | Contents | v1 priority |
| --- | --- | --- |
| Starting | Player spawn, no enemies | Required |
| Combat | 1-2 enemies (scaled per floor); may hold a chest or ore | Required |
| Stairs | Exit stairs, plus possible chest or ore | Required |
| Boss arena | One multi-tile boss; the room locks on entry | Required |
| Treasure | Chest, no enemies or light enemies | Nice to have; can be folded into Combat |

> Current build only ever assigns 'Starting' and 'Combat', and always generates exactly 5 rooms. Gap B1.

### 5.4 Obstacles

| Obstacle | Effect | Status |
| --- | --- | --- |
| Bushes | Block movement; hide the player (and enemies) behind them | Art partially exists |
| Rocks | Block movement; block ranged line of sight | Planned |
| Huabiao pillar | Block movement; tall - always blocks line of sight | Planned |
| Spider web | Roots the player for 2 turns when walked into, then is destroyed | Planned |

> Decision: Fire does not spread to obstacles or terrain. Burning bushes, spreading grass fires and destructible terrain are explicitly out of scope for v1.

### 5.5 Chests, ore and stairs

| Field | Value |
| --- | --- |
| Chests | 1-2 per floor. Open with E from an adjacent cell. Free action. Contents rolled at generation. |
| Ore veins | 2-4 per floor (proposed). Mined with E from an adjacent cell; costs one turn. Yields 1-3 ore of the realm's metal. |
| Exit stairs | Exactly one per non-boss floor, on a non-starting island. Revealed on the minimap when discovered. |
| Boss gate | Replaces the stairs on a realm's last floor. Opens when the boss dies. |

### 5.6 Generation rules that must hold

1. Every walkable cell is reachable from the player spawn (flood fill). Already implemented and must stay implemented.
2. Every chest, ore vein and the exit stairs have at least one reachable adjacent cell.
3. No enemy spawns in the starting island, and no two entities share a cell.
4. The boss island spawns only on the last floor of a realm and contains no other enemies.
5. Island count per floor is 6-8, excluding the boss island.

> Gap B2: the current generator can crash when a room has exactly one free direction (index out of range), and always produces exactly 5 rooms. Both must be fixed before floors can be sized per realm.

## 6. Turn system and action economy

### 6.1 When the game is turn-based

- Free movement: while no living enemy shares the player's island, the player walks freely. No turns are spent and enemies do not act.
- Turn-based: the moment the player is on an island with at least one living enemy, every input becomes a turn.
- Room lock: while enemies are alive on the island, the player cannot leave it. Corridors and empty islands are never locked. Entering an occupied island is always allowed.
- The lock releases the instant the last enemy on the island dies, including from a damage-over-time tick.

> Current build is turn-based everywhere and only gates the island boundary. Gap B3.

### 6.2 One action per turn

Each turn the player takes exactly one of the following. There is no move-then-attack.

| Action | Cost | Notes |
| --- | --- | --- |
| Move one tile | 1 turn | Orthogonal only; no diagonal movement |
| Attack with melee | 1 turn | Bump into an adjacent enemy, or attack with the sword drawn |
| Fire the bow | 1 turn + 1 arrow | Requires aim mode; see 8.3 |
| Use an item | 1 turn | Pills and cleanses only; equipping and runes are free |
| Place a torch | 1 turn | Tundra only; light is permanent |
| Mine ore | 1 turn | From an adjacent cell, with E |
| Open a chest | Free | From an adjacent cell, with E. Explicitly not a turn |
| Equip, apply runes, craft, level up, open UI | Free | Out of combat only for crafting |

### 6.3 Failed actions

- Bumping a wall, a crevasse, water or a locked gate costs nothing. The player keeps their turn.
- Bumping into an enemy is an attack, and therefore costs the turn.
- Aim mode: entering and leaving it is free and never triggers an enemy turn. Only the shot costs a turn.

> Current build consumes a turn before validating the move, so a wall bump hands the enemies a free turn, and entering aim mode fires the turn event without locking input. Both are fixed by this rule. Gap B4.

### 6.4 Turn order

1. The player acts. Damage, status application and pickups resolve immediately.
2. The enemy turn runs. Every enemy that is aware of the player takes its action in queue order.
3. End of round: damage-over-time ticks, then status durations decrease by one turn.

- Bosses obey the turn system exactly: one action per turn. No enemy, boss included, ever takes two actions in a round, and no boss action is ever free.
- Enemies act one at a time with a short visible step between them, so the player can read what is happening (current pacing: ~0.15s before the enemy phase, ~0.05s between enemies; keep both tunable in the Inspector).
- The player cannot act during the enemy turn. The input lock must be released reliably even if an enemy dies mid-phase.

### 6.5 Who acts

- An enemy acts only if it is aware of the player (it has noticed them). Unaware enemies run their roaming behaviour instead.
- Awareness, plains: the player entering the enemy's island alerts every enemy on it.
- Awareness, desert and tundra: an enemy notices the player inside its detection radius, with line of sight. Awareness is not shared - one enemy seeing the player does not alert the others.
- Once aware, an enemy stays aware until it loses the player and returns to its post, after which it becomes unaware again.

> Current build: alerting is purely island-based, detection radius is dead code, and unaware enemies never move at all. Gap B5.

### 6.6 Roaming

- Unaware enemies wander inside their island: one tile per round, never leaving the island, never stepping onto another entity.
- Roaming never triggers an attack and never opens a door.
- Roaming stops the moment the enemy becomes aware of the player.
- Enemies never leave their island, whether roaming or chasing. A stranded enemy walks home.

### 6.7 Line of sight

- Ranged attacks, both the player's and the enemy's, require a clear line of sight.
- Blockers: rocks, huabiao pillars, walls and the water surrounding islands block shots.
- Crevasses do not block line of sight, which is what makes the tundra dangerous.
- The aim overlay only highlights tiles the player can actually hit.

> Current build has no working line of sight: the collision check tests a tilemap that nothing paints, so both sides shoot through walls. Gap B6.

## 7. Player stats and progression

### 7.1 Stats

| Stat | Effect | Starting value |
| --- | --- | --- |
| Strength | Added to melee damage; gates melee weapon requirements | 0 |
| Dexterity | Added to ranged damage; gates bow requirements | 0 |
| MaxHealth | Health pool; the player dies at 0 | 100 |
| Health | Current health | 100 |

There is no Defence, no speed, no critical chance and no hit chance. Every attack that is fired in range and in line of sight hits.

- Melee damage = Strength + equipped melee weapon damage.
- Ranged damage = Dexterity + equipped ranged weapon damage.

### 7.2 Level and experience

| Field | Value |
| --- | --- |
| Starting level | 1 |
| Points per level | 5 |
| Point options | +1 Strength, +1 Dexterity, or +10 MaxHealth |
| Experience source | Enemy kills only. Bosses give a large fixed amount. |
| Curve | Level 1 needs 10 XP; each level's requirement is 1.5x the previous, rounded down |
| Level cap | None in practice (current cap 100 is never reached in a run) |
| On level up | Full heal, all status effects cleared, the game pauses for allocation |

> Proposed, not balance: the 1.5x curve plus 5 points per level produces roughly 12-18 levels in a 6-9 floor run, which is 60-90 stat points. Because damage is flat additive, stat points will outgrow weapon damage in the late run. Appendix A item A2 covers the options (raise enemy health faster, cut points per level, or make weapon tiers scale with the player).

### 7.3 Healing

- A level up fully heals the player.
- Health pills heal a fixed amount (proposed 25).
- There is no passive regeneration and no healing between floors.
- The player's health carries over between floors, including into boss fights.

### 7.4 Requirements

- Every craftable weapon has a minimum Strength (melee) or Dexterity (bow) requirement.
- A weapon the player cannot yet use is shown greyed out with the requirement in red, both in the crafting list and in the inventory.
- Requirements are proposed per weapon in Section 10.3.

### 7.5 Death

- At 0 health the run ends. The player returns to the menu.
- No extra life, no revival item, no checkpoint inside a floor.
- The death screen shows the run summary (Section 14.4).

## 8. Combat

### 8.1 Melee

- The player attacks by bumping into an adjacent enemy while a melee weapon is equipped.
- Melee is orthogonal only, single target, unless the weapon has the Cleave trait.
- Enemies attack by bumping into the player's cell.

### 8.2 Weapon swapping

- Key 1 draws the sword, key 2 draws the bow or enters aim mode. Swapping is free and does not cost a turn.
- The equipped melee weapon and the equipped bow are two separate slots, so both are always available.

### 8.3 Ranged and aim mode

1. Press 2. Aim mode opens and highlights every tile the bow can hit: within the bow's range, in line of sight. Entering aim mode is free.
2. Move the mouse to choose a tile. Left click on a tile containing an enemy fires.
3. Firing costs one turn and one arrow and resolves damage immediately.
4. Clicking anywhere else cancels with no cost. Right click leaves aim mode.

- Range is measured as a circle (Euclidean) from the player, matching the current overlay.
- Bows never misfire and never damage the player unless the weapon or rune has the Explosive property (Section 9.6).

### 8.4 Enemy attack patterns

| Pattern | Behaviour |
| --- | --- |
| Melee chase | Walks toward the player, one tile per round, and attacks on contact |
| Ranged telegraph | Marks the tile it will fire at, then fires on its next turn. Moving out of the marked tile avoids the shot entirely. Cooldown 2 rounds between shots. |
| Ranged strafe | If it cannot shoot, it steps sideways to find a line instead of standing still |
| Roamer | Wanders until aware, then behaves as its pattern dictates |

- The player sees the telegraphed tile highlighted in low-opacity red; this is the GDD's original art requirement and it is already implemented.

### 8.5 Armour

- Armour occupies one slot and adds to MaxHealth only. There is no damage reduction.
- Equipping armour does not heal the player, and does not change current health. Swapping armour removes the old piece's bonus first, so bonuses never stack.
- The original GDD's conditional 'heals to max only if you were at full health' rule is dropped as confusing.

### 8.6 Damage numbers

| Source | Formula | Example |
| --- | --- | --- |
| Player melee | Strength + melee weapon damage + rune item bonuses | 0 + 15 = 15, doubled later by runes |
| Player ranged | Dexterity + bow damage + rune item bonuses | 0 + 10 = 10 |
| Poisoned target | Incoming damage x1.25, rounded down | 15 -> 18 |
| Enemy melee | Flat value from the enemy's stat block | 10 |
| Enemy ranged | Flat value from the enemy's stat block | 12 |
| Explosive splash | 50% of the triggering attack's damage, per affected entity | 15 -> 7 |

## 9. Status effects

This is the largest missing system in the current build: no status effect of any kind exists. The five below are the full set for v1.

### 9.1 The five statuses

| Status | Effect | Duration (proposed) | Notes |
| --- | --- | --- | --- |
| Fire | 5 damage at the end of each round | 3 turns | Damage over time |
| Poison | 2 damage at the end of each round, and the target takes +25% damage from all sources | 3 turns | The +25% is the important half |
| Enfeeblement | -50% attack damage, and skips its turn while active | 3 turns | Bosses are immune to the skip; they still take the damage penalty |
| Root | Cannot move. Can still attack if already in range | 2 turns | Webs apply Root too |
| Explosive | Instant, not a duration. Splash damage in a 1-tile radius around the impact | Instant | See 9.6 |

> The GDD's original four were Fire, Poison, Enfeeblement and Explosive; Root was added because the shipped web obstacle needed a mechanical consequence. Any further statuses are out of scope for v1.

### 9.2 How statuses are applied

| Vector | Applies | Notes |
| --- | --- | --- |
| Weapon traits | Bleed-style effects and status on hit | Section 10.2 |
| Runes | On-hit status from the rune slotted in the weapon that attacks | Section 11 |
| Consumables | Pills, including cleanse pills | Section 12.6 |
| Enemies | Their own attacks can inflict statuses on the player | Section 13 |

### 9.3 Stacking

- Re-applying a status refreshes its duration to full. Magnitude never stacks.
- If a stronger and a weaker application land on the same target, the stronger magnitude is kept and the duration refreshes.
- Identical statuses from different sources are still one status.

### 9.4 Tick timing

- Damage over time resolves at the end of the round, after every enemy has acted.
- Durations decrease once per round, after the tick. A 3-turn Fire therefore ticks 3 times.
- A damage-over-time kill credits the player: it grants experience and rolls loot exactly as a direct kill does.

> Gap B7: damage-over-time kills must go through the same death path as normal kills, or they bypass drops and leave the dead enemy registered in the turn queue.

### 9.5 Player statuses and cleansing

- Enemies can inflict every status except Explosive (which is instantaneous) on the player.
- Statuses on the player always expire on their own timer.
- Active removal: level up clears all statuses, and a Cleanse Pill removes all statuses.
- There are no safe islands, shrines or fountains that cleanse.

### 9.6 Explosive and area damage

- Explosive is a weapon or rune property, not a lasting status.
- Melee explosive (e.g. an Explosive rune on a sword): the splash damages enemies only. The player is never hurt by their own melee splash.
- Ranged explosive (e.g. an explosive arrow): the splash damages every entity in the radius, including the player.
- Splash radius is 1 tile around the impact cell (proposed).

### 9.7 Resistances and immunities

- Every enemy type has a resistance table (Section 13.4). Resistance means the status is not applied; there is no partial resistance.
- Bosses are immune to Enfeeblement's skip, and each boss resists at least one damage status.
- Resistances are visible in the player's bestiary entry once the enemy has been killed once (proposed; see Appendix A item A8).

## 10. Weapons and equipment

### 10.1 Equipment slots

| Slot | Holds | Notes |
| --- | --- | --- |
| Melee | One melee weapon | Sword, cleaver. Uses Strength. |
| Ranged | One bow | Uses Dexterity, requires arrows. Range in tiles. |
| Armour | One armour piece | MaxHealth only, no damage reduction. |

- There are no accessory, ring, offhand or light slots in v1.
- Equipping removes the item from the backpack. Unequipping returns it. An item is never simultaneously equipped and carried.

> Gap B8: the current build has no unequip, and equipped items stay in the bag. Both behaviours must change.

### 10.2 Weapon traits

Traits are what separates one tier from the next, together with rune slots. Each weapon has at most one trait, and a trait is a property of the weapon definition, so it cannot be moved between weapons.

| Trait | Effect | Applies to |
| --- | --- | --- |
| Cleave | A melee attack also hits every other enemy orthogonally adjacent to the target | Melee |
| Pierce | An arrow continues through the first enemy it hits and hits the next one in line | Ranged |
| Multishot | Firing also shoots the two tiles flanking the target cell, if occupied | Ranged |
| Knockback | A hit pushes the target one tile directly away, if that tile is free | Melee |
| Bleed | The hit applies Fire-style damage over time with a bleed theme (3 turns) | Melee |
| Extended range | +2 tiles of range | Ranged |

### 10.3 Weapon progression

One craftable sword and one craftable bow per realm, plus one signature weapon dropped by each realm's boss. Rune slots scale with tier: tier 1 has one slot, tier 2 has two, tier 3 has three.

| Weapon | Realm | Tier | Source | Damage | Req. | Slots | Trait |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Rusty Sword | Plains | 0 | Starting kit | 10 | - | 1 | None |
| Wooden Bow | Plains | 0 | Starting kit | 10 | - | 1 | None |
| Iron Sword | Plains | 1 | Craft (5 Iron Ore) | 15 | 5 STR | 1 | None |
| Composite Bow | Plains | 1 | Craft (4 Iron Ore, 3 Spider Silk) | 12 | 5 DEX | 1 | None |
| Tusk Cleaver | Plains | 1 | Tusk Sovereign drop | 18 | 8 STR | 2 | Cleave |
| Bronze Sword | Desert | 2 | Craft (5 Bronze Ore) | 25 | 12 STR | 2 | Bleed |
| Recurve Bow | Desert | 2 | Craft (4 Bronze Ore, 3 Scorpion Carapace) | 20 | 12 DEX | 2 | Extended range |
| Mirage Bow | Desert | 2 | Mirage Serpent drop | 22 | 14 DEX | 3 | Multishot |
| Frost Iron Sword | Tundra | 3 | Craft (6 Frost Iron Ore) | 35 | 20 STR | 3 | Knockback |
| Longbow | Tundra | 3 | Craft (5 Frost Iron Ore, 3 Wolf Pelt) | 28 | 20 DEX | 3 | Pierce |
| Fallen Immortal's Blade | Tundra | 3 | Ashen Immortal drop | 45 | 25 STR | 3 | Cleave |

> All damage, requirement and slot values are proposed starting points. The intent is that a weapon upgrade is felt through slots and traits first, damage second.

### 10.4 Armour

| Armour | Realm | Source | MaxHealth bonus |
| --- | --- | --- | --- |
| Studded Leather Armor | Plains | Craft (3 Pig Yao Skin, 2 Iron Ore) | +50 |
| Bronze Scale | Desert | Craft (4 Bronze Ore, 2 Scorpion Carapace) | +75 |
| Frost Iron Plate | Tundra | Craft (5 Frost Iron Ore, 2 Wolf Pelt) | +100 |

### 10.5 Arrows

- Arrows are a tracked count, not an inventory slot each. The HUD shows the current count.
- Firing consumes exactly one arrow.
- Crafting converts 1 metal ore into 10 arrows (proposed ratio; the current build makes 1 arrow per ore, which caps the bow at the 10 starting arrows).
- Starting arrows: 10.
- A bow cannot fire with zero arrows; the aim overlay shows the player as out of ammo instead.

## 11. Inscriptions (runes)

### 11.1 Naming and model

- The system is called Inscription. The items are Runes.
- A rune is an item that is applied to one specific piece of equipment, not to a slot or to the player.
- Runes live on the equipment instance, so two Iron Swords can carry different runes.
- This matches the shipped implementation (EquipmentInstance, InscriptionService).

### 11.2 Slots and applying

- Slot count comes from the weapon tier (1, 2 or 3) - see 10.3.
- Applying a rune consumes it from the inventory and fills the chosen slot.
- Runes are permanent while slotted: they cannot be removed or recovered. Applying a rune to a full item overwrites the slot the player chooses, destroying the old rune.
- Applying a rune is free and can be done any time outside combat.
- Runes cannot be applied to arrows, materials or consumables.

### 11.3 Rune effects

| Rune | Effect type | Effect (proposed) | Valid target |
| --- | --- | --- | --- |
| Sharp Rune | Item stat | +5 Strength to the weapon's damage | Any weapon |
| Deft Rune | Item stat | +5 Dexterity to the bow's damage | Ranged |
| Sturdy Rune | Player stat | +20 MaxHealth while equipped | Any equipment |
| Ember Rune | On hit | Attacks apply Fire (3 turns) | Any weapon |
| Venom Rune | On hit | Attacks apply Poison (3 turns) | Any weapon |
| Binding Rune | On hit | Attacks apply Root (2 turns) | Any weapon |
| Thunder Rune | On hit | Attacks apply Explosive splash | Any weapon |
| Warding Rune | Player stat | +15 MaxHealth and immune to Poison | Armour |
| Frugal Rune | Utility | 25% chance not to consume an arrow | Ranged |
| Lantern Rune | Utility | +2 tiles of vision radius | Any equipment |
| Bounty Rune | Utility | +15% chance of a material drop from kills | Any equipment |
| Insight Rune | Utility | +20% experience from kills | Any equipment |

> Only item stat bonuses and player stat bonuses exist in code today (InscriptionEffect: ItemStatBonus, PlayerStatBonus). On-hit and utility effects need new effect types. Gap B9.

### 11.4 Rune sources and supply

- Chests: the main source. About one in three chests holds a rune.
- Enemies: a small chance (about 5%) from any normal kill, so long fights still pay.
- Bosses: every boss drops one guaranteed rune, biased to its realm.
- Crafting: 3 rune shards craft the rune of the player's choice; shards come from chests and bosses.
- Blueprint unlocking applies to runes too: a rune found or crafted once enters the permanent pool.
- Supply target: a full run should hand the player 10-14 runes, so a 2-3 slot weapon is normally filled by the tundra. Runes must not be scarce - they are the main build currency, not a trophy.
- Designer note: if playtesting shows players finishing runs with unspent empty slots, raise the chest rune chance before adding new rune types.

## 12. Items, loot and crafting

### 12.1 Item kinds

| Kind | Usable | Purpose |
| --- | --- | --- |
| Material | No | Crafting input only |
| Weapon | Equip | Melee or ranged, chosen by the ranged flag |
| Armour | Equip | Adds MaxHealth |
| Ammo | No | Tracked arrow count, consumed per shot |
| Consumable | Yes | Pills: stat buffs, healing, cleansing |
| Inscription | Yes | Runes, applied to equipment |
| Blueprint | Yes | Permanent recipe unlock (proposed new kind) |

### 12.2 Materials

| Material | Realm | Source | Used for |
| --- | --- | --- | --- |
| Iron Ore | Plains | Ore veins, chests | Iron Sword, Composite Bow, arrows |
| Pig Yao Tusk | Plains | Pig Yao kill (30% chance) | Tusk Cleaver, weapon handles |
| Pig Yao Skin | Plains | Pig Yao kill (30% chance) | Studded Leather Armor, pouches |
| Spider Silk | Plains | Spider Yao kill (40% chance) | Composite Bow, pouches |
| Bronze Ore | Desert | Ore veins, chests | Bronze Sword, Recurve Bow, Bronze Scale |
| Scorpion Carapace | Desert | Sand Scorpion kill (35% chance) | Bronze Scale, Recurve Bow |
| Venom Sac | Desert | Sand Scorpion kill (25% chance) | Venom Rune, poison pills |
| Frost Iron Ore | Tundra | Ore veins, chests | Frost Iron Sword, Longbow, Frost Iron Plate |
| Wolf Pelt | Tundra | Frost Wolf kill (40% chance) | Frost Iron Plate, Longbow |
| Everfrost Crystal | Tundra | Ice Wraith kill (20% chance) | Lantern Rune, rune crafting |
| Rune Shard | Any | Chests, boss drops | Crafting specific runes |
| Spirit Herb | Any | Chests | Pills |

- Ore veins are the guaranteed material income; monster parts are chance-based.
- Bosses drop a guaranteed signature weapon plus 1 rune and 2-3 rare materials.
- Material drops are type-specific: the enemy that drops it is the enemy you had to kill.

> Gap B10: in the current build no material is obtainable at all - Iron, Web, Tusk and Skin have no loot pool, chest pool or drop source. Both craftable recipes are therefore dead and the player never gets more than the 10 starting arrows.

### 12.3 Rarity and what can drop

- Chests and ordinary enemies never drop weapons or armour. Equipment comes from crafting. The only dropped equipment in the game is the three boss signature weapons.
- Loot rolls keep the current structure: epic 1%, rare 30%, otherwise common, rolled per kill. With equipment removed from the pools, these tiers now carry runes, materials, pills and blueprints.
- Rarity is a property of the loot table entry, not of the item definition (matching the current implementation), so the same item can appear at different rarities in different realms.
- Every realm needs a filled epic pool. The current epic pools are empty, so the 1% epic roll can never produce anything.

### 12.4 Loot tables per realm

| Realm | Common | Rare | Epic |
| --- | --- | --- | --- |
| Plains | Iron Ore bundle, Spirit Herb, Health Pill | Sharp Rune, Sturdy Rune, Rune Shard | Blueprint: Iron Sword or Composite Bow |
| Desert | Bronze Ore bundle, Venom Sac, Health Pill | Ember Rune, Venom Rune, Rune Shard | Blueprint: Bronze Sword or Recurve Bow |
| Tundra | Frost Iron Ore bundle, Everfrost Crystal, Health Pill | Lantern Rune, Insight Rune, Rune Shard | Blueprint: Frost Iron Sword or Longbow |

Chest pools use the same tables with a rune weight of roughly one in three. Ore bundles are 2-3 units.

### 12.5 Recipes and unlocking

- Crafting is the only way to obtain weapons and armour. Chests and enemies supply materials, runes, pills and blueprints; they never supply equipment.
- A recipe is a list of materials and amounts attached to the item definition; an empty recipe means the item cannot be crafted (current model, kept).
- In-run unlocking: obtaining a material for the first time this run reveals every recipe that uses it. Recipes the player cannot afford are shown greyed out.
- Permanent unlocking: blueprint items found in chests or dropped by bosses unlock a recipe for every future run. Blueprints are the meta progression.
- Crafting validates the whole recipe and inventory space before consuming anything.

| Item | Recipe (proposed) | Requirement |
| --- | --- | --- |
| Iron Sword | 5 Iron Ore | 5 Strength |
| Composite Bow | 4 Iron Ore, 3 Spider Silk | 5 Dexterity |
| Arrows x10 | 1 Iron Ore | - |
| Studded Leather Armor | 3 Pig Yao Skin, 2 Iron Ore | - |
| Pouch | 3 Spider Silk (or Pelt) | - |
| Bronze Sword | 5 Bronze Ore | 12 Strength |
| Recurve Bow | 4 Bronze Ore, 3 Scorpion Carapace | 12 Dexterity |
| Bronze Scale | 4 Bronze Ore, 2 Scorpion Carapace | - |
| Frost Iron Sword | 6 Frost Iron Ore | 20 Strength |
| Longbow | 5 Frost Iron Ore, 3 Wolf Pelt | 20 Dexterity |
| Frost Iron Plate | 5 Frost Iron Ore, 2 Wolf Pelt | - |
| Specific rune | 3 Rune Shards | - |

### 12.6 Consumables

| Item | Effect | Duration |
| --- | --- | --- |
| Health Pill | Heals 25 (proposed; currently 10) | Instant |
| Strength Pill | +10 Strength | 6 turns |
| Dexterity Pill | +10 Dexterity | 6 turns |
| Constitution Pill | +10 MaxHealth | 6 turns |
| Cleanse Pill | Removes all status effects | Instant |

- Buff durations sit in the 5-8 turn band; 6 is the starting value.
- Re-applying a pill replaces the existing buff rather than stacking it (current behaviour, kept).

> Gap B11: asset text is inconsistent with the data. The Strength Elixir description says 50 turns while the value is 10; the Constitution Pill says 'constitution' but maps to MaxHealth, and no Constitution stat exists; Pig Yao Skin and Pig Yao Tusk both describe themselves as 'Iron'.

### 12.7 Inventory

- 16 slots, matching the shipped UI.
- Pouches add 4 slots each, to a maximum of two pouches (24 slots).
- Equipment never stacks, and one slot holds one piece of equipment.
- Materials and consumables stack to their stack limit (99 for materials, 100 for consumables, arrows tracked separately).
- When the inventory is full, overflow must be dropped on the ground rather than destroyed.

> Gap B12: today overflow is silently destroyed with a 'inventory full (N lost)' message. With 16 slots that will happen constantly.

### 12.8 Chests

- 1-2 per floor. Contents are rolled at generation time from the realm's loot table.
- Opened with E from an adjacent cell. Free action.
- Opening sets the cell to walkable and updates the island map, so the chest tile does not block the room - both writes are required (current implementation is correct here).
- Chest contents: material bundles, runes, pills, Spirit Herbs, Rune Shards and blueprints. Never equipment.

## 13. Enemies and bosses

### 13.1 Roster

Three regular enemy types per realm, plus one boss per realm. Nine regular enemies and three bosses in total.

| Enemy | Realm | Pattern | HP | Damage | XP | Resists | Applies |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Pig Yao | Plains | Melee chase, brute | 50 | 10 | 10 | Root | - |
| Spider Yao | Plains | Melee, fast, ranged web | 40 | 8 | 10 | - | Root (web) |
| Jing Yao | Plains | Ranged, telegraphed, range 5 | 30 | 10 | 10 | Poison | - |
| Sand Scorpion | Desert | Melee, fast | 60 | 12 | 18 | Poison | Root (sting) |
| Desiccated Monk | Desert | Melee, slow, armoured | 80 | 14 | 22 | Root, Enfeeblement | - |
| Sand Wraith | Desert | Ranged, range 6, hides in storm | 45 | 12 | 20 | Fire | - |
| Frost Wolf | Tundra | Melee, very fast | 55 | 13 | 25 | Root | Enfeeblement |
| Snow Yao | Tundra | Melee brute, applies cold | 90 | 15 | 30 | Fire | Root |
| Ice Wraith | Tundra | Ranged, range 6 | 50 | 14 | 28 | Root, Fire | - |

> All stats are proposed. Grounding: Pig Yao, Spider Yao and Jing Yao already exist in the build; the six desert and tundra enemies are new content. Names are subject to the designer's approval.

### 13.2 Boss rules

- Every boss occupies more than one tile. The Tusk Sovereign is 2x2, Shen's surfaced body spans 3 tiles, and the Ashen Immortal is 3x3. A hit on any occupied tile damages the boss.
- A multi-tile boss blocks movement and blocks line of sight through every tile it occupies.
- Bosses obey the turn system exactly: exactly one action per turn. No boss ever takes two actions in a round, and no boss action is ever free. Any design that would give a boss a second action is invalid.
- Boss arenas are larger than the original GDD's 9-12 limit: 15x15 to 19x19, open, with no interior walls, so multi-tile movement and charge lanes have room.
- The arena locks when the player enters. Bosses never leave it.
- Each boss has two phases and is immune to Enfeeblement's skip. All three drop a signature weapon, one guaranteed rune and rare materials.

### 13.3 Boss stat blocks and behaviour

| Boss | Realm | Size | HP | Phase 1 (one action per turn) | Phase 2 (below 50%) |
| --- | --- | --- | --- | --- | --- |
| The Tusk Sovereign | Plains | 2x2 | 400 | Charge: spends its action marking a 2-tile-wide lane across the arena. On its next turn it charges that lane as its action for 20 damage and knocks back everything hit, then must spend its following turn recovering. Alternatively Gore (15, adjacent) or Roar (alerts the arena). | Drops the recovery turn, so it can charge every other turn. Gains Summon Boar (one Pig Yao, costs its action). Its charge leaves rubble in the tiles it crossed (impassable). Still only one action per turn. |
| Shen, the Mirage Serpent | Desert | 3 tiles of body when surfaced | 600 | Burrow or Surface: one action. Untargetable while burrowed; resurfaces in a new position. While surfaced its body occupies 3 tiles that block player movement and line of sight, so the player must route around it or break through it. Also Bite (18, adjacent) and Sand Blast (15, ranged line, telegraphed one full turn ahead). | Creates one mirage copy per turn as its action. Copies block movement, have 1 HP and deal no damage. The real serpent's attacks hit for double. The sandstorm tightens to 3 tiles. |
| The Ashen Immortal | Tundra | 3x3 | 900 | Frost Ring: telegraphs an expanding ring of ice across the arena, which detonates on its next action. Also Teleport (blink to a new position, one action) and Strike (20, adjacent). | Ashen Ground: may instead spend its action corrupting a 3x3 patch of floor into a hazard tile. Ashen Ground applies Fire to anything standing on it, persists for the rest of the fight, and cannot be lit by torches. It keeps Frost Ring and Teleport as alternatives. It no longer extinguishes torches; the spreading hazards are the pressure instead. |

> Open item A9: whether boss arenas get unique tilesets (recommended: unique tileset and hazard art for the final boss only, to keep art scope down).

> Gap B21: multi-tile bosses are new. The current entity, movement, line-of-sight and turn queue code all assume one entity per cell, so this touches EntityManager, movement validation, range targeting and the enemy turn queue.

### 13.4 Enemy awareness, restated

| Realm | Awareness | Reason |
| --- | --- | --- |
| Plains | Room-wide on entering the island | Islands define rooms, so the boundary is readable |
| Desert | Detection radius with line of sight | Open field; lets the player route around threats |
| Tundra | Detection radius with line of sight | Open field; pairs with limited vision |

### 13.5 Resistances

- A resistance means the status cannot be applied at all. There is no partial resistance.
- Every enemy resists at least one status, which is what makes rune choice matter per realm (for example, desert enemies resist Fire but not Poison).
- Resistances are listed in the table in 13.1 and are shown in the UI once the enemy has been killed at least once.

### 13.6 Per-floor scaling

- Within a realm, each floor multiplies enemy health by (1 + 0.15 x floor index) and enemy damage by (1 + 0.10 x floor index), where the realm's first floor is index 0.
- Across realms, the enemy roster changes rather than the multipliers jumping, so the player reads difficulty as new threats, not bigger numbers.
- Boss health and damage are fixed values, not scaled, because their floors are fixed.

### 13.7 Drops and experience

- Every kill gives experience and rolls the realm's loot table once.
- Chance-based, type-specific material drops as listed in 12.2.
- Damage-over-time kills give full credit.

## 14. UI and UX

### 14.1 HUD

| Element | Status | Notes |
| --- | --- | --- |
| HP bar | Exists | Keep; add current/max numbers |
| XP bar + level | Exists | Add the level number beside the bar |
| Minimap | New | Explored terrain, discovered stairs, known chests. Enemies always hidden |
| Arrow counter | New | Current arrow count; turns red at 0 |
| Equipped weapon indicator | New | Shows sword or bow, and the draw state |
| Status effect icons | New | Icon plus turns remaining, for the player's own statuses |
| Realm + floor indicator | New | e.g. 'Desert - Floor 2' |
| Quick-use pill slot | New | One slot for the selected pill, used with a key |
| Turn indicator | New | Shows whose turn it is and that input is locked during the enemy turn |

### 14.2 Screens

| Screen | Status | Contents |
| --- | --- | --- |
| Inventory + item detail | Exists | 16 slots; detail pane shows name, description, stats, runes and Use button |
| Crafting | Exists | Recipe list, greyed when unaffordable or locked; shows requirements |
| Level-up allocation | Exists | 5 points across Strength, Dexterity, MaxHealth, with reset |
| Rune application | Exists | Target list (equipped first), then slot chooser for the chosen item |
| Pause + settings | Partial | Volume exists; add keybind reference |
| Controls / how to play | New | One page listing every binding and the turn rules |
| Death screen (run summary) | New | Realm and floor reached, kills, time, materials gathered, unlocks earned |
| Victory / ending screen | New | Ending text for defeating the Ashen Immortal |
| Blueprint journal | New | Every permanently unlocked weapon, rune and recipe, per realm |

### 14.3 Controls

| Input | Action |
| --- | --- |
| W A S D | Move one tile (turn-based) or walk continuously (free movement) |
| 1 | Draw the melee weapon |
| 2 | Draw the bow / enter aim mode (free) |
| Left click | Fire at the highlighted tile while aiming |
| Right click | Cancel aim mode |
| E | Interact: open chest, mine ore, take stairs |
| I / Tab | Inventory |
| C | Crafting |
| Esc | Pause / close the current panel |
| Number keys in panels | Quick-use slots for pills |

- Input stays keyboard + mouse: WASD plus mouse aim. Keyboard-only aiming and gamepad support are out of scope for v1.
- The player is locked out of movement while any full-screen panel is open, and panels cannot be opened during the enemy turn.

### 14.4 Feedback rules

- Damage numbers float up from the victim (already implemented).
- The telegraphed enemy shot is drawn as a low-opacity red overlay on the target tile.
- The player flashes and shakes briefly when hit (already implemented).
- Full inventory and blocked actions report through the floating text system rather than the console.
- Picking up a material for the first time shows a 'new recipe unlocked' toast, which is the in-run legibility for recipe unlocking.

### 14.5 Minimap rules

- Terrain is revealed as the player explores, and stays revealed for the floor.
- Stairs appear when discovered; the marker persists after discovery.
- Chests and ore veins appear once their island has been entered.
- Enemies are never shown on the minimap, in any realm.
- The minimap is per floor and is cleared on descent.

## 15. Art and audio

### 15.1 Art scope

| Field | Value |
| --- | --- |
| Style | 32x32 pixel art, matching the current pipeline |
| Tilesets | One per realm (plains, desert, tundra), including water/dune/snow variants and hazard tiles |
| Enemies | 9 regular enemies plus 3 bosses. Bosses are multi-tile: 2x2 (Tusk Sovereign), a 3-tile body (Mirage Serpent) and 3x3 (Ashen Immortal), each with idle, move, attack and death animation |
| Player | Sword, bow, aim, hit and death animations; the GDD's 'player holding weapon' requirement |
| Obstacles | Bushes, rocks, huabiao pillars, spider webs |
| Hazards | Slippery ice, crevasses, rubble, and the final boss's Ashen Ground tiles |
| Items | Icons for 7 weapons, 3 armour pieces, 12 materials, 5 consumables, 12 runes |
| UI | HUD frames, status icons, minimap frame, panel art, journal art |

- Reuse existing tiles with per-realm palette work where a full new set is not ready; the palette pass is what makes a realm read differently at first glance.

### 15.2 Art requirements carried over from the original GDD

1. Player sprite visibly holding the current weapon (sword or bow).
2. A visual for skipping a turn, so the player can see an enemy's turn was consumed.
3. The telegraphed ranged attack: low-opacity red on the threatened tiles.
4. Torch placement art, and a visible lit radius on the tundra.
5. Boss telegraph art: the charge lane, the frost ring and the Ashen Ground tiles must all read clearly one full turn before they hurt the player.

### 15.3 Audio scope

| Field | Value |
| --- | --- |
| Music | One ambient track per realm, plus one boss theme per realm (6 tracks) |
| Ambience | Water, wind/sandstorm, blizzard, torch crackle |
| SFX | Sword hit, bow fire, arrow hit, enemy death, level up, craft, rune apply, chest open, ore mine, UI clicks, player hurt |
| Mixing | Master volume plus separate music and SFX sliders |
| Darkness | The tundra's audio should narrow with vision; wind masks distant enemy audio |

## 16. Technical design

### 16.1 Constraints to keep

- Unity 2022.3.62f2, URP 2D, Tilemap, TextMeshPro, legacy Input, no namespaces, no asmdef.
- ItemDefinition stays a shared, immutable ScriptableObject. Per-item state (runes) lives on the equipment instance, never on the definition.
- Item definitions stay under Assets/Resources/ItemDefinitions/<Kind>/ so the crafting loader keeps working.
- The grid remains the single authority for movement. Chests must update both the grid value and the island map when opened.
- Scenes: ui.unity is build index 0 (the death screen loads it) and 'Branching dungeon.unity' is index 1.

### 16.2 Systems to add

| System | Purpose | Depends on |
| --- | --- | --- |
| RunState | Realm, floor, seed, RNG state; drives generation and saving | Nothing |
| FloorGenerator changes | 6-8 islands per floor, multiple floors, stairs and boss islands | RunState |
| Status system | Apply, tick, expire, resist, skip-turn for all 5 statuses | Enemy behaviour refactor |
| Enemy awareness | Detection radius, line of sight, roaming for unaware enemies | Line of sight |
| Line of sight | Blocking check used by player aim and enemy ranged attacks | Obstacle tilemap |
| Multi-tile entities | Bosses occupying 2x2 / 3x3 / a 3-tile body, with movement, collision and targeting rules | EntityManager, line of sight |
| Loot tables | Per-realm common/rare/epic pools, materials and runes only (no equipment) | Realm data |
| Blueprint/Unlock store | Permanent unlocks and the journal | Save system |
| Save system | Run autosave at floor transitions; unlocks and settings | RunState |
| Minimap | Explored-tile reveal and markers | FloorGenerator |
| Localization tables | All player-facing strings | Nothing |

### 16.3 Save format

- Run state (autosave at each floor transition): realm index, floor index, player level and stats, current health, inventory contents including equipment instances and their slotted runes, unlocked-this-run recipes, arrows, and remaining placed torches.
- Permanent data (JSON, in the persistent data path): unlocked blueprints, journal entries, lifetime stats.
- Settings (PlayerPrefs, as today): master/music/SFX volume, any keybind overrides.
- A run save is deleted on death or victory; unlocks survive both.

### 16.4 Performance and platform

- Ship on Windows and macOS from the same project. Both builds are first-class; the game must be playable on Apple Silicon natively, not only through translation.
- Target 60 fps at 1080p on a mid-range Windows PC and on Apple Silicon macOS.
- Save data, unlock data and settings all use Application.persistentDataPath and PlayerPrefs, which behave the same on both platforms; no Windows-only file APIs.
- Verify input and fullscreen behaviour on both platforms, since the game is keyboard + mouse only.
- 120x120 grid with 6-8 islands and up to 12 enemies is well within budget; the risk is per-tile work in the fog-of-war and lighting pass, which must be limited to the visible radius rather than the whole map.
- Render the tundra light pass through a single Tilemap or a small mesh, not one GameObject per lit tile.

## 17. Milestones

The original week-by-week table (dated May 2026 with placeholder tasks) is replaced by milestone phases with no hard dates. Each milestone is playable and testable.

### M1 - Plains, finished end to end

- Scope: one realm, complete. Player starts a run, clears 2 plains floors of 6-8 islands, finds the stairs, descends, fights the Tusk Sovereign, and reaches a victory placeholder.
- Run structure: RunState, floor generation with 6-8 islands, exit stairs, boss island, descend transition, death and victory screens.
- Combat: all five statuses with tick and resist rules; enemy awareness (room-wide in plains) and roaming; line of sight; blocked moves are free; aim mode is free to enter.
- Boss: the Tusk Sovereign as a 2x2 multi-tile entity in a 15x15-plus open arena, obeying one action per turn, with the telegraphed charge lane and its phase 2.
- Items: materials obtainable (ore veins plus Pig Yao and Spider Yao drops), full recipe list for the plains, arrows in batches of 10, rune slots scaling with tier, and the on-hit rune effects that plains needs.
- UI: minimap, arrow counter, weapon indicator, status icons, realm/floor indicator, turn indicator, quick-use pill slot, death summary, blueprint journal.
- Exit criteria: a full plains run is playable in one sitting, and no known crash remains.

### M2 - All three realms

- Desert and tundra generation, sandstorm vision, darkness and torches, ice and crevasses.
- Nine regular enemies plus three bosses with their phases and resistances.
- Complete weapon, armour, rune and material tables for all three realms.
- Per-floor scaling, full loot tables with filled epic pools, boss drops.

### M3 - Meta, save and polish

- Run autosave and resume at floor transitions; permanent unlock JSON; journal completeness.
- Localization string tables and a full text pass.
- Balance pass across the whole run: experience curve, enemy health scaling, pill and status numbers, weapon requirements.
- Audio pass, UI polish, control reference screen, and the tuning items listed in Appendix A.

### 18. Out of scope for v1

- Fire spreading to terrain, destructible obstacles, and burning tiles.
- Accessory, ring, offhand and light equipment slots.
- NPCs, dialogue system, shops, currency and a hub scene.
- Gamepad and keyboard-only aiming.
- Mid-floor saving, extra lives, and endless or daily-run modes.
- A bestiary screen (resistance display is proposed but optional; see Appendix A item A8).

## Appendix A - Open decisions

Each item below is undecided. A recommendation is given for each; if the designer accepts them, they can be folded into the body text as decisions.

| # | Question | Recommendation |
| --- | --- | --- |
| A1 | Should the desert's sandstorm also reduce how far enemies notice the player, or only the player's vision? | Vision only. Reducing both makes the desert unfair rather than tense. |
| A2 | Flat additive stats plus 5 points per level will outgrow weapon damage by the tundra. How should the curve be controlled? | Scale enemy health per floor as specified, and review weapon tier damage after the first full-run playtest. |
| A3 | Does the tundra's permanent torch light persist across floors? | No. Torches placed are consumed, but light does not carry between floors. |
| A4 | Can the player place a torch in combat? | Yes, at the cost of one turn. It is a legitimate tactical choice. |
| A5 | Are chests visible on the minimap before their island is entered? | No. They appear when the island is entered, matching the exploration intent. |
| A6 | Do ore veins run out, and are there enough of them? | 2-4 per floor, finite, so a player who skips mining feels the shortage later. |
| A7 | Does slippery ice affect enemies as well as the player? | Yes. It makes the ice a battlefield the player can exploit with knockback and Root. |
| A8 | Is the enemy resistance display in scope (bestiary or tooltip)? | Tooltip on the enemy, revealed after the first kill. Keeps the information in-world and avoids a new screen. |
| A9 | Does each boss arena get its own tileset and hazard? | Unique hazard for the final boss only; the first two reuse the realm tileset. |
| A10 | Do runes have rarity, and do rarer runes need more slots? | Runes have rarity for loot purposes only; every rune costs one slot. |
| A11 | Can the player hold more than one of the same rune and choose? | Yes; runes stack in the inventory as separate items with a count, and applying consumes one. |
| A12 | Is there a use for spare materials late in a run? | Sell-less economy: spare ore converts to arrows and pills, which keeps it useful. |
| A13 | How big is a pouch craft, and what caps it? | Two pouches maximum, +4 slots each, crafted from realm cloth/leather. |
| A14 | Should the player be able to drop items on the ground? | Yes, dropping is needed to make room and to fix the current overflow-destroys-items behaviour. |
| A15 | Do enemies respawn on a floor the player has left? | No. Floors are one-way, so respawning has no purpose. |
| A16 | What exactly does the run summary show? | Realm and floor reached, kills, time, materials gathered, recipes unlocked. |
| A17 | Confirm the Tusk Sovereign's charge: lane width, telegraph length, cooldown, and the recovery turn. | 2-tile-wide lane, telegraphed for one full round, recovery turn after the charge; phase 2 drops the recovery so it can charge every other turn. Never more than one action per turn. |
| A18 | How many runes should a full run deliver? | 10-14 found, so a 3-slot weapon is usually full by the tundra. Runes are build currency, not trophies. |
| A19 | What exactly does Ashen Ground do, and does it interact with torches? | Applies Fire to anything standing on it, persists for the fight, and cannot itself be lit. It does not extinguish torches that are already placed. |
| A20 | Do multi-tile bosses block line of sight, or only movement? | Both. A body that large reading as transparent would make the ranged fights confusing. |

## Appendix B - Known issues and code gaps

Verified against the codebase while writing this document. Each item is a real gap between the design above and the current build.

| # | Gap | Where |
| --- | --- | --- |
| B1 | No run structure: one endless map, exactly 5 rooms, only Starting and Combat room types, no stairs, boss, win state or save. | Level generator |
| B2 | Mapgen crash when a room has exactly one free direction (index out of range); room count is hardcoded to 5. | ProceduralGeneraltionAlgorithm |
| B3 | The game is turn-based everywhere; free movement outside combat does not exist. | Controls / TurnManager |
| B4 | Blocked moves consume a turn (the turn event fires before validation), and entering aim mode triggers an enemy turn without locking input, which can start a second concurrent enemy-turn coroutine. | Controls, TurnManager |
| B5 | Enemy awareness is island-based only; detection radius is dead code; unaware enemies never move, so roaming does not exist. | EnemyBehavior |
| B6 | Line of sight is a no-op: the check tests a tilemap nothing paints, so both sides shoot through walls and rocks. | RangeAttackTilemap, RangeEnemyBehavior |
| B7 | Damage-over-time kills must use the normal death path or they bypass drops and leave the enemy registered in the turn queue. | To build with the status system |
| B8 | No unequip, and equipped items stay in the backpack, so an item is both equipped and carried. | Equipments, Inventory |
| B9 | Only item and player stat bonuses exist; on-hit and utility rune effects need new effect types. | InscriptionEffect |
| B10 | No material is obtainable at all, so both recipes and the arrow economy are dead ends. | LootGen, chest pools |
| B11 | Asset text contradicts data: Strength Elixir says 50 turns but is 10; Constitution Pill maps to MaxHealth; Pig Yao Skin and Tusk both describe themselves as 'Iron'. | ItemDefinitions |
| B12 | Inventory overflow destroys items instead of dropping them. | Inventory, LootGen, Chest |
| B13 | No status effect system of any kind exists. | To build |
| B14 | Armour only adds MaxHealth, and there is no unequip or swapping logic, so the armour rules in 8.5 cannot be verified yet. | Equipments, PlayerStats |
| B15 | Level-up screen has dangling listener entries pointing at null targets alongside the real handler, and a free heal call on the health button. | Branching dungeon.unity, levelUpUI |
| B16 | No rarity field on items, and every epic loot pool is empty, so the 1% epic roll yields nothing. | LootGen, scenes |
| B17 | Unused leftovers: Spider Yao prefab referenced by nothing, orphaned gameHUD scene, empty CastVisualizer folder, achievement art with no code. | Assets |
| B18 | The 'sword drawn' flag is always true and never modified; the real melee gate is having a melee weapon equipped. | Controls |
| B19 | Six correctness bugs documented but unfixed in the refactoring plan (flower density, combat inside MoveEntity, branching-room range errors, ranged cooldown going negative, hardcoded death scene index, string-based coroutine start). | See Docs/Refactoring-Plan.md |
| B20 | Documentation drift: ItemSystem.md points at a folder that does not exist and its data tables are stale; InscriptionSystem.md is superseded by shipped code. | Docs/ |
| B21 | Multi-tile bosses do not exist. Entity, movement, collision, line of sight and the enemy turn queue all assume one entity per cell. | EntityManager, Grid, EnemyBehaviour |
| B22 | Hazard terrain (slippery ice, crevasses, rubble, Ashen Ground) has no representation; terrain codes 2 and 5 are unused legacy values with no behaviour. | Grid terrain codes |
| B23 | The build has never shipped for macOS. Input, fullscreen, save paths and the build pipeline need verifying on Apple Silicon. | Build pipeline |

## Appendix C - Changes from the original GDD

| Original GDD | This document | Why |
| --- | --- | --- |
| Sections described only as 'section 2 = Dusk', 'section 3 = Midnight' | Three realms (plains, desert, tundra), each with its own visibility gimmick | Designer decision: biomes are independent, not a time-of-day ladder |
| No run structure stated | 3 realms x 2-3 floors, 6-8 islands per floor, one-way descent, 30-45 minute runs | Needed to be buildable; pacing measured in testing |
| 'Fog of war' with discovered areas faintly lit, then fully dark with torches | Desert: static reduced vision. Tundra: 3-tile vision plus permanent torches | Simpler, testable, and biome-specific |
| Engravings applied to armour/weapons | Inscriptions (runes), max 3 slots scaling with tier | Matches shipped code; naming confirmed by the designer |
| Weapons differ by damage | Weapons differ by rune slots and traits first, damage second | Designer decision: avoid boring damage upgrades |
| Status buffs listed as Fire, Poison, Enfeeblement, Explosive | Same four plus Root; 3-turn damage over time, 5-8 turn buffs; bosses immune to skip | Root added for the shipped web obstacle; durations resolved against code |
| Movement: moving a tile takes one turn, always | Free movement outside combat, one action per turn in combat | Designer decision, matches the original intent for empty rooms |
| Pills last 3-5 turns | Buffs last 5-8 turns (6 proposed); damage over time lasts 3 | Resolved against the shipped 10-turn value |
| Crafting unlocked by picking up a specific part | Material discovery in-run plus permanent blueprint unlocks from chests and bosses | Designer decision: blueprint meta progression |
| Week-by-week timeline dated May 2026 | Milestone phases M1-M3 with no dates | The dated table was already stale and had placeholder tasks |
| Armour heals to max only if at full health on the equip turn | Armour adds MaxHealth, never heals | Original rule was confusing and created an equip-swap exploit |
| 'Boss islands will always be between 9 and 12' | Boss arenas are 15x15 to 19x19 and open | Bosses occupy several tiles and need charge lanes, so the old cap is too small |
| Bosses implied to be single-tile like other enemies | Bosses are multi-tile: 2x2, a 3-tile body, and 3x3 | Designer correction: a boss should be visibly bigger than one tile |
| Loot was the source of weapons and armour | Chests and enemies never drop equipment; it is crafted. Boss signature weapons are the only dropped equipment | Designer correction: equipment drops diluted the crafting loop |
| Rune supply unspecified | Roughly one chest in three holds a rune; a run should deliver 10-14 runes | Designer correction: runes must not be so rare that a run ends with empty slots |
| No platform stated | Windows and macOS, both first-class | Designer correction |
| Boss actions unspecified (the first draft allowed two charges per round) | Every boss takes exactly one action per turn, with telegraphed lanes, rings and hazards doing the pressure instead | Two actions per round contradicts the game's own turn system |

> End of document. Generated from an interview with the designer plus a survey of the existing codebase; all decisions are traceable to one of the two.
