# OCC COMBAT-UX-75 战斗快捷操作与模式反馈验证（2026-09-01）

## 结果

- 双击合法可达地块可快捷切换到移动并提交；非移动模式下首击仅在“该格也可移动”的歧义窗口内延迟，避免双击前先误施法。
- 右键地块／单位打开位置行动菜单，只列当前行动点、个人魔力、冷却、剩余次数、目标与视线规则共同允许的移动、攻击、搜刮、互动或术式。
- 数字键 1–8 绑定对应术式槽；资源足够时只进入选目标状态，不提前消耗资源，资源不足时返回明确拒绝原因。
- HUD 以当前模式标题、按钮选中态、左键用途说明共同提示当前左键将执行移动、攻击、施术、搜刮或互动。

## Funplay 门禁

- `Application.dataPath = E:/数据库/OCC_Codex/UnityProject/Assets`。
- 活动场景：`Assets/Scenes/CombatPrototype.unity`；非 Play Mode。
- `request_recompile` 域重载恢复状态 `Success`；编译检查 0 error／0 warning。
- 聚焦 EditMode：`CombatSelectionControllerTests` 10/10，通过。
- 全量 EditMode：683/683，通过。
- Console error：0。Console 仍有 4 条既有 `RogueliteSettlementPresentationTests` Unity API 弃用 warning，不属于本次改动。
- dirty scene：0；未保存场景、未进入 Play Mode。

## 范围结论

本轮只改变剧情／肉鸽共用的战斗输入入口与表现反馈，不改变行动点、魔力、冷却、伤害、射程、目标规则或敌人行为。运行态真实鼠标／键盘手感接触留待用户明确授权 Play Mode 后执行。
