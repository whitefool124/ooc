#!/usr/bin/env python3
"""Prepare M-A32 trial manifests before any image generation."""

from __future__ import annotations

import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
BATCH = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A32"
CATALOG = BATCH / "m_a32_trial_catalog.json"
MANIFESTS = BATCH / "manifests"


def main() -> None:
    assets = json.loads(CATALOG.read_text(encoding="utf-8"))["assets"]
    MANIFESTS.mkdir(parents=True, exist_ok=True)
    for asset in assets:
        stem = asset["stem"]
        raw = f"Worldbuilding/05_美术与音频/正式美术生产/M-A32/raw/{stem}/source.png"
        normalized = f"Worldbuilding/05_美术与音频/正式美术生产/M-A32/normalized/{stem}.png"
        qa = f"Worldbuilding/05_美术与音频/正式美术生产/M-A32/QA/{stem}"
        native = None
        if asset["role"] == "material_pickup_24_to_ui32":
            native = f"Worldbuilding/05_美术与音频/正式美术生产/M-A32/normalized/{stem}_native24.png"
        manifest = {
            "schema": "occ-art-manifest-v1",
            "contract_version": 1,
            "asset_id": asset["asset_id"],
            "role": asset["role"],
            "status": "QA_PENDING",
            "provenance": {
                "source_channel": "codex_builtin_imagegen",
                "source_descriptor": "Codex built-in image generation; independent M-A32 trial source; non-Unity",
                "source_path": raw,
                "source_sha256": "PENDING_GENERATION"
            },
            "delivery": {
                "output_path": normalized,
                "output_sha256": "PENDING_NORMALIZATION",
                "native_output_path": native,
                "logical_cells": asset["logical_cells"],
                "palette_max": asset["palette_max"],
                "required_color_families": []
            },
            "application": {
                "runtime_draw_rect": asset["application"],
                "default_integer_scale": 4,
                "minimum_integer_scale": 2
            },
            "evidence": {
                "one_x": f"{qa}/1x.png",
                "four_x": f"{qa}/4x.png",
                "grayscale": f"{qa}/grayscale.png",
                "checker": f"{qa}/checker.png",
                "application_contact": "Worldbuilding/05_美术与音频/正式美术生产/M-A32/QA/contacts/m_a32_trial_contact.png"
            },
            "human_review": {
                "overall": "PENDING",
                "reviewer": "",
                "date": "",
                "silhouette": "PENDING",
                "material": "PENDING",
                "perspective": "PENDING",
                "style": "PENDING",
                "application": "PENDING",
                "notes": "Awaiting product review; trial only; must not enter Unity."
            },
            "unity_import": None
        }
        path = MANIFESTS / f"{stem}.occ-art-manifest-v1.json"
        path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        (BATCH / "raw" / stem).mkdir(parents=True, exist_ok=True)
    print(json.dumps({"prepared": len(assets), "status": "QA_PENDING"}, ensure_ascii=False))


if __name__ == "__main__":
    main()
