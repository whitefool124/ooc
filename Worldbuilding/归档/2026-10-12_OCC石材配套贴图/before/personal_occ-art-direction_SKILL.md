---
name: occ-art-direction
description: "为 OCC 制定美术简报、像素资产提示词和审阅标准；以当前 OCC 总策划案及美术规格为准。"
---

# OCC Pixel Art Direction

For an art decision, read the relevant art/interface sections of `E:/数据库/OCC_Codex/Worldbuilding/策划案/OCC_项目总策划案_v1.0.md` and the matching rows of `Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv`. These supersede archived specifications. `Tools/OCCArt/occ_art_contract_v1.json` is a machine mirror, not a competing source.

Use [brief-contract.md](references/brief-contract.md) when drafting a brief or prompt; use [occ-pixel-contract.md](references/occ-pixel-contract.md) for production pixel assets. Read place/faction/unit sources only when their meaning affects the requested art.

Treat the project files as the source of truth. Do not invent a new genre, faction language, or colour meaning when the source is silent; ask one focused question or mark the choice as an exploration.

## HD-2D applicability

For migrated OCC HD2D scene work, read the project's `.codex/skills/occ-hd2d-quality/SKILL.md` and A.2.4. Noncombat HD2D text carriers and props use the authorized 2026-10-09 adaptive library exception: independent built-in generation, visible clusters/stepped silhouettes and provenance; alpha processing and delivery resizing allowed; no mandatory 1.2 lattice confidence, fixed palette or hard-alpha decode. The strict production steps below apply to battlefield assets and functional icons, not this library. Source lattice and scene high-resolution output are separate.

## Workflow

1. Classify the request: visual exploration, in-game production asset, UI concept, or VFX study.
2. State the player-reading goal in one sentence. Define the silhouette, material story, palette roles, value hierarchy, perspective, and density budget before writing a prompt.
3. Select the correct OCC role before prompting: `16×16` semantic icon; `24×24` material source centred in a `32×32` delivery; fixed `32×32` equipment, relic and spell icons; non-battlefield display objects on a complete-subject-plus-safe-border adaptive canvas (user confirmed 2026-10-03), judged by consistent style and natural UI scale; `32×32` ground cells and `32×40` directional outer edges; or 32 PPU units and transparent battlefield props on the smallest fitting 32-multiple canvas. Typical humanoids use `32×64`; an ordinary single-cell prop may use `64×64` visual canvas without changing its `[1,1]` logical footprint. Do not create a separate resolution companion.
4. Produce 2–3 deliberately different art directions only for exploration. For production, choose one approved direction and preserve its invariants.
5. Write a generator-ready prompt using the contract. Describe visual causes, not prestige labels: specify light source, wear, material, shape language, and composition. Do not imitate a living artist or name a game as a style shortcut.
6. For a generated production asset, describe identity, application, view, material story and a single uniform coarse square macro-pixel lattice. Do not micromanage part counts, part positions or the generated image's exact pixel dimensions; final role dimensions belong to deterministic post-processing. Use a new independent source for every asset and retry.
7. Generate only with Codex built-in `image_gen`. Do not use localhost image services, private relays or automatic fallbacks. Decode through `Tools/OCCArt/decode_generated_pixel_grid.py`: infer pitch from the subject rather than its background, require lattice confidence at least `1.2`, and reject mixed-scale sources. A retry changes only the diagnosed failed dimension; stop after three independent rounds.
8. Preserve the decoded subject without geometric resizing or interpolation. Colour cleanup may map to observed source medoids only, with no new colours or dithering. Apply the role canvas, hard Alpha, anchor, palette limit and applicable one-native-pixel outer contour, then produce manifest and 1×／4×／grayscale／checker／application evidence.
9. Review at target size. Reject generic fantasy, medieval ornament, steampunk clutter, copy-like visual language, unreadable silhouettes, palette-role violations, details that cannot survive the final pixel cell, and any ordinary single-cell prop that requires a visual canvas larger than `64×64` without renewed occupancy review.

## Deliverable

Return: asset role; player reading; visual direction; locked invariants; final prompt; exclusions; target-size review checklist. For explorations, also explain the differentiating art-direction decision for each option.


OCC modeling policy (final user correction, 2026-10-10): minimize modeling; prefer pixel cards/thick cards. Simple polygonal models mainly serve environments and large scene props, and every visible model needs matching pixel textures reinforcing the 2D style. Before promoting a sample, bind a construction manifest listing model rationale, geometry/material/texture inputs and application evidence; review model/card consistency. A table is not rejected merely for being modeled; untextured visible primitives or geometry-driven detail are quality failures.


## 同场景像素大小：最高优先级（用户确认，2026-10-10）

同场景像素大小统一（用户确认，2026-10-10，最高优先级）：同一场景的全部可见像素资产必须使用同一像素密度基准，包含角色、纸板、厚纸板、模型贴图、环境、陈设、场景内装饰和像素特效；美观、细节丰富度、画布自适应及战斗外素材库例外均不得放宽此要求。先选定场景颗粒参照，记录主体像素单元、世界尺寸、显示缩放及模型UV密度，再生成或复用素材；不能只凭图片尺寸、Point采样或粗像素提示词判断一致。以实际游戏镜头验收，相同深度与投影条件下可见像素大小必须统一；远近和斜面只允许同一密度经真实投影产生的变化，不得以透视为逐物件改密度的借口。源图中混用粗细格、同场景杯碟与灯等陈设颗粒不一致，均为最高优先级阻断项，先修复再扩量。书桌当前颗粒参照为咖啡杯右侧蓝金晶灯，只认可其像素大小与色簇表现，不等于批准整个场景。正文和系统文字保持既定清晰度，真实光照、阴影及后处理仍按高清空间表现配置，不强制整帧低清；它们不替代像素资产的一致性检查。入库与样板晋升必须提交全部可见像素资产的密度清单、源与缩放/UV追溯、1920×1080及960×540实际同框对比、运动/缩放检查和真实人工复核；缺失、待定或不一致时保持QA_PENDING。更换资产、尺寸、相机、UV或采样使相关证据失效，重新验证受影响范围，不用旧截图或机器PASS代替场景审核。

执行时先查总案A.2.4与机器合同pixel_density_policy。每个场景只用一个有效世界像素间距；完整清单将源主体像素单元、显示缩放或UV换算到此基准，再检查同框实际颗粒。生成与复用简报必须提供颗粒参照，不能只写coarse/pixel art。机器声明一致不证明图片真实一致；旧证据不得继承为批准。
