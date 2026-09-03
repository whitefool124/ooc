# OCC M-A18 学院 Tileset P0 第一批生产简报 v0.1

> 状态：`PRODUCTION / QA_PENDING`  
> 归属：`ART-ACADEMY-TERRAIN-36`  
> 方向：`学院清晰型`  
> 批次：`terrain_tileset_v12`

## 1. 应用环境与屏幕占位

- 战场为严格 `12×9` 正交方格，逻辑格 `32×32`。
- 默认 128px 格显示 4×；最小 64px 格显示 2×；另验 96px 与 160px。
- 地面在单位、交互物、范围和目标覆盖层之下；禁止常驻战斗格边框。
- 纯地板原料以 16×16 逻辑像素密度生产，最近邻 2×交付为 32×32；道路与石土边界以原生 32×32 生产。

## 2. 一秒玩家读法

玩家首先读出：浅石灰色区域可正常行走；偏灰且有明确边缘的两格宽带是学院维护通道；土褐区域是训练泥地。局部石纹、磨损和细节不承担玩法分类。

## 3. 材质、形状与密度合同

- 浅石庭：2–4 色，低对比，大连续像素簇；不得出现孤立点、裂纹、污渍或一格一块完整石板。
- 维护通道：比基础面低一档明度，以两条克制的边缘带和大块灰石面读取；连接端固定在格边中段，直段、转角、端头使用同一宽度与边缘相位。
- 训练泥地：土褐大块面，边界用浅石收口；泥地内部不撒砂砾，边界件只表达直边或明确转角。
- 光向：左上方环境光；地面没有软投影、渐变或高光点。
- 颜色：石灰、暖灰、土褐为主体；本批不使用冷青、锈红或安全黄。

## 4. 第一批独立资产

| 资产 ID | 源密度 | 玩家职责 | 连接约束 |
| --- | --- | --- | --- |
| `academy_courtyard_base_a` | 16→32 | 安静浅石庭主面 | 四边同相位、可与 A/B 任意拼接 |
| `academy_courtyard_base_b` | 16→32 | 轻微整体明度变化 | 四边同相位、不得新增独立纹样 |
| `academy_aisle_straight` | 32 | 已否决的半格线状通道原型 | `PROTOTYPE / FAIL`，读成沟槽而非道路 |
| `academy_aisle_corner` | 32 | 已否决的半格线状转角原型 | `PROTOTYPE / FAIL` |
| `academy_aisle_end` | 32 | 已否决的半格线状端头原型 | `PROTOTYPE / FAIL` |
| `academy_aisle_base_a` | 32 | 维护通道安静内部 | 四边同相位，只用于边界围合后的道路区域 |
| `academy_aisle_edge_straight` | 32 | 石庭与道路直边 | 北石南路；旋转产生四向 |
| `academy_aisle_edge_outer_corner` | 32 | 道路区域凸角 | 东南为路，西北为石 |
| `academy_aisle_edge_inner_corner` | 32 | 道路区域凹角 | 仅西北为石，其余为路 |
| `academy_earth_base_a` | 16→32 | 训练泥地安静内部 | 四边同相位，只用于边界围合后的土区 |
| `academy_earth_edge_straight` | 32 | 石庭与泥地直边 | 北石南土；旋转产生四向 |
| `academy_earth_edge_outer_corner` | 32 | 土区凸角 | 东南为土，西北为石 |
| `academy_earth_edge_inner_corner` | 32 | 土区凹角 | 仅西北为石，其余为土 |
| `academy_seal_court_2x2` | 64×64 | 学院战场单一视觉锚点 | 透明安全边；旧铜校准刻阵；不承担玩法格边界 |

道路和石土边界各锁一套基准方向，运行时仅做 90° 离散旋转。道路是区域式 autotile；排水和以太才使用直段／端头／T／十字线连接器。不得用自由旋转或缩放补接口。

## 5. 生成提示词合同

所有资产使用内置图像生成，每次只生成一个独立源图。参考概念图只提供学院石材、旧铜和低密度层级，不作为切片来源。

### 5.1 纯地板共同提示词

`In-game production raw for one quiet academy courtyard floor tile; strict orthographic top-down; one seamless square tile filling the entire canvas; designed on a native 16x16 logical pixel grid for nearest-neighbour 2x delivery; pale warm limestone, two to four discrete flat colours, large deliberate square pixel clusters, extremely low contrast and low detail; material modules continue beyond the canvas and do not form one complete slab inside the tile; all four outer edges share a plain compatible phase. No object, border, grid, isolated pixel, crack, stain, pebble, symbol, shadow, gradient, antialiasing, perspective, text or UI.`

`base_b` 只允许整面轻微变亮或变暗，不改变边缘相位。

### 5.2 维护通道共同提示词

`In-game production raw for exactly one native 32x32 orthographic tactical floor tile; pale limestone court with one authored maintained grey-stone aisle connector; hard square pixel clusters and four to six discrete flat colours; aisle width and connector endpoints are centered and consistent; large calm surfaces, restrained edge bands, no decorative bricks. The tile must depict only the requested connector orientation and fill the full square canvas. No grid, per-cell frame, cobbles, random cracks, stains, noise, bevel glow, soft shadow, gradient, antialiasing, perspective, text, UI or objects.`

- `straight`：南北贯通直段。
- `corner`：北边入口转向东边出口，宽度不变，具有清楚内角和外角。
- `end`：从北边进入并在格内南侧以平直石帽收口。

### 5.3 石土边界共同提示词

`In-game production raw for exactly one native 32x32 orthographic tactical terrain transition tile; quiet pale limestone and compact muted brown training earth separated by one authored hard pixel boundary; four to six discrete flat colours; broad calm material masses; exact requested straight or corner topology; full square canvas. No pebbles, grass, random speckles, footprints, crack, stain, grid, per-cell frame, soft shadow, gradient, antialiasing, perspective, text, UI or objects.`

- `straight`：北半石庭、南半泥地，边界水平贯穿。
- `outer_corner`：东南土区凸角，西北石庭包围。
- `inner_corner`：西北石庭凹入，其余为土区。

## 6. 排除项

- 不模仿或复制任何现有游戏的具体地块。
- 不生成 spritesheet、拼板、地图概念图或带标签的展示板。
- 不接受高分辨率写实纹理缩小、AI 随机砖纹、斑驳噪声和假像素边缘。
- 不接受接口宽度漂移、双边线、断口、每格闭合轮廓或四件放在一起才成立的图案。

## 7. QA 与晋级

1. 每项先建立 `occ-art-manifest-v1`，状态 `QA_PENDING`。
2. 原料保留提示词、来源、哈希；规范化脚本只能裁切、最近邻缩放、硬 Alpha、限色和清孤立点，不得新增结构。
3. 输出单项 1×、4×、灰阶、棋盘格；同族执行连接端逐像素矩阵。
4. 组合严格 `384×288` 的 12×9 P0 接触图，并生成 `64/96/128/160` 格四档预览。
5. 单项与接触图人工审美通过后标记 `FORMAL_CANDIDATE`；Unity 导入、稳定 GUID、Importer 与运行时截图全部通过后才标记 `FORMAL`。
