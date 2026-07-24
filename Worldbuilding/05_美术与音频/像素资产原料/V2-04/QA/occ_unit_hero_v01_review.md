# occ_unit_hero_v01 QA review

- Review date: 2026-07-24
- Status: `QA_PENDING`
- Scope: independent static-frame source normalization only. This is not a formal-art approval.

| Gate | Result | Evidence / finding |
| --- | --- | --- |
| Independent source | PASS | `../Units64/occ_unit_hero_v01.png` is one single-character source, not a cut from a sprite sheet. |
| Fixed cell | PASS | Normalized output is `64x64`; `occ_unit_hero_v01_strip.png` has one cell. |
| Center / baseline | PASS | Reported normalized bounds `[22, 12, 42, 58]`; horizontal center reference is `X=32`, bottom is at `Y=58`. See `occ_unit_hero_v01_QA_4x.png`. |
| Alpha and palette automation | PASS (technical) | Chroma-key removal, hard alpha and 24-color quantization completed by the QA pipeline. |
| Pixel readability | FAIL | At target size the rifle and body detail merge too heavily; the silhouette needs hand cleanup before it can represent the playable hero. |
| Formal provenance | FAIL | The source is AI raw material, not an Aseprite/PixelOver hand-cleaned production sprite. |
| Unity import | NOT APPLICABLE | No file has been placed under `UnityProject/Assets/`. |

## Decision

Keep the source and all QA outputs as `QA_PENDING`. Do not promote, import or replace existing prototype icons with this file. The next acceptable action is an artist-cleaned 64x64 static frame with the same anchor evidence; animation frames may start only after that static frame is accepted.

## Generated evidence

- `occ_unit_hero_v01_QA_4x.png`: 4x nearest-neighbor overlay with cyan cell boundary, magenta centerline, yellow baseline and green content bounds.
- `occ_unit_hero_v01_report.json`: pipeline metrics and automated checks.
- `occ_unit_hero_v01_preview.gif`: one-frame transport preview only, not an animation deliverable.
