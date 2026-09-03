# OCC V2-04 首批独立原料集中审查

- Review date: 2026-07-24
- Scope: V2-04 raw-source production only. No result in this review is a `FORMAL` asset or a Unity import.
- Evidence root: `Worldbuilding/05_美术与音频/像素资产原料/V2-04/`

## Delivery Coverage

| Category | Required | Delivered | Result |
| --- | --- | --- | --- |
| Tactical command icons | move, attack, skill, loot, interact | 5 independent sources | PASS |
| Unit static frames | hero, rifleman, shieldguard, pyromancer, elite | 5 independent sources | PASS |
| Relay-station materials | floor, light cover, heavy cover, relay, loot crate | 5 independent sources | PASS |

Every entry has an independent source image, fixed-cell normalized review output, 4x QA overlay, palette preview and JSON report. All 15 JSON reports parse successfully. All sources have chroma-key cleanup and hard-alpha review output; units use `64x64 / X=32 / Y=58`, while icons and station materials use `32x32`.

## Palette Review

| Asset group | Color limit | Measured results | Evidence |
| --- | --- | --- | --- |
| Icons | 16 | 16 each | `QA/occ_icon_*/palette_4x.png` |
| Units | 24 | 24 each | `QA/occ_unit_*/palette_4x.png`, plus `occ_unit_hero_v01_palette_4x.png` |
| Relay materials | 16 | floor 17, other four 16 | `QA/occ_relay_*/palette_4x.png` |

The floor source is one color over the 16-color object target and must be reduced during cleanup. This is not a formal-art failure because V2-04 produces reviewable raw material only.

## Human Readability Review

| Group | Accepted source qualities | Mandatory cleanup before `FORMAL` |
| --- | --- | --- |
| Icons | Skill sigil, loot crate and interaction lever read at 32px as distinct command concepts. | Reduce the move icon to one directional glyph; simplify attack's long muzzle trace; remove incidental pixels and check grayscale distinction. |
| Units | Rifleman has a distinct long rifle; shieldguard reads through its wide shield; pyromancer has a staff/cable silhouette; elite has visibly larger armor and hammer. | Hand-clean at target size, tighten color ramps, remove generated insignia-like marks, and verify hero readability against the same standard. |
| Relay materials | Light cover, heavy cover, relay and crate have distinct top-down purposes and the relay concentrates cyan only on the conduit. | Redraw each onto a strict 32px grid, reduce the floor palette to 16 colors, and confirm light cover remains lower/shorter than heavy cover in the actual tactical camera. |

## V2-04b Normalization Approval

- Approval standard: local independent image-generation source plus normalized technical QA. The acceptance target is fixed cells, hard transparency, palette limits and readable surface semantics; it does not require hand-pixel cleanup or high-fidelity art in this batch.
- Evidence: 15 independent sources; 15 parser-valid JSON reports; the 14 directory-format reports each contain `qa_4x.png` and `palette_4x.png`; the separately archived hero report has `occ_unit_hero_v01_QA_4x.png` and `occ_unit_hero_v01_palette_4x.png`.
- Result: all five command icons, all five unit static frames and all five relay-station materials are promoted to `FORMAL` in the source library.
- Limit: `FORMAL` here does not copy any file to `UnityProject/Assets/`, replace prototype art, create a map tile set, or approve animations. V2-05 remains the required Unity import and runtime-replacement review.
