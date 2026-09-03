# OCC M-A33 生产与评审记录 v0.1

> 状态：`18/18 REVIEW_READY / PRODUCT REVIEW PENDING`，2026-08-30。  
> 总接触表：`QA/contacts/m_a33_production_contact.png`。  
> 其他接触：`m_a33_floor_family_contact.png`、`m_a33_semantic_material_contact.png`。

## 1. 生产结果

- 18 件资产全部在 manifest 建立后通过内置图像生成能力独立生产；未使用本地工作台、localhost、私有 relay 或回退渠道。
- 18/18 通过目标尺寸、硬 Alpha、调色板上限、可见边界、哈希和证据文件检查；合同单测 6/6、合同审计 PASS。
- 全部生成 1×、4×、灰阶、棋盘格和组级接触证据；地面额外生成 A–D 单格与 `12×9` 非周期铺设接触。
- 未向 `UnityProject/Assets/` 导入或写入本批资产；没有稳定 GUID、Importer 或运行时结论，因此不得标记 `FORMAL`。

## 2. 当前资产

| 组 | 资产 | 机器结论 | 内部规范复核 | 产品结论 |
| --- | --- | --- | --- | --- |
| 地面 | `harbor_inspection_floor_a` | PASS | 单一石板、安静磨损 | 待审 |
| 地面 | `harbor_inspection_floor_b` | PASS | v02 移除亮色按钮框 | 待审 |
| 地面 | `harbor_inspection_floor_c` | PASS | v02 降低噪点并统一边缘 | 待审 |
| 地面 | `harbor_inspection_floor_d` | PASS | v02 移除密集小砖 | 待审 |
| 物件 | `harbor_mooring_bollard` | PASS | 单格低障碍轮廓成立 | 待审 |
| 物件 | `harbor_cargo_handcart_2x1` | PASS | 2×1 低矮手推车 | 待审 |
| 物件 | `harbor_tally_desk_2x1` | PASS | 2×1 人工点货桌 | 待审 |
| 物件 | `harbor_cargo_rack_2x1` | PASS | v02 扩展为诚实两格宽 | 待审 |
| 物件 | `harbor_hand_crane_2x2` | PASS | 人工绞盘、无载荷 | 待审 |
| 结构 | `harbor_inspection_gate_3x2` | PASS | 中央通路与两侧阻挡可读 | 待审 |
| 语义 | `semantic_talk` | PASS | v02 简化为两张对向侧脸 | 待审 |
| 语义 | `semantic_examine` | PASS | 眼与证物点分离 | 待审 |
| 语义 | `semantic_handover` | PASS | 双手与交接物可分 | 待审 |
| 语义 | `semantic_relationship` | PASS | 无爱心，表达中性关系 | 待审 |
| 材料 | `waxed_field_bandage` | PASS | 布卷与蜡封 | 待审 |
| 材料 | `tarred_rope_coil` | PASS | 三圈焦油麻绳 | 待审 |
| 材料 | `salt_sample_sack` | PASS | 小型样袋而非钱袋 | 待审 |
| 材料 | `repair_pitch_jar` | PASS | 陶罐与深色修补料 | 待审 |

## 3. 拒绝版本

- `harbor_inspection_floor_b/v01`：亮度过高、内框和中心斑块读作按钮。
- `harbor_inspection_floor_d/v01`：密集小砖违反“一格一个主材料块”。
- `harbor_cargo_rack_2x1/v01`：有效轮廓过窄，无法诚实承担 2×1 占格。
- `harbor_inspection_floor_c/v01`：颗粒噪点偏多，边缘与同族不统一。
- `semantic_talk/v01`：16px 轮廓过密，出现多余上下形状。

这些版本只保留在 `rejected/` 追溯，不得用作提示词样本或生产资产。

## 4. 状态边界

M-A32 的“全部通过”授权了资产族进入生产；M-A33 是新的逐件输出，仍需产品逐件审核。当前可回复：`M-A33 全部通过`，或列出需返工的 stem。产品通过后，仍因本轮明确“不实装”而保持非 Unity 状态。
