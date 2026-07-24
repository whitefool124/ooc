# OCC V2-09 中继站静态地块审查

- 审查日期：2026-07-25
- 范围：普通工业地面、轨道地面、警戒地面三张独立静态地块；不包含动画、整张地图或场景 YAML。
- 输入：`raw_tiles/floor_industrial.png`、`floor_rail.png`、`floor_warning.png`，分别由本地工作台独立生成，未从拼板硬切。
- 输出：每项均有 `frames/frame_00.png`、`qa_4x.png`、`palette_4x.png` 与 `report.json`。

| 资产 | 尺寸 / 色数 | 表面语义 | 结果 |
| --- | --- | --- | --- |
| `floor_industrial` | `32×32` / 16 色 | 深灰工业面板与受限冷青导管 | PASS |
| `floor_rail` | `32×32` / 16 色 | 垂直钢轨与枕木，作为边界路线 | PASS |
| `floor_warning` | `32×32` / 16 色 | 深灰板面与安全黄警戒边 | PASS |

## 审查结论

三项均使用硬 alpha、固定 cell 和点过滤目标规范化；按当前“规格符合、表面含义可读”标准升为原料库 `FORMAL`。可导入 Unity 并仅改变既有地图的视觉 Sprite，不改变地块数值、碰撞、目标或场景数据。
