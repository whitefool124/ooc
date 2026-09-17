# OCC 生图提示词（2026-09-16 定案版）

配套文件：`Tools/OCCArt/native32_generation_profiles.json`、`occ_modular_ground_batch_contract_v1.json`（v5）、
`occ_art_contract_v1.json`、`README.md`。**只用 Codex 内置 image_gen**。

主题（日常西幻魔法学院庭院）：`theme_id=academy_court`、`material_family=slate`、
`base_palette=#9D9884 #898676 #666459 #B4AE96`、`wetness=1`、`wear=1`、
`motif=brick_joint`、`edge_profile=low_curb`。

---

## A. 地面方格 32×32（当前要做的）

**英文提示词（直接喂给 image_gen）**

```
Single isolated battlefield floor square for a near-modern aether-industrial tactical map.
Orthographic top-down view. The subject is exactly one square 32x32 gameplay cell, complete
and self-contained — one whole paving square, not a crop or a corner of a larger floor.

Material: warm grey-beige cut stone paving laid in square slabs about 8 pixels across with
1-pixel seams. Colour direction: dominant stone tone around #9D9884, seams around #666459,
a lighter top lip around #B4AE96. Near-flat field: the dominant stone tone covers about 85
percent of the square, with at most three small connected darker patches. Three colours or
fewer in total.

The square has a clearly visible 1-pixel border on all four edges, drawn in the stone's own
darker tone — dark grey-brown, never black and never pure black — plus a slightly lighter
1-pixel lip just inside that border so the square reads as a slightly raised block. All four
edges are identical, so neighbouring squares join into a single seam line.

Flat even illumination: no directional scene light, no cast shadow, no glow, no vignette,
no darkening at the edges, no baked lighting of any kind.

Authentic coarse low-resolution pixel art shown enlarged, one uniform square macro-pixel
lattice across the whole tile, each macro pixel becoming exactly one final pixel: large
deliberate clusters, crisp hard edges, sparse controlled detail. No anti-aliasing, no blur,
no gradient, no dithering, no microtexture, no isolated single-pixel speckles, no
high-frequency noise.

Opaque background. No neighbouring tiles, no board or contact sheet, no objects, no
characters, no text, no UI, no perspective, no facade.
```

**负向（同一次生成里声明）**：`board, contact sheet, neighbouring tiles, cross-cell continuation, perspective, facade, bright corner hotspot, baked light pool, baked cast shadow, vignette, microtexture, dither, evenly spread palette steps, anti-aliasing, gradient, text, UI, characters, objects`

---

## B. 前立面 32×8（和方格配套的立体感素材）

```
Single isolated front-face strip for the same warm grey-beige stone paving square: a 32x8
pixel band showing the tile's thickness seen from the front. One flat face tone, clearly
darker than the stone top (dark warm grey-brown), with at most two connected darker bands.
The topmost 1-pixel row repeats the square's border tone so the face joins the border
without a seam. Authentic coarse pixel art, one uniform square macro-pixel lattice, crisp
hard edges. Flat even illumination: no cast shadow, no glow, no gradient, no dither, no
anti-aliasing. Opaque background, no other elements, no neighbouring tiles, no text.
```

---

## C. 人形单位 32×64（比例基准：火法师）

比例已定案：**主体 46–58px（= 1.4–1.8 格）**，基准是火法师 51px = 1.59 格。

```
Single isolated side-view humanoid unit sprite for a near-modern aether-industrial tactics
game. Strict 90-degree side profile: only one eye may be visible; torso, pelvis, feet and
equipment all face the same side.

Proportion is the approval criterion: the figure's body is about 51 pixels tall on a 32x64
sprite, so it must read as clearly taller than one 32-pixel ground cell and shorter than
two — the upper body deliberately overhangs the cell it stands in. Keep the whole texture
and a clean foot anchor at the bottom of the canvas.

Authentic coarse low-resolution pixel art shown enlarged, one uniform square macro-pixel
lattice across the whole figure, large deliberate clusters, crisp hard edges, limited
cohesive palette (32 colours or fewer). Flat even illumination with no directional scene
light and no cast shadow; the only shading is the fixed upper-left form shading on solid
volumes. No anti-aliasing, no blur, no gradient, no dithering, no microtexture, no muddy
clusters.

Opaque background. Reject: three-quarter body, front-facing torso, oversized head hiding
the body, equipment hiding the body, broken silhouette.
```

---

## D. 给 Codex 的完整作业指令（中文，和上面任一条一起发）

```
读 Tools/OCCArt/README.md、native32_generation_profiles.json、
occ_modular_ground_batch_contract_v1.json、occ_art_contract_v1.json 后按它们执行。

【本轮只做一块 32×32 地面方格 surface a】，不要一次生成整套。

硬约束：
1. 只用 Codex 内置 image_gen，禁止 EasyCLI 或任何本地图像服务。
2. 32×32、一格一张、自成一体（不是从大片地面裁一块）。
3. 自带明显格边框：四边各 1px、本材质自身暗色（不得纯黑）、四边一致、相邻拼接重合为一条缝；
   边框内侧 1px 受光唇线读作高度。
4. 近平面：主色 ≥75%（目标 80–88%）、色数 ≤3、孤立单像素 ≤0.5%、不要均匀色阶/抖色。
   参照基准（已通过）：ArtSource/sephiria_style_study_2026-09-16/project_assets/approved_ground_stone_16_at6x.png
   （2 色、主色 87.9%、0 孤立像素）。
5. 石材板距 8px、缝 1px；不含场景光照；硬 Alpha；宏像素格置信度 ≥1.2。
6. **解码后的设计场不得超过交付尺寸的 1.4 倍**（各轴），超了直接判失败重来——
   上一版就是从 34×33 / 106×106 / 158×157 的大设计里裁了 16×16，所以看起来"不是那块尺寸的风格"，
   这次是 32×32，设计本身就必须是 32 像素见方。

六步，逐步落文件：
1 生成独立原料 → 2 decode_generated_pixel_grid.py（--ground-window 32x32）
→ 3 出证据 1x/4x/6x/灰阶/棋盘格/应用接触 → 4 写 manifest
→ 5 跑 validate_occ_art_asset.py 与 verify_ground_tile.py --paving
→ 6 停下给我复核，不要自己宣布通过、不要导入 Unity。

汇报四行：机器校验结果 / verify_ground_tile.py 八项结果 / 证据图路径 / 你认为最可能被驳回的一点。
```

---

## E. 验收硬数字（复核时对照）

| 项 | 阈值 | 依据 |
| --- | --- | --- |
| 尺寸 | 32×32 | 一格一张 |
| 色数 | ≤6，铺装目标 ≤3 | 已通过基准 2–4 色 |
| 主色占比 | ≥75%（目标 80–88%） | 已通过基准 75.3% / 87.9% |
| 孤立单像素 | ≤0.5% | 已通过基准 0.49% / 0% |
| 格边框 | 存在、外圈较暗、非纯黑 | 定案要求"边框明显" |
| 解码设计场 | ≤1.4× 交付尺寸 | 16px 试验的失败根因 |

命令：

```powershell
& 'C:/Users/FNHF/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' `
  Tools/OCCArt/verify_ground_tile.py path/to/square_32.png --paving
```
