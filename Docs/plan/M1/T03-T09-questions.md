# M1 — questions, decisions and hand-over (T03 → T09)

You asked for T03 through T09 to be done in order without interrupting you, with every open question
collected here instead. This file is that collection. It is organised as:

1. **Decisions I made unattended** — each one is easy to reverse; say the word.
2. **Hand-over tasks only you can do** (editor, prefab or Inspector work).
3. **Verification I could not run** — so the claims stay honest.
4. **Status of each task**, including anything deliberately split out.

Last updated after **T06** (commit `8b8ff2e`).

---

## 1. Decisions I made unattended

| # | Decision | Why | What I would like confirmed |
| --- | --- | --- | --- |
| 1 | **Root spends the round.** A rooted player who tries to walk spends the turn (with a "Rooted" float) rather than getting a free refusal. | M1 has no wait key. A free refusal meant a web on an otherwise empty island could never tick down — a soft lock. | Is losing the round the right feel, or do you want a Wait action (space bar) so the refusal can be free again? |
| 2 | **`swordDrawn` was deleted**, not renamed. Keys: **1 = melee, 2 = bow**, both free. | Nothing read it once bump-attack moved out of `EntityManager.MoveEntity`, and `aiming` is the only real selection state. | Fine? The Player prefab still stores `swordDrawn: 1`; Unity ignores it. |
| 3 | **Roaming is per-round for every enemy.** All ~12 enemies take a turn each round, so a round costs ~0.75 s at the default 0.05 s step delay. | GDD 6.6 says unaware enemies wander one tile per round. | If combat feels sluggish, lower `turnStepDelay` on the TurnManager. Or should roaming be limited to enemies near the player? |
| 4 | **Plains is room-wide aggro; the detection-radius path is written and tested but unreachable** until desert/tundra content exists. | GDD 13.4. | Fine to leave the radius path dormant? |
| 5 | **Enemies respawn each floor; the player does not.** The player keeps health, level and inventory across floors. | GDD carries player state between floors; respawning the player also broke the singleton guards. | Confirm enemies resetting per floor is intended. |
| 6 | **Arrows start at 0** (`startingArrows` on PlayerStats, default 0). | The quiver is a count now; the recipe that fills it is not wired yet. | Set `startingArrows` on the prefab if you want a starting quiver (e.g. 10). Otherwise archery is unavailable until crafting lands. |
| 7 | **Chests keep what did not fit**: the chest stays solid and closed-looking until it is emptied, then it opens normally. | GDD 12.7 — nothing may be destroyed silently. | Does "come back with room" read clearly in play? |
| 8 | **`LootGen` refuses equipment** with a warning, whatever its serialized pools contain. | GDD 12.3: chests and corpses never drop weapons or armour. | If any scene pool intentionally held equipment, it will silently stop dropping — check `LootGen`'s pools in the scene. |
| 9 | **No `LootTable` ScriptableObject was created** (T06 deviation). `LootGen`'s existing pools stay, with the equipment guard added. | Two sources of truth for the same pools. The substantive requirement was the guard. | Want the SO anyway, for per-realm loot later? It is a clean follow-up. |
| 10 | **Ground drops were split out** (the plan's own escape hatch). A corpse drop that does not fit is warned about and lost; chest leftovers are retained. | It is the largest single addition in T06 and would have doubled the task. | Confirm the split, and that losing a corpse drop is acceptable until it lands. |
| 11 | **The `StatusController` is auto-attached** by `PlayerStats.Awake` when the prefab has none. | Adding a component to a prefab needs editor wiring; auto-attach keeps the rules working now. | Move it onto the Player prefab when convenient — nothing in code changes. |
| 12 | **Webs and ore veins reuse the bridge tile as a marker** (`TilePlacer.webTile` is a new optional field; leaving it empty falls back). | No web or ore art exists in the project. | Assign real tiles in the Inspector when art exists (T09). |

---

## 2. Hand-over tasks (editor / Inspector work)

Run these from the Unity menu, in this order:

1. **`Tools/Content/Create Material Drop Tables`** — creates `Assets/Resources/DropTables/Pig_yao.asset`,
   `Spider_yao.asset` and `Jing_yao.asset` with the percentages from GDD 13.7 (Tusk 30% / Skin 30%;
   Silk 40%, 1-2; Silk 15% / Iron 20%, 1-2). Materials are found by asset name first, then display name;
   anything missing is reported in the console rather than dropped from the table.
2. **`Tools/Content/Create Plains Realm Asset`** — creates `Assets/Resources/Realms/Plains.asset`
   (2 floors, room-wide aggro). Optional: without it the run uses the built-in fallback, which is
   equivalent except that the asset is where you tune later realms.
3. **`Tools/Content/Set Plains Realm Metal`** — sets `metal` to Iron Ore on that asset. Also optional:
   the built-in fallback loads Iron already.

**Recipes (T06 note 5) — not applied.** Recipes live inside `ItemDefinition` assets, which is
editor-authored YAML that must not be hand-edited. Set them in the Inspector, or ask me to add a menu
item that writes them through `SerializedObject`:

| Item | Recipe |
| --- | --- |
| Iron Sword | 5 Iron Ore |
| Composite Bow | 4 Iron Ore + 3 Spider Silk |
| Arrows ×10 | 1 Iron Ore (should call `PlayerStats.AddArrows(ArrowEconomy.ArrowsFromOre(1))`, not grant an item) |
| Studded Leather Armor | 3 Pig Yao Skin + 2 Iron Ore |
| Pouch | 3 Spider Silk |

Note the arrow recipe is special now: arrows are a count, not an item, so the crafting call site has to
route it to `PlayerStats.AddArrows`. That wiring belongs with T07 (portable crafting).

**Optional prefab work:** move `StatusController` onto the Player prefab; set `startingArrows`;
assign `TilePlacer.webTile` and a real ore-vein tile.

---

## 3. Verification I could not run

The editor would not launch in this session (`open -a` produced no process), so the bridge-only harnesses
are unverified since T03. Everything else was verified with batchmode runs.

| Harness | Last verified | Why it matters |
| --- | --- | --- |
| `MapgenDiagnostics.TurnPipeline` | T03 | The turn pipeline's own assertions; the PlayMode suite covers the same ground. |
| `MapgenDiagnostics.ChestWalkability`, `OpenRealChest` | T03 | The chest two-write path; T06 changed `Chest.Open`'s hand-over. |
| `MapgenDiagnostics.ConsumableBuff` | T03 | Buff durations; T03 changed when the tick happens. |
| `PlayDriver.Status`, `StepTowardNearestEnemy`, `OpenAdjacentChest` | T03 | PlayDriver now drives `Controls.TryMove` and `TryMineAdjacentVein`. |
| `PlayDriver.AuditEnemyRooms`, `Audit Bridge Tiles` | never this session | Roaming must not leave an island; the PlayMode suite asserts this. |

Please run these in a session with the editor open (the agent bridge works: `execute_method`, then read
`Temp/testrun-<Mode>.json` for test runs).

---

## 4. Task status

| Task | State | Notes |
| --- | --- | --- |
| T03 turn state machine | **done, committed** `5c41b35` | EditMode 39/39, PlayMode 8/8 at the time; `swordDrawn` deleted; also fixed the T02 spawn-chest mystery. |
| T04 awareness, roaming, LOS | **done, committed** `e156f48` | EditMode 56/56, PlayMode 12/12. Fixed a real cell-space bug in the aim overlay. |
| T05 statuses (Root) and webs | **done, committed** `de6f978` | EditMode 69/69, PlayMode 14/14. Only Root is applyable; the other four kinds are refused. |
| T06 materials, loot, arrows | **done, committed** `8b8ff2e` | EditMode 85/85, PlayMode 16/16. Ground drops split out; recipes and the loot-table SO deferred (see above). |
| T07 portable crafting | **done, committed** | EditMode 97/97, PlayMode 19/19. Discovery, the three panel states, out-of-combat gating and the ammo path are in; **the recipes themselves are still un-authored**, so the panel lists nothing until you do task 4 below. |
| T08 minimap and HUD | **not started** | Needs scene wiring; the plan is code-plus-menu-item. |
| T09 Plains content | **not started** | Mostly data and tuning. |

### T07 additions to the hand-over

- **The recipes are now the blocker.** T07 lists and gates recipes correctly, but every shipped item still
  has an empty `recipe`, so the crafting panel shows nothing and discovery has nothing to reveal. The table
  in task 3 above is the data. `ItemDefinition` now has `Recipe`, `ConsumeMaterials()` and `SetRecipe`, so
  a small menu item could write them through code if you prefer that to Inspector work — say so and I will
  add `Tools/Content/Set M1 Recipes`.
- **Arrows flow into the quiver automatically**: crafting an `ItemKind.Ammo` definition consumes the recipe
  and calls `PlayerStats.AddArrows(10)`. The `Arrow` definition already exists as Ammo
  (`Resources/ItemDefinitions/Consumables/Arrow.asset`), so only its 1 Iron Ore recipe is missing.

### Scoping note for T07-T09 (written before starting them)

- **T07 (portable crafting)** rewires `CraftingManager` to allow crafting anywhere, and the arrow recipe
  has to route to `PlayerStats.AddArrows` now that arrows are a count. The crafting *UI* is scene-wired,
  so it will be code plus a `[MenuItem]` wiring script, like the content menu items above.
- **T08 (minimap and HUD)** draws discovered islands, the stairs, the arrow count and the Root indicator.
  `RunState.HasStairs`/`StairsCell`, `PlayerStats.Arrows` and `StatusController.Active` are the hooks it
  needs; the HUD panel itself is scene wiring.
- **T09 (Plains content)** is the tuning pass: island sizes against GDD 5.2 (T01 deliberately left rooms
  at 11-19), enemy counts, `turnStepDelay` for roaming pacing, and the art assignments listed above.

If you would rather reorder, or want one of the deferred items (ground drops, the loot-table SO, the
recipes menu item) before T07, say so and I will switch.
