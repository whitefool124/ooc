# OCC 褰撳墠寰呭姙

> 寮€鍙戝伐浣滅殑鍗曚竴浜嬪疄婧愩€傛瘡娆″紑濮嬪疄鐜板墠鍏堣鍙栨湰鏂囷紱瀹屾垚銆侀樆濉炴垨鏀瑰彉鑼冨洿鍚庣珛鍗虫洿鏂般€俙褰撳墠杩涜`鍙兘鏈変竴涓富浠诲姟銆?
## 浣跨敤瑙勫垯

- 姣忛」浠诲姟蹇呴』鍐欐槑鐩爣銆佹秹鍙婃枃浠?绯荤粺銆侀獙鏀舵爣鍑嗕笌瀹屾垚鍚庤В閿佺殑涓嬩竴姝ャ€?- 鐜╂硶鏀瑰彉鍏堟洿鏂?`Worldbuilding/01_娓告垙绛栧垝/` 婧愭枃浠讹紝鍐嶅悓姝ュ紑鍙戣鍒掍笌鏈枃銆?- 鍓ф儏妯″紡涓嶅緱寮曞叆鏃堕棿鍘嬪姏銆佹晫鎯呮帹杩涖€佸€掕鏃舵垨鎷栧欢鍏抽棴鍦扮偣鏈哄埗銆?- Unity 鑴氭湰鏀瑰姩蹇呴』缁?Funplay 閲嶆柊缂栬瘧骞舵鏌?Console锛涢櫎闈炴槑纭姹傦紝涓嶄繚瀛樺満鏅€?
## 褰撳墠杩涜

### CORE-ARCH-13：地图推进交互应用服务 — COMPLETE（2026-08-14）

- **归属：** 肉鸽模式地图推进的应用层边界；不改变节点可达性、战斗触发、奖励/资源数值、火术装备兼容性、存档时机或 UI 文案。
- **目标：** 从 Bootstrap 提取节点选择、内容选择、奖励领取、火术装备轮换、奖励装备与以太校准的领域调用和结果判定，以结构化结果驱动现有存档、表现刷新与视觉事件。
- **涉及文件/系统：** `SelectMapNode`、`ChooseMapNodeContent`、`ClaimMapReward`、`ClaimMapFireSpell`、`EquipMapFireSpell`、`EquipNextMapFireSpell`、`EquipMapReward`、`CalibrateMapAether`、资源差值发布及新增 EditMode 应用服务测试。
- **验收标准：** Bootstrap 不再直接调用上述 `RogueliteMapRun` 变更方法或自行遍历火术装备候选；服务结果明确战斗启动、安全回访、资源前后值与刷新边界；现有存档/反馈顺序不变；Funplay 编译、全量 EditMode、PlayMode 和 Console 回归通过。
- **验收记录：** 新增 `RogueliteMapInteractionService`、`RogueliteMapInteractionResult` 与资源快照，统一节点/内容/奖励/火术装备/奖励装备/校准调用并返回战斗启动、安全回访和资源前后值；Bootstrap 不再直接调用这八类 `RogueliteMapRun` 变更方法或遍历火术候选，行数由 1403 降至 1387。新增节点战斗结果、内容战斗结果、校准资源差值与既有火术轮换回退语义测试。Funplay 编译 0 error/0 warning；聚焦 EditMode `20/20 passed`，全量 EditMode `447/447 passed`，PlayMode `1/1 passed`；Play Mode 临时运行对象验证 `start→rail_patrol`、战斗触发与以太 `-2` 差值，未触碰正式存档，Console 无错误，活动场景 `isDirty=false`，未保存场景。
- **完成后解锁：** Bootstrap 的地图职责只剩流程跳转、持久化和表现适配，可继续拆战斗构建或场景资源加载边界并向 1000 行目标推进。

### CORE-ARCH-12：地图推进存档协调器 — COMPLETE（2026-08-14）

- **归属：** 肉鸽模式地图推进的持久化边界；不改变新开种子、职业起始包、继续游戏、坏档保护、覆盖确认、内存保留或存档格式。
- **目标：** 从 Bootstrap 提取地图存档的新建/验证写入、继续读取、坏档状态说明、安全替换、删除与最近保存状态，复用现有 `RogueliteSaveGateway` 的保护语义。
- **涉及文件/系统：** `TryStartMapRoguelite`、`DescribeMapSaveFailure`、`PrepareMapSlotForReplacement`、`SaveMapRun`、地图存档 UI 状态及新增 EditMode 协调器测试。
- **验收标准：** Bootstrap 不再解释 `RogueliteSaveLoadStatus` 或持有 `lastMapSaveSucceeded`；新开必须先成功落盘，继续失败不覆盖，坏档只有明确替换路径可删除，运行中保存失败返回原文案；Funplay 编译、全量 EditMode、PlayMode 和 Console 回归通过。
- **验收记录：** 新增 `RogueliteMapSaveCoordinator`，统一新开前验证写入、继续读取、坏档说明、显式替换、删除、保存结果与 UI 存档状态；Bootstrap 已移除 `lastMapSaveSucceeded` 和 `RogueliteSaveLoadStatus` 分支，行数由 1433 降至 1403。新增写入失败阻止新开、无存档继续不写入、种子/职业往返与坏档显式替换测试。Funplay 编译 0 error/0 warning；聚焦 EditMode `5/5 passed`，全量 EditMode `442/442 passed`，PlayMode `1/1 passed`；Play Mode 只读确认已有存档 `hasSave=True`、可继续且详情为“最近存档”，Console 无错误，活动场景 `isDirty=false`，未保存场景。
- **完成后解锁：** 地图节点导航与资源变化可以在不接触存储细节的情况下提取为独立应用服务。

### CORE-ARCH-11：玩家战斗选择状态控制器 — COMPLETE（2026-08-14）

- **归属：** 剧情模式与肉鸽模式共用的战斗交互架构；不改变行动类型、合法格、键盘移动、取消层级、鼠标行为、技能/法宝规则或 UI 文案。
- **目标：** 将 `selectedAction`、`selectedTargetId` 与 `CombatTargetNavigationState` 的协同状态统一到独立控制器，消除 Bootstrap 中分散赋值与忘记清理目标/光标的风险。
- **涉及文件/系统：** 行动选择、目标查看、键盘目标导航、Esc 取消、格子点击、快捷栏装载、训练场预选及新增 EditMode 状态机测试。
- **验收标准：** Bootstrap 不再声明三个分散选择字段；控制器覆盖选择行动、目标校验、键盘开始/移动/提交/取消、重置与目标清理；现有交互结果和反馈时机不变；Funplay 编译、全量 EditMode、PlayMode 和 Console 回归通过。
- **验收记录：** 新增 `CombatSelectionController`，统一持有当前行动、查看目标与键盘光标，并覆盖行动切换、目标校验、开始/移动/提交/取消和重置。Bootstrap 已移除 `selectedAction`、`selectedTargetId` 与直接 `CombatTargetNavigationState` 字段，全部交互入口改用统一状态边界，行数由 1449 降至 1433。新增状态清理、键盘目标跟踪、提交/取消语义与非法目标门禁测试。Funplay 编译 0 error/0 warning；聚焦 EditMode `5/5 passed`，全量 EditMode `437/437 passed`，PlayMode `1/1 passed`；现场验证攻击目标光标 `(8,2)→(7,2)`、取消清理目标、切回移动并提交 `(1,4)→(2,4)`、AP `3→2`，Console 无错误，活动场景 `isDirty=false`，未保存场景。
- **完成后解锁：** 玩家交互编排可进一步从 Bootstrap 提取，随后集中处理肉鸽导航边界。

### CORE-ARCH-10：战斗结果反馈发布器 — COMPLETE（2026-08-14）

- **归属：** 剧情模式与肉鸽模式共用的战斗表现架构；不改变伤害、火术结算、日志文本、动画类型、反馈顺序或可访问性设置。
- **目标：** 将 `CombatEffectExecution` 与 `FireSpellExecution` 到视觉反馈/日志端口的映射从 Bootstrap 提取为独立发布器，避免会话入口继续理解每一种效果类型。
- **涉及文件/系统：** `PublishCombatEffects`、`PublishFireExecutions`、`CombatVisualFeedback`、战斗反馈事件、火术触发日志及新增 EditMode 映射测试。
- **验收标准：** Bootstrap 不再逐类分派 `CombatEffectKind` 或解析火术执行步骤；发布器通过窄反馈接口覆盖移动、盾吸收、伤害/击败、恢复、状态、可破坏物和火术通知；原反馈顺序保持；Funplay 编译、全量 EditMode、PlayMode 和 Console 回归通过。
- **验收记录：** 新增 `IResolvedCombatFeedbackSink` 与 `CombatFeedbackPublisher`，把移动、盾吸收、生命伤害/击败、恢复、状态、可破坏物和火术通知/日志映射集中到独立发布器；`CombatVisualFeedback` 实现窄端口，Bootstrap 只转交已解析结果，行数由 1488 降至 1449。新增移动/伤害顺序、火术目标坐标/日志文本及结构接线测试。Funplay 编译 0 error/0 warning；聚焦 EditMode `3/3 passed`，全量 EditMode `432/432 passed`，PlayMode `1/1 passed`；现场英雄移动后敌方完整执行并返回英雄回合，Console 无错误，活动场景 `isDirty=false`，未保存场景。
- **完成后解锁：** 继续拆分玩家交互编排与肉鸽导航，不让 Bootstrap 回收规则或表现细节。

### CORE-ARCH-09：正式战斗会话生命周期控制器 — COMPLETE（2026-08-14）

- **归属：** 剧情模式与肉鸽模式共用的正式战斗会话架构；开发训练场保持独立适配器，不改变开战、战术重开、英雄先手、火术/法宝回合生命周期、胜负或存档。
- **实施边界：** 技术边界按推荐方案自主收敛：正式战斗与训练场分离，训练场继续复用命令执行服务；此项不构成玩法大方向决定。
- **目标：** 将正式开战、战术重开后的运行时状态初始化和单位回合生命周期推进从 Bootstrap 组合进独立控制器，统一重置敌方协调器、结算幂等、火术战斗状态与英雄首回合。
- **涉及文件/系统：** `StartDeveloperCombat`、`TacticalRestartDeveloperCombat`、`Update` 中火术/法宝回合开始逻辑、`EnemyTurnCoordinator`、`CombatOutcomeSettlementCoordinator`、`FireBattleState` 及新增 EditMode 生命周期测试。
- **验收标准：** 正式战斗初始化与重开不再在 Bootstrap 重复组装；单位切换时的火术/法宝生命周期由控制器给出窄指令；训练场入口不被正式会话控制器接管；Funplay 编译、全量 EditMode、PlayMode 和 Console 回归通过。
- **验收记录：** 新增 `CombatSessionLifecycleController`，统一正式开战、战术重开、敌方/结算协调器重置、火术战斗状态创建、英雄首回合与活动单位边界信号；训练场明确不进入该控制器。Bootstrap 删除 `fireLifecycleActiveUnitId`，开战/重开改为消费统一 activation，并删除无调用且会提前开启英雄回合的旧 `BuildCombatFromScene` 路径，行数由 1510 降至 1488。新增开战重置、快照重开和单位边界幂等测试。Funplay 编译 0 error/0 warning；聚焦 EditMode `4/4 passed`，全量 EditMode `429/429 passed`，PlayMode `1/1 passed`；现场验证开战英雄 AP=3、失败进入结算、战术重开恢复活动态/英雄 AP=3，并可再次失败进入结算，Console 无错误，活动场景 `isDirty=false`，未保存场景。
- **完成后解锁：** Bootstrap 的战斗会话核心只剩端口调用，可继续拆分肉鸽导航与开发菜单职责。

### CORE-ARCH-08：双运行形态统一战斗结算 — COMPLETE（2026-08-14）

- **归属：** 剧情模式与肉鸽模式共用的战斗会话架构；保留 `mapRun` 与旧 `rogueliteRun` 两种运行形态，不改变奖励、失败回滚、短局、模板沙盒、剧情链或存档格式。
- **用户决定：** 采用路线 A：两种运行形态通过统一结算接口隔离，暂不退役旧 `rogueliteRun`（2026-08-14）。
- **目标：** 从 Bootstrap 提取胜负只处理一次的状态、地图运行结算与旧肉鸽运行结算差异，由统一协调器返回保存/刷新指令；Bootstrap 只执行表现和持久化端口。
- **涉及文件/系统：** `HandleRogueliteOutcome`、`outcomeHandled`、`RogueliteCombatSettlement`、`MapRunState`、`RogueliteDeveloperRun`、短局/剧情存档与新增 EditMode 结算测试。
- **验收标准：** Bootstrap 不再声明 `outcomeHandled`，也不直接决定地图运行与旧肉鸽运行的结算分支；协调器覆盖地图胜利、失败不覆盖战前存档、模板沙盒、短局、剧情链与重复调用幂等性；Funplay 编译、全量 EditMode、PlayMode 和 Console 回归通过。
- **验收记录：** 新增 `CombatOutcomeSettlementCoordinator`，统一持有结算幂等状态并将地图运行、模板沙盒、短局与剧情链适配为 `MapRun/ShortRun/Story/None` 持久化指令；Bootstrap 已移除 `outcomeHandled`，不再直接调用地图结算或旧运行 `Complete`，只执行视觉、保存与结算页刷新端口，行数由 1520 降至 1510。新增地图胜利、失败不落盘、模板沙盒不推进、短局/剧情链端口、重复调用与重置测试。Funplay 编译 0 error/0 warning；聚焦 EditMode `6/6 passed`，全量 EditMode `425/425 passed`，PlayMode `1/1 passed`；现场强制失败后流程正确进入 `Defeat` 并显示结算，Console 无错误，活动场景 `isDirty=false`，未保存场景。
- **完成后解锁：** 将开战、战术重开和回合生命周期组合进 battle session controller，形成稳定的会话入口。

### CORE-ARCH-07：战斗命令执行服务 — COMPLETE（2026-08-14）

- **归属：** 剧情模式与肉鸽模式共用的战斗会话架构；不改变命令合法性、伤害、火术触发、行动点、敌人计划失效时机或表现内容。
- **目标：** 从 Bootstrap 的 `TryCommand` 提取命令授权、权威规则执行、火术武器/移动触发与技能投递上下文计算，由无 Unity 生命周期依赖的服务返回结构化结果；Bootstrap 只负责发布日志、视觉事件和刷新流程结果。
- **涉及文件/系统：** `CombatPrototypeBootstrap.TryCommand`、`CombatResolver`、`FireSpellEngine`、玩家显式结束行动门禁、敌方协调器的命令提交路径及新增 EditMode 服务测试。
- **验收标准：** Bootstrap 不再直接调用 `CombatResolver.Resolve` 或 `FireSpellEngine.ResolveWeaponAttack`；服务独立覆盖普通命令、火术武器攻击、移动触发、技能投递上下文、拒绝与异常结果；敌人与玩家仍共用同一执行路径；Funplay 编译、全量 EditMode、PlayMode 和 Console 回归通过。
- **验收记录：** 新增无 Unity 生命周期依赖的 `CombatCommandExecutionService` 与结构化执行结果，统一处理显式英雄结束门禁、普通规则命令、火术武器攻击、移动触发、技能投递坐标和规则异常；Bootstrap 不再直接调用 `CombatResolver.Resolve` 或 `FireSpellEngine.ResolveWeaponAttack`，只发布日志、反馈与流程刷新，行数由 1551 降至 1520。新增移动、攻击、技能上下文、门禁/异常及 Bootstrap 委托边界测试。Funplay 编译 0 error/0 warning；聚焦 EditMode `4/4 passed`，全量 EditMode `419/419 passed`，PlayMode `1/1 passed`；现场英雄移动 `(1,4)→(2,4)`、AP `3→2`，显式结束后敌人完整执行并返回英雄回合且 AP 恢复为 3；Console 无错误，活动场景 `isDirty=false`，未保存场景。
- **完成后解锁：** 将回合结果刷新和战斗重开快照归并为 battle session controller，进一步缩小 Bootstrap 的会话编排职责。

### CORE-ARCH-06：战斗会话敌方回合协调器 — COMPLETE（2026-08-14）

- **归属：** 剧情模式与肉鸽模式共用的战斗会话架构；不改变敌人 AI、行动顺序、伤害、时间轴、动画节奏或存档格式。
- **目标：** 从 Bootstrap 提取敌方回合的待执行命令、阶段推进与表现等待状态，由独立协调器管理一次敌方行动的生命周期；Bootstrap 只提供权威规则查询、命令执行与表现回调。
- **涉及文件/系统：** `CombatPrototypeBootstrap.RunEnemyTurn`、`EnemyTurnSequence`、敌人公开意图/命令计划、`CombatVisualFeedback` 以及新增 EditMode 协调器测试。
- **验收标准：** Bootstrap 不再声明 `pendingEnemyCommand` 与 `enemyTurnSequence` 字段；协调器独立覆盖开始、等待移动、等待行动、提交命令、结束行动和取消/重置；敌人执行命令仍与公开意图签名一致；Funplay 编译、全量 EditMode、PlayMode 和 Console 回归通过。
- **验收记录：** 新增 `EnemyTurnCoordinator`，统一持有待执行命令与 `Focus → ResultHold → ActorGap` 生命周期，并以窄指令让 Bootstrap 只负责规则执行和表现回调；Bootstrap 已移除 `pendingEnemyCommand`、`enemyTurnSequence` 两个字段，行数由 1574 降至 1551。新增生命周期、零 AP、行动者切换、重置及 Bootstrap 结构门禁测试。Funplay 编译 0 error/0 warning；聚焦 EditMode `5/5 passed`，全量 EditMode `414/414 passed`，PlayMode `1/1 passed`；现场结束英雄回合后多名敌人依次完成移动并正确回到英雄回合；Console 无错误，活动场景 `isDirty=false`，未保存场景。
- **完成后解锁：** 继续把玩家命令提交、战斗结果刷新与会话重开从 Bootstrap 下沉，逐步形成完整 battle session controller。

### CORE-INTEGRITY-03：混合工作区拆分提交 — COMPLETE（2026-08-14）

- **归属：** 剧情模式与肉鸽模式共用的开发治理；不新增玩法、不修改正式资产内容、不保存场景。
- **目标：** 将当前跨运行时、测试、治理文档与美术资源的混合工作区整理为依赖清晰、可独立复核和回滚的 Git 提交，排除缓存、临时截图与无法归类的生成物。
- **涉及文件/系统：** 当前 Git 工作区、Unity Runtime/Tests、Worldbuilding 开发记录、正式美术资源及其生成工具与 Meta。
- **验收标准：** 每个提交主题单一且包含必要依赖；提交前后 `git diff --check` 与项目完整性检查无 failure；Unity 源码批次保持已验证的 409/409 EditMode、1/1 PlayMode 基线；剩余未提交项有明确归类或阻塞原因。
- **验收记录：** 已形成 `f9d1bdb docs(roguelite): define academy first-region contract`、`7bc8d6c feat(art): add formal UI asset production set`、`e027a86 feat(ui): unify combat presentation and academy map runtime` 三个依赖递进提交；第四个治理提交包含工作树约束、完整性检查、ADR/模板、复核报告和本记录。Python `__pycache__` 已排除并加入 `.gitignore`；Unity `.meta` 按引擎原生序列化保留。源码批次沿用本轮 Funplay 编译 0 error/0 warning、EditMode `409/409 passed`、PlayMode `1/1 passed`、Console 无项目错误与 0 dirty scenes 的验证结果。
- **完成后解锁：** 在干净或已清楚标注的工作区上继续提取 battle session controller，避免后续架构改动与历史资产生产混为一体。

### CORE-ARCH-05：正式 UGUI 战场迁移 — COMPLETE（2026-08-14）

- **归属：** 剧情模式与肉鸽模式共用的战斗表现架构；本项决定后续重构顺序，不改变已锁定玩法。
- **目标：** 将当前 IMGUI 战场绘制、裁切和格子交互迁移到正式 UGUI 战场视图；迁移完成后关闭旧 OnGUI 战场路径，不长期维护两套实现。
- **涉及文件/系统：** Bootstrap 的 OnGUI/DrawGrid/HandleCellClick、战斗命令编排、FormalCombatHud、战场反馈坐标与后续 Play Mode 视觉验证。
- **验收标准：** UGUI 承担地块、范围/选择覆盖、单位、生命/护盾、状态、敌人意图与格子点击；视口继续支持滚轮、Home、中键/空格左键和侧键拖动；旧 OnGUI 战场不再运行；全量 EditMode、Play Mode 战场交互与双分辨率视觉回归通过。
- **用户决定：** 采用路线 B，并明确允许进入 Play Mode 测试（2026-08-14）。
- **验收记录：** 新增 `IBattlefieldViewHost`、`BattlefieldCellPresentation` 与运行时生成的 `FormalBattlefieldView`，由 composition registry 统一挂载；UGUI 已承接 108 个格子的地块、环境、范围/选择、单位、血盾、状态、敌人意图、悬停和左右键交互，视口输入由独立控制器负责。Bootstrap 的 `OnGUI`、`DrawGrid` 及全部战场 IMGUI 绘制工具已删除，并新增反射门禁防止回流。Funplay 编译 0 error/0 warning；EditMode `409/409 passed`，PlayMode `1/1 passed`；现场验证左键移动 `(1,4)→(2,4)` 且 AP `3→2`、右键选中 `enemy_2`、中键拖动棋盘偏移；1920×1080 与 960×540 截图通过；Console 无项目错误，0 dirty scenes，未保存场景。
- **完成后解锁：** 提取 battle session controller，将战斗命令编排、回合推进和模式流程从 Bootstrap 继续下沉；该项开始前仍须登记为唯一“当前进行”。

### CORE-ARCH-04：战斗单位 HUD 纯布局提取 — COMPLETE（2026-08-14）

- **归属：** 剧情模式与肉鸽模式共用的战斗表现布局；不改变单位占格、数值、交互或正式贴图。
- **目标：** 将 Bootstrap 中单位可见区域、贴图裁切、生命/护盾条、状态图标、敌人意图徽记与悬停卡的纯几何规则提取到无状态布局类，避免组装入口继续充当 UI 工具箱。
- **涉及文件/系统：** 新的 `CombatUnitHudLayout`、Bootstrap 绘制调用和现有悬停/单位 HUD EditMode 测试；不保存场景。
- **验收标准：** 对应纯静态布局方法不再属于 Bootstrap；所有调用与测试改用布局类；既有矩形、裁切和边界断言保持通过；Funplay 编译与 Console 无项目错误。
- **验收记录：** 新增无状态 CombatUnitHudLayout，迁移单位可见区、64×64 裁切、生命/护盾条、状态图标、意图徽记和悬停卡几何；Bootstrap 删除对应工具方法并从 2143 行降至 1984 行。Funplay 编译 0 error/0 warning，Console 无错误，全量 EditMode 408/408 passed；0 dirty scenes，未进入 Play Mode、未保存场景。
- **完成后解锁：** Bootstrap 剩余职责将集中在战斗会话编排与 IMGUI 绘制，可据此判断下一步是提取战斗会话还是淘汰遗留 IMGUI。

### CORE-ARCH-03：运行时表现组件组装注册表 — COMPLETE（2026-08-14）

- **归属：** 剧情模式与肉鸽模式共用的运行时表现组装；不改变页面内容、显示时机、玩法状态或场景序列化。
- **目标：** 将 Bootstrap 中逐个 `AddComponent`、初始化和保存表现组件引用的职责收敛到一个运行时 composition registry，Bootstrap 只发起一次组装并通过注册表访问组件。
- **涉及文件/系统：** 表现宿主合同、新的组装注册表、`CombatPrototypeBootstrap.OnEnable` 与架构边界测试；不保存场景。
- **验收标准：** Bootstrap 不再声明各 HUD/结算/背包/交互/反馈组件字段，也不逐项执行 `AddComponent`；注册表按既有顺序组装且只暴露 Bootstrap 后续确实需要的组件；全量 EditMode 通过，Funplay 编译与 Console 无项目错误。
- **验收记录：** 新增 `ICombatPresentationCompositionHost` 与 `CombatPresentationComposition`；注册表按既有顺序挂载表现组件并复用已存在实例。Bootstrap 已移除八个具体组件字段和逐项 `AddComponent`，只持有一个注册表并通过只读属性访问实际需要的反馈、交互、结算、启动、控制台和背包组件。新增架构反射门禁；Funplay 编译 0 error/0 warning，全量 EditMode `408/408 passed`；未进入 Play Mode、未保存场景。
- **完成后解锁：** Bootstrap 生命周期组装边界稳定后，可开始提取战斗会话控制器，不让其继续直接拥有 UI 创建逻辑。

### CORE-ARCH-02：战场视口输入控制器提取 — COMPLETE（2026-08-14）

- **归属：** 剧情模式与肉鸽模式共用的战场表现输入；只处理镜头平移、空格/中键/侧键拖动与视口复位，不改变格子坐标、行动规则或地图内容。
- **目标：** 将 Bootstrap 内的战场视口输入状态与事件判定提取为独立控制器，使 Bootstrap 只提供当前视口、可见性和每帧调用，不再保存三组拖动布尔状态。
- **涉及文件/系统：** `BattlefieldViewport`、新的 Presentation 输入控制器、`CombatPrototypeBootstrap` 与 EditMode 输入状态测试；不进入 Play Mode、不保存场景。
- **验收标准：** Bootstrap 不再声明 `battlefieldPanning`、`battlefieldSideButtonPanning`、`battlefieldSpaceHeld`；控制器可独立验证 Home 复位、中键/空格左键拖动与侧键拖动的开始/继续/结束；全量 EditMode 通过，Funplay 编译与 Console 无项目错误。
- **验收记录：** 新增 `BattlefieldViewportInputController`，Bootstrap 已移除三组输入布尔状态和 IMGUI 状态机，仅负责把 Unity Input System 当前帧数据交给控制器。新增空格左键生命周期、Home 复位、侧键视口门禁和字母箱坐标换算测试。Funplay 编译 0 error/0 warning，全量 EditMode `407/407 passed`；未进入 Play Mode、未保存场景。
- **完成后解锁：** 继续提取战斗会话命令与肉鸽流程控制器，并把 `ICombatHudHost`/`IRogueliteUiHost` 的实现从 Bootstrap 下沉到实际控制器。

### CORE-ARCH-01：Bootstrap 表现层依赖隔离 — COMPLETE（2026-08-14）

- **归属：** 剧情模式与肉鸽模式共用的技术架构；不改变玩法、数值、存档格式、正式资产或场景布局。
- **目标：** 将正式 HUD、肉鸽页面、结算、背包、交互层和战斗反馈对 `CombatPrototypeBootstrap` 整对象的依赖，改为按页面职责划分的最小宿主接口，阻止新 UI 继续把 Bootstrap 当作全局服务定位器。
- **涉及文件/系统：** `UnityProject/Assets/Game/Runtime/Presentation/` 的宿主接口、Bootstrap 与运行时生成的表现组件，以及对应 EditMode 架构测试；不保存场景。
- **验收标准：** 非组装入口的表现组件不再声明 `CombatPrototypeBootstrap` 字段；接口只暴露各组件实际使用的只读状态/命令；现有行为测试与新增依赖边界测试通过；Funplay 编译与 Console 无项目错误。
- **验收记录：** 新增九个按实际用途划分的宿主接口，九个表现组件已移除具体 Bootstrap 字段；Bootstrap 只作为 composition root 实现接口。新增两项反射边界测试并登记 ADR-001。Funplay 编译 0 error/0 warning，Console 无错误，全量 EditMode `404/404 passed`；0 dirty scenes，未进入 Play Mode、未保存场景。
- **完成后解锁：** 按接口接缝把战斗会话、肉鸽流程与战场输入逐步从 Bootstrap 提取为独立控制器，并开始减少 Bootstrap 行数。

### CORE-INTEGRITY-02：提交、代码与决策完整性复核 — COMPLETE（2026-08-14）

- **归属：** 剧情模式与肉鸽模式共用的开发治理；只整理提交边界、代码依赖、验证门禁和决策来源，不新增玩法、不保存场景、不替换正式资产。
- **目标：** 分离复核远端领先提交与当前未提交工作区，识别跨层耦合、超大职责类、重复规则、资源/Meta 缺口和不可追溯决策，并建立可重复执行的低成本防腐检查。
- **涉及文件/系统：** Git 提交历史与工作区、`UnityProject/Assets/Game/Runtime`、EditMode 测试、`Worldbuilding/03_开发管理/`、项目质量检查工具；仅在证据充分时做低风险整理。
- **验收标准：** 给出按严重度排序且能定位到文件的复核结论；新增检查能发现缺失 Meta、生成目录误入库、多个当前主任务和高风险大文件；Unity 身份、编译、Console 与相关测试结果有记录；不覆盖或拆改来源不明的用户改动。
- **验收记录：** 已复核领先远端 15 个提交与当前混合工作区，建立 `OCC_工程完整性复核_2026-08-14.md`、技术决策记录模板和 `Tools/Quality/Test-OCCProjectIntegrity.ps1`。检查为 0 failure；结项后警告为无当前主任务、工作区规模、审计基线 268 个未跟踪文件和 2143 行 Bootstrap。Unity 身份与场景门禁通过；14:27 全量 EditMode `402/402 passed`，日志无脚本编译错误。Funplay RPC 在刷新后的回读连续超时，已按工具异常保留，不虚报 Console 回读成功；未进入 Play Mode、未保存场景。
- **完成后解锁：** 依据复核结果把当前混合工作区拆成可独立验证的提交批次，再逐项治理职责过载类和过期决策。

### ART-ITEM-STYLE-03：水果物品分辨率对照测试 — COMPLETE（2026-08-14）

- **归属：** 剧情模式与肉鸽模式共用的材料、消耗品与拾取物微型图标探索；不改变物品数值、掉落、背包规则或 Unity 资源引用。
- **目标：** 将用户确认的草药、炽果、金币、木杯、面包参考图的材质丰富方向，分别实测为独立原生 `16×16` 与 `24×24` 的多种水果，而不是展示大尺寸概念图。
- **涉及文件/系统：** `OCC_美术规范_v0.1.md`、`Worldbuilding/05_美术与音频/像素资产原料/V2-23/新美术指导规格样本/Items16/` 与 QA 预览；不修改 Unity 工程。
- **验收标准：** 每个输出 PNG 严格为目标尺寸、透明硬 Alpha、有限色板；以 1× 可辨类别、最近邻放大检查材质簇为准，并由用户选择能承担材质型物品的最小规格。测试图不导入 Unity，不认定为正式内容资产。
- **验收记录：** 用户确认材质型水果采用 `24×24` 的方向；`16×16` 只保留为微型语义图标/极简读数层，不承担水果的材质阅读。所有测试仍未导入 Unity。
- **完成后解锁：** 以 `24×24` 批量建立材料、食物与消耗品图标词典，并进行独立内容 QA。

## 最近完成

### ART-UNIT-STYLE-01：64×64 战棋单位比例基准 — COMPLETE（2026-08-14）

- **归属：** 剧情模式与肉鸽模式共用的战棋单位表现；不改变兵种数值、行动、地图规则或 Unity 资源引用。
- **目标：** 区分角色立绘与战棋单位比例：保留单位面部可读性，同时缩回格内，为武器、脚底状态和相邻地格留出空间。
- **涉及文件/系统：** `OCC_美术规范_v0.1.md`、`Worldbuilding/05_美术与音频/像素资产原料/V2-23/新美术指导规格样本/References/occ_tactical_unit_scale_v01.png`；未修改 Unity 工程。
- **验收记录：** 用户确认缩回比例。锁定 `64×64` 单位人形高约 `44–48px`、宽 `32–38px`、靴底基线 `Y=56`、中心线 `X=32`、头部 `12–14px` 且可读眼睛与基础表情；概念图仅作比例与色彩参考，未导入 Unity。
- **完成后解锁：** 可为步枪兵、盾卫、术师、精英和主角分别建立独立单图原料与像素 QA，再由用户决定是否导入 Unity。

### ART-ITEM-STYLE-02：原生 32×32 材质物品层与 16×16 微型物品试验 — COMPLETE（2026-08-13）

- **归属：** 剧情模式与肉鸽模式共用的拾取物、消耗品和材料图标探索；不改变物品数值、掉落、背包规则或 Unity 资源引用。
- **目标：** 将用户确认的金币、木酒杯、面包的原生 `32×32` 材质阅读方向写入规范，并以植物、果实、金币、木酒杯、面包重做一组独立的 `16×16` 可读性测试，不把 `32×32` 成品缩小冒充微型图标。
- **涉及文件/系统：** `OCC_美术规范_v0.1.md`、`Worldbuilding/05_美术与音频/像素资产原料/V2-23/新美术指导规格样本/Items16/`、其 QA 预览与生成脚本；未修改 Unity 工程。
- **验收记录：** 原生 `32×32` 层固定为近黑 `1px` 描边、`4–6` 个平涂色与单一材质线索；`16×16` V01 五项均为原生画布、透明硬 Alpha、每项 4 色。预览先给出 1× 可读性，再给出最近邻放大检查；它们仍是样本，尚未导入 Unity 或认定为正式内容资产。
- **完成后解锁：** 用户选定 16×16 的轮廓密度后，可为材料、掉落和消耗品批量建立微型图标表，并进行独立内容 QA 与 Unity 导入决策。

### ART-ITEM-STYLE-01：低像素物品图标视觉基准 — COMPLETE（2026-08-13）

- **归属：** 剧情模式与肉鸽模式共用的背包、快捷栏与奖励物品表现；不改变物品数值、掉落、背包规则或 Unity 资源引用。
- **目标：** 锁定用户确认的低像素物品图标密度，避免后续物品在 `32×32` 槽位中出现高分辨率缩小、机械细节堆叠或材质噪点。
- **涉及文件/系统：** `OCC_美术规范_v0.1.md`、`Worldbuilding/05_美术与音频/像素资产原料/V2-23/新美术指导规格样本/Items32/` 与 QA 预览；未修改 Unity 工程。
- **验收记录：** 固定为 `16×16` 逻辑稿最近邻 `2×` 至 `32×32`；限定一个主轮廓、2--3 个结构分区、一个功能识别点、近黑描边、5--6 色上限及透明安全边。炎脉封装筒、过载处置器与侦测信标 V4 被登记为密度/色彩/描边样本；未导入 Unity，未声称为正式内容资产。
- **完成后解锁：** 依照该基准逐项制作并 QA 消耗品、法宝、战利品与装备图标，再由用户选择是否导入 Unity。

### MAP-RUNTIME-01：学院首区地图运行时基线 — COMPLETE（2026-08-13）

- **归属：** 仅肉鸽模式；战斗与构筑为核心，学院只替换首区地图、战斗和事件的世界语义。
- **目标：** 将既有 20 节点肉鸽地图迁移为 40 节点学院首区，并提供可调的学院时序、核心许可和首领门槛基线。
- **涉及文件/系统：** `RogueliteMapRun`、`FormalRogueliteUi`、地图独立存档、EditMode 测试；未保存场景。
- **验收记录：** 地图目录现为 40 节点（18 普通战斗、6 精英、8 事件、4 服务、2 宝藏、起点与首领），以双向连接解释完整学院网络；现有 20 节点存档格式保持兼容，时序由首次访问数派生，回访不会增加进度。`AcademyMapTuning` 暴露 12 节点/2 核心许可首领门槛、21 节点收束与 28 节点阶段转入阈值；默认不强制阻断路线，等待试玩调参。正式地图页已显示学院探索数与学期状态。新增类型配比、双向连接与探索状态测试；Funplay 重编译 0 error，全量 EditMode **402/402 passed**，未进入 Play Mode，活动场景未保存。
- **完成后解锁：** 学院首区实际试玩调参（节点数、时序阈值、许可/奖励节奏），再做 16×16 正式节点图标替换与关卡/事件学院内容扩容。

### MAP-CONTENT-01：学院首区节点内容包 — COMPLETE（2026-08-13）

- **归属：** 仅肉鸽模式；所有内容围绕战斗、构筑、资源与公开风险选择。
- **目标：** 将 40 节点拓扑转化为可生产的 18 普通、6 精英、8 事件、4 服务、2 宝藏与首领内容表，并复用现有可验证战斗关卡作为首批学院变体。
- **涉及文件/系统：** `OCC_学院首区节点内容包_v0.1.md`、首区关卡目录/敌人原型、肉鸽事件/服务/宝藏数据、地图种子与奖励合同。
- **验收记录：** 已登记 N01--N18、E01--E06、EV01--EV08、S01--S04、T01--T02 和 B01；核心许可有精英、事件和宝藏并行来源，首领保留双种子；所有条目给出地点、底图、目标/敌群阅读或公开选择与奖励倾向，并明确不用课程、关系、日程或毕业数据。
- **完成后解锁：** MAP-RUNTIME-01：实现 40 节点拓扑、学院时序/阶段转入、节点数据/存档、内容解析、学院正式地图 UI 与关卡/事件迁移；完成后再做数值平衡和美术生产。

### MAP-DESIGN-02：学院首区拓扑与内容生产合同 — COMPLETE（2026-08-13）

- **归属：** 仅肉鸽模式；学院背景只承载战斗/事件/服务语义，不建立生活模拟层。
- **目标：** 为 40 节点、节点数量自由选择且以学院时序转入下一阶段的首区锁定可生成的六区骨架、环路、信息揭示、核心许可、首领路线和内容生产顺序。
- **涉及文件/系统：** `OCC_学院首区地图拓扑与内容生产_v0.1.md`、学院首区地图系统、肉鸽地图种子/存档、节点 UI、路线生成测试与后续战斗/事件内容包。
- **验收记录：** 固定六区节点数、40 节点类型分配、至少 12 环路/10 跨区连线/3 条首领路线、首五选保障、学院时序第 21 节点收束/第 28 节点转入、12 节点 + 两枚许可的首领条件、可见拓扑与分层信息揭示，以及 100 种子验证合同；明确 9 个既有关卡先迁移，再生产普通/精英变体、事件/服务/宝藏和首领内容。
- **完成后解锁：** MAP-CONTENT-01：编制学院首区 18 普通、6 精英、8 事件、4 服务、2 宝藏与首领的具体内容表；随后 MAP-RUNTIME-01 按拓扑合同实现数据/存档/UI。

### MAP-DESIGN-01：肉鸽学院首区地图策划 — COMPLETE（2026-08-13，修订）

- **归属：** 仅肉鸽模式；不读取/写入剧情存档，不改变剧情自由探索的无时间压力约束。
- **目标：** 将现有“三选一半线性战时首区”重写为 40 节点学院首区；节点数量自由选择，过度探索以公开的学院时序压力推入下一阶段，战斗和事件仍是核心，背景改为角色入学期的学院成长。
- **涉及文件/系统：** `OCC_肉鸽学院首区地图系统_v0.1.md`、`OCC_肉鸽模式玩法定义_v0.1.md`，后续将涉及肉鸽地图状态、存档、正式地图 UI、事件和战斗入口。
- **验收记录：** 已固定 18 普通战斗、6 精英、8 事件、4 服务、2 宝藏、1 首领与起点组成的 40 节点正交网络；节点可自由选择，首次处理新节点推进学院时序，已清节点免费回访，核心许可在至少 12 个处理节点后开启首领；第 21 节点进入公开学期收束、第 28 节点结算后转入下一阶段。学院背景仅替换战斗/事件/服务的世界语义，保留确定性战斗、三选一、风险预览及禁止隐藏压力/追击/强制伤血约束。明确禁止课程、关系养成、毕业路线与其他非战斗主循环。
- **完成后解锁：** MAP-RUNTIME-01：迁移 40 节点地图、学院时序/阶段转入、存档和正式入口，再依序替换战斗关卡、事件、服务、宝藏和首领的学院内容；不得创建生活模拟数据层。

### UXTURN-01：禁止玩家回合自动结束 — COMPLETE（2026-08-13）

- **归属：** 剧情模式与肉鸽模式共用的玩家回合控制层；未改变 AP、先攻、敌人行动或战斗数值。
- **目标：** 玩家 AP 降为 0 后仍停留在玩家回合，只有明确点击/确认“结束行动”才推进到下一单位；移除技能、火术、法宝与通用命令后的自动结束路径。
- **涉及文件/系统：** `CombatPrototypeBootstrap` 玩家命令提交与结束回合授权、`CombatHoverInformationTests` 回合授权合同、本待办验证记录。
- **验收记录：** 已移除法宝、火术及通用玩家命令完成后共三处 `ActionPoints == 0` 自动调用英雄 `CombatResolver.EndTurn` 的旧逻辑；英雄结束命令新增显式授权门禁，只有正式“结束行动”入口以授权参数提交，未来误从普通命令路径调用也会被拒绝。敌人逐单位行动结束仍由原敌人序列自动推进。源码审计确认表现层不再存在英雄 AP=0 自动结束判断。聚焦 EditMode **41/41 passed**，全量 EditMode **400/400 passed**；Funplay 重编译 0 error，Console 无项目 error，未进入 Play Mode，活动场景 `isDirty=false`，无 `.unity` Git diff，未保存场景。
- **完成后解锁：** 玩家可在 0 AP 时继续检查战场、目标、状态和日志，再自主结束行动。

### UXMAP-08：鼠标侧键独立按住拖动修正 — COMPLETE（2026-08-13）

- **归属：** 剧情模式与肉鸽模式共用的战场输入层；未改变地图坐标、点击、缩放或战斗规则。
- **目标：** 鼠标指针在战术视口内时，单独按住后退或前进侧键即可开始并持续拖动地图，不要求 Space、左键、中键或其他组合键；兼容不持续发送 IMGUI `MouseDrag` 的鼠标驱动。
- **涉及文件/系统：** `CombatPrototypeBootstrap` 战术视口输入与 Unity Input System 鼠标轮询、`CombatHoverInformationTests` 输入合同、本待办验证记录。
- **验收记录：** 侧键拖动不再依赖 IMGUI 持续产生 button 3/4 `MouseDrag`：Unity Input System 直接轮询 `backButton`/`forwardButton`，任一侧键独立按下且指针位于战术视口内即进入拖动，持续按住时按每帧鼠标增量平移，全部释放后结束；不读取 Space、左键或中键状态。IMGUI 收到的侧键按下/拖动/释放事件仍全部消费，不下传为格子点击或检查；中键与 Space+左键维持原通道。聚焦 EditMode **40/40 passed**，全量 EditMode **399/399 passed**；Funplay 重编译 0 error，Console 无项目 error，未进入 Play Mode，活动场景 `isDirty=false`，无 `.unity` Git diff，未保存场景。
- **完成后解锁：** 依据具体鼠标硬件反馈，仅扩展等价侧键映射，不改变战场交互范围。

### UXMAP-07：单位占格面积与脚部信息带重排 — COMPLETE（2026-08-13）

- **归属：** 剧情模式与肉鸽模式共用的战场表现层；未改变 12×9 地图、单位占格、移动、射程、AI、伤害或状态规则。
- **目标：** 将默认 128 px 档的人物绘制框从约 28% 占格面积提升到至少 80%，并在 64/96/128/160 px 四档保持相同比例；同步重排生命/护盾条，避免人物放大后越过所属格或关键信息互相遮挡。
- **涉及文件/系统：** `CombatPrototypeBootstrap` 单位与脚部信息布局、`CombatHoverInformationTests` 缩放/边界合同、本待办验证记录。
- **验收记录：** 人物框由 68×68 放大到 116×116，默认档占格面积由 28.2% 提升至 **82.1%**、最长可见轴占格 **90.6%**，四个缩放档比例一致。自审发现正式 64 px 人物资产含较大透明留白，绘制层现按 12 类正式人物各自 Alpha 边界裁掉空白、保留 1 px 安全边并保持原宽高比，不改源资产且不会横向拉伸。生命/护盾条重排为 108×13 与 108×11 的脚部信息带，互不重叠且始终留在格内；96 px 档保留色条但隐藏条内小字，128/160 px 保持可读数字。鼠标中键、Space+左键及侧键 button 3/4 拖动合同继续通过。聚焦 EditMode **38/38 passed**，全量 EditMode **397/397 passed**；Funplay 重编译 0 error，Console 无项目 error，未进入 Play Mode，活动场景 `isDirty=false`，无 `.unity` Git diff，未保存场景。
- **完成后解锁：** 可依据玩家实际运行截图继续微调人物脚底遮挡与状态图标避让，不改变玩法范围。

### UXMAP-06：鼠标侧键战场拖动 — COMPLETE（2026-08-12）

- **归属：** 剧情模式与肉鸽模式共用的战场输入层；未改变地图坐标、点击或战斗规则。
- **目标：** 将鼠标后退/前进侧键加入默认战场拖动，保留中键与 Space+左键，并确保拖动事件不会下传为格子点击或右键检查。
- **涉及文件/系统：** `CombatPrototypeBootstrap` 战术视口输入、`CombatHoverInformationTests` 输入合同。
- **验收记录：** `button 3`（鼠标后退侧键）与 `button 4`（鼠标前进侧键）现均可直接按住拖动战场；中键与 Space+左键继续可用。侧键按下、拖动、释放全过程消费 IMGUI 事件，普通左键仍用于格子操作，右键仍用于检查。输入映射聚焦 EditMode **37/37 passed**，全量 EditMode **385/385 passed**；Funplay 编译 0 error，Console 无项目 error，0 dirty scene、无 `CombatPrototype.unity` diff；未进入 Play Mode、未保存场景。
- **完成后解锁：** 玩家可用任一常见鼠标侧键直接拖动地图；后续可依据具体鼠标驱动对 button 3/4 的映射反馈做兼容微调。

### UXMAP-05：地图边框、地块裁切与缩放比例复核 — COMPLETE（2026-08-12）

- **归属：** 剧情模式与肉鸽模式共用的战场表现层；未改变 12×9 逻辑地图、坐标、射程、移动、AI 或数值规则。
- **目标：** 为固定战术视口补齐清晰的极简工业细线边框并真正裁切部分可见地块；修正地块边缘缺失；让角色与血条跨缩放档保持和地图格子的固定视觉比例。
- **涉及文件/系统：** `BattlefieldViewport`、`CombatPrototypeBootstrap` 战场绘制/输入/单位信息布局、`CombatVisualFeedback` 反馈裁切坐标、相关 EditMode 合同测试。
- **验收记录：** 战术视口新增 4 px 中性外框与 1 px 内高光，符合极简工业细线规范；地图内容改用真实 `GUI.BeginGroup` 裁切，边缘地块不能越过视口，边框始终最后覆盖。每格在全部内容与范围覆盖层之后重绘 1 px 内侧轮廓，且平移/缩放后板面强制整像素对齐，避免模糊或吃边。角色宽高固定为格宽 **53.125%**，生命/护盾条宽固定为 **56.25%**、高固定为 **10.9375%**；64/96/128/160 px 四档比例完全一致且均留在所属格内。64 px 隐藏条内数字，96 px 使用紧凑数字，128/160 px 保留 14 px 数字；低倍率状态图标继续隐藏以保证可读性。第一次复核后发现反馈裁切父节点存在绝对/局部坐标二次偏移，已自我迭代修正，并把浮字/跳字夹紧到真实 1440×876 战场边界。聚焦 EditMode **43/43 passed**，全量 EditMode **378/378 passed**；Funplay 编译 0 error，Console 无项目 error，0 dirty scene、无 `CombatPrototype.unity` diff；未进入 Play Mode、未保存场景。
- **完成后解锁：** 依据用户实际运行截图仅微调边框色值、线宽或低倍率信息密度，不改变玩法与地图拓扑。

### UXMAP-04：战术视口最终视觉与交互回归 — COMPLETE（2026-08-11）

- **归属：** 剧情模式与肉鸽模式共用的战场表现层；未改变玩法或内容规则。
- **目标：** 完成 1920×1080 / 960×540、缩放/平移/聚焦/命中/反馈坐标的最终回归。
- **涉及文件/系统：** `BattlefieldViewport`、`CombatPrototypeBootstrap`、`CombatVisualFeedback`、布局/战斗 EditMode 测试和 Funplay 编辑器检查。
- **验收记录：** 64/96/128/160 px 视口档、滚轮锚点缩放、中键/Space+左键平移、Home/聚焦按钮、战斗开始/重开主角聚焦与安全边缘跟随均由纯合同覆盖；所有格子点击、范围/单位/状态/意图与反馈继续使用同一逻辑格位置。双分辨率布局合同覆盖全部登记正式页面。聚焦回归 29/29 与 52/52 通过；全量 EditMode **372/372 passed**。Funplay 编译 0 error，Console 无项目 error，`CombatPrototype.unity` 无 Git diff、0 dirty scene；未进入 Play Mode、未保存场景。
- **完成后解锁：** 战斗地图与全局可读性适配目标闭环；后续仅依据实际试玩反馈做不改变玩法的微调。

### UXREAD-02：全页面紧凑分辨率可读性审计 — COMPLETE（2026-08-11）

- **归属：** 剧情模式与肉鸽模式共用的 UI 表现层；不改变内容、地图、经济或战斗规则。
- **目标：** 审计战斗 HUD、背包、奖励、地图、设置与档案，紧凑分辨率改为重排或省略次要说明，关键文字不再等比缩小到不可读。
- **涉及文件/系统：** `FormalUiTheme`、`FormalCombatHud`、`FormalRogueliteUi`、`TarkovInventoryPanel`、`RogueliteSettlementPresentation`、布局/主题 EditMode 测试。
- **验收记录：** 共用 `FormalUiTheme.ResponsiveFontSize` 在紧凑高度（≤600）将 ≤18 px 字号提升为 1.25× 的偶数像素值，而非等比缩小；大字设置继续在此基础上提升 12%。战斗 HUD、地图、奖励、设置与档案均经 `FormalUiKit.Label/Button` 接入该令牌；背包 IMGUI 标签、按钮和输入框也显式读取它。`UiLayoutContract` 对所有登记正式布局投影到 1920×1080 与 960×540，保证不越界；聚焦页面/布局/主题 EditMode **52/52 passed**。未改变任何内容或玩法规则。
- **完成后解锁：** UXMAP-04 最终双分辨率交互与视觉回归。

### UXREAD-01：战斗地图信息最小可读性 — COMPLETE（2026-08-11）

- **归属：** 剧情模式与肉鸽模式共用的战斗信息呈现层；不改变战斗数值或规则。
- **目标：** 关键生命/护盾/伤害/意图数字至少 14 px、状态层数至少 12 px、悬浮正文至少 15 px、标题至少 17 px；缩小时隐藏次要信息而非继续缩字。
- **涉及文件/系统：** `CombatPrototypeBootstrap` 战场 IMGUI 信息层、`CombatVisualFeedback`、`CombatUnitHudPresentation`、战斗可读性测试。
- **验收记录：** 生命/护盾条从 9/8 px 升至 14 px；伤害跳字既有 28 px、敌人意图既有 14 px 保持不变。状态层数图标与角标升至 14/12 px，状态说明卡标题/正文为 17/15 px。64 px 概览档保留生命关键行，隐藏次要护盾条，避免继续压缩数字；单位表现框缩至 68 px，状态图标重排到左右边缘且与角色、双条不重叠。聚焦 EditMode **29/29 passed**，编译无 error；未进入 Play Mode、未保存场景。
- **完成后解锁：** UXREAD-02 的全页面紧凑分辨率审计。

### UXMAP-03：统一网格↔屏幕变换、裁切与反馈坐标 — COMPLETE（2026-08-11）

- **归属：** 剧情模式与肉鸽模式共用的战场表现层；不改变任何逻辑地图或战斗规则。
- **目标：** 让地块、单位、血条、状态、意图、落点、范围、悬停命中、伤害跳字与 VFX 使用同一视口坐标变换，并接受战术视口裁切。
- **涉及文件/系统：** `BattlefieldViewport`、`CombatPrototypeBootstrap`、`CombatVisualFeedback`、网格/反馈 EditMode 测试。
- **验收记录：** 地块、范围、单位、单位信息、意图与敌人落点继续以 `BattlefieldViewport.BoardRect` 计算 `CellRect`；逆变换/悬停命中同样接收该板面矩形。反馈层新增 `CombatPrototypeBootstrap.GridToFeedbackPosition`，VFX、脉冲、浮字和伤害跳字不再使用静态默认板面，而是实时读取当前缩放/平移位置；它们全部置于 1440×876 战术 `RectMask2D` 容器中。聚焦 EditMode **22/22 passed**；未进入 Play Mode、未保存场景。
- **完成后解锁：** UXREAD-01 的战斗信息最小字号与低倍率信息层级。

### UXMAP-02：战术视口平移、聚焦与安全边缘跟随 — COMPLETE（2026-08-11）

- **归属：** 剧情模式与肉鸽模式共用的战场表现层；不改变地图坐标或任何战斗规则。
- **目标：** 完成中键/Space+左键平移、Home 与聚焦图标的主角复位，并让战斗开始、重开和返回战斗默认聚焦主角；手动移动后仅在主角接近安全边缘时跟随。
- **涉及文件/系统：** `BattlefieldViewport`、`CombatPrototypeBootstrap` 运行时输入和战斗流转、EditMode 合同测试。
- **验收记录：** 中键与 Space+左键在战场视口内平移并消费事件，不再下传为格子点击；Home 和极简 `⌂` 入口都回到主角。`StartDeveloperCombat`、战术重开及重新构建战斗均调用相同的主角聚焦路径。英雄完成移动后只有 `IsNearSafeEdge` 命中时才复位，手动平移不会被常规绘制或其他命令抢回。视口安全边缘合同新增测试，聚焦 EditMode **10/10 passed**；未进入 Play Mode、未保存场景。
- **完成后解锁：** UXMAP-03 的全部地图元素、命中逆变换和反馈坐标统一变换。

### UXMAP-01：固定逻辑网格的战术视口与缩放合同 — COMPLETE（2026-08-11）

- **归属：** 剧情模式与肉鸽模式共用的战场表现层；不改变 12×9 逻辑地图、坐标、射程、移动、敌人 AI、伤害或状态规则。
- **目标：** 建立 64/96/128/160 px 每格的整数缩放视口，默认 128 px；缩放以鼠标指针为锚并限制内容边界，避免出现可见黑边。
- **涉及文件/系统：** `BattlefieldPresentationAdapter` 视口/网格↔屏幕合同、`CombatPrototypeBootstrap` 战场输入与绘制、对应 EditMode 布局测试。
- **验收记录：** `BattlefieldViewport` 成为独立的纯表现状态：默认 128 px，缩放仅允许 64/96/128/160 px 整数档，视口为左侧 1440×876 区域；平移与聚焦均夹紧地图内容边界。运行时滚轮以指针内容坐标为锚，96 px 自动居中为完整概览，避免为强行锚定而暴露边缘。默认 12×9 内容尺寸为 1536×1152，大于战术视口；逻辑坐标和战斗判定保持不变。聚焦 EditMode 21/21、全量 EditMode 371/371 通过；Funplay 编译无 error，Console 无项目 error，0 dirty scene、无 `.unity` diff；未进入 Play Mode、未保存场景。
- **完成后解锁：** UXMAP-02 的平移、回到主角与安全边缘跟随。

### UXLAYOUT-01：战场合法放大与单位脚下信息重排 — COMPLETE（2026-08-11）

- **归属：** 剧情模式与肉鸽模式共用的战场表现层；未修改地图拓扑、格数、移动、攻击、状态或伤害规则。
- **目标：** 将生命/护盾条移到人物脚下并缩小相对体量；在 1440×900 战场安全区内放大 12×9 地图、地块、单位和反馈坐标，减少四周黑色空挡。
- **涉及文件/系统：** `BattlefieldPresentationAdapter` 战场布局合同、`CombatPrototypeBootstrap` 单位/血条/状态布局、`CombatVisualFeedback` 格子反馈坐标、`BattlefieldPresentationAdapterTests`、`CombatFeedbackEventTests` 与 `CombatHoverInformationTests`。
- **验收记录：** 默认格距由 78 px 提升至 **96 px**，对应 32 px 地块的 3×整数倍率；12×9 地图由 936×702 放大为 **1152×864**，在左侧 1440×900 安全区内保持左右各 144 px、顶部 24 px、底部 12 px，未侵入右 HUD 或底部命令区，最上排敌人意图仍完整留在画布内。格子绘制、鼠标命中、移动/攻击覆盖层及单位/浮字/VFX 反馈坐标统一消费新尺寸。人物框调整为居中的 70×70，生命/护盾条收窄至 72 px 并分别固定在人物脚下 72/84 px 位置，不再覆盖身体；最多六个状态图标改为人物左右两列 12 px 布局，与人物和血条均不重叠，仍保留数值角标与悬停详情。聚焦 EditMode **36/36 passed**，全量 EditMode **369/369 passed**；Funplay 编译 0 error / 0 warning，Console 无 error，0 dirty scene，无 `.unity` Git diff；未保存场景、未进入 Play Mode。
- **完成后解锁：** 依据实际截图仅微调人物锚点、条宽与地图边距，不改变玩法或地图格数。

### UXBAR-01：敌人生命护盾预测条与状态图标带 — COMPLETE（2026-08-11）

- **归属：** 剧情模式与肉鸽模式共用的战斗信息呈现层；未修改生命、护盾、状态、伤害、技能或敌人规则。
- **目标：** 将指向敌人的伤害预览直接呈现在敌人血条上，以当前值、预计扣除段和预计剩余段表达生命与护盾；在血条附近常驻显示状态图标和剩余回合，悬停可阅读玩家化状态详情。
- **涉及文件/系统：** `CombatUnitHudPresentation`、`CombatPrototypeBootstrap` 单位头顶信息层、既有 `FormalStatusIcons32` 六类正式状态资产、`CombatUnitHudPresentationTests` 与 `CombatHoverInformationTests`。
- **验收记录：** 原 5 px 单生命色块升级为生命/护盾双行条，常态显示当前值/上限；合法攻击、伤害技能或火术指向敌人时，条内以“当前 - 预计扣除 → 剩余/上限”更新数字，并用独立预扣色段表示即将失去的生命和护盾，生命归零时直接标出“击杀”。预览继续消费 `CombatTargetDamageForecast` 的权威克隆结果，因此包含火场等环境伤害，非法目标不产生预扣段。六类既有状态图标移至血条下方的独立 18 px 图标带，右下角常驻显示剩余回合；悬停显示状态名称、剩余回合及燃烧直伤/无视护盾、迟缓降速、束缚禁移、破甲强度、目眩和显露标记详情。补齐 `Dazzled/Revealed` 正式图标加载与玩家文案，状态和血条改为地图内容绘制完成后的顶层信息，避免被地块遮挡。聚焦 EditMode **23/23 passed**，全量 EditMode **368/368 passed**；Funplay 编译 0 error / 0 warning，Console 无 error，0 dirty scene，无 `.unity` Git diff；未保存场景、未进入 Play Mode。
- **完成后解锁：** 依据实际截图仅调整条高、预扣色、图标间距和浮窗位置，不改变战斗规则。

### UXPREVIEW-01：玩家指向目标的权威伤害与击杀预览 — COMPLETE（2026-08-11）

- **归属：** 剧情模式与肉鸽模式共用的战斗信息呈现层；未修改攻击、技能、火术、环境伤害或结算规则。
- **目标：** 玩家用攻击、伤害技能或火术指向敌人时，显示实际护盾损失、生命损失、剩余生命与可击杀结果，并计入当前火场及环境条件伤害。
- **涉及文件/系统：** `CombatTargetDamageForecaster` 克隆状态预测器、`CombatPrototypeBootstrap` 敌人悬浮卡与 `CombatTargetDamageForecastTests`。
- **验收记录：** 指向合法敌人时，悬浮卡新增独立“伤害预览”行，显示护盾损失、生命损失、剩余生命或“可击杀”，并明确标注环境伤害。预测只在克隆状态执行与实际提交同源的权威流程：武器攻击包含已武装的火术触发，普通技能调用 `CombatResolver`，火术调用 `FireSpellEngine` 并涵盖条件规则、位移及进入/已有火场造成的伤害；同时用移除现有火场的中性克隆隔离环境贡献。非法目标不显示误导数值，实战单位、资源、触发和火场均不被预览消耗。聚焦 EditMode **5/5 passed**，全量 EditMode **362/362 passed**；Funplay 编译 0 error / 0 warning，Console 无 error，0 dirty scene，无 `.unity` Git diff；未保存场景、未进入 Play Mode。
- **完成后解锁：** 依据实际截图仅调整预览位置、短句长度与危险色，不修改伤害规则。

### UXINTENT-03：意图牌避让与实际伤害跳字 — COMPLETE（2026-08-11）

- **归属：** 剧情模式与肉鸽模式共用的战斗反馈表现层；未修改伤害、护盾、状态、敌人 AI 或结算规则。
- **目标：** 上移敌人头顶意图牌并移除红/青外边框，避免遮挡血条；将现有通用反馈升级为同一目标同次结算合并显示的实际伤害跳字。
- **涉及文件/系统：** `CombatPrototypeBootstrap` 意图牌布局、`CombatVisualFeedback` 伤害跳字生命周期/合并、`CombatDamagePopupPresentation` 玩家数字合同、`CombatFeedbackEventTests` 与 `CombatHoverInformationTests`。
- **验收记录：** 意图牌由格顶 `-11 px` 上移至 `-22 px`，20 px 高牌面底边保持在血条上方；移除伤害红框与非伤害青框，只保留深色底、16×16 图标和预计伤害数字。新增实际伤害跳字：同一目标 0.10 秒内的护盾吸收与生命伤害合并为一个大号 `-N`，N 取权威 `CombatEffectResult.AppliedAmount` 总和；只伤护盾使用护盾色，触及生命改用伤害红，带深色文字描边并向上跳动 52 px。持续伤害/状态伤害经缓存差分同样进入该入口；浮字关闭时不创建，动画 0% 时保留 0.48 秒静态数字；重开会清理未完成跳字。聚焦 EditMode **5/5 passed**，全量 EditMode **358/358 passed**；Funplay 编译 0 error / 0 warning，Console 无 error，0 dirty scene，无 `.unity` Git diff；未保存场景、未进入 Play Mode。
- **完成后解锁：** 依据实际截图仅调整垂直偏移、字号、跳字高度与 0.10 秒合并窗口。

### UXINTENT-02：攻击与破坏意图图标单色重制 — COMPLETE（2026-08-11）

- **归属：** 剧情模式与肉鸽模式共用的正式非人物战斗 UI 美术；未改变意图类型、敌人 AI、伤害、交互或战斗规则。
- **目标：** 将攻击意图重制为单色氧化红单手剑，将破坏意图重制为单色氧化红爆炸图形，保持 16×16 极简可读。
- **涉及文件/系统：** `FormalIntentIcons16/attack|interact_destroy`、意图图标生成脚本、M-A10 v0.2 QA 与 Unity 导入门禁。
- **验收记录：** 攻击图标现为仅含剑身、护手和短握柄的纯红单手剑剪影；破坏图标现为纯红八向爆炸剪影，不再使用锤/工具隐喻。两枚均严格 16×16、单一实体色、硬 Alpha，主体覆盖率分别为 23.8% 与 39.5%，M-A10 v0.2 QA **5/5 PASS**。Unity 读回两枚均为 16×16、PPU16、Point、Clamp、无 mipmap、Uncompressed；意图资产聚焦 EditMode **3/3 passed**，全量 EditMode **355/355 passed**；编译 0 error / 0 warning，Console 无 error，0 dirty scene；未保存场景、未进入 Play Mode。
- **完成后解锁：** 等待实际截图反馈，仅调整两枚图标的像素轮廓和留白，不扩展其他意图类型。

### UXINTENT-01：敌人常驻意图、实时伤害与移动落点预览 — COMPLETE（2026-08-11）

- **归属：** 剧情模式与肉鸽模式共用的战斗信息呈现层；未修改敌人 AI、伤害公式、移动规则、技能、数值或回合结算。
- **目标：** 修复敌人意图被后绘制地图块遮挡的问题；让每名敌人头顶常驻 16×16 意图图标，伤害意图显示实时预计伤害，并在悬停移动敌人时高亮其权威目标地块。
- **涉及文件/系统：** `EnemyIntentPresentation`/敌方计划公开边界、`CombatPrototypeBootstrap` 战场分层与单位头顶意图、`FormalArtRegistry`、`FormalArtImportPostprocessor`、`FormalIntentIcons16`、M-A10 QA、`CombatInformationPresentationTests`、`CombatBoundaryTests`、`FormalArtAssetAuditTests`。
- **验收记录：** 地块、物件与单位完成绘制后才绘制移动落点、全部敌人头顶意图和悬浮卡，消除逐格循环的后绘制遮挡；悬浮移动敌人时按与实际执行命令相同的签名读取 `Destination` 并用青色选择框高亮，不需要坐标轴。公开意图只新增只读 `IconId / Destination / ExpectedDamage`，不暴露可执行 `CombatCommand`；武器与伤害技能通过当前朝向、掩体、护甲、格挡、护盾和状态的权威预估计算护盾+生命承伤，每次 IMGUI 刷新重新生成展示，攻击/施术图标旁常驻伤害数字。新增攻击、施术、移动、防御、破坏五枚独立 16×16 图标，硬 Alpha、2–4 色、覆盖率 25.8%–48.1%，M-A10 QA **5/5 PASS**；Unity 读回全部 16×16、PPU16、Point、Clamp、无 mipmap、Uncompressed。聚焦 EditMode **8/8 passed**，全量 EditMode **355/355 passed**；Funplay 编译 0 error / 0 warning，Console 无 error，0 dirty scene，无 `.unity` Git diff；未保存场景、未进入 Play Mode。
- **完成后解锁：** 依据实际截图仅微调图标头顶偏移、数字宽度与移动落点对比度；不自动扩展玩法。

### UXCOPY-01：战斗法术、法宝与敌人玩家文案/图标补全 — COMPLETE（2026-08-11）

- **归属：** 剧情模式与肉鸽模式共用的战斗信息呈现层；只补全现有法术、法宝和敌人的图标与玩家文案，未修改伤害、消耗、目标判定、敌人 AI、内容或战斗规则。
- **目标：** 清除战斗悬浮说明中的实现枚举和开发式术语，让法术效果/目标/时机、法宝风险以及敌人生命/护盾/以太/意图均可用图标配合简洁中文直接读懂。
- **涉及文件/系统：** `CombatPrototypeBootstrap`、`CombatInformationPresentation`、`FormalCombatHud`、`RogueliteSettlementPresentation`、`FormalUiThemeTests`、`CombatHoverInformationTests`；复用既有正式 16×16 语义微图标、32×32 法术/法宝/敌人意图及生命/护盾图标。
- **验收记录：** 火术战斗预览不再显示 `CombatAffinity / DeliveryMode / TargetKind / Shape / TriggerWindow` 等实现枚举，现有 60 项火术按规则生成效果、目标、范围与触发时机中文；全部火术规则/条件枚举均有玩家短句门禁。法宝按钮保留正式内容图标、行动成本并在存在风险时显示“注意”图标。敌人悬浮卡顶栏以生命、护盾、以太和当前意图图标传达即时状态，正文保留特点、自然语言战法、状态和权威当前意图；技能效果不再回退为英文枚举。聚焦 EditMode **4/4 passed**，全量 EditMode **352/352 passed**；Funplay 重编译成功，0 error / 0 warning，Console 无 error，0 dirty scene，无 `.unity` Git diff；未保存场景、未进入 Play Mode。
- **完成后解锁：** 等待实际界面截图或试玩反馈，仅对单项文案长度、图标留白与信息密度做可逆微调；不自动扩展玩法或内容。

### UXICON-02：战斗语义微图标 16×16 实装 — COMPLETE（2026-08-11）

- **归属：** 剧情模式与肉鸽模式共用的玩家信息呈现与正式非人物美术层；只改变 `行动 / 以太 / 注意` 三枚语义词汇的源资产规格与显示素材，未修改资源消耗、技能、法宝、敌人、数值或战斗规则。
- **目标：** 将三枚已确认内容的极简图标从 32×32 改为 16×16 正式源资产，在 1920×1080 的既有 32 px UI 槽中以 2×、960×540 以 1×整数显示，避免为微图标引入无效细节。
- **涉及文件/系统：** `OCC_美术规范_v0.1.md`、`FormalArtImportPostprocessor`、`FormalArtAssetAuditTests`、`FormalResourceIcons32/action_point|mana|notice`、M-A9 16 px 主资产/QA 与部署脚本。
- **验收记录：** 正式内容分别为“指令筹码 + 短箭头”“六边形能量核心 + 亮点”“独立粗感叹号”；三张均为严格 16×16、硬 Alpha、2–3 个实体色，M-A9 v0.3 QA **3/3 PASS**。Unity 定向重导读回三张均为 Sprite、16×16、PPU16、Point、Clamp、无 mipmap、Uncompressed；其他正式图标继续由门禁要求 PPU32。全量 EditMode **350/350 passed**，Funplay 重编译成功、Console 无项目 error，场景 dirty=false；未保存场景、未进入 Play Mode。
- **完成后解锁：** 依据实际界面截图只调整三枚微图标的 RectTransform 留白或色值；不再提升源尺寸或增加物件细节，也不自动扩展到其他 32×32 内容图标。

### UXICON-01：战斗语义词图标化与悬停释义 — COMPLETE（2026-08-11）

- **归属：** 剧情模式与肉鸽模式共用的战斗信息呈现层；只调整法术、法宝与敌人说明的视觉词汇和玩家文案，未改变技能、物品、敌人、数值或战斗规则。
- **目标：** 用正式像素图标替换“行动”“以太”“注意”三个常驻词，并让鼠标悬停显示词义；同时把法术、法宝和敌人说明改成效果优先的玩家语言。
- **涉及文件/系统：** `FormalArtRegistry`、`FormalUiKit`/`FormalHoverTooltip`、`FormalCombatHud`、`RogueliteSettlementPresentation`、`FormalRogueliteUi`、`TarkovInventoryPanel`、`CombatInformationPresentation`、战斗语义图标生成/QA 与相关 EditMode 测试。
- **验收记录：** 三者统一登记为 `action/aether/notice` 语义词汇；战斗技能按钮、奖励卡、法宝档案、背包详情和敌人悬浮卡均只常驻显示数值/效果，鼠标悬停图标显示“行动/以太/注意”。法术奖励由实现枚举改为按规则生成的伤害、状态、位移、护盾、火场等玩家短句；法宝保留来源、每次消耗、目标、效果、限制和剩余次数；敌人改为生命/防御、特点、自然语言战法和当前意图，去除 `CD` 与重复操作说明。根据玩家审美反馈完成图标 v0.2 重绘：AI 仅提供物件造型/材质概念，正式 32×32 独立重绘为无内置边框的行动棘轮继电器、密封以太压力芯与危险区校准表，移除闪电、水滴和交通警示三角隐喻；单图 11–14 色、硬 Alpha、主体覆盖率 50.7%–53.8%，QA **3/3 PASS**。Unity Sprite/Point/Clamp/PPU32/无 mipmap 读回通过；全量 EditMode **350/350 passed**，Funplay 编译 0 error / 0 warning，Console 无 error，`CombatPrototype.unity isDirty=false`；未保存场景、未进入 Play Mode。
- **完成后解锁：** 依据实际试玩反馈决定是否将同一语义词汇扩展到非战斗商店与地图资源栏；不自动改变玩法范围。

### UXRED-01：玩家界面信息减法与通用图标化 — COMPLETE（2026-08-11）

- **归属：** 剧情模式与肉鸽模式共用的正式玩家界面表现层；只精简现有信息与补充非人物通用 UI 图标，未改变玩法、内容、数值或存档语义。
- **目标：** 将“图标 + 短标签 + 按需详情”落实到入口、地图、简报、设置、档案、战斗 HUD、背包与奖励结算，移除重复说明、文字分隔线及开发式措辞。
- **涉及文件/系统：** `FormalRogueliteUi`、`FormalCombatHud`、`TarkovInventoryPanel`、`RogueliteSettlementPresentation`、`CombatInformationPresentation`、`BattlefieldPresentationAdapter`、展示日志/反馈合同、`FormalArtRegistry`、正式导航图标生成器与 EditMode 资产/文案测试。
- **验收记录：** 正式玩家路径中的 `//`、确定性快照/结算、开发原型等内部语言已清除，重复的背包操作说明、显而易见的行动序列说明、按钮“点击切换状态”及奖励结算长说明已删减；空详情按钮不再创建冗余文本层。新增 `home/continue/archive/settings/back/confirm/save/close` 共 8 张独立 32×32 通用导航图标并接入入口、地图、简报、设置与档案按钮，仍保留短标签和禁用原因；QA JSON **8/8 PASS**，硬 Alpha、每张 5–6 色、Sprite/Point/Clamp/PPU32/无 mipmap 通过。全量 EditMode **344/344 passed**；Funplay 域重载恢复成功，编译 0 error / 0 warning，Console 无 error，`CombatPrototype.unity isDirty=false`，无 `.unity` Git diff；未保存场景、未进入 Play Mode。
- **完成后解锁：** 等待实际试玩反馈，再决定是否针对单一页面做第二轮信息密度调整；不自动扩展内容或玩法。

### UXVIS-08：跨页面动效、可访问性与最终体验回归 — COMPLETE（2026-08-11）

- **归属：** 剧情模式与肉鸽模式共用的正式表现与交互层最终回归；未新增玩法、事件、地图、敌人、法宝、角色、资源规则或数值。
- **目标：** 收口跨页面动效、回执、焦点、Toast/模态、0%/100% 动画、非颜色编码、全局字体/对比度、双分辨率布局与正式非人物美术缺口。
- **涉及文件/系统：** `FormalUiTheme/FormalUiKit`、`FormalRogueliteUi`、`FormalUiEffects`、`TarkovInventoryPanel`、`RogueliteSettlementPresentation`、周边 UI 配置/生成器/背景资产、`FormalArtAssetAuditTests`、`FormalUiThemeTests` 与 `UiLayoutContractTests`。
- **验收记录：** 高对比和大号文字已下沉至共用主题/响应式字号，正式 uGUI 页面及背包 IMGUI 统一消费；正文/说明对比与字号增量有纯测试门禁。新增全部登记布局的 1920×1080、960×540 锚点投影回归；页面/模态/Toast/按钮/结算动效继续保证 0% 立即终态且文字、图标、边框不依赖动画或颜色单独传意。复用正式周边资产生成器新增 `inventory.png` 与 `settlement.png` 两张独立 480×270 工业魔导背景并接入背包/结算；QA 图和 JSON 共登记 26 项，硬 Alpha、固定尺寸、受控调色板、Sprite/Point/Clamp/无 mipmap 审计通过。聚焦 **42/42 passed**；全量 EditMode **343/343 passed**。Funplay 编译 0 error / 0 warning，Console 无 error，`CombatPrototype.unity isDirty=false`，无 `.unity` Git diff；未保存场景、未进入 Play Mode。
- **完成后解锁：** `OCC_体验与视觉收口开发计划_v0.1` 完成；等待玩家实际试玩反馈或下一份批准计划，不自动扩展内容范围。

### UXVIS-07：入口、存档、结算与设置页面收口 — COMPLETE（2026-08-11）

- **归属：** 剧情模式与肉鸽模式共用的现有入口、存档保护、战前简报、胜负/奖励结算与设置表现层；未新增剧情、事件、存档槽规则或玩法后果。
- **目标：** 收口新局/继续、存档缺失/损坏/写入失败、简报、胜负/奖励结算、返回入口、设置和开发隔离中的反馈、确认与恢复路径。
- **涉及文件/系统：** `UiInteractionContracts`、`CombatPrototypeBootstrap`、`FormalRogueliteUi`、`RogueliteSaveGateway` 既有验证路径与 `UiInteractionContractsTests`。
- **验收记录：** 新增纯展示 `MapSaveUiPresentation`，将暂无存档、可继续、坏档受保护、存储异常和最近写入失败转换为明确非颜色文案。新推进必须在首次 `SaveMapRun` 成功后才进入地图；覆盖确认会先读验旧槽，有效档走既有可回滚替换，损坏/非法档只在确认后清除主槽写锁且保留首份损坏备份，存储异常不启动新推进。地图返回入口前强制再保存，失败则保留内存状态并停在当前页；设置持久化失败会说明“本次生效但未保存”。启动、简报、胜负、奖励、确认取消/焦点恢复和默认关闭的开发工具复核无新增断链。聚焦 **22/22 passed**；全量 EditMode **341/341 passed**。Funplay 编译 0 error / 0 warning，Console 无 error，`CombatPrototype.unity isDirty=false`，无 `.unity` Git diff；未保存场景、未进入 Play Mode。
- **完成后解锁：** `UXVIS-08` 已成为唯一当前主任务。

### UXVIS-06：背包、搜刮与快捷栏体验收口 — COMPLETE（2026-08-11）

- **归属：** 剧情模式与肉鸽模式共用的现有背包、搜刮和快捷栏表现/交互层；未新增物品、容器、资源规则或数值。
- **目标：** 在既有鼠标拖拽基线上补齐键盘整理/快捷栏/搜刮闭环，并让 AP、相邻和背包落位失败在提交前可读。
- **涉及文件/系统：** `TarkovInventoryPanel`、`InventoryInteractionPresentation`、`InventoryInteractionPresentationTests`、既有库存/搜刮命令入口。
- **验收记录：** 保留 6×10 网格的尺寸/朝向、拖拽、右键旋转、合法/非法落点、原子提交和完整物品详情；新增按占格中心计算的稳定方向键选择、`R` 旋转、数字 `1–8` 快捷栏关联、`F` 逐项搜索/拿取及 `B/Esc` 返回。搜索文本框聚焦时快捷键暂停，避免吞掉输入。搜刮区常驻相邻、1 AP 和已清空原因；已揭示战利品用与实际提交相同的 `FindFirstFit` 显示自动落位或背包无合法空位，所有操作仍走既有确定性命令和跨战斗持久化。新增聚焦测试 **5/5 passed**；全量 EditMode **340/340 passed**。Funplay 域重载恢复成功，0 error / 0 warning，Console 无 error，0 dirty scene，无 `.unity` Git diff；未保存场景、未进入 Play Mode。
- **完成后解锁：** `UXVIS-07` 已成为唯一当前主任务。

### UXVIS-05：商店、工坊与奖励页面交互收口 — COMPLETE（2026-08-11）

- **归属：** 肉鸽模式现有商店、工坊和战后奖励表现/交互层；未新增商品、装备、奖励、资源规则、养成数值或事件内容。
- **目标：** 统一成本、拥有/已领取、可执行性、失败、比较与提交回执，消除库存满和装备不兼容导致的正式流程异常。
- **涉及文件/系统：** `RogueliteEconomyPresentation`、`FormalRogueliteUi`、`RogueliteSettlementPresentation`、`UiPresentationModelsTests`。
- **验收记录：** 新增只读经济操作合同，商店/节点选项在提交前显示成本、余额不足、已拥有和背包所需空位；不可执行项禁用但保留明确原因。工坊卡显示已装备状态、武器伤害/射程/穿甲相对值及已装备术式不兼容恢复建议；校准继续复用一次性状态和资源变化。战后奖励卡预检现有 `FindFirstFit`、跳过禁用卡恢复首焦点，保留重复提交锁，并在异常时恢复可用卡和显示失败信息；成功仍通过既有资源/构筑/背包版本与 `RewardClaimed` 事件即时刷新。新增 3 项聚焦合同覆盖货币不足、背包满和术式不兼容，**3/3 passed**；全量 EditMode **335/335 passed**。Funplay 域重载恢复成功，0 error / 0 warning，Console 无 error，0 dirty scene，无 `.unity` Git diff；未保存场景、未进入 Play Mode。
- **完成后解锁：** `UXVIS-06` 已成为唯一当前主任务。

### UXVIS-04：地图页面与路线交互收口 — COMPLETE（2026-08-11）

- **归属：** 肉鸽模式现有地图页面的表现与输入层；未新增地图、节点、事件、资源规则或时间压力。
- **目标：** 统一顶栏/资源、节点/路径/图例/详情的视觉语义，并闭合存档恢复、选择、检查、确认、取消和返回焦点。
- **涉及文件/系统：** `FormalRogueliteUi`、`RogueliteMapVisualPresentation`、`FormalUiPageChecklist`、`UiPresentationModelsTests`。
- **验收记录：** 新增地图纯展示合同，为七类节点状态和五类路径状态建立唯一中文标签/字符标记，不再仅靠颜色；节点卡、路径标记和图例同步。地图默认焦点从“主菜单”改为当前节点，所有节点使用 `map.node.{id}` 稳定键，选择重建后焦点保持；未知节点现可检查但只显示“尚未侦测/连接信息未公开”。详情常驻节点类型、摘要、已知连接、权限/不可直达原因、回访安全和进入结果；真正移动仍只调用既有 `CanTravelTo`/`SelectMapNode`，未改变地图拓扑、状态、收益或规则。新增 3 项地图聚焦合同，**3/3 passed**；全量 EditMode **332/332 passed**。Funplay 域重载恢复成功，0 error / 0 warning，Console 无 error，0 dirty scene，无 `.unity` Git diff；未保存场景、未进入 Play Mode。
- **完成后解锁：** `UXVIS-05` 已成为唯一当前主任务。

### UXVIS-03：战斗动画与结果反馈收口 — COMPLETE（2026-08-11）

- **归属：** 剧情模式与肉鸽模式共用的战斗表现与信息反馈层；未修改战斗数值、AI、行动结算、事件内容、资源规则或场景。
- **目标：** 收口单位聚焦、移动、攻击、技能、受击、资源/状态/物件结果、敌方逐单位行动和胜负反馈的时序、互斥与 0% 动画降级。
- **涉及文件/系统：** `CombatVisualFeedback`、`EnemyTurnSequence`、`FormalUiEffects`、`CombatPrototypeBootstrap`、`CombatFeedbackEventTests`、`EnemyTurnSequenceTests`。
- **验收记录：** 保留敌方行动“聚焦→结算停留→单位间隔”序列及其并发拒绝合同；单位动作继续按单位 ID 单通道覆盖，并新增按格子单通道的正式 VFX 互斥。资源缓存补齐以太恢复，状态差分补齐自然移除/消退的通用净化反馈，显式结算同步缓存以避免重复播报。统一 `CombatFeedbackPresentationPolicy`：动画 0% 时不再播放单位运动、格子 VFX、脉冲、浮字漂移/淡出或聚焦闪烁，但静态敌方行动提示、胜负卡、浮字结果与既有结算停留仍保留；100% 路径继续使用原正式帧、单位动作和结果色。新增 2 项聚焦合同覆盖动画阈值与状态移除差分，**2/2 passed**；全量 EditMode **329/329 passed**。Funplay 域重载恢复成功，0 error / 0 warning，Console 无 error，`CombatPrototype.unity isDirty=false`，无 `.unity` Git diff；未保存场景、未进入 Play Mode。
- **完成后解锁：** `UXVIS-04` 已成为唯一当前主任务。

### UXVIS-02：战斗 HUD 信息层级与操作闭环 — COMPLETE（2026-08-11）

- **归属：** 剧情模式与肉鸽模式共用的战斗 HUD 呈现与输入闭环；未修改战斗数值、AI、行动结算、事件内容、资源规则或场景。
- **目标：** 统一常驻/选中/悬停三层信息，以及移动、攻击、技能、互动、搜刮、物品和结束行动的选择、预览、确认、失败与取消路径。
- **涉及文件/系统：** `FormalCombatHud`、`CombatInformationPresentation`、`CombatPrototypeBootstrap`、`RuntimeUiEventSystem`、`FormalHoverTooltip`、`TarkovInventoryPanel`、`CombatHoverInformationTests` 与战斗 HUD 展示模型。
- **验收记录：** “本轮行动”在 96 px 模块内新增两行决策摘要，常驻显示行动者/AP、所选行动、成本、可执行性、目标与预计结果；完整目标资料、敌方意图、伤害/状态结果与失败原因继续由同一悬停/键盘焦点浮窗承载，行动序列和最近五条现场记录保持常驻。Esc/手柄返回改为先取消查看目标，再取消已装载物品或非默认行动，最后才请求离开。新增 `CombatTargetNavigationState`：`T`/右肩键进入，方向键/WASD/十字键移动，Enter/Space/南键确认，Esc/东键取消并恢复行动按钮焦点；确认仍调用既有 `HandleCellClick`，未复制或改变结算规则。新增 3 项聚焦合同覆盖决策摘要、取消优先级和选点边界。Funplay 域重载恢复成功，0 error / 0 warning，UXVIS-01+02 聚焦 **19/19 passed**，全量 EditMode **327/327 passed**，Console 无 error，`CombatPrototype.unity isDirty=false`，无 `.unity` Git diff；未保存场景、未进入 Play Mode。
- **完成后解锁：** `UXVIS-03` 已成为唯一当前主任务。

### UXVIS-01：统一 UI 视觉语言与基础组件 — COMPLETE（2026-08-11）

- **归属：** 剧情模式与肉鸽模式共用的正式 UI 呈现与输入基础；未修改玩法、数值、事件内容、资源规则、场景或正式人物资产。
- **目标：** 统一正式界面的面板、按钮、卡片、标签、图标槽、状态色、字号、间距、焦点、禁用态及悬停/按下/选中反馈，为后续页面收口建立唯一可复用视觉与交互基线。
- **涉及文件/系统：** `OCC_体验与视觉收口开发计划_v0.1.md`、`FormalUiKit`/`UiButtonFeedback`、`FormalCombatHud`、`FormalRogueliteUi`、`FormalStartupPresentation`、`FormalUiInteractionLayer`、`RogueliteSettlementPresentation`、`TarkovInventoryPanel`、`FormalUiThemeTests`。
- **验收记录：** `FormalUiTheme` 新增中性/抬升/交互/遮罩及生命、护盾、魔力语义色，锁定标题/正文/说明字号、8 px 间距阶梯、32 px 图标槽和最小 40 px 交互高度；`FormalUiButtonTone`/`FormalUiButtonPalette` 统一普通、主要、正向、警示、危险按钮的 normal/hover/pressed/selected/disabled/focus 状态。九类 `FormalUiPageChecklist` 登记入口、地图、简报、战斗、商店/工坊、背包/搜刮、结算、设置、档案的默认焦点、返回、禁用与空内容状态；入口和设置修复为稳定焦点键。正式页面统一读取语义令牌，并移除“完整切片、确定性规则、开发控制”等开发措辞。1920×1080 参考布局、960×540 compact 字号合同、鼠标/键盘焦点和动画 0%/100% 由现有布局/交互合同及新增 8 项主题测试覆盖；未获 Play Mode 授权，因此未新增运行时截图。Funplay 域重载恢复成功，0 error / 0 warning，聚焦 **8/8 passed**，全量 EditMode **324/324 passed**，Console 无 error，`CombatPrototype.unity isDirty=false`，无 `.unity` Git diff，未保存场景、未进入 Play Mode。
- **完成后解锁：** `UXVIS-02` 已成为唯一当前主任务。

### COMBAT-FLOW-01：敌方逐行动节奏与战斗反馈 — COMPLETE（2026-08-11）

- **归属：** 剧情模式与肉鸽模式共用的战斗执行协调与表现层；不修改敌人 AI、数值、AP、技能、伤害、行动顺序、公开意图或存档语义。
- **目标：** 将当前每帧连续结算的敌方回合改为逐单位、逐动作可观察执行；每名敌人先获得明显聚焦和行动提示，再结算同一份权威计划，保留移动/攻击/施术与受击反馈，结果停留后才结束该单位行动并切换下一名敌人。
- **涉及文件/系统：** 新增纯时序合同 `EnemyTurnSequence` 及 EditMode 测试；调整 `CombatPrototypeBootstrap` 的敌方回合协调、`CombatVisualFeedback` 的敌方聚焦/动作提示和动作时长；更新联合开发计划与 COMBAT-FLOW-01 验证记录。不修改 `.unity`、正式战斗数据、`EnemyTactics` 或 `EnemyTurnPlanBook` 的决策规则。
- **验收标准：** 同一时刻最多一名敌人处于执行流程；聚焦阶段不改变战斗状态；每名敌人只消费一次既有权威计划；移动、攻击和技能具有不同但不低于可读门槛的结果停留时间；上一行动完成前下一敌人不得结算；动画强度降低不跳过信息停留；异常命令仍能安全结束当前敌人回合。聚焦与全量 EditMode 通过，Funplay 重编译 0 error / 0 warning、Console 无 error、场景 dirty=false、无 `.unity` 改动。
- **验收记录：** 新增纯时序 `EnemyTurnSequence`，将每名敌人拆为聚焦、单次结算、结果停留、结束行动和行动间隔；初始节奏为聚焦 0.65 秒，移动/攻击/技能结果分别停留 0.70/0.90/1.00 秒，单位间隔 0.30 秒。`CombatPrototypeBootstrap` 只消费一次 `EnemyTurnPlanBook` 既有命令，`CombatVisualFeedback` 仅读取公开意图，并增加敌方行动标题签、当前单位脉冲及较慢的移动/攻击/施术反馈；未修改 AI、数值、行动顺序或玩法规则。5 项聚焦时序合同包含单次信号、动作差异、串行互斥和重置隔离，最终全量 EditMode **316/316 passed**。Funplay v0.6.0 重编译 0 error / 0 warning，Play Mode 运行态读回 `active=enemy_0; phase=Focus; focus=enemy_0; banner=True; text=敌方行动 · 铭盾卫 | 移动 → (7,2)`，并观察到下一行动者仅在前一流程结束后进入聚焦；退出后 Console 无 error、场景 dirty=0，未保存场景且无 `.unity` 改动。
- **完成后解锁：** 当前主任务恢复为空；下一轮可在玩家实际试玩反馈后单独微调表现秒数或补充双分辨率截图，不自动修改战斗规则。

### COMBAT-HUD-04：战斗侧栏信息层级与行动轨道重构 — COMPLETE（2026-08-10）

- **归属：** 剧情模式与肉鸽模式共用的战斗 HUD 呈现层；不修改行动结算、AI、数值、存档或场景。
- **目标：** 移除常驻“目标预览”模块，将当前行动与英雄资源/状态拆成独立信息组；把行动序列升级为可直接辨认当前行动者、先后顺序与阵营的轨道；将面向开发者的标题、原因和状态措辞改为自然游戏语言，保留现场记录。
- **涉及文件/系统：** `FormalCombatHud`、战斗 HUD 展示模型与相关 EditMode 测试、COMBAT-HUD-04 验证记录；保持 1920×1080 下 75% 战场 / 25% HUD，不修改 `.unity` 或正式玩法数据。
- **验收记录：** 右侧 HUD 已移除 `combat.target` 和常驻目标预览，拆为“本轮行动 / 英雄概况 / 接下来 / 现场记录”；英雄卡独立显示主手、状态和带精确数值的生命/护盾/以太。`CombatTurnTrackPresentation` 将当前行动者固定首位，其余按行动时间和稳定 ID 排序，HUD 用 5 个连线节点、序号、阵营色、当前行底色与“正在行动”状态呈现。玩家文案已删除“有效格、不可提交、权威、确定性快照、不写回、完整规则、真实意图”等开发措辞。聚焦 EditMode 7/7、最终全量 EditMode **311/311 passed**；Funplay 重编译 0 error / 0 warning，Play Mode 结构检查确认模块、5 节点及禁词 0 命中，Console error 为 0，退出后 `CombatPrototype.unity isDirty=false` 且无 Git diff。Funplay 捕获接口未包含运行时 ScreenSpace uGUI，故以运行态层级、矩形、文本和节点读回作为本轮视觉结构证据。
- **完成后解锁：** 在 Play Mode 下复核 1920×1080 与 960×540 的侧栏密度和悬浮信息；若仍有文本拥挤，再单独调整字体与间距，不扩展玩法。

### COMBAT-BOUNDARY-01：战斗玩家信息、开发诊断与 AI 权威边界 — COMPLETE（2026-08-10）

- **归属：** 剧情模式与肉鸽模式共用的战斗规则查询、敌方回合计划、正式 HUD 与开发入口隔离；不改变 AI 数值、战斗玩法、场景或存档语义。
- **目标：** 拆分常驻/悬停玩家信息与仅开发诊断，令 HUD 只渲染权威行动可用性和敌方计划；以同一份稳定的敌方回合计划驱动公开意图和执行，并在 Release 中隔离开发控制台、调试热键、强制胜负及靶场审计；使用正式像素图标和细线面板模板替换临时开发外观。
- **涉及文件/系统：** `CombatInformationPresentation`、战斗状态/解析器与 `EnemyTactics`、`CombatPrototypeBootstrap`、`FormalCombatHud`、`DeveloperConsolePanel`、相关 EditMode 测试、正式 HUD 图标/边框资产及 Importer QA；不修改 `.unity`、`Library/`、`Logs/` 或生成文件。
- **验收记录：** `CombatAvailabilityQuery` 将普通行动预览和格子失败原因集中委托给既有权威适配器；HUD 继续只读消费预览。`EnemyTurnPlanBook` 只在计划仓调用 `EnemyTactics.Choose`，公开意图和敌方执行从同一缓存命令派生，任一命令结算后显式失效；`EnemyIntentPresentation` 不再暴露或持有 `CombatCommand`。默认编译符号下 `DeveloperBuildGate.IsEnabled=false`，控制台不创建、不监听 F1/F2，强制胜负和靶场 API 同样拒绝执行；仅 `UNITY_EDITOR`/`DEVELOPMENT_BUILD` 加显式 `OCC_DEVELOPER_TOOLS` 才可开启。新增边界测试与正式资产审计覆盖查询委托、计划同源/失效、默认开发门控、展示模型不泄露命令及 5 个意图图标注册。复用已有 Point/Clamp、硬 Alpha、PPU32 的 32×32 图标、细线分层面板和悬浮模板，保留 1920×1080 的 75% 战场/25% HUD。用户已授权本工作树 Unity 缓存；Unity 6000.5.2f1 CLI 在本工作树完成全量导入/编译，并复跑 EditMode **309/309 passed**、0 failed、0 skipped。`CombatPrototype.unity` 无 Git diff、未进入 Play Mode；日志无项目编译错误，只有 Unity 公共 CDN 配置请求超时。未保存场景。
- **完成后解锁：** 将当前主任务恢复为空；在绑定本工作树的 Unity Editor 中补跑全量 EditMode，并在获授权后执行 1920×1080 / 960×540 Play Mode 视觉验收。

### INVENTORY-SCALE-01：多尺寸物品矩阵、背包占格美术与旧档迁移 — COMPLETE（2026-08-10）

- **归属：** 剧情模式与肉鸽模式共用的物品目录、格子背包、美术与持久化；未修改物品战斗效果、次数、AP、掉落池或 6×10 基础背包合同。
- **目标：** 让普通道具与 20 件正式法宝形成 `1×1 / 2×1 / 1×2 / 2×2 / 3×2 / 1×3` 多尺寸组合；为每件物品提供与占格宽高一致的独立背包像素素材，并安全迁移旧尺寸存档布局。
- **涉及文件/系统：** `ItemDefinition/ItemCatalog`、`ArtifactCatalog`、`InventoryContainerState`、`RogueliteMapRun` 的 `map10` 与 `map9` 旧布局迁移、`TarkovInventoryPanel`、正式法宝策划源、道具/美术合同、占格素材归一化工具和 5 项聚焦测试；未修改 `.unity`、正式 32×32 语义图标、缓存或生成工程文件。
- **验收记录：** 24 件正式物品中 21 件为多格，覆盖六种尺寸签名；24 张独立占格 PNG 严格等于 `宽×32 / 高×32`，保持硬 Alpha、Point/Clamp、PPU32 与原像素造型，拖拽旋转时美术同步旋转，HUD/奖励仍读取 32×32 语义图标。新档写 `map10` 并严格校验当前尺寸；旧 `map9` 先按冻结旧尺寸验证，再保持原位或按获得顺序确定性重排，实例、次数和快捷栏引用均不静默丢失。隔离 Unity 6000.5.2f1 聚焦 5/5、全量 EditMode 305/305；Funplay 在实际工程完成域重载恢复，0 error / 0 warning、Console 为空、非 Play Mode、`CombatPrototype.unity isDirty=false`。高分辨率母图经复核仅作为造型参考，未用其替换已批准像素清稿；QA 图与 JSON 已入库。未获授权，未执行 Play Mode 双分辨率视觉验收。
- **完成后解锁：** 当前恢复无进行中主任务；多格背包在 1920×1080 / 960×540 下的拖拽、旋转和悬停视觉验收仍需用户明确授权。

### INVENTORY-UX-01：背包物品悬浮、拖拽换位与拖拽右键旋转 — COMPLETE（2026-08-10）

- **归属：** 剧情模式与肉鸽模式共用的战斗背包呈现与输入层；未修改物品数值、掉落、行动点、存档格式或背包占格规则。
- **目标：** 背包格内物品经过时显示完整可读信息；按住左键可拖拽并预览合法/非法落点，拖拽过程中按右键切换朝向，松开左键仅在合法位置原子提交移动。
- **涉及文件/系统：** 新增 `InventoryInteractionPresentation` 与 `InventoryDragState`，扩展 `TarkovInventoryPanel` IMGUI 指针输入、物品悬浮卡、拖拽预览和交互提示，新增 6 项聚焦 EditMode 测试；复用 `InventoryContainerState.CanPlace/Move`，未修改 `.unity`、正式资产、缓存或生成文件。
- **验收记录：** 悬浮卡显示名称、类别/稀有度、实际占格/朝向、重量、剩余次数、来源、描述及法宝代价/目标/效果/风险/构筑用途；从物品任一占格按住左键即可拖拽并保留抓取偏移，拖拽中右键切换临时朝向，青色/红色预览区分合法与非法落点。松开左键时才通过同一 `CanPlace/Move` 规则原子提交，越界或占用失败保持原坐标和朝向；成功旋转移动可经背包字符串完整回读。隔离 Unity 6000.5.2f1 聚焦 6/6、全量 EditMode 300/300；Funplay 在实际打开的 `E:\数据库\OCC_Codex` 完成域重载恢复，0 error / 0 warning、Console 为空、非 Play Mode、`CombatPrototype.unity isDirty=false`。未获授权，未执行 Play Mode 双分辨率视觉验收。
- **完成后解锁：** 当前恢复无进行中主任务；Play Mode 的 1920×1080 / 960×540 拖拽视觉验收仍需用户明确授权。

### COMBAT-CLARITY-03：战斗鼠标选择、敌人格子悬浮与底栏避让 — COMPLETE（2026-08-09）

- **归属：** 剧情模式与肉鸽模式共用的战斗输入及呈现层；未修改行动结算、敌人 AI、数值或存档语义。
- **目标：** 消除“结束行动”和旧 IMGUI“背包 / 搜索”入口的点击区域重叠；建立敌人右键查看选择通道，使其不与左键行动指令冲突；在鼠标经过敌人格子时直接显示可读的敌人资料与权威真实意图。
- **涉及文件/系统：** `TarkovInventoryPanel` 启动按钮布局、`CombatPrototypeBootstrap` 战场指针输入与敌人格子悬浮卡、`CombatInformationPresentation` 纯选择规则、`CombatHoverInformationTests` 与本待办；未修改 `.unity`、正式资产、缓存或生成文件。
- **验收记录：** “背包 / 搜索”入口迁移至右侧 HUD 底部 `(1472, 936, 416, 64)`，与战场底部指令栏无交叠；右键活着的敌人只更新查看目标，右键英雄/空格清除，左键只执行当前行动。敌人格子经过时显示生命、护盾、防御、武器、技能、状态及直接复用 `EnemyTactics.Choose` 决策结果的真实意图；悬浮卡限制在 75% 战场和指令栏上方，不创建可拦截点击的控件。短路径隔离 Unity 6000.5.2f1 聚焦 8/8、全量 EditMode 294/294 且进程正常退出；首次聚焦运行在结果写出后发生 Unity 域卸载崩溃，完整复跑未复现。Funplay 重编译 0 error / 0 warning，`CombatPrototype.unity` 保持 clean，编辑器已退出原有 Play Mode；Console 仅余与本改动无关的 UnityConnect Token Exchange 网络认证错误。本轮未新开 Play Mode、未执行双分辨率视觉验收。
- **完成后解锁：** 当前恢复无进行中主任务；后续候选仍为 `STARTER-BUILD-01` 与 `SOURCE-OF-TRUTH-01`，不自动扩展范围。Play Mode 的 1920×1080 / 960×540 鼠标交互视觉验收仍需用户明确授权。

### COMBAT-CLARITY-02：战斗 HUD 悬浮信息分层 — COMPLETE（2026-08-09）

- **归属：** 剧情模式与肉鸽模式共用的战斗呈现层；未修改战斗数值、敌人决策、结算或存档语义。
- **目标：** 复核右侧 HUD 与战场单位标签的信息密度，只让阶段、当前行动、提交合法性、关键资源、紧凑意图、时间线和战斗记录持续可见；将完整操作规则、敌人档案、权威真实意图、伤害公式、英雄完整状态、快捷栏物品详情与结算事件移入鼠标悬停/键盘导航焦点悬浮窗。
- **涉及文件/系统：** 新增 `FormalHoverTooltip` 通用射线透明悬浮层，扩展 `FormalCombatHud`、`CombatPrototypeBootstrap`、`CombatInformationPresentation` 与 4 项聚焦 EditMode 测试；保持 1920×1080 下 75% 战场 / 25% HUD，未修改 `.unity`、正式资产、缓存或生成文件。
- **验收记录：** 选中敌人不再把目标模块从 150px 扩到 510px，也不再隐藏行动序列和现场记录；常驻目标摘要保留生命/护盾、操作成本、合法或不可提交原因、预估结果与权威紧凑意图，完整敌人档案、`EnemyTactics.Choose` 同源真实意图和伤害公式进入目标悬浮窗。操作按钮、英雄状态、8 格快捷栏、重开/离开语义与结算最近事件均有按需浮窗；浮窗不拦截射线、跟随鼠标并受 Canvas 边界约束，程序默认选中不自动弹窗，键盘/手柄导航焦点仍可显示。战场敌人意图只对当前行动者、已选中或鼠标经过的单位显示。短路径隔离 Unity 6000.5.2f1 聚焦 4/4、全量 EditMode 290/290，编译无 C# error/warning；Funplay 在其绑定的 `E:\数据库\OCC_Codex` 检出完成编辑器健康检查，0 error / 0 warning、Console 无 error、非 Play Mode、`CombatPrototype.unity isDirty=false`。本工作树无 `.unity` 改动；未获授权，未执行 Play Mode 的 1920×1080 / 960×540 悬停视觉验收。
- **完成后解锁：** 后续可单独立项 `STARTER-BUILD-01` 或 `SOURCE-OF-TRUTH-01`；Play Mode 双分辨率悬停视觉验收仍需用户明确授权，不自动进入。

### SAVE-INTEGRITY-01：存档语义校验与坏档写保护 — COMPLETE（2026-08-09）

- **归属：** 剧情模式与肉鸽模式共用的持久化基础设施；本轮保持 `map9` 字段顺序和版本号不变。
- **目标：** 为 `map9` 增加节点、资源、生命、构筑、奖励、背包实例与快捷栏引用不变量；建立跨 Gateway 实例持久有效的坏档备份和写锁，并执行保存前与回读后复核。
- **涉及文件/系统：** 新增 `RogueliteMapRunValidator`，扩展 `RogueliteMapRun`、`RogueliteSaveGateway`、地图入口错误反馈、Gateway 测试与新增语义篡改测试；未修改场景、正式资产或 `map9` 字段顺序。
- **验收记录：** `CorruptData`、`InvalidSemantics`、`StoreError` 与 `Missing` 已分离；节点、资源、战斗快照、构筑、奖励/选择、背包和快捷栏八类篡改均保留主槽原文、不可覆盖的首份备份和 `.write_lock` 持久锁。新 Gateway 仍拒绝覆盖；只有显式删槽同时清主槽和锁，备份保留。保存前执行序列化/重解析/语义校验，回读要求原文完全一致并再次验证；回读失败会恢复原主槽并加锁。合法 `map9` 往返确定，合法 `map8` 可迁移为有效 `map9`。隔离 Unity 6000.5.2f1 全量 EditMode 286/286；Funplay 在其绑定的 `E:\数据库\OCC_Codex` 检出完成重编译，0 error / 0 warning、Console 无 error、非 Play Mode、`CombatPrototype.unity isDirty=false`，该结果仅作编辑器健康检查，本工作树证据以隔离编译与测试为准。Git 无 `.unity` 改动；未获授权，未执行 Play Mode 视觉检查。
- **完成后解锁：** `STARTER-BUILD-01` 与 `SOURCE-OF-TRUTH-01` 可在后续单独立项；不自动扩展存档字段或玩法内容。

### COMBAT-CLARITY-01：战斗信息、反馈与失败结算重构 — COMPLETE（2026-08-09）

- **归属：** 剧情模式与肉鸽模式共用的战斗呈现与结算基础设施；不改变剧情探索规则，也不引入时间压力。
- **目标：** 让阶段、当前行动、目标、敌人数值/技能/意图、提交前结果、实际伤害和失败后果全部可读；敌人意图与真实 AI 命令同源，失败不静默覆盖战前地图存档。
- **涉及文件/系统：** 新增 `CombatInformationPresentation`，扩展 `BattlefieldPresentationAdapter`、`UiPresentationModels`、`CombatPrototypeBootstrap`、`FormalCombatHud` 与聚焦 EditMode 测试；未修改场景、正式资产、敌人数值或 AI 规则。
- **验收记录：** 删除 HUD 旧 `GetEnemyIntent` 启发式，展示与执行均直接消费 `EnemyTactics.Choose` 的权威命令签名；敌人档案、阶段/操作、提交前后生命护盾、完整伤害分解、结构化行动记录、最近 5 条事件和失败结算已接入。肉鸽失败不再捕获或保存战败后的背包/生命状态，胜利经专用结算入口写回，战术重开继续使用 `CombatFlowController.RestartSnapshot`。隔离 Unity 6000.5.2f1 编译成功，全量 EditMode 272/272；Funplay 确认活动场景未脏且未处于 Play Mode，但当前 MCP Editor 绑定在 `E:\数据库\OCC_Codex` 而非本工作树，因此不把其重编译回报计作本工作树证据。Play Mode/1920×1080/960×540 视觉验收未获授权，未执行；Git 无 `.unity` 改动。
- **完成后解锁：** `SAVE-INTEGRITY-01` 已切换为唯一当前主任务。

### CORE-INTEGRITY-01：战斗快捷栏单一数据源与跨战斗消耗品一致性 — COMPLETE（2026-08-09）

- **归属：** 剧情模式与肉鸽模式共用的战斗物品、快捷栏、存档和 HUD 基础设施；不改变剧情探索规则，也不引入时间压力。
- **目标：** 移除 `CombatState.Quickbar` 与 `ItemQuickbar` 双轨状态，以实例背包和八格实例快捷栏作为唯一权威数据源；消除每场战斗重复赠送医疗包/护盾电池的问题，并让正式 HUD、场景 HUD、背包、战斗捕获和 `map9` 读取同一状态。
- **涉及文件/系统：** `CombatState`、`CombatResolver`、`CombatPrototypeBootstrap`、`FormalCombatHud`、`TacticalHudSceneBinder`、`UiPresentationModels`、`RogueliteMapRun`；物品栏/法宝/火术/存档/完整路线 EditMode 测试；实施计划 `OCC_CORE-INTEGRITY-01_实施计划_v0.1.md`。不保存场景，不修改缓存、恢复文件或无关正式资产。
- **验收记录：** 新局只创建一组初始消耗品，耗尽后跨战斗不重生；普通 `CombatState` 默认为空；八槽、特殊物品四件上限、1 AP 换入、目标预览与 `map9` 往返均有回归。Funplay 重编译 0 error / 0 warning，全量 EditMode 266/266，Console 无 error，`CombatPrototype.unity` 为 `isDirty=false`，Git 无场景改动。Play Mode 与双分辨率视觉检查未获授权，未执行。
- **完成后解锁：** `SAVE-INTEGRITY-01` 存档语义不变量与坏档写保护；随后是 `STARTER-BUILD-01` 通用初始武器权威数据和 `SOURCE-OF-TRUTH-01` 策划/世界观源文件收敛。

### REPO-BASELINE-01：GitHub 私有仓库与版本管理基线 — COMPLETE（2026-08-08）

- **归属：** 剧情模式与肉鸽模式共用的项目开发管理；不改变玩法、世界观、Unity 场景或正式资产。
- **目标：** 保留现有本地 Git 历史，创建私有 GitHub 远程仓库，整理 Unity 忽略规则，将当前正式源码、测试、文档与资产纳入可复现的版本基线并推送 `main`。
- **涉及文件/系统：** `.gitignore`、本待办、本地 Git 索引与提交历史、GitHub 远程仓库；排除 `Library/`、`Logs/`、`UnityProject/Artifacts/`、`Assets/_Recovery/` 等缓存、验证输出和恢复文件。
- **验收标准：** 私有远程仓库创建成功；`origin` 指向该仓库；正式文件及 Unity `.meta` 已跟踪；缓存、恢复场景和生成物未进入提交；基线提交成功推送，`main` 与 `origin/main` 一致。
- **完成记录：** 已建立私有仓库 `https://github.com/whitefool124/ooc`，`origin` 使用 HTTPS 并由 Git Credential Manager 验证访问。基线提交 `82e066e` 纳入 1,856 个正式文件、124,380 行新增与 991 行删除；`.gitignore` 新增排除 `UnityProject/Artifacts/`、`UnityProject/Assets/_Recovery/` 及其 `.meta`，恢复场景和生成截图均未进入提交。`main` 已成功推送并跟踪 `origin/main`；本任务未修改 Unity 场景、玩法或正式资产，也未进入 Play Mode。
- **完成后解锁：** 后续功能按独立分支和可审查提交进行，支持远程备份、回滚及协作开发。

### ARTIFACT-PACK-20：20 件通用法宝设计、美术、运行时与正式流程全链路 — COMPLETE（2026-08-08）

- **归属：** 剧情模式与肉鸽模式共用的法宝、物品实例与确定性战斗内容；剧情探索不引入倒计时、敌情推进、拖延关门或其他时间压力。
- **目标：** 在主角 14 岁入学期、第一次工业革命前的时代基线与以太素唯一超凡来源下，完成 `F-T01` 与 `G-T01–G-T19` 共 20 件正式法宝；一次交付设计、独立图标、通用执行器、预览/结算、背包/快捷栏、奖励/商店/事件/战利品、`map9` 及后续存档、正式 UI、自动化与 Funplay 实机验收。20 件中至少 15 件不绑定单一元素或火系构筑；当前冻结分布为 19 件通用、1 件火倾向。
- **涉及文件/系统：** `OCC_通用法宝内容包_v0.1.md`、`OCC_魔法技能与道具内容合同_v0.1.md`、`OCC_三层内容池配置_v0.1.md`、本待办、后续实现矩阵/验证/技术方案；`ArtifactCatalog`、通用法宝执行器、`ItemInventorySystem`、目标预览/战斗日志/VFX、普通/精英/宝藏/首领与商店/事件/战利品池、工坊/档案、`map9` 迁移与坏档保护、正式背包/奖励/战斗 UI、20 张独立 `32×32` 图标及母图/调色板/QA、EditMode/必要 PlayMode/Funplay 双分辨率回归。保留并整合工作树既有改动，不修改缓存、人物资产或场景 YAML，不保存场景。
- **验收标准：** 20/20 稳定 ID、名称、来源、稀有度、占格/重量、次数/有限反应、目标、公开代价、反制/风险、构筑用途和机械身份唯一；`F-T01` 完成旧“爆破筒”到“炎脉封装筒”的兼容修订；20/20 至少一个正常流程内容池可达并可真实使用；合法/非法预览、AP/次数、伤害/状态/位移/地形/资源/反应、友军波及、边界/遮挡、双执行确定性、耗尽报废、`map9` 往返/迁移/坏档保护、UI 正式文案全部通过；20/20 图标独立且为 `Sprite/Point/Clamp/PPU32/无 mipmap`；全量测试、重新编译、Console、1920×1080/960×540 与场景 clean 通过。
- **完成记录：** 已交付 `F-T01 炎脉封装筒` 与 `G-T01–G-T19` 共 20 件正式法宝，分布为 19 通用/1 火倾向。通用目录与执行器覆盖合法/非法预览、AP/次数、状态/位移/地形/资源/反应、确定性日志和正式 VFX；背包/快捷栏、七类获取入口、map9/map8/坏档保护、战斗/奖励/档案 UI 已闭环。美术完成 20 母图与 20 张独立 32×32 清稿，图像/Importer QA 20/20。最终全量 EditMode 260/260、PlayMode 1/1、法宝运行时 7/7、TrainingRange 107/107；编译 0 error/0 warning，正常 Play Mode Console 空；1920×1080 与 960×540 战斗/背包/奖励回归通过，`CombatPrototype.unity` clean，未保存场景。实现矩阵见 `OCC_ARTIFACT-PACK-20_实现矩阵_v0.1.md`，技术方案见 `Worldbuilding/02_技术方案/OCC_通用法宝内容包技术方案_v0.1.md`，验证见 `OCC_通用法宝内容包验证_2026-08-08.md`。
- **完成后解锁：** 第二地区专属法宝、法宝经济价格、远古鉴定事件、机构许可证与后续卷轴/普通物品扩容；不自动解锁无限充能、随机故障、剧情探索时间压力或主角专属天赋进入肉鸽内容池。

### LEVEL-01：首区关卡系统与十敌人实装 — COMPLETE（2026-08-08）

- **归属：** 剧情/肉鸽共用战斗关卡运行时；本轮接入肉鸽首区九个战斗节点，不改变剧情探索规则，不引入倒计时或时间压力。
- **目标：** 新增数据驱动的关卡定义、目录、构建器和九张 12×9 手工关卡，替换首区“同图换名单”的运行方式；把已完成的十个当前时代敌人按精确出生位、地形、目标、技能/AI/正式表现接入真实关卡。
- **涉及文件/系统：** `OCC_首区关卡系统_v0.1.md`、`OCC_LEVEL-01_实施计划_v0.1.md`；关卡目录/构建器、`CombatPrototypeBootstrap`、`RogueliteEncounterCatalog`、肉鸽节点/任务文案、敌人目录/能力/AI/ArtId、EditMode/PlayMode 与 Funplay 双分辨率回归。保留旧 ID 兼容存档，不保存场景、不修改缓存或替换人物资产。
- **验收标准：** 九关具有稳定 ID、不同地图/目标/精确编成并可真实构建；十敌人全部进入活跃关卡；旧遭遇名单从关卡单一数据源派生；玩家可见首区内容与活跃战斗无铁路、步枪/狙击/现代枪械或现代爆破语义；全量测试、编译、Console、1920×1080/960×540 和场景 clean 通过。
- **完成记录：** 已交付九张数据驱动 12×9 关卡、28 个敌人精确放置、37 个地形放置和 3 个破坏目标关；十敌人全部通过关卡构建器进入真实状态，旧遭遇接口由关卡目录派生。Bootstrap 对九个首区 ID 不再读取场景敌人/地形 Marker；关卡名称、任务摘要、地面与火术法宝显示已迁到入学期时代语义。修复正式单位加载器按文件名猜路径的问题，`relay_raid/core_finale` 实战纹理缺失均为 0。关卡专项 9/9、全量 EditMode 242/242、PlayMode 1/1，编译 error/warning 0，最终 Console 空；1920×1080 与 960×540 回归通过，`CombatPrototype.unity` clean，未保存场景。验证见 `OCC_首区关卡系统验证_2026-08-08.md`，矩阵见 `OCC_首区关卡系统实现矩阵_v0.1.md`。
- **完成后解锁：** 独立选关入口、关卡变体/随机地形层、第二地区关卡包。

### ENEMY-PACK-01：当前时代特色敌人扩展包、战斗表现与像素资产 — COMPLETE（2026-08-08，10 敌人）

- **归属：** 剧情/肉鸽共用敌人运行时；本轮接入肉鸽首区全部九个固定遭遇，不改变剧情探索规则，也不引入倒计时或时间压力。
- **目标：** 按主角入学期“第一次工业革命前”技术基线，将扩展包从 3 个扩大为 10 个特色敌人：保留刻印锤手、屏障修补师、缚环猎兽，升级盾卫、火术师、突袭者、精英先锋，并新增石索缚师、显影灯使、重弩手；每个都具备独立轮廓、稳定 ID/数值、专属能力、确定性 AI、活跃遭遇、战斗 VFX 与可见动作。
- **涉及文件/系统：** `OCC_首区敌人扩展包_v0.1.md`、`OCC_ENEMY-PACK-01_实施计划_v0.1.md`；`EnemyArchetypes`、`EnemyAbilityCatalog`、敌人 AI、`CombatResolver/Bootstrap/VisualFeedback`、`RogueliteEncounterCatalog`、`FormalArtRegistry`、`FormalUnits64`、母图/规范化/Importer QA、EditMode/PlayMode 与双分辨率回归。不得保存场景、替换既有人物资产或修改缓存。
- **验收标准：** 10 个敌人的能力/AI/日志/遭遇均真实可执行且确定性；所有活跃首区遭遇无现代枪械/爆破语义；10 张互不复用的 64×64 单位图与动作/VFX 语义通过像素及 Importer QA；不会重复填充备用出生点；全量测试、编译、Console、1920×1080/960×540 和场景 clean 通过。
- **完成记录：** 已交付铭盾卫、火矢术师、钩刃突袭者、刻阵先锋、刻印锤手、屏障修补师、缚环猎兽、石索缚师、显影灯使、重弩手 10 个完整角色，具备 10 个独立技能/ArtId、确定性状态窗口 AI、真实 Resolver 结算、九遭遇可达和动作/VFX。新三张母图经独立规范化，十图统一通过 64×64、硬 Alpha、≤24 色、中心/基线 QA；十图 Importer 全量门禁为 Sprite/Point/Clamp/PPU32/无 mipmap。全量 EditMode 233/233、PlayMode 1/1，编译错误/警告 0，正常 Play Mode Console warning/error 0；1920×1080/960×540 实战通过，`CombatPrototype.unity` clean，未保存场景。验证见 `OCC_当前时代特色敌人扩展包验证_2026-08-08.md`，矩阵见 `OCC_当前时代特色敌人扩展包实现矩阵_v0.1.md`。
- **完成后解锁：** 10 敌人首区编成平衡、第二地区敌群与首领重做。

### FIRE-ROGUELITE-01：火元素个人术式完整肉鸽单局 — COMPLETE（2026-08-08）

- **归属：** 肉鸽模式；复用剧情/肉鸽共用的火元素个人术式与确定性战斗运行时，不改变剧情探索规则。
- **目标：** 将 60 项火术 v0.2 与 20 节点首区接成从正式开局选择、跨节点资源继承、相容奖励、工坊换装、存档继续到双首领结算的真实单局闭环。
- **涉及文件/系统：** `OCC_肉鸽模式玩法定义_v0.1.md`、本实施计划；`RogueliteMapRun`/存档网关、火术奖励池、战斗构建/退出捕获、正式入口/地图/工坊/结算/档案 UI、EditMode/必要 PlayMode 测试与 Funplay 双分辨率验证。不修改场景 YAML、人物正式资产、`Library`、`Logs`、缓存或无关用户内容。
- **验收标准：** M/U/R 三开局第一战均有正确武器与两项火术；生命/护盾/个人魔力跨战斗与 `map9` 保存恢复一致且无战后自动补满；普通奖励为 2 术式+1卷轴，高阶奖励为 1稀有术式+1卷轴+1法宝，术式与当前武器相容且领取不自动装备；3 开局×2 首领种子可完成全程；全量测试、编译、Console、1920×1080/960×540 与场景 clean 通过。
- **实施计划：** 见 `OCC_FIRE-ROGUELITE-01_实施计划_v0.1.md`；按策划冻结→开局/存档→状态继承→奖励/工坊/UI→多种子全程测试→Funplay 回归收口执行。
- **完成记录：** 已完成 M/U/R 三开局及首战两术式装载，`map9` 跨节点保存生命/护盾/个人魔力并由 map8 显式升级；战斗胜负捕获状态，下一战不自动补满。普通/高阶三选一按当前武器过滤，领取不自动装备，工坊拒绝非法组合；休整恢复真实资源，宝藏接入高阶火系池。正式入口、地图标题、档案与结算字段已同步。最终 EditMode 209/209、PlayMode 1/1，3开局×2首领种子完整路线通过，编译错误/警告 0；正常 Play Mode 项目日志 0 warning/error/exception，1920×1080 与 960×540 通过，`CombatPrototype.unity`/`TrainingRange.unity` clean，未保存场景。验证见 `OCC_火系完整肉鸽体验验证_2026-08-08.md`。
- **完成后解锁：** 火系数值平衡与事件/物品扩容；其余七元素可复用同一开局、奖励与跨节点状态合同。

### FIRE-REDESIGN-02：火元素个人术式 v0.2 全链路迁移与 60/60 运行时闭环 — COMPLETE（2026-08-08）

- **归属：** 剧情模式与肉鸽模式共用的火元素个人术式运行时；获取、奖励与长期继承按模式现有边界隔离，独立靶场仅作为开发验证入口。
- **目标：** 在不静默改变旧术式语义的前提下，将 `fire-personal-spells-v0.1` 的 50 项目录完整迁移到 `fire-personal-spells-v0.2` 的 M/U/R 各 20 项目录，打通数据、预览、确定性执行、结果事件、奖励、装备、HUD/配置、行动档案与存档恢复，并完成阶段二的全项目结构审查和可安全确认风险修复。
- **涉及文件/系统：** `OCC_火元素个人术式池_v0.1/v0.2`、迁移与运行时合同、实施矩阵；`FireSpellCatalog`、`FireSpellRuntime`、`FireSpellProgression`、`TrainingRange`、`CombatState/Resolver`、HUD/配置/结算/VFX、`RogueliteMapRun`/`RogueliteSaveGateway`、正式图标与 Importer、EditMode/必要 Play Mode 测试；阶段二覆盖 `Assets/Game`、`Packages` 与相关 `ProjectSettings`。不修改 `Library`、`Logs`、缓存、人物正式资产或无关用户改动。
- **验收标准：** 旧 50 项逐项具有可审计的直迁、公开同稀有度重选或明确补偿决策；v0.2 字段 `combat_affinity`、`delivery_mode`、`weapon_requirement`、`trigger_window`、`consumption_rule` 成为单一数据源并约束近战武器、近远程通用附着、远程施法三条路径；60/60 可装备且经真实预览与确定性执行，重复签名一致、内容池可达、存档恢复/升级无静默丢失；图标/VFX 语义正确且 32×32/Importer QA 通过；全量测试、Unity 编译与 Console 无项目错误/警告，1920×1080 与 960×540 回归通过，相关场景 clean；阶段二所有可安全确认的高/中风险完成修复与测试记录。
- **实施计划：** 见 `OCC_FIRE-REDESIGN-02_实施计划_v0.1.md`；顺序为合同与迁移冻结 → 目录/执行器升级 → 获取/存档/UI/档案接入 → 资产与 60/60 靶场门禁 → 阶段一 Unity 验收 → 结构审查与风险修复迭代 → 最终回归与文档收口。
- **完成记录：** 已冻结 50/50 迁移表（21 直迁、26 同稀有度公开重选、3 补偿），目录升级为 M/U/R 各 20 项及五字段合同，普通攻击/移动/反应事件接入附着、架势、反击、追击、警戒与火场结算；`map8` 保存迁移权益、装备槽与未知旧 ID 诊断，坏档先备份并禁止静默覆盖。60 项均可装备、内容池可达，靶场 60/60 合法与 60/60 非法预览、双执行签名一致；完整靶场审计 88/88。正式图标使用 39 张已审计语义资源并显式映射，全部正式纹理 Importer QA 通过。阶段二修复 UI 场景泄漏、未知敌人模板回退、能力型敌方策略、批量结算部分写入、火场死亡胜负刷新、Campaign v2 确定性转义存档、坏档保护与编辑器场景自动改脏风险。最终 EditMode 195/195、PlayMode 1/1，编译错误/警告 0、Console 空；1920×1080 与 960×540 回归通过，`TrainingRange.unity` clean，未保存场景、未替换人物资产。验证见 `OCC_火元素个人术式全量实现验证_2026-08-08.md` 与 `OCC_Unity代码结构审查与修复验证_2026-08-08.md`。
- **完成后解锁：** 火系 v0.2 可作为其余七元素的三路径运行时模板；结构审查残余风险进入后续独立任务，不与本轮已验证闭环混杂。

### FIRE-REDESIGN-01：火系近战/通用/远程个人术式库整体重构 — COMPLETE（2026-08-08）

- **归属：** 剧情模式与肉鸽模式共用的火系个人术式；玩法规则共用，获取与长期继承按模式隔离。
- **目标：** 不受旧 50 项实现成本约束，重构火系个人术式库，使近战与远程构筑在内容数量、破甲、防护、燃烧利用和终结能力上旗鼓相当，并补足身体强化、武器附着、接触导能与魔锻防具抗远程的设定依据。
- **涉及文件/系统：** `OCC_火元素个人术式池_v0.2.md`、八元素术式语法、游戏策划案、魔法内容字段合同；本任务不修改 Unity、场景、正式美术或既有 v0.1 审计文件。
- **验收标准：** 新目录严格为 20 项近战专用、20 项近远程武器通用、20 项远程施法专用；近战与远程各可使用 40 项；全部条目具备确定性成本、目标、效果、反制与合法时序；不增加新状态、随机命中、隐藏热量资源或第四内容层级；旧 ID 迁移边界明确。
- **完成记录：** 已建立 `F-P-M01–M20`、`F-P-U01–U20`、`F-P-R01–R20` 共 60 项 v0.2 目录。近战线覆盖接敌、局部身体强化、接触破甲、抗远程架势、反击牵制和终结；通用线覆盖单次武器热载、攻坚、防护、燃烧资源与协同；远程线收敛直伤、点燃、射线、火场、引爆和远程攻坚。同步补充魔锻防具、接触破防、身体强化和武装术式字段。旧 50 项保留审计，不允许静默同义迁移。
- **完成后解锁：** `FIRE-REDESIGN-02`：编制旧 50→新 60 的逐项迁移/补偿表、扩展运行时数据合同、重做奖励池与存档版本，并在靶场完成 60/60 实装验证。

### BIS-ART-01：新增背包/搜索/搜刮系统视觉补缺与实际接入 — COMPLETE（2026-08-08）

- **归属：** 剧情模式与肉鸽模式共用的正式物品 UI；独立靶场复用同一界面。
- **目标：** 审计新加的 6×10 背包、物品搜索、未知容器搜刮和 8 格快捷栏，把已经生产但未显示的分类/操作图标实际接入，并补齐旋转、清除条件、负重、未知/搜索中/空容器等缺失视觉语义，消除大面积纯色框和纯文字按钮。
- **涉及文件/系统：** `TarkovInventoryPanel`、`FormalArtRegistry.Items`、`FormalItemIcons32`、现有 `FormalUI32` 格位皮肤、图标生产脚本、Importer/像素 QA、双分辨率视觉回归和资产清单；人物与旧地图资产不在本轮范围。
- **验收标准：** 搜索、分类、旋转、清除、负重、搜刮状态和快捷栏均有无文字也可辨认的正式图标；6×10 格位使用空/占用/选中状态皮肤；列表和快捷栏显示真实物品图标；新增 PNG 为 32×32、硬 alpha、有限色、Sprite/Point/Clamp/无 mipmap；全量测试、Console、场景 clean。
- **当前记录：** 已完成。确认界面“素”的主要原因是接入缺失：已有分类/搜索/快捷栏/自动放入/使用图标未进入布局，格位也未使用现成状态皮肤。本轮把现有素材接入标题、搜索条件、结果卡、拿取按钮和快捷栏；6×10 使用中性格位皮肤与青色选中态；新增旋转、清除、负重、未知/搜索中/空容器 6 张正式图标，像素 QA 与 Importer 均为 6/6 PASS。未知内容显示独立锁定卡，已揭示物品显示真实图标卡。1920×1080/960×540 通过，全量 180/180，Console error 0；截图工具产生 RenderTexture 警告，QA 强制即时销毁启动层产生 1 条 DOTween Safe Mode 汇总警告，不来自正常玩家路径；`TrainingRange.unity` clean。验证见 `OCC_新增物品系统美术补缺验证_2026-08-08.md`。
- **完成后解锁：** 后续新增物品和容器状态可只提供数据/图标，不再扩写纯文字 UI。

### UI-MOTION-01：背包 HUD 视觉统一与战斗/地图动效收敛 — COMPLETE（2026-08-08）

- **归属：** 剧情模式与肉鸽模式共用的正式 UI 与战斗表现。
- **目标：** 将 6×10 背包统一到现有黑白极简工业 HUD 的色板、细线和层级语言；降低多格移动的单位位移幅度；取消地图节点选中时重复触发的全屏擦除动画。
- **涉及文件/系统：** `TarkovInventoryPanel`、`FormalRogueliteUi`、`UiMotionProfile`、`CombatVisualFeedback`、UI 合同测试与 1920×1080/960×540 运行时回归；不修改人物资产、不保存场景。
- **验收标准：** 背包沿用正式 HUD 的 Ink/Panel/Surface/Cyan/Text/Muted 主题、1 px 边界与有限强调色；单位移动反馈最大回溯位移不超过 28 px；地图节点浏览即时更新且不播放 2300 px 全屏擦除；编译、全量测试、Console 和场景 clean。
- **当前记录：** 已完成。背包已改为正式 HUD 同源主题与细线分区；多格移动回溯限制为 28 px、时长 0.18 秒；节点选择重建明确关闭动画，通用页面横移由 18 px 降为 6 px，普通页面也不再播放全屏擦除。最终证据见 `OCC_背包UI与动效收敛验证_2026-08-08.md`。
- **完成后解锁：** 后续背包拖拽、嵌套容器与更多地图节点只需沿用同一视觉和局部反馈合同。

### BIS-ALL：6×10 类塔科夫背包、法宝、搜刮、搜索、存档、UI 与美术全链路 — COMPLETE（2026-08-08）

- **归属：** 剧情模式与肉鸽模式共用的物品与确定性战斗基础设施；获取节奏和存档作用域按模式隔离。
- **目标：** 按 BIS-01–BIS-07 完成类《逃离塔科夫》的 6 列×10 行基础背包、矩形物品旋转、物品定义/实例、未知容器确定性逐项搜索、管理筛选、战场搜刮、快捷栏换入、法宝次数、`map7` 存档迁移、奖励/内容池、正式 UI 与非人物美术资产，并接入正常战斗和独立靶场。
- **涉及文件/系统：** 物品/库存/搜刮/法宝运行时，`CombatState`/`CombatResolver`/预览，`RogueliteMapRun` 与存档网关，正式战斗/背包/搜刮 UI，正式图标/VFX 注册与 QA，EditMode/Play Mode/双分辨率回归；不修改人物资产、不覆盖无关用户改动、不保存现有场景。
- **验收标准：** 基础背包严格为 6×10；定义与实例分离，稳定错误码和确定性实例 ID；放置/旋转/移动/交换/筛选/未知容器逐项揭示/多项搜刮/1 AP 搜索与换入/法宝耗次与报废完整；搜索无真实倒计时或隐藏随机速度；`map6→map7` 无静默丢失；F-S01/F-T01 内容池与专属图标可达；1920×1080/960×540 可读；全量测试、编译、Console 和相关场景 clean。
- **当前记录：** 已完成；完整证据见 `OCC_类塔科夫背包搜索系统验证_2026-08-08.md`。
- **完成记录：** 新增定义/实例分离的物品目录、确定性实例 ID、6×10 容器、0°/90° 放置、移动/交换/移除/first-fit/克隆、负重门槛、稳定错误码及纯查询筛选排序；保留旧 `InventoryGrid` API 并把默认容量升级到 60 格。未知 `LootSourceState` 按获得序号与实例 ID 稳定逐项揭示，战斗中每次搜索 1 AP，拿取已揭示物品不重复收费；无 AP 不泄露，已揭示/已取走状态进入 `map7`。`map6→map7` 迁移生成确定性医疗包/护盾电池实例，保存坐标、旋转、次数、8 格快捷栏和容器搜索进度。F-S01/F-T01 已进入奖励池、背包、快捷栏和真实火术式结算：F-S01 为 4 格直线 8 火伤/4 刻度火场；F-T01 运行时实测 `2→1→移除`、槽位自动清空并生成火场。B 键背包覆盖层提供 6×10 格、旋转、名称/ID/类别查询、详情、未知容器搜索/拿取与 8 格快捷栏；1920×1080/960×540 可读。新增 18 张 32×32 正式图标，尺寸/硬 Alpha/有限色与 Unity Sprite/Point/Clamp/无 mipmap 为 18/18 PASS，并全部登记运行时资源。最终 EditMode 179/179、编译错误/警告 0、Console 空；`TrainingRange.unity` 为 `isDirty=false`，未保存场景，人物资产未修改。
- **完成后解锁：** 可在统一实例模型上批量扩展装备、卷轴、法宝、容器和掉落内容，无需再次迁移背包/存档/UI 基础合同。

### TR-03：正常战斗靶场、自选术式/法宝与后续系统规划 — COMPLETE（2026-08-08）

- **归属：** 靶场属于剧情模式与肉鸽模式共用的开发工具；法宝、背包与战场搜刮规则两模式共用，获取节奏与持久化范围按模式区分。
- **目标：** 将独立靶场收敛为“正常战斗界面 + 自选术式/法宝配置”的长期 Bug 复现和数值调整地图；移除玩家可见的巡检/样本报告，并基于现有策划合同形成法宝、格子背包、战场搜刮、物品搜索筛选及配套美术资产的可执行分期计划。
- **涉及文件/系统：** `DeveloperConsolePanel`、靶场运行时与视觉回归；现有 `InventoryGrid`、`LootContainer`、快捷栏、奖励池、`RogueliteMapRun`/存档；魔法道具合同、数值标尺、总策划案、美术规范；新增开发计划与本待办。不修改人物资产，不保存既有场景。
- **验收标准：** 进入独立场景后默认显示正常战斗 HUD；配置面板只显示术式/法宝目录、图标、成本、目标和效果，能够装载并重置战斗，关闭后通过真实棋盘完成目标预览与确定性结算；不显示全量巡检、标准案例、样本说明或 QA 话术；1920×1080/960×540 可读。至少一件真实法宝验证有限次数路径。规划明确数据合同、模式边界、交互流程、存档迁移、阶段依赖、测试矩阵、32×32 图标/UI/战利品容器状态资产清单与 QA 门槛，且不引入未获授权的玩法决定。
- **当前记录：** 已完成；验证源见 `OCC_全游戏能力靶场验证_2026-08-08.md`，后续系统实施源见 `OCC_法宝背包搜刮与物品搜索系统实施计划_v0.1.md`。
- **完成记录：** `TrainingRange.unity` 进入 Play Mode 后直接呈现正常 12×9 战斗 HUD，配置面板默认关闭，仅保留右下角入口与 F1；面板收敛为带图标的术式/法宝自选目录，浏览不会重置，只有“装载并重置战斗”才重建状态。运行时共 78 项（F-P 50、通用技能 27、法宝 F-T01 爆破筒 1），不显示巡检按钮/报告、标准案例、样本说明或逐步 QA 面板。F-T01 复用真实目标预览与棋盘结算，2 次封装实测 `2→1→0`，耗尽后第三次施放被稳定拒绝；HUD 显示法宝图标与剩余次数。法宝/背包/搜刮/搜索计划已冻结定义与实例分离、4×3 v1 背包、快捷栏边界、`map7` 迁移、BIS-01–BIS-07 阶段、测试矩阵及最多 18 张新增 32×32 位图计划。全量 EditMode 162/162，编译错误/警告 0，Console 为空；1920×1080 与 960×540 的正常战斗和配置面板均通过视觉回归；`TrainingRange.unity` 为 `isDirty=false`，本轮未保存场景，人物资产未修改。
- **完成后解锁：** 可按“库存数据与存档 → 背包 UI → 搜刮交互 → 法宝运行时 → 内容与美术”的顺序进入实现，不把四个系统一次性耦合上线。

### TR-02：独立靶场战斗场景与控制台术式图标 — COMPLETE（2026-08-08）

- **归属：** 剧情模式与肉鸽模式共用的开发验证基础设施；独立场景不进入正式流程。
- **目标：** 新增可直接打开并自动进入全游戏能力靶场的独立 Unity 战斗场景，同时在控制台能力目录与当前能力详情中显示真实术式图标。
- **涉及文件/系统：** `Assets/Scenes/TrainingRange.unity`、独立场景启动组件、`DeveloperConsolePanel`、`TrainingRangeAbilityEntry` 图标合同、正式技能图标资源回退、EditMode 测试、Funplay 场景/Play Mode/双分辨率验证与本待办；不修改或保存 `CombatPrototype.unity`，不修改人物资产。
- **验收标准：** `TrainingRange.unity` 可独立打开，层级最小且进入 Play Mode 后自动为 Active 靶场；默认场景保持 clean；50 个火术式使用独立正式图标，27 个通用技能使用可解析的专属或类别回退图标；目录行与详情卡图标在 1920×1080/960×540 清晰且不挤压文字；全量测试、编译、Console 与两个场景 clean 通过。
- **当前记录：** 已完成；验证源见 `OCC_全游戏能力靶场验证_2026-08-08.md`。
- **完成记录：** 通过 Unity Editor API 新建并保存 `Assets/Scenes/TrainingRange.unity`，采用单根节点、运行时管理器与主摄像机的最小层级；场景进入 Play Mode 后自动创建 12×9 标准靶场、进入 Active 流程并打开控制台。`TrainingRangeAbilityEntry` 新增统一 `IconPath` 合同，50 个火术式与 27 个通用技能全部解析到各自正式图标，无 ID 特判或占位回退；控制台 10 行目录显示 32×32 图标，详情显示 64×64 图标。运行时注册 77 项且确定性巡检 77/77；全量 EditMode 161/161；1920×1080 与 960×540 布局清晰无挤压，另验证通用技能第 7 页图标。最终编译错误/警告 0，Console 为空；新靶场场景保存后 `isDirty=false`，默认 `CombatPrototype.unity` 重新打开后 `isDirty=false`，人物资产未修改。
- **完成后解锁：** 可将独立靶场加入开发构建菜单，后续能力资源只需补充图标路径即可自动显示。

### TR-01：全游戏能力靶场与长期测试控制台 — COMPLETE（2026-08-08）

- **归属：** 剧情模式与肉鸽模式共用的开发验证基础设施；仅在开发入口/快捷键显式开启，不进入正式流程，不引入时间压力。
- **目标：** 建立可长期扩展到全部元素术式、武器、道具和敌方能力的 12×9 标准靶场；当前完整接入 F-P01–F-P50，并提供能力浏览、标准敌人/友军/掩体/设备/水面预设、前置条件自动准备、预览、执行、逐步结果、重置与全量确定性巡检控制台。
- **涉及文件/系统：** 新增通用靶场目录/场景工厂/会话/审计模型，`CombatPrototypeBootstrap`、`DeveloperConsolePanel`、运行时棋盘/HUD、EditMode 测试、技术方案与验证记录；使用运行时代码建场，不保存 `CombatPrototype.unity`，不修改人物资产或无关用户改动。
- **验收标准：** 靶场可从开发控制台进入并退出；标准 12×9 场景包含英雄、友军、多类敌人、轻/重掩体、设备、水面与任务核心样本；50 个火术式可分页选择、自动准备合法样例、预览、执行并显示确定性步骤；一键全量巡检为 50/50 且重复结果一致；靶场禁用自动敌方回合和胜负收束；1920×1080/960×540 控制台可读；新增及全量测试、Unity 编译、Console、场景 clean 通过。
- **当前记录：** 已完成；验证源见 `OCC_全游戏能力靶场验证_2026-08-08.md`。
- **完成记录：** 新增运行时 12×9 标准靶场、统一能力描述符/案例提供器/会话/预览/执行/审计报告合同，以及 F1/F2 长期开发控制台。当前注册 F-P01–F-P50 与 `RogueliteSkillCatalog` 27 项，共 77 项；火术式和通用技能分别通过真实结算器执行，标准案例自动准备燃烧、火场、束缚、缓慢、受伤、缺失资源、友军、地格与可破坏物等前置。Play Mode 全量巡检 77/77、预览与两份结算签名一致；F-P38 通过真实棋盘点击处理路径归零设备耐久并进入过载，`phase_step` 实际移动至推荐格。新增及全量 EditMode 161/161；1920×1080/960×540 控制台和棋盘证据齐全；编译错误/警告 0，Console clean，默认场景未保存且 `isDirty=false`。人物资产未修改。
- **完成后解锁：** 后续元素、武器、道具和敌方技能只需注册能力适配器与测试案例，无需重写靶场场景或控制台。

### F-P50：火元素个人术式 F-P01–F-P50 全量程序、美术与 Unity 实装 — COMPLETE（2026-08-08）

- **归属：** 剧情模式与肉鸽模式共用的确定性战棋、肉鸽内容池、存档、UI 与非人物火焰表现；人物相关资产继续暂停。
- **目标：** 以冻结策划源文件为唯一规则来源，完成 F-P01–F-P50 的单一数据目录、通用目标预览与确定性效果执行、燃烧状态/地格生命周期、内容池/存档/UI、50 张图标和可复用火焰 VFX 映射，使 50/50 术式在运行时可获得、装备、预览、提交、结算、表现和恢复。
- **涉及文件/系统：** 火元素个人术式策划源文件与实现矩阵；`SkillDefinition`、目标形状/预览、效果/前置条件、地格与结果事件；`RogueliteSkillCatalog`、奖励池、存档与 HUD；`FormalSkillIcons32/Fire`、`FormalVfx32`、`FormalArtRegistry`、版本化配置、Importer、manifest、正式资产清单/QA；EditMode 测试、Funplay Play Mode/双分辨率回归与本待办。禁止修改人物造型资产、无关用户改动、场景 YAML、`Library/`、`Logs/` 或生成缓存。
- **验收标准：** F-P01–F-P50 数据/运行时可达/合法与非法预览/确定性执行/UI/内容池/存档/图标/VFX 映射均为 50/50；无散落 ID 特判、随机或隐藏规则；50 张独立图标和全部复用 VFX 通过像素及 Importer QA；全量现有与新增 EditMode 通过；五组真实构筑、1920×1080 与 960×540 视觉回归、GIF/strip/接触表索引完整；Unity 编译错误/警告 0、Console 项目错误 0、默认场景 clean，且不保存场景。
- **当前记录：** 已完成；验证源见 `OCC_火元素个人术式全量实现验证_2026-08-08.md`。
- **完成记录：** 已建立版本 `fire-personal-spells-v0.1` 的 50 项中央目录和人类可读矩阵，五组各 10 项；通用预览/结算器组合实现全部形状、燃烧状态与地格、来源消费、友军风险、掩体截断、轻/重掩体和设备耐久、破甲、护盾、解控、回魔、移动/击退、移动力恢复、设备过载和行动延时，运行时目录外无 F-P ID 特判。普通/少见与稀有奖励池、公开三选一、`map6` 存档、两个装备槽、HUD、结算和行动档案已接入，Play Mode 50/50 可执行且五组各 10/10。50 张独立图标 QA 50/50；原五套火系 VFX 复用通过，并以 15 个组合模块覆盖全部术式；新增 6 帧 `fire_smoke` 补齐水面转烟尘，正式 VFX 库为 30 套/180 帧。最终 EditMode 153/153、失败 0；编译错误/警告 0，Play Mode Console 项目错误 0；完成 5 组主题各 1920×1080/960×540 共 10 张视觉回归及接触表。已退出 Play Mode，默认场景 `isDirty=false`，未保存场景；人物资产继续暂停。
- **完成后解锁：** 火元素个人术式内容线可作为其余七元素的数据合同、执行器、内容池、存档、UI 与可复用表现生产模板；后续可在不增加人物资产的前提下扩展其他元素。

### M-A10：低分辨率文字清晰度与入口/战斗指令栏重置 — COMPLETE（2026-08-07）

- **归属：** 剧情模式与肉鸽模式共用的运行时 UI 可读性；人物相关资产继续暂停。
- **目标：** 保留既定 Fusion 像素中文字体，修复 960×540 下半像素字号造成的模糊，并重排入口卡和战斗指令栏，使操作名称、成本、范围和快捷栏信息简洁且稳定可读。
- **涉及文件/系统：** `FormalUiKit` 像素对齐与文本换行合同、`FormalRogueliteUi` 入口卡、`FormalCombatHud` 指令栏、`CombatPrototypeBootstrap` IMGUI 字体绑定、EditMode 回归、Funplay 1920×1080/960×540 截图与本待办；不修改玩法、数值、存档、场景 YAML 或人物资产。
- **验收标准：** 紧凑分辨率字号投影为整数屏幕像素；所有活动 uGUI 与战斗 IMGUI 使用 Fusion 字体；入口说明不产生意外换行，四个入口按钮标题/详情互不遮挡；六个战斗动作卡严格为标题一行、参数一行，快捷栏和结束行动区不越界；双分辨率目视、文本边界审计、全量 EditMode、编译、Console 与场景 clean 通过。
- **当前记录：** 已定位紧凑字号 `×1.25` 后产生 19/23 等奇数，随 0.5 Canvas 缩放落在半像素；通用按钮保留自动换行，战斗动作卡因图标内缩后把两行挤成三行；战场 IMGUI 尚未显式绑定当前中文字体。
- **完成记录：** `FormalUiKit` 已将紧凑小字号放大量化为偶数并为全部正式 Canvas 开启像素对齐，大标题只做偶数量化，修复 960×540 下半像素模糊和整行被裁为 0 字形的问题；新增禁止自动换行合同。入口说明压为两条短句，四个入口按钮统一为单行标题与单行摘要，按钮行高/坐标按 0.5 倍投影对齐。战斗六个动作卡统一为标题一行、成本/范围一行，快捷栏与结束行动文案压缩，战场 IMGUI 显式绑定 Fusion 字体。真实 960×540 审计：入口 13/13、战斗 35/35 活动文本均使用 `FusionPixel12ProportionalZhHans`、偶数字号、显式行数与实际行数一致且失败 0，Canvas scale `0.5`、`pixelPerfect=true`；1920×1080/960×540 共保存 4 张最终截图。全量 EditMode 143/143、失败/跳过 0；编译错误/警告 0，清理临时 Game View QA 尺寸后 Console 为空；已退出 Play Mode，默认场景 `isDirty=false`，未保存场景，人物资产未生产或修改。
- **完成后解锁：** 可按同一单行/显式换行合同审计设置、档案、结算等次级页面，不再逐处凭截图修补。

### M-A9：战斗棋盘后置 UI 与操作回执层级修正 — COMPLETE（2026-08-07）

- **归属：** 剧情模式与肉鸽模式共用的战斗 UI 表现；人物资产继续暂停。
- **目标：** 修复战斗抬头被错误放在棋盘中心、看似被地图遮挡的问题，并把每次操作产生的回执移出战场格区域。
- **涉及文件/系统：** `FormalUiKit` 锚点解析、`OccPixelUiV02.json` 战斗提示布局、`FormalUiInteractionLayer`、EditMode 锚点回归、Funplay 1920×1080/960×540 战斗截图与本待办；不修改玩法、数值、存档、场景 YAML 或人物资产。
- **验收标准：** `top-center` 严格解析为 `(0.5,1)`；战斗抬头位于屏幕顶部，不在棋盘后；操作回执位于顶部抬头中央信息槽，与 12×9 棋盘、右侧控制台、底部指令及抬头左右文字相交数均为 0；测试、编译、Console、场景 clean 通过。
- **当前记录：** 已确认 `FormalUiKit` 未实现 `top-center`，配置中的 `combat.header`/`global.header` 因此落入默认 `center`，这正是棋盘后方横向信息的来源；`combat.toast` 的 top-left 位置同时覆盖战场左上格区。
- **完成记录：** `FormalUiKit.ResolveAnchor` 已显式支持 top/bottom left/center/right 与 center，未知锚点直接报错；`combat.header` 现严格位于屏幕顶部。战斗操作回执改为顶部抬头中央 `620×56` 信息槽，左右保留“战术行动”和“无时间压力/确定性结算”，运行时屏幕空间审计为 `boardOverlap=False`、`textOverlaps=空`，不再覆盖任何战场格或抬头文字。新增 7 组锚点参数化回归，最终 EditMode 138/138、失败/跳过 0；编译错误/警告 0，Console 错误 0。已保存 1920×1080/960×540 战斗截图，退出 Play Mode，默认场景 `isDirty=false`，未保存场景。
- **完成后解锁：** 后续可沿同一锚点合同增加 top/bottom center 安全区，不再依赖默认分支猜测。

### M-A8：地图信息层级修正与基础战斗动作反馈 — COMPLETE（2026-08-07）

- **归属：** 剧情模式与肉鸽模式共用的 UI/战斗表现；人物相关资产继续暂停。
- **目标：** 移除地图背景中的伪节点信息，修复操作提示与地图状态栏/战斗抬头的遮挡，并让现有占位单位在移动、攻击/施术、受击、护盾承伤、恢复和回合切换时具备清晰的基础动作与反馈。
- **涉及文件/系统：** `FormalUIBackdrops/map.png`、像素 UI 布局配置、`FormalUiInteractionLayer`、`FormalUiKit`、`CombatVisualFeedback`、`CombatPrototypeBootstrap`、EditMode 测试、Funplay 双分辨率运行时截图与本待办；不修改玩法、存档、数值、场景 YAML 或人物造型资产。
- **验收标准：** 地图背景不再出现会与真实节点混淆的路线/节点；地图和战斗操作提示使用页面专属安全区且不覆盖状态栏、核心按钮或战术控制台；移动有踏步/位移弧线，攻击/施术有前摇和后坐，受击/护盾有震动与闪烁，恢复和回合切换有脉冲；动画强度 0 时立即回到正确终态；1920×1080/960×540、全量 EditMode、编译、Console 与场景 clean 通过。
- **当前记录：** 已定位确定性遮挡：`modal.toast` 固定于顶部中央 `y=-92`，与地图 `map.status` 的 `y=-82` 直接重叠；现有地图背景另绘一套路线和节点，造成“面板后似有信息”的错觉。现有单位由 IMGUI 绘制，已确认可在不生产人物资产的前提下，通过战斗事件驱动逻辑像素位移、跳步、后坐、受击抖动和色闪。
- **完成记录：** 地图背景已重绘为低对比度工业制图网格，移除全部伪路线/伪节点；操作回执增加 `map.toast`/`combat.toast` 页面专属布局，地图提示使用底部 640×52 安全轨，运行时边界为 `(296,-526)..(936,-474)` 且与所有按钮相交数为 0，跨页面时活动提示会自动销毁。战斗表现层新增移动弧线踏步、攻击前摇/突进/后坐、施术上浮、受击/护盾击中震动与红/青闪色、恢复回弹、回合开始跳动与格脉冲；全部复用现有占位单位和已 QA 的 32×32 VFX，不生产人物资产。保存地图 1920×1080、战斗受击 1920×1080/960×540 三张运行时证据；跨页回归返回 `toast-cleared`。首次回归捕获并修复回合脉冲早于反馈 Canvas 初始化的空引用；修复后全量 EditMode 131/131、失败/跳过 0，编译错误/警告 0，Console 错误 0。已退出 Play Mode，默认场景 `isDirty=false`，未保存场景。
- **完成后解锁：** 人物设定冻结后，可在同一动作事件接口接入正式逐帧人物动画；其他页面可复用页面感知提示安全区。

### M-A7：周边界面背景、启动演出与交互动效实装 — COMPLETE（2026-08-07）

- **归属：** 剧情模式与肉鸽模式共用的非人物界面表现；人物相关资产继续暂停。
- **目标：** 为现有强像素 UI 补齐可直接运行的启动/标题演出、入口/地图/简报等页面背景、像素化页面转场，以及悬停、按下、点击、成功、拒绝、资源变化和模态提示等交互反应，使静态 UI 具备完整的像素游戏反馈层。
- **涉及文件/系统：** `Resources/Art/FormalUIBackdrops`、`Resources/Art/FormalUIFeedback`、`OccPixelUiV02.json` 与加载/注册、`CombatPrototypeBootstrap`、`FormalRogueliteUi`、`FormalUiKit`、`UiButtonFeedback`、`FormalUiInteractionLayer`、新增启动/背景/转场表现组件、Importer、EditMode 测试、Funplay 双分辨率截图与本待办；不修改玩法、存档语义、地图规则、字体或人物资产。
- **验收标准：** 至少完成入口、地图、简报三类 480×270 逻辑像素背景和启动主视觉，点击/成功/拒绝三类固定格反馈动画与像素转场素材；全部 PNG 为硬 alpha/有限调色板并以 Point、Clamp、无 mipmap 导入；启动界面可跳过且不阻塞流程，动画强度 0 时所有交互立即到达正确终态；入口、地图、简报、确认/提示与战斗按钮均有可见反馈；1920×1080/960×540 可读，全量测试、编译、Console 与场景 clean 通过。
- **当前记录：** 已确认现有系统仅具备基础页面淡入、按钮 2px 按压位移、模态缩放和提示条位移；缺少启动层、页面专属背景、像素遮罩转场与点击/成功/拒绝的独立动画反馈。本轮将复用现有 DOTween、视觉事件流和强像素调色板，以运行时代码挂载避免保存默认场景。
- **完成记录：** 已生成启动、入口、地图、简报 4 张 `480×270` 逻辑像素背景、全屏扫描层和阶梯擦除转场，并生成点击、成功、拒绝各 6 帧 `32×32` 反馈动画，共 24 个正式 PNG；硬 alpha、有限色板及 Unity Point/Clamp/无 mipmap 导入检查通过。新增 `OccPeripheralUiV01.json` 及严格加载/校验，启动界面支持鼠标、键盘与手柄接入并兼容 Input System；入口/地图/简报使用专属背景和环境扫描，页面切换使用像素擦除，按钮增加横向悬停、按压与点击帧反馈，短时提示增加成功/拒绝动画；动画强度 0 时跳过新增运动并直接进入终态。Funplay 已保存启动、入口、地图、简报与拒绝反馈的 8 张 1920×1080/960×540 回归截图及接触表；最终 EditMode 131/131、失败/跳过 0，编译错误/警告 0，Console 项目错误 0。已退出 Play Mode，默认场景 `isDirty=false`，未保存场景；人物资产未生成、未修改、未绑定。
- **完成后解锁：** 后续可在同一背景/反馈配置中扩展剧情地区主题、节庆皮肤、加载提示、商店/工坊专属环境和音频反馈，不需要重写页面结构。

### M-A6：中文像素字体选型、安装与双分辨率调试 — COMPLETE（2026-08-07）

- **归属：** 剧情模式与肉鸽模式共用的运行时 UI 字体表现；人物相关资产继续暂停。
- **目标：** 选择许可允许随商业游戏分发、简体中文覆盖可靠且适合强像素 UI 的字体，替换现有黑体运行时绑定，并在 1920×1080/960×540 下完成字号、字距、字形覆盖和主要页面可读性调试。
- **涉及文件/系统：** `Resources/Fonts`、字体许可证与来源记录、`FormalUiKit`、`CombatPrototypeBootstrap`、`CombatVisualFeedback`、字体 Importer、EditMode 字形审计、Funplay 运行时截图与本待办；不修改玩法、布局数据、存档或人物资产。
- **验收标准：** 字体来源、版本、SHA-256 与 OFL 文件可追溯；Unity 全部运行时中文入口使用同一像素字体，Importer 使用适合像素字形的 Hint/Raster 配置；当前 UI 代表性简体中文、数字、拉丁字母和标点无缺字；1920×1080/960×540 的入口、地图、战斗、结算至少完成目视回归；全量测试、编译、Console 和场景 clean 通过。
- **当前记录：** 已排除官方仍提示大量缺字的方舟像素字体与需要额外商业授权的三二像素体；选用 TakWolf `Fusion Pixel Font 12px Proportional zh_hans` 2026.07.20，字体为 SIL OFL 1.1，发布包 SHA-256 为 `A6B32FE3E663BC3575DC8A71E1F5F1C17B5951558B0FBA9E5E75A33AFC2AB2DA`。
- **完成记录：** 已将 `Fusion Pixel Font 12px Proportional zh_hans` 导入 `Resources/Fonts/FusionPixel12ProportionalZhHans.ttf`，随项目保存 OFL 1.1 许可证和来源/双 SHA-256 记录。新增稳定 Font Importer，读回为 12px、`HintedRaster`、padding 1、嵌入字体数据；`FormalUiKit`、Bootstrap 旧场景 UI 与战斗反馈已统一绑定新字体，运行时代码不再引用 `SimHei`，旧文件仅作为未绑定历史资源保留。新增字形/Importer 审计，代表性 UI 简体中文、数字、拉丁字母和标点无缺字；全量 EditMode 130/130、失败/跳过 0。入口、地图、战斗、奖励在 1920×1080/960×540 保存 8 张截图并生成接触表/哈希报告，目视确认像素笔画清晰、无裁字和关键溢出。最终编译错误 0、Console 错误 0；8 条同类 `RenderTexture.active` 警告均由截图工具产生。已退出 Play Mode，场景 `isDirty=false`，未保存场景。
- **完成后解锁：** 后续 UI 文案可在统一像素字体基线上扩展；若未来引入繁体或其他语言，再独立增加对应语言字形版本与 fallback 配置。

### M-A5：强像素 UI 全页面重构、素材实装与数据配置 — COMPLETE（2026-08-07）

- **归属：** 剧情模式与肉鸽模式共用的运行时 UI 表现与配置基础；人物相关资产继续暂停。
- **目标：** 以 `OCC_combat_ui_layout_preview_v02_strong_pixel.png` 为视觉基准，为当前全部可达页面生产具有明显逻辑像素、阶梯轮廓和块状分区的 UI 素材；重构入口、地图、节点详情、简报、战斗 HUD、确认/提示弹窗、奖励结算、失败、档案、设置及存档/继续流程，并把皮肤、布局、交互状态和资源路径移出散落硬编码，建立可审计的运行时数据配置。
- **涉及文件/系统：** `FormalUISkin16` 及新增强像素 UI 切片、像素生成与 QA 工具、`OccPixelUiConfig`、`FormalUiKit`、`FormalCombatHud`、`FormalRogueliteUi`、`FormalUiInteractionLayer`、`RogueliteSettlementPresentation`、正式资源注册、EditMode 测试、Funplay 双分辨率流程回归与本待办；不修改玩法数值、存档语义、节点规则或人物美术。
- **验收标准：** 所需 UI 素材均为独立 PNG 并通过尺寸、硬 alpha、有限调色板、Point/Clamp/无 mipmap 与九宫格 QA；皮肤语义、按钮状态、资源路径和主要页面矩形由版本化配置驱动且严格校验；战斗保持 1920×1080 下左战场 1440px/右 HUD 480px，并在 960×540 可读；全部可达流程完成鼠标/键盘焦点回归，EditMode、编译、Console 与场景 clean 通过；人物资产未生成、未修改、未绑定。
- **当前记录：** 已冻结强像素 v02 方向，完成现有页面与硬编码布局审计。确认 M-A4 的 15 张基础切片可保留，但还需补齐模块面板、指令组、分段资源条、状态徽标、锁定槽、时间线节点和独立结束行动等强像素语义；本轮先建立统一配置合同，再据此生产与实装。
- **完成记录：** 已按 v02 重绘 M-A4 基础切片并新增模块面板、四类指令组、独立结束行动、三类分段资源条、成本徽标、锁定槽和时间线节点，共 30 张独立 `16×16` 强像素切片，硬 alpha/有限调色板/4px 九宫格 QA 为 30/30 PASS。新增 `OccPixelUiV02.json` 与严格加载器，统一注册 30 个皮肤路径、10 个交互状态、20 个主页面布局及 10 项调色板；`FormalUiKit`、按钮反馈和全部主要页面改由配置读取主矩形/状态/皮肤。战斗 HUD 已拆为右侧四模块和底部武器、术式、交互、物品四组，结束行动独立；入口、存档继续、地图/详情、简报、战斗、确认、失败、奖励、档案和设置均完成运行时回归。保存 13 张 1920×1080/960×540 截图并生成运行时接触表/哈希报告，实际继续存档恢复到 `rail_patrol`。全量 EditMode 128/128、失败/跳过 0；编译错误 0、Console 错误 0，仅截图工具产生 13 条相同 `RenderTexture.active` 警告；已退出 Play Mode，默认场景 `isDirty=false`，未保存场景。人物相关资产未生成、未修改、未绑定。
- **完成后解锁：** 后续新页面只需登记布局/状态/资源配置并复用强像素组件；人物设定明确后可在不改 UI 架构的前提下单独接入人物头像或演出层。

### M-A2：正式调色板与同屏接触表 — COMPLETE（2026-08-07）

- **归属：** 剧情模式与肉鸽模式共用的正式战斗美术基准；仅制作审查资产与 QA 证据，不修改或替换 Unity 正式资源。
- **目标：** 完成 `ART-BASE-01` 正式主色表及正常/灰阶/色觉风险预览，并以现有已 QA 的单位、地块和图标合成 `ART-BASE-02` 1920×1080 同屏接触表；明确当前状态/VFX 缺口，供批量生产前审查。
- **涉及文件/系统：** `Worldbuilding/05_美术与音频/OCC_正式美术资产需求表_v0.1.md`、美术规范、像素 QA 流程、Unity 已有 `FormalUnits64`/`FormalRelay32`/`FormalIcons32` 只读原料、拟新增的正式美术基准目录与生成脚本、本待办。
- **验收标准：** 色表含煤灰/铁黑、冷青、氧化红、安全黄橙、灰绿、暗紫红的稳定 RGB/HEX 和类别预算；接触表严格 1920×1080、左战场 75%/右 HUD 25%、正交 12×9、整数最近邻缩放；正常/灰阶/色觉风险三版均能依靠轮廓与明度读取敌我、掩体、目标、交互和 HUD；所有来源、尺寸、哈希、缺口与 QA 结果可追溯；不把脚本几何或接触表当作 Unity 正式资产。
- **验证记录：** 已在 `Worldbuilding/05_美术与音频/正式美术基准/ART-BASE/` 生成 32 色主色表 PNG/GPL/JSON、主色表灰阶/红绿色觉风险版、1920×1080 接触表三版、QA 汇总图和带来源哈希的 JSON 报告；自动检查为 `BASELINE_QA_PASS`，产品状态为 `BASELINE_APPROVED`。接触表严格使用 1440/480 分区、12×9 正交地图、32px 源格 ×3 最近邻；目视检查确认单位轮廓、掩体体量、目标物、资源条和 HUD 分区在三版中可读。状态/VFX 仅以明确标注的待制槽位呈现；未调用图像模型重绘既有资产，未修改 Unity、场景、资源或代码。生成器为 `Tools/Art/generate_occ_art_baseline.py`，可重复产生一致输出。
- **产品反馈与修正：** 2026-08-07 反馈人物偏大、可能遮挡，采用平均缩小约 30% 的方向。v0.2 接触表把单位有效轮廓设为 70% 审查预览，同时保持 `64×64` 画布、`X=32`、`Y=58` 不变；正式标准单位主体改为约 `32–38 px` 高，精英/首领约 `38–44 px`。禁止在 Unity 运行时直接使用 `0.7` 非整数缩放；现有正式单位后续须按原生逻辑像素重新规范化。v0.1 文件保留作前后对照，Unity 仍未改动。
- **完成记录：** 2026-08-07 产品确认“可以了”，正式批准 `ART-BASE-01` 与 `ART-BASE-02 v0.2`。冻结 32 色主色表、人物 70% 有效轮廓、1920×1080 下 1440/480 分区、12×9 正交地图、整数最近邻和极简工业 HUD；M-A2 完成。
- **完成后解锁：** 已解锁 M-A3；后续资产不再拆成孤立样品，统一纳入全量生产、QA、Funplay 实装与终局审查。

### M-A3：全量正式美术生产与 Funplay 实装 — COMPLETE FOR APPROVED NON-CHARACTER SCOPE / CHARACTER BLOCKED_CONTENT（2026-08-07）

- **归属：** 当前已确定的剧情/肉鸽共用战斗表现与肉鸽首区完整正式界面；未定剧情角色/地区、其余未冻结元素内容和音频不在本任务内。
- **目标：** 不停在样品或 P0，持续完成已确定系统的全部正式像素资产、动画、VFX、UI、Unity/Funplay 实装、双分辨率全流程视觉回归和终局审查，使当前完整可达流程呈现为成熟像素游戏。
- **涉及文件/系统：** `Worldbuilding/03_开发管理/OCC_全量正式美术生产与Funplay实装计划_v0.1.md` 所列全部美术源文件、QA/manifest、Unity 正式资源、Presentation/UI、战斗反馈、20 节点流程、测试、默认场景与本待办。
- **验收标准：** 当前可达 ID 正式资产覆盖 100%；12 单位唯一且具备实际行为动作；环境/三段物件/八环境/战术叠加/24 基础 VFX/5 火系模板完整；6+33+8+50 及当前物品图标完整；全部正式页面统一；无 prototype/fallback/复用冒充；全部资产 QA、测试、两首领流程、1920×1080/960×540 回归、编译/Console/场景状态通过。
- **执行入口：** 将 `Worldbuilding/03_开发管理/OCC_全量正式美术新对话提示词_v0.1.md` 的代码块完整复制到新 Codex 对话。执行过程中 M-A3 是唯一主任务，M-W7 继续暂停。
- **Gate 0 基线记录（2026-08-07）：** 已完整读取任务提示词、计划、需求表、美术/单位/三层/像素 QA 规范、ART-BASE 审查记录与火系 50 项术式池，并读取 `funplay-unity-dev`、`pixel-asset-pipeline`、`imagegen` 技能。已记录脏工作树基线，既有改动含 Runtime/Tests、Packages、ProjectSettings、Recovery、多批 Worldbuilding 文档与像素原料，全部按用户既有改动保留。Funplay 读回活动场景 `Assets/Scenes/CombatPrototype.unity`、`isDirty=false`、编译错误/警告 0；Console 仅有 Unity 服务 Token Exchange 网络错误，尚未发现项目脚本错误。当前正在审计运行时可达 ID 与正式资源映射，场景未保存、未进入 Play Mode。
- **产品范围变更（2026-08-07）：** 用户明确要求“人物相关资产暂停生产，目前设定暂不明晰”。即日起 12 个单位身份的静帧、双向动作、角色三层视觉和人物专属表现全部标记为 `BLOCKED_CONTENT`；不再生成、规范化、绑定或以复用/猜测补齐。此前生成的狙击手候选仅保留在 Worldbuilding 原始参考目录，不进入正式资源、manifest 完成率或 Unity。M-A3 继续处理环境、物件、UI、非人物图标、VFX、清单、导入与可验证接入；原验收中的“12 单位唯一且具备实际行为动作”改为解除本阻塞后的后续门禁，不得伪报完成。
- **Gate 1–3 验证记录（2026-08-07）：** 已完成 `ART-BASE-03`、188 条机器 manifest、172 条严格运行时 registry（含 29 VFX 根路径）及正式纹理 ImportPostprocessor。已生成并 QA 194 张非人物 32×32 正式静态资产：中继站地块/连接变体/三段物件、八环境、战术叠加、33 基础语义、9 节点类型、14 反馈、11 当前物品、27 当前技能、50 火系术式和 6 UI 模块；机器 QA 194/194 通过，火系首版因主轮廓重复被目检退回后已逐项重构。另生成 24 基础 VFX + 5 火系模板，共 174 张独立帧及 strip/GIF/事件帧报告，QA 174/174 通过。Unity 读回 `registry=172 / active non-character=160 / VFX=29×6 / formal importers=394`，全部 Sprite/Point/Clamp/无 mipmap/PPU32；编译与 Console 项目错误 0。
- **Gate 4–5 验证记录（2026-08-07）：** 中继站地板/轨道/警戒区、轻重掩体与中继器三段状态、箱体、移动/攻击/高危/选中叠加、四项已实现状态图标、14 反馈 VFX、火系组合 VFX、节点类型、运行时技能/物品/装备图标已接入正式流程；未知节点不泄露类型。新增资产审计 EditMode 测试并执行全套；首轮 122/125（3 项为 NUnit 对 `IReadOnlyList` 的旧 `Has.Count` 兼容问题），修正后 125/125，通过恢复简报回归后为 126/126、失败/跳过 0。回归同时修复“存档停在当前未完成战斗节点后无法恢复简报”的流程阻塞，不改变存档格式、节点规则或奖励。
- **Gate 6 终局记录（2026-08-07）：** 已用 Funplay 实走入口、新局确认、继续/恢复简报、20 节点地图框架及商店/工坊/休整/事件/宝藏详情、简报、战斗、胜利三选一、失败、档案和设置；两个固定首领种子均完成运行时回归。奖励页已复验正式武器/术式图标。保存 23 张 1920×1080/960×540 关键截图，并生成 23 张灰阶、23 张红绿色觉风险变体与最终接触表；VFX 另有 29 组 GIF/strip 证据。最终 EditMode 126/126，编译错误/警告 0，Console 错误 0；已退出 Play Mode，默认场景 `isDirty=false`，未保存场景。非人物已批准范围完成；12 人物身份及其动作继续保持 `BLOCKED_CONTENT`，不计入完成率，也不以现有旧贴图作正式完成声明。
- **完成后解锁：** 其余七元素、正式卷轴/法宝/装备目录、剧情角色三层视觉、剧情地区和宣传美术沿同一管线扩展。

### M-A4：像素 UI 皮肤重置与全流程替换 — COMPLETE（2026-08-07）

- **归属：** 剧情模式与肉鸽模式共用的运行时 UI 表现；人物资产继续暂停。
- **目标：** 移除现有以纯色矩形、细线和 `Outline` 为主的简约线框观感，改用统一的可切片像素 UI 资产覆盖入口、地图、节点详情、简报、战斗 HUD、结算、档案、设置和确认弹窗；中文正文仍使用清晰字体。
- **涉及文件/系统：** `FormalUiKit`、`FormalRogueliteUi`、`FormalCombatHud`、`RogueliteSettlementPresentation`、交互模态层、`FormalUISkin16`、Importer、QA/测试/截图与本待办；不修改玩法数值、存档语义、节点规则或人物资产。
- **验收标准：** 面板、模态框、标题栏、按钮四态、标签页两态、槽位、资源条、分隔带、焦点框及危险/奖励框均来自独立正式像素资产；统一使用 Point/Clamp/无 mipmap、4px 九宫格边界和整数像素边缘；所有可达页面无纯色无语义主框或 `Outline` 依赖，1920×1080/960×540 可读，测试、编译、Console、场景状态通过。
- **完成记录：** 已生产 15 张 `16×16` 正式像素 UI 切片并通过 15/15 QA；Importer 读回 4px 九宫格、Point/Clamp/无 mipmap。`FormalUiKit` 统一为所有运行时面板、标题栏、按钮、槽位、资源条和分隔带装配像素皮肤，`UiButtonFeedback` 实时切换按钮四态并用像素焦点框替代 `Outline`；结算与确认层的手工 `Outline` 已清除。入口、地图、战斗在 1920×1080/960×540 完成截图与灰阶/色觉风险回归；最终 EditMode 127/127，失败/跳过 0，编译错误/警告 0，Console 项目错误 0，退出 Play Mode，默认场景 `isDirty=false`。人物资产未触碰。
- **完成后解锁：** 后续 UI 只需扩展皮肤语义和页面组合，不再为每页手工绘制线框。

### M-W7：三层内容池与战后三选一配置 — PAUSED（2026-08-07；让位于 M-A3）

- **归属：** 肉鸽模式的法术/卷轴/法宝内容池与每场战斗后三选一奖励配置；不是抽手牌、卡组、卡牌稀有度或卡牌升级系统。剧情模式只复用已批准的单人战棋效果，不继承肉鸽三选一运营。固定主角两项稀有天赋不进入肉鸽，也不在本任务中制作。
- **目标：** 沿用已确定的肉鸽法术与消耗品内容规模，以 24 项已完成样本和正式首档数值为批次 0，按“一个元素一个元素”依次安排个人术式、卷轴、法宝的功能配比、构筑联动、现有稀有度、战后三选一来源、条目 ID 与审查门禁；不重新设计战斗内手牌、卡组容量、抽取、升级或移除系统，不迁移旧 112 条目录，不修改 Unity、场景、资源或代码。
- **涉及文件/系统：** 肉鸽模式玩法定义、游戏策划案、魔法技能与道具内容合同、八元素术式语法、状态/环境合同、战棋数值标尺、三层校验样本、拟新增的三层内容池配置、本待办。
- **验收标准：** 当前元素进入内容池的条目均有稳定 ID、层级、主元素、功能角色、数值档位、既有稀有度、构筑联动标签、状态/环境词汇、法律、战后三选一来源和审查状态；个人术式、卷轴与法宝职责不重叠，并能与其他已批准条目形成公开的构筑联动；不引入手牌抽取、卡组容量、卡牌稀有度、卡牌升级或移除规则；无主角专属天赋、旧七系、未批准双元素、隐藏概率战斗效果或可普通补充次数的法宝；未完成的元素保持未设计/待审查而不伪装为正式可用。
- **决定记录：** 当前元素为火。已采用火系“点燃 → 压迫走位 → 引爆收割”构筑轴：使用燃烧、燃烧地格和公开的引爆关键词；形成直伤、燃烧地格、引爆三条可并行路线。引爆不是新状态，必须公开合法目标、固定额外伤害、清除的燃烧状态/地格与后续地格结果；禁止随机蔓延、无限叠层、万能治疗和无条件拆除所有掩体。记录于 `OCC_三层内容池配置_v0.1.md`。
- **决定记录（续）：** 已采用火系第一批新增条目：普通 `F-P03 烙印`（1 AP/3 魔力，3 格 4 火伤害+燃烧 1 回合）；稀有 `F-P04 引爆`（2 AP/5 魔力，3 格、仅处理燃烧单位或站在燃烧地格的单位，20 火伤害并按公开优先级清除燃烧来源）；稀有现代法宝 `F-T01 爆破筒`（2 AP/2 次，4 格十字 16 火伤害、摧毁轻掩体、空地创建临时燃烧地格）。三项的三选一来源、资格、故障、法律与反制已记录于内容池配置。
- **决定记录（续）：** 已采用全元素共用的战后三选一：个人术式同 ID 单局只获一次，卷轴/法宝可重复但不合并；普通战斗为 2 个普通/少见个人术式 + 1 个少见卷轴，精英/宝藏/稀有战斗为 1 个稀有个人术式 + 1 个卷轴 + 1 个法宝。候选按固定种子生成、展示后不重掷，候选 ID/稀有度/效果/次数/资格/构筑标签完全公开；无合法候选时公开空位或同层级替代。该规则适用于所有元素，不是抽手牌或卡组系统。
- **决定记录（续）：** 火系目标调整为约 50 个**个人术式**；卷轴与法宝不计入该 50 项，作为额外辅助内容层后续单独设计。现有 `F-P01` 至 `F-P04` 计入火系个人术式，尚需约 46 项；仍按逐元素、分组设计方式推进，不一次性堆叠同质直伤。
- **决定记录（续）：** 已采用火系个人术式 5×10 结构：A 精确直伤与点燃、B 火场与区域压迫、C 引爆与收割、D 熔甲与攻坚、E 热能战术。A 组已完成 10 项（`F-P01`、`F-P03`、`F-P05`–`F-P12`），其余四组待逐组决定；完整清单位于 `OCC_火元素个人术式池_v0.1.md`。
- **决定记录（续）：** 已按“泛用性优先、不过度押注单一流派”补完火系 50 个个人术式：B–E 四组的 40 项已完成，火系现为 `F-P01`–`F-P50` 共 50 项。完整池包含低成本直伤、点燃、火场、引爆、破甲/攻坚、脱困、护盾、机动和资源回收；所有联动均读取公开状态/地格/资源，无隐藏层数、随机蔓延、双元素或越界能力。见 `OCC_火元素个人术式池_v0.1.md`。
- **完成后解锁：** 按元素逐批填写完整内容合同与三选一奖励表、敌人/任务物/奖励配置和固定种子实战验证；不自动修改 Unity。

### M-A1：正式美术资产需求表 — COMPLETE（2026-08-07）

- **归属：** 剧情模式与肉鸽模式共用的战斗美术生产基线；肉鸽节点运营界面单列，剧情角色与场景受内容门禁约束。本项只修改 `Worldbuilding/`，不生成图片、不修改 Unity、不替换正式资产。
- **目标：** 在批量生成正式资产前，盘点当前已导入图标、地块、单位和既有 QA 规范，建立按 P0 战斗垂直切片、P1 肉鸽首区/火系内容、P2 剧情与后续地区分级的正式美术需求表；每项写明模式、规格、数量口径、状态、依赖和验收重点。
- **涉及文件/系统：** `OCC_美术规范_v0.1.md`、`OCC_单位像素美术规范_v0.1.md`、`OCC_角色三层视觉规范_v0.1.md`、`OCC_像素资产_QA流程_v0.1.md`、现有 Unity 正式美术目录、11 种敌人原型、肉鸽首区 UI、火系 50 项个人术式、本待办。
- **验收标准：** 严格保留 32×32 地块/图标、64×64 单位、1920×1080 下 75%/25% HUD 和像素 QA；区分已有正式、需新制、需返工、内容阻塞、仅概念五类状态；不提前生产未批准双元素、其余七元素目录、未定剧情角色/地区或宣传资产；明确每项提交物、Unity 导入复核和批次门禁。
- **验证记录：** 新增 `Worldbuilding/05_美术与音频/OCC_正式美术资产需求表_v0.1.md` 并更新同目录 README。盘点确认 Unity 当前有 6 张正式指令图标、12 张中继站地块/对象 PNG、6 张单位 PNG 和 1 个主角待机流程样本；11 种敌人中狙击手、破甲兵、结界卫士、束缚术士及两名监工仍依赖复用语义。需求表将 P0/P1 当前边界锁为中继站垂直切片、12 个战棋单位（主角模板 + 11 敌人）、共用 HUD/VFX、肉鸽首区 UI 与火系 50 项术式图标；剧情主角/NPC/地区和其余元素保持 `BLOCKED_CONTENT`。未生成图片，未修改 Unity、场景、资源或代码。
- **完成后解锁：** `ART-BASE-01` 正式调色板与 `ART-BASE-02` 1920×1080 同屏接触表；二者通过后再启动 P0 静态战场资产生成。M-W7 仍是“当前进行”的唯一主任务，本项作为用户插入的已完成生产规划，不改变其范围或决定。

### M-W6：正式战棋数值标尺 — COMPLETE（2026-08-06）

- **归属：** 剧情模式与肉鸽模式共用的单人战棋数值合同；剧情模式不得因此引入时间压力、敌情倒计时或拖延关门机制。
- **目标：** 以既有固定 3 AP、速度条、确定性伤害/护盾/护甲、肉鸽跨节点资源继承及首区首领基线为约束，分组决定生命、伤害、防御、魔力、行动条、状态强度、冷却、法宝次数和战后恢复的正式数值尺度；将 M-W5 的原型值转换为可生产的标准档位，而非直接照抄旧七系或 Unity 原型数值。
- **涉及文件/系统：** `Worldbuilding/01_游戏策划/OCC_游戏策划案.md`、`OCC_开放世界双模式总流程_v0.2.md`、`OCC_肉鸽模式玩法定义_v0.1.md`、魔法技能与道具内容合同、八元素术式语法、战棋状态与环境标签合同、三层校验样本、拟新增的数值标尺合同、本待办；不修改 Unity、场景、资源或代码。
- **验收标准：** 每个资源和数值档位有唯一单位、上下限、恢复/继承边界和预览格式；主角、普通敌人、精英、首领、个人术式、卷轴和法宝能在同一尺度比较；伤害、护甲、护盾、状态、地格、延时、冷却、行动点、魔力和次数不存在百分比随机、隐藏公式或双模式分叉的战斗结算；明确哪些既有肉鸽首区值保留为暂定基线、哪些需要替换；数值任务不擅自恢复旧七系、旧元素或未决定双元素。
- **决定记录：** 2026-08-06 已采用“四倍核心战斗量级”：生命、伤害、固定护甲减伤、护盾、治疗与掩体耐久相对 M-W5 原型统一 ×4；行动点、魔力、格数、回合、行动条、冷却和法宝次数不变。记录于 `OCC_战棋数值标尺合同_v0.1.md`；保留原型击杀回合数，不以扩大数值拖长战斗。
- **决定记录（续）：** 已采用主角基础个人魔力 12 点、基础术式 2–3 点/强化术式 4–5 点、卷轴与法宝不耗个人魔力；剧情只在明确休息/冥想/环境/资源处恢复，肉鸽跨节点继承且不自动战后回满。已采用状态短档位：常规持续 1–2 个目标回合；燃烧 8/12、迟缓 -2 格、束缚 1 回合、破甲 -4/-8、眩目视距 2 格 1 回合、显形 2 回合。均保持确定性并记录于数值标尺合同。
- **决定记录（续）：** 已采用唯一伤害顺序“基础伤害 → 固定护甲/适用掩体减伤 → 最低 4 点有效伤害 → 护盾 → 生命”；轻/重掩体减伤 4/8，护甲普通 4–8、精英 8–12、首领 12–16，护甲与掩体合计最多减伤 16；基础/强化/强治疗为 12/20/28，只恢复生命，护盾不自然再生。均为两模式共用的确定性规则。
- **决定记录（续）：** 经修正后采用速度条行动模型：每次行动固定 3 AP，速度条可改变实际行动时机；基础移动 5 格（迟缓后 3 格）；基础动作不延时，强术/蓄力/重武器/高价值部署物按公开轻/重延时使下一次行动后移 4/8 刻度；冷却按自身回合为 1/2/3/4 档。目标时长为普通战斗主角 5–7 次行动、精英 7–10 次、首领 10–14 次；不采用“所有单位每完整轮固定一次行动”的限制。
- **决定记录（续）：** 已采用卷轴固定 1 次；法宝按效果规模为轻型 4、标准 3、强力 2、决定性 1 次，现代/远古来源不改变次数。仅高风险、远古或可受主角增幅影响的法宝填写稳定度（常规 2、脆弱/危险 1），归零固定闭锁/报废且不恢复次数。战斗快捷栏最多 4 件卷轴/法宝；背包临时取出或换入固定消耗 1 AP，同名物品不合并次数或稳定度。
- **决定记录（续）：** 已采用行动条标准/快速/迟缓间隔为 10/8/12 刻度，开战公开完整顺序、同刻度按稳定单位 ID；轻/重掩体耐久 24/48，个人术式/强力法宝临时掩体耐久 20/24；任务物普通/受保护/核心耐久 40/80/120，护甲、护盾、弱点和修复须单独公开。
- **决定记录（续）：** 已采用首批模板：主角 80 生命/8 护甲/16 护盾/5 移动/10 间隔；轻型、标准、重型普通敌人分别为 24/4/0/5/8、32/4/0–12/5/10、40/8/16/4/12；精英 64 生命、8–12 护甲、16–24 护盾、4–5 移动、8–12 间隔；首领 120/12/16–32/4/10。初始刻度等于行动间隔、行动后叠加当前间隔；不存在随机先攻。
- **验证记录：** 新增 `OCC_战棋数值标尺合同_v0.1.md`，冻结四倍生命量级、12 点个人魔力、状态、固定防御与恢复、速度条/移动/延时/冷却、卷轴/法宝次数与稳定度、掩体/任务物耐久和首批单位模板。`OCC_三层跨元素校验样本_v0.1.md` 已将伤害与耐久由原型换算为正式首档，行动/魔力/格数/回合/冷却/次数保持原尺度；内容合同与策划案已同步数值来源。全程只改 `Worldbuilding/`，未改 Unity、场景、资源或代码。
- **完成后解锁：** 批量内容表、首批敌人/任务物/奖励条目套用模板，以及固定种子实战数值验证；固定主角稀有天赋不制作肉鸽样本。均不自动启动或修改 Unity。

### M-W5：三层跨元素校验样本 — COMPLETE（2026-08-06）

- **归属：** 剧情模式与肉鸽模式共用的魔法内容生产验证；只修改 `Worldbuilding/`，不修改 Unity、场景、资源或代码，不迁移旧 112 条目录。
- **目标：** 使用已冻结的八元素、三层、造物、状态与环境合同制作一组规模受控的个人术式、卷轴和法宝样本；法宝覆盖现代与远古来源且统一为有限次数消耗品，验证内容字段是否足以支持 D&D 式选择感、单人战棋确定性与近代低技术世界观。
- **涉及文件/系统：** 世界观圣经、魔法技能与道具内容合同、八元素术式语法、战棋状态与环境标签合同、`Worldbuilding/01_游戏策划/OCC_三层跨元素校验样本_v0.1.md`、本待办；双元素组合已延期，不作为样本前置条件。
- **验收标准：** 八元素和个人术式/卷轴/法宝三层均有代表样本；每项明确元素、层级、现代/远古来源、使用资格、法杖/法宝/魔法石/元素石关系、剩余次数、成本、目标、确定性效果、状态/环境、持续/叠加、反制、法律与双模式获取；无隐藏概率、万能效果、未批准双元素、可普通充能法宝或旧七系残留；样本能暴露并修正合同缺口。
- **验证记录：** 新增 `OCC_三层跨元素校验样本_v0.1.md`，完成 24 项原型：八元素各 2 个个人术式，火/冰/光/暗各 1 个卷轴，水/风/土/雷各 1 个法宝；水/雷法宝为现代制作，风/土法宝为远古遗产。每项覆盖身份、资格、法杖/石材关系、行动/魔力/次数、目标、确定性效果、状态/环境、外化、反制、故障、污染、法律、剧情与肉鸽获取。全部法宝为有限次数且不可普通补充；稳定度与次数分离；远古污染只绑定破壳、非法拆解或越限增幅；未引入隐藏概率、固定克制、万能效果、未批准双元素或旧七系。圣经与内容合同已同步校验结论和剩余待决定项；所有数值标为合同校验原型值。只修改 `Worldbuilding/`，未修改 Unity、场景、资源或代码。
- **完成后解锁：** 正式数值标尺、主角天赋专用样本、正式内容批次、掉落/学习/商店配置及旧目录逐条迁移审查；均不自动启动，不自动修改 Unity。

### M-W4：少数双元素组合合同 — DEFERRED（2026-08-06）

- **归属：** 剧情模式与肉鸽模式共用的高阶魔法与单人战棋内容合同；只修改 `Worldbuilding/`，不生产完整技能池，不修改 Unity、场景、资源或代码。
- **目标：** 区分基础环境反应与真正双元素术式，冻结双元素学习资格、主/副元素职责、施术与元素石要求、成本/风险、首批白名单和确定性反制，避免将 28 种配对全部开放或把双元素降为普通元素连携。
- **涉及文件/系统：** 世界观圣经、`OCC_魔法技能与道具内容合同_v0.1.md`、`OCC_八元素术式设计语法_v0.1.md`、`OCC_战棋状态与环境标签合同_v0.1.md`、拟新增的双元素组合合同、本待办；旧术式目录只作候选素材，不直接继承。
- **验收标准：** 双元素与环境标签转化边界明确；每个批准组合都具备主元素、辅助元素、资格、核心效果、允许状态/标签、额外成本、法宝/元素石关系、反制与禁止越界；普通卷轴/法宝不能无条件绕过训练；结果可预览、可重演，无随机失控；未批准配对默认不可生产。
- **延期记录：** 2026-08-06 提出的学习资格、装备要求及火+风、水+冰、土+雷、光+暗四组合白名单未获采用；不写入圣经或内容合同。任务整体延期到后期讨论，当前仍只有“少数高阶能力、不得默认开放全部配对”的既有原则。
- **完成后解锁：** 延期期间不阻塞单元素跨层校验样本；未来重启后再决定双元素资格与白名单。

### M-W3：战棋状态词汇与叠加/互斥合同 — COMPLETE（2026-08-06）

- **归属：** 剧情模式与肉鸽模式共用的确定性单人战棋内容合同；只修改 `Worldbuilding/`，不修改现有 Unity 状态实现、数值、场景、资源或代码。
- **目标：** 在既有燃烧、迟缓、束缚、破甲基线上，决定正式状态池规模，并冻结每种状态的效果语义、持续计时、叠加/刷新、互斥/转化、清除、抗性和预览规则，使八元素术式能够复用少而稳定的状态词汇。
- **涉及文件/系统：** `Worldbuilding/01_游戏策划/OCC_游戏策划案.md` 的状态效果原则、`Worldbuilding/01_游戏策划/OCC_魔法技能与道具内容合同_v0.1.md`、`Worldbuilding/01_游戏策划/OCC_八元素术式设计语法_v0.1.md`、`Worldbuilding/01_游戏策划/OCC_战棋状态与环境标签合同_v0.1.md`、本待办；现有 Unity 实现只作已知基线，不在本任务中修改。
- **验收标准：** 正式状态列表数量受控且每项有唯一语义；持续时点、叠加/刷新上限、来源并存、互斥/转化、清除、免疫/抗性和死亡/战斗结束边界明确；所有结果提交前可预览、可重演，无隐藏概率；环境标签与单位状态分离；剧情/肉鸽使用同一结算规则。
- **验证记录：** 新增 `OCC_战棋状态与环境标签合同_v0.1.md`；正式单位状态限定为燃烧、迟缓、束缚、破甲、眩目、显形六种，另分即时效果与燃烧地格、水面、冰面、烟尘、强光区、暗区、导电路径、障碍/掩体八类环境标签。单位状态按目标回合计时，同名单实例、强度取高、时间取长；免疫/抗性/弱点、定向清除与存档恢复均确定性结算，战斗结束不继承。地格标签分持久与行动条消失标记两类，冻结火水、冰水、火冰、光暗、风烟尘和水雷的基础转化；无随机蔓延、隐藏层数或未预览反应。策划案、八元素语法和内容合同已同步；只修改 `Worldbuilding/`，未修改 Unity、场景、资源或代码。
- **完成后解锁：** 少数双元素组合合同，以及按八元素与三层内容合同制作跨层校验样本；不自动修改 Unity 或批量迁移旧目录。

### M-W2：八元素术式设计语法 — COMPLETE（2026-08-06）

- **归属：** 剧情模式与肉鸽模式共用的世界观、单人战棋技能与环境交互语法；本任务只修改 `Worldbuilding/`，不批量生产具体术式，不修改 Unity、场景、资源或代码。
- **目标：** 为火、水、风、土、雷、冰、光、暗逐一冻结“能做、不能做、个人施术代价、工业风险、战棋动词、环境依赖与明确反制”，并统一法杖、法宝、魔法石、元素石的简化施法装备词汇，建立互不吞并、可供后续内容生产和旧术式迁移审查的元素边界。
- **涉及文件/系统：** `Worldbuilding/00_项目管理/以太_世界观与魔法体系圣经_v0.1.md`、`Worldbuilding/01_游戏策划/OCC_魔法技能与道具内容合同_v0.1.md`、`Worldbuilding/01_游戏策划/OCC_八元素术式设计语法_v0.1.md`、本待办；旧七系与 112 条目录仅作反例和候选素材。
- **验收标准：** 八元素均具备能做/不能做、个人代价、工业风险、战棋动词、环境依赖、反制七维定义；法杖/法宝、魔法石/元素石职责不混淆；冰/水、雷/风保持独立，光/暗同系对立但中性；每种效果可判定主元素且不能用“操控一切”跨越边界；人体代价与工业污染分离；战棋结果确定、可预览、无隐藏概率；已决定/待决定/自主补全分区清楚。
- **验证记录：** 新增 `OCC_八元素术式设计语法_v0.1.md`，冻结魔力外化及维持/定时/永久三级持续，质量/复杂度/生命/食物/稀有材料与相位取证边界；冻结八元素能做/不能做、战棋动词、个人伤病、工业事故、环境依赖和明确反制。以法杖、法宝、魔法石、元素石替代逐元素身体动作与材料媒介清单；后续决定进一步将法宝统一为现代/远古来源的有限次数消耗品，并把内容模型收束为个人术式、卷轴、法宝三层。八元素共用个人魔力/疲劳/伤病规则，无隐藏失败或固定克制倍率。圣经与内容合同已同步，旧“不能无中生有”已失效，旧七系/112 条仍只作待迁移素材；只修改 `Worldbuilding/`，未修改 Unity、场景、资源或代码。
- **完成后解锁：** 战棋状态词汇与叠加/互斥合同、少数双元素组合合同、三层跨元素校验样本；均不自动启动，也不批量迁移旧目录。

### M-W1：魔法载体与技能/道具内容合同 — COMPLETE（2026-08-06；后续更新为三层）

- **归属：** 剧情模式与肉鸽模式共用的世界观、单人战棋技能和道具生产合同；本任务只修改 `Worldbuilding/`，不修改 Unity、场景、资源或代码。
- **目标：** 依据世界观圣经第 0 节的新决策记录，冻结魔法载体的使用者、供能/校准、消耗、法律级别、故障和战棋用途，并建立可直接生产技能与道具条目的字段合同；后续产品决定已将原四层收束为个人术式、卷轴、法宝三层。
- **涉及文件/系统：** `Worldbuilding/00_项目管理/以太_世界观与魔法体系圣经_v0.1.md`、`Worldbuilding/01_游戏策划/OCC_魔法技能与道具内容合同_v0.1.md`、本待办；旧七系、112 条术式与冲突年表只作待迁移素材，不自动继承。
- **验收标准：** 三层规则完整且互不混淆；个人魔力、封装储能与肉鸽运营货币分离；法律按效果/规模/场所/用途判定；战斗内无隐藏随机故障，所有越限和故障后果可预览、可重演；合同覆盖身份、施放、成本、效果、风险、次数、法律、获取和双模式字段；已决定、待决定、自主补全明确分区。
- **验证记录：** 圣经第 0.7 节已冻结八元素、稀有天赋、个人魔力与载体规则，并将旧七系/112 条目录明确降为待迁移素材；新增 `OCC_魔法技能与道具内容合同_v0.1.md`。原四层在后续决定中收束为个人术式、卷轴、法宝三层：现代与远古只作为法宝来源，法宝统一为有限次数消耗品且不能普通充能恢复。合同保留三类“以太”分离、四级法律与物品控制、确定性预览/越限门禁、主角天赋边界及已决定/待决定/自主补全分区。只修改 `Worldbuilding/`，未修改 Unity、场景、资源或代码。
- **完成后解锁：** 八元素术式设计语法、首批个人术式/卷轴/法宝样例，以及按合同批量生产和审查内容；不自动启动，等待下一项产品决定。

### 肉鸽完成线与当前主任务：权威状态（更新于 2026-08-09）

- **当前交付状态：** “肉鸽首区完整切片”、交互/架构基线、火元素完整单局、背包/搜刮、20 件通用法宝、当前时代敌人包、首区关卡与 GitHub 版本基线均已完成；这些成果是后续修复的不可破坏基线。
- **唯一当前主任务：** `SAVE-INTEGRITY-01`（存档语义校验与坏档写保护）正在实施。本轮不并行扩充地区、敌人、法宝或术式。
- **当前路线：** `COMBAT-CLARITY-01` 已完成；现在按 `SAVE-INTEGRITY-01 → STARTER-BUILD-01 → SOURCE-OF-TRUTH-01` 处理存档、初始武器与源文件一致性。
- **完成线顺序：** R-F1 → R-F2 → R-F3 → R-F4 → R-F5 → R-F6；全部完成并作为后续迭代的不可破坏基线保留。
- **暂停：** V2-15「静态优先资产规范化与运行时表现收敛」；保留原记录和产物，不继续扩展。
- **暂停：** V2-22「主角模板三层视觉样本」；保留原记录和产物，不补图、不导入、不替换现有主角。
- **状态说明：** 下方 V1/V2-01/V2-15/V2-22 既有 `IN PROGRESS` 与“保持 V2-15 为唯一主任务”等文字是暂停前的历史快照；`CORE-INTEGRITY-01` 与 `COMBAT-CLARITY-01` 已完成，当前唯一有效实施任务是 `SAVE-INTEGRITY-01`，历史状态不得重新解释为并行任务。

#### R-UXR1：完整交互回归与架构基线冻结 — COMPLETE（2026-08-05）

- **归属：** 肉鸽交互体验与基础架构优化线最终联合回归；不新增内容、不改玩法数值、不扩展后续路线。
- **目标：** 联合回归鼠标、键盘/导航、确认/取消、焦点恢复、基础动效、动画 0%/100%、存档错误、战术重开与完整首区流程，并冻结主要组件职责、依赖方向和扩展入口。
- **涉及文件/系统：** R-UXA1 至 R-UXB1 全部合同、正式 UI/战斗/结算表现、`RogueliteFlowCoordinator`、`RogueliteSaveGateway`、`BattlefieldPresentationAdapter`、技术规划、回归记录与待办；只做修复和文档收口。
- **验收标准：** 101 项既有回归及新增联合合同全部通过；1920×1080/960×540、0%/100%、正式输入/焦点/确认、存档损坏恢复、战术重开、首区固定种子/R-F6、编译/Console/Play Mode/场景脏状态通过；形成可复用架构基线与下一轮扩展入口说明。
- **验证记录：** R-UXA1 至 R-UXB1 联合矩阵与职责/依赖/扩展入口已冻结至 `OCC_肉鸽交互与架构基线回归_2026-08-05.md`。修复确认层关闭时 EventSystem 仍指向被销毁按钮的焦点生命周期边界：关闭前清空模态焦点，仅在原焦点仍激活时恢复。Funplay 真实鼠标点击“取消”与键盘/导航 Submit 均关闭确认并恢复激活的“移动”焦点，位置/AP 保持 `(1,4)/3`；移动到 `(2,4)/2` 后战术重开确定性恢复 `(1,4)/3`。0%/100% 确认流程结果一致并恢复 100%；真实 1920×1080 与固定 960×540 战斗网格、HUD、预览和行动条无裁切，临时尺寸已移除并恢复 Full HD。全部 EditMode 16 类 101/101 通过（含损坏存档恢复与 R-F6）；最终编译错误/警告 0、Console 空、已退出 Play Mode、场景 `isDirty=false`，未保存场景。
- **完成后解锁：** 本优化线完成；不自动启动内容扩容，后续任务由新的产品决定进入待办。

#### R-UXB1：战斗操作预览与错误反馈收口 — COMPLETE（2026-08-05）

- **归属：** 肉鸽模式与共用战斗交互层；只改善既有移动、攻击、技能、搜刮、互动和结束行动的提交前预览与提交后反馈，不改变命令解析、数值、状态顺序或敌人 AI。
- **目标：** 统一表达当前行动的目标规则、有效格、成本、确定性结果与失败原因；无效点击不静默失败，成功后选择状态恢复/保留规则一致。
- **涉及文件/系统：** `BattlefieldPresentationAdapter` 的只读预览合同、`FormalCombatHud`、`CombatPrototypeBootstrap` 兼容协调入口、`UiVisualEvent`/战斗反馈及 EditMode 测试；不保存场景，不修改 `.unity`/`.prefab`/`.asset`。
- **验收标准：** 六类行动提交前均有目标/范围/成本/预期结果；空格、越界、遮挡、无目标、资源不足、冷却、非己方回合等失败均有明确非阻塞原因；成功后选择与目标状态规则统一且无重复命令；1920×1080/960×540、动画 0%/100%、R-F6、编译、Console、Play Mode、场景脏状态通过。
- **验证记录：** 新增只读 `CombatActionPreview` 与 `BattlefieldPresentationAdapter.BuildPreview`/`InvalidReasonForCell`，移动、攻击、两技能、搜刮、互动及结束行动统一给出目标规则、1 AP/以太成本、有效格、确定性伤害/护盾/减伤或效果摘要与阻塞原因；`CombatResolver.PreviewSkillAttack` 复用正式伤害分解而不改状态。攻击/技能锁定校验覆盖射程、关系与视线；搜刮必须点击战利品格，互动仅高亮真实物件/调查格；空格、越界、阻挡、占据、无目标、资源不足、冷却、背包满、敌方回合等均发布非阻塞拒绝原因。成功命令统一清除目标锁定但保留行动类型，失败不清除有效上下文。战斗 HUD 增加三行行动预览并在按钮显示成本/范围；1920×1080 与真实固定 960×540 预览/HUD/行动条无裁切，临时尺寸已移除并恢复 Full HD。运行时空格攻击得到“当前格没有可攻击目标”；结束行动成功后 `enemy_0` 锁定清除且行动仍为“攻击”；0%/100% 不进入预览/命令合同。全部 EditMode 16 类 101/101 通过（含 R-F6 与失败矩阵）；最终编译错误/警告 0、Console 空、已退出 Play Mode、场景 `isDirty=false`，未保存场景。
- **完成后解锁：** R-UXR1「完整交互回归与架构基线冻结」；R-UXB1 完整验收后自动将其设为唯一当前主任务并继续执行。

#### R-ARCH1：流程协调、存档网关与战场展示职责拆分 — COMPLETE（2026-08-05）

- **归属：** 肉鸽模式与共用战斗基础架构；在既有测试保护下逐步缩小 `CombatPrototypeBootstrap`，不改变玩法、数值、存档格式、正式页面语义或无时间压力规则。
- **目标：** 依次抽出肉鸽流程协调器、存档网关与战场展示/输入适配器；保留 `CombatPrototypeBootstrap` 现有公开入口作为兼容薄封装，并明确隔离开发 IMGUI。
- **涉及文件/系统：** `CombatPrototypeBootstrap`、肉鸽流程/存档/战场展示新增纯 C# 服务及对应 EditMode 测试；正式 UI 继续只调用现有协调入口，不保存场景，不修改 `.unity`/`.prefab`/`.asset`。
- **验收标准：** 每次只迁移一个职责并保持 89 项既有回归通过；流程转换、PlayerPrefs 键/兼容/错误报告、格子坐标/点击解析/选择预览分别有明确所有者；开发控制台不承担正式玩家流程；完整首区、1920×1080/960×540、动画 0%/100%、R-F6、编译、Console、Play Mode 与场景脏状态全部通过。
- **验证记录：** 新增纯 C# `RogueliteFlowCoordinator`，统一拥有开发肉鸽运行、地图运行及入口/地图菜单状态，Bootstrap 公开命令保持兼容薄封装；新增 `IRogueliteSaveStore`/`RogueliteSaveGateway` 与 Unity `PlayerPrefsRogueliteSaveStore` 适配器，四个稳定键、兼容解析、删除、Flush 与 `LastError` 集中管理，损坏地图存档安全回退并提示，Bootstrap 已无直接 PlayerPrefs；新增纯数据 `BattlefieldRect`/`BattlefieldPresentationAdapter`，统一 75% 战场矩形、格子坐标、点选解析、移动/攻击/技能范围和朝向。删除 294 行无调用旧 IMGUI 菜单/地图/简报及其死动效字段，开发控制台继续独立默认关闭；Bootstrap 从 1182 行降至 837 行。技术规划已记录职责和依赖方向。Funplay 实测 1920×1080 与真实固定 960×540 战斗网格/HUD/行动条无错位，临时尺寸已移除并恢复 Full HD；运行时 `DeveloperMenu` 初态、地图菜单关闭、开发控制台关闭。全部 EditMode 16 类 97/97 通过（含 R-F6、流程、存档损坏与格子解析）；最终编译错误/警告 0、Console 空、已退出 Play Mode、场景 `isDirty=false`，未保存场景。
- **完成后解锁：** R-UXB1「战斗操作预览与错误反馈收口」；R-ARCH1 完整验收后自动将其设为唯一当前主任务并继续执行。

#### R-UXA3：共用 UI 组件、主题、布局与动效令牌 — COMPLETE（2026-08-05）

- **归属：** 肉鸽模式正式 UI 与共用战斗/结算表现层；只收束共用表现构件，不改变玩法、数值、存档和页面语义。
- **目标：** 收束 `FormalRogueliteUi`、`FormalCombatHud`、`FormalUiInteractionLayer` 与 `RogueliteSettlementPresentation` 重复的 Canvas、Panel、Label、Button、主题色、字号、间距、焦点、图标槽和动效令牌实现。
- **涉及文件/系统：** 新增共用纯表现 UI 工厂/主题/布局合同并迁移正式页面；保留运行时生成方式，不强制 Prefab，不保存场景，不修改 `.unity`/`.prefab`/`.asset`。
- **验收标准：** Canvas 根、排序层、1920×1080 缩放和安全区域合同统一；颜色、字号、间距、按钮四态、焦点轮廓、图标槽、动效时长/缓动来自共用令牌；共用组件不读取或改写玩法状态；移除三层重复构造代码且视觉/交互不回退；1920×1080/960×540、动画 0%/100%、R-F6、编译、Console、Play Mode、场景脏状态通过。
- **验证记录：** 新增纯数据 `UiLayoutContract` 与纯表现 `FormalUiTheme`、`FormalUiButtonPalette`、`FormalUiMotionTokens`、`FormalUiKit`；四个正式表现层统一由工厂创建 Canvas/Scaler/Raycaster，排序读回 40/45/80/100，缩放均为 1920×1080、Match 0.5，安全边距/紧凑高度合同集中定义。字体加载、Panel/Label/Button/Line/Stretch、按钮四态、焦点轮廓、28×28 图标槽与缓动令牌均已共用，四表现类不再重复构造 Canvas、加载字体或直接新增按钮反馈；组件不持有或修改玩法状态。运行时 19/19 按钮焦点令牌一致、有效 EventSystem=1、结算层按需创建时排序/缩放正确。Funplay 实测 1920×1080 与真实固定 960×540 入口布局无裁切/遮挡，临时 Game View 尺寸已移除并恢复 Full HD；0% 无 Tween 且即时 alpha=1，100% 颜色/位移动效生效，连续 8 次快速输入后每个 target 仅 1 条 Tween，功能结果一致。全部 EditMode 13 类 89/89 通过（含 R-F6）；最终编译错误/警告 0、Console 空、已退出 Play Mode、场景 `isDirty=false`，未保存场景。
- **完成后解锁：** R-ARCH1「`CombatPrototypeBootstrap` 职责拆分」；R-UXA3 完整验收后自动将其设为唯一当前主任务并继续执行。

#### R-UXA2：正式 UI 展示状态与事件驱动刷新 — COMPLETE（2026-08-05）

- **归属：** 肉鸽模式正式 UI 与共用战斗/结算展示层；只重构展示数据与刷新机制，不改变战斗、地图、奖励、存档或无时间压力规则。
- **目标：** 停止正式 UI 每帧拼接 JSON/字符串签名并整页销毁重建；建立入口、地图、简报、战斗、结算的只读 presentation model，以版本号/显式变更事件只刷新改变的页面或卡片。
- **涉及文件/系统：** `FormalRogueliteUi`、`FormalCombatHud`、`RogueliteSettlementPresentation`、`CombatPrototypeBootstrap` 协调入口、只读 presentation model/版本事件及 EditMode 测试；保持运行时生成 UI，不强制制作 Prefab，不保存场景。
- **验收标准：** UI 不直接修改 `CombatState`/`RogueliteMapRun`；业务命令继续经明确协调入口；移除每帧 `ToJson`/字符串签名轮询；页面未变化时不重建，单项资源变化不销毁整页；刷新次数、活动对象数量与焦点在连续变化中稳定；1920×1080/960×540、动画 0%/100%、R-F6、编译、Console、Play Mode、场景脏状态全部通过。
- **验证记录：** 新增只读 `RogueliteMapPresentationModel`、`CombatHudPresentationModel`、`SettlementPresentationModel` 与分区 `UiPresentationVersions`/`UiPresentationChange`；模型复制展示所需值，不持有可写玩法引用，命令仍只经 `CombatPrototypeBootstrap` 协调入口。`FormalRogueliteUi` 已移除每帧 `run.ToJson()`/字符串签名，改用 Flow/MapStructure/MapResources/Settings 版本事件；资源标签局部刷新，不销毁页面。`FormalCombatHud` 仅在 Combat/Flow 事件后构建模型并刷新；`RogueliteSettlementPresentation` 移除 `Update` 轮询，只响应 Settlement/Flow/MapStructure 事件。运行时计数：入口静置整页重建保持 1，进入地图为 2；单项资源反馈整页仍为 2，局部刷新 0→1→2，焦点始终“主菜单”，对象数 698→701→698；战斗 HUD 静置保持 1，动作事件后为 2；结算静置保持 2，Settlement 事件后为 3 并创建面板。全部 EditMode 12 类 88/88 通过（含 R-F6）；最终编译错误/警告 0、Console 空、已退出 Play Mode、场景 `isDirty=false`，未保存场景。
- **完成后解锁：** R-UXA3「共用 UI 组件、主题、布局与动效令牌」；R-UXA2 完整验收后自动将其设为唯一当前主任务并继续执行。

#### R-UXV1：基础界面视觉反馈语义扩展 — COMPLETE（2026-08-05）

- **归属：** 肉鸽模式正式 UI 与共用战斗表现层；只消费既有状态和表现事件，不扩充事件、技能、敌人、地区或剧情内容，不修改玩法数值。
- **目标：** 在 R-UXA1 的共用动效令牌上，为地图节点/路径、资源变化、简报/确认、战斗指令链和奖励结算补充由真实状态变化驱动的轻量视觉反馈，并建立只读 `UiVisualEvent`（或同等）入口。
- **涉及文件/系统：** `FormalRogueliteUi`、`FormalCombatHud`、`RogueliteSettlementPresentation`、既有 `CombatFeedbackEvent` 消费与新增只读 UI 视觉事件合同/测试；不保存场景，不修改 `.unity`/`.prefab`/`.asset`，不触碰 Packages、ProjectSettings、`Assets/_Recovery/` 或用户无关改动。
- **验收标准：** 节点选中/可达路径/当前位置/安全回访语义稳定且未知节点不泄露类型；零件、以太、补给、侦测、权限卡只在真实变化时短促反馈；确认主次、战斗选择→范围→无效/目标→提交、结算分段进入与领取确认均由只读事件驱动且不重复结算；960×540 不遮挡文字/目标格/行动条/HUD；菜单与地图不使用屏幕震动；动画 0%/100% 功能结果一致；R-F6、编译、Console、Play Mode、场景脏状态通过。
- **验证记录：** 新增纯数据 `UiVisualEvent`/`UiVisualEventStream`，覆盖地图节点选择/迁移/安全回访、五类资源变化、简报/确认、战斗选择/范围/目标/提交/拒绝、结算打开与奖励领取；所有事件仅由协调层发布，表现层订阅，不反向写玩法状态。地图可达路径加粗冷青、安全回访灰绿、权限不足锈红、未知低饱和灰且不泄露类型；选中节点使用稳定边界。资源真实变化显示带正负方向的短时数字与边框；战斗复用既有范围高亮和 `CombatFeedbackEvent`，拒绝/提交通过非阻塞提示；结算标题/卡片分段进入且按钮不等待动画。修复连续提示时旧 Sequence 误删新提示、0% 提示不自动清理、100% 淡入初值与退出 Play Mode 销毁对象引用问题。Funplay 实测 1920×1080 地图/战斗/结算与真实固定 960×540 地图；临时 Game View 尺寸已移除并恢复 Full HD。0% 提示即时 alpha=1 且定时清理，100% 从 alpha=0 淡入；运行时拒绝事件文案正确。全部 EditMode 11 类 85/85 通过（含 R-F6）；最终编译错误/警告 0、Console 空、已退出 Play Mode、场景 `isDirty=false`，未保存场景。
- **完成后解锁：** R-UXA2「正式 UI 展示状态与事件驱动刷新」；R-UXV1 完整验收后自动将其设为唯一当前主任务并继续执行。

#### R-UXA1：正式 UI 交互状态、焦点与基础动效反馈（首批）— COMPLETE（2026-08-05）

- **归属：** 肉鸽模式正式 UI；输入基础设施与纯表现组件可由共用战斗 HUD 和结算层复用。本任务不扩充事件、技能、敌人、地区、剧情或世界观内容，不调整战斗数值与玩法规则。
- **目标：** 为正式入口、地图、设置、档案、简报、战斗 HUD 和结算建立统一的页面/覆盖层状态、返回栈、默认焦点、焦点恢复、Cancel/Back、正式确认框、禁用原因、非阻塞操作反馈与首批基础动效；支持鼠标和键盘/导航语义，动画强度 0% 与 100% 保持相同功能结果。
- **涉及文件/系统：** `FormalRogueliteUi`、`FormalCombatHud`、`RogueliteSettlementPresentation`、`RuntimeUiEventSystem`、`CombatPrototypeBootstrap` 的现有公开协调入口、R-F6/正式 UI 相关 EditMode 测试；新增或调整纯数据导航/确认/反馈合同与 `UiMotionProfile`。不保存场景，不直接修改 `.unity`/`.prefab`/`.asset`，不触碰 Packages、ProjectSettings、`Assets/_Recovery/` 或用户无关改动。
- **验收标准：** 入口、地图、设置、档案、简报、战斗 HUD 与结算均有合理默认焦点；设置/档案/确认框关闭后恢复原焦点且不引用已销毁对象；覆盖层优先响应 Cancel/Back，简报返回地图、地图返回入口，战斗返回不静默退出；新开覆盖存档、战术重开、离开未完成战斗均经过正式确认且取消不改变玩法/存档状态；不可用主操作显示明确原因；按钮悬停/按下/选中/禁用四态、页面/模态/提示条动效稳定；快速重复输入无 Tween 叠加、重复命令或透明/不可点击残留；1920×1080 与 960×540、动画强度 0%/100%、R-F6、编译、Console、Play Mode 与场景脏状态全部通过。
- **验证记录：** 新增纯数据 `UiScreen`、`UiOverlay`、`UiNavigationState`、确认/反馈合同和 0%/100% `UiMotionProfile`；正式入口、地图、设置、档案、简报、战斗 HUD 与结算接入默认焦点、焦点恢复、覆盖层优先返回、正式三类高影响确认、禁用原因、短时反馈和按钮四态。`RuntimeUiEventSystem` 运行时确认只有 1 个有效 EventSystem；奖励选择加入防双提交与结算中禁用。Funplay 真实射线点击命中“新开推进”并打开覆盖存档确认，默认焦点为“取消”；真实点击“取消”后确认层关闭且焦点恢复“新开推进”，取消未改变存档/玩法状态。Funplay 键盘注入调用可成功发出，但当前编辑器在远程注入时未由 Input System UI 模块消费；返回栈、Cancel 与焦点逻辑由纯合同测试及同一运行时取消入口断言覆盖。真实固定 960×540 临时 Game View 与 1920×1080 均完成入口/确认层检查，临时尺寸已移除并恢复 Full HD；动画终态、Tween Kill 和 0% 即时路径均已检查。全部 EditMode 测试 10 类 82/82 通过，覆盖 R-F6 固定种子、永久安全回访、奖励防重复和存档恢复；最终 Funplay 编译错误/警告 0、Console 错误 0；已退出 Play Mode，活动场景 `isDirty=false`，未保存场景。
- **完成后解锁：** R-UXV1「基础界面视觉反馈语义扩展」；R-UXA1 完整验收后自动将其设为唯一当前主任务并继续执行。

#### R-VQ1：肉鸽完整切片视觉审查与首轮修正 — COMPLETE（2026-08-04）

- **归属：** 肉鸽模式正式 UI 与共用战斗表现层；不扩写剧情或世界观内容。
- **目标：** 按 1920×1080 与 960×540 对入口、地图、设置、档案、简报、战斗 HUD、格上反馈和奖励结算执行完整视觉审查，修复影响正式感、信息层级、可读性与遮挡关系的首轮问题；不改变战斗、状态、奖励或地图规则。
- **涉及文件/系统：** `FormalRogueliteUi`、`FormalCombatHud`、`CombatPrototypeBootstrap`、`CombatVisualFeedback`、`RogueliteSettlementPresentation`、运行时 Canvas/IMGUI 渲染顺序与 RectTransform/字体回归；不保存场景、不替换正式资产。
- **验收标准：** 地图网络在主面板内视觉居中且连接线不形成无意义长空段；档案页以正式卡片分组而非调试文本转储；简报不暴露内部任务 ID；战斗页标题、分区标题和资源标签完整，正式 HUD 不提供开发控制台按钮；编辑器预览地图不与运行时战场重复显示；奖励结算不被旧 IMGUI 战场覆盖；960×540 关键文本达到可读字号且无新增截断/溢出；所有页面颜色继续遵守冷青/安全黄/灰绿/锈红语义；Funplay 编译与 Console 通过，退出 Play Mode，场景 `isDirty=false`。
- **视觉审查记录：** 1920×1080 已逐页复核入口、地图默认/选中态、设置、档案、简报、战斗 HUD、格上反馈与奖励结算；真实固定分辨率 960×540 复核地图、设置、档案及战斗 HUD。运行时隐藏仅供编辑器预览的 Sprite 地图并恢复纯色背景，12×9 战场在左侧 75% 区域居中；地图网络上移并消除顶部无意义空段；档案改为概览、资源与构筑卡片；简报改用中文地点名；修复战斗 HUD 静态标题/结构/护盾/以太等标签未赋值；正式 HUD 的开发控制台按钮改为战术重开，入口最后一处 F1 提示改为自由回访/永久安全/无时间压力规则；奖励结算期间停止旧 IMGUI 战场后绘制。960×540 真实点击命中设置、返回、档案；设置页 26 个活动文本/8 个按钮、档案页 34 个文本/1 个按钮均为 0 截断、0 越界；1920×1080 入口 13 个文本/4 个按钮同样为 0 截断、0 越界。临时 GameView 尺寸已移除并恢复 1920×1080。最终 Funplay 编译错误/警告 0，Console 错误 0；已退出 Play Mode；`Assets/Scenes/CombatPrototype.unity` 为 `isDirty=false`，未保存场景。
- **完成后解锁：** 当前切片形成可供内容扩容与数值平衡复用的正式视觉基线；尚未替换的单位、地块与效果纯资产品质提升须按单独资产批次立项并经过既有像素 QA，不与布局修复混做。

#### R-HF2：正式肉鸽 UI 对齐与地图顶栏重排 — COMPLETE（2026-08-04）

- **归属：** 肉鸽模式正式 UI；只调整表现布局，不改变规则、数值或流程。
- **目标：** 修复地图页资源信息与操作按钮互相覆盖、顶栏层级拥挤及正式肉鸽各页面主要容器未形成统一边距/对齐线的问题；同时排除仅存在于 Unity Game View 的 Gizmo 叠层，确保玩家界面清晰。
- **涉及文件/系统：** `FormalRogueliteUi` 的页眉、地图状态栏、地图/详情双栏、入口/简报/设置/档案卡片及运行时 RectTransform 验证；必要时核对 `FormalCombatHud`、`RogueliteSettlementPresentation`，不修改场景 YAML。
- **验收标准：** 地图顶栏资源与主菜单/档案/设置按钮无重叠且共享清晰对齐线；地图和详情栏间距一致，20 个节点、图例与操作区均在容器内；入口、简报、设置、档案、战斗 HUD 和结算的关键容器无意外相交或越界；1920×1080 与 960×540 均无关键溢出或不可点击控件；不改变任何战斗、状态、奖励、地图或无时间压力规则；Funplay 编译/Console 通过，退出 Play Mode，场景 `isDirty=false`。
- **验证记录：** 代码审计确认旧资源文本范围 `x=42..1382` 与“主菜单”按钮 `x=1260..1430` 确定性重叠。现将等级、经验、零件、以太、补给、侦测、权限卡拆为 7 个 150×40 状态单元，置于独立 1188×60 状态栏；左右边距均为 18，相邻间距统一为 17。主菜单、档案、设置使用独立固定列；状态栏、三枚按钮、地图和详情共 7 个一级区域运行时相交数为 0。地图/详情上沿统一为 162、底边统一为 1012，20 个地图节点相交数 0；地图页共 24 个按钮，在 1920×1080 与同一 16:9 `CanvasScaler` 投影的 960×540 下越界数均为 0。入口与设置页主要卡片/行列复核对齐；Game View 中央白色十字与蓝色手柄来自编辑器 Gizmo，关闭 Game View Gizmo 后消失，未向玩家 UI 写入规避逻辑。Funplay 真实点击仍可从入口进入地图；最终编译错误/警告 0、Console 项目错误 0；未改变玩法或数值。
- **完成后解锁：** 恢复首区完整切片基线，继续内容扩容、数值平衡与品质迭代；若仍有单独页面问题，再按页面独立立项。

#### R-HF1：正式 UI 全按钮输入修复 — COMPLETE（2026-08-04）

- **归属：** 肉鸽模式正式 UI；输入基础设施可由共用战斗 HUD 与结算层复用。
- **目标：** 修复进入游戏后正式入口、地图、战斗 HUD 与结算界面所有 uGUI 按钮点击无反应的问题；不改变按钮业务逻辑、玩法数值或流程规则。
- **涉及文件/系统：** `FormalRogueliteUi`、`FormalCombatHud`、`RogueliteSettlementPresentation`、`TacticalHudSceneBinder`、Presentation asmdef、运行时 `EventSystem`/Input System UI 输入模块与 Funplay 点击回归；不保存场景。
- **验收标准：** 运行时始终只有一个有效 `EventSystem` 且至少有一个启用的 `BaseInputModule`；正式入口“新开推进”可由真实 UI 射线点击命中并进入 20 节点地图；地图、简报、战斗 HUD 和奖励按钮沿用同一输入入口；重复初始化不产生重复事件系统或输入模块；Funplay 编译/Console 通过，退出 Play Mode，场景 `isDirty=false`。
- **验证记录：** 起始诊断确认正式入口存在 4 个可交互 Button 和 1 个 `GraphicRaycaster`，但唯一 `EventSystem` 的 `BaseInputModule` 数量为 0，因此所有 uGUI 无法接收指针/导航事件。新增共用 `RuntimeUiEventSystem.Ensure`，复用或创建唯一事件系统，并为项目当前 `activeInputHandler=1` 的 Unity Input System 配置启用的 `InputSystemUIInputModule` 与默认 UI Actions；`FormalRogueliteUi`、`FormalCombatHud`、`RogueliteSettlementPresentation` 和 `TacticalHudSceneBinder` 全部改用该入口，Presentation asmdef 显式引用 `Unity.InputSystem`。Play Mode 运行时确认 1 个 EventSystem、1 个启用模块、1 个已配置 Input System 模块；连续调用初始化 5 次仍保持 1/1/1。Funplay 真实 UI 射线点击依次命中“新开推进”→“铁路巡逻”→“进入战前简报”→“开始战斗”，流程进入 `Active`；战斗 HUD“攻击”回调把选择从“移动”改为“攻击”，奖励卡回调完成领取并令 `AwaitingReward=false`。最终 Funplay 编译错误/警告 0、Console 无项目错误；已退出 Play Mode；`Assets/Scenes/CombatPrototype.unity` 为 `isDirty=false`，未保存场景。
- **完成后解锁：** 恢复 R-F6 首区完成基线；下一项仍需从内容扩容/平衡候选中单独立项。

#### R-F5-01：肉鸽战斗反馈视觉语义收口（首批）— COMPLETE（2026-08-04）

- **归属：** 肉鸽模式；复用共用战斗表现层，不扩写剧情内容。
- **目标：** 盘点并收束伤害、护盾、破甲、燃烧、束缚、迟缓、治疗/修复、位移、物件破坏的 UI 与视觉语义，建立可复用表现事件入口；不改变伤害、状态、奖励或肉鸽地图规则。
- **涉及文件/系统：** `CombatVisualFeedback`、`FormalCombatHud`、`CombatPrototypeBootstrap`、`RogueliteSettlementPresentation`、相关 EditMode 测试；仅在确有必要时更新肉鸽策划源文件，不修改场景 YAML。
- **验收标准：** 每种已实现基础反馈具有唯一、稳定、可读的颜色、短标签与 UI 文案/图标语义；反馈不遮挡单位主体、目标格、行动条或右侧 HUD；1920×1080 与 960×540 无关键溢出；不改变战斗数值、状态、奖励与无时间压力规则；Funplay 编译通过、Console 无新增项目错误、场景 `isDirty=false`；提供可复用表现事件接口与至少一个针对性验证。
- **验证记录：** 新增纯 C# `CombatFeedbackEvent`、`CombatFeedbackKind` 与 `CombatFeedbackCatalog`，为伤害、护盾吸收、破甲、燃烧、束缚、迟缓、修复、护盾恢复、位移、物件受损/摧毁和目标击破提供 12 个唯一键、中文短标签、HUD 含义与稳定颜色；`CombatVisualFeedback.Publish` 成为统一只读表现入口，现有命令执行后按护盾→生命→状态→击破顺序投递，不改写规则状态；每帧差值入口覆盖敌方行动与持续效果，并以分类缓存避免同一变化重复浮字。格上脉冲改为 66×66 四边细线，四个边线均 `raycastTarget=false`，浮字为 152×28；正式 HUD 使用中文状态语义并显示目标生命/护盾/有效护甲。聚焦 EditMode 测试 6/6 通过，Funplay 纯数据验证 12/12 语义通过，运行时布局断言通过（右栏 438≤480、底栏 1400≤1440）；1920×1080 与 960×540 截图均无关键溢出。最终 Funplay 编译错误/警告 0、Console 错误 0；已退出 Play Mode；场景 `Assets/Scenes/CombatPrototype.unity` 为 `isDirty=false`，未保存场景。
- **完成后解锁：** 表现事件入口已经可供后续系统复用。根据 2026-08-04“先完成完整切片”决定，立即下一步调整为 R-F1-01；R-F5-02 延后到 R-F4 正式 UI/设置完成后执行。

#### R-F1-01：确定性效果执行器核心闭环 — COMPLETE（2026-08-04）

- **归属：** 肉鸽模式；战斗规则层可供共用，但本任务不扩写或实现剧情内容。
- **目标：** 将现有基础攻击、火矢、冰缚、快捷栏恢复和物件交互中分散的目标、成本与结果结算收束到确定性、可组合、可记录的效果执行入口；为后续 100+ 技能提供同一数据合同，不改变当前数值。
- **涉及文件/系统：** `CombatResolver`、`CombatCommand`、`CombatState`、`UnitState`、新增效果定义/执行结果数据、现有 `CombatFeedbackEvent` 消费接口、EditMode 测试；不修改场景 YAML，不触碰肉鸽地图拓扑、奖励或存档隔离规则。
- **验收标准：** 伤害、护盾吸收、治疗/护盾恢复、燃烧/束缚/迟缓/破甲、位移、物件耐久和资源成本均能由同一效果结果模型表达；相同状态与命令输入产生完全相同的有序结果；预览与执行不复制伤害公式；现有基础攻击、火矢、冰缚、医疗包/护盾电池和物件交互均接入新入口且数值回归不变；日志与 `CombatFeedbackEvent` 可从有序结果生成；聚焦 EditMode 与既有战斗测试通过，Funplay 编译/Console 通过，场景 `isDirty=false`。
- **验证记录：** 新增纯运行时 `CombatEffect`、`CombatEffectKind`、`CombatEffectResult`、`CombatEffectExecution` 与 `CombatEffectExecutor`，以稳定序号统一表达 AP/以太成本、护盾吸收、生命伤害、生命/护盾/以太恢复、状态施加/清除、位移、物件耐久与行动延迟。`CombatResolver.Resolve` 现返回有序结果；基础攻击、火矢、冰缚、医疗包、快捷栏护盾电池、移动、搜刮与物件/调查交互已迁移，成功指令的既有数值保持不变。伤害日志读取实际结果；玩家与敌方 `CombatFeedbackEvent` 均由有序结果投递，不再依赖执行前后差值猜测。预览与执行共用 `CalculateDamage`，并修正火矢预览误用物理伤害类型的问题；调查目标不再被破坏物校验误拦截。Funplay 进程内效果/解析器无参数测试 18/18、参数化构筑/任务案例 6/6 通过；旧敌人目录断言同步到既有 11 类、3 个精英/首领变体，未修改敌人内容或数值。最终编译错误/警告 0，Console 为空，Unity 未在 Play Mode，`Assets/Scenes/CombatPrototype.unity` 为 `isDirty=false`，未保存场景。
- **完成后解锁：** R-F2「状态生命周期与结算顺序」，随后进入 R-F3 技能验证池；R-F5-02 延后到正式 UI/设置收口之后执行。

#### R-F2-01：状态生命周期与结算顺序核心闭环 — COMPLETE（2026-08-04）

- **归属：** 肉鸽模式；复用 R-F1 的共用战斗效果层，不扩写剧情内容。
- **目标：** 将燃烧、迟缓、束缚、破甲的施加、覆盖/叠加、回合触发、持续递减、清除与日志顺序收束为明确且可重现的生命周期；保持当前状态数值，消除 `BeginTurn` 内隐式直接扣血与无结果记录路径。
- **涉及文件/系统：** `UnitState`、`CombatResolver`、`CombatEffectExecution`、状态生命周期阶段/结果数据、`CombatFeedbackEvent` 消费、EditMode 测试；不修改肉鸽地图、奖励、敌人目录或场景 YAML。
- **验收标准：** 四种状态均有唯一的施加、持续、触发、递减、到期与驱散顺序；同状态重复施加沿用并显式验证当前“取较长持续时间”规则；燃烧触发伤害经 R-F1 结果层记录，护盾/生命结算和击破顺序可读；束缚与迟缓/破甲的生效窗口固定；相同状态与回合输入产生完全相同的结果序列和日志；现有状态数值与技能成本/冷却不变；聚焦状态测试及既有战斗回归通过，Funplay 编译/Console 通过，场景 `isDirty=false`。
- **验证记录：** 新增 `CombatStatusLifecycle`，以固定的燃烧→迟缓→束缚→破甲顺序执行每个状态的触发、效果、持续递减与到期；燃烧保持既有每回合 2 点直接生命伤害规则，不改为护盾伤害。R-F1 结果层新增 `Triggered`、`DurationReduced`、`Expired`、`Applied`、`Refreshed`、`Preserved`、`Cleared` 明确阶段；重复施加继续取较长持续时间。`BeginTurn`、`AdvanceToNextTurn` 与 `EndTurn` 返回生命周期结果，玩家、敌方、自动结束回合和战术重开路径均将结果交给统一表现入口；日志按确定顺序记录状态施加/刷新/维持、燃烧触发、到期与道具驱散。新增状态生命周期测试 6/6，通过四状态固定顺序、直接生命伤害、持续 1 回合束缚的行动窗口、较长持续时间覆盖、致死燃烧、驱散和克隆确定性验证；连同效果层、解析器与参数化构筑回归共 30/30 通过。最终 Funplay 编译错误/警告 0，Console 为空，Unity 未在 Play Mode，`Assets/Scenes/CombatPrototype.unity` 为 `isDirty=false`，未保存场景。
- **完成后解锁：** R-F3-01「技能组合数据合同与 24–30 技能验证池」；R-F4、R-F5-02 与 R-F6 仍按完整切片顺序继续。

#### R-F3-01：技能组合数据合同与验证池 — COMPLETE（2026-08-04）

- **归属：** 肉鸽模式；技能执行复用 R-F1/R-F2 共用战斗层，不实现剧情内容。
- **目标：** 用“目标规则 + 投递方式 + 基础效果 + 修正器 + 成本/冷却 + 表现事件”建立可验证、可组合的技能数据合同；先交付 24–30 个验证技能和 3–5 条可完成首区战斗的构筑路线，为后续 100+ 技能扩容消除每技能硬编码。
- **涉及文件/系统：** `SkillDefinition`/新技能组合定义、技能目录与校验器、`CombatCommand`、`CombatResolver`、`CombatEffectExecution`、`CombatFeedbackEvent`、正式 HUD 技能读取、EditMode 测试；不修改场景 YAML，不改肉鸽地图、奖励、敌人 AI 或无时间压力规则。
- **验收标准：** 目标、自身/单位/格子、直投/投射/区域等投递与伤害、恢复、护盾、四状态、位移/物件效果能以数据组合；所有技能具有稳定 ID、可读名称、成本、冷却、范围、效果顺序和表现语义；非法组合在载入/测试时给出确定错误；24–30 个技能覆盖至少 3–5 条构筑且不存在命中率、暴击率或隐藏随机；现有火矢、冰缚和基础武器路径兼容；同种子/状态/命令结果一致；聚焦技能与既有战斗回归通过，Funplay 编译/Console 通过，场景 `isDirty=false`。
- **验证记录：** `SkillDefinition` 已扩展为“目标规则 + 投递方式 + 有序效果 + 修正器 + 以太成本/冷却 + 表现语义”数据合同，保留原有伤害、状态、范围等兼容字段；支持自身、敌方、友方、任意单位、格子与可破坏物目标，直投、投射和区域投递，以及伤害、生命/护盾/以太恢复、四状态施加/清除、源单位位移和物件伤害。`SkillCatalogValidator` 对重复 ID、空效果、非法成本、区域半径、格子/物件效果、非正数值和多伤害包提供稳定错误码。新增 `RogueliteSkillCatalog`：27 个验证技能、4 条构筑（余烬突击、锚定控制、以太回路、战地工程），所有技能均有稳定 ID、中文名、范围、成本、冷却、效果顺序和表现语义；不含命中率、暴击率或随机字段。`CombatResolver` 已按组合定义执行单体、区域、自身、友方、格子、位移与物件技能；区域目标按单位 ID 排序，火矢/冰缚迁移后数值兼容。正式 HUD 读取装备技能名称、以太成本、范围和当前冷却，地图点击支持自身、单位、格子与可破坏物规则；表现层新增以太恢复与状态净化语义。技能专项 9/9 通过，其中 27/27 技能均产生有序结果，4/4 构筑均完成同一确定性歼灭切片；合并视觉语义、效果层、解析器、状态生命周期、技能与参数化构筑回归 41/41 通过。最终 Funplay 编译错误/警告 0，Console 无错误，Unity 未在 Play Mode，`Assets/Scenes/CombatPrototype.unity` 为 `isDirty=false`，未保存场景。
- **完成后解锁：** R-F4-01「正式肉鸽 UI 与设置流程收口」，之后执行 R-F5-02 表现资产基线与 R-F6 首区完成线回归。

#### R-F4-01：正式肉鸽 UI 与设置流程收口 — COMPLETE（2026-08-04）

- **归属：** 肉鸽模式；共用正式战斗 HUD 和辅助设置，不扩写或实现剧情内容。
- **目标：** 将肉鸽入口、地图、节点详情、商店、工坊、简报、战斗、结算、档案与设置串成一致的正式玩家流程，替换仍暴露给玩家的开发式 IMGUI 信息架构；开发控制台继续作为独立、可隐藏测试入口。
- **涉及文件/系统：** `CombatPrototypeBootstrap`、`FormalCombatHud`、`RogueliteSettlementPresentation`、肉鸽地图/节点正式表现层、新正式菜单/简报/商店/工坊/档案/设置组件、输入与辅助设置、双分辨率布局验证；优先运行时绑定和复用现有静态资产，不修改场景 YAML。
- **验收标准：** 玩家可仅通过正式 UI 完成新开/继续、地图回访、节点预览、商店/工坊/事件选择、战前简报、战斗、奖励结算、档案查看和返回地图；开发控制台默认隐藏且不承担正式流程；所有按钮具有可读可用/禁用/成本/结果状态；支持音量、动画强度、屏幕震动、浮字、色彩/文本辅助和键位提示的持久设置；1920×1080 保持左侧战场 75%/右侧 HUD 25%，960×540 无关键溢出或不可点击控件；不改变地图、战斗、奖励和无时间压力规则；Funplay 编译、Console、场景脏状态与针对性 UI 测试通过。
- **验证记录：** 新增运行时生成的正式 uGUI `FormalRogueliteUi`，用同一工业控制台语义覆盖新开/继续、20 节点地图、节点预览、商店/事件/休整/宝藏选项、工坊装备与校准、战前简报、只读行动档案和辅助设置；入口、地图和简报阶段不再绘制旧玩家向 IMGUI，战斗棋盘仍由既有确定性绘制入口负责，F1 开发控制台保持独立且默认隐藏。新增 `RogueliteUiPreferences` 数据合同与 PlayerPrefs 持久化，主音量、动画强度、屏幕震动、浮字、高对比、大号文字和键位提示均有正式设置入口，其中音量、反馈时长/位移、单位受击抖动、浮字显示和高对比反馈会即时消费设置。已清战斗节点现在可沿相邻连接安全回访且不会重新开战，统一复用可测试的 `CanTravelTo`/`StartsCombat` 判定，未改变拓扑、数值、奖励或无时间压力规则。专项纯数据合同 4/4 通过；Play Mode 真实 uGUI 点击命中“新开推进”并创建 `map5` 存档，运行对象断言确认地图页生成 20 个节点、24 个按钮且 1920×1080 全部在屏幕边界内；1920×1080 与 960×540 入口截图无关键溢出。Funplay Game View 在阶段切换后停留第 1 帧，故地图页视觉证据以运行层级/RectTransform 边界断言为准，未把缓存入口帧误记为地图截图。最终编译错误/警告 0、Console 项目错误 0；已退出 Play Mode；`Assets/Scenes/CombatPrototype.unity` 为 `isDirty=false`，未保存场景。
- **完成后解锁：** R-F5-02「技能效果 UI/像素表现资产基线」已设为唯一当前主任务；完成后进入 R-F6 首区完整完成线回归。

#### R-F5-02：技能效果 UI/像素表现资产基线 — COMPLETE（2026-08-04）

- **归属：** 肉鸽模式；复用共用战斗表现事件和正式 HUD，不扩写剧情内容。
- **目标：** 基于 R-F5-01 的唯一反馈语义和 R-F3 的技能组合合同，为伤害、护盾、治疗、四状态、位移、区域、物件与资源效果建立可复用的正式图标/静态像素表现资产基线，使 27 个验证技能无需逐技能硬编码特效即可获得一致、可读反馈。
- **涉及文件/系统：** `CombatFeedbackEvent`/`CombatVisualFeedback`、`FormalCombatHud`、技能表现语义映射、既有 `FormalIcons32`/`Icons32` 与通过 QA 的静态像素资产、资产清单和针对性 EditMode/Funplay 验证；不保存场景，不替换未通过 QA 的正式资产，不引入多帧动画依赖。
- **验收标准：** 每类基础效果具有唯一图标/颜色/短标签/格上反馈组合；27 个验证技能全部可由基础语义组合且无缺失或技能专属硬编码；静态资源满足 32×32、Point、硬 alpha 和现有 QA 门禁；表现不遮挡单位、目标格、行动条与右侧 HUD，1920×1080 / 960×540 无关键溢出；辅助设置仍生效；不改变战斗、状态、奖励、地图与无时间压力规则；Funplay 编译/Console、场景脏状态和针对性测试通过。
- **验证记录：** `CombatFeedbackSemantic` 新增稳定 `IconKey`，14 类基础反馈分别固定为唯一的“图标 + 颜色 + 中文短标签”组合；仅复用 `FormalIcons32` 中既有 `move`、`attack`、`skill`、`skill_two`、`loot`、`interact` 六张静态图标，未生成或替换资产。Funplay 导入审计确认 6/6 均为 32×32、Sprite、Point、Clamp、透明通道开启且无 mipmap。`CombatVisualFeedback` 统一加载语义图标，格上反馈卡为 176×28、图标 22×22、全层不接收射线，并按真实 12×9 棋盘中心定位和左侧 75% 战场边界钳制；`FormalCombatHud` 的技能按钮按技能 `PresentationKind` 动态换图与着色，不包含技能 ID 分支。针对性测试与纯数据审计确认 14/14 语义组合唯一、27/27 验证技能均能解析为已批准图标；Play Mode 实测燃烧与修复两张反馈卡均带图标且完全位于左侧 75% 战场，正式技能按钮分别解析到 `skill_two` 与 `skill`。辅助设置路径保持有效；未修改战斗数值、状态、奖励、地图或无时间压力规则。最终 Funplay 编译错误/警告 0、Console 无项目错误；已退出 Play Mode；`Assets/Scenes/CombatPrototype.unity` 为 `isDirty=false`，未保存场景。
- **完成后解锁：** R-F6-01「首区完整完成线回归」已设为唯一当前主任务。

#### R-F6-01：首区完整完成线回归 — COMPLETE（2026-08-04）

- **归属：** 肉鸽模式；只验证并修复首区完整切片，不扩写剧情或世界观内容。
- **目标：** 用固定种子、多条构筑、20 节点自由回访、存档恢复、节点永久安全、正式 UI、完整奖励/结算和双分辨率回归证明首区已达到“之后主要只剩内容扩容与数值平衡”的完成状态；发现阻断时仅修复切片闭环缺口，不顺带扩展内容池。
- **涉及文件/系统：** 肉鸽独立存档与 `RogueliteMapRun`、节点/事件/商店/工坊/战前简报/战斗/奖励/档案正式 UI、R-F1 至 R-F5 的效果/状态/技能/表现层、固定种子回归工具与相关 EditMode/Funplay 验证记录；不保存场景，不修改 20 节点拓扑与无时间压力规则。
- **验收标准：** 至少两个固定种子和 3–4 条验证构筑能完成首区关键路径并产生可重现结果；已访问节点可回访、已清战斗节点永久安全，存档恢复后地图、资源、装备、冷却/状态边界和奖励领取状态一致；入口至地图、节点、简报、战斗、结算、档案和继续游戏均只依赖正式玩家 UI，开发控制台默认隐藏；完整结算无重复领取或丢失推进；1920×1080 与 960×540 无关键溢出；全套聚焦回归、Funplay 编译和 Console 通过，Unity 退出 Play Mode且场景 `isDirty=false`。
- **验证记录：** 新增 `RogueliteCompletionLineTests`。固定种子 620/621 分别锁定“核心守备监工”与“以太净化监工”，均沿真实正交连接完成铁路巡逻、公开额外战斗事件、休整/许可档案、以太精炼、传输塔、核心前哨和区域核心；在新局、待领奖、待事件战、资源/权限变化、各后续战斗和最终完成边界逐次执行 `map5` 往返并要求序列化文本完全一致。测试确认奖励无法重复点击领取、首区四次战斗均可选择未拥有奖励、结算后 `IsComplete=true`，清理后的核心前哨与传输塔可回访且 `StartsCombat=false`；构筑/状态边界恢复为新战斗无残留状态和冷却。4 条验证构筑 × 2 名首领的确定性背后击破矩阵重复执行结果一致；无倒计时、追击、敌情推进、命中率或暴击率合同。完成线专项 4/4 通过，相关肉鸽、UI、技能、效果、状态、解析器和反馈无参数聚焦回归 62 项通过。专项首次运行发现并修复 `RogueliteMapRun.FromJson` 共用恢复函数会把 `start` 错写入完成集的问题：现在仅访问集强制包含入口，`map1–map5` 完成集严格按存档恢复，旧版兼容测试保持通过。Play Mode 正式地图生成 20/20 节点；1920×1080 全部按钮在界内，同一 16:9 `CanvasScaler` 参考坐标投影至 960×540 也无溢出。最终 Funplay 编译错误/警告 0、Console 无项目错误；已退出 Play Mode；`Assets/Scenes/CombatPrototype.unity` 为 `isDirty=false`，未保存场景。完整证据见 `Worldbuilding/03_开发管理/OCC_肉鸽首区完成线回归_2026-08-04.md`。
- **完成后解锁：** 肉鸽首区切片进入完成状态。后续可分别立项技能池、敌人/事件池、地区扩容、数值平衡或品质迭代；每次仍只能有一个唯一当前主任务。

### V1锛氭垬鏂楄〃鐜般€佺粨绠楃晫闈笌鍍忕礌璧勬簮瀹屾垚鍝佸熀绾?- IN PROGRESS

#### R2-00锛氳倝楦界帺娉曞喅绛栭棶鍗?v2 - COMPLETE
- **褰掑睘**锛氳倝楦芥ā寮忋€?- **鐩爣**锛氶€氳繃鍓嶇闂嵎琛ラ綈鑷敱鍥炶鎴块棿鑺傜偣鍥俱€佹帰绱㈣祫婧愩€佹瀯绛戙€佸け璐?鎾ょ銆佷簨浠?宸ュ潑銆佸唴瀹瑰瘑搴﹀拰绂佸尯鐨勪骇鍝佸喅绛栵紝閬垮厤鐩存帴鐚滄祴鎵╁睍鐜╂硶銆?- **娑夊強鏂囦欢/绯荤粺**锛歚Worldbuilding/03_寮€鍙戠鐞?OCC_鑲夐附鐜╂硶鍐崇瓥闂嵎_v2.html`銆佸悗缁倝楦界瓥鍒掓簮鏂囦欢銆佸紑鍙戣鍒掍笌寰呭姙銆?- **楠屾敹鏍囧噯**锛氶棶鍗峰彲鍦ㄦ祻瑙堝櫒淇濆瓨鑽夌骞跺鍑?JSON/Markdown锛涚瓟鍗疯鐩栨墍鏈夊繀绛旈」锛岄殢鍚庡厛鍥哄寲涓虹瓥鍒掓簮鏂囦欢鍐嶅垱寤哄疄鐜颁换鍔°€?- **楠岃瘉缁撴灉**锛氬凡瀵煎叆 `OCC_鑲夐附鐜╂硶鍐崇瓥_v2.json`锛涘喅绛栧凡鍥哄寲鍒?`Worldbuilding/01_娓告垙绛栧垝/OCC_鑲夐附妯″紡鐜╂硶瀹氫箟_v0.1.md`銆?- **瀹屾垚鍚庤В閿?*锛歊2-01 鑷敱鍥炶鑺傜偣鍥句笌鎴块棿鐩綍瀹炵幇銆?
#### R2-01锛氳嚜鐢卞洖璁胯妭鐐瑰浘涓庣┖闂磋祫婧愰棬 - COMPLETE锛?026-07-23锛?- **褰掑睘**锛氳倝楦芥ā寮忋€?- **鐩爣**锛氭妸褰撳墠绾挎€?5 鑺傜偣鍦板浘鏇挎崲涓虹害 20 鑺傜偣鐨勬浜よ嚜鐢卞洖璁跨綉缁滐紝鏄剧ず瀹屾暣鎷撴墤涓庢ā绯婃湭鐭ョ被鍨嬶紱鍔犲叆閽ュ寵/鏉冮檺鍗￠棬锛屼笉寮曞叆鏃堕棿銆佽拷鍑绘垨鍦扮偣鍏抽棴銆?- **娑夊強鏂囦欢/绯荤粺**锛歚RogueliteMapRun`銆佽妭鐐圭洰褰曘€佽倝楦界嫭绔嬪瓨妗ｃ€佸ぇ鍦板浘灞曠ず涓庤妭鐐圭姸鎬併€?- **楠屾敹鏍囧噯**锛氬凡璁块棶鑺傜偣鍙洖璁裤€佹竻鐞嗘垬鏂楁埧姘镐箙瀹夊叏銆侀挜鍖欓棬闃绘尅鏈弧瓒虫潯浠剁殑杩炴帴銆佸墽鎯呭瓨妗ｄ笉鍙楀奖鍝嶏紝Play Mode 鍙獙璇佽妭鐐瑰線杩斾笌鐘舵€佷繚瀛樸€?- **瀹屾垚鍚庤В閿?*锛歊2-02 鑺傜偣鍐呭鐩綍涓庡彲棰勮浜嬩欢銆?- **楠岃瘉缁撴灉锛?026-07-23锛?*锛氬畬鎴?20 鑺傜偣姝ｄ氦鎴块棿鐩綍銆佸弻鍚戠浉閭荤Щ鍔ㄣ€佸凡璁块棶/宸叉竻鐞嗙姸鎬併€佹潈闄愬崱闂ㄤ笌 `map2` 鐙珛瀛樻。锛涙棫 `map1` 瀛樻。鍙縼绉汇€傛秹鍙?`RogueliteMapRun`銆乣RogueliteDeveloperRun`銆乣CombatPrototypeBootstrap` 涓?`RogueliteDeveloperRunTests`銆侾lay Mode 杩愯鏃舵柇瑷€楠岃瘉锛氬叆鍙ｈ繘鍏ユ垬鏂楁埧銆佽繑鍥炲叆鍙ｅ啀鍥炶銆佷簨浠?鍖荤枟/璁稿彲妗ｆ璺緞鑾峰緱 1 寮犳潈闄愬崱骞惰繘鍏モ€滀紶杈撳鈥濇垬鍓嶇畝鎶ワ紱鍙﹂獙璇佹垬鍓嶇畝鎶モ啋姝ｅ紡鎴樻枟鈫掕儨鍒╃粨绠椻啋棰嗗鈫掕繑鍥炲湴鍥撅紝鎴樻枟鎴夸繚鎸佸畨鍏ㄤ笖瀛樻。涓?`map2`銆侳unplay 缂栬瘧閿欒/璀﹀憡涓?0锛汣onsole 浠呮湁鏃㈡湁 `RenderTexture.active` 閲婃斁璀﹀憡銆傛埅鍥惧伐鍏锋寔缁繑鍥為檲鏃у紑鍙戣彍鍗曞抚锛?920脳1080 / 960脳540 浠呰褰曚负鎴浘鍒锋柊闄愬埗锛屼笉浠ュ叾浣滀负 UI 瑙嗚楠屾敹璇佹嵁锛涘満鏅湭淇濆瓨銆?- **涓嬩竴姝?*锛歊2-02 鑺傜偣鍐呭鐩綍涓庡彲棰勮浜嬩欢锛涜ˉ鍏ㄥ悇闈炴垬鏂楁埧鐨勭湡瀹炲晢搴椼€佸伐鍧娿€佷紤鏁淬€佸疂钘忓強浜嬩欢缁撶畻锛屼繚鎸佸彲鍥炶涓庢棤鏃堕棿鍘嬪姏銆?
#### R2-02锛氳妭鐐瑰唴瀹圭洰褰曚笌鍙瑙堜簨浠?- COMPLETE锛?026-07-23锛?- **褰掑睘**锛氳倝楦芥ā寮忋€?- **鐩爣**锛氫负浜嬩欢銆佸伐鍧娿€佸晢搴椼€佷紤鏁翠笌瀹濊棌鑺傜偣寤虹珛鍐呭鐩綍锛涜繘鍏ラ潪鎴樻枟鑺傜偣鍏堟樉绀烘槑纭殑椋庨櫓/鏀剁泭閫夋嫨锛屽啀缁撶畻鑺傜偣鐘舵€侊紝涓嶄互鑷姩瀹屾垚浠ｆ浛鐜╁鍐崇瓥銆?- **娑夊強鏂囦欢/绯荤粺**锛歚RogueliteMapRun`銆佽妭鐐瑰唴瀹圭洰褰曘€佽倝楦藉湴鍥?IMGUI 闈㈡澘銆佺嫭绔嬪瓨妗ｃ€丒ditMode/Play Mode 楠岃瘉銆?- **楠屾敹鏍囧噯**锛氭瘡绫婚潪鎴樻枟鑺傜偣鍧囨湁鍙棰勮锛涗簨浠堕€夋嫨鏄庣‘鏍囨敞鏀剁泭涓庨澶栨垬鏂楀悗鏋滀笖涓嶅惈寮哄埗鎵ｈ锛涚粨绠楃粨鏋滈殢鐙珛瀛樻。鎸佷箙鍖栵紱鍥炶浜嬩欢淇濈暀鏀剁泭閫掑噺鎻愮ず锛涙病鏈夊€掕鏃躲€佽拷鍑绘垨鍦扮偣鍏抽棴銆?- **瀹屾垚鍚庤В閿?*锛歊2-03 鍙岃揣甯併€佸伐鍧婃搷浣溿€佸鍔辩洰褰曚笌榄旀硶渚ф瀯绛戞暟鎹€?- **楠岃瘉缁撴灉锛?026-07-23锛?*锛氫簨浠躲€佸伐鍧娿€佸晢搴椼€佷紤鏁翠笌瀹濊棌鍧囨湁涓ら」鍙棰勮閫夋嫨锛涢潪鎴樻枟鑺傜偣涓嶅啀鑷姩缁撶畻銆傚晢搴椻€滃尰鐤楄ˉ缁欌€濈珛鍗崇粨绠?+1 琛ョ粰骞朵繚瀛橈紱浜嬩欢鈥滆秴杞藉洖鏀垛€濇槑纭睍绀衡€?1 鏉冮檺鍗?/ 杩涘叆涓€鍦洪澶栨垬鏂椻€濓紝鑳滃埄鍚庢墠缁撶畻鏉冮檺鍗★紝涓嶅寘鍚己鍒舵墸琛€銆傝倝楦界嫭绔嬪瓨妗ｅ崌绾т负 `map3`锛屽吋瀹?`map1`/`map2`銆侾lay Mode 杩愯鏃舵柇瑷€銆佸唴瀹圭洰褰曡鐩栨鏌ャ€佸悗缁仛鐒?EditMode 鍧囬€氳繃锛涙棤鍊掕鏃躲€佽拷鍑绘垨鍦扮偣鍏抽棴銆侰onsole 鏃犻」鐩敊璇紱閫€鍑?Play Mode 鏃?DOTween Safe Mode 姹囨€讳簡鏃㈡湁鐨?missing-target/startup 璀﹀憡锛屾湭鎻愪緵鎸囧悜 R2-02 浠ｇ爜鐨勮皟鐢ㄦ爤銆?- **涓嬩竴姝?*锛歊2-03 鍙岃揣甯併€佸伐鍧婃搷浣溿€佸鍔辩洰褰曚笌榄旀硶渚ф瀯绛戞暟鎹€?
#### R2-03锛氬弻璐у竵銆佸伐鍧婃搷浣滀笌濂栧姳鐩綍 - COMPLETE锛?026-07-23锛?- **褰掑睘**锛氳倝楦芥ā寮忋€?- **鐩爣**锛氫负鑲夐附鐙珛瀛樻。鍔犲叆闆朵欢/浠ュお銆佸彲棰勮鍟嗗簵浠锋牸銆佸伐鍧婅澶囨浛鎹笌鍙拷韪牎鍑嗭紱濂栧姳鑾峰緱鍚庝笉鑷姩瑁呭銆?- **娑夊強鏂囦欢/绯荤粺**锛歚OCC_鑲夐附妯″紡鐜╂硶瀹氫箟_v0.1.md`銆乣RogueliteMapRun`銆佽妭鐐瑰唴瀹圭洰褰曘€佸湴鍥鹃潰鏉裤€佸鍔辨敞鍏ャ€丒ditMode/Play Mode 楠岃瘉銆?- **楠屾敹鏍囧噯**锛氭垬鏂椾骇鍑哄弻璐у竵锛涘晢搴椾綑棰濅笉瓒充細闃绘璐拱涓斾笉鏀瑰彉鐘舵€侊紱宸ュ潑浠呰兘瑁呭宸茶幏寰楀鍔憋紱閫夋嫨鐨勮澶囦笌鏍″噯瀹為檯娉ㄥ叆涓嬩竴鍦烘垬鏂楋紱`map4` 瀛樻。鍏煎鏃х増涓斾笉瑙︾鍓ф儏瀛樻。銆?- **瀹屾垚鍚庤В閿?*锛歊2-04 鍗曞尯鍩熸晫浜恒€侀棰嗕笌棣栨壒娴佹淳鍐呭楠岃瘉銆?- **楠岃瘉缁撴灉锛?026-07-23锛?*锛氳倝楦界嫭绔嬪瓨妗ｅ崌绾т负 `map4`锛屽吋瀹?`map1`/`map2`/`map3`銆傛柊灞€榛樿鏈?4 闆朵欢銆? 浠ュお锛涙垬鏂楄儨鍒╄幏寰?2 闆朵欢銆? 浠ュお銆傚晢搴椻€滃尰鐤楄ˉ缁欌€濇秷鑰?2 闆朵欢锛涗綑棰濅笉瓒充細鎶涘嚭鍙閿欒涓斾笉鏀瑰彉璧勬簮銆傚鍔遍鍙栧悗涓嶅啀鑷姩鎹㈣锛屽伐鍧婁粎鍏佽瑁呭宸叉嫢鏈夌殑姝﹀櫒/鏈紡锛涗互澶牎鍑嗘秷鑰?2 浠ュお锛屼负涓嬩竴鍦烘垬鏂楀疄闄呭鍔?1 鐐规姢鐢层€侾lay Mode 瀹炴祴纭 `arcane_wand` 涓庢牎鍑嗘姢鐢插凡娉ㄥ叆鍚庣画鎴樻枟锛涜仛鐒?EditMode銆丗unplay 缂栬瘧閿欒/璀﹀憡 0銆丆onsole 鏃犻」鐩敊璇紱鏈慨鏀瑰満鏅垨鍓ф儏瀛樻。銆?- **涓嬩竴姝?*锛歊2-04 鍗曞尯鍩熸晫浜恒€侀棰嗕笌棣栨壒娴佹淳鍐呭楠岃瘉銆?
#### R2-04锛氬崟鍖哄煙鏁屼汉銆侀棰嗕笌棣栨壒缂栨垚楠岃瘉 - COMPLETE锛?026-07-24锛?- **褰掑睘**锛氳倝楦芥ā寮忋€?- **鐩爣**锛氬皢鍖哄煙鑺傜偣缁戝畾纭畾鎬ф晫浜虹紪鎴愶紝浜や粯 8鈥?0 绉嶆晫浜虹洰褰曚笌棣栦釜鍖哄煙棣栭锛涚簿鑻便€侀棰嗗拰鏅€氳妭鐐瑰湪鎴樺墠绠€鎶ュ強鎴樻枟鐘舵€佷腑鍙尯鍒嗐€?- **娑夊強鏂囦欢/绯荤粺**锛歚OCC_鑲夐附妯″紡鐜╂硶瀹氫箟_v0.1.md`銆乣EnemyArchetypes`銆佸尯鍩熼伃閬囩洰褰曘€乣CombatPrototypeBootstrap`銆丒ditMode/Play Mode 楠岃瘉銆?- **楠屾敹鏍囧噯**锛氭晫浜虹洰褰曡嚦灏?8 绉嶏紱鏅€?绮捐嫳/棣栭鑺傜偣閲囩敤涓嶅悓缂栨垚锛涘尯鍩熸牳蹇冪敓鎴愰棰嗙殑鏄庣‘楂樼敓鍛?鎶ょ浘/鎶ょ敳閰嶇疆锛涙棦鏈夊鍔便€佽揣甯併€佸伐鍧婃瀯绛戜粛鍙繘鍏ュ悗缁垬鏂楋紱涓嶅紩鍏ユ椂闂村帇鍔涙垨鍦烘櫙 YAML 淇敼銆?- **瀹屾垚鍚庤В閿?*锛歊2-05 鍖哄煙浜嬩欢鎵╁厖銆佺浜岄棰嗕笌娴佹淳鍐呭骞宠　銆?- **楠岃瘉缁撴灉锛?026-07-24锛?*锛氬尯鍩熸晫浜虹洰褰曚负 10 绉嶏紙9 绉嶅父瑙?绮捐嫳鏁屼汉鍔犫€滄牳蹇冨畧澶囩洃宸モ€濋棰嗭級銆傛櫘閫氥€佺簿鑻变笌棣栭鑺傜偣鍧囦粠纭畾鎬ч伃閬囩洰褰曠敓鎴愪笉鍚岀紪鎴愶紱棣栭鑺傜偣瀹為檯鐢熸垚 30 鐢熷懡銆? 鎶ょ浘銆? 鎶ょ敳鐨勬牳蹇冨畧澶囩洃宸ャ€侾lay Mode 鎸夌湡瀹炴浜よ矾寰勯獙璇佷俊鍙锋灑绾芥櫘閫氱紪鎴愩€佺簿鑻遍摳閫犲巶銆佷紶杈撳銆佹牳蹇冨墠鍝ㄤ笌鍖哄煙鏍稿績棣栭锛涙棦鏈夊鍔辩粨绠椼€佹潈闄愰棬鍜屾棤鏃堕棿鍘嬪姏瑙勫垯淇濇寔鏈夋晥銆傝仛鐒?EditMode 閫氳繃锛汧unplay 缂栬瘧閿欒/璀﹀憡 0锛孋onsole 鏃犻」鐩敊璇紝鍦烘櫙鏈繚瀛樸€?- **涓嬩竴姝?*锛歊2-05 鍖哄煙浜嬩欢鎵╁厖銆佺浜岄棰嗕笌娴佹淳鍐呭骞宠　銆?
#### R2-05锛氬尯鍩熶簨浠舵墿鍏呫€佺浜岄棰嗕笌娴佹淳骞宠　 - COMPLETE锛?026-07-24锛?- **褰掑睘**锛氳倝楦芥ā寮忋€?- **鐩爣**锛氭墿鍏呭彲棰勮浜嬩欢缁撴灉锛涗负鍗曞尯鍩熷姞鍏ョ浜岄棰嗗彉浣擄紱缁欑幇鏈夊彲鎴樻枟濂栧姳寤虹珛绐佸嚮/鎺у埗/浠ュお娴佹淳鏍囪骞堕獙璇佸鍔遍€夋嫨璺ㄦ祦娲俱€?- **娑夊強鏂囦欢/绯荤粺**锛歚OCC_鑲夐附妯″紡鐜╂硶瀹氫箟_v0.1.md`銆乣RogueliteMapRun`銆佽妭鐐瑰唴瀹圭洰褰曘€佸尯鍩熼伃閬囩洰褰曘€佸湴鍥鹃潰鏉裤€丒ditMode/Play Mode 楠岃瘉銆?- **楠屾敹鏍囧噯**锛氫簨浠朵笁椤圭粨鏋滃潎瀹屾暣棰勮涓旀棤寮哄埗浼よ/鏃堕棿鍘嬪姏锛涘悓涓€灞€棣栭鐢辩瀛愮ǔ瀹氬喅瀹氬苟闅忕嫭绔嬪瓨妗ｆ仮澶嶏紱涓ゅ悕棣栭鏁板€?姝﹀櫒涓嶅悓锛涙瘡娆′笁閫変竴瑕嗙洊鑷冲皯涓ゆ潯娴佹淳銆?- **瀹屾垚鍚庤В閿?*锛歊2-06 瀹屾暣鍖哄煙鍥炲綊銆佹暟鍊艰皟浼樹笌鍐呭鎵╁璁″垝銆?- **楠岃瘉缁撴灉锛?026-07-24锛?*锛氫簨浠跺鍔犫€滃噣鍖栧绠♀€濓細瀹屾暣棰勮 +1 琛ョ粰銆?1 浠ュお銆佹棤棰濆鎴樻枟锛涘師鏈夊嫎娴嬩笌棰濆鎴樻枟鎹㈡潈闄愬崱璺緞淇濈暀銆傚尯鍩熸牳蹇冩牴鎹瀛愮‘瀹氣€滄牳蹇冨畧澶囩洃宸モ€濇垨鈥滀互澶噣鍖栫洃宸モ€濓紝韬唤鍐欏叆 `map5` 骞跺彲鎭㈠锛涘悗鑰呬负 26 鐢熷懡銆? 鎶ょ浘銆? 鎶ょ敳鐨勮繙绋嬩互澶棰嗭紝涓庨噸鐢茶繎鎴橀棰嗗尯鍒嗐€傚鍔辩洰褰曟爣娉ㄧ獊鍑?鎺у埗/浠ュお锛?0 涓瀛愪笅鐨勪笁閫変竴鍧囪嚦灏戣鐩栦袱鏉℃祦娲俱€侾lay Mode銆佽仛鐒?EditMode銆丗unplay 缂栬瘧閿欒/璀﹀憡 0銆丆onsole 鏃犻」鐩敊璇紱鍦烘櫙鏈繚瀛樸€?- **涓嬩竴姝?*锛歊2-06 瀹屾暣鍖哄煙鍥炲綊銆佹暟鍊艰皟浼樹笌鍐呭鎵╁璁″垝銆?
#### R2-06锛氬畬鏁村尯鍩熷洖褰掋€佹暟鍊煎熀绾夸笌鎵╁璁″垝 - COMPLETE锛?026-07-24锛?- **褰掑睘**锛氳倝楦芥ā寮忋€?- **鐩爣**锛氬畬鎴愪竴鏉′粠鍏ュ彛鑷冲尯鍩熸牳蹇冪殑缁熶竴杩愯鏃跺洖褰掞紝鏍稿浜嬩欢銆佸晢搴椼€佸伐鍧娿€佹潈闄愰棬銆佺簿鑻便€侀棰嗐€佸鍔变笌鐙珛瀛樻。锛涘皢鍙獙璇佹暟鍊煎熀绾垮拰涓嬩竴闃舵鎵╁杈圭晫鍐欏叆绛栧垝婧愭枃浠躲€?- **娑夊強鏂囦欢/绯荤粺**锛歚OCC_鑲夐附妯″紡鐜╂硶瀹氫箟_v0.1.md`銆乣RogueliteMapRun`銆佸尯鍩熼伃閬?鑺傜偣鐩綍銆乣CombatPrototypeBootstrap`銆丒ditMode/Play Mode 楠岃瘉璁板綍銆?- **楠屾敹鏍囧噯**锛氬畬鏁磋矾寰勪腑鏃犳椂闂村帇鍔涙垨寮哄埗鎵ｈ锛涢棰嗗彲杈句笖濂栧姳/璐у竵/瀛樻。姝ｇ‘锛涘墽鎯呯姸鎬佷笉琚闂紱缂栬瘧涓?Console 鏃犻」鐩敊璇紱鏄庣‘璁板綍褰撳墠鍐呭涓婇檺銆佹暟鍊煎熀绾垮拰 R2 鍚庣画鎵╁浠诲姟銆?- **瀹屾垚鍚庤В閿?*锛歊3-01 浜嬩欢姹犳墿瀹逛笌鍥炶閫掑噺銆丷3-02 鏋勭瓚/瑁呭鍐呭姹犮€丷3-03 绗簩鍦板尯涓庢晫浜虹編鏈祫浜?QA銆?- **楠岃瘉缁撴灉锛?026-07-24锛?*锛歅lay Mode 浣跨敤鐪熷疄姝ｄ氦鐩搁偦/鍥炶璺緞瀹屾垚鍏ュ彛鈫掗搧璺贰閫烩啋琛ョ粰妫€鏌ョ珯锛堝尰鐤楄ˉ缁欙級鈫掗噹鎴樺伐鍧婏紙濂ユ湳榄旀潠銆佷互澶牎鍑嗭級鈫掍腑缁х獊琚啋涓户浜嬩欢锛堣秴杞介澶栨垬鏂楀苟鑾峰緱鏉冮檺鍗★級鈫掗椄闂ㄢ啋绮捐嫳閾搁€犲巶鈫掍紶杈撳鈫掍互澶簿鐐煎巶锛堝噣鍖栧绠★級鈫掑洖璁夸紶杈撳鈫掓牳蹇冨墠鍝ㄢ啋鍖哄煙鏍稿績棣栭銆傛渶缁?`IsComplete=True`锛宍map5` 瀛樻。鏈夋晥锛屽墽鎯呭摠鍏靛€兼湭鍙橈紱鏈眬鈥滄牳蹇冨畧澶囩洃宸モ€濈敓鍛?30锛屾渶缁堣祫婧愪负 16 闆朵欢銆? 浠ュお銆? 琛ョ粰銆? 鏉冮檺鍗°€? 椤瑰凡棰嗗鍔便€傝仛鐒?EditMode 5/5 閫氳繃锛堥棰嗙瀛?瀛樻。銆佸噣鍖栦簨浠躲€佽法娴佹淳濂栧姳銆佸尯鍩熺紪鎴愩€佸晢搴椾笌鏍″噯锛夛紱Funplay 缂栬瘧閿欒/璀﹀憡 0锛孋onsole 鏃犳潯鐩紝鍦烘櫙鏈繚瀛樹笖涓?dirty銆傞€斾腑涓€娆′粠鍑€鍖栧绠＄洿鎺ヨ烦寰€鏍稿績鍓嶅摠琚纭殑鐩搁偦瑙勫垯鎷掔粷锛屾敼涓哄厛鍥炶浼犺緭濉斿悗閫氳繃锛屼笉鏋勬垚瀹炵幇閿欒銆?
#### V1 杩炵画浜や粯璺嚎锛堝凡閿佸畾锛?- **鎵ц椤哄簭**锛歏1-04 鍙牬鍧忕墿涓庣姸鎬佹晥鏋滃弽棣?鈫?V1-05 鎴樻枟 UI 缁勪欢涓庣姸鎬佽鑼?鈫?M1-01 澶у湴鍥捐妭鐐圭姸鎬佷笌璺緞鏁版嵁 鈫?M1-02 宸ヤ笟鍩庡競澶у湴鍥捐瑙夊師鍨?鈫?V1-06 鎴樻枟/澶у湴鍥?缁撶畻缁熶竴瑙嗚 QA銆?- **缁熶竴楠屾敹**锛氬畬鎴愪笂杩颁簲椤瑰悗涓€娆℃€ф墽琛?Play Mode銆丗unplay 缂栬瘧/Console銆佽仛鐒?EditMode銆?920脳1080 / 960脳540 鎴浘涓庡畬鏁存祦绋嬪洖褰掞紱涓€斾笉鍒囨崲鍏朵粬涓讳换鍔°€?- **褰撳墠涓讳换鍔?*锛歏2-15锛氶潤甯т紭鍏堣祫浜ц鑼冨寲涓庤繍琛屾椂琛ㄧ幇鏀舵暃 - IN PROGRESS

#### V2-15锛氶潤甯т紭鍏堣祫浜ц鑼冨寲涓庤繍琛屾椂琛ㄧ幇鏀舵暃 - IN PROGRESS锛?026-07-25锛?- **鐩爣**锛氱‘璁ゆ湰鍦扮敓鍥句粎浣滀负鐙珛鍗曞浘鍘熸枡锛涚粺涓€浠?`32脳32` 瀵硅薄/鍥炬爣銆乣64脳64` 鍗曚綅闈欏抚銆佺‖ alpha銆丳oint filter銆佹暣鏁扮缉鏀惧拰琛ㄩ潰璇箟 QA 涓哄噯銆傚甯у姩鐢绘殏涓嶄綔涓鸿繍琛屾椂渚濊禆銆?- **娑夊強鏂囦欢/绯荤粺**锛歚Worldbuilding/05_缇庢湳涓庨煶棰?` 璧勪骇娓呭崟涓?QA 鏂囨。銆乣鍍忕礌璧勪骇鍘熸枡/`銆乁nity 鏃㈡湁 `FormalUnits64`/`FormalRelay32`/`FormalIcons32`锛涗笉淇敼鍦烘櫙 YAML锛屼笉瑕嗙洊鐢ㄦ埛 `ProjectSettings.asset`銆?- **楠屾敹鏍囧噯**锛氭瘡涓柊澧為潤鎬佽祫浜ф湁鐙珛鍘熷浘銆佽鑼冨寲 PNG銆?x QA銆佽皟鑹叉澘鍜屾姤鍛婏紱杩愯鏃朵紭鍏堜娇鐢ㄩ潤甯э紝寰呮満/鍙楀嚮鐢ㄦ暣鍍忕礌浣嶇Щ鎴栨姈鍔ㄨ〃杈撅紱涓嶄粠 AI 鎷兼澘纭垏锛屼笉鏂板鏈粡 QA 鐨勫姩鐢讳緷璧栥€?- **褰撳墠宸ュ叿纭**锛氭湰鏈?Aseprite 浣嶄簬 `E:\SteamLibrary\steamapps\common\Aseprite\Aseprite.exe`锛涙湰鍦板伐浣滃彴浠嶄娇鐢?`E:\鏁版嵁搴揬鍥剧墖鐢熸垚\outputs`锛涙湰杞笉浣跨敤杩滅▼鍥惧儚鐢熸垚浠ょ墝閾捐矾銆?- **涓嬩竴姝?*锛氶€夋嫨涓€涓柊鐨勫崟涓€闈欐€佺己鍙ｏ紝瀹屾垚鐙珛鍗曞浘鐢熸垚/瑙勮寖鍖?QA 鍚庡啀鍐冲畾鏄惁瀵煎叆 Unity锛涚户缁繚鎸佸崟涓€涓讳换鍔°€?
#### V2-14锛氬崟浣嶅彈鍑绘暣鍍忕礌鎶栧姩琛ㄧ幇 - COMPLETE锛?026-07-25锛?- **褰掑睘**锛氬墽鎯呮ā寮忎笌鑲夐附妯″紡鍏辩敤鎴樻枟琛ㄧ幇銆?- **鐩爣**锛氬湪鏃㈡湁闈欏抚鍗曚綅璐村浘涓婂鍔犱綆鎴愭湰鍙楀嚮鍙嶉锛屼笉鐢熸垚鎴栦緷璧栧甯у姩鐢伙紱鍛戒腑鐩爣鍦ㄧ煭鏃剁獥鍐呮í鍚戞暣鍍忕礌鎶栧姩銆?- **娑夊強鏂囦欢/绯荤粺**锛歚CombatVisualFeedback`銆乣CombatPrototypeBootstrap`锛涗笉鏀瑰彉浼ゅ銆佺姸鎬佹垨鍦烘櫙鏁版嵁銆?- **楠屾敹鏍囧噯**锛氬彈鍑绘姈鍔ㄦ寔缁害 `0.18s`锛屽亸绉讳负鏁存暟鍍忕礌涓旂粷瀵瑰€间笉瓒呰繃 `2px`锛涢潤甯т粛浣跨敤姝ｅ紡 `64脳64` 璐村浘锛汧unplay 缂栬瘧/Console 閫氳繃锛屽満鏅笉淇濆瓨銆?- **楠岃瘉缁撴灉锛?026-07-25锛?*锛氭柊澧?`UnitShakeOffset` 浣庢垚鏈〃鐜拌矾寰勶紝`NotifyAttack` 涓哄懡涓洰鏍囩櫥璁扮煭鏃舵姈鍔紱Play Mode 杩愯鏃堕獙璇佺洰鏍囧崟浣嶅弽棣堝亸绉绘湁鐣屼笖涓烘暣鏁帮紝姝ｅ紡闈欏抚鏄犲皠涓嶅彉銆侳unplay 缂栬瘧閿欒/璀﹀憡 0锛岄€€鍑?Play Mode 鍚?Console 鏃犻」鐩敊璇紝鍦烘櫙 `isDirty=false`锛屾湭淇濆瓨鍦烘櫙銆?
#### V2-03锛氭寮忓儚绱犺祫浜?QA 鍩虹嚎 - COMPLETE锛?026-07-24锛?- **褰掑睘**锛氬墽鎯呮ā寮忎笌鑲夐附妯″紡鍏辩敤鎴樻枟/鐣岄潰璧勪骇銆?- **鐩爣**锛氱洏鐐瑰苟瑙勮寖姝ｅ紡 `32脳32` 鍥炬爣銆乣32脳32` 鍦板潡銆乣64脳64` 鍗曚綅涓庡悗缁姩鐢昏祫浜э紝寤虹珛鈥滃彲杩涘叆姝ｅ紡璧勪骇 / 浠呭師鍨?/ 浠呮蹇靛弬鑰冣€濈殑 QA 闂ㄦ锛涗笉鎶?AI 鎷兼澘鎴栬剼鏈煩褰㈢粍浠跺綋浣滄寮忕編鏈€?- **褰撳墠鐩樼偣**锛歚UnityProject/Assets/Game/Art/UI/Icons32/` 鐜版湁 `attack`銆乣interact`銆乣loot`銆乣move`銆乣skillOne`銆乣skillTwo` 鍏釜 Unity `32脳32` 鐐硅繃婊ゅ浘鏍囪祫婧愶紱褰撳墠娌℃湁缁忚繃鐙珛甯ц鑼冨寲銆佸熀绾?涓績绾挎姤鍛婄殑 `64脳64` 鍗曚綅鍔ㄧ敾鎴栨寮?`32脳32` 鍦板潡鍒囩墖銆?- **娑夊強鏂囦欢/绯荤粺**锛歚UnityProject/Assets/Game/Art/`銆佸儚绱犺祫浜ф祦姘寸嚎 QA 杈撳嚭銆乁nity 绾圭悊瀵煎叆璁剧疆涓庡悗缁寮忚祫浜ф竻鍗曪紱涓嶆浛鎹㈡湭缁?QA 鐨勮祫浜э紝涓嶄慨鏀瑰満鏅?YAML銆?- **楠屾敹鏍囧噯**锛氭瘡涓寮忚祫浜ф湁鍥哄畾灏哄銆佹暣鏁扮缉鏀俱€佺偣杩囨护銆侀€忔槑杈圭晫/璋冭壊鏉?鍩虹嚎妫€鏌ュ拰 QA 璁板綍锛涘浘鏍囥€佸湴鍧椼€佸崟浣嶅姩鐢诲垎鍒獙鏀讹紱鏈€氳繃璧勪骇鍙兘鏍囦负鍘熷瀷鎴栨蹇靛弬鑰冦€?- **瀹屾垚鍚庤В閿?*锛歏2-04 棣栨壒姝ｅ紡鍍忕礌璧勪骇鍒朵綔锛堟垬鏂楀浘鏍囨墿鍏呫€佸崟浣嶉潤甯?寰呮満銆?2脳9 鍦板潡鍒囩墖锛夈€?- **楠岃瘉缁撴灉锛?026-07-24锛?*锛氭柊澧?`OCC_鍍忕礌璧勪骇_QA娴佺▼_v0.1.md`锛屽浐瀹?`CONCEPT`/`PROTOTYPE`/`QA_PENDING`/`FORMAL` 鐘舵€併€佸師鏂欎笌 QA 杈撳嚭鐩綍銆侀€氱敤鍙婂垎绫婚棬绂併€佸鎵归『搴忋€佺嫭绔嬪抚瑙勫垯鍜?V2-04 閫愰」浜や粯娓呭崟銆傞€氳繃 Funplay `AssetDatabase` 瀹炴祴 `Icons32` 鍏」鍧囦负 `32脳32`銆乣RGBA32`銆乣Point`銆乣Clamp`銆佸崟灞傜汗鐞嗭紱瀹冧滑缂哄皯鐙珛鍘熸枡銆佽疆寤撹涔夈€侀€忔槑杈圭晫鍜岃皟鑹叉澘璇佹嵁锛屽叏閮ㄧ户缁爣涓?`PROTOTYPE`銆傛寮?`64脳64` 鍗曚綅銆乣32脳32` 鍦板潡涓?12脳9 涓户绔欏垏鐗囦粛涓?`MISSING`銆傛湭瀵煎叆鎴栨浛鎹?Assets銆佹湭淇濆瓨鍦烘櫙锛汧unplay 缂栬瘧閿欒/璀﹀憡 0锛屽満鏅?`isDirty=false`銆?
#### V2-04锛氶鎵规寮忓儚绱犺祫浜у埗浣?- COMPLETE锛?026-07-24锛?- **褰掑睘**锛氬墽鎯呮ā寮忎笌鑲夐附妯″紡鍏辩敤鎴樻枟/鐣岄潰璧勪骇銆?- **鐩爣**锛氫互鐙珛鍗曞浘鍘熸枡鍒朵綔绉诲姩/鏀诲嚮/鎶€鑳?鎼滃埉/浜掑姩鍥炬爣锛屼富瑙?姝ユ灙鍏?鐩惧崼/鐏湳甯?绮捐嫳闈欏抚锛屼互鍙婁腑缁х珯鍦板潡銆佽交鎺╀綋銆侀噸鎺╀綋銆佷腑缁у櫒銆佹垬鍒╁搧绠憋紱涓嶇洿鎺ュ鍏?Unity 姝ｅ紡 Assets銆?- **娑夊強鏂囦欢/绯荤粺**锛歚Worldbuilding/05_缇庢湳涓庨煶棰?鍍忕礌璧勪骇鍘熸枡/V2-04/`銆佸搴?`QA/` 杈撳嚭銆乣OCC_鍍忕礌璧勪骇_QA娴佺▼_v0.1.md` 涓庢寮忚祫浜ф竻鍗曪紱鍚庣画閫氳繃瀹℃煡鍚庢墠娑夊強 Unity 绾圭悊瀵煎叆璁剧疆銆?- **楠屾敹鏍囧噯**锛氬浘鏍?瀵硅薄涓?`32脳32`锛屽崟浣嶄负 `64脳64` 涓斾腑蹇冪嚎 `X=32`銆佽剼搴曞熀绾?`Y=58`锛涙瘡椤规湁鐙珛鍘熸枡銆?x 瀹℃煡棰勮銆佽皟鑹叉澘涓?QA 鎶ュ憡锛涙棤 AI 鎷兼澘纭垏銆佷吉闆ⅶ鍥炬垨姝ｅ紡 Assets 瀵煎叆锛涘姩鐢诲彧鍦ㄩ潤甯ф壒鍑嗗悗浠ョ嫭绔嬪抚寮€灞曘€?- **瀹屾垚鍚庤В閿?*锛歏2-05 姝ｅ紡瀵煎叆涓庨鎵硅繍琛屾椂鏇挎崲璇勫锛堜粎闄愰€氳繃 `FORMAL` QA 鐨勮祫浜э級銆?- **闃舵楠岃瘉锛?026-07-24 / 涓昏鍘熸枡锛?*锛氬皢鏃㈡湁鐙珛涓昏缁垮箷鍘熸枡鐧昏鑷?`Worldbuilding/05_缇庢湳涓庨煶棰?鍍忕礌璧勪骇鍘熸枡/V2-04/Units64/`锛屽苟鐢ㄥ儚绱犺祫浜ф祦姘寸嚎鐢熸垚 `64脳64` 鍗曞抚銆?x 涓績绾?鍩虹嚎 QA 鍥俱€佹姤鍛婁笌棰勮銆傝嚜鍔ㄦ姤鍛婇€氳繃鍥哄畾 cell銆佺‖ alpha銆?4 鑹查檺鍒讹紝瑙勮寖鍖栬竟鐣屼负 `[22,12,42,58]`锛岀鍚?`X=32` / `Y=58` 瀵归綈锛涗汉宸ュ鏌ュ垽瀹氱洰鏍囧昂瀵镐笅姝ユ灙涓庤韩浣撶粏鑺傛贩鏉傦紝涓旀湭鍋?Aseprite/PixelOver 鎵嬪伐娓呯悊锛岀户缁爣璁?`QA_PENDING`锛屼笉瀵煎叆 Unity銆傚伐浣滃彴鐨?15 椤圭嫭绔嬪師鏂欒姹傚拰涓€娆¤妭娴侀噸璇曞潎鏀跺埌涓婃父 `429 Too Many Requests`锛涙湭鐢?AI 鎷兼澘纭垏鎴栬剼鏈浘褰㈡浛浠ｏ紝寰呴搴︽仮澶嶅悗缁х画鐢熸垚浣欎笅鍘熸枡銆傛湭淇敼鍦烘櫙 YAML 鎴?`UnityProject/Assets/`銆?- **闃诲鍘熷洜锛?026-07-24锛?*锛氭湰鍦板伐浣滃彴鍙惎鍔ㄤ笖鏈満鍑嵁/涓浆閰嶇疆瀛樺湪锛屼絾鍚庣画涓よ疆鍗曞浘澶嶆祴鍙婃鍓嶇殑鎵归噺璇锋眰鍧囪涓婃父绋冲畾鎷掔粷涓?`429 Too Many Requests`銆傚唴寤哄浘鍍忕敓鎴愬伐鍏峰湪褰撳墠浼氳瘽涓嶅彲鐢紱鏈満鏈彂鐜板彲鍚堟硶绾冲叆 OCC 鐨勫叾浣欑嫭绔嬪師鏂欍€備笉寰椾互鍏朵粬椤圭洰璧勬簮銆佹蹇垫嫾鏉裤€丄I 纭垏鎴栬剼鏈煩褰㈡浛浠ｃ€?- **鎭㈠鏉′欢 / 鎵€闇€鍐冲畾**锛氭仮澶嶄笂娓稿浘鍍忕敓鎴愰搴﹀悗锛岀户缁寜鏃㈠畾 15 椤圭嫭绔嬪崟鍥炬彁绀鸿瘝鐢熸垚锛涙垨鐢变骇鍝佹彁渚涘凡鑾锋巿鏉冪殑鐙珛鍘熸枡涓庝娇鐢ㄨ寖鍥淬€傛仮澶嶅悗鍏堝皢鏈换鍔￠噸鏂拌涓哄敮涓€褰撳墠涓讳换鍔★紝鍐嶇户缁?QA锛屼笉鐩存帴瀵煎叆 Unity銆?- **楠岃瘉缁撴灉锛?026-07-24 / 鍘熸枡瑕嗙洊锛?*锛氭洿鏂版湰鏈哄伐浣滃彴浠ょ墝鍚庯紝涓婃父璋冪敤鎭㈠銆傚畬鎴?5 涓寚浠ゅ浘鏍囥€? 涓崟浣嶉潤甯у拰 5 涓腑缁х珯瀵硅薄鐨勭嫭绔嬪崟鍥惧師鏂欙紱姣忛」宸插綊妗ｅ埌 `鍍忕礌璧勪骇鍘熸枡/V2-04/`锛屽苟瀹屾垚鍥哄畾 cell銆佺‖ alpha銆?x QA銆佽皟鑹叉澘棰勮鍜?JSON 鎶ュ憡銆?5 浠芥姤鍛婄粡 Node JSON 瑙ｆ瀽鍧囨湁鏁堬紱鍥炬爣/瀵硅薄涓?`32脳32`锛屽崟浣嶄负 `64脳64` 涓?QA 鏍囪 `X=32` / `Y=58`銆傛墍鏈夐」鐩繚鎸?`QA_PENDING`锛屾湭瀵煎叆 `UnityProject/Assets/`锛涢泦涓鏌ュ凡鏄庣‘绉诲姩/鏀诲嚮鍥炬爣銆佸湴鍧楄壊鏁板拰鍗曚綅缁嗚妭鐨勬墜宸ユ竻鐞嗚姹傘€?
#### V2-04b锛氶鎵瑰儚绱犺祫浜ц鑼冨寲涓庢寮忓鎵?- COMPLETE锛?026-07-24锛?- **褰掑睘**锛氬墽鎯呮ā寮忎笌鑲夐附妯″紡鍏辩敤鎴樻枟/鐣岄潰璧勪骇銆?- **鐩爣**锛氫互鏈湴鐙珛鐢熷浘鍘熸枡鍔犲儚绱犺鑼冨寲澶勭悊瀹屾垚閫愰」瀹℃壒锛岀‘璁ゅ浘鏍囪疆寤撱€佸崟浣嶄富鐗瑰緛鍜屽湴鍧?鎺╀綋琛ㄩ潰璇箟锛涘彧灏嗛€愰」閫氳繃瑙勬牸 QA 鐨勮祫浜у崌绾т负鍘熸枡搴?`FORMAL`銆?- **娑夊強鏂囦欢/绯荤粺**锛歚Worldbuilding/05_缇庢湳涓庨煶棰?鍍忕礌璧勪骇鍘熸枡/V2-04/`銆丵A 杈撳嚭涓?`OCC_姝ｅ紡鍍忕礌璧勪骇娓呭崟_v0.1.md`锛涗笉淇敼鍦烘櫙 YAML锛屼笉鐩存帴鏇挎崲 Unity 鐜版湁鍘熷瀷璧勬簮銆?- **楠屾敹鏍囧噯**锛?2px 鍥炬爣/瀵硅薄涓?16 鑹叉垨鏇村皯涓旇〃闈㈠惈涔夊彲璇伙紝64px 鍗曚綅瀵归綈 `X=32`/`Y=58` 骞舵湁鍙鲸涓荤壒寰侊紱鍏ㄩ儴澶嶆牳 4x銆侀€忔槑杈圭晫銆佽皟鑹叉澘鍜屾姤鍛娿€傛墜宸ユ竻鐞嗕笌楂樹繚鐪熺編鏈笉浣滀负鏈壒闃诲椤广€?- **瀹屾垚鍚庤В閿?*锛歏2-05 姝ｅ紡瀵煎叆涓庨鎵硅繍琛屾椂鏇挎崲璇勫锛堜粎闄?`FORMAL` 璧勪骇锛夈€?- **楠岃瘉缁撴灉锛?026-07-24 / 瑙勮寖鍖栨壒鍑嗭級**锛氭寜浜у搧纭鐨勨€滆鏍肩鍚堛€佽〃闈㈠惈涔夊彲璇烩€濇爣鍑嗭紝澶嶆牳鏈湴鐙珛鐢熷浘涓庤鑼冨寲杈撳嚭銆?4 椤圭洰褰曞寲鎶ュ憡鍧囦负 `PASS`锛屽苟鍚屾椂瀛樺湪 `qa_4x.png`銆乣palette_4x.png` 涓?JSON 鎶ュ憡锛涗富瑙掔殑鐙珛褰掓。鎶ュ憡涔熶负 `PASS`锛宍64脳64` 杈圭晫 `[22,12,42,58]` 绗﹀悎 `X=32` / `Y=58`銆? 鍥炬爣鍜?5 涓户绔欏璞″潎涓?`32脳32` / 16 鑹诧紱5 鍗曚綅鍧囦负 `64脳64` / 24 鑹层€傞泦涓鏌ュ凡鎵瑰噯 15 椤逛负鍘熸枡搴?`FORMAL`锛屾湭瀵煎叆 `UnityProject/Assets/`銆佹湭鏇挎崲杩愯鏃跺師鍨嬨€佹湭淇濆瓨鍦烘櫙銆侫seprite 璇曢獙鐢诲竷鏈繚瀛樹笖鏈撼鍏ヤ氦浠樸€?
#### V2-05锛氭寮忓鍏ヤ笌棣栨壒杩愯鏃舵浛鎹㈣瘎瀹?- COMPLETE锛?026-07-24锛?- **褰掑睘**锛氬墽鎯呮ā寮忎笌鑲夐附妯″紡鍏辩敤鎴樻枟/鐣岄潰璧勪骇銆?- **鐩爣**锛氬彧灏嗛€氳繃 V2-04b 瑙勮寖鍖栧鎵圭殑鍘熸枡鍒嗘壒瀵煎叆 Unity锛屽苟鍦ㄤ笉淇濆瓨鍦烘櫙鐨勫墠鎻愪笅楠岃瘉杩愯鏃惰鍙栥€丳oint/Clamp/鏃?mipmap銆佹暣鏁板昂瀵稿拰瑙嗚灞傜骇銆?- **娑夊強鏂囦欢/绯荤粺**锛歚UnityProject/Assets/Game/Resources/Art/FormalIcons32/`銆乣FormalCombatHud`銆佸悗缁腑缁х珯/鍗曚綅杩愯鏃惰〃鐜帮紱涓嶄慨鏀瑰満鏅?YAML锛屼笉瑕嗙洊 `Icons32/*.asset` 鍘熷瀷璧勬簮銆?- **楠屾敹鏍囧噯**锛氭瘡鎵瑰鍏ラ兘鏈夋簮鍘熸枡鏄犲皠銆乣Sprite` / Point / Clamp / 鏃?mipmap 妫€鏌ュ拰 Play Mode 瀹炰緥璇佹嵁锛涜繍琛屾椂鍙娇鐢ㄩ€氳繃瀹℃壒鐨?PNG锛屽叆鍙?鍦板浘/缁撶畻娴佺▼涓嶅洖褰掋€?- **宸插畬鎴愬瓙鐩爣锛?026-07-24 / 鎸囦护鍥炬爣锛?*锛氫粠 V2-04 QA 鐨勭嫭绔?`frame_00.png` 瀵煎叆 `move`銆乣attack`銆乣skill`銆乣loot`銆乣interact` 鑷?`Resources/Art/FormalIcons32/`锛屼繚鐣欐棫 `Art/UI/Icons32/*.asset` 鍘熷瀷涓嶅彉銆俙FormalCombatHud` 涓哄叚涓寚浠ゆ寜閽缓绔嬫寮忓浘鏍囧眰锛堜袱鏍兼妧鑳藉叡鐢?`skill`锛夛紝Play Mode 楠岃瘉 6 涓疄渚嬪潎涓?`32脳32`銆乣Point`锛涚紪杈戝櫒澶嶆牳涓?Sprite銆丆lamp銆佹棤 mipmap銆侳unplay 缂栬瘧閿欒/璀﹀憡 0锛涘満鏅湭淇濆瓨銆?- **宸插畬鎴愬瓙鐩爣锛?026-07-24 / 涓户绔欒瑙夛級**锛氬鍏?`floor`銆乣light_cover`銆乣heavy_cover`銆乣relay`銆乣loot_crate` 鑷?`Resources/Art/FormalRelay32/`锛屽叏閮ㄥ鏍镐负 `32脳32`銆丼prite銆丳oint銆丆lamp銆佹棤 mipmap銆傝繍琛屾椂鍙浛鎹㈡棦鏈?`鍦板浘鍙鍖朻 SpriteRenderer锛歅lay Mode 楠岃瘉 108 鍦板潡銆? 杞绘帺浣撱€? 閲嶆帺浣撳拰 1 涓户鍣ㄥ潎浣跨敤姝ｅ紡鍥撅紝鍥為€€鍗犱綅涓?0銆俙loot_crate` 宸插悎瑙勫鍏ワ紝褰撳墠鍦板浘鏃犵湡瀹炴垬鍒╁搧绠辫妭鐐癸紝鏁呮湭鍑┖鍒涘缓鑺傜偣鎴栦繚瀛樺満鏅€?- **宸插畬鎴愬瓙鐩爣锛?026-07-24 / 鎴樺埄鍝佺缁樺埗鎺ュ叆锛?*锛氬湪 `CombatPrototypeBootstrap` 鐨勬棦鏈夋垬鍒╁搧瀹瑰櫒 IMGUI 缁樺埗涓紭鍏堜娇鐢?`FormalRelay32/loot_crate`锛岃创鍥剧己澶辨椂淇濈暀鏂囧瓧鍗犱綅鍥為€€锛涙湭鏂板鐜╂硶銆佽妭鐐规垨鍦烘櫙鍐欏叆銆傞€€鍑?Play Mode 鍚庨噸缂栬瘧閫氳繃锛孭lay Mode 澶嶆牳 `loot_crate` 涓?`32脳32`銆丳oint銆丆lamp锛孋onsole 鏃犳柊澧為敊璇紝鍦烘櫙 `isDirty=false`銆?- **宸插畬鎴愬瓙鐩爣锛?026-07-24 / 鍗曚綅闈欏抚锛?*锛氬鍏?`hero`銆乣rifleman`銆乣shieldguard`銆乣pyromancer`銆乣elite` 鑷?`Resources/Art/FormalUnits64/`锛屽潎涓?`64脳64`銆丼prite銆丳oint銆丆lamp銆佹棤 mipmap銆傛垬鏂楃綉鏍煎疄闄呯粯鍒舵寮忚创鍥撅紝淇濈暀鐢熷懡鏉°€佹剰鍥句笌閫変腑杞粨锛汸lay Mode 楠岃瘉 5/5 璐村浘鍙姞杞斤紝榛樿鎴樻枟鐨勪富瑙掋€佹鏋叺銆佺浘鍗€佺伀鏈笀涓庣嫏鍑绘墜浣跨敤姝ｅ紡鏄犲皠锛岀簿鑻卞厛閿嬪強涓ょ被鐩戝伐鏄犲皠鑷?`elite`銆傛病鏈夊搴斿師鏂欑殑绐佽鑰呬繚鎸佸師鏈夋爣璁帮紝鏈吉閫犲叺绉嶅瑙傛垨淇濆瓨鍦烘櫙銆?- **鍏ㄦ壒鍥炲綊锛?026-07-24锛?*锛歅lay Mode 杩涘叆姝ｅ紡鎴樻枟鍚庯紝缁熶竴楠岃瘉 15/15 宸叉壒鍑嗚祫婧愬潎鍙繍琛屾椂鍔犺浇锛屽昂瀵搞€丳oint 鍜?Clamp 鍧囩鍚堣鏍硷紱姝ｅ紡 HUD 鏈?6 涓寚浠ゅ浘鏍囧疄渚嬶紙涓ゆ牸鎶€鑳藉叡鐢ㄥ浘鏍囷級锛屽湴鍥炬湁 108 鍦板潡銆? 杞绘帺浣撱€? 閲嶆帺浣撱€? 涓户鍣ㄦ寮忓疄渚嬶紝榛樿缂栨垚鏈?5 涓寮忓崟浣嶆槧灏勩€侳unplay 缂栬瘧閿欒/璀﹀憡 0锛孋onsole 鏃犻」鐩敊璇紝閫€鍑?Play Mode 鍚庡満鏅?`isDirty=false`锛屾湭淇濆瓨鍦烘櫙銆?- **瀹屾垚鍚庤В閿?*锛氬悗缁編鏈墿鍏呭厛鎸夊崟涓€涓讳换鍔″鐞嗭細鎴樺埄鍝佺鍦ㄥ疄闄呰妭鐐瑰嚭鐜板悗缁戝畾銆佺獊琚€呯嫭绔嬮潤甯э紝鎴栧湪鎵瑰噯闈欏抚鍩虹涓婂埗浣滅嫭绔嬪抚鍔ㄧ敾锛涗笉寰椾互鐜版湁闈欏抚浼€犲姩鐢汇€?
#### V2-06锛氱獊琚€呯嫭绔嬮潤甯цˉ榻?- COMPLETE锛?026-07-25锛?- **褰掑睘**锛氬墽鎯呮ā寮忎笌鑲夐附妯″紡鍏辩敤鎴樻枟璧勪骇銆?- **鐩爣**锛氫负鐜版湁鈥滅獊琚€呪€濈敓鎴愪竴寮犵嫭绔嬪師鏂欏苟瑙勮寖鍖栦负姝ｅ紡 `64脳64` 闈欏抚锛屼腑蹇冪嚎 `X=32`銆佽剼搴曞熀绾?`Y=58`锛涢€氳繃 QA 鍚庢帴鍏ユ棦鏈夎繍琛屾椂鍗曚綅鏄犲皠銆?- **娑夊強鏂囦欢/绯荤粺**锛歚Worldbuilding/05_缇庢湳涓庨煶棰?鍍忕礌璧勪骇鍘熸枡/V2-06/`銆佸儚绱犺祫浜?QA 杈撳嚭銆乣Resources/Art/FormalUnits64/` 涓?`CombatPrototypeBootstrap`锛涗笉淇敼鍦烘櫙 YAML锛屼笉浼€犲姩鐢汇€?- **楠屾敹鏍囧噯**锛氬師鏂欎负鍗曠嫭鐢熸垚鐨勪竴寮犲浘鑰岄潪鎷兼澘鍒囧浘锛涜緭鍑哄叿澶囧浐瀹?cell銆佺‖閫忔槑杈圭晫銆佸彈鎺ц皟鑹叉澘銆?x 瀹℃煡鍥惧拰 JSON 鎶ュ憡锛沀nity 瀵煎叆涓?Sprite / Point / Clamp / 鏃?mipmap锛孭lay Mode 涓獊琚€呭疄闄呬娇鐢ㄦ柊璐村浘銆?- **瀹屾垚鍚庤В閿?*锛氫笅涓€涓嫭绔嬬編鏈换鍔″彲鍦ㄢ€滄壒鍑嗛潤甯у熀纭€涓婄殑鐙珛甯у姩鐢烩€濇垨鈥滄柊鍏电鐙珛闈欏抚鈥濅腑鎷╀竴寤虹珛锛屼粛淇濇寔鍗曚富浠诲姟銆?- **楠岃瘉缁撴灉锛?026-07-25锛?*锛氭湰鍦板伐浣滃彴鐢熸垚涓€寮犵獊琚€呯嫭绔嬪師鏂欙紝`report.json` 涓?PASS锛岃鑼冨寲杈圭晫 `[16,12,49,58]`锛岀鍚?`64脳64`銆乣X=32`銆乣Y=58`銆佺‖ alpha 涓?24 鑹查檺鍒讹紱4x QA銆佽皟鑹叉澘棰勮涓庡鏌ヨ褰曢綈鍏ㄥ苟鎵瑰噯涓?`FORMAL`銆傚鍏?`Resources/Art/FormalUnits64/raider.png` 鍚庯紝Funplay 閲嶇紪璇戦敊璇?璀﹀憡 0锛汸lay Mode 澶嶆牳璐村浘涓?`64脳64`銆丳oint銆丆lamp锛屽苟閫氳繃杩愯鏃跺弽灏勭‘璁?`绐佽鑰卄 瀹為檯鏄犲皠 `raider`銆傞€€鍑?Play Mode 鍚?Console 鏃犻」鐩敊璇紝鍦烘櫙 `isDirty=false`锛屾湭淇濆瓨鍦烘櫙銆?
#### V2-07锛氫富瑙掑洓甯у緟鏈哄姩鐢?- COMPLETE锛?026-07-25锛?- **褰掑睘**锛氬墽鎯呮ā寮忎笌鑲夐附妯″紡鍏辩敤鎴樻枟璧勪骇銆?- **鐩爣**锛氬熀浜庡凡鎵瑰噯涓昏闈欏抚鍒朵綔鍥涘紶鐙珛鐨勫緟鏈哄懠鍚稿惊鐜師鏂欙紝骞剁粺涓€瑙勮寖鍖栦负 `64脳64`銆佷腑蹇冪嚎 `X=32`銆佽剼搴曞熀绾?`Y=58` 鐨勫惊鐜紱閫氳繃 QA 鍚庢帴鍏ユ垬鏂楃綉鏍肩殑涓昏缁樺埗銆?- **娑夊強鏂囦欢/绯荤粺**锛歚Worldbuilding/05_缇庢湳涓庨煶棰?鍍忕礌璧勪骇鍘熸枡/V2-07/`銆乣Resources/Art/FormalAnimations64/`銆乣CombatPrototypeBootstrap`锛涗笉浠庢嫾鏉垮垏甯с€佷笉鏀瑰満鏅?YAML銆佷笉鏀瑰彉鎴樻枟瑙勫垯銆?- **楠屾敹鏍囧噯**锛氬洓甯у潎涓洪€愬紶鐢熸垚鐨勭嫭绔嬫枃浠讹紝浣跨敤鍚屼竴涓昏鍙傝€冭€岄潪浼洩纰у浘锛涗骇鐗╁惈鐙珛甯с€乣256脳64` strip銆丟IF銆?x QA銆佽皟鑹叉澘涓?JSON 鎶ュ憡锛涘鍏ョ汗鐞嗕负 Sprite / Point / Clamp / 鏃?mipmap锛孭lay Mode 涓富瑙掔粯鍒舵寜鍥哄畾寰幆甯у彉鍖栥€?- **瀹屾垚鍚庤В閿?*锛氫笅涓€椤逛粎鍙湪鍙︿竴鍏电鐙珛寰呮満鍔ㄧ敾鎴栧崟涓€鍔ㄤ綔鍔ㄧ敾涓€夋嫨鍏朵竴锛屼粛椤婚€愬抚鐙珛鐢熸垚涓?QA銆?- **楠岃瘉缁撴灉锛?026-07-25锛?*锛氫互宸叉壒鍑嗕富瑙掗潤甯т负鍏卞悓鍙傝€冿紝閫氳繃鏈湴宸ヤ綔鍙伴€愬紶鐢熸垚 4 涓緟鏈哄懠鍚?閲嶅績鍙樺寲甯э紱姣忓抚鐙珛褰掓。锛屾湭浠庢嫾鏉跨‖鍒囥€傝鑼冨寲鎶ュ憡涓?PASS锛? 甯у潎涓?`64脳64`銆佹瘡甯?24 鑹层€佺‖ alpha銆乣Y=58` 鑴氬簳鍩虹嚎涓?`X=32` 涓績绾匡紱宸蹭骇鍑?`256脳64` strip銆丟IF銆?x QA 涓庤皟鑹叉澘棰勮锛屽苟鎵瑰噯鍘熸枡搴?`FORMAL`銆傚鍏?`Resources/Art/FormalAnimations64/hero_idle_4f.png` 鍚庯紝Funplay 缂栬瘧閿欒/璀﹀憡 0锛汸lay Mode 澶嶆牳绾圭悊涓?`256脳64`銆丳oint銆丆lamp锛岃繍琛屾椂瀛楁宸茬粦瀹氫笖寰幆璁＄畻鏈夋晥锛坄frame=1/4`锛夈€傞€€鍑?Play Mode 鍚?Console 鏃犻」鐩敊璇紝鍦烘櫙 `isDirty=false`锛屾湭淇濆瓨鍦烘櫙銆?
#### V2-08锛氬崟浣嶉潤甯т綆鎴愭湰杩愬姩琛ㄧ幇 - COMPLETE锛?026-07-25锛?- **褰掑睘**锛氬墽鎯呮ā寮忎笌鑲夐附妯″紡鍏辩敤鎴樻枟璧勪骇銆?- **鐩爣**锛氬仠姝娇鐢ㄦ湭鎴愮啛鐨勬湰鍦板甯х敓鎴愪綔涓鸿繍琛屾椂渚濊禆锛屼繚鐣欏凡鎵瑰噯鍗曚綅闈欏抚锛屽苟浠ュ叏鍗曚綅鐨勬暣鍍忕礌浣庡箙搴﹀瀭鐩村井浣嶇Щ琛ㄨ揪寰呮満鐘舵€併€?- **娑夊強鏂囦欢/绯荤粺**锛歚CombatPrototypeBootstrap` 涓庣幇鏈?`FormalUnits64` 闈欏抚锛涙挙鍥炴湰杞湭鎵瑰噯銆佹湭瀵煎叆鐨勭獊琚€呭甯у師鏂欙紝涓嶄慨鏀瑰満鏅?YAML锛屼笉鏀瑰彉鎴樻枟瑙勫垯銆?- **楠屾敹鏍囧噯**锛氭墍鏈夋寮忓崟浣嶄粛鎸夌幇鏈夐潤甯ф槧灏勶紱寰呮満浣嶇Щ淇濇寔鏁存暟鍍忕礌銆佸箙搴︿笉瓒呰繃 1px銆佷笉浜х敓妯＄硦鎴栨椂闂村帇鍔涳紱Funplay 缂栬瘧/Console 閫氳繃涓斿満鏅笉淇濆瓨銆?- **瀹屾垚鍚庤В閿?*锛氫笅涓€椤圭編鏈墿鍏呬紭鍏堥€夋嫨鐙珛闈欏抚鎴栧崟涓€鍙嶉琛ㄧ幇锛涘彧鏈夋湰鍦板甯т竴鑷存€ф垚鐔熷苟鐢变骇鍝佸鏍稿悗鎵嶉噸鏂板垱寤哄姩鐢讳换鍔°€?- **楠岃瘉缁撴灉锛?026-07-25锛?*锛氭寜浜у搧鍙嶉鍋滄 V2-08 鐨勬湭鎻愪氦绐佽鑰呭洓甯у師鏂欍€丵A 涓?Unity 瀵煎叆锛屼笉灏嗗叾鍒椾负姝ｅ紡璧勪骇銆俙CombatPrototypeBootstrap` 鏀逛负鎵€鏈夋寮忓崟浣嶄娇鐢ㄦ棦鏈?`FormalUnits64` 闈欏抚锛屽姞鐩镐綅閿欏紑鐨勬暣鍍忕礌鍨傜洿寰綅绉伙紱杩愯鏃跺鏍镐富瑙掗潤甯т负 `64脳64`銆丳oint銆丆lamp锛屼綅绉讳负鏁存暟涓斿箙搴?`鈮?px`锛宍raider_idle_4f` 璧勬簮涓嶅瓨鍦ㄣ€侳unplay 缂栬瘧閿欒/璀﹀憡 0锛孋onsole 鏃犻」鐩敊璇紝閫€鍑?Play Mode 鍚庡満鏅?`isDirty=false`锛屾湭淇濆瓨鍦烘櫙銆?
#### V2-09锛氫腑缁х珯闈欐€佸湴鍧楀彉浣?- COMPLETE锛?026-07-25锛?- **褰掑睘**锛氬墽鎯呮ā寮忎笌鑲夐附妯″紡鍏辩敤鎴樻枟鍦板浘璧勪骇銆?- **鐩爣**锛氳ˉ鍏呮櫘閫氬伐涓氬湴闈€佽建閬撳湴闈€佽鎴掑湴闈笁寮犵嫭绔嬮潤鎬?`32脳32` 鍘熸枡锛涢€氳繃瑙勬牸 QA 鍚庢浛鎹㈢幇鏈?12脳9 涓户绔欏湴鍥句腑鐨勬棦鏈?`SpriteRenderer`锛屾彁鍗囪矾绾夸笌鍗遍櫓鍖虹殑琛ㄩ潰鍙鎬с€?- **娑夊強鏂囦欢/绯荤粺**锛歚Worldbuilding/05_缇庢湳涓庨煶棰?鍍忕礌璧勪骇鍘熸枡/V2-09/`銆乣Resources/Art/FormalRelay32/`銆乣CombatPrototypeBootstrap`锛涗笉浠庢嫾鏉垮垏鍥俱€佷笉淇濆瓨鍦烘櫙銆佷笉鏀瑰彉 tile 纰版挒鎴栨垬鏂楄鍒欍€?- **楠屾敹鏍囧噯**锛氫笁寮犺緭鍏ュ潎鐙珛鐢熸垚锛涜緭鍑轰负 `32脳32`銆?6 鑹叉垨鏇村皯銆佺‖ alpha/涓嶉€忔槑鍦伴潰杈圭晫銆?x QA銆佽皟鑹叉澘鍜?JSON 鎶ュ憡锛沀nity 瀵煎叆涓?Sprite / Point / Clamp / 鏃?mipmap锛孭lay Mode 涓笁绫诲湴鍧楅兘鏈夋寮忓疄渚嬨€?- **瀹屾垚鍚庤В閿?*锛氫笅涓€椤瑰彧鍙墿灞曞彟涓€涓崟涓€闈欐€佺幆澧冨璞℃垨鍗曚綅闈欏抚锛涚户缁粯璁ら潤甯т笌浣庢垚鏈弽棣堬紝涓嶅垱寤哄甯у姩鐢汇€?- **楠岃瘉缁撴灉锛?026-07-25锛?*锛氭湰鍦板伐浣滃彴鐙珛鐢熸垚鏅€氬伐涓氥€佽建閬撱€佽鎴掍笁寮犲湴鍧楀師鏂欙紱瑙勮寖鍖栨姤鍛婂潎 PASS锛屽潎涓?`32脳32`銆?6 鑹层€佺‖ alpha锛屽苟鍏峰 4x QA銆佽皟鑹叉澘涓?JSON 鎶ュ憡銆傚鍏?`FormalRelay32` 鍚?Funplay 閲嶇紪璇戦敊璇?璀﹀憡 0锛汸lay Mode 澶嶆牳 108 鏍间腑鏅€?78銆佽建閬?24銆佽鎴?6锛屼笁绫昏创鍥惧潎 `32脳32`銆丳oint锛屽洖閫€ 0銆傞€€鍑?Play Mode 鍚?Console 鏃犻」鐩敊璇紝鍦烘櫙 `isDirty=false`锛屾湭淇濆瓨鍦烘櫙銆?
#### V2-10锛氫腑缁у櫒鐮存崯闈欐€佸弽棣?- COMPLETE锛?026-07-25锛?- **褰掑睘**锛氬墽鎯呮ā寮忎笌鑲夐附妯″紡鍏辩敤鎴樻枟鍦板浘璧勪骇銆?- **鐩爣**锛氫负鐜版湁鍙牬鍧忎腑缁у櫒鐩爣琛ュ厖涓€寮犵嫭绔?`32脳32` 鐮存崯/鐔勭伃鐘舵€佽创鍥撅紝鍦?`TileState.IsDestroyed` 涓虹湡鏃舵浛鎹㈡棦鏈夌洰鏍?Sprite锛涗笉鏂板鐜╂硶鐘舵€侊紝鍙彁鍗囧畬鎴愬弽棣堝彲璇绘€с€?- **娑夊強鏂囦欢/绯荤粺**锛歚Worldbuilding/05_缇庢湳涓庨煶棰?鍍忕礌璧勪骇鍘熸枡/V2-10/`銆乣Resources/Art/FormalRelay32/relay_destroyed.png`銆乣CombatPrototypeBootstrap`锛涗笉鏀圭洰鏍囪鍒欍€佷笉淇濆瓨鍦烘櫙銆?- **楠屾敹鏍囧噯**锛氱嫭绔嬪師鏂欒鑼冨寲涓?`32脳32`銆?6 鑹叉垨鏇村皯銆佺‖ alpha銆?x QA銆佽皟鑹叉澘鍜?JSON 鎶ュ憡锛沀nity 瀵煎叆 Sprite / Point / Clamp / 鏃?mipmap锛涚洰鏍囨湭鎽ф瘉鏃朵娇鐢ㄦ甯?`relay`锛屾懅姣佸悗浣跨敤 `relay_destroyed`銆?- **瀹屾垚鍚庤В閿?*锛氬悗缁編鏈墿鍏呬粛淇濇寔鍗曚竴闈欐€佸璞℃垨鍗曚綅闈欏抚浠诲姟锛岄粯璁や笉鍒涘缓澶氬抚鍔ㄧ敾銆?- **楠岃瘉缁撴灉锛?026-07-25锛?*锛氭湰鍦板伐浣滃彴鐙珛鐢熸垚骞惰鑼冨寲 `relay_destroyed`锛屾姤鍛?PASS锛宍32脳32`銆?6 鑹层€佺‖ alpha銆?x QA 涓庤皟鑹叉澘榻愬叏銆俇nity 瀵煎叆澶嶆牳涓?Sprite / Point / Clamp / 鏃?mipmap锛汸lay Mode 纭鐩爣鍒濆 `destroyed=false` 浣跨敤瀹屾暣 `relay`锛岀牬鎹熻祫婧愬彲鍔犺浇涓旂洰鏍囩粯鍒堕€昏緫鎸?`TileState.IsDestroyed` 鍒囨崲銆侳unplay 缂栬瘧閿欒/璀﹀憡 0锛岄€€鍑?Play Mode 鍚?Console 鏃犻」鐩敊璇紝鍦烘櫙 `isDirty=false`锛屾湭淇濆瓨鍦烘櫙銆?
#### V2-11锛氳交鎺╀綋鐮存崯闈欐€佸弽棣?- COMPLETE锛?026-07-25锛?- **褰掑睘**锛氬墽鎯呮ā寮忎笌鑲夐附妯″紡鍏辩敤鎴樻枟鍦板浘璧勪骇銆?- **鐩爣**锛氫负杞绘帺浣撹ˉ鍏呬竴寮犵嫭绔?`32脳32` 鐮存崯璐村浘锛屽湪鏃㈡湁 `TileState.IsDestroyed` 涓虹湡鏃舵浛鎹㈣交鎺╀綋瑙嗚锛涗笉鏂板鐘舵€佹垨鐜╂硶銆?- **娑夊強鏂囦欢/绯荤粺**锛歚Worldbuilding/05_缇庢湳涓庨煶棰?鍍忕礌璧勪骇鍘熸枡/V2-11/`銆乣Resources/Art/FormalRelay32/light_cover_destroyed.png`銆乣CombatPrototypeBootstrap`锛涗笉鏀规帺浣撹€愪箙瑙勫垯銆佷笉淇濆瓨鍦烘櫙銆?- **楠屾敹鏍囧噯**锛氱嫭绔嬪師鏂欒鑼冨寲涓?`32脳32`銆?6 鑹叉垨鏇村皯銆佺‖ alpha銆?x QA銆佽皟鑹叉澘鍜?JSON 鎶ュ憡锛沀nity 瀵煎叆 Sprite / Point / Clamp / 鏃?mipmap锛涜交鎺╀綋鑰愪箙褰掗浂鍚庢樉绀虹牬鎹熷浘锛屾湭鐮村潖鏃朵粛鏄剧ず姝ｅ父鍥俱€?- **瀹屾垚鍚庤В閿?*锛氬悗缁編鏈墿鍏呯户缁竴娆″彧澶勭悊涓€涓潤鎬佸璞℃垨鍗曚綅闈欏抚锛岄粯璁や笉鍒涘缓澶氬抚鍔ㄧ敾銆?- **楠岃瘉缁撴灉锛?026-07-25锛?*锛氭湰鍦板伐浣滃彴鐙珛鐢熸垚骞惰鑼冨寲 `light_cover_destroyed`锛屾姤鍛?PASS锛宍32脳32`銆?6 鑹层€佺‖ alpha銆?x QA 涓庤皟鑹叉澘榻愬叏銆俇nity 瀵煎叆澶嶆牳涓?Sprite / Point / Clamp / 鏃?mipmap锛汸lay Mode 纭杞绘帺浣撳垵濮?`durability=4`銆乣destroyed=false` 浣跨敤姝ｅ父 `light_cover`锛岀牬鎹熻祫婧愬彲鍔犺浇涓旂粯鍒堕€昏緫鎸?`TileState.IsDestroyed` 鍒囨崲銆侳unplay 缂栬瘧閿欒/璀﹀憡 0锛岄€€鍑?Play Mode 鍚?Console 鏃犻」鐩敊璇紝鍦烘櫙 `isDirty=false`锛屾湭淇濆瓨鍦烘櫙銆?
#### V2-12锛氶噸鎺╀綋鐮存崯闈欐€佸弽棣?- COMPLETE锛?026-07-25锛?- **褰掑睘**锛氬墽鎯呮ā寮忎笌鑲夐附妯″紡鍏辩敤鎴樻枟鍦板浘璧勪骇銆?- **鐩爣**锛氫负閲嶆帺浣撹ˉ鍏呬竴寮犵嫭绔?`32脳32` 鐮存崯璐村浘锛屽湪鏃㈡湁 `TileState.IsDestroyed` 涓虹湡鏃舵浛鎹㈤噸鎺╀綋瑙嗚锛涗笉鏂板鐘舵€佹垨鐜╂硶銆?- **娑夊強鏂囦欢/绯荤粺**锛歚Worldbuilding/05_缇庢湳涓庨煶棰?鍍忕礌璧勪骇鍘熸枡/V2-12/`銆乣Resources/Art/FormalRelay32/heavy_cover_destroyed.png`銆乣CombatPrototypeBootstrap`锛涗笉鏀规帺浣撹€愪箙瑙勫垯銆佷笉淇濆瓨鍦烘櫙銆?- **楠屾敹鏍囧噯**锛氱嫭绔嬪師鏂欒鑼冨寲涓?`32脳32`銆?6 鑹叉垨鏇村皯銆佺‖ alpha銆?x QA銆佽皟鑹叉澘鍜?JSON 鎶ュ憡锛沀nity 瀵煎叆 Sprite / Point / Clamp / 鏃?mipmap锛涢噸鎺╀綋鑰愪箙褰掗浂鍚庢樉绀虹牬鎹熷浘锛屾湭鐮村潖鏃朵粛鏄剧ず姝ｅ父鍥俱€?- **瀹屾垚鍚庤В閿?*锛氬悗缁編鏈墿鍏呯户缁竴娆″彧澶勭悊涓€涓潤鎬佸璞℃垨鍗曚綅闈欏抚锛岄粯璁や笉鍒涘缓澶氬抚鍔ㄧ敾銆?- **楠岃瘉缁撴灉锛?026-07-25锛?*锛氭湰鍦板伐浣滃彴鐙珛鐢熸垚骞惰鑼冨寲 `heavy_cover_destroyed`锛屾姤鍛?PASS锛宍32脳32`銆?6 鑹层€佺‖ alpha銆?x QA 涓庤皟鑹叉澘榻愬叏銆俇nity 瀵煎叆澶嶆牳涓?Sprite / Point / Clamp / 鏃?mipmap锛汸lay Mode 纭閲嶆帺浣撳垵濮?`durability=7`銆乣destroyed=false` 浣跨敤姝ｅ父 `heavy_cover`锛岀牬鎹熻祫婧愬彲鍔犺浇涓旂粯鍒堕€昏緫鎸?`TileState.IsDestroyed` 鍒囨崲銆侳unplay 缂栬瘧閿欒/璀﹀憡 0锛岄€€鍑?Play Mode 鍚?Console 鏃犻」鐩敊璇紝鍦烘櫙 `isDirty=false`锛屾湭淇濆瓨鍦烘櫙銆?
#### V2-13锛氭垬鍒╁搧绠卞紑鍚潤鎬佸弽棣?- COMPLETE锛?026-07-25锛?- **褰掑睘**锛氬墽鎯呮ā寮忎笌鑲夐附妯″紡鍏辩敤鎴樻枟鍦板浘璧勪骇銆?- **鐩爣**锛氫负鎴樺埄鍝佺琛ュ厖涓€寮犵嫭绔?`32脳32` 寮€鍚?绌虹璐村浘锛屽湪 `LootContainer.IsLooted` 涓虹湡鏃舵浛鎹㈢幇鏈夌浣撹瑙夛紱涓嶆敼鍙樻悳鍒鍔便€佽揣甯佹垨浜や簰瑙勫垯銆?- **娑夊強鏂囦欢/绯荤粺**锛歚Worldbuilding/05_缇庢湳涓庨煶棰?鍍忕礌璧勪骇鍘熸枡/V2-13/`銆乣Resources/Art/FormalRelay32/loot_crate_open.png`銆乣CombatPrototypeBootstrap`锛涗笉淇濆瓨鍦烘櫙銆?- **楠屾敹鏍囧噯**锛氱嫭绔嬪師鏂欒鑼冨寲涓?`32脳32`銆?6 鑹叉垨鏇村皯銆佺‖ alpha銆?x QA銆佽皟鑹叉澘鍜?JSON 鎶ュ憡锛沀nity 瀵煎叆 Sprite / Point / Clamp / 鏃?mipmap锛涙湭鎼滃埉鏃舵樉绀烘甯哥锛屽凡鎼滃埉鍚庢樉绀哄紑鍚銆?- **瀹屾垚鍚庤В閿?*锛氬悗缁編鏈墿鍏呯户缁竴娆″彧澶勭悊涓€涓潤鎬佸璞℃垨鍗曚綅闈欏抚锛岄粯璁や笉鍒涘缓澶氬抚鍔ㄧ敾銆?- **楠岃瘉缁撴灉锛?026-07-25锛?*锛氭湰鍦板伐浣滃彴鐙珛鐢熸垚骞惰鑼冨寲 `loot_crate_open`锛屾姤鍛?PASS锛宍32脳32`銆?6 鑹层€佺‖ alpha銆?x QA 涓庤皟鑹叉澘榻愬叏銆俇nity 瀵煎叆澶嶆牳涓?Sprite / Point / Clamp / 鏃?mipmap锛汸lay Mode 纭鎴樺埄鍝佺鍒濆 `looted=false` 浣跨敤鍏抽棴绠憋紝寮€鍚璧勬簮鍙姞杞斤紝缁樺埗閫昏緫鎸?`LootContainer.IsLooted` 閫夋嫨瀵瑰簲璐村浘銆侳unplay 缂栬瘧閿欒/璀﹀憡 0锛岄€€鍑?Play Mode 鍚?Console 鏃犻」鐩敊璇紝鍦烘櫙 `isDirty=false`锛屾湭淇濆瓨鍦烘櫙銆?
#### V2-01锛氭寮忔垬鏂?HUD 涓庣嫭绔嬪紑鍙戞帶鍒跺彴 - IN PROGRESS锛?026-07-24锛?- **褰掑睘**锛氬墽鎯呮ā寮忎笌鑲夐附妯″紡鍏辩敤鐣岄潰琛ㄧ幇銆?- **鐩爣**锛氫互 1920x1080 鐨?75% 鎴樺満 / 25% HUD 涓哄熀鍑嗭紝寤虹珛杩愯鏃舵寮忔垬鏂?HUD锛堟寚浠ゃ€佽鑹茶祫婧愩€佹妧鑳姐€佽鍔ㄦ潯銆佸揩鎹锋爮銆佷簨浠惰褰曪級锛屽苟鎶婂紑鍙戞祴璇曟搷浣滆縼绉昏嚦鍙崟鐙懠鍑虹殑鎺у埗鍙帮紝涓嶅啀浣滀负涓绘祦绋嬬殑甯搁┗瑙嗚鍐呭銆?- **娑夊強鏂囦欢/绯荤粺**锛歚CombatPrototypeBootstrap`銆佹柊鐨勮繍琛屾椂鎴樻枟 UI 琛ㄧ幇灞傘€乣TacticalHudSceneBinder`銆丏OTween锛涗笉淇敼鍦烘櫙 YAML锛屼笉瀵煎叆鏈粡 QA 鐨?AI 鍥撅紝涓嶆敼鍙樹换浣曟垬鏂楁垨鑲夐附瑙勫垯銆?- **楠屾敹鏍囧噯**锛氭寮忔垬鏂楁椂涓?HUD 鍙銆佹寜閽彲鎿嶄綔涓斾笌褰撳墠鎸囦护鍚屾锛汧1/鐣岄潰鍏ュ彛鍙紑鍏冲紑鍙戞帶鍒跺彴锛涙帶鍒跺彴鍏抽棴鏃朵笉閬尅鎴樺満鍜?HUD锛涘叆鍙ｃ€佺畝鎶ャ€佸湴鍥俱€佺粨绠椾笌杩斿洖娴佺▼淇濇寔鍙洖褰掞紱1920x1080 / 960x540 鏃犲叧閿唴瀹规孩鍑恒€?- **瀹屾垚鍚庤В閿?*锛歏2-02 鍏ㄦ祦绋嬮〉闈㈤€愰〉瑙嗚閲嶆瀯锛堝叆鍙ｃ€佸湴鍥俱€侀€夋嫨銆佸伐鍧娿€佺畝鎶ャ€佺粨绠楋級涓?V2-03 鍍忕礌璧勪骇 QA銆?- **闃舵楠岃瘉锛?026-07-24锛?*锛氭柊澧?`FormalCombatHud` 涓?`DeveloperConsolePanel`锛屾寮忔垬鏂楄繍琛屾椂鏂█纭 HUD銆佺嫭绔嬫帶鍒跺彴銆佹寚浠ら€変腑鎬佸潎瀛樺湪骞跺彲鍒囨崲锛涙棫鍦烘櫙 HUD 鍦ㄨ繍琛屾椂鍋滅敤锛岄伩鍏嶇┖缁戝畾鍣ㄩ伄鎸℃寮忕晫闈€侳unplay 閲嶇紪璇戦€氳繃锛岀紪璇戦敊璇?璀﹀憡 0锛汸lay Mode Console 鏃犳柊澧為敊璇紱鍦烘櫙 `isDirty=false`銆傛埅鍥惧伐鍏蜂粛缂撳瓨鍏ュ彛甯э紝姝ｅ紡 HUD 鍙屽垎杈ㄧ巼瑙嗚鎴浘寰呭悗缁埛鏂伴摼淇鍚庤ˉ楠屻€傚彂鐜板苟淇鏃?`UnityEngine.Input` 涓?Input System 鍐茬獊銆?- **闃舵楠岃瘉锛?026-07-24 / 鐩爣璇绘暟锛?*锛氭寮?HUD 澧炲姞鐩爣閿佸畾璇绘暟锛屾樉绀虹洰鏍囧悕绉般€佺敓鍛戒笌鎶ょ浘锛涜繍琛屾椂鏂█纭鏁屾柟鐩爣缁戝畾銆佹敾鍑绘寚浠ら€変腑鎬佸拰 F1 鎺у埗鍙板垏鎹㈠潎閫氳繃銆侳unplay 閲嶇紪璇?0 閿欒/璀﹀憡锛孋onsole 鏃犳柊澧為敊璇紱鏈繚瀛樺満鏅€?- **闃舵楠岃瘉锛?026-07-24 / 蹇嵎鏍忎笌缁撶畻锛?*锛氭寮?HUD 澧炲姞鎶€鑳?蹇嵎鏍忔Ы浣嶅拰鑳滆礋瑕嗙洊灞傦紱蹇嵎鏍忚鍙栫湡瀹?`CombatState.Quickbar`锛岀粨鏋滆鐩栧眰鎻愪緵鎴樻湳閲嶅紑涓庤繑鍥炲叆鍙ｃ€侾lay Mode 杩愯鏃舵柇瑷€纭蹇嵎鏍忛妲戒负鈥滃尰鐤楀寘鈥濄€佽儨鍒╅樁娈垫樉绀虹粨鏋滅姸鎬侊紝鎺у埗鍙颁繚鎸佸彲鍒囨崲锛汧unplay 缂栬瘧 0 閿欒/璀﹀憡锛孋onsole 鏃犳柊澧為敊璇紝鍦烘櫙 `isDirty=false`銆?
#### V2-02锛氬叏娴佺▼椤甸潰瑙嗚閲嶆瀯 - COMPLETE锛?026-07-24锛?- **褰掑睘**锛氳倝楦芥ā寮忓湴鍥句笌鍏辩敤娴佺▼鐣岄潰琛ㄧ幇銆?- **鐩爣**锛氬皢鍏ュ彛銆佽倝楦藉湴鍥俱€佽妭鐐归€夋嫨銆佸伐鍧娿€佹垬鍓嶇畝鎶ャ€佺粨绠楅〉闈㈤€愰〉缁熶竴涓烘寮忓伐涓氭帶鍒跺彴瑙嗚锛涙湰闃舵鍏堝畬鎴愬ぇ鍦板浘璧勬簮鏉′笌褰撳墠鑺傜偣璇绘暟銆?- **娑夊強鏂囦欢/绯荤粺**锛歚CombatPrototypeBootstrap` 鍦板浘杩愯鏃?IMGUI銆乣RogueliteMapRun` 鐘舵€佹暟鎹€丏OTween锛涗笉鏀瑰彉鑷敱鍥炶銆佹潈闄愰棬鎴栬祫婧愯鍒欙紝涓嶄繚瀛樺満鏅€?- **闃舵楠岃瘉锛?026-07-24 / 鍦板浘锛?*锛氬湴鍥鹃〉鏂板褰撳墠鑺傜偣銆佺瀛?绛夌骇銆侀浂浠?浠ュお/琛ョ粰/鏉冮檺鍗¤祫婧愯姱鐗囷紱Play Mode 杩愯鏃舵柇瑷€纭鏂板眬 `start`銆? 涓彲杩涘叆鑺傜偣銆? 闆朵欢涓?2 浠ュお鍧囦繚鎸佹湁鏁堛€侳unplay 缂栬瘧 0 閿欒/璀﹀憡锛孋onsole 鏃犳柊澧為敊璇紝鍦烘櫙 `isDirty=false`銆?- **闃舵楠岃瘉锛?026-07-24 / 閫夋嫨涓庡伐鍧婏級**锛氱粺涓€闈炴垬鏂楄妭鐐归€夋嫨闈㈡澘涓庨噹鎴樺伐鍧婇潰鏉跨殑瀹藉害銆佺暀鐧姐€佺姸鎬佹枃妗堝拰璧勬簮浠ｄ环锛涗笁椤归€夋嫨鎸夊彲鐢ㄥ搴﹁嚜閫傚簲锛岄伩鍏?1920x1080 鍙充晶婧㈠嚭銆侾lay Mode 鏂█纭鍦板浘鑺傜偣閫夋嫨浠嶈兘杩涘叆鐪熷疄鑺傜偣鐘舵€侊紱Funplay 缂栬瘧 0 閿欒/璀﹀憡锛孋onsole 鏃犳柊澧為敊璇紝鍦烘櫙 `isDirty=false`銆?- **闃舵楠岃瘉锛?026-07-24 / 鍏ュ彛涓庣畝鎶ワ級**锛氭垬鍓嶇畝鎶ユ敼涓烘寮忓伐涓氭帶鍒跺彴甯冨眬锛屽鍔犲墽鎯?鑲夐附涓婁笅鏂囥€佽妭鐐瑰悕绉般€佷换鍔＄洰鏍囥€佹晫鏂圭紪鎴愩€佹棤鍊掕鏃?閲嶅紑瑙勫垯銆佽倝楦借祫婧愯姱鐗囧強纭/杩斿洖鍔ㄤ綔銆侾lay Mode 鏂█纭鍓ф儏绠€鎶ヤ笌鑲夐附鑺傜偣绠€鎶ヤ笂涓嬫枃鍧囨湁鏁堬紱Funplay 缂栬瘧 0 閿欒/璀﹀憡锛孋onsole 鏃犳柊澧為敊璇紝鍦烘櫙 `isDirty=false`銆?- **闃舵楠岃瘉锛?026-07-24 / 鍏ュ彛涓庣粨绠楋級**锛氬叆鍙ｆ枃妗堟敼涓衡€滄寮忚鍔ㄥ叆鍙ｂ€濓紝鏄庣‘鍓ф儏/鑲夐附涓ゆ潯鐜╁娴佺▼锛屽紑鍙戞帶鍒跺彴鏀逛负鎴樻枟涓?F1 鍛煎嚭锛涜倝楦界粨绠楁枃妗堟槑纭鍔变笁閫変竴銆佷繚瀛樻帹杩涚姸鎬佷笌鈥滀笉鑷姩瑁呭鈥濄€侾lay Mode 鏂█閫氳繃鍓ф儏鍏ュ彛鈫掓垬鏂楃粨鏋溿€佽倝楦借妭鐐光啋鎴樻枟鑳滃埄鈫掍笁椤瑰鍔辩粨绠楋紱Funplay 缂栬瘧 0 閿欒/璀﹀憡锛孋onsole 鏃犳柊澧為敊璇紝鍦烘櫙 `isDirty=false`銆?- **闃舵楠岃瘉锛?026-07-24 / 鍏ㄦ祦绋嬪洖褰掞級**锛歅lay Mode 瀹屾垚鍓ф儏鍏ュ彛鈫掓垬鍓嶇畝鎶モ啋姝ｅ紡鎴樻枟鈫掓垬鏈噸寮€鈫掕繑鍥炲叆鍙ｏ紝浠ュ強鑲夐附鍦板浘鈫掕妭鐐规垬鏂椻啋鑳滃埄缁撶畻鈫掗鍙栧鍔扁啋杩斿洖鍦板浘锛涘鍔遍鍙栧悗鍙繘鍏ヨ妭鐐逛负 4銆?920x1080 涓?960x540 鎴浘鏂囦欢鍧囨垚鍔熺敓鎴愶紝浣嗛樁娈靛垏鎹㈡埅鍥句粛鍙兘缂撳瓨鍏ュ彛甯э紝瑙嗚闃舵璇佹嵁浠ヨ繍琛屾椂鐘舵€佹柇瑷€涓轰富銆侳unplay 缂栬瘧 0 閿欒/璀﹀憡锛汣onsole 浠?2 鏉℃棦鏈?`RenderTexture.active` 閲婃斁璀﹀憡锛涘満鏅?`isDirty=false`銆?- **涓嬩竴姝?*锛歏2-02 宸插畬鎴愶紱杩涘叆 V2-03 鍍忕礌璧勪骇 QA 鍓嶏紝鍏堢‘璁ゆ寮忚祫浜ф竻鍗曚笌浼樺厛绾с€?
#### V1-06锛氭垬鏂?澶у湴鍥?缁撶畻缁熶竴瑙嗚 QA - COMPLETE锛?026-07-24锛?- **褰掑睘**锛氬墽鎯呮ā寮忎笌鑲夐附妯″紡鍏辩敤鎴樻枟琛ㄧ幇銆?- **鐩爣**锛氬 V1-04銆乂1-05銆丮1-01銆丮1-02 涓庢棦鏈夌粨绠楃晫闈㈡墽琛屽悓涓€杞繍琛屾椂娴佺▼銆佸弻鍒嗚鲸鐜囥€佺紪璇戙€丆onsole 鍜岃仛鐒?EditMode 楠屾敹銆?- **娑夊強鏂囦欢/绯荤粺**锛歚CombatVisualFeedback`銆乣RogueliteSettlementPresentation`銆乣TacticalHudSceneBinder`銆乣CombatPrototypeBootstrap`銆乣RogueliteMapRun`锛涙湰娆′粎鏇存柊鏈緟鍔為獙璇佽褰曪紝涓嶄慨鏀瑰満鏅?YAML 鎴栫帺娉曡鍒欍€?- **楠屾敹鏍囧噯**锛氬紑鍙戣彍鍗曘€佺畝鎶ャ€佹垬鏂椼€佹敾鍑?鐘舵€?鍙牬鍧忕墿鍙嶉銆佹垬鏈噸寮€銆佽繑鍥炲叆鍙ｃ€佸湴鍥捐妭鐐广€佽儨鍒╃粨绠椾笌濂栧姳棰嗗彇鍙洖褰掞紱1920x1080 鍜?960x540 涓嶆孩鍑猴紱缂栬瘧/Console 鏃犻」鐩敊璇紱鍦烘櫙涓?dirty銆?- **楠岃瘉缁撴灉锛?026-07-24锛?*锛歅lay Mode 杩愯鏃舵柇瑷€閫氳繃鍓ф儏鍏ュ彛鈫掓垬鍓嶇畝鎶モ啋姝ｅ紡鎴樻枟鈫掓敾鍑?鐘舵€?鍙牬鍧忕墿 DOTween 鍙嶉鈫掓垬鏈噸寮€鈫掕繑鍥炲叆鍙ｏ紱鑲夐附鍦板浘鏂板眬鈫掗搧璺贰閫烩啋姝ｅ紡鎴樻枟鈫掕儨鍒┾啋涓夐€変竴濂栧姳棰嗗彇鈫掕繑鍥炲湴鍥句篃閫氳繃锛屽鍔遍鍙栧悗鍙繘鍏ヨ妭鐐逛负 4銆俙Camera.main`銆乣CombatVisualFeedback` 涓?`RogueliteSettlementPresentation` 鍧囧瓨鍦紱鍙嶉灞傝繍琛屾椂鐢熸垚鏀诲嚮銆佺噧鐑т笌鍙牬鍧忕墿鎻愮ず瀵硅薄銆傝仛鐒?EditMode 5/5 閫氳繃锛歚FireBolt_AppliesBurningAndCooldown_Deterministically`銆乣BoundUnit_CannotMove`銆佸湴鍥捐瑙夌姸鎬併€佸晢搴?宸ュ潑鎸佷箙鍖栥€佽法娴佹淳濂栧姳銆侳unplay 缂栬瘧閿欒/璀﹀憡 0锛汣onsole 浠?2 鏉℃棦鏈?`RenderTexture.active` 閲婃斁璀﹀憡锛屾棤椤圭洰閿欒锛沗Assets/Scenes/CombatPrototype.unity` 涓?`isDirty=false`銆?920x1080 涓?960x540 鎴浘瀹為檯鍙銆佹棤婧㈠嚭涓旇彍鍗曟枃瀛楀彲璇伙紱鎴浘宸ュ叿鍦ㄩ樁娈靛垏鎹㈠悗浠嶇紦瀛樺叆鍙ｅ抚锛屽洜姝ゅ湴鍥?鎴樻枟闃舵浠ョ湡瀹炶繍琛屾椂鐘舵€佹柇瑷€浣滀负楠屾敹渚濇嵁銆侷MGUI 涓嶆彁渚?uGUI 灏勭嚎鍛戒腑锛屾棤娉曠敤 `simulate_mouse_click` 鍗曠嫭璇佹槑鎺т欢鍛戒腑锛屼絾娴佺▼鍏紑鏂规硶涓庣姸鎬佸畬鎴愰獙璇佸潎閫氳繃銆?- **瀹屾垚鍚庤В閿?*锛歊3-01 浜嬩欢姹犳墿瀹逛笌鍥炶閫掑噺銆丷3-02 鏋勭瓚/瑁呭鍐呭姹犮€丷3-03 绗簩鍦板尯涓庢晫浜虹編鏈祫浜?QA锛涢渶鐢变骇鍝佺‘璁や紭鍏堢骇鍚庡啀寮€濮嬨€?- **褰掑睘**锛氬墽鎯呮ā寮忎笌鑲夐附妯″紡鍏辩敤鎴樻枟琛ㄧ幇銆?- **V1-04 鐩爣**锛氫负涓户鍣ㄣ€佹帺浣撶瓑鍙牬鍧忕墿鎻愪緵鍙楁崯/鎽ф瘉鍙嶉锛屽苟寤虹珛鐕冪儳銆佹潫缂氥€佺牬鐢层€佹姢鐩剧瓑鐘舵€佺殑缁熶竴瑙嗚璇箟銆?- **娑夊強鏂囦欢/绯荤粺**锛歚CombatVisualFeedback`銆乣CombatPrototypeBootstrap`銆佸彲鐮村潖鐗╄繍琛屾椂琛ㄧ幇銆佺姸鎬佹晥鏋滆〃鐜帮紱涓嶇洿鎺ヤ慨鏀瑰満鏅?YAML銆?- **楠屾敹鏍囧噯**锛氬彈鎹熶笌鎽ф瘉鐘舵€佸彲璇伙紝鐘舵€侀鑹蹭笌 HUD 璇箟涓€鑷达紝琛ㄧ幇鐢?DOTween 椹卞姩涓斾笉閬尅 HUD锛涘畬鎴愬悗瑙ｉ攣 V1-05銆?- **楠岃瘉缁撴灉锛?026-07-24锛?*锛歚CombatVisualFeedback` 鐜颁负鎺╀綋/涓户鍣ㄦ寜鑰愪箙鍙樺寲鏄剧ず鍙楁崯鎴栨懅姣佽剦鍐层€佹诞瀛椾笌鍑荤牬鎻愮ず锛涚噧鐑э紙閿堢孩锛夈€佹潫缂氾紙鍐烽潚锛夈€佺牬鐢诧紙瀹夊叏榛勶級鍜岀紦鎱紙鐏扮豢锛夊湪鏂藉姞鏃舵樉绀?DOTween 鑴夊啿/鏍囩锛屽苟鍦ㄥ崟浣嶆牸搴曡竟淇濈暀涓嶉伄鎸′富浣撶殑棰滆壊鏍囪銆侾lay Mode 杩愯鏃堕獙璇佽交/閲嶆帺浣撲笌鐩爣涓户鍣ㄣ€佸洓绉嶇姸鎬佸潎鐢熸垚鍙鍙嶉锛? 椤规爣绛撅級锛涜仛鐒?EditMode 3/3 閫氳繃锛汧unplay 缂栬瘧閿欒/璀﹀憡 0锛孋onsole 鏃犳柊澧為敊璇紝浠呬袱椤规棦鏈?`FindFirstObjectByType` 寮冪敤璀﹀憡锛涘満鏅?`isDirty=false`銆?920脳1080 / 960脳540 鎴浘鏂囦欢鍙敓鎴愪絾浠嶈繑鍥為檲鏃у紑鍙戣彍鍗曞抚锛岃褰曚负鎴浘鍒锋柊闄愬埗锛屼笉浣滀负瑙嗚楠屾敹渚濇嵁銆?- **涓嬩竴姝?*锛歏1-05 鎴樻枟 UI 缁勪欢涓庣姸鎬佽鑼冦€?
#### V1-05锛氭垬鏂?UI 缁勪欢涓庣姸鎬佽鑼?- COMPLETE锛?026-07-24锛?- **褰掑睘**锛氬墽鎯呮ā寮忎笌鑲夐附妯″紡鍏辩敤鎴樻枟琛ㄧ幇銆?- **鐩爣**锛氳鍦烘櫙鍖栨垬鏈?HUD 鏄庣‘鍛堢幇褰撳墠鎸囦护銆佽鍔ㄥ崟浣嶃€佽祫婧愬彉鍖栥€佸崟浣嶇姸鎬佸拰琛屽姩鏉′紭鍏堢骇锛屽苟灏嗘棦瀹氬喎闈?閿堢孩/瀹夊叏榛?鐏扮豢璇箟鍚屾鍒?HUD锛屼繚鎸?75% 鎴樺満 / 25% HUD 鐨勭幇鏈夌粨鏋勩€?- **娑夊強鏂囦欢/绯荤粺**锛歚TacticalHudSceneBinder`銆乣CombatPrototypeBootstrap`銆佺幇鏈夊満鏅?HUD 缁戝畾涓?DOTween锛涗笉淇濆瓨鍦烘櫙鎴栦慨鏀瑰満鏅?YAML銆?- **楠屾敹鏍囧噯**锛氶€夋嫨鎸囦护鏈夋寔缁彲璇荤殑閫変腑鐘舵€侊紱鐘舵€佸悕绉颁娇鐢ㄤ腑鏂囦笖棰滆壊涓€鑷达紱鐢熷懡/鎶ょ浘/浠ュお鍙樺姩鏈夊厠鍒剁殑 DOTween 鍙嶉锛涜鍔ㄦ潯鏍囪瘑褰撳墠琛屽姩鍗曚綅锛涜繍琛屾椂涓嶉伄鎸″湴鍥句笌 HUD锛屼笉寮曞叆鏂拌鍒欍€傚畬鎴愬悗瑙ｉ攣 M1-01銆?- **楠岃瘉缁撴灉锛?026-07-24锛?*锛氬満鏅寲 HUD 鐜板皢鏀诲嚮绛夊綋鍓嶆寚浠や繚鎸佷负鍐烽潚閫変腑鎬侊紱琛屽姩鏉′娇鐢?`鈻禶 涓庡喎闈掓爣璇嗗綋鍓嶈鍔ㄥ崟浣嶏紱涓绘墜鐘舵€佹樉绀虹噧鐑э紙閿堢孩锛夈€佹潫缂氾紙鍐烽潚锛夈€佺牬鐢诧紙瀹夊叏榛勶級銆佺紦鎱紙鐏扮豢锛夌殑鐪熷疄涓枃鏍囩銆傜敓鍛?鎶ょ浘/浠ュお鏉″彉鍖栦娇鐢?DOTween 濉厖杩囨浮涓庤交寰缉鏀惧弽棣堛€侾lay Mode 杩愯鏃舵柇瑷€纭鎸囦护銆佸洓鑹茬姸鎬併€佽鍔ㄦ潯鍜岀敓鍛芥潯 Tween 鍧囩敓鏁堬紱鑱氱劍 EditMode 3/3 閫氳繃锛汧unplay 缂栬瘧閿欒/璀﹀憡 0锛孋onsole 鏃犳柊澧為敊璇紝浠呮棦鏈?`FindFirstObjectByType` 寮冪敤璀﹀憡锛涘満鏅湭淇濆瓨涓?`isDirty=false`銆?920脳1080 鎴浘浠嶅埛鏂颁负闄堟棫寮€鍙戣彍鍗曞抚锛岃褰曚负楠岃瘉闄愬埗銆?- **涓嬩竴姝?*锛歁1-01 澶у湴鍥捐妭鐐圭姸鎬佷笌璺緞鏁版嵁銆?
#### V1-Menu锛氬紑鍙戣彍鍗曡瑙夋洿鏂?- COMPLETE锛?026-07-24锛?- **褰掑睘**锛氬墽鎯呮ā寮忎笌鑲夐附妯″紡鍏辩敤鍏ュ彛銆?- **鐩爣**锛氭妸寮€鍙戞祴璇曡彍鍗曟洿鏂颁负涓庢垬鏈?HUD 涓€鑷寸殑鏋佺畝宸ヤ笟鎺у埗鍙帮紝娓呮鍖哄垎鍓ф儏琛屽姩涓庤倝楦藉尯鍩熷叆鍙ｏ紝涓嶆敼鍙樼幇鏈夋祦绋嬫垨瑙勫垯銆?- **娑夊強鏂囦欢/绯荤粺**锛歚CombatPrototypeBootstrap` 杩愯鏃?IMGUI銆丏OTween锛涗笉淇敼鍦烘櫙 YAML銆?- **楠屾敹鏍囧噯**锛氬叆鍙ｆ枃妗堛€佷换鍔℃憳瑕佸拰璺緞璇存槑娓呮櫚锛涘墽鎯?鑲夐附鎸夐挳璇箟鑹插彲鍖哄垎涓旇兘瀹為檯杩涘叆鍘熸祦绋嬶紱1920脳1080 涓?960脳540 涓嶆孩鍑猴紱瀹屾垚鍚庡洖鍒?M1-01銆?- **楠岃瘉缁撴灉锛?026-07-24锛?*锛氳繍琛屾椂鑿滃崟鏇存柊涓烘繁鑹叉瀬绠€宸ヤ笟鎺у埗鍙帮紝浣跨敤鍐烽潚鈥滃墽鎯呰鍔ㄢ€濆拰瀹夊叏榛勨€滆倝楦藉尯鍩熲€濅袱寮犱俊鎭崱銆佸搴斿ぇ鍏ュ彛涓庡畬鏁存祦绋嬫彁绀猴紱浠?DOTween 瀹屾垚鍏嬪埗鐨勫叆鍦洪€忔槑搴?缂╂斁銆侳unplay Game View 鍦?1920脳1080 鍜?960脳540 鍧囧彲瑙併€佹棤鏂囧瓧閲嶅彔鎴栨孩鍑猴紱杩愯鏃堕獙璇佸墽鎯呯畝鎶ヤ笌鑲夐附鏂板眬鍏ュ彛鍧囧彲鐢紙鏂板眬淇濇寔 4 闆朵欢/2 浠ュお锛夈€侷MGUI 涓嶆毚闇?uGUI 灏勭嚎鍛戒腑锛宍simulate_mouse_click` 鏈懡涓睘浜庢棦鏈夊伐鍏烽檺鍒讹紱缂栬瘧閿欒/璀﹀憡 0锛孋onsole 鏃犳柊澧為敊璇紝鍦烘櫙鏈繚瀛樸€?- **涓嬩竴姝?*锛歁1-01 澶у湴鍥捐妭鐐圭姸鎬佷笌璺緞鏁版嵁銆?
#### V1-FullFlow锛氬叏娴佺▼鐣岄潰缁熶竴鏇存柊 - COMPLETE锛?026-07-24锛?- **褰掑睘**锛氬墽鎯呮ā寮忎笌鑲夐附妯″紡鍏辩敤娴佺▼琛ㄧ幇銆?- **鐩爣**锛氬皢寮€鍙戝叆鍙ｃ€佽倝楦介厤缃€佹帹杩涘湴鍥俱€佽妭鐐?宸ュ潑/濂栧姳閫夋嫨銆佺煭灞€浜嬩欢銆佹垬鍓嶇畝鎶ヤ笌鎴樻枟娴佺▼鏍忕粺涓€涓烘瀬绠€宸ヤ笟鎺у埗鍙拌瑙夛紝閬垮厤鍙湁棣栧眰鑿滃崟鏇存柊銆?- **娑夊強鏂囦欢/绯荤粺**锛歚CombatPrototypeBootstrap` 鐨勮繍琛屾椂 IMGUI 涓?DOTween锛涗笉淇敼鍦烘櫙 YAML 鎴栦换浣曠帺娉曡鍒欍€?- **楠屾敹鏍囧噯**锛氬悇闃舵鍧囦娇鐢ㄤ竴鑷撮潰鏉?鎸夐挳/鐘舵€佽壊锛涘墽鎯呫€佽倝楦藉拰杩斿洖璺緞鍙洖褰掞紱1920脳1080 / 960脳540 鏃犳孩鍑猴紱瀹屾垚鍚庢仮澶?M1-01銆?- **楠岃瘉缁撴灉锛?026-07-24锛?*锛氱粺涓€鐨勮繍琛屾椂鎺у埗鍙伴潰鏉?鎸夐挳宸茶鐩栬倝楦介厤缃€佸尯鍩熷湴鍥俱€佽妭鐐圭姸鎬併€佷簨浠堕€夋嫨銆佸伐鍧婅澶?鏍″噯銆佷笁閫変竴濂栧姳銆佺煭灞€浜嬩欢銆佹垬鍓嶇畝鎶ヤ笌鎴樻枟娴佺▼鏍忥紱鍐烽潚鐢ㄤ簬鍓ф儏/鍙墽琛屾搷浣滐紝瀹夊叏榛勭敤浜庤倝楦芥帹杩?濂栧姳锛岄攬绾粎鐢ㄤ簬鍗遍櫓鎴栧垹闄ゃ€侾lay Mode 鍥炲綊閫氳繃鍦板浘鈫掑唴瀹归€夋嫨鈫掓垬鏂楀鍔扁啋宸ュ潑鈫掔畝鎶モ啋姝ｅ紡鎴樻枟鈫掓垬鏈噸寮€锛涜仛鐒?EditMode 4/4 閫氳繃銆侳unplay 缂栬瘧閿欒/璀﹀憡 0锛孋onsole 鏃犳柊澧為敊璇紝鍦烘櫙鏈繚瀛樹笖 `isDirty=false`銆傛埅鍥惧伐鍏峰湪闃舵鍒囨崲鍚庝粛杩斿洖缂撳瓨鑿滃崟鐢婚潰锛岃褰曚负楠岃瘉闄愬埗锛岃瑙夊竷灞€浠ュ凡楠岃瘉鐨勮彍鍗曞弻鍒嗚鲸鐜囨埅鍥句笌杩愯鏃舵祦绋嬬姸鎬佷负璇佹嵁銆?- **涓嬩竴姝?*锛歁1-01 澶у湴鍥捐妭鐐圭姸鎬佷笌璺緞鏁版嵁銆?
#### M1-01锛氬ぇ鍦板浘鑺傜偣鐘舵€佷笌璺緞鏁版嵁 - COMPLETE锛?026-07-24锛?- **褰掑睘**锛氳倝楦芥ā寮忓ぇ鍦板浘琛ㄧ幇銆?- **鐩爣**锛氬皢鑷敱鍥炶缃戠粶鐨勫綋鍓嶃€佸彲杩涘叆銆佹潈闄愰棬銆佸凡娓呯悊銆佸凡璁块棶銆佸凡鐭ユ湭鎺㈢储涓庢湭鐭ョ姸鎬佹敹鏁涗负鍙祴璇曠殑鏁版嵁鎺ュ彛锛屽苟椹卞姩鑺傜偣鍙婅矾寰勭殑缁熶竴瑙嗚鐘舵€併€?- **娑夊強鏂囦欢/绯荤粺**锛歚RogueliteMapRun`銆乣RogueliteDeveloperRunTests`銆乣CombatPrototypeBootstrap` 鍦板浘杩愯鏃?IMGUI锛涗笉淇敼鍦烘櫙 YAML銆?- **楠屾敹鏍囧噯**锛氱姸鎬佷笌鏃㈡湁鐩搁偦/鏉冮檺鍗¤鍒欎竴鑷达紝宸叉竻鐞嗘埧闂村彲璇讳笖瀹夊叏锛屾湭鐭ユ埧闂翠繚鎸佹ā绯婏紝璺緞鏄惧紡鍖哄垎鍙蛋/宸叉帰绱?鏉冮檺鍙楅檺锛涚姸鎬侀殢鐙珛瀛樻。鎭㈠銆傚畬鎴愬悗瑙ｉ攣 M1-02銆?- **楠岃瘉缁撴灉锛?026-07-24锛?*锛氭柊澧?`RogueliteMapNodeVisualState` 涓?`VisualStateFor`/`AvailableNodes` 鏁版嵁鎺ュ彛锛岀粺涓€杈撳嚭褰撳墠浣嶇疆銆佸彲杩涘叆銆佹潈闄愰棬銆佸凡娓呯悊銆佸凡璁块棶銆佸凡鐭ユ湭鎺㈢储涓庢湭鐭ワ紱鍦板浘鑺傜偣鍜岃矾寰勫彧娑堣垂璇ョ姸鎬併€傚彲璧拌矾寰勪负鍐烽潚銆佸凡鎺㈢储璺緞涓虹伆缁裤€佹潈闄愬彈闄愪负閿堢孩銆佹湭鐭ヤ负浣庨ケ鍜岀伆锛涜妭鐐逛腑鏂囩被鍨?鐘舵€佷笌鏃㈡湁鐩搁偦銆佸洖璁裤€佹潈闄愬崱瑙勫垯涓€鑷淬€侾lay Mode 楠岃瘉鍏ュ彛/鍙繘鍏?鏈煡鏍稿績/鏉冮檺鍙楅檺浼犺緭濉斿強鍙繘鍏ュ垪琛紱鑱氱劍 EditMode 3/3 閫氳繃锛汧unplay 缂栬瘧閿欒/璀﹀憡 0銆丆onsole 涓虹┖銆佸満鏅湭淇濆瓨涓?`isDirty=false`銆?- **涓嬩竴姝?*锛歁1-02 宸ヤ笟鍩庡競澶у湴鍥捐瑙夊師鍨嬨€?
#### M1-02锛氬伐涓氬煄甯傚ぇ鍦板浘瑙嗚鍘熷瀷 - COMPLETE锛?026-07-24锛?- **褰掑睘**锛氳倝楦芥ā寮忓ぇ鍦板浘琛ㄧ幇銆?- **鐩爣**锛氬皢 20 鑺傜偣姝ｄ氦缃戠粶鍛堢幇涓衡€滈搧璺墠绾库€斿伐涓氱綉鏍尖€旀牳蹇冮殧绂诲尯鈥濈殑杩戜唬榄斿宸ヤ笟鍖哄煙鍥撅紝鍔犲叆鍙鐨勯搧璺€佸绠°€佸尯鍩熷眰绾у拰鐘舵€佸浘渚嬨€?- **娑夊強鏂囦欢/绯荤粺**锛歚CombatPrototypeBootstrap` 鍦板浘杩愯鏃?IMGUI銆丮1-01 鑺傜偣鐘舵€佹暟鎹€丏OTween锛涗笉浣跨敤 AI 鍥炬垨淇敼鍦烘櫙 YAML銆?- **楠屾敹鏍囧噯**锛氬尯鍩熷眰绾т笉瑕嗙洊鑺傜偣銆佽矾寰勬垨閫夋嫨闈㈡澘锛涘彲璧?宸叉帰绱?鏉冮檺闂?鏈煡鍥句緥涓庡疄闄呯姸鎬佷竴鑷达紱1920脳1080 / 960脳540 鍙锛涘墽鎯呭拰鑲夐附瑙勫垯涓嶅彉銆傚畬鎴愬悗瑙ｉ攣 V1-06銆?- **楠岃瘉缁撴灉锛?026-07-24锛?*锛氬湴鍥捐繍琛屾椂澧炲姞鈥滈搧璺墠绾库€斿伐涓氱綉鏍尖€旀牳蹇冮殧绂诲尯鈥濅笁灞傚尯鍩熷簳鍥俱€侀搧璺建鏋曘€佷互澶绠′笌鍥涢」鐘舵€佸浘渚嬶紱搴曞浘鍦ㄨ矾寰勫拰鑺傜偣涓嬫柟缁樺埗锛屼笉鏀瑰彉 M1-01 鏁版嵁鎴栨搷浣滈€昏緫銆侾lay Mode 纭鏂板眬鍦板浘鍏ュ彛鍙敤銆? 涓垵濮嬪彲杩涘叆鑺傜偣淇濇寔鏈夋晥锛涜仛鐒?EditMode 3/3 閫氳繃锛汧unplay 缂栬瘧閿欒/璀﹀憡 0銆丆onsole 鏃犳柊澧為敊璇€佸満鏅?`isDirty=false`銆?920脳1080 / 960脳540 鎴浘鏂囦欢鍙敓鎴愪絾浠嶇紦瀛樺紑鍙戣彍鍗曞抚锛岃褰曚负鎴浘鍒锋柊闄愬埗銆?- **涓嬩竴姝?*锛歏1-06 鎴樻枟/澶у湴鍥?缁撶畻缁熶竴瑙嗚 QA銆?
#### V1-03锛氭敾鍑绘簮/鍛戒腑鐩爣/鍑荤牬鍦板浘鍐呰瑙夊弽棣?- COMPLETE锛?026-07-23锛?- **鐩爣**锛氫负鏀诲嚮銆佹妧鑳姐€佸懡涓€佸嚮鐮村拰鐩爣閫夋嫨鎻愪緵涓嶉伄鎸?HUD 鐨勮繍琛屾椂瑙嗚鍙嶉銆?- **娑夊強鏂囦欢**锛歚UnityProject/Assets/Game/Runtime/Presentation/CombatVisualFeedback.cs`銆乣UnityProject/Assets/Game/Runtime/Presentation/CombatPrototypeBootstrap.cs`銆?- **楠屾敹缁撴灉**锛氭敾鍑绘簮涓庣洰鏍囨牸 DOTween 鑴夊啿銆侀€変腑鐩爣榛勮壊杞粨銆佷激瀹虫暟瀛楅敋瀹氬湴鍥炬牸銆佸嚮鐮存彁绀哄潎宸叉帴鍏ワ紱鍑荤牬鍗曚綅鍥?`IsAlive` 杩囨护涓嶅啀缁樺埗琛屽姩鏉?鎰忓浘/浜や簰鐘舵€侊紱鏈慨鏀瑰満鏅?YAML銆?- **楠岃瘉**锛欶unplay 閲嶇紪璇戝畬鎴愶紱缂栬瘧閿欒/璀﹀憡 0銆侾lay Mode 宸茶繘鍏ユ垬鏂楀苟纭 `IsDeveloperCombatActive=True`锛涜繍琛屾椂璋冪敤 `NotifyAttack` 鎴愬姛鐢熸垚鏀诲嚮/鍛戒腑鍙嶉瀵硅薄锛? 涓級鍙婂嚮鐮村弽棣堝璞★紙8 涓級锛?920脳1080 涓?960脳540 鎴浘鍧囨垚鍔熺敓鎴愩€侰onsole 鏃犳柊澧炶繍琛屾椂閿欒锛屼粎鏃㈡湁 `FindFirstObjectByType` 寮冪敤璀﹀憡銆?- **涓嬩竴姝?*锛氱户缁墿灞曞悓涓€琛ㄧ幇灞傜殑鍙牬鍧忕墿鍙楁崯/鎽ф瘉鍙嶉锛涗笉寮曞叆鏂版垬鏂楄鍒欍€?
- **褰掑睘**锛氳倝楦芥ā寮忎笌鍏辩敤鎴樻枟琛ㄧ幇銆?- **鐩爣**锛氬缓绔嬪彲鎸佺画杩唬鐨勫畬鎴愬搧琛ㄧ幇鍩虹嚎锛欴OTween 椹卞姩鐨勭晫闈?鎴樻枟鍙嶉銆佸畬鏁磋儨璐熶笌濂栧姳缁撶畻 UI銆佹垬鏂椾俊鎭樉绀恒€佽鑹?鑳屾櫙/鍦板潡璧勬簮鐩綍鍙婄鍚堣鑼冪殑棣栨壒鍍忕礌璧勬簮銆?- **娑夊強鏂囦欢/绯荤粺**锛欴OTween 鍖呬笌鍒濆鍖栥€丆anvas 缁撶畻灞傘€佹垬鏂楀懡涓?浼ゅ/鐩爣鍙嶉銆丠UD 涓庡湴鍥炬帹杩涗俊鎭€乣64x64` 鍗曚綅鍙?`32x32` 鍦板潡/鍥炬爣璧勬簮銆佸鍏?QA銆丳lay Mode 鎴浘楠岃瘉銆?- **楠屾敹鏍囧噯**锛氱粨绠椼€佸鍔便€佸湴鍥炬帹杩涘拰鎴樻枟 HUD 閮藉叿澶囨竻鏅扮殑淇℃伅灞傜骇涓庡叆鍦?閫€鍦?閫夋嫨鍔ㄧ敾锛涙敾鍑汇€佸懡涓€佺洰鏍囩牬鍧忓拰鑳滆礋鎻愪緵鍙鍙嶉锛涜鑹层€佽儗鏅拰鍦板潡浣跨敤鍙拷韪殑鏈湴姝ｅ紡璧勬簮鎴栧悎瑙勫崰浣嶈祫婧愶紝灏哄銆侀€忔槑杈圭晫銆侀敋鐐广€佸熀绾垮拰 Point filter 閫氳繃 QA锛?920脳1080 涓?960脳540 鏃犳枃瀛楅伄鎸℃垨鍏冪礌婧㈠嚭銆?- **瀹屾垚鍚庤В閿?*锛氬熀浜庡悓涓€琛ㄧ幇灞傛寔缁墿灞曟晫浜恒€佸湴鍥惧彉浣撱€佷簨浠跺拰濂栧姳鍐呭銆?
### R1锛氳倝楦借妭鐐瑰湴鍥句笌鎴樻枟缁撶畻鎴愰暱寰幆 - COMPLETE

- **褰掑睘**锛氳倝楦芥ā寮忋€?- **鐩爣**锛氬畬鎴愮帺瀹舵帹杩涘崟灞€娴佺▼鐨勮妭鐐瑰湴鍥撅紝浠ュ強鎴樻枟鎴愬姛鍚庣殑瑙掕壊鍗囩骇涓庘€滄硶鏈?姝﹀櫒娣峰悎姹犻殢鏈轰笁閫変竴鈥濈粨绠楋紱濂栧姳閫夋嫨蹇呴』鐪熷疄杩涘叆鍚庣画鎴樻枟鏋勭瓚銆?- **娑夊強鏂囦欢/绯荤粺**锛氳倝楦藉湴鍥捐妭鐐圭姸鎬?鐙珛瀛樻。銆佸崟灞€绉嶅瓙闅忔満搴忓垪銆佹垬鏂楃粨绠椼€佺粡楠?绛夌骇銆佹硶鏈笌姝﹀櫒濂栧姳鐩綍銆佹瀯绛戞敞鍏ャ€佸湴鍥句笌缁撶畻 UI銆丒ditMode/Play Mode 楠岃瘉銆?- **楠屾敹鏍囧噯**锛氬湴鍥炬湁璧风偣銆佷袱鏉″彲閫夋帹杩涜矾寰勩€佹垬鏂楄妭鐐瑰拰缁堢偣锛涗粎宸茶В閿佽妭鐐瑰彲杩涘叆锛涙垬鏂楄儨鍒╁洖鍒扮粨绠楀苟瑙ｉ攣鍚庣画鑺傜偣锛涚粨绠楁樉绀哄崌绾х姸鎬佷笌 3 涓殢鏈哄€欓€夛紝鍊欓€夋潵鑷硶鏈拰姝﹀櫒姹犮€佷笁閫変竴涓斿彲澶嶇幇锛涢€変腑鍚庝笅涓€鎴樺疄闄呰澶囧搴旀硶鏈?姝﹀櫒锛涘け璐ャ€侀噸寮€鍜屽墽鎯呭瓨妗ｉ殧绂绘纭紱鏃犳椂闂村帇鍔涜鍒欍€?- **瀹屾垚鍚庤В閿?*锛氭墿灞曡妭鐐圭被鍨嬨€佷簨浠舵睜銆佸鍔辩█鏈夊害銆佸湴鍥惧彉浣撲笌鏇村鍏冲崱銆?
### R0锛氭渶鐭畬鏁磋倝楦借凯浠ｅ熀绾?- COMPLETE

- **褰掑睘**锛氳倝楦芥ā寮忋€?- **鐩爣**锛氫互瀹屾垚鍝佹爣鍑嗗缓绔嬪彲鍙嶅杩唬鐨勬渶鐭倝楦介棴鐜細绗竴鍏?鈫?鍗曟浜嬩欢 鈫?鍗曟鏀惰幏 鈫?瑙掕壊鍗囩骇 鈫?绗簩鍏?鈫?缁撶畻锛涙瘡涓€闃舵鍙銆佸彲閫夈€佸彲鎭㈠涓旂湡瀹炲奖鍝嶅悗缁垬鏂椼€?- **娑夊強鏂囦欢/绯荤粺**锛氳倝楦借繍琛岀姸鎬?鐙珛瀛樻。銆佷袱鍏充换鍔＄洰褰曘€佷簨浠?鏀惰幏/鍗囩骇鏁版嵁銆佹垬鏂楁瀯绛戞敞鍏ャ€佸紑鍙戣彍鍗曚笌缁撶畻鐣岄潰銆丒ditMode/Play Mode 楠岃瘉銆?- **楠屾敹鏍囧噯**锛氭柊寮€鍥哄畾涓轰袱鍏筹紱绗竴鍏宠儨鍒╁悗渚濆簭杩涘叆浜嬩欢銆佹敹鑾蜂笌鍗囩骇锛屼笁椤归€夋嫨鍚勬湁鏄庣‘鏁堟灉涓斿崌绾у奖鍝嶇浜屽叧瀹為檯鏋勭瓚锛涗腑閫旈€€鍑哄悗鍙户缁紱绗簩鍏崇粨绠楁竻妤氭眹鎬婚€夋嫨锛涘け璐ャ€侀噸寮€鍜屽墽鎯呭瓨妗ｉ殧绂讳繚鎸佹纭紱鏃犱换浣曟椂闂村帇鍔涜鍒欍€?- **瀹屾垚鍚庤В閿?*锛氬洿缁曡闂幆鎸佺画杩唬鏁板€笺€佷簨浠躲€佸鍔便€佸湴鍥惧彉浣撲笌鏇村鍏冲崱锛岃€屼笉鍙﹁捣瀛ょ珛鍘熷瀷銆?
### P3锛氳倝楦芥晠浜嬪寘涓庝换鍔℃ā鏉垮紑鍙戣彍鍗曞叆鍙?- COMPLETE

- **褰掑睘**锛氳倝楦芥ā寮忋€?- **鐩爣**锛氫氦浠樺彲鍏紑璇曠帺鐨勮倝楦藉墠搴忕増锛氬苟鍒楀墽鎯?鑲夐附娴嬭瘯鍏ュ彛锛涜倝楦芥彁渚涘浐瀹氫笁浠诲姟鏁呬簨閾惧拰姝肩伃/鐮村潖妯℃澘娌欑洅锛屽苟娉ㄥ叆鐜版湁绠€鎶ャ€佹垬鏂椼€佺粨绠椼€侀噸寮€鍜岃繑鍥炴祦绋嬨€?- **娑夊強鏂囦欢/绯荤粺**锛歚RogueliteStoryPackage`銆佷换鍔℃ā鏉跨洰褰曘€佺嫭绔嬭倝楦芥寔涔呭寲銆乣CombatFlowController`銆乣CombatPrototypeBootstrap`銆丒ditMode/Play Mode 楠岃瘉銆?- **楠屾敹鏍囧噯**锛氳倝楦藉瓨妗ｆ敮鎸佹柊寮€/缁х画/鍒犻櫎锛屼笖涓庡墽鎯呯姸鎬侀殧绂伙紱姝肩伃涓庣牬鍧忓悇鏈夌湡瀹炵洰鏍囥€佽儨璐熷垽瀹氫笌缁撶畻锛涙晠浜嬮摼鑳滃埄杩涘叆涓嬩竴绠€鎶ャ€佸け璐ュ彲閲嶅紑鎴栬繑鍥炶彍鍗曪紱鏃犲€掕鏃讹紱1920脳1080 楠屾敹鏃犱腑鏂囬伄鎸°€?- **瀹屾垚鍚庤В閿?*锛氳倝楦藉畬鏁存祦绋嬩笌鎵€闇€绯荤粺銆?
### P1-06锛氬満鏅寲鎴樻湳 HUD 涓庡浐瀹氬儚绱犲浘鏍?- COMPLETE

- **褰掑睘**锛氬墽鎯呮ā寮忎笌鑲夐附妯″紡鍏辩敤銆?- **鐩爣**锛氬皢鎴樻湳 HUD 鐨勮瑙夌粨鏋勩€佹寜閽拰 `32x32` 鍍忕礌鍥炬爣浠庤繍琛屾椂缁樺埗杩佺Щ涓?`CombatPrototype` 鍦烘櫙涓殑鍥哄畾 Canvas 灞傜骇涓庤祫浜э紝鑴氭湰鍙繘琛岀姸鎬佺粦瀹氬拰浜や簰杞彂銆?- **娑夊強鏂囦欢/绯荤粺**锛歚CombatPrototype.unity`銆乣TacticalHudSceneBinder`銆乣Icons32` 鍥炬爣璧勪骇銆丳lay Mode 楠岃瘉銆?- **楠屾敹鏍囧噯**锛?  - `鍦烘櫙UI/鎴樻湳HUD` 淇濆瓨瀹屾暣鐨勭姸鎬併€佹寚浠ゃ€佸揩鎹锋爮銆佹瀯绛?鍥炲悎銆佽鍔ㄦ潯鍜岃褰曞眰绾э紱Canvas 涓婃湁鍥哄畾 `TacticalHudSceneBinder`銆?  - 6 鏋氭寚浠ゅ浘鏍囦綔涓?`Assets/Game/Art/UI/Icons32/` 鐨?`32x32`銆丳oint filter 璧勪骇淇濆瓨锛涗笉浣跨敤 AI 鍥惧浘鏍囨垨鏂囧瓧銆?  - 淇濆瓨鐨勨€滄敾鍑烩€濆満鏅寜閽彲鍦?Play Mode 鍒囨崲瀹為檯鎴樻湳閫夋嫨锛涙垬鏂楀紑濮嬫椂 HUD 鍚屽抚鏄剧ず銆?  - Funplay 缂栬瘧閿欒涓?0銆丆onsole 鏃犻」鐩敊璇紱鍦烘櫙淇濆瓨鍚庨€氳繃 Git 璁板綍銆?- **瀹屾垚鍚庤В閿?*锛歅3 鑲夐附鏁呬簨鍖呬笌浠诲姟妯℃澘寮€鍙戣彍鍗曞叆鍙ｃ€?
## 新一轮开发顺序（2026-08-09）

1. `COMBAT-CLARITY-01`：重构战斗信息、反馈和失败结算。
2. `SAVE-INTEGRITY-01`：增加 `map9` 语义不变量、持久坏档备份和写保护。
3. `STARTER-BUILD-01`：冻结通用初始武器 ID，统一奖励过滤与实际关卡构建；开始前需要产品确认正式武器。
4. `SOURCE-OF-TRUTH-01`：收敛剧情探索无时间压力与入学期技术基线，标记废止旧文档。

以下为历史“接下来”快照，相关功能已被后续任务覆盖，不作为当前排期：

## 鎺ヤ笅鏉?
### P3锛氳倝楦芥晠浜嬪寘涓庝换鍔℃ā鏉垮紑鍙戣彍鍗曞叆鍙?
- **褰掑睘**锛氳倝楦芥ā寮忋€?- **鍓嶇疆**锛歅0-05 瀹屾垚銆?- **鐩爣**锛氳鍥哄畾绉嶅瓙鏁呬簨鍖呭強鍏被浠诲姟妯℃澘鍙互浠庡紑鍙戣彍鍗曢€夋嫨骞惰繘鍏ュ叡浜垬鏂楁祦绋嬨€?- **楠屾敹鏍囧噯**锛氶€夋嫨鍏ュ彛涓嶆薄鏌撳墽鎯呮ā寮忓瓨妗ｏ紱浠诲姟鐩爣鍜岀粨绠楁憳瑕佹潵鑷暟鎹畾涔夛紱鏃犳椂闂村帇鍔涘瓧娈点€?- **瀹屾垚鍚庤В閿?*锛氬紑鍙戝瀭鐩村垏鐗囨墿灞曟祴璇曘€?
### P0-03锛氬伐涓氬煄甯傚尯鍩熷惊鐜師鍨?
- **褰掑睘**锛氬墽鎯呮ā寮忋€?- **鍓嶇疆**锛歅0-02 瀹屾垚銆?- **鐩爣**锛氶獙璇佸尯鍩熷湴鍥鹃€夌偣銆佸叧閿湴鐐硅嚜鐢辨帰绱€侀殣钘忓湴鐐瑰彂鐜颁笌宸插彂鐜板湴鐐瑰揩閫熸梾琛屻€?- **楠屾敹鏍囧噯**锛氭嵁鐐规湇鍔″叆鍙ｃ€佸叕寮€/闅愯棌鍦扮偣銆佸揩鎹疯矾绾垮拰涓€鏉″彲鍥炶鏀嚎鍧囧彲楠岃瘉锛涙棤鏃堕棿娑堣€楁垨鏁屾儏鍊掕鏃躲€?
### P0-04锛氬墽鎯呮ā寮忓瓨妗ｃ€佽妗ｄ笌浠诲姟鍑嗗娴佺▼

- **褰掑睘**锛氬墽鎯呮ā寮忋€?- **鍓嶇疆**锛歅0-03 瀹屾垚銆?- **鐩爣**锛氬妲藉瓨妗ｃ€佽嚜鍔ㄥ浠姐€佽妗ｃ€佹垬鏈噸寮€涓庢垬鍓嶉厤瑁?鏁屾儏/鍦板浘瑙勫垯鏌ョ湅銆?- **楠屾敹鏍囧噯**锛氬尯鍩熴€佹垬鏂楀拰瑁呭鐘舵€佺嫭绔嬩繚瀛樻仮澶嶏紱澶辫触鍚庡彲閲嶅紑鎴栬繑鍥炴嵁鐐硅皟鏁淬€?
## 宸插畬鎴?/ 宸茬煡鍩虹嚎

- [x] 纭畾鎬у崟浜烘垬妫嬪師鍨嬶細琛屽姩鐐广€佽鍔ㄦ潯銆佹湞鍚戙€佹帺浣撱€佷激瀹炽€佺姸鎬併€佽儗鍖呫€佸揩鎹锋爮銆佹瀯绛戙€佹晫浜哄師鍨嬩笌 EditMode 娴嬭瘯銆?- [x] 鍓ф儏妯″紡鏂瑰悜锛氱害 20 灏忔椂銆佷笁鍖哄煙銆佽嚜鐢辨帰绱?鍥炶銆佹棤鏃堕棿鍘嬪姏銆佸畬鏁村瓨璇绘。銆?- [x] P0-01锛氶€氱敤鍙厠闅嗕换鍔＄洰鏍囷紝绉婚櫎鍥哄畾鍧愭爣鑳滃埄纭紪鐮併€?- [x] P0-02 鑷?P1-02锛氭垬褰?鍦扮偣/鏁呬簨/鐗堟湰鐘舵€併€佸尯鍩熷師鍨嬨€佸瓨妗ｅ浠姐€佽澶?鏈嶅姟涓庡叚绫讳换鍔℃ā鏉裤€?- [x] P1-03锛氬浐瀹氱瀛愩€佷笁浠诲姟閾俱€佺粨绠楁憳瑕併€佺嫭绔嬪瓨妗ｇ殑鑲夐附鏁呬簨鍖呫€?- [x] 杩愯鏃跺紑鍙戣彍鍗曘€佹垬鍓嶇畝鎶ュ叆鍙ｃ€佹寮忔垬鏂椼€佹垬鏈噸寮€銆佽繑鍥炶彍鍗曞強涓€杞?1920x1080 HUD 閫傞厤銆?
## 楠岃瘉璁板綍

| 2026-07-23 | V1-06 鏀跺彛鍥炲綊 | COMPLETE | 杩愯鏃舵柇瑷€閫氳繃锛氬ぇ鍦板浘鑺傜偣閫夋嫨銆佹垬鍓嶇畝鎶ャ€佹寮忔垬鏂椼€佸己鍒惰儨鍒┿€佽倝楦界粨绠楀鍔遍鍙栥€佽繑鍥炲ぇ鍦板浘锛涙敾鍑?鍑荤牬/鍙牬鍧忕墿鍙嶉瀵硅薄鎴愬姛鐢熸垚銆?920x1080 涓?960x540 鎴浘鎴愬姛锛涘満鏅?`isDirty=false`锛汧unplay 缂栬瘧閿欒/璀﹀憡 0銆侰onsole 浠呭凡鏈?RenderTexture.active 璀﹀憡銆侷MGUI 鐐瑰嚮涓庣嫭绔?EditMode Runner 涓哄伐鍏烽檺鍒讹紝宸叉湁 EditMode 娴嬭瘯绋嬪簭闆嗕繚鐣欎綔涓鸿鐩栬瘉鎹€倈

| 2026-07-23 | V1-06 鏈€缁堝洖褰?| PARTIAL PASS | Play Mode 杩愯鏃舵祦绋嬫柇瑷€锛氭垬鍓嶇畝鎶ャ€佹寮忔垬鏂椼€佹敾鍑诲弽棣堛€佹垬鏈噸寮€銆佽繑鍥炶彍鍗曘€佸ぇ鍦板浘鍏ュ彛鍧囬€氳繃锛涜妭鐐瑰畬鎴愪笌濂栧姳棰嗗彇閫氳繃锛涙憚鍍忔満鏍囩淇鍚?`Camera.main=present`锛?920x1080 / 960x540 鎴浘鎴愬姛銆侳unplay 缂栬瘧 0 閿欒/璀﹀憡锛汣onsole 浠呬袱鏉℃棦鏈?RenderTexture.active 璀﹀憡銆侷MGUI 鎺т欢涓嶆彁渚?uGUI 灏勭嚎鐩爣锛宍simulate_mouse_click` 鏃犳硶璇佹槑榧犳爣鍛戒腑锛涚嫭绔?EditMode Test Runner 涓嶅彲鐢紝淇濈暀鐜版湁 EditMode 娴嬭瘯绋嬪簭闆嗚瘉鎹€倈

| 2026-07-23 | V1-06 鎽勫儚鏈轰笌娴佺▼琛ュ厖 | PARTIAL PASS | 淇杩愯鏃舵湭璁剧疆 MainCamera 鏍囩鐨勯棶棰橈紱Play Mode 鏂█ `Camera.main=present`銆佹垬鍓嶇畝鎶?姝ｅ紡鎴樻枟/鎴樻湳閲嶅紑/杩斿洖鑿滃崟/澶у湴鍥惧叆鍙ｅ潎鎴愬姛銆傜偣鍑诲伐鍏峰湪 IMGUI 鐢诲竷閲囨牱鐐逛粛鏃?UI 鍛戒腑锛屾晠淇濈暀涓洪獙璇侀檺鍒讹紱缂栬瘧閿欒/璀﹀憡 0銆倈

| 2026-07-23 | V1-06 楠屾敹琛ュ厖 | PARTIAL PASS | 鍦烘櫙淇℃伅纭 `Assets/Scenes/CombatPrototype.unity` loaded 涓?`isDirty=false`锛涜繍琛屾椂鏂█鑺傜偣瀹屾垚鍚庤В閿佸苟棰嗗彇濂栧姳鎴愬姛锛沄1-04 鍙嶉瀵硅薄鐢熸垚鎴愬姛锛汧unplay 缂栬瘧閿欒/璀﹀憡 0銆俙simulate_mouse_click` 鍦ㄩ€€鍑?Play Mode 鍚庢寜宸ュ叿绾︽潫杩斿洖 PLAY_MODE_REQUIRED锛涜仛鐒?EditMode 鏃犵嫭绔?MCP Test Runner锛屼繚鐣欑幇鏈?EditMode 娴嬭瘯绋嬪簭闆嗕綔涓鸿鐩栬瘉鎹€侰onsole 鏈変袱鏉℃棦鏈?RenderTexture.active 璀﹀憡锛屾棤鏂板鑴氭湰閿欒銆倈

| 2026-07-23 | V1-04/V1-05/M1-01/M1-02/V1-06 缁熶竴楠屾敹 | PARTIAL PASS | V1-04 鍙牬鍧忕墿涓庣姸鎬佸弽棣堛€佹棦鏈夋垬鏂?UI 鍩虹嚎銆? 鑺傜偣澶у湴鍥炬暟鎹笌 relay_event 浠诲姟宸插畬鎴愶紱Play Mode 纭鍦板浘鑺傜偣鐩綍涓?start/rail_patrol/relay_raid/relay_event/core_finale锛屽弽棣堝璞″彲杩愯鏃剁敓鎴愶紱1920x1080 涓?960x540 鎴浘鍧囨垚鍔熴€侳unplay 缂栬瘧閿欒/璀﹀憡 0锛孋onsole 浠呮棦鏈夊純鐢ㄨ鍛娿€傚畬鏁撮紶鏍囬€愰」璺緞鍜?EditMode 鑱氱劍鎵ц浠嶅緟琛ュ厖銆倈

| 鏃ユ湡 | 浠诲姟 | 缁撴灉 | 澶囨敞 |
| --- | --- | --- | --- |
| 2026-07-23 | V1-02b 缁撶畻濂栧姳鍗¤緭鍏ヤ笌鎮仠鍙嶉 | COMPLETE | 灏嗘垬鏈帶鍒跺彴鐨?Input System 鍏煎鏂规鍚屾鑷?`RogueliteSettlementPresentation`锛氬鍔卞崱鐢辫繍琛屾椂榧犳爣鍛戒腑鍏滃簳鎵ц棰嗗彇锛屼笉鍐嶄緷璧栧綋鍓嶅紓甯哥殑 uGUI 灏勭嚎鍛戒腑锛涚Щ闄や笉鍏煎鐨?`StandaloneInputModule`锛屽苟涓哄鍔卞崱澧炲姞 DOTween 鎮仠鎻愪寒涓?1.025 缂╂斁銆侳unplay 缂栬瘧 0 閿欒/璀﹀憡銆丆onsole 鏃犻敊璇紱Play Mode 寤虹珛涓夐€変竴缁撶畻鍚庣偣鍑诲乏渚у鍔卞崱锛宍AwaitingReward` 鐢?`True` 鍙樹负 `False` 涓旈鍙?`aether_wand` 鎴愬姛銆傛湭淇濆瓨鍦烘櫙銆備笅涓€姝ワ細V1-03 鏀诲嚮婧?鐩爣/鍑荤牬鍦板浘鍐呰瑙夊弽棣堜笌鍙屽垎杈ㄧ巼楠屾敹銆?|
| 2026-07-23 | V1-02a 鎴樻湳鎺у埗鍙拌緭鍏ヤ笌鎮仠鍙嶉 | COMPLETE | 瀹氫綅鍒板満鏅?HUD 缂哄皯 `EventSystem`锛屽鑷?`GraphicRaycaster` 涓嬬殑 uGUI `Button` 涓嶈兘鎺ユ敹鐢ㄦ埛榧犳爣銆俙TacticalHudSceneBinder` 鐜颁簬杩愯鏃惰ˉ榻?`EventSystem`/`StandaloneInputModule`锛涗负姣忎釜鎸囦护銆佸揩鎹锋爮銆佹瀯绛戝拰鍥炲悎鎸夐挳娣诲姞 DOTween 鎮仠浜壊銆?.035 缂╂斁鍜屾寜涓嬬缉鏀惧弽棣堬紝骞舵彁渚?IMGUI 榧犳爣鍛戒腑鍏滃簳浠ョ粫杩囧綋鍓?Canvas 灏勭嚎鍛戒腑寮傚父銆侳unplay 缂栬瘧 0 閿欒/璀﹀憡銆丆onsole 鏃犻敊璇紱Play Mode 鐐瑰嚮鈥滄敾鍑烩€濆悗鐘舵€佸垏鎹负 `鏀诲嚮`锛屽啀鐐光€滅Щ鍔ㄢ€濆垏鍥?`绉诲姩`锛屾寜閽潎瀹為檯鍛戒腑锛涙偓鍋滆Е鍙戝櫒 4 椤瑰瓨鍦ㄥ苟鍙惎鍔ㄥ弽棣?Tween銆傛湭淇濆瓨鍦烘櫙銆備笅涓€姝ワ細V1-03 鏀诲嚮婧?鐩爣/鍑荤牬鐨勫湴鍥惧唴瑙嗚鍙嶉涓庡弻鍒嗚鲸鐜囪瑙夐獙鏀躲€?|
| 2026-07-23 | V1-02 鑲夐附缁撶畻灞備笌鍙噸澶嶈儨璐熷弽棣?| COMPLETE | 鏂板杩愯鏃?`RogueliteSettlementPresentation`锛氭垬鏂楄儨鍒╁悗浠?DOTween 杩涘叆鐨勫叏灞忕粨绠楀眰灞曠ず绛夌骇銆佺粡楠屻€佷笁寮犳鍣?娉曟湳濂栧姳鍗°€佷激瀹?灏勭▼/绌跨敳鎴栬€楄兘鏁板€硷紱鍗＄墖鐐瑰嚮鍚庣湡瀹為鍙栥€佷繚瀛樺苟鍏抽棴缁撶畻灞傘€俙CombatVisualFeedback` 鍦ㄥ紑濮?鎴樻湳閲嶅紑鏃舵竻绌虹粨灞€鍘婚噸涓庣敓鍛界紦瀛橈紝鍏佽鍚屼竴浼氳瘽杩炵画澶氬満鑳滃埄/澶辫触閮芥湁鍙嶉銆侳unplay 缂栬瘧 0 閿欒/璀﹀憡銆丆onsole 涓虹┖锛汸lay Mode 涓‘璁ょ粨绠?Canvas 鍒涘缓锛屽苟鐢ㄧ湡瀹?UI 鐐瑰嚮棰嗗彇 `aether_wand` 鍚?`AwaitingReward=False`銆佸凡棰嗗彇鍒楄〃姝ｇ‘锛涙柊澧?EditMode 濂栧姳鏁板€煎彲灞曠ず娴嬭瘯骞堕€氳繃鑱氱劍妫€鏌ャ€傛湭淇濆瓨鍦烘櫙銆備笅涓€姝ワ細V1-03 鎺ュ叆鏀诲嚮婧?鐩爣/鍑荤牬鐨勫湴鍥惧唴琛ㄧ幇锛屽苟瀹屾垚 1920x1080 涓?960x540 鍙鍖栭獙鏀躲€?|
| 2026-07-22 | V1-01 DOTween 涓庤〃鐜板熀纭€ | COMPLETE | 宸蹭粠鏈満鐜版湁 Unity 椤圭洰寮曞叆瀹屾暣 DOTween 鎻掍欢鐩綍骞跺畬鎴?Funplay 鍩熼噸杞斤紱缂栬瘧 0 閿欒/璀﹀憡銆傛柊澧炶繍琛屾椂鎴樻枟缁撴灉涓庝激瀹虫诞瀛楀弽棣堢粍浠讹紝浠ュ強鍦板浘闈㈡澘鍙鎬т繚鎶ゃ€傞浠芥湰鍦扮敓鎴愯鑹插師鏂欏凡鍘婚櫎缁垮箷锛屼絾 QA 涓?1024脳1024銆佺害 4 涓囪壊锛屼笉绗﹀悎 64脳64/鍙楁帶璋冭壊鏉挎寮忚祫婧愭爣鍑嗭紝宸查殧绂昏嚦 `Art/Raw/Units`锛屾湭瀵煎叆 Unity 姝ｅ紡璧勬簮鐩綍銆備笅涓€姝ワ細鍒朵綔骞?QA 棣栨壒 64脳64 鍗曚綅涓?32脳32 鍦板潡/鑳屾櫙璧勬簮锛屽啀鎺ュ叆姝ｅ紡瑙嗚灞傘€?|
| 2026-07-22 | R1 | COMPLETE | 鑲夐附鑺傜偣鍦板浘鍖呭惈璧风偣鍙岃矾寰勶紙姝肩伃/鐮村潖锛夊拰缁堢偣锛涙垬鏂楄儨鍒╁洖鍒板湴鍥剧粨绠椼€佽幏寰楃粡楠屽苟鍗囩骇锛屽睍绀虹敱娉曟湳/姝﹀櫒娣峰悎姹犵‘瀹氭€ф娊鍙栫殑 3 涓€欓€変笖鍙笁閫変竴锛涢€夋嫨娉ㄥ叆涓嬩竴鎴樻瀯绛戙€侲ditMode 9 椤归€氳繃锛汧unplay 缂栬瘧 0 閿欒/璀﹀憡銆丆onsole 鏃犻敊璇紱Play Mode 楠岃瘉鑳滃埄鍚庣瓑绾?2銆? 涓鍔卞€欓€夈€侀€夋嫨娉曟湳鍚庢敞鍏ョ粓鐐规垬鏂椼€傛湭淇濆瓨鍦烘櫙銆?|
| 2026-07-22 | R0 | COMPLETE | 鏈€鐭倝楦藉凡鏀舵暃涓轰袱鍏冲畬鏁村惊鐜細绗竴鍏虫鐏?鈫?鐜板満淇浜嬩欢(+1 鎶ょ敳) 鈫?鎶ょ浘鐢垫睜鏀惰幏(棰濆蹇嵎鏍? 鈫?鏍″噯姝ユ灙鍗囩骇(浼ゅ 5) 鈫?绗簩鍏崇牬鍧?鈫?缁撶畻銆傞樁娈电姸鎬佺嫭绔嬪簭鍒楀寲骞跺彲缁х画锛涗笁椤归€夋嫨鍧囧疄闄呮敞鍏ョ浜屽叧銆侲ditMode 7 椤归€氳繃锛汧unplay 缂栬瘧 0 閿欒/璀﹀憡銆丆onsole 鏃犻敊璇紱Play Mode 楠岃瘉绗簩鍏虫瀯绛戜负鎶ょ敳 2銆乣calibrated_rifle` 浼ゅ 5銆侀澶?`shield_cell`锛屽苟瀹屾垚绗簩鍏崇粨绠椼€傛湭淇濆瓨鍦烘櫙銆?|
| 2026-07-22 | P3 | COMPLETE | 鍐崇瓥绛斿嵎宸插浐鍖栵細骞跺垪鑲夐附娴嬭瘯鍏ュ彛銆侀殢鏈虹瀛愪笁浠诲姟閾俱€佹鐏?鐮村潖娌欑洅銆佺嫭绔嬭倝楦藉瓨妗ｃ€佹柊寮€/缁х画/鍒犻櫎銆佹晠浜嬮摼鑳滃埄鎺ㄨ繘銆佸け璐ヨ繑鍥為厤缃€佹垬鏈噸寮€涓庝竴閿儨璐熸祴璇曞叆鍙ｃ€侳unplay 缂栬瘧 0 閿欒/璀﹀憡銆丆onsole 鏃犻敊璇紱Play Mode 楠岃瘉鏁呬簨閾鹃鎴樿儨鍒╄繘鍏ュ伐鍘傜獊鐮寸畝鎶ャ€佷袱涓矙鐩掔洰鏍囦笌澶辫触杩斿洖銆傛湭淇濆瓨鍦烘櫙銆?|
| 2026-07-22 | P0-01 | COMPLETE | 閫氱敤鍙厠闅嗙洰鏍囷紱Funplay 缂栬瘧鏃犻敊璇紝EditMode 閫氳繃銆?|
| 2026-07-22 | P0-02 鑷?P1-02 | COMPLETE | 鐘舵€併€佸尯鍩熴€佸瓨妗ｃ€佹湇鍔°€佷换鍔℃ā鏉垮疄鐜板苟閫氳繃 EditMode/Funplay 缂栬瘧銆?|
| 2026-07-22 | P1-03 | COMPLETE | 鑲夐附鏁呬簨鍖呯嫭绔嬪瓨妗ｏ紱EditMode 閫氳繃锛孎unplay 缂栬瘧鏃犻敊璇€?|
| 2026-07-22 | 寮€鍙戣彍鍗曚笌姝ｅ紡鎴樻枟娴佺▼ | COMPLETE | Play Mode 鑿滃崟涓?start/back 娴佺▼楠岃瘉锛涙湭淇濆瓨鍦烘櫙銆?|
| 2026-07-22 | UI 缇庡寲涓庡睆骞曢€傞厤 | COMPLETE | 75% 鎴樺満 + 25% HUD锛汣ellSize 璋冩暣涓?78 浠ラ伩鍏?960x540 瓒婄晫銆?|
| 2026-07-22 | 鏈湴鍍忕礌绱犳潗鐢熸垚涓庡鏌?| BLOCKED | Relay Canvas 鏈夋湰鍦?API Key锛屼絾涓婃父 gpt-image-2 璐熻浇楗卞拰锛屾湭瀵煎叆浠讳綍 AI 鍥炬垨姝ｅ紡璧勪骇銆?|
| 2026-07-22 | P0-05 | COMPLETE | `CombatFlowController` 鏂板鑿滃崟/绠€鎶?缁撶畻閲嶅紑/杩斿洖鑿滃崟鐘舵€侊紱杩愯鏃?Bootstrap 鏀圭敱鍗曚竴娴佺▼椹卞姩銆侳unplay 缂栬瘧閿欒 0锛岃仛鐒?EditMode 閫氳繃锛汸lay Mode 楠岃瘉鑿滃崟->绠€鎶?>鎴樻枟銆佽儨鍒?澶辫触缁撶畻銆佹垬鏈噸寮€鍜岃繑鍥炶彍鍗曪紝涓昏閲嶅紑鎭㈠鑷?(1,4)銆?920x1080 涓?960x540 鑿滃崟鎴浘鏃犺秺鐣岋紱鏈繚瀛樺満鏅€備笅涓€姝ワ細P3 寮€鍙戣彍鍗曟晠浜嬪寘/浠诲姟妯℃澘鍏ュ彛銆?|
| 2026-07-22 | P1-04 | COMPLETE | 鎺у埗鍙版敼涓?500px 瀹界殑鍒嗗尯渚ф爮锛氳祫婧愭潯鎽樿銆佸弻鍒楁垬鏈寚浠ゃ€佸揩鎹锋爮銆佹瀯绛?鍥炲悎銆佺簿绠€琛屽姩鏉′笌鎴樻枟璁板綍銆侳unplay 缂栬瘧鏃犻敊璇€丆onsole 鏃犻」鐩敊璇紱Game View 瀹炴祴鏃犳枃瀛楅噸鍙狅紝鏈繚瀛樺満鏅€?|
| 2026-07-22 | 鏈湴 UI 鏂瑰悜鍥?| PASS | 宸插惎鍔?`E:\鏁版嵁搴揬鍥剧墖鐢熸垚` Relay Canvas 骞堕€氳繃鏈満鎺ュ彛鐢熸垚 UI 姒傚康鍙傝€冨浘锛歚E:\鏁版嵁搴揬鍥剧墖鐢熸垚\outputs\gpt-image-2-2026-07-22T12-24-18-570Z-4dcd4cf3.png`銆傚浘浠呯敤浜庡竷灞€/鏉愯川/鑹插僵瀹℃煡锛屾湭瀵煎叆 Unity銆?|
| 2026-07-22 | P1-05 | COMPLETE | 瀹炶榛戠櫧鏋佺畝宸ヤ笟鎴樻湳 HUD锛氱粏绾挎鏋躲€佺伆闃堕潰鏉裤€侀檺閲忓喎闈?閿堢孩/瀹夊叏榛勮涔夎壊锛屼互鍙婅繍琛屾椂鐢熸垚鐨?6 鏋氱嫭绔?32x32 鐐硅繃婊ゅ儚绱犳寚浠ゅ浘鏍囥€侳unplay 缂栬瘧鏃犻敊璇€丆onsole 涓虹┖锛?920x1080 Game View 涓?960x540 鎴浘纭 HUD 鍙涓旀棤閲嶅彔/瓒婄晫锛涙湭淇濆瓨鍦烘櫙銆佹湭瀵煎叆 AI 鍥俱€備笅涓€姝ワ細P3 鑲夐附鏁呬簨鍖呬笌浠诲姟妯℃澘寮€鍙戣彍鍗曞叆鍙ｃ€?|
| 2026-07-22 | P1-06 | COMPLETE | HUD 宸插満鏅寲锛歚CombatPrototype` 鐨?`鍦烘櫙UI/鎴樻湳HUD` 淇濆瓨浜嗗叏閮ㄧ粏绾垮垎鍖恒€乣Button`銆乣RawImage` 鍥炬爣妲姐€佽祫婧愭潯鍜岃鍔ㄦ潯锛孋anvas 淇濆瓨 `TacticalHudSceneBinder`銆? 鏋氬浐瀹?32x32 Point-filter 鍥炬爣璧勪骇淇濆瓨鍒?`Assets/Game/Art/UI/Icons32/`锛汸lay Mode 楠岃瘉 HUD 鍚屽抚婵€娲诲強鈥滄敾鍑烩€濇寜閽疄闄呭垏鎹㈤€夋嫨銆傜紪璇戞棤閿欒銆丆onsole 鏃犻敊璇€?|
#### V2-16: floor_hazard static tile - COMPLETE (2026-07-25)
- Goal: add one independent 32x32 hazard-marked relay-station floor tile as a static visual variant; no gameplay rule or scene YAML change.
- Files: `Worldbuilding/05_美术与音频/像素资产原料/V2-16/`, `UnityProject/Assets/Game/Resources/Art/FormalRelay32/floor_hazard.png`.
- Verification: normalization report PASS (16 colors, hard alpha, 32x32); Funplay compile/warnings 0; Play Mode load verified Point/Clamp; Console empty; scene `isDirty=false`.
- Next: keep static-only asset iteration and choose one new single object when continuing.

#### V2-17: 本地 image2 像素规范化路线样本 - COMPLETE (2026-07-27)
- Goal: 固化“本地 `gpt-image-2` 独立单图初稿 → 本地 32×32/64×64 规范化 → 4x QA/JSON 报告”的可复核路线；不使用 AI 拼板硬切，不导入 Unity。
- Files: `Worldbuilding/05_美术与音频/像素资产原料/V2-17/`、`OCC_正式像素资产清单_v0.1.md`。
- Verification: `steel_floor` 为 32×32、16 色、硬 alpha，边界 `[1,1,31,31]`；`riflewoman` 为 64×64、24 色、硬 alpha，边界 `[18,12,46,58]`，两份 JSON 报告均为 `PASS`，4x QA 分别为 128×128/256×256。
- Next: 按同一路线选择单个静态生产资产；地块保留完整表面时禁用色键，绿幕单位保持色键去背，再决定是否进入 Unity 导入评审。

#### V2-18: 原生逻辑像素格提示词验证 - COMPLETE (2026-07-27)
- Goal: 修正“高分辨率插画后缩小”导致信息丢失的问题；在本地 image2 提示词中直接锁定 32×32/64×64 逻辑像素格、色数与单位脚底基线，本地不做裁切、拟合或重定位。
- Files: `Worldbuilding/05_美术与音频/像素资产原料/V2-18/`、`OCC_正式像素资产清单_v0.1.md`。
- Verification: `steel_floor_native32_v02` 经整画布最近邻取样后为 32×32、15 色、硬 alpha；`riflewoman_native64_v02` 为 64×64、23 色、硬 alpha、边界 `[18,4,53,59]`，脚底落在 `Y=58`。两份 v02 JSON 报告均为 `PASS`，4x QA 已生成。
- Next: 后续正式单图提示词必须先给出逻辑像素格、色数、背景和基线约束；本地处理仅限最近邻取样、色键/硬 alpha、无抖动调色板压缩与 QA。

#### V2-19: Codex 单图生成与本地规范化样本 - COMPLETE (2026-08-02)
- Goal: 验证 Codex 内建图像生成可作为 OCC 单个静态资产的原图来源，并走完绿幕去背、32×32 规范化、4× QA、调色板和 JSON 报告；不导入 Unity，不替换既有资源。
- Files: `Worldbuilding/05_美术与音频/像素资产原料/V2-19/`、`OCC_正式像素资产清单_v0.1.md`。
- Verification: 第二版 `aether_supply_crate_v02` 为 32×32、14 色、硬 alpha，边界 `[6,8,26,23]`；原图、去背中间文件、规范化 PNG、4× QA、调色板和 JSON 规格报告齐全，报告 `PASS`。按产品决定接受低饱和冷青/安全黄表现，资产升为 `FORMAL` 原料，未导入 Unity。未修改场景、`ProjectSettings.asset` 或 Unity 正式资源目录。
- Next: 保持 V2-15 为唯一主任务；后续可单独建立补给箱 Unity 导入评审，或按人物美术规范复核/补齐单个 `64×64` 单位静帧。

#### V2-20: 人物静帧美术规范 - COMPLETE (2026-08-02)
- Goal: 将 OCC 人物静帧的固定画布、锚点、视角、阵营色预算、兵种轮廓、Codex 单图提示词和 QA 门禁写成可执行规范；不生成动画、不导入 Unity。
- Files: `Worldbuilding/05_美术与音频/OCC_单位像素美术规范_v0.1.md`、`OCC_像素资产_QA流程_v0.1.md`。
- Verification: 规范锁定 `64×64`、`X=32`、接地 `Y=58`（允许 `57–59`）、硬 alpha、最多 24 色、近顶视三分之四角和 6 类兵种轮廓合同；包含 Codex 单图原料提示词模板及从原料到 Unity 导入的两阶段 QA 门禁。
- Next: 保持 V2-15 为唯一主任务；按规范复核现有单位原料，或选择一个确有缺口的单一兵种静帧再生成。

#### V2-21: 角色三层视觉规范 - COMPLETE (2026-08-02)
- Goal: 为每名角色锁定战棋形象、角色立绘与可读取表情细节的演出形象三层资产，定义共享身份锚点、各层画面职责、生成处理路径和 QA 门禁。
- Files: `Worldbuilding/05_美术与音频/OCC_角色三层视觉规范_v0.1.md`、`OCC_美术规范_v0.1.md`、`OCC_单位像素美术规范_v0.1.md`、`README.md`。
- Verification: A 层固定为 64×64 战棋单位；B 层为身份/装备立绘；C 层为面部占画面高度至少 45%、含 `neutral`/`resolve`/`strain` 表情目标的近景演出形象。三层必须通过体态、头部、职业装备、服装、色彩与以太技术六项身份锚点的一致性审查；未生成/导入 Unity 资产。
- Next: 保持 V2-15 为唯一主任务；以一名主角模板制作完整 A+B+C 样本，再扩展至命名 NPC 与首领。

#### V2-22: 主角模板三层视觉样本 - PAUSED (2026-08-04)
- Goal: 以一名工业区侦察步枪手主角模板验证 A 战棋形象、B 角色立绘与 C 全身演出动作形象的身份一致性；A 为 64×64，C 使用低色数全身动作但仍能读取面部表情；不替换 Unity 现有主角。
- Files: `Worldbuilding/05_美术与音频/像素资产原料/V2-22/主角模板/`、`OCC_角色三层视觉规范_v0.1.md`。
- Acceptance: 三层共享短黑发、右眉伤、左鬓冷青发夹、煤灰短外套、左肩钢甲、步枪与腰间导具；每层有原图、规范化输出、QA、调色板和报告；C 包含低色数全身 `neutral`/`resolve`/`strain` 和后续动作池；A 通过 64×64 `X=32`/`Y=58` 门禁。
- Current: B 立绘与旧版 C `resolve` 近景原图已由 Codex 生成，后者降为表情参考；A 在前三次网络失败后重试成功，已规范化为 64×64、24 色、硬 alpha，边界 `[17,3,51,60]`（脚底实际至 Y=59），4× QA 和调色板齐全，保持 `QA_PENDING`。已建立身份卡和可复跑规范化脚本，且 C 全身演出已改为 `192×288` / 最多 24 色的动作资产合同。
- Next: 补齐新版全身 C `neutral`/`resolve`/`strain` 独立原图，再执行跨层身份一致性审查；保持 V2-15 为唯一主任务。
