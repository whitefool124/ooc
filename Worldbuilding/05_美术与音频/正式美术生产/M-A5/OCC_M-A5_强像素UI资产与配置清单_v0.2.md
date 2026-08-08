# OCC M-A5 强像素 UI 资产与配置清单 v0.2

## 视觉基准

- 基准图：`正式美术生产/M-A5/LayoutPreview/OCC_combat_ui_layout_preview_v02_strong_pixel.png`
- 参考画布：1920×1080；战场 1440px，HUD 480px。
- 逻辑像素在最终画面中至少形成 4px 块感；边框使用阶梯角、双层明暗边和块状语义色，不使用细线 `Outline`。
- 中文正文保留清晰字体；像素味由框体、槽位、图标、分隔块、条形资源与布局节奏承担。
- 人物相关资产为 `BLOCKED_CONTENT`，本批次没有生成、修改或绑定人物图像。

## 正式切片

运行时目录：`UnityProject/Assets/Game/Resources/Art/FormalUISkin16/`

- M-A4 保留并按 v02 重绘的基础 15 项：`panel`、`panel_elevated`、`header`、按钮四态、标签两态、`slot`、资源条轨道/填充、`focus`、`danger`、`reward`。
- M-A5 新增 15 项：`panel_console`、`panel_module`、`panel_target`、`panel_log`、四类指令组、`button_end_turn`、三类分段资源条、`badge_cost`、`slot_locked`、`timeline_node`。
- 全部为独立 16×16 PNG，4px 九宫格边界，Point、Clamp、无 mipmap、无压缩、PPU 32。
- 生成器：`Tools/Art/generate_occ_pixel_ui_v02_assets.py`。
- QA：`PixelUIV02/QA/occ_pixel_ui_v02_qa.json` 与 `occ_pixel_ui_v02_contact.png`；30/30 `QA_PASS`。

## 数据配置

运行时配置：`UnityProject/Assets/Game/Resources/Config/OccPixelUiV02.json`

- `skins`：皮肤语义 ID 到 Resources 路径的唯一映射。
- `states`：按钮 normal/hover/pressed/disabled/selected、面板 normal/elevated/danger/reward、锁定槽状态映射。
- `layouts`：入口、地图/详情、简报、设置、档案、确认/提示、结算/奖励卡、战斗页眉、右侧四模块、底部指令区与胜负页主矩形。
- `palette`：煤黑表面、冷青、安全黄、灰绿、危险红、正文与弱化文字的稳定颜色。
- `OccPixelUiConfig` 严格读取并校验 schema、1920×1080、1440/480 分区、逻辑像素倍率、重复键、状态引用和矩形尺寸；缺失映射直接抛错，不提供静默 fallback。
- `FormalArtRegistry.UiSkins` 从同一份配置注册全部 UI 资源，避免配置与注册表双写。

## 页面组合

- 入口、档案、设置、简报、确认层、提示条、结算与胜负页均从配置读取主卡矩形。
- 地图页从配置读取状态栏、1392px 地图区和 456px 节点详情区。
- 战斗页将旧长栏拆为“选中单位/资源、目标预览、行动序列、现场记录”四个像素模块；底部拆为武器、术式、交互、物品四组，结束行动独立。
- 960×540 使用同一 1920×1080 参考画布等比缩放，并通过既有紧凑字号规则提高正文相对字号。

## 验收门禁

1. 30 张切片机器 QA 和 Unity importer 检查通过。
2. 配置完整性、状态映射、资源注册、1440/480 分区与必需页面布局 EditMode 测试通过。
3. 入口→继续/新局→地图/详情→简报→战斗→奖励/失败→档案/设置/确认在 1920×1080 与 960×540 可达、无关键遮挡。
4. Funplay 编译错误/警告 0，Console 项目错误 0，默认场景 `isDirty=false`，不保存场景。
