#!/usr/bin/env python3
from __future__ import annotations

import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
M28 = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A28"
ARTIFACTS = ROOT / "UnityProject/Artifacts/UiTrims6"
ASSETS = {
    "binding_spine": ("ui_trim_vertical_32x64", "32x64", "a narrow dark charcoal cloth dossier binding spine, two coarse stitched seams and one tiny oxidized brass retaining plate"),
    "index_tab": ("ui_trim_horizontal_64x32", "64x32", "a blank warm archive-paper index tab with a short cloth root and squared folded end"),
    "measure_ruler": ("ui_trim_horizontal_64x32", "64x32", "a short worn wooden survey ruler with alternating long and short notch cuts, strictly no numbers"),
    "corner_clasp": ("ui_trim_square_32", "32x32", "an L-shaped forged-iron dossier corner protector with one tiny oxidized brass rivet"),
    "folded_corner": ("ui_trim_corner_64", "64x64", "one warm paper page corner folded into a clean triangular flap with a crisp hard-edged underside shadow"),
    "status_clip": ("ui_trim_square_32", "32x32", "a compact forged-iron document clip gripping a short blank paper tongue with one restrained sealed-red edge"),
}


def main() -> None:
    manifests = M28 / "manifests"
    manifests.mkdir(parents=True, exist_ok=True)
    catalog = []
    for stem, (role, size, subject) in ASSETS.items():
        (ARTIFACTS / stem).mkdir(parents=True, exist_ok=True)
        prompt = (
            "Use case: stylized-concept\n"
            f"Asset type: independent modular OCC UI edge decoration source; final logical pixel canvas {size}\n"
            f"Primary request: {subject}\n"
            "Scene/backdrop: genuinely transparent background, isolated single object only\n"
            "Style/medium: coarse hand-clustered pixel art, dominant readable silhouette, deliberate square stair-step contour, two or three structural partitions\n"
            "Lighting/mood: restrained fixed upper-left light, no glow\n"
            "Color palette: warm paper, charcoal cloth or forged iron, muted wood brown, tiny oxidized brass; sealed red only where explicitly requested\n"
            "Composition/framing: original orientation exactly as described, centered with clean transparent safety space; do not rotate or mirror\n"
            "Constraints: one isolated trim component, transparent background, no interface, no text, no letters, no numbers, no symbols, no pseudo-writing, no button, no full frame, no drop shadow beyond the object\n"
            "Avoid: blue crystal, cyan energy, neon, hologram, terminal grid, scanlines, steampunk gears, pipes, medieval scroll curls, wax seals, gradients, bloom, soft focus, anti-aliasing, watermark"
        )
        catalog.append({"stem": stem, "role": role, "size": size, "subject": subject, "prompt": prompt})
        manifest = {
            "schema": "occ-art-manifest-v1", "contract_version": 1,
            "asset_id": f"ui.trim.{stem}", "role": role, "status": "QA_PENDING",
            "provenance": {
                "source_channel": "codex_builtin_imagegen",
                "source_descriptor": "Independent single transparent UI trim source; no board slicing",
                "source_path": f"UnityProject/Artifacts/UiTrims6/{stem}/source.png",
                "source_sha256": "PENDING_GENERATION",
            },
            "delivery": {
                "output_path": f"UnityProject/Assets/Game/Resources/Art/ValidationUITrims/{stem}.png",
                "output_sha256": "PENDING_NORMALIZATION", "native_output_path": None,
                "logical_cells": None, "palette_max": 10, "required_color_families": [],
            },
            "application": {
                "runtime_draw_rect": f"modular page-edge trim at native {size}, nearest-neighbour 2x or 4x",
                "default_integer_scale": 4, "minimum_integer_scale": 2,
            },
            "evidence": {
                "one_x": f"UnityProject/Artifacts/UiTrims6/{stem}/1x.png",
                "four_x": f"UnityProject/Artifacts/UiTrims6/{stem}/4x.png",
                "grayscale": f"UnityProject/Artifacts/UiTrims6/{stem}/grayscale.png",
                "checker": f"UnityProject/Artifacts/UiTrims6/{stem}/checker.png",
                "application_contact": "UnityProject/Artifacts/UiTrims6/contacts/PENDING.png",
            },
            "human_review": {
                "overall": "PENDING", "reviewer": "", "date": "",
                "silhouette": "PENDING", "material": "PENDING", "perspective": "PENDING",
                "style": "PENDING", "application": "PENDING", "notes": subject,
            },
            "unity_import": None,
        }
        (manifests / f"trim_{stem}.occ-art-manifest-v1.json").write_text(
            json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
        )
    (M28 / "ui_trims_6_catalog.json").write_text(json.dumps(catalog, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"status": "PASS", "prepared": len(catalog)}))


if __name__ == "__main__":
    main()
