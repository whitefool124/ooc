# ART-ACADEMY-TERRAIN-37 学院九图空间结构与地材尺度二次迭代验证

**日期：2026-08-25**  
**结论：APPROVE（地面宏块与结构层）；地材交界压边为后续可选改进**

## 实机问题与修复

- 旧版 `32×32` 单格地材在约 128px 的玩法格中形成巨型砖块和逐格重复；夯土成为大面积同纹理色块。
- 首轮 `3×3` 宏块消除了单格重复，但道路过黑、夯土过橙，且遗迹坑洞每三格重复。
- 最终版为庭院／道路／遗迹／夯土各生产两张独立 `96×96` 宏块，以中明度暖灰和去饱和土色二次限色；按宏块坐标 A/B 交替，每格只采样宏块的连续 `1/3×1/3` 子区。
- 新增 `academy_cloister_wall_4x1` 与 `academy_broken_wall_3x1` 两项透明跨格结构，复用于多张地图，层级位于地面之上、单位之下，`raycastTarget=false`。

## 可复用性

- 地材宏块是主题级资产，不含关卡布局、单位、碰撞、节点或文字；同一资产可用于任意逻辑地图，只需按世界格坐标采样。
- 子区不作为独立 PNG 或独立正式资产，避免把一张拼板硬切为资产；Unity 只导入 8 张完整宏块。
- 九图仍由 `AcademyBattlefieldLayoutCatalog` 的地材族、模块坐标与结构放置构成，没有关卡专属整图。

## 视觉验收

- 九图接触：`UnityProject/Artifacts/Terrain37/NineMaps/terrain37_nine_maps_contact.png`。
- 每图均有 `1920×1080` 与 `960×540` 实机截图，位于 `UnityProject/Artifacts/Terrain37/NineMaps/`。
- 单格巨砖和三格周期重复已消除；庭院、门厅道路、遗迹与训练场能按材质和构图区分；单位、范围框、掩体与目标仍高于地面信息。
- 已知残余：石材／夯土等异材质相邻处仍使用硬切轮廓。后续若继续迭代，应生产透明压边、直段、内外角和端头覆盖层，不应再次增加底材噪声。

## 机器与 Unity 门禁

- `occ-art-contract-v1` 审计：PASS。
- 8 张 `multi_cell_ground_32` 与 2 张 `multi_cell_prop_32`：10/10 `validate_occ_art_asset.py` PASS。
- Unity Importer：10/10 Point／Clamp／PPU32／Uncompressed／无 mipmap；稳定 GUID 已写入 manifest。
- 专项 EditMode：`5/5 passed`。
- 全量 EditMode：`630/630 passed`。
- PlayMode：`1/1 passed`。
- 编译错误：0；Console error：0；dirty scene：0；未保存 `CombatPrototype.unity`。

## 素材来源

- 所有正式新素材均通过 Codex 内建直接图像生成逐项独立生产；没有使用本地生图工作台、localhost API、私有 relay 或概念拼板切片。
- 生产简报：`Worldbuilding/05_美术与音频/正式美术生产/M-A18/OCC_M-A18_学院结构层二次迭代简报_v0.1.md`。
- 原料、规范化、QA 与 manifest 分别位于 M-A18 的 `source/terrain_*`、`normalized/terrain_*`、`QA/terrain_*` 和 `manifests/terrain_*`。
