# OCC 第一阶段验收交接（含界面验收对比与 GPT 提示词）

版本：2026-09-21　交接人：DeepSeek Harness 会话　接手：GPT（新会话）

本文件是过程记录，位于 `Worldbuilding/归档/`，只用于追溯与交接，不构成策划源。玩法口径一律以 `Worldbuilding/策划案/OCC_项目总策划案_v1.0.md` 与 `Worldbuilding/数据表/` 为准，世界设定以 `Worldbuilding/OC世界观/` 为准。

---

## 一、交接范围

第一阶段（学院）验收收尾，共 5 项：

| 项 | 内容 | 状态 |
|-|-|-|
| ① | 首轮固定段商店结束后**不换图**：在原有大地图上以商店为起点逐环显现剩余节点 | 完成并实机验证 |
| ② | 战斗结束回到地图（不再掉回开始界面） | 完成并实机验证 |
| ③ | 开发线「一键过关」＝消灭全部敌人 → 播胜利 → 停在战后奖励界面 | 完成并实机验证 |
| ④ | 按 Pixso 对齐三屏：工坊（11C／11D／11E）、医务室（12）、节点预览（09） | 三屏均已实装并截图验证；09 已逐项比对 |
| ⑤ | 每步编译、清 Console、跑 EditMode 回归 | 当前编译无错误、Console 无错误、全量 1058 通过 / 24 既有失败（无新增） |

附加已批准事项：在学院层展开区补工坊、医务室、商店各 1 个服务节点，并同步文档。**模型层已完成**（见第三节），**界面与额度接线未完成**（见第四节）。

---

## 二、证据（截图）

目录：`Worldbuilding/归档/2026-09-21_第一阶段验收交接/证据截图/`

| 文件 | 内容 | 对应验收项 |
|-|-|-|
| `game-view-20260921-140551-841.png` | 学院层入口地图：3 个可走节点＋商店起点牌＋接入连线 | ① |
| `game-view-20260921-141040-667.png` | 推进后地图：扩到 5 个节点 | ① |
| `game-view-20260921-141934-091.png` | 同一张地图：首轮固定段 11 节点留在原位＋展开区 | ① |
| `game-view-20260921-141019-412.png` | 战后奖励界面「战斗胜利・挑一件带走」三选一 | ③ |
| `game-view-20260921-142852-347.png` | 工坊 11C／11D：装备锻造页 | ④ |
| `game-view-20260921-143316-356.png` | 工坊 11E：术式专精页 | ④ |
| `game-view-20260921-143728-554.png` | 医务室 12 | ④ |
| `game-view-20260921-143956-919.png` | 节点预览 09（出发准备） | ④ |

---

## 三、已完成：学院层 3 个服务节点的模型层

改动文件：

- `UnityProject/Assets/Game/Runtime/Campaign/RogueliteAcademyLayerCatalog.cs`
  - 新增 `ServiceNodes`：`layer_workshop`（校准工坊，锚点 5,1）、`layer_medical`（公共医务室，6,1）、`layer_shop`（学院补给商店，5,3）；NextIds 分别接 `clinic_hall`＋`layer_medical`、`wilds_path`＋`layer_workshop`、`supply_depot`＋`observatory_path`。
  - 三个 id 并入 `NodeIds`（24 个）与 `LayerNodeIds`；新增 `TryResolveLayerNode`／`IsServiceNode`；`ServiceNodeById`。
  - 内容映射补 `W01`／`S01`／`SHOP01` 三条（无遭遇变体）；`AcademyNodeContentMapping` 的 `Type`、`RegularNodes`、`LayerNodes` 改为经层目录解析。
- `UnityProject/Assets/Game/Runtime/Campaign/RogueliteMapRun.cs`
  - `RogueliteMapCatalog.Node(id)` 改为层目录优先再回全图；`MapNode(id)`、`ResolveNode(id)` 同样层目录优先。
- `UnityProject/Assets/Game/Runtime/Campaign/RogueliteMapRunValidator.cs`
  - `IsAcademyLayerAdjacent` 改为经 `TryResolveLayerNode`，不再对层专属 id 抛未知节点。

设计要点（写进总案 2.1／7.1）：

- 这 3 个是**学院层专属节点**：8×5 锚点阵中与首轮 11 节点不重叠的空格位只有 `(5,1)(6,1)(4,2)(5,2)(6,2)(4,3)(5,3)(4,4)`，而全图目录在这些格位上有别的节点，所以服务节点不进 `RogueliteMapCatalog.Nodes`，只走层目录解析。
- 额度口径按数据表：`S01`「恢复…餐食…两项各可执行一次」、`S04`「每个地图节点只可结算一次」、`W01`「免费完成一次装备锻造和一次术式专精」→ **每个服务节点各自结算一次**，与商店前的工坊、医务室互不影响。

---

## 四、未完成（接手任务）

### 任务 A（必须）：服务页按节点类型打开 + 每节点额度

现状：11C／11D／11E、12 与首轮商店页在 `FormalRogueliteUi.DrawNodeRoom` 里绑的是首轮 id：

- `if (run.IsTutorialPhase && current && node.Id == "W") { DrawFirstRunWorkshop(...); return; }`
- `if (run.IsTutorialPhase && current && node.Id == "M") { DrawFirstRunMedical(...); return; }`
- 商店页在 `DrawNodeRoomActions` 里绑 `FirstRunExperienceCatalog.ShopNodeId`。

而三张页读写的是首轮快照 `FirstRunExperience.Workshop／Medical`，那是**整轮一次性**状态。直接把这些页也挂到 `layer_workshop`／`layer_medical`／`layer_shop` 上会出现错误结果：商店前做完锻造与专精后，展开区的工坊会显示「本轮已安装」而不可用。

落地顺序：

1. 单轮状态加「服务节点已结算」集合：`RogueliteMapRun` 的读写与查询方法、`OCC.Combat.Roguelite.RogueRunDto` 新字段与序列化、存档迁移与 `RogueliteMapRunValidator` 覆盖（**这是存档格式变更，必须一次做完并连存档回归一起验**）。
2. 三张服务页改为按 `RogueliteMapNodeType.Workshop／Medical／Shop` 打开（而不是按 `"W"`／`"M"`／`"S"`），页内动作接到上面的集合；首轮固定段的 W／M／S 仍走原来的整轮一次性状态，两者不要混用。
3. 商店页（Pixso 13＝`2:434`）同样按类型打开；它目前只有首轮实现，`layer_shop` 走的是通用房间。

### 任务 B（必须）：实机验证

- 从首轮开局走到商店、进入展开区，走到 `layer_workshop`／`layer_medical`／`layer_shop`，逐个截图比对 Pixso（见第五节清单）。
- 全量 EditMode 回归 + Console 检查（见第六节协议）。

### 任务 C（已确认待执行）：飞书镜像同步

本地 `OCC_项目总策划案_v1.0.md` 的 2.1 与 7.1 已改写（写明展开区固定含工坊／医务室／商店，编号 W01／S01／SHOP01），**飞书母版尚未同步**。按仓库规则飞书是产品内容母版、本地是镜像；用户已表示「允许修改」，接手时按飞书文档工具执行一次同步并回读校验。

---

## 五、界面验收对比清单（Pixso 稿号 → 逐项）

Pixso 文件：单页 `0:1`「OCC｜正式界面总览」，须保持 Pixso 桌面端打开该文件。

| 屏 | 稿号 | 逐项对比 |
|-|-|-|
| 节点预览 09 | `2:430` | 顶栏「出发准备（节点名）／编号 类型（可达）」；左侧深色任务档案（类别眉标含 FIRST ENCOUNTER、大字节点名、说明、底部耗时条）；右侧「任务公开信息」标题＋行动目标行（浅底＋左侧强调线，不得实心填充）；「敌情」「场地」两卡；底部「先不去／学院整备／出发」 |
| 工坊 11C／11D | `2:1360`／`12:1459` | 顶栏「校准工坊｜确定性强化」＋右侧「精英门槛 锻造 x/1 专精 x/1」＋「返回地图」；页签「装备锻造／术式专精」；左「强化台」（目标位、材料位：名称／当前份数／本次消耗／剩余）；右「强化预览」（空态文案；有目标时「安装前 → 安装后」＋材料＋「确认安装」＋不可更换提示）；下「库存」（已有装备 n／已有术式 n／背包材料 n 三页签＋卡牌列表：槽位・稀有度、名称、编号、占格／重量／强化位、效果）；底栏操作提示 |
| 工坊 11E | `12:1669` | 同 11C，切到术式专精：材料位变「增幅刻墨」，卡牌为已掌握合法术式，预览显示专精结果 |
| 医务室 12 | `2:433` | 顶栏「医务室（健康确认与服务）／不消耗学院时序／返回地图」；左侧深色身份卡（ACADEMY MEDICAL SERVICE、公共医务室、生命（上限 18）／魔力（上限 12）／金・贡・食 三行读数）；右侧「健康确认与服务」＋三条服务行（免费健康确认／接受治疗／餐食选择，各含图标、两行说明、右侧状态）；餐食行内三个餐食按钮；底部「返回地图／接受治疗」 |
| 商店 13 | `2:434` | 任务 A 完成后再比对（购买、售罄、金币不足） |
| 学院地图 08 | `2:429` | 只在地图改动后复核：节点不出现未探明项、起点牌与连线、顶部资源条 |

已知实现取舍（不是缺陷，复核时不要改回去）：

- 耗时显示以数据为准：B1 的 `AcademyMapTuning.TimeCost` 为 0，界面显示「不耗时」，Pixso 稿画的 `+1` 属稿面示意。
- 页签与库存页签用「浅底＋青色调」表示选中，未用 Pixso 的深底白字（`ActionButton` 的文字色由组件决定，深底会不可读）。
- 装备／术式卡片的说明行只显示数据里存在的效果与结果，不编造稿面上的风味描述。

---

## 六、环境、工具与协议

### 环境

- 主工作树（唯一）：`E:/数据库/OCC_Codex`；Unity 工程：`E:/数据库/OCC_Codex/UnityProject`；操作前确认 `Application.dataPath == E:/数据库/OCC_Codex/UnityProject/Assets`。
- 默认场景 `Assets/Scenes/CombatPrototype.unity`；本会话用 Funplay MCP 驱动编辑器。
- 开发入口开关：`ProjectSettings/ProjectSettings.asset` 里 `Standalone: DOTWEEN;OCC_DEVELOPER_TOOLS`；`DeveloperBuildGate.IsEnabled` 由该符号决定。游戏内开发控制台按 **F1** 打开（「一键过关／连续推进到首领／结算当前奖励」）。
- 本地存档写在 PlayerPrefs（`HKCU\Software\DefaultCompany\My project`）；开发线按钮会写真实存档。回滚备份：`Worldbuilding/归档/2026-09-21_地图显现改造前/playerprefs_backup.reg`。

### 协议

1. 改脚本后 `request_recompile` → 等编译 → `get_compilation_errors`；Console 用 `get_console_logs(log_type=error)` 检查。
2. 回归：`run_tests(mode=EditMode)` 全量 1082 项。**基线是 24 个既有失败**，必须逐名对比、不得新增：
   `CampaignStateTests.Progression_HasNoEquipmentDurabilityAndValidatesSixTemplates`；
   `CombatBattlefieldCellPresenterTests.BoundaryOverlay_IsDisabledForSelfContainedFloorTiles`／`CoverVariant_OnlySelectsVisualAssetForExistingCoverKind`；
   `CombatTestArenaPlaythroughTests` 12 项（E01–E03、EveryAuthoredBattle、N02–N09）；
   `CombatTestArenaScenarioTests` 2 项（BossPreset…、ReactionPressure…）；
   `EnemyPackTests.FinalUnitArt_LoadsWithRequiredPixelImporter("pyromancer")`；
   `FormalArtAssetAuditTests` 3 项（AcademyMapTypeIcons…、EveryNonBlockedRegistryEntry…、PeripheralUi…）；
   `FormalUiThemeTests` 3 项（PeripheralArtSizes…、SharedContentTooltip_LongEffect…、SharedContentTooltip_UsesPixelAligned…）。
3. 实机验证：`enter_play_mode`，用 `execute_code`（`skip_refresh=true`）驱动，`capture_game_view` 截图后 `read_image` 逐项比对。
4. 只保存本次改动的场景／资产；不手改 `.unity`／`.prefab`／`.asset` 文本。

### 驱动实机的现成手法（本会话验证有效）

- 从入口进游戏：`FirstExperiencePrototypeController.ShowLanding() → ContinueRun() → ChooseSlot(0)`；新档走 `StartNewRun() → ChooseSlot(0) → ConfirmSelectedSlot() → ConfirmConfiguration() → EnterAcademy()`。
- 推进首轮固定段：`run.AcknowledgeFirstRunOrigin()`；战斗 `run.SelectNode(id) + run.CompleteCurrentCombat() + run.ClaimReward(run.CurrentFirstRunRewardIds[0])`；事件 `run.ChooseCurrentNodeContent(run.FirstRunExperience.EventForNode(id).OptionIds[0])`；工坊 `run.SelectNode("W") + run.CompleteFirstRunForge("equipment") + run.CompleteFirstRunSpecialization("spell")`；医务室 `run.SelectNode("M") + run.CompleteFirstRunHealthCheck()`。
- 打开节点房间：模型改完后**要等一帧**，下一次工具调用里 `GameObject.Find("map.node.<ID>")` 再 `GetComponent<UnityEngine.UI.Button>().onClick.Invoke()`；页内按钮同理用 `GameObject.Find("按钮_返回地图")` 等返回。
- 精英节点 `X` 有入口门槛（需先完成锻造＋专精＋健康确认），未满足时 `SelectNode("X")` 抛「First-run node is locked」。

### 两个已踩过的坑（务必注意）

1. **`FormalUiKit.Label` 的文字槽会被压进父框**：父框高度不足时，24 号像素字整行不渲染（不是被裁切，是完全不画）。经验值：容器高度需 ≥64，标签槽最终高度需 ≥36。页签、库存页签、底部提示都踩过这个坑。
2. **`FormalUiKit.FocusFrame` 会整块填充**：它的贴图在无九宫格边框时按 `Image.Type.Simple` 拉伸，会把整行填成实心色。表示强调请用「浅底面板＋左侧 4px 强调线」，不要用它。

---

## 七、给 GPT 的提示词（可直接整段复制）

```
你接手 OCC（Unity 战棋 Roguelite）第一阶段验收收尾。仓库根目录 E:/数据库/OCC_Codex，Unity 工程 E:/数据库/OCC_Codex/UnityProject。先读：
- 仓库根 AGENTS.md、UnityProject/AGENTS.md（含 Funplay 使用约定）
- Worldbuilding/归档/2026-09-21_第一阶段验收交接/第一阶段验收交接与GPT提示词.md（交接全文：当前状态、证据截图、验收清单、环境与协议、两个已踩过的坑）
- Worldbuilding/策划案/OCC_项目总策划案_v1.0.md 的 2.1 与 7.1；Worldbuilding/数据表/OCC_学院节点内容数据表_v1.0.csv 的 W01／S01／SHOP01 行

已完成（不要重做）：① 商店结束后同一张地图以商店为起点逐环显现剩余节点；② 战斗结束回地图；③ 开发线一键过关＝消灭全部敌人→播胜利→停在战后奖励界面；④ 工坊 11C/11D/11E、医务室 12、节点预览 09 已按 Pixso 实装并通过截图比对；学院层 3 个服务节点的模型层已实装（节点定义、层目录优先解析、内容映射 W01/S01/SHOP01、每节点各结算一次的口径），全量 EditMode 回归 1058 通过 / 24 既有失败（无新增），编译与 Console 干净。

你的任务，按顺序做，每步都要编译 + Console + 回归：
A. 单轮状态加「服务节点已结算」集合（RogueliteMapRun 读写查询、RogueRunDto 字段与序列化、存档迁移、验证器覆盖）。这是存档格式变更，一次做完并连存档用例一起验。
B. 把工坊／医务室／商店三张页从「按首轮 id W/M/S 打开」改为「按 RogueliteMapNodeType 打开」，页内动作接到 A 的集合；首轮固定段的 W/M/S 仍用原来的整轮一次性状态，两者不得混用（否则商店前做完锻造后，展开区工坊会错误显示「本轮已安装」）。
C. 实机验证：从首轮开局走到商店进入展开区，走进 layer_workshop / layer_medical / layer_shop，逐个截图与 Pixso 比对（稿号：09=2:430、11C=2:1360、11D=12:1459、11E=12:1669、12=2:433、13=2:434）。Pixso 桌面端须开着该文件（单页 0:1）。
D. 把本地总案 2.1／7.1 的改动同步到飞书母版并回读校验（用户已授权修改）。

约束与验收：
- 所有改动只在主工作树；不得用 Git worktree 或仓库副本。
- 先判断能否用简化设计解决，再考虑特殊处理；不为通过测试而放宽断言。
- 回归基线是那 24 个既有失败（名单在交接文件第六节），必须逐名对比，出现新增失败必须修到零新增。
- 汇报格式：先说「玩家现在能完成什么、实机验证到哪一步、最严重的剩余问题」，技术细节附后。不要用「你/我/他」写游戏内文案。
```

---

## 八、改动文件索引（本阶段）

- `UnityProject/Assets/Game/Runtime/Presentation/FormalRogueliteUi.cs`：地图逐环显现与商店起点牌；工坊 11C/11D/11E 整页；医务室 12 整页；09 行动目标行改浅底＋强调线；文字槽高度与餐食行重叠修复。
- `UnityProject/Assets/Game/Runtime/Campaign/RogueliteAcademyLayerCatalog.cs`、`RogueliteMapRun.cs`、`RogueliteMapRunValidator.cs`：学院层 3 个服务节点与层目录优先解析。
- `UnityProject/Assets/Game/Runtime/Campaign/RogueliteDeveloperRunPolicy.cs`、`UnityProject/Assets/Game/Runtime/Presentation/CombatPrototypeBootstrap.cs`、`DeveloperConsolePanel.cs`：一键过关改为停在战后奖励界面。
- `UnityProject/Assets/Game/Tests/EditMode/CombatBoundaryTests.cs`：开发入口断言改为跟随 `OCC_DEVELOPER_TOOLS` 的不变量（原断言在验收开关打开后必然失败）。
- `Worldbuilding/策划案/OCC_项目总策划案_v1.0.md`、`Worldbuilding/数据表/OCC_学院节点内容数据表_v1.0.csv`、`OCC_学院第一阶段全流程内容清单_v1.0.csv`：服务节点与口径同步。
