# OCC V2-11 轻掩体破损静态反馈审查

- 审查日期：2026-07-25
- 输入：本地工作台独立生成的 `Covers32/light_cover_destroyed.png`，不是拼板切图。
- 输出：`light_cover_destroyed/frames/frame_00.png`、`qa_4x.png`、`palette_4x.png`、`report.json`。

| 检查项 | 结果 |
| --- | --- |
| 固定尺寸 `32×32` | PASS |
| 硬 alpha / 透明边界 | PASS |
| 调色板 `≤16` 色 | PASS（16 色） |
| 表面语义 | PASS；低矮弯折金属、裂口与锈红应力痕迹可读，仍明显低于重掩体体量 |

## 审查结论

按“规格符合、表面含义可读”标准升为原料库 `FORMAL`，仅允许作为轻掩体耐久归零后的静态反馈贴图。
