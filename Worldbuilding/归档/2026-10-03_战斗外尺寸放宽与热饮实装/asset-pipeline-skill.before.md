---
name: pixel-asset-pipeline
description: "将原始像素图或动画帧规范化为固定尺寸 Sprite，处理锚点、透明度、调色板及 GIF/QA 交付。"
---

# Pixel Asset Pipeline

Normalize supplied pixel sources into reviewable sprite assets. This skill does not select an image-generation service, prescribe a rigging tool, or authorize game import.

## Contract before processing

Read the project's current art specification and matching asset role. Determine cell size, source scale, frame count, playback, anchor, alpha and shared palette from that contract. The bundled [64px example](references/character_64_8f.json) is a historical preset, not a universal default.

For OCC, use the current master art sections, matching specification-table rows, `Tools/OCCArt/occ_art_contract_v1.json` and `Tools/OCCArt/native32_generation_profiles.json`. All pixel assets share the same source-grid discipline while retaining role-specific delivery sizes: 16px semantic assets; 24px material sources in 32px padded delivery; fixed 32px equipment and non-battlefield assets; 32px ground cells; and 32 PPU units or transparent battlefield props on the smallest fitting 32-multiple canvas. Typical humanoids use 32×64; an ordinary one-cell prop may use 64×64 visual canvas while remaining logically `[1,1]`. Do not create resolution companions. New art needs an `occ-art-manifest-v1`; raw material cannot enter Unity directly.

## Process

1. Record source files and provenance. Verify a sheet is a strict grid before slicing; otherwise request independent frames or identify cells explicitly.
2. Determine one coherent subject pixel lattice and shared palette. For generated OCC sources, infer pitch and phase from the subject contour and internal colour boundaries, never from a checkerboard or transparent background. Require confidence at least `1.2`; reject mixed pixel scales. One macro pixel decodes to one native pixel without geometric resizing or interpolation.
   - If diagnosis fails, generate a new independent source that changes only the failed dimension. Do not lower the confidence threshold or repair structure through program drawing. Stop after three rounds.
   - For animation, keep one shared scale and palette across frames; preserve intended root motion and pose compression rather than fitting each pose independently.
3. Clean background and alpha without removing similar-colored parts of the subject. Align by the specified anchor and contact phase, not blindly by bounding-box center.
   - Painted checkerboards and reconstructed alpha can leave neutral fringe. Inspect contour pixels against both light and dark backgrounds; do not hide fringe by quantizing it into the palette.
   - Register a coherent body by root/contact landmarks. Do not freeze a horizontal body region or reuse one lower-body frame to stop foot drift: that creates waist or joint discontinuities. If feet must remain planted, preserve full-frame anatomy and correct the motion continuously through the limb chain or redraw the affected poses.
4. Place the decoded subject into its role-specific canvas without resizing. Fixed roles retain exact dimensions; OCC units and transparent battlefield props use 32 PPU and round each canvas axis up to a multiple of 32. Treat an ordinary one-cell prop requiring more than 64×64 as an occupancy or silhouette review failure.
5. Produce the deliverables needed for the task: asset or strip/frames, correctly timed GIF where applicable, manifest, 1×／4×／grayscale／checker／application evidence and a report recording actual parameters and limitations.
6. Inspect at target size and enlarged nearest-neighbor scale, including transparency, palette, clipping, anchor stability and action continuity. Lying, jumping and non-humanoid poses need phase-appropriate checks.
   - Treat blur-like softness as a cluster problem when interpolation is disabled: excessive one-pixel value changes, weak edge contrast, gray fringe and noisy internal texture can look blurred despite hard alpha.
   - Quantization is not pixel cleanup. Check connected cluster sizes, isolated-pixel count, contour colour changes and frame-to-frame pixel churn; simplify or redraw noisy regions instead of only lowering the palette size.

## Bundled script limitation

[`normalize_frame_sequence.py`](scripts/normalize_frame_sequence.py) currently fits each frame independently by its bounding box and quantizes each frame separately. It does **not** enforce sequence-wide scale or palette. Use it only where independent pose fitting is intended; do not claim it passes a shared-scale animation contract. For such animation, use a verified project wrapper or adapt the implementation within the authorized processing task and validate the result.

The [pipeline note](references/pipeline-note.md) describes a historical DeathFall experiment. Its 64px/8-frame thresholds and output paths are examples, not current OCC specifications.

## Acceptance and import

Check dimensions, alpha, palette, contact and identity in the actual output. A numerical report cannot replace visual review; reject accidental extra limbs, detached props, scale pulsing and unreadable motion. Regenerate only rejected sources when approved frames can be retained.

Follow the target project's formal gates. For OCC, retain provenance and manifest, run the asset validator and 1×/4×/grayscale/checkerboard checks, verify application contact and obtain actual human aesthetic acceptance before FORMAL_CANDIDATE; stable GUID/importer/runtime review is needed for FORMAL. Import only within authorized scope through the established editor pipeline.
