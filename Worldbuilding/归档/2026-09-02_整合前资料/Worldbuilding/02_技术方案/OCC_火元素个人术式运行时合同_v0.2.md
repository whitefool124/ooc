# OCC 火元素个人术式运行时合同 v0.2

## 1. 权威边界

- 玩法源：`Worldbuilding/01_游戏策划/OCC_火元素个人术式池_v0.2.md`。
- 运行时目录：`FireSpellCatalog.All`，版本 `fire-personal-spells-v0.2`，严格为 `F-P-M01–M20`、`F-P-U01–U20`、`F-P-R01–R20`。
- v0.1 的 `F-P01–F-P50` 只作为迁移审计源；运行时不得按旧 ID 静默替换新语义。

## 2. 稳定数据合同

每项必须声明 `combat_affinity`、`delivery_mode`、`weapon_requirement`、`trigger_window`、`consumption_rule`。对应运行时字段为 `CombatAffinity`、`DeliveryMode`、`WeaponRequirement`、`TriggerWindow`、`ConsumptionRule`。

- M：`MeleeOnly`，要求近战武器，覆盖身体强化、接触导能、近战架势与反击。
- U：`WeaponUniversal`，按条目要求允许任意/近战/远程武器，主要通过下一次合法武器攻击、当前行动或反应窗口投递。
- R：`RangedSpell`，不依赖武器，使用脱体投射、火场与远程标记路径。

兼容卷轴/法宝的旧构造器只映射为 `RangedSpell + DetachedProjection + None + Immediate + OnCast`，不得进入个人术式奖励池。

## 3. 预览、提交与触发

1. `Preview` 纯查询验证 AP、以太、冷却、亲和、武器、目标、范围、视线、状态/火场来源、自伤生命门槛及可破坏物掩码。
2. `Execute` 只提交已通过预览的施放时规则，并记录递增 `FireSpellResultStep`；步骤包含投递、窗口与消费字段。
3. 非即时条目写入可克隆 `FirePendingEffect`，由普通攻击、物件攻击、相邻攻击、标记目标移动、敌方进入标记格或下一行动窗口触发。
4. `FireSpellEngine.ResolveWeaponAttack` 是正式武器闭环：先应用公开的甲前远程减伤，再执行武器结算，随后结算附着/反击并统一刷新胜负。
5. 移动提交后依次结算火场入格、追击与警戒；行动开始结算火场停留、冷却与窗口到期。所有路径无随机命中、暴击或隐藏倒计时。

## 4. 结果、UI 与资产

- HUD/靶场配置公开亲和、投递、武器要求、触发窗口与消费规则；战斗预览显示合法格和失败原因。
- `FireSpellExecution` 是结果事件源；正式表现从目录的 `IconPath`/`PresentationModules` 读取，不按 ID 在 HUD/VFX 层分支。
- 60 项显式映射到 39 张语义匹配、已通过 QA 的正式 32×32 图标；允许同语义复用，不允许通用 fallback 或错误占位。Importer 必须为 Sprite、Point、Clamp、PPU32、无 mipmap。

## 5. 奖励、装备与保存

- `FireSpellProgression` 从 v0.2 目录按稀有度、已拥有项和当前武器合法性生成确定性候选；60 项内容池均可达。
- `RogueliteMapRun map9` 在 map8 的目录版本、拥有项、两个装备槽、迁移权益与诊断之上，新增火系开局 ID、战斗快照有效位、当前生命/护盾/个人魔力；map8 及更旧版本显式升级为“尚无战斗快照”，首次战斗使用完整基础值。
- 正式新局通过 `FireRogueliteStarterCatalog` 选择 M/U/R 开局；结算候选使用当前主手过滤，普通节点组合为 2 术式+1卷轴，高阶节点为 1稀有术式+1卷轴+1法宝。领取术式只加入拥有列表，换装与武器相容校验只在工坊入口提交。
- v0.1 迁移固定为 21 直迁、26 同稀有度重选、3 补偿；重选恢复原装备槽，未知 ID 只记录诊断，不得丢弃或猜测替换。
- 坏档读取保留原槽并首次备份到 `.corrupt_backup`；失败实例写保护，只有显式删除后才可覆盖。

## 6. 验证门禁

- 60/60 可装备；每项两份合法预览/执行签名一致，并至少一份确定性非法预览。
- EditMode 覆盖目录、触发、内容池、迁移、map9、三开局×双首领完整路线、跨节点战斗资源、Importer 与结构风险；必要 PlayMode 覆盖 UI 场景卸载。
- 每轮脚本改动经 Funplay 重编译；最终编译错误/警告 0、Console 无项目错误、1920×1080 与 960×540 可读、相关场景 clean。
