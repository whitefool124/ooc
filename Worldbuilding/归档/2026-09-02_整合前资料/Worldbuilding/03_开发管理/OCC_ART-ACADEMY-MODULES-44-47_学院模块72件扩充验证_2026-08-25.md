# OCC ART-ACADEMY-MODULES-44~47 学院模块 72 件扩充验证

## 结论

PASS。四批共 72 件高复用学院地图组件已完成独立生成、规格化、机器验证、Unity 正式导入、12×9 应用接触、三张代表地图双分辨率复核与人工审美，全部晋级 `FORMAL`。与上一批 18 件累计为 90 件；按产品要求到此停止，不自动开启第五批。

本任务只改变表现层资源池和同语义视觉替换：未修改地图拓扑、逻辑格、占格、碰撞、寻路、敌群、单位、数值、节点、存档或玩法规则，未保存 `CombatPrototype.unity`。

## 资产清单

| 批次 | 数量 | 内容 |
|---|---:|---|
| 44 · 低矮地面附属 | 16 | `academy_floor_drain_round`、`drain_slot`、`service_hatch_round`、`service_hatch_square`、`repair_stone`、`repair_iron`、`cable_cap`、`pipe_socket`、`anchor_plate`、`inspection_window`、`mortar_inlay`、`threshold_studs`、`safety_marker`、`herb_drain`、`rain_channel`、`conduit_blank` |
| 45 · 单格场景物 | 20 | `academy_prop_wicker_basket`、`book_crate`、`scroll_case`、`tool_satchel`、`folding_stool`、`clay_jar`、`coal_scuttle`、`rope_coil`、`specimen_cage`、`practice_shields`、`oak_chest`、`iron_locker`、`stone_planter`、`reagent_cabinet`、`field_lectern`、`gear_cabinet`、`warding_post`、`fire_bucket_stand`、`medical_chest`、`sealed_trunk` |
| 46 · 多格结构 | 16 | `academy_teaching_desk_2x1`、`alchemy_bench_2x1`、`map_table_2x1`、`repair_bench_2x1`、`low_bookcase_2x1`、`specimen_counter_2x1`、`medical_cot_2x1`、`supply_rack_2x1`、`smithing_table_2x1`、`tool_cabinet_2x1`、`timber_barricade_2x1`、`stone_balustrade_2x1`、`aether_pump_2x2`、`archive_sorter_2x2`、`infirmary_station_2x2`、`sealing_apparatus_2x2` |
| 47 · 独立方向端件 | 20 | `academy_wall_corner_{nw,ne,se,sw}`、`wall_gate_{n,e,s,w}`、`stair_landing_{n,e,s,w}`、`wall_buttress_{n,e,s,w}`、`pipe_terminal_{n,e,s,w}` |

完整机器目录见 `Worldbuilding/05_美术与音频/正式美术生产/M-A18/tools/module_expansion_72_catalog.json`；72 份正式清单位于 `Worldbuilding/05_美术与音频/正式美术生产/M-A18/manifests/academy_modules_v22_25/`。

## 生产与规格

- 72 件均来自内建 imagegen 的独立单项调用；没有关卡整图、关卡专属大贴图、生成拼板切片、本地工作台、localhost API、私有 relay 或自动回退。
- 单格严格 `32×32`；`2×1` 严格 `64×32`；`2×2` 严格 `64×64`。全部硬 Alpha、Point 采样、PPU32、Clamp、Uncompressed、无 mipmap。
- 地面附属保持自包含边缘和至少 2px 安全边，不依赖邻格；带方向阴影的 20 件端件分别生产，运行时不旋转、不镜像。
- 首次 `academy_medical_cot_2x1` 原料为空白，被拒绝且重新独立生成后才进入规格化。

## Unity 接触与审美

- 72 件九页 12×9 接触：`Worldbuilding/05_美术与音频/正式美术生产/M-A18/QA/academy_modules_v22_25/academy_modules_72_unity_12x9_contact.png`。
- 三张代表地图双分辨率：`UnityProject/Artifacts/ArtModules72/module72_three_maps_contact.png`。
- 与上一批运行时基准的前后对照：`UnityProject/Artifacts/ArtModules72/module72_before_after_contact.png`。
- 结果：地面附属保持低矮，单格轻／重掩体轮廓可分，多格设备体量清楚；教学、工坊、医务、郊野、封存区出现可读差异但仍共享石材、旧木、锻铁、旧铜与克制以太青语言。角落、边缘、重复节奏、左上阴影、单位遮挡、青色范围框及交互提示均通过。

## 最小表现层接线

- `AcademyBattlefieldLayoutCatalog` 的地面附属仍只占用四个既有纯视觉槽位。
- Light／Heavy 外观只在已经具有对应 CoverType 的格上选择，不改变掩体类别或耐久。
- `signal_hub`、`elite_foundry`、`depot_wreck` 只用同宽高新图替换已有永久阻挡视觉。
- 20 件方向端件保留为可复用正式池和接触证据，本轮不强行铺入关卡，不存在旋转复用。

## 验证结果

| 项目 | 结果 |
|---|---|
| `validate_occ_art_asset.py` | `72/72 PASS` |
| 机器合同审计 | `PASS`，0 error |
| 正式结构目录 Importer | `97/97 PASS`，0 bad |
| 稳定 GUID | `72/72` 由 AssetDatabase 移动保留并写入 manifest |
| 聚焦 EditMode | `3/3 PASS` |
| 全量 EditMode | `648/648 PASS`，0 fail／skip |
| PlayMode | `1/1 PASS` |
| 编译 | 0 error／0 warning |
| 最终 Console | 0 error／0 warning |
| 场景 | `Assets/Scenes/CombatPrototype.unity`，dirty `false`，未保存 |
| Unity 身份 | `Application.dataPath = E:/数据库/OCC_Codex/UnityProject/Assets` |

## 后续建议

停止批量扩充。只有真实地图接触暴露明确缺口时才立定点任务：

1. 已有玩法出现损坏、开启／关闭或激活／失活状态后，再补对应状态图；不预造无逻辑来源的状态。
2. 新地图确有 `3×1`、L 形或更大占格并已登记同等阻挡语义时，再补该尺寸结构。
3. 若郊野或医务区仍不够易读，优先替换当前池中的低收益件，不继续无上限增加数量。
