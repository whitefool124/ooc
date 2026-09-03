# ART-ACADEMY-MODULES-43 学院高复用模块组件验证

## 结论

本轮 18 件学院模块均由独立来源生成并完成规范化、机器验证、Unity Importer、稳定 GUID、12×9 运行时接触和人工审美复核，状态为 `FORMAL`。试装只增加表现层纹理选择与透明覆盖，不改地图定义、逻辑格、阻挡、碰撞、寻路、单位、敌群、数值、存档或节点内容；`CombatPrototype.unity` 未保存且最终 dirty scene 为 0。

## 资产清单与实机接触

| 类别 | 资产 | 原生尺寸 | 运行时接触 |
| --- | --- | --- | --- |
| 地面附属 | `academy_floor_drain_grate`、`academy_floor_maintenance_hatch`、`academy_floor_repair_plate`、`academy_floor_convergence_scribe` | 32×32 | 九图确定性透明覆盖；弱／强／首领双分辨率 |
| 单格场景物 | `academy_prop_wood_crate`、`academy_prop_iron_crate`、`academy_prop_instrument_rack`、`academy_prop_potion_case`、`academy_prop_maintenance_lamp`、`academy_prop_stone_bollard` | 32×32 | 仅替换已有轻／重掩体的完好态视觉，逻辑类别不变 |
| 多格结构 | `academy_workbench_2x1`、`academy_archive_cabinet_2x1`、`academy_pipe_service_rack_2x1`、`academy_aether_device_2x2` | 64×32／64×64 | `elite_foundry`、`depot_wreck`、`signal_hub` 的既有同等永久阻挡格 |
| 有向端头 | `academy_wall_end_n`、`academy_wall_end_e`、`academy_wall_end_s`、`academy_wall_end_w` | 32×32 | 四向独立源；全部 `QuarterTurns=0`，禁止旋转／镜像 |

应用接触总表：`UnityProject/Artifacts/ArtModules43/module43_application_contact.png`。代表地图双分辨率接触：`UnityProject/Artifacts/ArtModules43/module43_three_maps_contact.png`。前后对照：`UnityProject/Artifacts/ArtModules43/module43_before_after_contact.png`。

## 美术与机器门禁

- 18 份 manifest 位于 `Worldbuilding/05_美术与音频/正式美术生产/M-A18/manifests/academy_modules_v21/`；18/18 为 `FORMAL`。
- 18/18 通过 `Tools/OCCArt/validate_occ_art_asset.py`；机器合同审计 PASS。报告位于 `Worldbuilding/05_美术与音频/正式美术生产/M-A18/QA/academy_modules_v21/validator_reports/`。
- 1×、4×、灰阶、棋盘格证据齐全；尺寸严格为每格 32px，硬 Alpha、有限调色板与透明安全边通过。
- Unity Importer 18/18：Sprite、PPU 32、Point、Clamp、Uncompressed、mipmap 关闭；GUID 已写入各 manifest 的 `guid` 与 `stable_guid`。
- `rail_patrol`、`elite_foundry`、`core_finale` 在 1920×1080 与 960×540 下复核角落、边缘、重复节奏、左上光照方向、单位遮挡、范围覆盖与交互提示，均通过。`signal_hub` 和 `depot_wreck` 补齐设备、南北端头和档案柜的实际 12×9 接触。

## Unity 与回归

- `Application.dataPath = E:/数据库/OCC_Codex/UnityProject/Assets`。
- 新增表现层映射不修改 `FirstRegionLevelDefinition`；测试明确验证九图逻辑定义在视觉模块接入前后不变。
- 相关 Presenter／布局专项：20/20 PASS。
- 全量 EditMode：647/647 PASS；全量 PlayMode：1/1 PASS。
- 编译：0 error／0 warning；Console：0 error／0 warning；dirty scenes：0。
- 全程未保存场景。

## 人工审美结论

地面附属保持低对比，不破坏独立地砖边缘；轻掩体仍以木／铁箱、器材与药剂器具为主，重掩体保持更深、更厚的外轮廓；工坊台、管线架、档案柜和以太设备在已有阻挡轮廓中提升区域识别。新增结构没有压过单位、生命／护盾条、移动范围、目标框或悬浮信息。首领图高密度格局仍可读，960×540 下未出现新增文字或交互溢出。

## 下一批建议

1. 优先独立生产门洞／台阶的 N／E／S／W 四向族，继续禁止带阴影资产旋转或镜像。
2. 补 4–6 件医务／郊野低构图权重识别件，例如折叠担架、封闭药柜、测绘桩与植被护框；继续复用现有碰撞语义。
3. 只有当现有玩法状态明确需要时，才为箱体／设备补损坏态；不以美术任务新增状态、碰撞或占格。
