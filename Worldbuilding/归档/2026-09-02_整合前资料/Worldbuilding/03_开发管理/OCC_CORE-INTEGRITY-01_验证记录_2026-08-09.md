# OCC CORE-INTEGRITY-01 验证记录（2026-08-09）

## 结论

`CORE-INTEGRITY-01` 已完成。战斗物品以 `ItemInventory + ItemQuickbar[8]` 为唯一权威状态；旧定义快捷栏和无实例医疗包命令已移除。肉鸽初始物品只由地图运行创建一次，战斗耗尽后写回地图运行，后续战斗不会重新生成。

## 实施范围

- 战斗状态与命令：`CombatState`、`CombatCommand`、`CombatResolver`。
- 地图运行与展示签名：`RogueliteMapRun`、`UiPresentationModels`。
- 战斗构建与 HUD：`CombatPrototypeBootstrap`、`FormalCombatHud`、`TacticalHudSceneBinder`。
- EditMode 回归：物品系统、效果执行与 HUD 展示模型测试。
- 未修改 `.unity` 场景、正式美术资产、`map9` 字段顺序或存档版本。

## 自动化结果

| 检查 | 结果 |
| --- | --- |
| Funplay MCP | 本地服务器 `v0.5.4`，项目身份匹配当前 Unity 工程 |
| Unity 状态 | Unity `6000.5.2f1`；非 Play Mode、非暂停、非编译中 |
| 重编译 | 0 error / 0 warning |
| 全量 EditMode | 266 passed / 0 failed / 0 skipped / 0 inconclusive；2.044 秒 |
| Console | 无 error 条目 |
| 活动场景 | `Assets/Scenes/CombatPrototype.unity`，`isDirty=false` |
| 场景文件 | `CombatPrototype.unity` 与 `TrainingRange.unity` 均无 Git 改动 |
| 旧入口扫描 | 无 `CombatState.Quickbar`、`ConfigureQuickbar`、`ClearQuickbarSlot`、`UseQuickbarSlot` 或 `CombatCommand.UseItem` 运行时引用 |
| 补丁格式 | `git diff --check` 通过 |

## 关键回归

- 普通 `CombatState` 默认背包与八格快捷栏均为空。
- 新肉鸽运行只有一个医疗包和一个护盾电池；医疗包耗尽并捕获后，下一场战斗不会重生。
- 八个实例槽位均可绑定，单一实例不会重复占槽；卷轴与法宝合计最多四件，在上限状态下移动或替换已有特殊物品仍合法。
- 普通消耗品从快捷栏按实例执行，扣除既有 AP 后消耗实例；耗尽时清除背包实例与所有槽位引用。
- HUD 展示签名包含实例 ID、定义 ID 与剩余次数，因此消耗一次但尚未耗尽的法宝也会触发刷新。
- `map9` 往返继续保留实例 ID、剩余次数、位置、旋转和八格槽位顺序。

## 未执行项

未进入 Play Mode，也未进行 1920×1080 / 960×540 运行时视觉检查，因为用户未明确授权。本项不影响代码与数据一致性验收；八槽排版仍需在未来获授权的视觉回归中确认。

## 解锁下一步

下一候选为 `SAVE-INTEGRITY-01`：为 `map9` 增加节点、武器、资源、生命与实例引用的语义不变量，并将失败接入坏档备份和写保护。
