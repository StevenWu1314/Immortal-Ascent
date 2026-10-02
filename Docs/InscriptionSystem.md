# 铭刻系统设计方案（3 个候选）

> 状态：**设计提案，未实现**。本文只做方案对比，不含代码改动。
> 相关既有文档：[ItemSystem.md](ItemSystem.md)（`ItemDefinition` + `ItemStack` 物品系统）

---

## 0. 现状与硬约束

在看方案之前，有四件事决定了所有方案的形状。前两件是**必须先解决的阻塞项**。

### 0.1 宝箱目前完全不可交互（阻塞）

`TilePlacer.generateLootTiles()`（[TilePlacer.cs:445-459](../Assets/Scripts/Level%20Generator/TilePlacer.cs#L445-L459)）为每个房间按 50% 概率做这些事：

```csharp
int x = Random.Range(room.xMin+3, room.xMax-3);
int y = Random.Range(room.yMin+3, room.yMax-3);
grid.SetValueAtLocation(x, y, 99);          // 99 = 宝箱，不可行走
floor.SetTile(new Vector3Int(x, y), chest); // 只是画了张瓦片
```

**全项目没有任何代码读取地形码 99，也没有任何宝箱交互。** 之前存在的 `Grid.chestOpen` 事件没有任何触发者（死代码），已随 BUG-12 删除。所以「铭刻道具从宝箱掉落」需要先实现宝箱交互，见 [§5](#5-无论选哪个方案都要先做的事)。

### 0.2 敌人没有状态系统，且有两套并行组件（阻塞）

「让武器施加负面效果」是四类效果里唯一需要**改敌人**的，但目前：

- 敌人身上有**两个平级组件**：`Enemy`（生命/伤害/死亡，[Enemy.cs](../Assets/Scripts/Enemy/Enemy.cs)）和 `EnemyBehavior`（AI/回合，[EnemyBehavior.cs](../Assets/Scripts/Enemy/EnemyBehavior.cs)），它们不是继承关系，靠 `GetComponent` 互相找。
- **没有任何状态/持续效果系统**。`Enemy.takeDamage(int)` 只做 `health -= damage`。
- `turn()` 在 `EnemyBehavior` 和 `RangeEnemyBehavior` 里**各覆写了一份**，任何「跳过回合/眩晕」判定都要写两遍，否则远程敌人免疫。

所以「施加负面效果」的代价远高于其他三类，必须单独设计。

### 0.3 `ItemDefinition` 是不可变共享资产 —— 铭刻的落点是核心分歧

上一批重构刻意让 `ItemDefinition` 成为**共享、不可变**的 ScriptableObject：背包里两把「铁剑」共享同一个 asset，只有 `ItemStack.amount` 是实例状态。

这意味着：**铭刻不能存在 `ItemDefinition` 上**。如果存上去，A 剑的铭刻会同时出现在 B 剑上，而且在编辑器里会写回磁盘资产。

三个方案的**根本区别**就是铭刻挂在哪：

| | 铭刻挂在 | 「铭刻在这把剑上」是否成立 |
|---|---|---|
| 方案 A | `Equipments` 的**装备槽** | ❌ 换武器铭刻会跟着槽走 |
| 方案 B | 新引入的**装备实例** | ✅ 真正属于那一件 |
| 方案 C | **玩家**身上（按槽位分类） | ❌ 本质是玩家天赋 |

### 0.4 现有可用的钩子

| 位置 | 作用 |
|---|---|
| [PlayerStats.attack](../Assets/Scripts/Player/PlayerStats.cs#L69-L103) | 玩家造成伤害的**唯一收口**（近战+远程都走这里）→ 「命中时施加效果」挂这里 |
| [PlayerStats.takeDamage](../Assets/Scripts/Player/PlayerStats.cs#L104-L115) | 玩家受伤收口 → 「代价类」效果挂这里 |
| [PlayerStats.increaseStatTemp](../Assets/Scripts/Player/PlayerStats.cs#L235-L262) | 现有 3 种限时 buff（力/敏/最大生命），带 `temporaryStatAdjustments[3,2]` |
| [Equipments](../Assets/Scripts/Player/Equipments.cs) | 持有 `meleeWeapon` / `rangeWeapon` / `armor` 三个 `ItemDefinition` 引用 |
| [Enemy.takeDamage](../Assets/Scripts/Enemy/Enemy.cs#L21-L31) | 敌人受伤收口 |
| [CollectableMap](../Assets/Scripts/Level%20Generator/CollectableMap.cs) | 格子→内容的注册表，是宝箱容器的现成范式 |

---

## 方案 A — 轻量插槽式

**一句话**：铭刻是普通 `ItemDefinition`，使用时写进 `Equipments` 的三个槽位；改动最小，能最快跑起来。

### 数据模型

```csharp
// 复用现有物品系统，只加一个 Kind
enum ItemKind { Material, Weapon, Armor, Ammo, Consumable, Inscription }

// 铭刻自己的效果载荷（加在 ItemDefinition 上）
enum InscriptionEffect {
    OnHitDebuff,        // 命中时给敌人负面效果
    StatBonus,          // 纯加数值
    PassiveBuff,        // 给使用者常驻正面效果
    CostlyPower         // 强力但有代价
}
```

`Equipments` 增加三个槽：

```csharp
[SerializeField] private ItemDefinition meleeInscription;
[SerializeField] private ItemDefinition rangedInscription;
[SerializeField] private ItemDefinition armorInscription;
public ItemDefinition GetInscription(ItemKind slot);   // 供结算读取
```

### 应用流程

1. 背包里右键铭刻 → 需要选目标。
2. **为避免新建 UI**，铭刻定义上带 `InscriptionTarget { MeleeWeapon, RangedWeapon, Armor, AnyWeapon, Any }`；目标是具体槽就直接应用，是 `AnyWeapon`/`Any` 才弹一个两按钮选择框。
3. 应用后铭刻从背包扣除，写入对应槽位。

### 宝箱投递

格子容器（推荐做法见 [§5.1](#51-宝箱交互两种做法)）：新增 `ChestMap`，生成时 roll 内容并注册；玩家走到相邻格按 `E` 开箱，内容直接 `Inventory.addItem()`。

### 效果结算

- **OnHitDebuff**：`PlayerStats.attack` 在 `target.takeDamage(...)` 后，查当前武器槽的铭刻，调 `target.GetComponent<EnemyStatus>().Apply(...)`。
- **StatBonus**：`Equipments` 变化时重算，写进 `PlayerStats`（需要引入「基础值 + 加值」分离，见风险）。
- **PassiveBuff**：同上，常驻。
- **CostlyPower**：`PlayerStats.takeDamage` 里叠加额外伤害；`attack` 里放大伤害。

### 四类效果示例

| 效果 | 铭刻名 | 机制 |
|---|---|---|
| 负面效果 | 淬毒之铭 | 命中时敌人中毒，每回合 −2 生命，持续 3 回合 |
| 纯加数值 | 锐利之铭 | 该武器伤害 +5 |
| 常驻正面 | 坚韧之铭 | 装备期间最大生命 +20 |
| 带代价 | 血契之铭 | 伤害 +50%，但玩家每回合 −1 生命 |

### 评价

| | |
|---|---|
| 工作量 | **小**（约 2–3 天；不含宝箱与敌人状态系统） |
| 优点 | 复用现有物品系统，改动集中在 `Equipments` + `attack`；最快可玩 |
| 代价 | 铭刻属于**槽位不属于物品**。卸下铁剑换木剑，铭刻立刻作用到木剑上。想做「这把剑带毒，那把不带」是不可能的 |
| 破坏性 | 低，不碰资产文件（`Equipments` 加字段安全） |

---

## 方案 B — 装备实例化（结构上正确）

**一句话**：引入「装备实例」层，铭刻真正属于那一件装备；最符合直觉，但改动最大。

### 数据模型

```csharp
[Serializable]
public class EquipmentInstance {
    public ItemDefinition definition;
    public List<ItemDefinition> inscriptions = new();
}

// Equipments 改为持有实例
[SerializeField] private EquipmentInstance meleeWeapon;
[SerializeField] private EquipmentInstance rangeWeapon;
[SerializeField] private EquipmentInstance armor;
```

背包里**装备类**物品改用实例存储（装备 `stackLimit` 恒为 1，与现有 `ItemStack` 不冲突，可以并存）：

```csharp
[Serializable] public class EquipmentStack {   // 与 ItemStack 并列
    public EquipmentInstance instance;
    public int amount;                          // 装备恒为 1
}
```

铭刻本身仍是 `ItemDefinition`（`ItemKind.Inscription`），使用时附加到选中的实例上。

### 应用流程

1. 背包右键铭刻 → 弹出目标选择（遍历背包中的装备实例 + 当前已装备的实例）。
2. 选中实例 → `instance.inscriptions.Add(inscription)`，铭刻从背包扣除。
3. 若目标实例当前已装备，立即重算效果。
4. 需要一个「装备详情」UI 显示已附铭刻（可先只做文本列表）。

### 宝箱投递

同方案 A 的格子容器；或改用实体宝箱 prefab（表现更好，但需要新建 prefab 与场景接线）。

### 效果结算

`Equipments.GetActiveInscriptions()` 汇总三件装备实例上的全部铭刻；其余同方案 A。

### 评价

| | |
|---|---|
| 工作量 | **大**（约 1–2 周；且需要武器/防具 UI 与场景接线配合） |
| 优点 | 唯一让「铭刻属于这件装备」成立的方案；可支持比较、替换、丢弃、将来的交易/掉落；是长期正确的结构 |
| 代价 | 要动 `Equipments`、`Inventory`、`ItemBox`、`ItemDisplay` 以及装备的拾取/掉落路径；`Recipe.result` 产出的也必须是实例而非共享定义，牵扯合成与 `LootGen` |
| 破坏性 | 高，会改动已迁移的物品数据结构；`Equipments` 的 3 个序列化字段类型会变，需要一次迁移 |

> 注意：方案 B 会让「装备」和「可堆叠消耗品」在 `Inventory` 里走两套存储。这是**有意为之**（装备需要实例身份），但必须明确 `getItems()` 之外的接口边界，否则 UI 层会分裂成两套渲染逻辑。

---

## 方案 C — 玩家侧常驻铭刻（折中，推荐先做）

**一句话**：铭刻使用时被「消耗掉」，效果**常驻在玩家身上**并按武器/防具槽分类；规避了共享资产与实例化的两难。

### 数据模型

```csharp
[Serializable]
public struct ActiveInscription {
    public ItemDefinition inscription;
    public ItemKind boundSlot;   // Weapon / Armor
}

public class PlayerInscriptions : MonoBehaviour {
    [SerializeField] private List<ActiveInscription> active = new();
    public void Apply(ItemDefinition inscription, ItemKind slot);
    public IReadOnlyList<ActiveInscription> ForSlot(ItemKind slot);
}
```

铭刻仍是 `ItemDefinition`（`ItemKind.Inscription`），用后从背包扣除。

### 应用流程

右键铭刻 → 选「武器 / 防具」（两个按钮，或定义上预设 `boundSlot`）→ 写入 `PlayerInscriptions`。**不需要遍历背包、不需要实例比较**，UI 最简。

### 宝箱投递

同方案 A。

### 效果结算

- **OnHitDebuff**：`PlayerStats.attack` 判断「当前用的是武器槽」→ 取 `ForSlot(Weapon)` 里的 OnHit 铭刻结算。**天然满足「可以被施加在任何武器上」**——换任何武器都生效。
- **StatBonus / PassiveBuff**：在 `PlayerInscriptions` 变更时调 `PlayerStats.Recalculate()`。
- **CostlyPower**：挂在 `takeDamage` / 每回合 tick。

### 语义澄清

这一方案的本质是**「玩家天赋」而不是「装备铭刻」**。用户的原始需求「道具可以被施加在任何武器和防具上」在方案 C 下被解读为「作用域是某个装备槽，而非某一件具体装备」——这在 roguelike 里其实是常见且合理的设计（例如《杀戮尖塔》的遗物），但**如果你们要的是「这把剑带毒、那把不带」，方案 C 做不到**。

### 评价

| | |
|---|---|
| 工作量 | **中**（约 3–5 天；不含宝箱与敌人状态系统） |
| 优点 | 完全绕开共享资产与实例化冲突；不需要新 UI 骨架；卸装/换装不会丢效果，玩家体验稳定；后续想升级到方案 B 时，这层数据可以直接迁移 |
| 代价 | 铭刻不在物品上；「把铭刻从剑上取下来」这类操作无从表达 |
| 破坏性 | 低，纯新增组件与字段 |

---

## 对比

| | A 插槽式 | **B 装备实例** | **C 玩家常驻** |
|---|---|---|---|
| 铭刻归属 | 装备槽 | 装备实例 | 玩家（按槽分类） |
| 「这把剑带毒」 | ❌ | ✅ | ❌ |
| 换装后效果 | 跟随槽位 | 跟随物品 | 跟随玩家 |
| 工作量 | 2–3 天 | 1–2 周 | 3–5 天 |
| 新 UI 需求 | 目标选择框 | 目标选择 + 装备详情面板 | 两个按钮 |
| 需要数据迁移 | 否 | 是（`Equipments` 字段类型） | 否 |
| 可升级到 B | 需重做落点 | — | 数据可直接迁移 |
| 风险 | 语义误导 | 引入两套背包存储 | 语义偏离「装备铭刻」 |

---

## 5. 无论选哪个方案都要先做的事

### 5.1 宝箱交互（两种做法）

**做法 1 — 格子容器（推荐，纯代码）**

照抄 `CollectableMap` 的范式新增 `ChestMap`：`Dictionary<Vector2Int, List<ItemDefinition>>`。

- `TilePlacer.generateLootTiles` 生成宝箱时，同时 roll 内容并 `ChestMap.Instance.Register(cell, contents)`。
- `Controls` 在移动后检测**四邻**是否有未开启宝箱；有则按 `E` 开箱（现在是纯 WASD + 鼠标，`E` 空闲）。
- 开箱 → 逐个 `Inventory.addItem()`，用返回值报告放不下的部分 → 标记已开启。

优点：不需要新 prefab、不需要场景接线，完全在代码内可验证。
缺点：宝箱仍然是瓦片，没有开箱动画/特效。

**做法 2 — 实体宝箱 prefab**

生成时 `Instantiate(chestPrefab, cell)`，prefab 带 `Chest` 组件 + 碰撞体 + 动画。

> **重要**：即使改成实体，也应**保留 `grid=99`**。地形码是移动的唯一权威（这是 BUG-07 的方向），宝箱实体只负责表现与交互。不要让它反过来承担阻挡语义，否则又会出现两套真相源。

缺点：需要新建 prefab 并接线（受约束 #1 限制，需要人工在编辑器完成）。

### 5.2 敌人状态系统（仅 OnHitDebuff 需要）

建议引入 `EnemyStatus : MonoBehaviour`，与 `Enemy` 同物体：

```csharp
public void Apply(StatusKind kind, int magnitude, int turns);
public bool ShouldSkipTurn { get; }
public int DamageModifier { get; }
public void Tick();     // 每回合结算 DoT / 递减
```

必须处理的两个坑：

1. **`takeTurn()` 有两份实现**（`EnemyBehavior` 与 `RangeEnemyBehavior`）。眩晕/跳回合判定要么抽到基类，要么两处都加——只改基类会导致远程敌人免疫。
2. **DoT 的结算时机**：`Enemy.takeDamage` 会触发 `Die()`，而 `Die()` 会 `Destroy`。DoT 结算必须走同一条 `Kill(true)` 路径，否则会绕过掉落与 `EntityManager` 注销（这正是 BUG-11 修过的坑）。

### 5.3 「纯加数值」与「常驻正面效果」需要基础值/派生值分离

`PlayerStats` 目前直接在 `strength` / `MaxHealth` 等字段上做加减（`increaseStrength()` 就是 `strength++`）。已有 `ApplyAchievementBuffs()` 在 `OnEnable` 里重复叠加永久属性的 bug（BUG-13，未修）。

铭刻要加常驻加值，**不能**沿用这种「直接改字段」的做法，否则装卸装备会反复叠加。必须先做：

```csharp
int BaseStrength;      // 存档值
int StrengthBonus;     // 装备 + 铭刻 + 成就的派生加值
public int Strength => BaseStrength + StrengthBonus;
```

这一项建议**无论选哪个方案都先做**，否则铭刻一定会踩到和 BUG-13 同一类的重复叠加问题。

---

## 6. 建议

**先做方案 C，并把 5.3 的基础值/派生值分离一起做掉。**

理由：

1. 方案 C 完全绕开「`ItemDefinition` 不可共享」这个结构性冲突，不需要数据迁移，破坏性最低。
2. 它天然满足需求里最关键的一条——**「道具可以被施加在任何武器和防具上」**。方案 A 虽然更简单，但铭刻跟着槽位走，换武器时效果会「粘」到新武器上，这个语义很难向玩家解释。
3. 它留下的数据（铭刻 + 作用槽）是方案 B 的**子集**，将来真要做「每件装备独立铭刻」，迁移路径是清晰的，不会白做。
4. 唯一真正的取舍是「这把剑带毒 / 那把不带」无法表达。**这一点需要你们先确认**——如果这是核心诉求，就直接上方案 B，不要在 A/C 上浪费工期。

**四类效果的实现难度差异很大，建议分期：**

| 期 | 内容 | 依赖 |
|---|---|---|
| 1 | 宝箱交互 + 铭刻物品 + 纯加数值 + 常驻正面效果 | 5.1、5.3 |
| 2 | 带代价的强力效果 | 无新增依赖 |
| 3 | 命中施加负面效果 | 5.2（最贵，可能有多份 `takeTurn` 要改） |

分期做的话，第 1 期结束就能验证「宝箱→铭刻→生效」这条完整链路，而不必等敌人状态系统。

---

## 7. 需要你们确认的问题

1. **「铭刻属于某一件具体装备」是核心诉求吗？** 如果是 → 方案 B；如果「只要作用于某个装备槽就行」→ 方案 C。
2. **一个装备槽能叠几个铭刻？** 1 个（替换）还是多个（叠加）？影响 UI 与结算复杂度。
3. **铭刻能取下/销毁吗？** 能取下需要实例化（方案 B）；不能取下则 A/C 都行。
4. **宝箱交互用哪套？** 格子容器（纯代码，我先能做）还是实体 prefab（表现好，需要人工接线）。
5. **铭刻是否也走合成系统？** 现在有 `Recipe`，可以支持「3 个碎片合成 1 个铭刻」。
6. **负面效果需要哪几种？** 中毒（DoT）/ 眩晕（跳回合）/ 虚弱（降伤害）/ 减速 的取舍决定 5.2 的规模。
