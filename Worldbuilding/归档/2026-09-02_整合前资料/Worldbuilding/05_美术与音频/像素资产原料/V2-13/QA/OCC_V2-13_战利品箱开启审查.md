# OCC V2-13 战利品箱开启静态反馈审查

- 审查日期：2026-07-25
- 输入：本地工作台独立生成的 `Relay32/loot_crate_open.png`，不是拼板切图。
- 输出：`loot_crate_open/frames/frame_00.png`、`qa_4x.png`、`palette_4x.png`、`report.json`。

| 检查项 | 结果 |
| --- | --- |
| 固定尺寸 `32×32` | PASS |
| 硬 alpha / 透明边界 | PASS |
| 调色板 `≤16` 色 | PASS（16 色） |
| 表面语义 | PASS；翻起箱盖、空内腔与少量残留物明确区别于关闭箱体 |

## 审查结论

按“规格符合、表面含义可读”标准升为原料库 `FORMAL`，仅允许作为战利品已领取后的静态反馈贴图。
