# V2-19：中继站以太补给箱（关闭态）

- **归属：** 剧情模式与肉鸽模式共用战斗表现；静态交互物。
- **目标：** 验证 Codex 内建图像生成可直接作为 OCC 单一资产原料，并完成可复核的本地规范化与 QA；不导入 Unity，不替换现有 `loot_crate`。
- **生成工具：** Codex 内建 ImageGen。
- **原始提示词要点：** 单个关闭态工业以太补给箱；32×32 逻辑像素格、最多 16 色、正交俯视、煤灰/铁黑为主、少量安全黄和冷青回路；纯 `#00ff00` 绿幕；禁止文字、阴影、反射、UI、水印和拼板。

## 产物与处理

| 类型 | 路径 | 说明 |
| --- | --- | --- |
| Codex 原图 | `Props32/raw/aether_supply_crate_codex_raw.png` | 保留的单图原料，不直接进入 Unity。 |
| 去背中间文件 | `Props32/aether_supply_crate_keyed.png` | 使用内建工作流的 chroma-key 去背助手输出。 |
| 规范化 PNG | `Props32/aether_supply_crate_v02.png` | 第二次单图生成后，整画布最近邻采样至 32×32，无裁切、拟合或重定位。 |
| 4× QA | `QA/aether_supply_crate_v02/qa_4x.png` | 审查轮廓、硬 alpha 与画布边界。 |
| 调色板 | `QA/aether_supply_crate_v02/palette_4x.png` | 14 色可见色调色板。 |
| 报告 | `QA/aether_supply_crate_v02/report.json` | 机器可读 QA 结果。 |

## QA 结论

- **规格 PASS：** 32×32、14 个可见色、硬 alpha。
- **透明边界：** `[6, 8, 26, 23]`；四角透明，未见绿幕残留。
- **表面语义 ACCEPTED：** 按产品决定，箱体、锁扣和补给容器轮廓在 1×/4× 下均可读；冷青导管与安全黄锁扣的低饱和表现不再作为本项阻塞条件。
- **范围限制：** `FORMAL` 原料；尚未复制到 `UnityProject/Assets/`，未修改场景或运行时引用。导入必须另建任务并复核 Sprite / Point / Clamp / 无 mipmap。
