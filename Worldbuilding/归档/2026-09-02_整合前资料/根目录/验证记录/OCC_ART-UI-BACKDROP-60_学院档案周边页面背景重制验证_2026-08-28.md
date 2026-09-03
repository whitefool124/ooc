# OCC ART-UI-BACKDROP-60 学院档案周边页面背景重制验证

日期：2026-08-28  
范围：剧情／肉鸽共用 UI 表现层；不改玩法、数值、交互、存档、页面信息层级或场景。

## 结论

启动、入口、地图、简报、库存、结算、档案、设置共 8 张页面背景已完成独立生产、规范化、Unity 正式注册与双分辨率接触，全部晋级 `FORMAL`。旧蓝黑终端网格占位已替换为暖纸、布面档案夹、学院石材、旧木、锻铁和少量黄铜组成的“学院行动档案／实地调查手册”语言；真实标题、卡片、按钮仍由 UGUI 绘制。

生产仅使用 Codex 内建 imagegen，逐张生成独立原料；未使用拼板切图、本地生图工作台、localhost API、私有 relay 或自动回退。

## 资产与接触表

| 资产 ID | 页面 | 主要识别物 | 1920×1080 | 960×540 | 结果 |
|---|---|---|---|---|---|
| `ui.backdrop.startup` | 启动 | 合拢的布面卷宗与旧铜压条 | `startup_1920x1080.png` | `startup_960x540.png` | PASS |
| `ui.backdrop.landing` | 入口 | 学院门廊与远处封存塔 | `landing_1920x1080.png` | `landing_960x540.png` | PASS |
| `ui.backdrop.map` | 地图 | 摊开的总图、木尺、登记夹 | `map_1920x1080.png` | `map_960x540.png` | PASS |
| `ui.backdrop.briefing` | 简报 | 调度夹板与封存红边签 | `briefing_1920x1080.png` | `briefing_960x540.png` | PASS |
| `ui.backdrop.inventory` | 库存 | 工坊桌、布垫、锻铁夹具 | `inventory_1920x1080.png` | `inventory_960x540.png` | PASS |
| `ui.backdrop.settlement` | 结算 | 归档卷宗、印台、托盘 | `settlement_1920x1080.png` | `settlement_960x540.png` | PASS |
| `ui.backdrop.archive` | 档案 | 索引抽屉、书架、登记簿 | `archive_1920x1080.png` | `archive_960x540.png` | PASS |
| `ui.backdrop.settings` | 设置 | 校准卡、机械旋钮、测试片 | `settings_1920x1080.png` | `settings_960x540.png` | PASS |

接触目录：`UnityProject/Artifacts/UiBackdrops8/contacts/`。  
8 页 Unity 汇总：`unity_ui_backdrops_8_contact_1920x1080.png`、`unity_ui_backdrops_8_contact_960x540.png`。  
前后对照：`ui_backdrops_8_old_new_review.png`。  
真实 UGUI 叠层：`application_landing_1920x1080.png`、`application_landing_960x540.png`。

## 机器与人工门禁

- 8/8：`480×270`，24 色，完全不透明硬 Alpha，最近邻整数 2×／4×显示。
- 8/8：1×、2×、4×、灰阶、棋盘格证据齐全；无文字、数字、按钮或完整 UI 烘焙。
- 8/8：`occ-art-manifest-v1` 为 `FORMAL`；`validate_occ_art_asset.py` PASS；合同审计 PASS。
- 8/8：Unity GUID 唯一；Sprite／Point／Clamp／PPU32／Uncompressed／无 mipmap。
- 8/8：Resources 路径和 `OccPeripheralUiV01` 注册通过；`FormalUiEffects.ApplyBackdrop` 实际加载 Sprite 并使用白色乘色。
- 人工审美：中央信息区安静，角落叙事物未压住正文；纸、布、木、铁与学院石材可辨；页面之间有身份差异，且没有重新落回统一蓝晶／冷青能量模板。

批次验证报告：`UnityProject/Artifacts/UiBackdrops8/validation_report.json`。  
Unity 导入审计：`UnityProject/Artifacts/UiBackdrops8/import_audit.json`。  
Manifest：`Worldbuilding/05_美术与音频/正式美术生产/M-A27/manifests/`。

## Unity 回归

- 主工作树门禁：`Application.dataPath = E:/数据库/OCC_Codex/UnityProject/Assets`。
- 聚焦 EditMode：`1/1 PASS`。
- 全量 `OCC.Combat.EditModeTests`：`652/652 PASS`。
- 编译：错误 0、警告 0。
- Console：error 0。
- 活动场景：`Assets/Scenes/CombatPrototype.unity`，`isDirty = false`。
- 未进入 Play Mode，未保存场景。

## 下一批建议

若继续，可另立 6 件跨页面复用的低权重边缘装饰：左侧装订脊、右上索引页签、下沿测量尺、档案夹角扣、折页阴影、状态色纸夹。它们应是透明独立层，不烘焙文字或按钮，也不改变 UI 层级。若真实页面暴露对比或裁切问题，应优先只返修对应背景。
