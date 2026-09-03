# OCC M-A18 学院庭院材质尺度无缝返修 v0.1

**状态：FORMAL / QA PASS / RUNTIME COMPLETE（2026-08-23）。** 本批替换实机复核失败的 clean32 v07；保持 `academy_courtyard_a-d` 稳定键、路径、GUID、32×32 交付、PPU 与玩法不变。

## 1. 应用环境与玩家阅读

- 应用位置：12×9 正交战棋地图的一格纯地面；默认 1920×1080 时一格约 128px，960×540 时一格约 64px；单位、范围覆盖、掩体与生命条位于其上。
- 一秒阅读：安静、质朴、长期维护的学院浅暖灰切石地面；地面只提供尺度和材质，不描画逻辑战斗格。
- 失败原因：clean32 v07 把单块石板尺寸绑定为一格，实机成为 128px 巨型方板；下沿／右沿归属在变体混排时产生断口、L 角和格框感。

## 2. 锁定美术合同

- 正式画布仍为原生 32×32、全不透明、正交俯视。画布尺寸是交付格，不等于材质重复单元。
- 默认一格内读取为 2×4 个 16×8px 小切石模块；主要接缝位于格内，不与战斗格四边重合。石材模块跨格延续，禁止一格一板。
- 四张外缘必须共享兼容的连续石面：上下左右边界逐像素可任意 A–D 拼接，不得出现双线、断线、亮边、暗角或闭合格框。
- 使用浅暖灰石灰岩、一个低对比缝色和至多一个微弱整面明度层；不画污点、裂纹、划痕、徽记、圆角、倒角按钮框或孤立像素。
- 先在 1× 判断接缝节奏，再在 4×、12×9 混铺和真实 128px／64px 战斗格检查；单图好看不能替代混铺。

## 3. 正式生产提示词骨架

`Use case: stylized-concept; Asset type: one independently generated production source for an OCC 32x32 seamless academy floor tile; Primary request: quiet rustic western-fantasy academy limestone paving, smaller masonry modules crossing gameplay-cell boundaries; top-down orthographic, flat material only; warm light gray limestone, very low-contrast grout, large connected pixel clusters; the outer border is continuous stone surface and contains no frame or grout line; no single slab matching the canvas; no object, text, crack, stain, rune, bevel, rounded square, shadow, perspective, sci-fi detail or watermark.`

## 4. 放行门禁

- 每张独立原料、32×32 正式 PNG、1×／4×、A–D 全组合边缘矩阵、12×9 混铺、64／96／128／160 整数缩放、颜色／Alpha／孤立像素／边缘兼容报告齐全。
- Unity 读回必须为 Sprite／Point／Clamp／PPU32／Uncompressed／无 mipmap；稳定 `.meta` 不替换。
- 1920×1080 与 960×540 Play Mode 中不得再读成战斗格边框；范围框与地面接缝必须明显分层。

## 5. 生产与否决记录

- 直接图像渠道生成的 `academy_courtyard_master_a_source.png`／`master_b_source.png` 保留在 `raw/terrain_runtime_materialscale_v10/` 作为审计原料；规范化后出现斜向噪点与不稳定石缝，明确判为 `REJECTED / NOT FOR UNITY`，候选及 QA 已归档至 `rejected/terrain_runtime_materialscale_v10_generated_candidates_20260823/`。
- 正式 A–D 改由 `source/terrain_runtime_materialscale_v10/*.pxart` 逐像素明确绘制，不使用本地生图工作台、脚本几何部件或运行时绘制。四图均为原生 32×32、全不透明、2 色；每格含 2×4 个 16×8px 材质模块，石缝 1px，A–D 只在格内使用受控 1px 手工折线。
- 高频 `move_range`、`attack_range`、`selected` 同步改为 `source/tactical_overlay_edges_v10/semantic_inset_outline.pxart` 的一像素内缩方角边框；旧粗边／内缩缺口版本归档至 `rejected/tactical_overlay_edges_v03_application_rejected_20260823/`。该层只表达玩法语义，不属于地砖材质边缘。

## 6. QA 与 Unity 读回

- `academy_courtyard_family_report.json`：A–D 四个独立哈希，32×32，Alpha 仅 255，2 色，孤立像素 0；平均明度差 0.15；16 组有向横／纵边缘组合全部 0 mismatch，`all_variant_edges_exact=true`。
- 1×／4×、12×9 混铺与 64／96／128／160px 战斗格预览均通过；1920×1080 与 960×540 实机确认材质为 2×4 小石块节奏，范围边框为独立一像素语义层，无 L 角、双线、断缝或一格一板。
- Unity 保持 Sprite／Point／Clamp／PPU32／Uncompressed／无 mipmap；A–D 与三个高频语义框均保留原 `.meta`／稳定 GUID。Funplay 编译 0 error；专项 `19/19`、全量 EditMode `612/612`、PlayMode `1/1`；Console 无 error，`dirty scenes=0`，未保存场景。
