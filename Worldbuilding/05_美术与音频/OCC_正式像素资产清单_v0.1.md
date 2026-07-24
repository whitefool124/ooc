# OCC 正式像素资产清单 v0.1

> 目的：把概念参考、原型占位和可进入正式版本的像素资产分开管理。

## QA 门槛

- `32x32` 图标/地块：固定尺寸、点过滤、整数缩放、硬边界透明、可读轮廓。
- `64x64` 单位：脚底基线约 `Y=58`、中心线约 `X=32`、轮廓可区分兵种，不使用 AI 拼板硬切。
- 动画：独立帧优先；必须有固定 cell、基线/中心线、透明边界、调色板和 QA 报告。
- 未通过 QA 的资源只能标记为 `PROTOTYPE` 或 `CONCEPT`，不得作为正式资产验收。

## 当前盘点

| 资产 | 尺寸 | 状态 | 说明 |
| --- | --- | --- | --- |
| `attack` | 32x32 | PROTOTYPE | Unity 内置点过滤图标，待轮廓/语义 QA |
| `interact` | 32x32 | PROTOTYPE | Unity 内置点过滤图标，待轮廓/语义 QA |
| `loot` | 32x32 | PROTOTYPE | Unity 内置点过滤图标，待轮廓/语义 QA |
| `move` | 32x32 | PROTOTYPE | Unity 内置点过滤图标，待轮廓/语义 QA |
| `skillOne` | 32x32 | PROTOTYPE | Unity 内置点过滤图标，待轮廓/语义 QA |
| `skillTwo` | 32x32 | PROTOTYPE | Unity 内置点过滤图标，待轮廓/语义 QA |
| 战斗地块切片 | 32x32 | MISSING | 尚无正式 12x9 切片集 |
| 主角/步枪兵/盾卫/火术师/精英 | 64x64 | MISSING | 尚无独立帧与基线 QA |

## V2-04 原料进度（2026-07-24）

| 资产 | 尺寸 | 状态 | 审查结论 |
| --- | --- | --- | --- |
| 5 指令图标 | 32x32（规范化审查副本） | `FORMAL` | 5 项独立原图、4x 网格 QA、16 色调色板预览和 JSON 报告均通过；以移动方向、攻击、技能、搜刮、互动的表面语义批准。 |
| 主角/步枪兵/盾卫/火术师/精英 | 64x64（规范化审查副本） | `FORMAL` | 5 项独立原图、`X=32` / `Y=58` QA 预览、24 色调色板和报告均通过；以主轮廓和装备特征的表面语义批准。 |
| 地块/轻掩体/重掩体/中继器/战利品箱 | 32x32（规范化审查副本） | `FORMAL` | 独立原图、4x QA、16 色调色板与报告齐全；以地面、掩体、设备、箱体的表面语义批准。 |

集中审查结果位于 `像素资产原料/V2-04/QA/OCC_V2-04_集中审查.md`。上述 `FORMAL` 是已批准的原料库资产，不代表 Unity 已导入；Unity 运行时替换须由 V2-05 单独评审。

## V2-05 首批 Unity 导入（2026-07-24）

| 资产 | Unity 路径 | 运行时用途 | 导入/运行时复核 |
| --- | --- | --- | --- |
| `move`、`attack`、`skill`、`loot`、`interact` | `Assets/Game/Resources/Art/FormalIcons32/` | `FormalCombatHud` 指令按钮；`skill` 供两格技能共用 | `32×32`、Sprite、Point、Clamp、无 mipmap；Play Mode 为 6 个按钮建立正式图标层 |
| `floor`、`light_cover`、`heavy_cover`、`relay`、`loot_crate` | `Assets/Game/Resources/Art/FormalRelay32/` | 运行时 `地图可视化` 地块/掩体/中继器；战利品箱用于既有 IMGUI 战利品容器 | `32×32`、Sprite、Point、Clamp、无 mipmap；Play Mode 复核 108 地块、2 轻掩体、2 重掩体、1 中继器，回退为 0 |
| `hero`、`rifleman`、`shieldguard`、`pyromancer`、`elite` | `Assets/Game/Resources/Art/FormalUnits64/` | 战斗网格的正式单位绘制；同系兵种复用相近静帧 | `64×64`、Sprite、Point、Clamp、无 mipmap；Play Mode 复核 5/5 可加载，主角/步枪兵/盾卫/火术师/精英及监工映射有效 |
| `raider` | `Assets/Game/Resources/Art/FormalUnits64/raider.png` | 突袭者的专用正式静帧 | `64×64`、Sprite、Point、Clamp、无 mipmap；Play Mode 确认 `突袭者` 映射到 `raider` |

旧 `Assets/Game/Art/UI/Icons32/*.asset` 继续是 `PROTOTYPE`，未被覆盖。中继站战利品箱已完成既有 IMGUI 战利品容器绘制接入；突袭者现在使用独立静帧。所有上述单位仍只具备静帧，动画须作为后续独立任务制作。

## V2-06 突袭者独立静帧（2026-07-25）

| 资产 | 原料/QA 路径 | 尺寸与状态 | 审查结论 |
| --- | --- | --- | --- |
| `raider` | `像素资产原料/V2-06/Units64/occ_unit_raider_v01.png` 与 `QA/occ_unit_raider_v01/` | `64×64`，原料库 `FORMAL` | 独立单图原料经硬 alpha、24 色调色板、`X=32` / `Y=58`、4x 审查图和 JSON 报告复核通过；已导入 `FormalUnits64/raider.png` 并通过运行时映射复核，动画仍缺失。 |

## V2-07 主角待机动画（2026-07-25）

| 资产 | 原料/QA 路径 | 尺寸与状态 | 审查结论 |
| --- | --- | --- | --- |
| `hero_idle_4f` | `像素资产原料/V2-07/QA/occ_hero_idle_4f/` | `4×64×64`，原料库 `FORMAL` | 4 张基于同一主角参考单独生成的输入，均经硬 alpha、24 色调色板、`X=32` / `Y=58`、GIF、4x QA 与 JSON 报告复核通过；可导入为 `256×64` 固定循环 strip。 |

| Unity 资产 | Unity 路径 | 运行时用途 | 导入/运行时复核 |
| --- | --- | --- | --- |
| `hero_idle_4f` | `Assets/Game/Resources/Art/FormalAnimations64/hero_idle_4f.png` | 已批准的独立帧审查样本，当前不作为运行时依赖 | `256×64`、Sprite、Point、Clamp、无 mipmap；因本地多帧一致性仍待成熟，运行时暂统一采用静帧与整像素微位移。 |

## V2-09 中继站静态地块变体（2026-07-25）

| 资产 | 原料/QA 路径 | 尺寸与状态 | 审查结论 |
| --- | --- | --- | --- |
| `floor_industrial`、`floor_rail`、`floor_warning` | `像素资产原料/V2-09/` | `32×32`，原料库 `FORMAL` | 3 张独立输入均经硬 alpha、16 色调色板、4x QA 和 JSON 报告复核通过；可导入中继站地图的既有地块 SpriteRenderer。 |

| Unity 资产 | Unity 路径 | 运行时用途 | 导入/运行时复核 |
| --- | --- | --- | --- |
| `floor_industrial`、`floor_rail`、`floor_warning` | `Assets/Game/Resources/Art/FormalRelay32/` | 普通地面、边界轨道、中心警戒区 | 三者均 `32×32`、Sprite、Point、Clamp、无 mipmap；Play Mode 验证 78 / 24 / 6 个既有格实例，回退 0。 |

## V2-10 中继器破损静态反馈（2026-07-25）

| 资产 | 原料/QA 路径 | 尺寸与状态 | 审查结论 |
| --- | --- | --- | --- |
| `relay_destroyed` | `像素资产原料/V2-10/Relay32/` 与 `QA/relay_destroyed/` | `32×32`，原料库 `FORMAL` | 独立原料经硬 alpha、16 色、4x QA 与 JSON 报告复核；仅用于 `TileState.IsDestroyed` 的目标反馈。 |

## V2-11 轻掩体破损静态反馈（2026-07-25）

| 资产 | 原料/QA 路径 | 尺寸与状态 | 审查结论 |
| --- | --- | --- | --- |
| `light_cover_destroyed` | `像素资产原料/V2-11/Covers32/` 与 `QA/light_cover_destroyed/` | `32×32`，原料库 `FORMAL` | 独立原料经硬 alpha、16 色、4x QA 与 JSON 报告复核；仅用于轻掩体 `TileState.IsDestroyed` 的静态反馈。 |

## V2-12 重掩体破损静态反馈（2026-07-25）

| 资产 | 原料/QA 路径 | 尺寸与状态 | 审查结论 |
| --- | --- | --- | --- |
| `heavy_cover_destroyed` | `像素资产原料/V2-12/Covers32/` 与 `QA/heavy_cover_destroyed/` | `32×32`，原料库 `FORMAL` | 独立原料经硬 alpha、16 色、4x QA 与 JSON 报告复核；仅用于重掩体 `TileState.IsDestroyed` 的静态反馈。 |

| Unity 资产 | Unity 路径 | 运行时用途 | 导入/运行时复核 |
| --- | --- | --- | --- |
| `light_cover_destroyed`、`heavy_cover_destroyed` | `Assets/Game/Resources/Art/FormalRelay32/` | 轻/重掩体耐久归零后的静态反馈 | 均 `32×32`、Sprite、Point、Clamp、无 mipmap；Play Mode 确认正常状态仍使用原掩体图，破损资源均可加载。 |

## V2-03 实测导入审查（2026-07-24）

| 资产 | 尺寸 | Unity 导入实测 | QA 结论 |
| --- | --- | --- | --- |
| `attack`、`interact`、`loot`、`move`、`skillOne`、`skillTwo` | 32x32 | `RGBA32`、`Point`、`Clamp`、`mipmapCount=1` | 尺寸与像素过滤合格；缺少独立原料、轮廓/语义、透明边界和调色板审查，全部维持 `PROTOTYPE` |
| 战斗地块切片 | 32x32 | 未发现可审查的正式切片 | `MISSING` |
| 主角/步枪兵/盾卫/火术师/精英 | 64x64 | 未发现独立帧、中心线或脚底基线报告 | `MISSING` |

审查方法、状态定义、提交物和 V2-04 逐项清单见 `OCC_像素资产_QA流程_v0.1.md`。`mipmapCount=1` 是单层纹理本身的结果，不表示生成了 mipmap；导入正式 PNG 后仍须在 Unity 中复核关闭 mipmap。

## 下一批正式制作优先级

1. 战斗指令图标：移动、攻击、技能、搜刮、互动。
2. 单位静帧：主角、步枪兵、盾卫、火术师、精英。
3. 12x9 中继站地块切片与中继器、轻掩体、重掩体、战利品箱。
4. 命中、受击、击破的 4--10 帧低成本像素动画。
