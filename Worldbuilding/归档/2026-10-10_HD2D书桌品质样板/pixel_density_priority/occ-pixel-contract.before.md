适用范围补充（2026-10-10）：以下严格网格、色数、硬Alpha与无缩放步骤用于战场与功能图标。战斗外HD2D文字载体及陈设按总案A.2.2的2026-10-09素材库例外；已迁移场景高清输出与品质门槛按A.2.4及项目occ-hd2d-quality技能。不得把旧通用步骤覆盖例外。

# OCC Pixel Asset Contract

Use this reference with the current OCC master plan, art/interface CSV and
`Tools/OCCArt/occ_art_contract_v1.json`. Those project files remain authoritative.

## Shared production route

All future OCC pixel assets use one route:

1. Generate one independent source with Codex built-in `image_gen`.
2. State identity, application, view and material story. Require visibly coarse pixel
   art and one uniform square macro-pixel lattice across the complete subject. Do not
   prescribe part counts, part positions or the generated image's exact dimensions.
3. Infer pixel pitch and phase from subject contours and internal colour boundaries,
   never from a checkerboard or transparent background. Reject confidence below `1.2`
   or mixed subject pixel scales.
4. Retry with a new independent source and change only the diagnosed failed dimension.
   Do not lower the threshold; stop after three rounds.
5. Decode one macro pixel to one native pixel without geometric resize or interpolation.
   Remove background without changing subject Alpha. Merge RGB noise only to colours
   observed in the source; introduce no new colours and no dithering.
6. Apply the role canvas, hard Alpha, palette, anchor and applicable one-native-pixel
   outer contour. Produce manifest, 1x, 4x, grayscale, checker and application evidence.

Do not use localhost image services, private relays or resolution-companion
workflows for OCC pixel production.

## Role routing

| Asset role | Delivery rule |
| --- | --- |
| Semantic icon | Fixed `16×16`, at most four colours, one functional silhouette. |
| Material pickup | Native `24×24`, embedded unscaled at `(4,4)` in `32×32`. |
| Equipment, relic and spell icons | Fixed `32×32` unless its active role contract says otherwise. |
| Battlefield ground | One `32×32` logical cell; multi-cell ground is `(W×32)×(H×32)`. |
| Directional outer edge | `32×40`; top `32×32` is the logical cell and lower 8px is non-interactive facade. |
| Transparent battlefield prop | 32 PPU; smallest fitting 32-multiple visual canvas; logical occupancy declared separately. Ordinary single-cell props normally do not exceed `64×64`. |
| Tactical unit | 32 PPU; typical humanoid `32×64`, centre `X=16`, feet near `Y=62`, strict pure side view. Larger bodies expand by 32-pixel canvas multiples instead of shrinking. |
| Terrain VFX | Native `32×32` frames unless its active role contract says otherwise. |

One native asset is displayed only at integer scale. Do not create separate 64px and
32px versions for different screen resolutions.

## Visual language

- Visual theme and materials change by life stage. The current academy stage uses a
  clear Japanese fantasy magic isekai direction: adventurous magical-academy spaces,
  fantasy nature, light readable silhouettes and region-specific materials such as
  light stone, warm timber, glazed roof tile, fabric, plants, water and aether apparatus.
  Aether remains measurable, maintainable and costly energy engineering. Avoid gritty
  medieval realism, glossy science fiction, decorative steampunk as a global default,
  copying a specific work and generic anime shorthand without material or gameplay logic.
- Use large connected clusters and a limited palette. Reject dense cracks, rivet fields,
  fine grain, isolated speckles, high-frequency dithering, gradients and blur.
- Cyan means active aether or actionable state; safety yellow means service, loot or
  warning; rust red means hostile threat. Ordinary material colour remains restrained.
- Map props share one oblique top-down view, visible top and front faces, fixed light and
  screen-axis alignment. Only battlefield units use strict horizontal-game side view.
- Units and transparent props replace the existing outermost opaque texel ring with one
  pure-black native pixel after final placement. Do not expand Alpha or add a second ring.
  Ground tiles do not receive a full black outline.

## Review and promotion

Machine acceptance requires correct dimensions or adaptive 32-multiple canvas, binary
Alpha, palette limit, safe border, declared logical occupancy and all evidence files.
Technical PASS does not override silhouette, view or application failures. Human approval
promotes to `FORMAL_CANDIDATE`; stable GUID, Point/Clamp/no mipmaps/no compression and
runtime verification are still required for `FORMAL`.

The 2026-09-14 academy-maintenance stability batch is the baseline: 6 assets, 11
independent generations, 2/6 first-pass lattice success and 6/6 success after one targeted
uniform-lattice retry. Therefore automatic rejection and targeted regeneration are part of
the production workflow; one-shot generation is not treated as unattended production.

Non-battlefield display objects (user confirmed 2026-10-03) use a canvas that fits the complete subject and safe border, without a fixed32px size or 32px-multiple rule. Judge consistent art style, natural UI scale and actual support contact. Battlefield rules and functional icon contracts remain unchanged.
