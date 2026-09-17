# OCC 生图提示词（2026-09-16 定案版）

配套：`Tools/OCCArt/README.md`、`native32_generation_profiles.json`、
`occ_modular_ground_batch_contract_v1.json`（v5）、`occ_art_contract_v1.json`。
**只用 Codex 内置 image_gen。**

## 提示词写法原则（重要）

**写死的**：交付尺寸与 PPU、画布里各部分的分工、边框/接缝这类结构要求、
不含场景光照、体积明暗方向、像素画语言（单一宏像素格、无抗锯齿/渐变/抖色）、
调色板上限、禁止出现的元素。

**不写的**：砖有几块、每块多大、主色占百分之几、哪里有磨损——
这些交给模型自由发挥。数字阈值放在**验收门禁**里，不进提示词。

理由：写太细会让模型去"凑描述"而不是画一块自然的材质，反而弄巧成拙。

---

## A. 地面方格 32×32

```
Single isolated battlefield floor square for a near-modern aether-industrial tactical map.
Orthographic top-down view. Exactly one square 32x32 gameplay cell, complete and
self-contained: one whole paving square, not a crop or a corner of a larger floor.

Warm grey-beige cut stone paving.

Hard requirements:
- a clearly visible 1-pixel border on all four edges, drawn in the stone's own darker tone,
  never black; all four edges identical so neighbouring squares join into a single seam line
- a slightly lighter 1-pixel lip just inside the border, so the square reads as a slightly
  raised block
- the field itself stays quiet and low-contrast, with one stone tone clearly dominant

Flat even illumination: no directional scene light, no cast shadow, no glow, no vignette,
no darkening at the edges, no baked lighting of any kind.

Authentic coarse low-resolution pixel art shown enlarged, one uniform square macro-pixel
lattice across the whole tile, each macro pixel becoming exactly one final pixel: crisp hard
edges, deliberate clusters. No anti-aliasing, no blur, no gradient, no dithering, no
microtexture, no isolated single-pixel speckles.

Opaque background. No neighbouring tiles, no board or contact sheet, no objects, no
characters, no text, no UI, no perspective, no facade.
```

**负向**：`board, contact sheet, neighbouring tiles, cross-cell continuation, perspective, facade, bright corner hotspot, baked light pool, baked cast shadow, vignette, microtexture, dither, evenly spread palette steps, anti-aliasing, gradient, text, UI, characters, objects`

---

## B. 前立面 32×8（地面方格的立体感素材）

```
Single isolated front-face strip for the same warm grey-beige stone paving square: a 32x8
pixel band showing the tile's thickness seen from the front. One flat face tone, clearly
darker than the stone top, with the topmost 1-pixel row repeating the square's border tone
so the face joins the border without a seam.

Flat even illumination: no cast shadow, no glow, no gradient, no dither. Authentic coarse
pixel art, one uniform square macro-pixel lattice, crisp hard edges. Opaque background, no
other elements, no neighbouring tiles, no text.
```

---

## C. 不可破坏地形（永久墙）32×32 ★本轮要做

**一个铺满整格的柱状体**：方形顶面 + 朝屏幕下方的前立面，左右略内收读出"柱"的体积；
连排即成墙。不能是贴边的矮墙条，不能有透明空档。

```
Single isolated indestructible terrain block for a near-modern aether-industrial tactical
map, orthographic top-down pseudo-2.5D view. One stone pillar: a single solid block that
completely fills one 32x32 gameplay cell, edge to edge, with no transparent gaps and no
overhang in any direction.

You see the block from above and slightly toward its front: a square top surface with a
front face turned toward the bottom of the screen, and the left and right sides slightly
inset so the shape reads as a standing stone column rather than a flat floor tile.
A pale weathered stone cap over a darker warm stone body. Same material family as the
academy courtyard paving (warm grey-beige stone).

This is indestructible terrain, not destructible cover: solid, thick, permanent masonry.
Nothing broken — no cracks, no chips, no rubble, no damage.

Hard requirements:
- fills the whole 32x32 cell; the four edges are edge-to-edge so identical neighbouring
  blocks join with no gap and no seam, and a run of them reads as a continuous wall
- flat even illumination: no directional scene light, no cast shadow on the floor, no glow,
  no vignette, no ambient darkening; the only shading is the fixed upper-left form shading
  (lit top surface, darker front face)

Authentic coarse low-resolution pixel art shown enlarged, one uniform square macro-pixel
lattice across the whole block, crisp hard edges, deliberate clusters, limited cohesive
palette.

Opaque background. Only this block: no floor tiles, no neighbouring pieces, no board, no
characters, no objects, no text, no UI, no perspective distortion.
```

**负向**：`transparent gaps, low wall strip, ledge, cracks, chips, rubble, damage, breakable cover, floor shadow, baked light pool, glow, vignette, dither, microtexture, anti-aliasing, gradient, text, UI, characters, objects, board, contact sheet`

---

## D. 人形单位 32×64（比例基准：火法师）

```
Single isolated side-view humanoid unit sprite for a near-modern aether-industrial tactics
game. Strict 90-degree side profile: only one eye may be visible; torso, pelvis, feet and
equipment all face the same side.

Proportion is the approval criterion: the figure must read as clearly taller than one
32-pixel ground cell and shorter than two, so its upper body deliberately overhangs the cell
it stands in. Keep the whole texture with a clean foot anchor at the bottom of the canvas.

Authentic coarse low-resolution pixel art shown enlarged, one uniform square macro-pixel
lattice across the whole figure, crisp hard edges, limited cohesive palette. Flat even
illumination with no directional scene light and no cast shadow; the only shading is the
fixed upper-left form shading on solid volumes. No anti-aliasing, no blur, no gradient, no
dithering, no microtexture, no muddy clusters.

Opaque background. Reject: three-quarter body, front-facing torso, oversized head hiding the
body, equipment hiding the body, broken silhouette.
```

---

## E. 给 Codex 的作业指令（中文，配上面任一条发送）

```
读 Tools/OCCArt/prompt_templates_2026-09-16.md、README.md、native32_generation_profiles.json、
occ_art_contract_v1.json 后执行。只用 Codex 内置 image_gen，禁止 EasyCLI 或本地图像服务。

【本轮只做一件】，不要一次生成整套。先按提示词生成独立原料，然后：
1 decode_generated_pixel_grid.py（地面用 --ground-window 32x32）
2 出证据 1x / 4x / 6x / 灰阶 / 棋盘格 / 应用接触
3 写 manifest（occ-art-manifest-v1 模板）
4 跑 validate_occ_art_asset.py 与对应验收脚本
5 停下给我复核，不要自己宣布通过、不要导入 Unity

验收脚本：
  地面方格  python Tools/OCCArt/verify_ground_tile.py <file> --paving
  永久墙    检查是否 32x40、硬 Alpha、色数 ≤6、左右边缘可拼接、无受损痕迹、无烘焙光照

启动要求：新生成必须换一张独立原料，不要在旧图上修补；最多 3 轮，只修被诊断出的那一维。
```

---

## F. 验收硬数字

| 项 | 阈值 | 依据 |
| --- | --- | --- |
| 地面方格 | 32×32，一格一张 | 地图编辑器按格放置 |
| 地面主色占比 | ≥75%（目标 80–88%） | 已通过基准 75.3% / 87.9% |
| 地面孤立单像素 | ≤0.5% | 已通过基准 0.49% / 0% |
| 地面格边框 | 存在、外圈较暗、非纯黑 | 定案"边框明显" |
| 前立面 | 32×8 | 与方格配套 |
| 不可破坏地形 | 32×32 **铺满整格**、全部像素不透明、四边可无缝连排、柱状体积、无裂纹破损 | 玩法含义是不可破坏地形 |
| 人形单位 | 32×64，主体 46–58px（1.4–1.8 格） | 火法师 51px = 1.59 格，已确认 |
| 解码设计场 | ≤1.4× 交付尺寸 | 16px 试验失败根因 |

```powershell
& 'C:/Users/FNHF/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' `
  Tools/OCCArt/verify_ground_tile.py path/to/square_32.png --paving
```
