# OCC Art Contract Tools

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
a stable GUID, verified importer and runtime application record.

## Approved 32 PPU production workflow

Use Codex built-in `image_gen` to create an independent coarse pixel-art reference.
Do not call EasyCLI or another local image service. Describe the object, application
scene, logical occupancy and shared map view without micromanaging component counts
or target pixel dimensions. This route applies to all future OCC pixel assets; each
fixed-size role still keeps its own delivery contract.

Decode the generated subject's own macro-pixel lattice with
`decode_generated_pixel_grid.py`. Use `--target-size auto` for units and transparent
props so each canvas axis rounds up to a multiple of 32 without resizing the subject.
Ground stays 32x32 per logical cell. Typical humanoids use a 32x64 canvas; a one-cell
prop may use a 64x64 visual canvas while its gameplay footprint remains 1x1.

Require a single uniform subject lattice and confidence of at least `1.2`. Reject a
mixed or unstable grid instead of lowering the threshold. Retry with a new independent
source, changing only the diagnosed failed dimension, for at most three rounds. One
macro pixel becomes one native pixel; geometric resize, interpolation, dithering and
new colours are forbidden.

`native32_generation_profiles.json` contains the approved unit and prop profiles and
their calibration assets. Every output still requires 1x, 4x, grayscale, checker and
application-contact evidence before promotion.

## Modular battlefield ground batches

For a family of many themed, composable floor tiles, use the machine-readable
batch contract in `occ_modular_ground_batch_contract_v1.json`. It fixes the
topology (four surface variants, four directional edges and four corners), one
native 32 PPU delivery, the light/value invariants and the review gates.

Opaque ground is role-specific: it is a decoded material field, not an isolated
transparent subject. After a stable decode, it may produce its deterministic
centered 32×32 logical window using `--ground-window 32x32`. This extracts only
observed decoded pixels: it never rescales, interpolates, paints, adds colours,
or changes a selected pixel. Record the source hash, decoded field size and
window bounds in the QA report and manifest. Visible cell edges belong to the
shared tactical-grid layer, not a dark frame baked into each ground tile.

Regular repeating brick/material fields may make the generic non-repeating-subject
confidence ambiguous. In that role only, a reviewer may declare the visibly
observed square macro-pixel pitch with `--ground-field-pitch`; it still uses no
resize or synthesis, and its report is only `REVIEW_READY`. It requires explicit
human pixel-scale, material and application PASS before `FORMAL_CANDIDATE`.
Plan a theme without generating art:

```powershell
& 'C:/Users/FNHF/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' `
  Tools/OCCArt/prepare_modular_ground_batch.py `
  --theme-id archive_court --material-family slate `
  --base-palette '#3E4850' '#56636B' '#252C31' `
  --wetness 2 --wear 1 --motif maintenance_cut --edge-profile low_curb
```

Only after the single reference tile passes G0-G3 should the remaining pieces
be generated. The script is planning/scaffolding only; it never calls an image
model and never imports assets into Unity.

For a new single-cell battlefield prop, use `single_cell_prop_adaptive_32ppu`.
Its visual canvas is the smallest width and height, each a multiple of 32, that
preserves the decoded subject. The canvas may overhang adjacent cells visually;
selection, collision and gameplay occupancy remain bound to the declared owning cell.
