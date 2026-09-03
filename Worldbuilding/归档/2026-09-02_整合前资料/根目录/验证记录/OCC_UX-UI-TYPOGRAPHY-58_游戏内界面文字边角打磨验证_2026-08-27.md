# UX-UI-TYPOGRAPHY-58 游戏内界面文字边角打磨验证

**日期：** 2026-08-27  
**范围：** 剧情／肉鸽共用 UI 表现层。未修改文案含义、玩法信息层级、战斗逻辑、数值、地图、交互、存档、正式 PNG 或场景。

## 改动

- `FormalUiKit` 增加单行读数与段落两类公共文字配置。单行读数关闭自动换行和自动缩字并启用几何对齐；段落保持换行、纵向截断、几何对齐和 `1.08` 行距。
- `FormalCombatHud` 的阶段、行动者、装备、状态、AP、生命／护盾／个人魔力、行动序列和现场记录使用明确文字角色；默认战斗标签由纵向溢出改为纵向截断。
- `FormalHoverTooltip` 标题固定单行，正文按自适应卡片宽度换行，并在最大高度内截断。
- 新增 EditMode 回归，锁定默认边界、数字单行、右对齐、禁自动缩字及段落行距。

## 双分辨率接触

| 接触 | 证据 | 结果 |
| --- | --- | --- |
| 1920×1080 正常基准 | `UnityProject/Artifacts/UX58/ui_typography_after_1920x1080.png` | 标题、行动摘要、三类资源、行动序列、悬浮说明与窄栏混排无越界或串栏 |
| 960×540 紧凑基准 | `UnityProject/Artifacts/UX58/ui_typography_after_960x540.png` | 使用项目紧凑字号规则；AP、三类资源与行动序列仍保持单行和稳定右边界，段落无裁掉上下缘 |

人工检查覆盖中文与 `AP`／`WASD`／`Enter`／斜杠数字混排、长敌人名称、无上限护盾、两行行动摘要和长悬浮正文。语义色面积未增加，文字仍使用暖纸面／炭墨主体系。

## Unity 门禁

- `Application.dataPath = E:/数据库/OCC_Codex/UnityProject/Assets`；`EditorApplication.isPlaying = false`。
- Unity 脚本重新编译成功，编译错误 `0`。
- 聚焦 EditMode：`3/3 passed`。
- 全量 EditMode：`651/651 passed`。
- Console：error `0`，warning `0`。
- dirty scenes：`0`。
- 未进入 Play Mode，未保存 `CombatPrototype.unity`。
