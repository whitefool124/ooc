# OCC Art Contract Tools

> **开始任何美术工作前，先读 `ART_SPEC_LOCK_2026-09-16.md`** —— 它是唯一入口：一句话规格、
> 角色尺寸表、画风硬规则、世界尺度推导、摆放规则、生图六步、验收门禁、FORMAL 要求，以及
> **已废弃口径清单**（16 PPU / 1／2／4 倍 / 无边框等旧决定）。本 README 只讲工具怎么用。

`Worldbuilding/策划案/OCC_项目总策划案_v1.0.md` is the only active art-direction
source. `Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv` is its structured
companion, and the JSON contract in this folder is only a machine-readable mirror.

Every new asset must copy `occ_art_manifest_template_v1.json`, choose one role,
record repository-relative source/output/evidence paths and hashes, then run:

```powershell
& 'C:/Users/FNHF/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' `
  Tools/OCCArt/validate_occ_art_asset.py `
  path/to/asset.occ-art.json
```

Audit only the canonical documents and machine mirror:

```powershell
& 'C:/Users/FNHF/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' `
  Tools/OCCArt/validate_occ_art_asset.py --audit-contract
```

Audit every registered OCC art manifest in one command:

```powershell
& 'C:/Users/FNHF/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' `
  Tools/OCCArt/validate_all_occ_art.py
```

The validator never draws or repairs art. A machine `PASS` plus human review
permits `FORMAL_CANDIDATE`, not direct Unity import. `FORMAL` additionally needs
the canonical six-times evidence, the illumination review, a stable GUID, a
verified importer and a passing runtime lattice capture (see below).

## Per-role PPU and the locked display policy

Locked 2026-09-16 (`occ_art_contract_v1.json` → `battlefield_display_policy`).
One native buffer of 320x180 is integer upscaled exactly 6x to 1920x1080, so one
native pixel is 6 screen pixels; the 1408x768 battlefield viewport shows 7x4
gameplay cells.

| role family | native PPU | delivery |
| --- | --- | --- |
| battlefield ground square | 32 | **32x32, exactly one per gameplay cell**, self-contained with a visible 1 px cell border |
| ground front face | 32 | 32x8, attached to a cell's south edge for depth |
| directional board edge | 32 | 32x40 = 32x32 walkable + 8 px facade outside the board |
| wall | 32 | 32x40 = cell + 8 px above the owning cell |
| prop, structure, unit | 32 | role contract size, e.g. 32x64 humanoid |

The whole battlefield is one 32 PPU grid, so every native pixel is exactly 6 screen
pixels at the canonical tier and no role can drift out of scale against another. The
cell is deliberately shorter than the characters: a unit's 32-58 px body is 1.0x to
1.8x the 32 px cell, so the upper body overhangs its own cell while occupancy stays
unchanged. This version ships **no material transitions**: each square carries its own
**visible border** (1 px, the material's own dark tone, never pure black, all four edges
identical so neighbours overlap into one seam, plus a 1 px lighter inner lip) and depth
comes from the separate front face. Display tiers are integer only; a resolution that is
not an integer multiple is letterboxed rather than fractionally scaled.

## Approved generation workflow

Use Codex built-in `image_gen` to create an independent coarse pixel-art reference.
Do not call EasyCLI or another local image service. Describe the object, application
scene, logical occupancy and shared map view without micromanaging component counts
or target pixel dimensions. This route applies to all future OCC pixel assets; each
fixed-size role still keeps its own delivery contract.

Decode the generated subject's own macro-pixel lattice with
`decode_generated_pixel_grid.py`. Use `--target-size auto` for units and transparent
props so each canvas axis rounds up to a multiple of the role's PPU grid without
resizing the subject. Ground decodes to one 32x32 square per gameplay cell; typical
humanoids use a 32x64 canvas; a one-cell prop may use a 64x64 visual canvas while its
gameplay footprint remains 1x1.

Require a single uniform subject lattice and confidence of at least `1.2`. **Also require
that the decoded design field stays within 1.4x of the delivery in each axis**: a window
that crops a much larger, finer-grained design yields a fragment of a bigger picture
instead of one whole square. That was the recorded failure of the 16 px ground trial,
where decoded fields measured 34x33 up to 158x157 against a 16 px window (coverage 23
percent down to 1 percent) and the delivered tile read as noise instead of a paving
square. Reject a mixed or unstable grid instead of lowering the threshold. Retry with a
new independent source, changing only the diagnosed failed dimension, for at most three
rounds. One macro pixel becomes one native pixel; geometric resize, interpolation,
dithering and new colours are forbidden.

`native32_generation_profiles.json` contains the approved profiles, the role PPU
table, the display policy, the lighting policy and their calibration assets. Every
output still requires 1x, 4x, grayscale, checker and application-contact evidence
before promotion, plus the canonical 6x evidence for the battlefield ground roles.

## Assets carry no scene lighting

Generated sources must not contain any scene illumination: no light pool, no cast
floor shadow, no vignette, no ambient darkening, no darkened border and no baked
torch or aether glow. The single exception is the fixed upper-left **form shading**
(lit top face, dark front face) that makes a solid read as a volume; that is shape
language, not lighting, and no role may vary its direction.

All scene light and shadow belongs to the runtime URP 2D Light2D layer. This is what
the reference does: unlit regions keep zero intra-block variance because light is
composited after the integer upscale instead of being painted into the art. The
`illumination` human-review dimension and the FORMAL gate enforce it.

## Modular battlefield ground batches

For a family of themed battlefield squares, use the machine-readable batch contract in
`occ_modular_ground_batch_contract_v1.json` (v5). It fixes the topology (**four square
variants and two front faces first**, then four directional edges and four corners), the
32 PPU delivery, the bolder/flatness/illumination invariants and the review gates G0-G6.
A square is **self-contained**: the map editor places and swaps one square at a time, so
there is no cross-cell continuation and no material transition piece in this version.

Opaque ground is role-specific: it is a decoded material field, not an isolated
transparent subject. After a stable decode, it produces its deterministic centered
**32x32** logical window using `--ground-window 32x32`. This extracts only observed
decoded pixels: it never rescales, interpolates, paints, adds colours or changes a
selected pixel. Record the source hash, decoded field size and window bounds in the QA
report and manifest, and confirm the decoded field is within 1.4x of the window. The
visible cell edge is **baked into the square** as its 1 px border; a shared tactical-grid
overlay may still be drawn on top but is no longer what defines the cells.

Regular repeating brick/material fields may make the generic non-repeating-subject
confidence ambiguous. In that role only, a reviewer may declare the visibly
observed square macro-pixel pitch with `--ground-field-pitch`; it still uses no
resize or synthesis, and its report is only `REVIEW_READY`. It requires explicit
human pixel-scale, material and application PASS before `FORMAL_CANDIDATE`.
Plan a theme without generating art:

```powershell
& 'C:/Users/FNHF/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' `
  Tools/OCCArt/prepare_modular_ground_batch.py `
  --theme-id academy_court --material-family slate `
  --base-palette '#9D9884' '#898676' '#666459' '#B4AE96' `
  --wetness 1 --wear 1 --motif brick_joint --edge-profile low_curb
```

Only after the single reference square passes G0-G3 should the remaining pieces
be generated. The script is planning/scaffolding only; it never calls an image
model and never imports assets into Unity.

Check every square before review — this is the per-tile style gate, and it is the one
that catches "a fragment of a bigger design" and "no visible border":

```powershell
& 'C:/Users/FNHF/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' `
  Tools/OCCArt/verify_ground_tile.py path/to/ground_x_square_a_32.png --paving
```

For a new single-cell battlefield prop, use `single_cell_prop_adaptive_32ppu`.
Its visual canvas is the smallest width and height, each a multiple of 32, that
preserves the decoded subject. The canvas may overhang adjacent cells visually;
selection, collision and gameplay occupancy remain bound to the declared owning cell.

## Promotion to FORMAL

`FORMAL_CANDIDATE` keeps the standing five evidence files and five human-review
dimensions. `FORMAL` adds four gates (`occ_art_contract_v1.json` →
`formal_requirements`), all recorded in the manifest:

1. `evidence.six_x` — the canonical 6x contact sheet.
2. `human_review.illumination: PASS` — no baked scene lighting.
3. `unity_import` — `filter_mode: Point`, `mesh_type: Full Rect`, `compression: None`,
   `mipmaps: false`, `wrap_mode: Clamp`, atlas padding 0 / no rotation / no tight
   packing / extrude 0, plus the stable GUID. A tight mesh crops the sprite quad and
   shifts it off the native pixel lattice, which breaks uniform pixel size and
   seam-free adjacency.
4. `unity_import.runtime_lattice` — the result of the runtime pixel-grid check.

Verify the runtime lattice on a real capture:

```powershell
& 'C:/Users/FNHF/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' `
  Tools/OCCArt/verify_runtime_pixel_grid.py `
  --capture path/to/runtime_capture.png --expect-tier 6 `
  --region ground:40,300,700,760 --region props:150,120,900,420 `
  --json-out path/to/runtime_lattice.json
```

The tool reports the detected tier per region, the lattice offset and whether the
regions agree; the true tier is the largest pitch whose blocks are internally
uniform. It exits non-zero when the tier differs from the expectation or the lattice
is offset, and its JSON is what the manifest records. Use it for every battlefield
promotion, because it is the only check that catches a role rendering at the wrong
pixel size or a fractional scale.
