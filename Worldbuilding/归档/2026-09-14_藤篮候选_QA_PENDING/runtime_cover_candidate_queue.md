# 学院运行时掩体候选队列

依据 `UnityProject/Assets/Game/Runtime/Presentation/AcademyBattlefieldLayoutCatalog.cs` 的 `LightCoverIds` 与 `HeavyCoverIds` 建立。此文件不改变玩法或资源绑定，只记录非覆盖式候选的生产顺序。

| 实际运行时槽位 | 类别 | 当前生产状态 | 下一步 |
| --- | --- | --- | --- |
| academy_prop_wicker_basket | 轻掩体 | FORMAL | 已导入；64×32 视觉画布底部中心锚定 |
| academy_prop_clay_jar | 轻掩体 | FORMAL | 已导入；保持单格逻辑占位 |
| academy_prop_coal_scuttle | 轻掩体 | FORMAL | 已导入；保持单格逻辑占位 |
| academy_prop_stone_planter | 轻掩体 | FORMAL | 已导入；保持单格逻辑占位 |
| academy_prop_tool_satchel | 轻掩体 | 已淘汰 | 三轮网格不达标；保留当前正式资源 |
| academy_prop_fire_bucket_stand | 轻掩体 | 已淘汰 | 三轮后网格或单格画布不达标；保留当前正式资源 |
| academy_prop_book_crate | 轻掩体 | FORMAL | 已导入；64×64 是视觉画布，不是双格占位 |
| academy_prop_scroll_case | 轻掩体 | FORMAL | 已导入；64×64 是视觉画布，不是双格占位 |
| academy_prop_folding_stool | 轻掩体 | FORMAL | 已导入；64×64 视觉画布保持单格逻辑占位 |
| academy_prop_rope_coil | 轻掩体 | FORMAL | 已导入；64×64 视觉画布保持单格逻辑占位 |
| academy_prop_practice_shields | 轻掩体 | FORMAL | 已导入；64×64 视觉画布保持单格逻辑占位 |
| academy_prop_medical_chest | 轻掩体 | FORMAL | 已导入；保持单格逻辑占位 |
| academy_prop_specimen_cage | 重掩体 | 已淘汰 | 三轮后无法同时满足单格画布与网格稳定门槛；保留当前正式资源 |
| academy_prop_oak_chest | 重掩体 | FORMAL | 已导入；64×64 视觉画布保持单格逻辑占位 |
| academy_prop_iron_locker | 重掩体 | 已淘汰 | 三轮网格或单格画布不达标；保留当前正式资源 |
| academy_prop_reagent_cabinet | 重掩体 | FORMAL | 已修正斜向候选并导入；保持单格逻辑占位 |
| academy_prop_field_lectern | 重掩体 | 已淘汰 | 用户授权的第四轮正向重生仍未通过网格与画布门槛；保留当前正式资源，后续不再重试 |
| academy_prop_gear_cabinet | 重掩体 | FORMAL | 已导入；64×64 视觉画布保持单格逻辑占位 |
| academy_prop_warding_post | 重掩体 | FORMAL | 已导入；保持单格逻辑占位 |
| academy_prop_sealed_trunk | 重掩体 | FORMAL | 已导入；保持单格逻辑占位 |

队列优先顺序遵循单格剪影复杂度、运行时已引用和不修改玩法的约束。十五项已部署资源均为单格 `[1,1]` 逻辑占位，运行时按原生 32 PPU 比例以所属格底部中心锚定；64px 宽的画布仅用于容纳剪影越界，不能改变 CoverType、选择、碰撞或格位映射。每项已保留稳定 GUID，并通过 Point/Clamp、32 PPU、无 mipmap／无压缩与 `Resources.Load` 复核。
