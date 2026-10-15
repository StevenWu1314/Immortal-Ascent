# 重构预案 — Rogue Asian Fantasy

> 范围：整个仓库（42 个 C# 文件 / 6,267 行 + 资源接线）。
> 方法：ponytail-audit（只找**过度工程与复杂度**，按可删行数从大到小排序）。
> 正确性 bug、安全、性能不在本预案内，另见文末「另案（不属于本次范围）」。
> 本预案**只列清单，不改代码**。

---

## 0. 结论摘要

这个代码库已经被人认真清理过一轮（注释里到处是「这替换了旧设计 X，因为 Y」），
所以**没有大块腐烂**。剩下的主要是三类：

1. **一次性 Editor 脚手架（437 行）**：产物（prefab、铭刻资产、场景接线）已经全部落库，脚本本身已死。
2. **并行重复的实现**：同一件事有两套（告警通路、寻路步进、受击闪烁、显示名字）。
3. **死配置 / 死字段 / 注释掉的代码 / 叙述性日志**：零散但总量可观。

净收益估算：**-716 行 C#（约 11%）**、**-2 个脚本文件**、**-约 350 个资源文件 / -约 7.5 MB**。

---

## 1. 发现清单（按可删体积排序）

```
delete: 两个一次性 Editor 脚手架已死。产物（Chest.prefab 已被场景 chestPrefab 引用、Sharp/Sturdy Rune 已在 chestLootPool、InscriptionPanel.prefab 已入场）全部落库，脚本无剩余职责。替代：无。 [Assets/Scripts/Item/Editor/ChestSetup.cs (187), Assets/Scripts/Item/Editor/InscriptionPanelSetup.cs (250)]
delete: TilePlacer.chest 字段的唯一读者是上面的 ChestSetup。替代：无。 [Assets/Scripts/Level Generator/TilePlacer.cs:13]
shrink: PlaceTiles 八个边界分支全都在做同一件事 grid.SetValueAtLocation(x,y,1)，可合成一个条件；同时 xsize/ysize/alternate 三个变量算完全程无人用。替代：if (x==xMin||x==xMax-1||y==yMin||y==yMax-1) → 1 ; else floor。 -47 行。 [Assets/Scripts/Level Generator/TilePlacer.cs:25-26,34,36-83]
shrink: branchingCorridors 的 right/left 分支各有 if(i!=length && i!=0){} else if(i==length){} else{}，三个分支体逐字相同；up 分支的 offset 机器与 xsize 判断（x+gridSize/2<0 恒为假）是死代码；三处 alternate 无人用。 -27 行。 [Assets/Scripts/Level Generator/TilePlacer.cs:119,125-135,145,152-165,178-198]
yagni: TurnManager 的「按房间广播警戒」通路（AllEnemies[]/rooms[]/numberofRooms/initialize() + callTaketurn 里的房间扫描）与 EnemyBehavior.checkForPlayerInRoom 的岛屿判定订阅同一个 onMoveEvent，做的是同一件事。替代：保留岛屿判定，删掉房间索引这一整套。 -51 行。 [Assets/Scripts/Managers/TurnManager.cs:14,16,18,39-47,80-87,100-121,124-127; Assets/Scripts/Level Generator/RoomFirstDungeonGenerator.cs:80-81,88,194-197]
delete: EnemyBehavior.room 字段只被生成器写过，从无读者（随上一条一起走）。替代：无。 [Assets/Scripts/Enemy/EnemyBehavior.cs:11; RoomFirstDungeonGenerator.cs:192]
reuse: RangeEnemyBehavior.chasePlayer 内联复制了 EnemyBehavior.NextSteps 的整段主导轴选择（基类注释还专门写了「Shared by chase and return, which each carried their own copy of this」）。替代：NextSteps(enemyCell, playerCell)。 -19 行。 [Assets/Scripts/Enemy/RangeEnemyBehavior.cs:32-51 → Assets/Scripts/Enemy/EnemyBehavior.cs:183]
delete: 注释掉的死代码：Grid 构造里的 Debug.DrawLine 循环与 createWorldText 调用、TilePlacer 的 8 行 walls.SetTile、Controls 的 printGrid、bowBehavior 的旋转实验、TurnManager 的 finishTurn 事件。替代：无。 -30 行。 [Assets/Scripts/GameGrid/Grid.cs:40-48,67-68; TilePlacer.cs:38,43,48,53,58,63,69,74; Player/Controls.cs:198-201; Item/bowBehavior.cs:15-18; Managers/TurnManager.cs:19,174]
delete: InscriptionEffect.OnHitDebuff / CostlyPower 两个未实现枚举值及其全部守卫与文案（IsInscriptionImplemented、InscriptionDescription 两分支、InscriptionService.CanApply 的拒绝分支）。仓库内 15 个 ItemDefinition 资产只用 0/1 两个值，phase 2 未启动。替代：无。 -20 行。 [Assets/Scripts/Item/ItemDefinition.cs:33-37,149-156,197-200; Item/InscriptionService.cs:71-77]
reuse: Enemy.DamageIndicator 与 PlayerStats.DamageIndicator 是同一个「依次换色再复原」协程的两份拷贝。替代：utilityFunction 里一个 FlashSprite(SpriteRenderer, Color[], float) 协程。 -14 行。 [Assets/Scripts/Enemy/Enemy.cs:29-43; Player/PlayerStats.cs:189-210]
delete: 12 处叙述性 Debug.Log（"queued up"/"taking turns"/"looping"/"chasing player"/"trying to find"/"trying to enable"/Debug.Log(center) 等），只报「我走到这了」。替代：无（保留所有 Warning/Error 与 Debug.Log($"Crafted…") 一类带数据的日志）。 -12 行。
delete: PlayerStats.getStrength/getDexterity/getMaxHealth 三个纯转发方法，后面就是 Strength/Dexterity/EffectiveMaxHealth 属性。替代：直接用属性（改 levelUpUI 三处调用）。 -9 行。 [Assets/Scripts/Player/PlayerStats.cs:300-313]
delete: levelUpUI 的 addButtons 序列化数组在 OnEnable 里被 GameObject.Find 三连覆盖，playerStats 同理；points<=0 / points>0 两个循环做同一件事。替代：一个循环 addButtons[i].interactable = points > 0；字段改非序列化。 -10 行。 [Assets/Scripts/Managers/levelUpUI.cs:8-9,21-25,42-56]
delete: CraftingManager.Instance 全仓库零读者（场景只按类型绑定 UnityEvent）。替代：无，删单例与 Awake 分支。 -8 行。 [Assets/Scripts/Managers/CraftingManager.cs:11-24]
delete: ItemDefinition.StatDisplayName 与私有 StatLabel 完全等价，之所以存在只是为了把枚举降级成字符串给 PlayerStats.increaseStatTemp(string) 用。替代：increaseStatTemp(BonusStat) + 已有 TempStatIndex switch，删掉字符串契约。 -12 行。 [Assets/Scripts/Item/ItemDefinition.cs:238-255; Player/PlayerStats.cs:365-393; Item/ItemUsage.cs:74-76]
delete: NewCustomRuleTile.InvalidateTilemapCache 零调用者。替代：无。 -6 行。 [Assets/Tilesets/NewCustomRuleTile.cs:53-57]
delete: RangeAttackTilemap 的 prevPos 哨兵（-999999）已被 lastHighlighted 取代，首次 SetTile(prevPos,…) 是冗余写入。替代：lastHighlighted.Value + overlayTile。 -4 行。 [Assets/Scripts/Player/RangeAttackTilemap.cs:14,22,38-39]
shrink: EntityManager.MoveEntity 内部已经写过 transform.position，4 个调用点紧接着又写一遍同样的值。替代：删调用点的重复赋值。 -4 行。 [Assets/Scripts/Enemy/EnemyBehavior.cs:231-232,272-273; Enemy/RangeEnemyBehavior.cs:127-128,135-136 → Managers/EntityManager.cs:61]
yagni: TilePlacer.flowerDensity 从不生效：Random.Range(0,1) 整数版恒为 0，所以它是 0/非 0 的开关而不是「密度」。替代：Random.value < flowerDensity（1 个词；同时这修掉一个正确性问题，见另案）。 [Assets/Scripts/Level Generator/TilePlacer.cs:15,21]
delete: TurnManager 的 [SerializeField] 挂在 Queue<>/HashSet<> 上，Unity 根本不序列化这两个类型，Inspector 里永远是空的。替代：普通字段。 (0 行，但去掉一个假的可调项) [Assets/Scripts/Managers/TurnManager.cs:10-11]
delete: Grid.Move 的 entity 参数无使用者（注释自己写着 retained for call-site compatibility），唯一调用点是 Controls。替代：删参数。 -1 行。 [Assets/Scripts/GameGrid/Grid.cs:110; Player/Controls.cs:167]
shrink: utilityFunction.ClearLineLow / ClearLineHigh 是 public，但只被同文件的 ClearLine 调用。替代：private。 (0 行，收窄 API) [Assets/Scripts/utilities/utilityFunction.cs:59,105]
```

**net: -716 行 C#（-11%），-2 个脚本文件，-0 个依赖。** 另有资源侧 **-约 350 个文件 / -约 7.5 MB**（见 §4）。

### 资源侧（同样按体积）

```
delete: Assets/TextMesh Pro/Examples & Extras 的示例场景/脚本/贴图（249 个跟踪文件、6.1 MB）。仓库三个场景只引用其中 1 个字体 Electronic Highway Sign SDF。替代：把该字体移到 Assets/Resources/ 后整目录删除。 [Assets/TextMesh Pro/Examples & Extras/]
delete: 存档态的美术迭代目录，35 个资产、0 引用。替代：无。 [Assets/gameHUD/custom-game-hud-assets/archive/ (hp-bar, hp-bar-v2, exp-bar)]
delete: 名字就叫 "copy" 的重复 tileset，14 个资产，场景/prefab/Resources 零引用。替代：无。 [Assets/Tilesets/bridge-tiles/updated-bridge-tiles copy/]
delete: 未被任何场景引用的样例瓦片，101 个资产、804 KB。替代：无。 [Assets/Tilesets/sample tiles/]
delete: 空目录（git 不跟踪，但会污染 Project 视图与搜索）。替代：无。 [Assets/testing scripts for map gen/, Assets/CastVisualizer/, Assets/Tilesets/testing-tiles/, Assets/Scripts/Item/Equipments/Weapon/, .../Armor/, Assets/Tilesets/sample tiles/New folder/]
```

### 文档侧

```
delete: Docs/InscriptionSystem.md 是「三选一方案」的历史决策文档，方案 B 已实现落地。替代：无，或压成一节并入 ItemSystem.md。 [Docs/InscriptionSystem.md (16.8 KB)]
delete: Docs/ItemSystem.md §3c（Collectables / PerlinNoiseMap / Iron.prefab 全部已不存在）、§8、§9（说 Tools/Items 步骤 1-3 已死、ItemMigration.cs 可删 —— 那批脚本早已删除，现在是步骤 5、6 成为死脚手架）。替代：按当前代码重写这三节。 [Docs/ItemSystem.md:117-129,225-268]
```

---

## 2. 分阶段执行预案

原则：**先删零风险的死物，再动结构，最后清资源**。每一阶段结束都能单独提交、单独回滚、单独验收。

### Phase 0 — 基线（30 分钟，必须先做）

| 步骤 | 动作 |
|---|---|
| 0.1 | 确认干净工作区：`git status` 无未提交改动（当前仓库有未跟踪的 .csproj/.DS_Store，先确认它们本来就被 .gitignore 忽略，不要顺手提交）。 |
| 0.2 | 打开 `Assets/Scenes/Branching dungeon.unity`，Play 一次，记录基线：能移动、能进第二间房、敌人会追、弓能瞄准能射、宝箱能开、升级面板能加点、合成面板能出按钮。 |
| 0.3 | 记下 Console 里现有的 Warning/Error 数量，作为后续对比基线。 |
| 0.4 | 打标签：`git tag pre-refactor`。 |

### Phase 1 — 纯删除（零行为变化，占总量约 65%）

改动：清单里的 **delete / shrink 死代码** 项 —— 两个 Editor 脚手架、TilePlacer 死变量与恒等分支、注释代码、叙述性日志、无调用者的方法与字段、死枚举值。

- 2 个一次性 Editor 工具 + `TilePlacer.chest`：注意先从 `Branching dungeon.unity` 的 TilePlacer 组件上看一眼 `chest` 槽位（删字段后该槽位会消失，这是预期的）。删除前用 Unity 打开一次场景确认 Chest/Prefab/铭刻资产都还在、`chestPrefab` 与 `chestLootPool` 槽位仍被引用 —— 已在本仓库核实：`Chest.prefab` 被 `chestPrefab` 引用，`Sharp Rune`/`Sturdy Rune` 在 `chestLootPool` 里，`InscriptionPanel.prefab` 已在场景中。**它们的产物不能一起删。**
- 删注释代码、日志、无调用者 API：无风险。
- `Shrink` 项（TilePlacer 条件合并、levelUpUI 循环合并）**逐项单独提交**，因为它们虽然等价，但等价性需要靠「重新生成一次地牢」来验。

验证：编译通过 + 重新生成一次地牢（Inspector 的 "Create Branching Dungeon" 按钮，这个按钮是 `EditorDungeonGenerator.cs`，**不要删**），对比生成结果与 Phase 0 一致（房间数、可达性日志、宝箱数）。

预估：**-620 行**。

### Phase 2 — 结构性去重（需要试玩验收，占约 12%）

按风险从低到高：

1. `RangeEnemyBehavior` 改用 `NextSteps`（低风险，纯替换，行为逐字等价）。
2. 抽公共 `FlashSprite` 协程给 Enemy / PlayerStats 共用（低风险，视觉）。
3. `PlayerStats` 三个转发方法改属性调用（低风险）。
4. `increaseStatTemp(string)` → `increaseStatTemp(BonusStat)`（中风险：会动到消耗品 buff 的数值路径，必须实测 StrPill / DexPill / ConPill 三种药丸的 buff 与到期回退）。
5. **TurnManager 房间告警通路**（中高风险，本预案里唯一有行为语义差异的一项）：
   - 差异点：现有 `callTaketurn` 广播无视敌人状态，即使在 `Return`（回家）途中也会被重新拉成 `Chase`；而 `checkForPlayerInRoom` 只在 `Idle` 时触发，且一旦 `becomeAlerted` 就退订。
   - 因此验收要专门测：**引一只敌人追出来 → 走远让它进入 Return → 再靠近**，观察它是否还能重新追击。如果这个行为是你要的，就把「Return 中可被重新唤醒」显式写进 `checkForPlayerInRoom`（去掉 `currentstate != state.Idle` 的早退，或允许 Idle/Return），再把 TurnManager 那套删掉；如果不要，直接删。
   - 收益：`TurnManager` 少 3 个公有字段 + `initialize()` + 一段双循环，生成器少 5 行注册，`EnemyBehavior` 少 1 个字段。

验证：完整走一遍 Phase 0.2 的清单，重点测**敌人警戒**、**Return 行为**、**每回合队列**（杀一只后其余敌人仍能行动 —— 这条已有 `UnregisterEnemy` 兜底，别误删）。

预估：**-96 行**。

### Phase 3 — 资源清理（与代码解耦，可随时做）

1. 先把 `Electronic Highway Sign SDF.asset`（`Scenes/ui.unity` 唯一引用）移到 `Assets/Resources/Fonts/`，在 `ui.unity` 里重新指一次字体，确认文字仍正常。
2. 删 `Assets/TextMesh Pro/Examples & Extras/`（-249 文件 / -6.1 MB）。
3. 删 `gameHUD/custom-game-hud-assets/archive/`（-67 文件）、`Tilesets/bridge-tiles/updated-bridge-tiles copy/`（-28）、`Tilesets/sample tiles/`（-201）。
4. 删空目录。

验证：Unity 重新导入后 Console 无 missing script / missing reference；三个场景各 Play 一次。

### Phase 4 — 文档对齐

删 `Docs/InscriptionSystem.md`（或压成一节），按当前代码重写 `Docs/ItemSystem.md` §3c/§8/§9。
顺手修掉两处指向已删除工具的注释：`Controls.cs:175`（"Tools > Diagnose Map Walkability" 不存在）、`RoomFirstDungeonGenerator.cs:49-52` 与 `LevelValidation.cs:12`（`MapgenDiagnostics` 不存在）。

### Phase 5 — 命名与拖尾卫生（可选，0 行收益）

不属于过度工程，但会在每次重构里持续咬人，建议一次做完：

| 现状 | 建议 |
|---|---|
| `Assets/Scripts/Managers/UI Manager.cs` | 改 `UIManager.cs`（文件名带空格会让部分工具链出问题） |
| `ProceduralGeneraltionAlgorithm.cs` / `BrachingRooms` / `roomsQue` | 拼写：`ProceduralGenerationAlgorithm` / `BranchingRooms` / `roomsQueue` |
| `levelUpUI.cs`（类名小写开头） | `LevelUpUI.cs` |
| `Managers` 与 `utilities` 目录命名风格不一致 | 统一 PascalCase：`Utilities` |
| `Enemy/JingYao.cs`、`PigYao.cs` 两个空子类 | 只有 prefab 需要类型时才保留；若要删，必须先在 `Prefabs/Enemies/*.prefab` 上换成 `Enemy` 的具体化版本，属于资产改动，风险高于收益，**默认不动** |

---

## 3. 不要"顺手"改的地方（属于别的 review pass）

审计时撞见的正确性问题，**不在本预案范围**，请单开一次正确性 review：

1. `TilePlacer.RandomFloorTile`：`Random.Range(0, 1)` 整数版恒为 0，`flowerDensity` 实际是布尔开关（见 §1 的 yagni 项，一行改 `Random.value`）。
2. `EntityManager.MoveEntity` 里塞了战斗判定（`swordDrawn` → `attack("melee")`），"移动"这个动作会改血量；移动失败时还 `Debug.Log` 而非返回 bool。
3. `ProceduralGeneraltionAlgorithm.BrachingRooms`：`overlap = test.position == branchedRoom.position;` 在循环里被反复覆写，一旦某轮为 false 就把前一轮的 true 冲掉；`Random.Range(1, numberBranch)` 的用法也接近越界。
4. `RangeEnemyBehavior.chasePlayer` 的三层 if/else 里，`rangeAttackCooldown` 有两条路径被减到 0 以下，且 `lastAttackPos` 用了 `(-10000,-10000)` 哨兵（与 §1 里已删的 `RangeAttackTilemap` 哨兵是同一个坏习惯）。
5. `PlayerStats.takeDamage` 里 `Health <= 0 → LoadScene(0)`：场景索引硬编码。
6. `TurnManager.callTaketurn` 用 `StartCoroutine("TakeTurns")` 字符串启动；`PlayerStats.Instance.gameObject.GetComponent<Controls>()` 这类每回合的组件查找。

---

## 4. 汇总

| 阶段 | 内容 | 行数/文件 | 风险 | 验收 |
|---|---|---|---|---|
| 0 | 基线 + tag | — | 无 | Play 一遍 |
| 1 | 纯删除 | -620 行，-2 脚本 | 极低 | 编译 + 重生成地牢 |
| 2 | 结构性去重 | -96 行 | 中 | 试玩：警戒/Return/回合/药丸 |
| 3 | 资源清理 | -约 350 文件，-约 7.5 MB | 低 | 三场景 Play + Console 无 missing |
| 4 | 文档对齐 | -约 400 行 Markdown | 无 | 人工读一遍 |
| 5 | 命名卫生 | 0 行 | 低 | 编译 |

**net: -716 行 C#（-11%），-2 个脚本文件，-0 个依赖；资源另 -约 350 文件 / -约 7.5 MB。**

不在本次范围：正确性 bug（§3）、性能（每帧 `FindObjectOfType`、`GameObject.Find`、`GetComponent` 热路径）、安全。
