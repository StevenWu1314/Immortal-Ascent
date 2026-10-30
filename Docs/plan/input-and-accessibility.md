# Input and accessibility: full playability with one device

**Status:** decisions taken with the designer, nothing implemented yet.
**Goal:** every action in a run is reachable with the mouse alone *or* the keyboard alone. No action may
require the other device.

This is not part of M1. It sits on top of the finished T03-T09 systems (turn pipeline, aiming, panels).

## 1. Decisions taken

| # | Question | Decision |
| --- | --- | --- |
| 1 | Aiming with the keyboard | `A` / `D` cycle the locked target, `Space` fires, `Esc` cancels |
| 2 | Wait key | `Enter` / `Return`. Root does **not** depend on it: attempting to move already spends the round (contract 10), so the wait key is for baiting and repositioning, not to escape Root |
| 3 | Click-to-move in combat | real turn-based pacing: one step, wait for the enemy phase, then the next step |
| 4 | Clicking an enemy | walk until adjacent, then bump-attack |
| 5 | Clicking a chest, ore vein or stairs | walk to an adjacent cell, then interact |
| 6 | Clicking an unreachable cell | no reaction |
| 7 | Inventory navigation | two-dimensional grid, `WASD` |
| 8 | Crafting list | every listed entry is selectable, including greyed-out and locked ones (so the player can read what they are missing) |
| 9 | Inscription panel | three steps: pick the equipment, pick the slot, confirm |
| 10 | Inscription hotkey | none. Select the rune in the inventory and press `Return` - the panel opens from there |
| 11 | Key bindings | a rebindable data asset, not code constants |

## 2. What is missing today

| Action | Keyboard | Mouse |
| --- | --- | --- |
| Move | yes (`WASD`) | **no** - there is no click-to-move |
| Melee attack | yes (bump) | **no** - needs click-to-move |
| Ranged attack | **no** - targeting is mouse only | yes (aim + click) |
| Interact (chest, ore, stairs) | yes (`E`) | **no** |
| Use a pill | yes (`Q`) | **no** - the HUD slot is not clickable |
| Open a panel | **no** - no hotkeys | yes (buttons) |
| Navigate a panel | **no** | yes |
| Close a panel | **partial** | yes |
| Wait | **no** | **no** |

So both single-device runs are currently blocked, in opposite directions.

## 3. Work, in four independently verifiable phases

### P1 - Keyboard UI (no pathfinding, no turn code)

- Hotkeys to open and close: `I` inventory, `C` crafting, `Esc` close. Inscription has no hotkey (see #10).
- Two-dimensional focus for the inventory grid (`WASD`), a single selectable list for crafting (all entries),
  and the three-step inscription flow driven by focus + `Return` / `Esc`.
- A visible focus marker on the selected widget, and a scroll that keeps it on screen.
- Every panel needs a mouse-reachable close button for the mouse-only path (audit the four panels).

**Acceptance:** open, navigate and close every panel with the keyboard alone; and with the mouse alone.

### P2 - Keyboard aiming + the lock arrow

- Enter aiming with the existing bow key; `A` / `D` cycle candidate targets, `Space` fires, `Esc` cancels.
- Candidates: in range, clear line of sight, **and on the player's island** (the rule added with the
  corridor-sniping fix). Cycle order is stable: by angle from straight up, clockwise.
- **The locked enemy gets an arrow above it** (the designer's note: the mouse shows a ground reticle, the
  keyboard shows a marker over the target). Build it as a scene object through a `[MenuItem]`, per the
  UI-in-scene rule; no targets means a clear "no target" message instead of silence.

**Acceptance:** fire at every enemy in a room using only `A`, `D`, `Space`; the marker always names the
enemy that would take the shot.

### P3 - Click-to-move (the largest piece)

- A pure `GridPath.NextStep(from, to, canStep)` BFS over the terrain grid, unit-tested on fixed maps.
- A `ClickToMove` driver that takes one step per turn, waits for `takingTurn` to clear in combat, and
  recomputes the path after every enemy phase.
- Interruptions: damage taken, the target dying, any movement key, a menu opening, or the island gate
  refusing the next step. Clicking an unreachable cell does nothing (#6).
- Island gating must be applied **per step** through `IslandManager.CanMoveTo`, with the existing message
  when the room is still occupied - never by planning a route through a doorway.
- Clicking an enemy walks until adjacent and then bump-attacks (#4); clicking a chest, vein or stairs walks
  to an adjacent cell and interacts (#5).

**Acceptance:** clear a floor with the mouse alone; in combat each click costs exactly one turn per step.

### P4 - Mouse interaction, wait key, bindings asset

- Clickable pill slot in the weapon HUD corner (uses the same `ItemUsage` path as `Q`).
- Hover highlight on the tile under the cursor (walkable / interactive / blocked).
- `Return` waits one turn in the world, and confirms inside a panel that has focus.
- `InputBindings` data asset plus one reader in `Controls`; every new key goes through it. This keeps the
  later move to the new Input System (and the open Unity 6 question) to a single file.

**Acceptance:** a full run with the mouse only, and a full run with the keyboard only.

## 4. Risks to design for, not to discover

- **Turn economy.** Click-to-move must never batch steps in combat. Same class of bug as the old double
  attack and the corridor sniping: any path that lets a click act twice per round is wrong.
- **Single-flight phase.** The driver relies on `TurnManager.IsEnemyPhaseRunning` / `Controls.takingTurn`;
  do not clear those from the driver.
- **Island gate.** Applied per step, never wholesale.
- **Menus vs world.** `MenuIsOpen` must block click-to-move, aiming and the wait key, and the HUD hides while
  a panel is open (already the case).
- **Fog.** Clicking unexplored terrain is out of scope until fog is wired; the pathfinder sees the grid.

## 5. Out of scope

- Gamepad support.
- Migrating to the new Input System, and the Unity 6 upgrade. Both stay open; the bindings asset is written
  so the migration is local.
- Any new combat rules: this work adds input paths, not mechanics.

## 6. Verification

- EditMode: `GridPath` on fixed grids (reachable, blocked, unreachable, island boundary, no diagonal).
- PlayMode: click-to-move pacing (one turn per step), interrupt on damage, keyboard aim cycling and firing,
  panel focus navigation.
- Manual: one run with only the mouse, one with only the keyboard. Neither can be proven by tests.
