# OCC CORE-INTEGRITY-01 战斗快捷栏单一数据源实施计划 v0.1

> 日期：2026-08-09
> 状态：COMPLETE / 2026-08-09
> 归属：剧情模式与肉鸽模式共用的战斗物品、快捷栏、存档和 HUD 基础设施

## 1. 目标

消除 `CombatState.Quickbar` 与 `CombatState.ItemQuickbar` 并行造成的免费消耗品、状态分叉和 UI 显示不一致，使实例背包与八格实例快捷栏成为唯一权威数据源。

完成后必须满足：新肉鸽局只获得一次医疗包和护盾电池；战斗消耗会写回地图运行并在后续节点保持耗尽；正式 HUD、场景 HUD、背包面板、战斗结算和 `map9` 存档读取同一组实例 ID、剩余次数与槽位。

## 2. 当前问题与证据

- `CombatState` 同时持有 `ConsumableDefinition[8] Quickbar` 与 `string[8] ItemQuickbar`。
- `CombatState` 构造函数隐式创建 `combat-medkit` 和 `combat-shield-cell`；`RogueliteMapRun` 新局又创建一组正式实例。
- 两条战斗构建路径仍调用 `ConfigureQuickbar(Medkit, ShieldCell)`，随后才载入地图运行的实例背包。
- `FormalCombatHud` 只创建四个槽位并调用旧 `UseQuickbarSlot`；`TacticalHudSceneBinder` 虽绑定八个场景槽位，仍调用同一旧入口。
- 背包面板已经使用八格 `ItemQuickbar` 和 `ActivateInventoryQuickbar`，而地图存档也只持久化实例背包与 `ItemQuickbar`。
- 现有旧快捷栏测试验证的是已过时的第二套状态，缺少“消耗后下一场不重生”的跨战斗回归。

## 3. 冻结决策

1. `InventoryContainerState ItemInventory` 与 `string[8] ItemQuickbar` 是唯一物品和快捷栏状态。
2. 删除 `CombatState.Quickbar`、`ConfigureQuickbar`、`ClearQuickbarSlot`，并删除可绕过实例背包直接产生医疗包效果的旧 `CombatCommand.UseItem`；不保留双写或镜像同步层。
3. 保留 `CombatCommand.UseQuickbar` 的命令名称作为直接使用消耗品的兼容入口，其槽位解析必须读取 `ItemQuickbar`；需要选择目标的卷轴或法宝继续由统一的 `ActivateInventoryQuickbar` 协调入口进入既有预览和武装流程。
4. `CombatState` 构造函数不再隐式赠送物品。肉鸽新局的初始医疗包与护盾电池只由 `RogueliteMapRun` 创建；靶场、独立原型和测试如需物品，必须显式注入固定夹具。
5. 正式战斗 HUD 与场景 HUD 都展示八个连续槽位，并统一调用实例快捷栏入口。卷轴与法宝合计最多四件的规则保持不变。
6. 战斗内换入快捷栏继续消耗 1 AP；直接使用普通消耗品继续按现有规则消耗 1 AP；无效操作不得扣 AP、消耗次数或改变槽位。
7. `map9` 已具备实例与八槽保存字段，本任务不升级存档版本；只修正运行时读写路径并补迁移/往返回归。
8. 不保存或直接编辑 `.unity` 场景；优先调整绑定脚本和运行时 HUD。不得进入 Play Mode，除非用户另行明确授权。

## 4. 涉及文件与系统

### 运行时

- `UnityProject/Assets/Game/Runtime/Combat/CombatState.cs`
- `UnityProject/Assets/Game/Runtime/Combat/CombatResolver.cs`
- `UnityProject/Assets/Game/Runtime/Combat/CombatCommand.cs`（仅在兼容命令需要补充语义时修改）
- `UnityProject/Assets/Game/Runtime/Presentation/CombatPrototypeBootstrap.cs`
- `UnityProject/Assets/Game/Runtime/Presentation/FormalCombatHud.cs`
- `UnityProject/Assets/Game/Runtime/Presentation/TacticalHudSceneBinder.cs`
- `UnityProject/Assets/Game/Runtime/Campaign/UiPresentationModels.cs`
- `UnityProject/Assets/Game/Runtime/Campaign/RogueliteMapRun.cs`（只收束注入、捕获与断言，不改 `map9` 格式）

### 测试与文档

- `UnityProject/Assets/Game/Tests/EditMode/ItemInventorySystemTests.cs`
- `UnityProject/Assets/Game/Tests/EditMode/CombatEffectExecutionTests.cs`
- `UnityProject/Assets/Game/Tests/EditMode/UiPresentationModelsTests.cs`
- `UnityProject/Assets/Game/Tests/EditMode/FireRogueliteExperienceTests.cs`
- 必要时增加独立 `QuickbarIntegrationTests.cs` 及对应 `.meta`
- 本计划、当前待办与完成后的验证记录

## 5. 实施阶段

### 阶段 A：先建立失败回归

1. 增加新局物品唯一性测试：地图运行和首场战斗中各只有一个医疗包、一个护盾电池，实例 ID 一致。
2. 增加跨战斗测试：首战消耗医疗包、捕获战斗状态、构建下一战后，医疗包不得重新出现。
3. 增加普通 `CombatState` 无隐式物品测试；靶场与既有测试改用显式夹具。
4. 增加八槽绑定、重复实例互斥、特殊物品四件上限和无效操作原子性测试。

### 阶段 B：收束战斗状态与命令

1. 移除旧 `Quickbar` 存储和配置 API。
2. 将快捷栏槽位激活统一到实例查找；普通消耗品走 `UseInventoryItem`，需选目标的卷轴/法宝沿用既有预览和武装流程。
3. 确保耗尽时同步移除实例并清除所有槽位引用；战斗日志、效果执行和 AP 结果继续使用现有确定性执行器。
4. 删除两条 Bootstrap 战斗构建路径中的旧免费消耗品配置。

### 阶段 C：统一 HUD 与展示模型

1. `FormalCombatHud` 从四槽改为八槽，显示实例图标、名称、剩余次数和空槽状态。
2. `TacticalHudSceneBinder` 的八个现有槽位改绑实例快捷栏入口，不保存场景。
3. `CombatHudPresentationModel` 的刷新签名只读取实例快捷栏，确保物品消耗、换入和目标武装后触发局部刷新。
4. 保持 1920×1080 的 75% 战场 / 25% HUD 基准；八槽使用既有 32×32 正式图标、细线框和中性色，不新增 AI 图或高饱和大面积底色。

### 阶段 D：持久化与兼容回归

1. 核对战斗开始时 `ConfigureItemInventory`、战斗变化后的 `CaptureCombatInventory` 及退出/胜负/重开路径。
2. 验证 `map9` 往返保留实例 ID、剩余次数、位置、旋转和八槽顺序。
3. 验证 `map8 → map9` 迁移不额外赠送消耗品；损坏存档写保护行为不得回退。
4. 核对战术重开语义：恢复该场战斗起始快照，而不是创建一组新消耗品。

### 阶段 E：验证与收口

1. 运行快捷栏、物品栏、法宝、火术、存档、完整肉鸽路线的聚焦 EditMode 测试。
2. 运行全量 EditMode 测试，并记录通过数与耗时。
3. 通过 Funplay 请求重新编译，确认编译错误/警告为 0；检查 Console 无项目错误。
4. 检查 `CombatPrototype.unity` 和 `TrainingRange.unity` 均未被写入且编辑器场景 `isDirty=false`。
5. 未获 Play Mode 明确授权时，仅保留 1920×1080 / 960×540 运行时视觉检查为待授权项，不伪造视觉验收结论。
6. 更新当前待办为 COMPLETE，新增验证记录，并明确解锁下一任务。

## 6. 自动化验收矩阵

| 场景 | 必须结果 |
| --- | --- |
| 新肉鸽局 | 背包中医疗包、护盾电池各 1；快捷栏 1、2 槽引用对应实例 |
| 普通 `CombatState` | 默认背包与八槽均为空 |
| 首战载入 | 不产生第二组消耗品，实例 ID 与地图运行一致 |
| 使用医疗包 | 扣除既有 AP，耗尽实例被移除，所有槽位引用清空 |
| 下一战载入 | 已耗尽医疗包不重生，护盾电池状态保持 |
| 八槽操作 | 0–7 均可显示、换入和激活；同一实例最多占一槽 |
| 卷轴/法宝 | 合计最多四件；需目标物品先进入合法预览，不提前消耗 |
| 非法操作 | AP、次数、背包、槽位和存档文本均不改变 |
| 战术重开 | 恢复战斗起始实例快照，不产生新实例 |
| `map9` 往返 | 实例、次数、位置、旋转、槽位和序列化文本确定性一致 |
| 代码清理 | 运行时代码不再引用旧 `CombatState.Quickbar` / `ConfigureQuickbar` |

## 7. 风险与控制

- **测试夹具依赖隐式物品：** 先建立显式物品夹具，再删除构造函数赠送，避免批量假失败。
- **法宝目标流程与普通消耗品入口不同：** 统一的是槽位数据源，不强行合并目标预览执行器。
- **战术重开重复赠送：** 快照必须来自战斗开始时的实例背包，不能重新调用新局赠送逻辑。
- **HUD 八槽空间不足：** 优先复用连续底部快捷栏和 32×32 图标；不缩小正文到不可读字号，不挤占 75% 战场。
- **旧存档兼容：** 不改 `map9` 字段顺序；任何迁移修复先写往返测试再动解析代码。

## 8. 完成后解锁

1. `SAVE-INTEGRITY-01`：为 `map9` 增加节点、武器、资源、生命与实例引用的语义不变量验证，并把失败接入备份与写保护。
2. `STARTER-BUILD-01`：冻结通用初始武器 ID，使奖励过滤与实际关卡构建使用同一武器；开始前需确认通用开局正式武器。
3. `SOURCE-OF-TRUTH-01`：标记废止的时间压力、铁路工业与枪械旧文档，统一当前入学期时代和剧情探索无时间压力基线。

## 9. 实施结果

- 已移除旧 `CombatState.Quickbar`、旧配置/清槽 API，以及可绕过实例背包直接产生医疗包效果的 `CombatCommand.UseItem`。
- 地图运行、独立原型和短肉鸽战斗均通过显式实例背包注入物品；跨战斗捕获只持久化实例、剩余次数与八格槽位，耗尽物品不再重生。
- 正式战斗 HUD 与场景 HUD 均绑定八格 `ItemQuickbar`；消耗、换入和剩余次数变化进入同一展示刷新签名。
- `map9` 格式保持不变；特殊物品四件上限支持在上限状态下移动已有实例和替换已有槽位。
- 验证证据见 `OCC_CORE-INTEGRITY-01_验证记录_2026-08-09.md`。
