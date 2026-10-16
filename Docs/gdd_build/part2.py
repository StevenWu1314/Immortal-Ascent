"""GDD v2 content, part 2: turn system, stats, combat, status effects."""
from helpers import h1, h2, h3, p, b, n, note, t, kv, tag, page_break


def build():
    # ------------------------------------------------------- 6 turn system
    page_break()
    h1("6. Turn system and action economy")
    h2("6.1 When the game is turn-based")
    b("Free movement: while no living enemy shares the player's island, the player walks freely. "
      "No turns are spent and enemies do not act.")
    b("Turn-based: the moment the player is on an island with at least one living enemy, every "
      "input becomes a turn.")
    b("Room lock: while enemies are alive on the island, the player cannot leave it. Corridors and "
      "empty islands are never locked. Entering an occupied island is always allowed.")
    b("The lock releases the instant the last enemy on the island dies, including from a "
      "damage-over-time tick.")
    note("Current build is turn-based everywhere and only gates the island boundary. Gap B3.")

    h2("6.2 One action per turn")
    p("Each turn the player takes exactly one of the following. There is no move-then-attack.")
    t(["Action", "Cost", "Notes"], [
        ["Move one tile", "1 turn", "Orthogonal only; no diagonal movement"],
        ["Attack with melee", "1 turn", "Bump into an adjacent enemy, or attack with the sword drawn"],
        ["Fire the bow", "1 turn + 1 arrow", "Requires aim mode; see 8.3"],
        ["Use an item", "1 turn", "Pills and cleanses only; equipping and runes are free"],
        ["Place a torch", "1 turn", "Tundra only; light is permanent"],
        ["Mine ore", "1 turn", "From an adjacent cell, with E"],
        ["Open a chest", "Free", "From an adjacent cell, with E. Explicitly not a turn"],
        ["Equip, apply runes, craft, level up, open UI", "Free", "Out of combat only for crafting"],
    ])

    h2("6.3 Failed actions")
    b("Bumping a wall, a crevasse, water or a locked gate costs nothing. The player keeps their turn.")
    b("Bumping into an enemy is an attack, and therefore costs the turn.")
    b("Aim mode: entering and leaving it is free and never triggers an enemy turn. Only the shot "
      "costs a turn.")
    note("Current build consumes a turn before validating the move, so a wall bump hands the "
         "enemies a free turn, and entering aim mode fires the turn event without locking input. "
         "Both are fixed by this rule. Gap B4.")

    h2("6.4 Turn order")
    n("The player acts. Damage, status application and pickups resolve immediately.")
    n("The enemy turn runs. Every enemy that is aware of the player takes its action in queue order.")
    n("End of round: damage-over-time ticks, then status durations decrease by one turn.")
    b("Bosses obey the turn system exactly: one action per turn. No enemy, boss included, ever "
      "takes two actions in a round, and no boss action is ever free.")
    b("Enemies act one at a time with a short visible step between them, so the player can read "
      "what is happening (current pacing: ~0.15s before the enemy phase, ~0.05s between enemies; "
      "keep both tunable in the Inspector).")
    b("The player cannot act during the enemy turn. The input lock must be released reliably even "
      "if an enemy dies mid-phase.")

    h2("6.5 Who acts")
    b("An enemy acts only if it is aware of the player (it has noticed them). Unaware enemies run "
      "their roaming behaviour instead.")
    b("Awareness, plains: the player entering the enemy's island alerts every enemy on it.")
    b("Awareness, desert and tundra: an enemy notices the player inside its detection radius, with "
      "line of sight. Awareness is not shared - one enemy seeing the player does not alert the others.")
    b("Once aware, an enemy stays aware until it loses the player and returns to its post, after "
      "which it becomes unaware again.")
    note("Current build: alerting is purely island-based, detection radius is dead code, and unaware "
         "enemies never move at all. Gap B5.")

    h2("6.6 Roaming")
    b("Unaware enemies wander inside their island: one tile per round, never leaving the island, "
      "never stepping onto another entity.")
    b("Roaming never triggers an attack and never opens a door.")
    b("Roaming stops the moment the enemy becomes aware of the player.")
    b("Enemies never leave their island, whether roaming or chasing. A stranded enemy walks home.")

    h2("6.7 Line of sight")
    b("Ranged attacks, both the player's and the enemy's, require a clear line of sight.")
    b("Blockers: rocks, huabiao pillars, walls and the water surrounding islands block shots.")
    b("Crevasses do not block line of sight, which is what makes the tundra dangerous.")
    b("The aim overlay only highlights tiles the player can actually hit.")
    note("Current build has no working line of sight: the collision check tests a tilemap that "
         "nothing paints, so both sides shoot through walls. Gap B6.")

    # --------------------------------------------------- 7 stats progression
    page_break()
    h1("7. Player stats and progression")
    h2("7.1 Stats")
    t(["Stat", "Effect", "Starting value"], [
        ["Strength", "Added to melee damage; gates melee weapon requirements", "0"],
        ["Dexterity", "Added to ranged damage; gates bow requirements", "0"],
        ["MaxHealth", "Health pool; the player dies at 0", "100"],
        ["Health", "Current health", "100"],
    ])
    p("There is no Defence, no speed, no critical chance and no hit chance. Every attack that is "
      "fired in range and in line of sight hits.")
    b("Melee damage = Strength + equipped melee weapon damage.")
    b("Ranged damage = Dexterity + equipped ranged weapon damage.")
    h2("7.2 Level and experience")
    kv([
        ("Starting level", "1"),
        ("Points per level", "5"),
        ("Point options", "+1 Strength, +1 Dexterity, or +10 MaxHealth"),
        ("Experience source", "Enemy kills only. Bosses give a large fixed amount."),
        ("Curve", "Level 1 needs 10 XP; each level's requirement is 1.5x the previous, rounded down"),
        ("Level cap", "None in practice (current cap 100 is never reached in a run)"),
        ("On level up", "Full heal, all status effects cleared, the game pauses for allocation"),
    ])
    note("Proposed, not balance: the 1.5x curve plus 5 points per level produces roughly 12-18 "
         "levels in a 6-9 floor run, which is 60-90 stat points. Because damage is flat additive, "
         "stat points will outgrow weapon damage in the late run. Appendix A item A2 covers the "
         "options (raise enemy health faster, cut points per level, or make weapon tiers scale "
         "with the player).")
    h2("7.3 Healing")
    b("A level up fully heals the player.")
    b("Health pills heal a fixed amount (proposed 25).")
    b("There is no passive regeneration and no healing between floors.")
    b("The player's health carries over between floors, including into boss fights.")
    h2("7.4 Requirements")
    b("Every craftable weapon has a minimum Strength (melee) or Dexterity (bow) requirement.")
    b("A weapon the player cannot yet use is shown greyed out with the requirement in red, both in "
      "the crafting list and in the inventory.")
    b("Requirements are proposed per weapon in Section 10.3.")
    h2("7.5 Death")
    b("At 0 health the run ends. The player returns to the menu.")
    b("No extra life, no revival item, no checkpoint inside a floor.")
    b("The death screen shows the run summary (Section 14.4).")

    # -------------------------------------------------------------- 8 combat
    page_break()
    h1("8. Combat")
    h2("8.1 Melee")
    b("The player attacks by bumping into an adjacent enemy while a melee weapon is equipped.")
    b("Melee is orthogonal only, single target, unless the weapon has the Cleave trait.")
    b("Enemies attack by bumping into the player's cell.")
    h2("8.2 Weapon swapping")
    b("Key 1 draws the sword, key 2 draws the bow or enters aim mode. Swapping is free and does not "
      "cost a turn.")
    b("The equipped melee weapon and the equipped bow are two separate slots, so both are always "
      "available.")
    h2("8.3 Ranged and aim mode")
    n("Press 2. Aim mode opens and highlights every tile the bow can hit: within the bow's range, "
      "in line of sight. Entering aim mode is free.")
    n("Move the mouse to choose a tile. Left click on a tile containing an enemy fires.")
    n("Firing costs one turn and one arrow and resolves damage immediately.")
    n("Clicking anywhere else cancels with no cost. Right click leaves aim mode.")
    b("Range is measured as a circle (Euclidean) from the player, matching the current overlay.")
    b("Bows never misfire and never damage the player unless the weapon or rune has the Explosive "
      "property (Section 9.6).")
    h2("8.4 Enemy attack patterns")
    t(["Pattern", "Behaviour"], [
        ["Melee chase", "Walks toward the player, one tile per round, and attacks on contact"],
        ["Ranged telegraph", "Marks the tile it will fire at, then fires on its next turn. Moving out of the marked tile avoids the shot entirely. Cooldown 2 rounds between shots."],
        ["Ranged strafe", "If it cannot shoot, it steps sideways to find a line instead of standing still"],
        ["Roamer", "Wanders until aware, then behaves as its pattern dictates"],
    ])
    b("The player sees the telegraphed tile highlighted in low-opacity red; this is the GDD's "
      "original art requirement and it is already implemented.")
    h2("8.5 Armour")
    b("Armour occupies one slot and adds to MaxHealth only. There is no damage reduction.")
    b("Equipping armour does not heal the player, and does not change current health. Swapping "
      "armour removes the old piece's bonus first, so bonuses never stack.")
    b("The original GDD's conditional 'heals to max only if you were at full health' rule is "
      "dropped as confusing.")
    h2("8.6 Damage numbers")
    t(["Source", "Formula", "Example"], [
        ["Player melee", "Strength + melee weapon damage + rune item bonuses", "0 + 15 = 15, doubled later by runes"],
        ["Player ranged", "Dexterity + bow damage + rune item bonuses", "0 + 10 = 10"],
        ["Poisoned target", "Incoming damage x1.25, rounded down", "15 -> 18"],
        ["Enemy melee", "Flat value from the enemy's stat block", "10"],
        ["Enemy ranged", "Flat value from the enemy's stat block", "12"],
        ["Explosive splash", "50% of the triggering attack's damage, per affected entity", "15 -> 7"],
    ])

    # ------------------------------------------------------ 9 status effects
    page_break()
    h1("9. Status effects")
    p("This is the largest missing system in the current build: no status effect of any kind exists. "
      "The five below are the full set for v1.")
    h2("9.1 The five statuses")
    t(["Status", "Effect", "Duration (proposed)", "Notes"], [
        ["Fire", "5 damage at the end of each round", "3 turns", "Damage over time"],
        ["Poison", "2 damage at the end of each round, and the target takes +25% damage from all sources", "3 turns", "The +25% is the important half"],
        ["Enfeeblement", "-50% attack damage, and skips its turn while active", "3 turns", "Bosses are immune to the skip; they still take the damage penalty"],
        ["Root", "Cannot move. Can still attack if already in range", "2 turns", "Webs apply Root too"],
        ["Explosive", "Instant, not a duration. Splash damage in a 1-tile radius around the impact", "Instant", "See 9.6"],
    ])
    note("The GDD's original four were Fire, Poison, Enfeeblement and Explosive; Root was added "
         "because the shipped web obstacle needed a mechanical consequence. Any further statuses "
         "are out of scope for v1.")

    h2("9.2 How statuses are applied")
    t(["Vector", "Applies", "Notes"], [
        ["Weapon traits", "Bleed-style effects and status on hit", "Section 10.2"],
        ["Runes", "On-hit status from the rune slotted in the weapon that attacks", "Section 11"],
        ["Consumables", "Pills, including cleanse pills", "Section 12.6"],
        ["Enemies", "Their own attacks can inflict statuses on the player", "Section 13"],
    ])
    h2("9.3 Stacking")
    b("Re-applying a status refreshes its duration to full. Magnitude never stacks.")
    b("If a stronger and a weaker application land on the same target, the stronger magnitude is "
      "kept and the duration refreshes.")
    b("Identical statuses from different sources are still one status.")
    h2("9.4 Tick timing")
    b("Damage over time resolves at the end of the round, after every enemy has acted.")
    b("Durations decrease once per round, after the tick. A 3-turn Fire therefore ticks 3 times.")
    b("A damage-over-time kill credits the player: it grants experience and rolls loot exactly as a "
      "direct kill does.")
    note("Gap B7: damage-over-time kills must go through the same death path as normal kills, or "
         "they bypass drops and leave the dead enemy registered in the turn queue.")
    h2("9.5 Player statuses and cleansing")
    b("Enemies can inflict every status except Explosive (which is instantaneous) on the player.")
    b("Statuses on the player always expire on their own timer.")
    b("Active removal: level up clears all statuses, and a Cleanse Pill removes all statuses.")
    b("There are no safe islands, shrines or fountains that cleanse.")
    h2("9.6 Explosive and area damage")
    b("Explosive is a weapon or rune property, not a lasting status.")
    b("Melee explosive (e.g. an Explosive rune on a sword): the splash damages enemies only. The "
      "player is never hurt by their own melee splash.")
    b("Ranged explosive (e.g. an explosive arrow): the splash damages every entity in the radius, "
      "including the player.")
    b("Splash radius is 1 tile around the impact cell (proposed).")
    h2("9.7 Resistances and immunities")
    b("Every enemy type has a resistance table (Section 13.4). Resistance means the status is not "
      "applied; there is no partial resistance.")
    b("Bosses are immune to Enfeeblement's skip, and each boss resists at least one damage status.")
    b("Resistances are visible in the player's bestiary entry once the enemy has been killed once "
      "(proposed; see Appendix A item A8).")
