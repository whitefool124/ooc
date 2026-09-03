# OCC ART-ACADEMY-TERRAIN-36 Tileset 方向审计（2026-08-24）

## 结论

- 旧连续斑驳材质方向：`PRODUCT_REJECTED / PROTOTYPE`。
- 新模块化 tileset 架构：`DIRECTION LOCK`。
- 第二张构图概念：`COMPOSITION PASS / PRODUCTION FAIL / CONCEPT ONLY`。
- 任务状态：保持 `IN PROGRESS / PRODUCT REWORK`，未宣称美术问题已修复。

## 参考审计所得规则

成熟正交像素战棋地图首先用可重复、可连接的地块语法表达地形类别；大面积可行走区域保持安静，细节集中于道路、墙缘、台阶、材质边界、建筑和少量多格地标。正式学院 tileset 因此必须具备直段、内外角、端头、T 口、十字和专用过渡件，不能继续依赖随机纹理遮盖拼接。

参考只用于提炼共性，不复制具体作品资产：Fire Emblem GBA 实机截图、Advance Wars 地图分析、Wargroove 官方实机页面，以及 Dark Deity 等正交战棋画面。

## 本轮产物

- 方向合同：`Worldbuilding/05_美术与音频/正式美术生产/M-A18/OCC_M-A18_学院像素战棋Tileset方向重制_v0.1.md`
- 概念参考：`Worldbuilding/05_美术与音频/正式美术生产/M-A18/alternates/terrain_tileset_direction_v01/academy_tileset_composition_concept_v01.png`
- 概念 SHA-256：`E39F714F367660F0D4F0FE4C85CC7D9EC1B55609EB88FF7C9750DBF37CBD153E`

概念参考通过非对称构图、安静地面比例、道路／墙缘／泥地／地标层级；未通过严格 `12×9×32`、低纹理密度、完整连接件与正式像素 QA。禁止切图或导入 Unity。

## Unity 基线验证

- `Application.dataPath`：`E:/数据库/OCC_Codex/UnityProject/Assets`
- 活动场景：`Assets/Scenes/CombatPrototype.unity`
- 编译：`0 error / 0 warning`
- 聚焦 EditMode：`OCC.Combat.Tests.CombatBattlefieldCellPresenterTests`，`4/4 passed`
- Console：无 error
- Dirty scenes：`0`
- 场景保存：未执行

## 下一验收点

独立生产安静基础面 A/B、两格宽道路直段／角／端头、石土直边／内外角，组成严格 `384×288` 的 `12×9` P0 接触图。只有接触图在远景类别、连接连续性、低噪点和单位／覆盖层承载能力上通过人工审美，才扩展完整词汇并接入运行时。
