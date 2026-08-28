#!/usr/bin/env python3
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
M29 = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A29"
ARTIFACTS = ROOT / "UnityProject/Artifacts/UiEmptyIllustrations6"
ASSETS = {
    "empty_archive_tray": "an empty shallow archive return tray, worn dark wood with forged-iron corner bands, visibly containing nothing",
    "empty_inventory_pouch": "an opened and flattened empty academy supply pouch, muted grey-green canvas with leather edge binding, visibly containing nothing",
    "empty_route_case": "a closed cylindrical survey map case beside one plain wooden locator peg, weathered leather and oxidized brass, no map visible",
    "empty_reward_crate": "an open shallow academy supply crate, worn wood and forged-iron braces, interior clearly empty",
    "empty_loadout_rack": "a compact vacant forged-iron equipment bracket with two empty hooks on a small worn wooden foot, no equipment attached",
    "locked_document_satchel": "a closed grey-green canvas document satchel cinched by one restrained sealed-red cloth restraint strap, no lock symbol",
}

def main() -> None:
    manifests = M29 / "manifests"
    manifests.mkdir(parents=True, exist_ok=True)
    catalog = []
    for stem, subject in ASSETS.items():
        (ARTIFACTS / stem).mkdir(parents=True, exist_ok=True)
        prompt = (
            "Use case: stylized-concept\n"
            "Asset type: independent reusable OCC empty-state UI illustration source; final logical pixel canvas 64x64\n"
            f"Primary request: {subject}\n"
            "Scene/backdrop: genuinely transparent background, isolated single object grouping only\n"
            "Style/medium: coarse hand-clustered pixel art, readable at 64x64, strong simple silhouette, deliberate square stair-step contours, restrained material texture\n"
            "Lighting/mood: fixed upper-left light, short hard-edged self-shadow only, quiet archival mood, no glow\n"
            "Color palette: warm paper beige, worn dark wood, charcoal forged iron, muted grey-green canvas, tiny oxidized brass; sealed red only if explicitly requested\n"
            "Composition/framing: centered, slightly elevated three-quarter view where suitable, clean transparent safety space on all sides, original orientation; no rotation or mirror variants\n"
            "Constraints: one isolated empty-state object, visibly empty or closed as described, transparent background, no interface, no text, no letters, no numbers, no symbols, no pseudo-writing, no button, no panel, no floor tile\n"
            "Avoid: blue crystal, cyan energy, magic glow, neon, hologram, terminal grid, scanlines, steampunk gears, pipes, medieval scroll curls, wax seal emblem, gradients, bloom, soft focus, anti-aliasing, watermark"
        )
        catalog.append({"stem": stem, "subject": subject, "prompt": prompt})
        manifest = {
            "schema": "occ-art-manifest-v1", "contract_version": 1,
            "asset_id": f"ui.empty.{stem}", "role": "ui_empty_illustration_64", "status": "QA_PENDING",
            "provenance": {"source_channel": "codex_builtin_imagegen", "source_descriptor": "Independent single transparent empty-state illustration source; no board slicing", "source_path": f"UnityProject/Artifacts/UiEmptyIllustrations6/{stem}/source.png", "source_sha256": "PENDING_GENERATION"},
            "delivery": {"output_path": f"UnityProject/Assets/Game/Resources/Art/ValidationUIEmptyIllustrations/{stem}.png", "output_sha256": "PENDING_NORMALIZATION", "native_output_path": None, "logical_cells": None, "palette_max": 12, "required_color_families": []},
            "application": {"runtime_draw_rect": "existing empty or locked content branch only; 64x64 native at nearest-neighbour 2x or 4x", "default_integer_scale": 4, "minimum_integer_scale": 2},
            "evidence": {"one_x": f"UnityProject/Artifacts/UiEmptyIllustrations6/{stem}/1x.png", "four_x": f"UnityProject/Artifacts/UiEmptyIllustrations6/{stem}/4x.png", "grayscale": f"UnityProject/Artifacts/UiEmptyIllustrations6/{stem}/grayscale.png", "checker": f"UnityProject/Artifacts/UiEmptyIllustrations6/{stem}/checker.png", "application_contact": "UnityProject/Artifacts/UiEmptyIllustrations6/contacts/PENDING.png"},
            "human_review": {"overall": "PENDING", "reviewer": "", "date": "", "silhouette": "PENDING", "material": "PENDING", "perspective": "PENDING", "style": "PENDING", "application": "PENDING", "notes": subject},
            "unity_import": None,
        }
        (manifests / f"empty_{stem}.occ-art-manifest-v1.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    (M29 / "ui_empty_illustrations_6_catalog.json").write_text(json.dumps(catalog, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"status": "PASS", "prepared": len(catalog)}))

if __name__ == "__main__":
    main()
