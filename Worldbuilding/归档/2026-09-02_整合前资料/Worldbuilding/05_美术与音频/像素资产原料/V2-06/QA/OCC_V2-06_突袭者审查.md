# OCC V2-06 突袭者独立静帧审查

- 审查日期：2026-07-25
- 范围：`occ_unit_raider_v01` 单张独立原料与其规范化静帧；不包含动画、其他兵种或场景改动。
- 原料：`../Units64/occ_unit_raider_v01.png`，由本地工作台单图生成，不是拼板切图。
- QA 输出：`occ_unit_raider_v01/frames/frame_00.png`、`qa_4x.png`、`palette_4x.png`、`report.json`。

| 检查项 | 证据 | 结果 |
| --- | --- | --- |
| 固定 cell | `report.json`：`64×64` | PASS |
| 中心/脚底对齐 | `normalizedBounds=[16,12,49,58]`；`qa_4x.png` 标出 `X=32` / `Y=58` | PASS |
| 透明边界 | 色键清除后使用硬 alpha；规范化帧以透明边界输出 | PASS |
| 调色板 | `palette_4x.png` 与复核结果为 24 色 | PASS |
| 表面语义 | 兜帽、短枪、轻装与便携以太罐形成突袭者轮廓；与步枪兵、盾卫、火术师、精英不同 | PASS |

## 审查结论

按当前“规格符合、表面含义可读”的批准标准，该独立静帧升为原料库 `FORMAL`，可进入 Unity 导入与运行时映射复核。它只是一张静帧；不得用它伪造动画。
