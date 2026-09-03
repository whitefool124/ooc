# OCC ART-COMBAT-SEMANTICS-56 战斗核心语义图标重制验证（2026-08-27）

## 结论

- 状态：`COMPLETE / FORMAL / QA_PASS`。
- 范围：6 张玩家指令、5 张敌方意图、6 张持续状态、14 张即时反馈，共 31 张。
- 玩法边界：只替换正式 PNG、资源路径和 Importer／审计接线；未改指令、AI、状态、伤害、VFX、数值、HUD 布局、占格、碰撞、寻路、存档或节点内容。
- Unity 操作只通过 Funplay；未进入 Play Mode，未保存场景。

## 生产与美术审查

- 31 项均先建 `occ-art-manifest-v1`，再通过 Codex 内建 imagegen 单件独立生成；无拼板切图、本地工作台、localhost、私有 relay 或自动回退。
- 指令与意图为原生 `16×16`、不超过 4 色；状态与反馈为原生 `32×32`、不超过 10 色。全部通过硬 Alpha、透明安全边、1×、4×、灰阶与棋盘格证据。
- 指令／意图强调单一功能轮廓；状态使用闭合、持续构图；反馈使用冲击、收束、崩解等瞬时构图。暖金、紫罗兰、灰绿、锈红、橙红、棕和赭黄承担主要功能区分，冷青只保留给位移，没有通用蓝晶模板。
- 1920×1080 使用 2×、960×540 使用 1× 的 Unity 实际接触通过；地图／HUD 比例为 75%／25%，检查了角落、边缘、重复节奏、单位遮挡、敌方意图、状态邻接和反馈密度。

## Unity 导入与运行时

- 身份门禁：`Application.dataPath=E:/数据库/OCC_Codex/UnityProject/Assets`；活动场景 `Assets/Scenes/CombatPrototype.unity`；开始时 `dirty=False`。
- 玩家指令迁入 `Assets/Game/Resources/Art/FormalCommandIcons16/`，`FormalArtRegistry.Commands` 改为对应 PPU16 路径；旧 `FormalIcons32` 保留给仍存在的物品 fallback，不删除。
- 5 张意图、6 张状态和 14 张反馈覆盖原正式 PNG，原 `.meta` 与 GUID 25/25 保持；6 张新指令获得独立 GUID。31 GUID 全部唯一。
- Resources 31/31 可加载；Sprite、Point、Clamp、Uncompressed、无 mipmap、PPU16／PPU32 31/31 通过。

## 自动验证

- `validate_combat_semantics_31.py`：31/31 PASS。
- `test_occ_art_contract.py`：6/6 PASS。
- Unity 聚焦 EditMode：3/3 PASS。
- Unity 全量 EditMode：650/650 PASS。
- 编译错误／警告：0／0。
- Console error／warning：0／0。
- dirty scenes：0。

## 证据

- 生产简报与 manifest：`Worldbuilding/05_美术与音频/正式美术生产/M-A24/`。
- 资产清单：`Worldbuilding/05_美术与音频/正式美术生产/M-A24/combat_semantics_31_catalog.json`。
- Unity 导入报告：`UnityProject/Artifacts/CombatSemantics31/unity_import_report.json`。
- 正式验证报告：`UnityProject/Artifacts/CombatSemantics31/validation_report_formal.json`。
- 1920×1080 接触：`UnityProject/Artifacts/CombatSemantics31/contacts/unity_combat_semantics_1920x1080.png`。
- 960×540 接触：`UnityProject/Artifacts/CombatSemantics31/contacts/unity_combat_semantics_960x540.png`。
- 前后对照：`UnityProject/Artifacts/CombatSemantics31/contacts/combat_semantics_before_after_1920x1080.png`。

## 后续

- 本批完成后停止，不自动扩展图标池。
- 后续若真实游玩出现单件误读，只返修对应 manifest 与 PNG；八元素或资源货币图标另立任务。
