# OCC 20 件法宝图标 QA

- 方法：每件独立 ImageGen 母图 → 自动 32px 引导稿（不导入）→ 逐件像素级语义重绘 → 有限色板/硬 Alpha/轮廓/中心/基线 QA。
- 图像 QA：20/20 PASS；失败 0。
- Unity Importer QA：20/20 PASS；失败 0。
- Unity 正式目录：`Assets/Game/Resources/Art/FormalArtifactIcons32`；Importer 由 `FormalArtImportPostprocessor` 统一设为 Sprite / Point / Clamp / PPU32 / no mipmap。

| ID | 文件 | 中文名 | 色数 | 可见像素 | 重心偏移 | 底线 | 轮廓率 | 结果 |
| --- | --- | --- | ---: | ---: | --- | ---: | ---: | --- |
| F-T01 | `demolition_canister.png` | 炎脉封装筒 | 7 | 392 | [0.158, 2.717] | 27 | 1.00 | PASS |
| G-T01 | `aegis_fold.png` | 折盾匣 | 7 | 465 | [-0.042, 2.373] | 28 | 1.00 | PASS |
| G-T02 | `phase_spindle.png` | 移相线轴 | 8 | 456 | [0.493, 0.342] | 28 | 0.85 | PASS |
| G-T03 | `binding_frame.png` | 缚位框 | 6 | 616 | [0.5, 0.636] | 29 | 1.00 | PASS |
| G-T04 | `survey_lens.png` | 显迹测镜 | 6 | 415 | [-0.555, -0.698] | 29 | 0.97 | PASS |
| G-T05 | `field_siphon.png` | 以太虹吸泵 | 7 | 417 | [-1.399, 1.829] | 29 | 0.77 | PASS |
| G-T06 | `mending_lattice.png` | 复元编架 | 6 | 462 | [0.587, 0.541] | 29 | 0.93 | PASS |
| G-T07 | `cover_stamp.png` | 掩体压模 | 5 | 649 | [0.5, 1.602] | 29 | 1.00 | PASS |
| G-T08 | `breach_wedge.png` | 解构楔 | 5 | 428 | [1.299, 2.93] | 29 | 0.78 | PASS |
| G-T09 | `relay_compass.png` | 导位罗盘 | 6 | 540 | [0.537, 0.726] | 30 | 0.90 | PASS |
| G-T10 | `reaction_bell.png` | 截击铃 | 7 | 514 | [0.44, 1.208] | 29 | 0.79 | PASS |
| G-T11 | `hazard_condenser.png` | 险地冷凝器 | 7 | 422 | [-0.206, 2.211] | 29 | 0.67 | PASS |
| G-T12 | `turn_ledger.png` | 行程簿 | 7 | 502 | [1.08, 0.129] | 29 | 0.93 | PASS |
| G-T13 | `anchor_brace.png` | 定锚支架 | 6 | 317 | [0.472, 3.708] | 30 | 1.00 | PASS |
| G-T14 | `prism_regulator.png` | 棱返调节器 | 6 | 589 | [0.5, 0.5] | 30 | 1.00 | PASS |
| G-T15 | `decoy_lantern.png` | 诱导灯 | 7 | 414 | [0.512, 1.068] | 30 | 1.00 | PASS |
| G-T16 | `shield_balancer.png` | 护盾均衡阀 | 7 | 493 | [0.504, 0.22] | 29 | 0.77 | PASS |
| G-T17 | `seismic_plumb.png` | 震测铅锤 | 6 | 459 | [0.5, 0.517] | 29 | 0.53 | PASS |
| G-T18 | `null_veil.png` | 静默幕 | 7 | 540 | [0.5, -0.761] | 29 | 0.99 | PASS |
| G-T19 | `fortune_seal.png` | 冒险封签 | 7 | 571 | [0.5, 0.516] | 30 | 1.00 | PASS |

## 统一门禁

- 32×32 RGBA；Alpha 仅 0/255；最多 16 个可见色；四角透明且不触边。
- 重心相对 (15.5,15.5) 的水平偏移不超过 2.5 px、垂直偏移不超过 4 px；图标最低像素位于 Y=27–30。
- 主连通轮廓占比至少 72%；外轮廓深色覆盖率至少 42%；20 个 SHA-256 唯一。
- Unity Importer：Sprite / Single / Point / Clamp / PPU32 / alphaIsTransparency / no mipmap。
