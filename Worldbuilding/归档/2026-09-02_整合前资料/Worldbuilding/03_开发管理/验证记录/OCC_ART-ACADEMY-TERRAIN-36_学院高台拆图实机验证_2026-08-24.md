# OCC ART-ACADEMY-TERRAIN-36 学院高台拆图实机验证

日期：2026-08-24  
结论：`academy_north_dais_6x2` 为 `REVIEW_READY`；完整地图构图仅为 `PROTOTYPE / BLUEPRINT ONLY`；未修改 Unity 正式资源。

## 本轮判断

- 第一版 12×9 地块接触图虽然机器无缝，但实机仍是空白平地与灰色 UI 条，人工审美失败。
- 完整学院构图原料证明连续墙体、短前立面、台阶、入口和边缘装饰方向成立；但视觉建筑与逻辑阻挡不一致，禁止直接导入正式战场。
- 手工 8×2 横栏式高台仍读作 UI 横条，已判 `FAIL` 并归档。
- 独立生成并规格化后的高台真实轮廓适合 6×2，占格改为 192×64；中央四级台阶、两侧墙沿和短立面在默认视图成立。

## 资产门禁

- 来源：Codex 内置图像生成的独立透明原料；未使用本地工作台、localhost 或私有 relay。
- 交付：`192×64`，`logical_cells=[6,2]`，硬 Alpha，仅 `0/255`，9 个可见色。
- 关键色：原料青色掩码在缩放后保留 41px；首次量化吞色被验证器拒绝，修复后复验 PASS。
- 证据：1×、4×、灰阶、棋盘格、离线 12×9 接触图、1920×1080 与 960×540 实机截图齐全。
- 正式状态：产品审美尚未确认，保持 `REVIEW_READY`；Unity 只放入 `Assets/Game/Validation/TerrainTilesetV12/`，未进入正式 Resources 路径。

## 实机与交互

- `Application.dataPath`：`E:/数据库/OCC_Codex/UnityProject/Assets`。
- 导入读回：Sprite Single、PPU32、Point、Clamp、Uncompressed、无 mipmap。
- 实机 1920×1080 与 960×540：高台轮廓、四级台阶、短立面和青色指示可读；单位、范围、掩体和 HUD 优先级仍高于建筑。
- 点击穿透：背景与高台 `raycastTarget=false`；真实鼠标点击命中 `格子_4_4`，角色从 `(1,4) / AP3` 移至 `(4,4) / AP2`。
- Console：无 error。场景：`Assets/Scenes/CombatPrototype.unity`，`dirty=false`，未保存。

## 后续门禁

下一批只允许补独立端墙、门洞、侧台阶与地面分区。每个建筑必须同时登记占用格、可走台阶和路线锚点；任何移动范围穿墙、单位站在立面、生成噪声或关键青色吞失都必须失败。
