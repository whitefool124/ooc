# OCC M-A18 学院高复用模块组件首批生产简报 v0.1

> 任务：`ART-ACADEMY-MODULES-43`  
> 类型：游戏内正式资产生产；先独立原料，后规范化、QA、Importer、12×9 实机接触与人工审美。  
> 玩家读取目标：一秒区分可走地面、低矮装饰、轻掩体、重掩体与设备／目标物，丰富地图但不压过单位、范围覆盖和交互提示。

## 1. 锁定视觉方向

- **轮廓：** 地面附属扁平并留出完整地砖边缘；单格物件控制在 26×26 安全框；多格结构严格占用声明的 `(W×32)×(H×32)` 画布并保留至少 2px 透明安全边；墙端头贴合既有学院墙体厚度。
- **材质故事：** 暖灰切石、深色锻铁、旧木、低饱和学院蓝灰与少量青色以太玻璃；是可维护、可测量、有磨损的近代魔法工业设施，不是中世纪雕花或蒸汽朋克管线堆砌。
- **调色角色：** 暖灰／旧木为主体，深铁作结构，青色只作低面积功能信号；不得用高饱和色与战术范围、选中态或交互提示竞争。
- **明度层级：** 单位与 HUD 最高；设备／目标物次高；掩体靠清晰外轮廓；地面附属最低。所有组件灰阶仍能读出高度与类别。
- **视角与光照：** 正交俯视像素战棋，轻微可见正面；统一左上光源，阴影落向右下。含有向阴影的组件禁止旋转、镜像；方向族独立生产。
- **密度预算：** 3–5 个主要像素簇、1–2 个识别细节；无散点噪声、无文字、无徽章、无微缩场景。

## 2. 第一批 18 件

| 类别 | 资产 ID | 尺寸／角色 | 玩家读取与复用条件 |
|---|---|---|---|
| 地面附属 | `academy_floor_drain_grate` | 32×32 / `single_cell_prop_32` | 非阻挡排水盖；完整独立边框，不接邻格 |
| 地面附属 | `academy_floor_maintenance_hatch` | 32×32 / `single_cell_prop_32` | 嵌入式维护口；低对比、可走 |
| 地面附属 | `academy_floor_repair_plate` | 32×32 / `single_cell_prop_32` | 修补板；不画战术格边框 |
| 地面附属 | `academy_floor_convergence_scribe` | 32×32 / `single_cell_prop_32` | 收束刻线；单格闭合、青色面积小 |
| 单格物件 | `academy_prop_wood_crate` | 32×32 / `single_cell_prop_32` | 旧木轻掩体；仅放既有轻掩体格 |
| 单格物件 | `academy_prop_iron_crate` | 32×32 / `single_cell_prop_32` | 锻铁重掩体；仅放既有重掩体格 |
| 单格物件 | `academy_prop_instrument_rack` | 32×32 / `single_cell_prop_32` | 教学／工坊器材；复用既有阻挡格 |
| 单格物件 | `academy_prop_potion_case` | 32×32 / `single_cell_prop_32` | 医务识别；复用既有箱体阻挡格 |
| 单格物件 | `academy_prop_maintenance_lamp` | 32×32 / `single_cell_prop_32` | 低矮维护灯；复用既有低掩体格 |
| 单格物件 | `academy_prop_stone_bollard` | 32×32 / `single_cell_prop_32` | 低矮石墩；复用既有低掩体格 |
| 多格结构 | `academy_workbench_2x1` | 64×32 / `multi_cell_prop_32` | 工坊台；只覆已有同等连续阻挡 |
| 多格结构 | `academy_archive_cabinet_2x1` | 64×32 / `multi_cell_prop_32` | 档案柜；只覆已有同等连续阻挡 |
| 多格结构 | `academy_pipe_service_rack_2x1` | 64×32 / `multi_cell_prop_32` | 管线维护架；只覆已有同等连续阻挡 |
| 多格结构 | `academy_aether_device_2x2` | 64×64 / `multi_cell_prop_32` | 以太设备／目标物；只覆已有 2×2 阻挡 |
| 定向端头 | `academy_wall_end_n` | 32×32 / `modular_structure_32` | 北向，独立左上光照，0° 使用 |
| 定向端头 | `academy_wall_end_e` | 32×32 / `modular_structure_32` | 东向，独立左上光照，0° 使用 |
| 定向端头 | `academy_wall_end_s` | 32×32 / `modular_structure_32` | 南向，独立左上光照，0° 使用 |
| 定向端头 | `academy_wall_end_w` | 32×32 / `modular_structure_32` | 西向，独立左上光照，0° 使用 |

## 3. 生成提示合同

每件调用 Codex 内置 image generation 独立生成，不用拼板、不从拼板切图。通用提示：

> Use case: stylized-concept. Asset type: independent production source for one OCC orthographic pixel-art tactical map module. Create exactly one isolated object, centered, transparent background, no text. Near-modern aether-industrial academy; warm cut stone, dark forged iron, aged wood, restrained desaturated blue-gray, tiny cyan aether-glass accent only when functional. Orthographic top-down with a slight readable front face, crisp pixel clusters, hard alpha, upper-left light and short lower-right shadow. Preserve a quiet value hierarchy below combat units and tactical overlays. No medieval fantasy ornament, no brass gear clutter, no steampunk boiler language, no soft antialiasing, no bloom, no perspective tilt, no UI border, no neighboring tiles, no watermark. The formal delivery will be normalized to the declared native cell footprint; keep silhouette simple enough to survive at 32px per cell.

逐件追加主体、尺寸和方向。例如地面附属要求“flush with the floor, self-contained perimeter, no mark reaches the canvas edge”；多格件明确 `2×1 (64×32)` 或 `2×2 (64×64)` 完整逻辑脚印；端头明确 N/E/S/W 几何方向并重申“do not rotate the shadow”。

## 4. 排除项与审核

- 禁止关卡整图、关卡专属大贴图、生成拼板切片、跨格纹理续接、逻辑格边框、方向阴影旋转／镜像。
- 地面附属不得假装阻挡、发光目标或可拾取物；掩体与设备不得超出声明脚印或遮住相邻单位核心轮廓。
- 目标尺寸检查：1× 轮廓、4× 像素簇、灰阶类别、棋盘格硬 Alpha、12×9 实际遮挡与重复节奏。
- 晋级顺序：`QA_PENDING` → 机器合同与人工审美通过后 `FORMAL_CANDIDATE` → 稳定 GUID、Importer、运行时复核后 `FORMAL`。
