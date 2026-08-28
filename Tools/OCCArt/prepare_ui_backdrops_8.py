#!/usr/bin/env python3
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
M27 = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A27"
ARTIFACTS = ROOT / "UnityProject/Artifacts/UiBackdrops8"

ASSETS = {
    "startup": "closed cloth-bound academy field dossier with blank paper tabs and an oxidized brass retaining strip",
    "landing": "quiet academy entrance arcade with a distant sealed tower, seen across a pale morning courtyard",
    "map": "opened academy survey plan on a worn wooden registry table with a ruler and document clips",
    "briefing": "field dispatch clipboard holding blank layered assignment sheets with one restrained red quarantine edge tab",
    "inventory": "worn academy workshop table with a large empty cloth sorting mat, iron clamps and a few brass maintenance fittings at the edges",
    "settlement": "completed field dossier on an archive return desk with ink pad, filing tray and restrained circular registry marks",
    "archive": "academy archive room with index drawers and shelves framing a large open blank registry ledger",
    "settings": "academy calibration desk with blank test cards, two tactile mechanical dials and neutral light-check swatches",
}


def main() -> None:
    manifests = M27 / "manifests"
    manifests.mkdir(parents=True, exist_ok=True)
    catalog = []
    for stem, subject in ASSETS.items():
        (ARTIFACTS / stem).mkdir(parents=True, exist_ok=True)
        prompt = (
            "Use case: stylized-concept\n"
            f"Asset type: independent OCC {stem} page background source for a Unity UI, no interface baked in\n"
            f"Primary request: {subject}\n"
            "Scene/backdrop: near-modern rustic academy aether-industry expressed through warm paper, cloth folders, old wood, forged iron and very limited oxidized brass\n"
            "Composition/framing: exact 16:9 landscape, straight-on or shallow top-down; keep the central 55 percent calm, pale and low contrast for real UI cards and text; concentrate narrative objects at the edges and corners\n"
            "Style/medium: deliberately coarse hand-clustered pixel-art background, large readable clusters, crisp stair-step edges, restrained material detail\n"
            "Lighting/mood: soft upper-left daylight, quiet practical archival atmosphere, no dramatic glow\n"
            "Color palette: warm paper white, archive beige, charcoal ink, muted wood brown; only tiny functional accents of oxidized brass, medical green, sealed red, or active-aether cyan where appropriate\n"
            "Constraints: background artwork only; no people; no letters, words, numbers, fake writing, title, buttons, cards, icon slots or complete UI; all visible paper surfaces blank or marked only by non-semantic wear\n"
            "Avoid: blue-black full screen, cyan borders, terminal grid, scanlines, hologram, radar, oscilloscope, sci-fi console, steampunk gear clutter, medieval parchment curls, wax-seal excess, holy glow, gradients, bloom, soft focus, anti-aliased vector look, watermark"
        )
        catalog.append({"stem": stem, "subject": subject, "prompt": prompt})
        manifest = {
            "schema": "occ-art-manifest-v1", "contract_version": 1,
            "asset_id": f"ui.backdrop.{stem}", "role": "ui_backdrop_480x270", "status": "QA_PENDING",
            "provenance": {
                "source_channel": "codex_builtin_imagegen",
                "source_descriptor": "Independent single OCC page backdrop; no board slicing",
                "source_path": f"UnityProject/Artifacts/UiBackdrops8/{stem}/source.png",
                "source_sha256": "PENDING_GENERATION",
            },
            "delivery": {
                "output_path": f"UnityProject/Assets/Game/Resources/Art/ValidationUIBackdrops/{stem}.png",
                "output_sha256": "PENDING_NORMALIZATION", "native_output_path": None,
                "logical_cells": None, "palette_max": 24, "required_color_families": [],
            },
            "application": {
                "runtime_draw_rect": "480x270 full-screen page backdrop at integer 2x or 4x",
                "default_integer_scale": 4, "minimum_integer_scale": 2,
            },
            "evidence": {
                "one_x": f"UnityProject/Artifacts/UiBackdrops8/{stem}/1x.png",
                "four_x": f"UnityProject/Artifacts/UiBackdrops8/{stem}/4x.png",
                "grayscale": f"UnityProject/Artifacts/UiBackdrops8/{stem}/grayscale.png",
                "checker": f"UnityProject/Artifacts/UiBackdrops8/{stem}/checker.png",
                "application_contact": "UnityProject/Artifacts/UiBackdrops8/contacts/PENDING.png",
            },
            "human_review": {
                "overall": "PENDING", "reviewer": "", "date": "",
                "silhouette": "PENDING", "material": "PENDING", "perspective": "PENDING",
                "style": "PENDING", "application": "PENDING", "notes": subject,
            },
            "unity_import": None,
        }
        (manifests / f"backdrop_{stem}.occ-art-manifest-v1.json").write_text(
            json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
        )
    (M27 / "ui_backdrops_8_catalog.json").write_text(json.dumps(catalog, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"status": "PASS", "prepared": len(catalog)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
