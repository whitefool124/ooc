# OCC V2-10 中继器破损静态反馈审查

- 审查日期：2026-07-25
- 输入：本地工作台独立生成的 `Relay32/relay_destroyed.png`，不是拼板切图。
- 输出：`relay_destroyed/frames/frame_00.png`、`qa_4x.png`、`palette_4x.png`、`report.json`。

| 检查项 | 结果 |
| --- | --- |
| 固定尺寸 `32×32` | PASS |
| 硬 alpha / 透明边界 | PASS |
| 调色板 `≤16` 色 | PASS（16 色） |
| 表面语义 | PASS；破裂外壳、熄灭/断裂导管与少量锈红火花可读 |

## 审查结论

按“规格符合、表面含义可读”标准升为原料库 `FORMAL`，允许仅作为目标摧毁后的静态反馈贴图。
