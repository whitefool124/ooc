# OCC M-A4 像素 UI 皮肤资产清单 v0.1

## 设计结论

UI 不使用整页背景图或 AI 生成文字。正式皮肤由 15 张 `16×16` 独立像素切片组成，统一以 4px 九宫格边界拉伸；中文信息继续由清晰黑体承担。所有切片使用硬 alpha、Point、Clamp、无 mipmap。

| 资产 | 用途 |
| --- | --- |
| `panel` / `panel_elevated` | 普通与高层级面板、节点详情 |
| `header` | 页眉和战斗抬头 |
| `button_idle/hover/pressed/disabled` | 按钮四态，由 `UiButtonFeedback` 实时换图 |
| `tab_idle/tab_active` | 页签与当前页语义 |
| `slot` | 图标、快捷栏及装备槽 |
| `bar_track/bar_fill` | 结构、护盾、以太及像素分隔带 |
| `focus` | 键盘/手柄焦点角标，替代 `Outline` |
| `danger` / `reward` | 危险确认框与奖励强调框 |

## 使用点与证据

- 中央入口、20 节点地图、节点详情、简报、设置、档案：`FormalRogueliteUi` → `FormalUiKit`。
- 战斗页眉、右侧读数、资源条、底部指令与结算浮层：`FormalCombatHud`。
- 三选一：`RogueliteSettlementPresentation`；确认/反馈浮层：`FormalUiInteractionLayer`。
- 生产脚本：`Tools/Art/generate_occ_pixel_ui_skin.py`。
- 静态 QA：`PixelUISkin/QA/occ_pixel_ui_skin_qa_v01.json`，15/15 PASS。
- 运行时视觉 QA：`UnityProject/Assets/Game/QA/M-A4/VisualAudit/`，入口/地图/战斗覆盖 1920×1080 与 960×540，并含灰阶与红绿色觉风险变体。

人物相关资产仍为 `BLOCKED_CONTENT`，本任务未生成或修改人物美术。
