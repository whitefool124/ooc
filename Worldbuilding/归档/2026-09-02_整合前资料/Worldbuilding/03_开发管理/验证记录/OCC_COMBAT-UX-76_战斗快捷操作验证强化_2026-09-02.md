# OCC COMBAT-UX-76 战斗快捷操作验证强化（2026-09-02）

## 范围

本轮只增强 COMBAT-UX-75 的可重复行为证据，并修复验证暴露的表现层空安全问题；不改变行动点、个人魔力、冷却、伤害、护盾、射程、目标规则或敌人行为。

## 新增行为证据

- 快捷移动提交后，英雄位置按目标格更新，行动点按 `CombatResolver.BasicActionPointCost` 扣除；向敌方占用格提交时位置和行动点均不变。
- 右键攻击提交会关闭菜单并切换到攻击模式；行动点扣除、护盾吸收与生命伤害分别和 `CombatResolver.PreviewAttack` 的权威字段一致。
- 右键术式 `spell:1` 映射到“技能2”，只扣该槽 `SpellDefinition` 声明的行动点与个人魔力，并对合法敌人产生效果。
- 个人魔力为 0 时，敌人格仍保留合法普通攻击，但所有 `spell:*` 项均隐藏。
- 数字键选中零魔力术式时不预扣资源；空槽拒绝后保留上一合法战斗模式。

## 缺陷与修复

聚焦测试首次运行发现，快捷移动、右键攻击和右键术式均已执行成功，但独立战斗会话在后续调用 `developerFlow.RefreshOutcome()` 时抛出空引用。现将普通命令、火系术式与法宝提交后的结算刷新改为 `developerFlow?.RefreshOutcome()`；已绑定正式流程时行为不变，未绑定的独立会话不再因表现层依赖中断。

## Funplay 证据

- Funplay 包：`com.gamebooom.unity.mcp 0.6.4`；第三方 OpenUPM 无签名提示作为已知包来源警告保留，不属于编译错误。
- 身份门禁：`Application.dataPath = E:/数据库/OCC_Codex/UnityProject/Assets`。
- 活动场景：`Assets/Scenes/CombatPrototype.unity`；非 Play Mode。
- `request_recompile` 域重载恢复 `Success`；最终编译 0 error／0 warning。
- 聚焦 EditMode：`CombatSelectionControllerTests` 14/14，通过。
- 全量 EditMode：687/687，通过。
- Console error：0。Console 保留 4 条既有 `RogueliteSettlementPresentationTests` Unity API 弃用 warning，与本任务无关。
- dirty scene：0；`.unity` 文件无本轮改动；未保存场景。

## 剩余边界

真实鼠标双击节奏、右键菜单射线命中、数字键输入和 1920×1080／960×540 视觉接触仍需要明确授权进入 Play Mode；本记录不以 EditMode 证据替代实机手感结论。
