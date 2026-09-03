#!/usr/bin/env python3
"""Create M-A21 footprint catalog and manifests before generation."""

from __future__ import annotations

import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
M20 = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A20/academy_equipment_32_catalog.json"
OUT = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A21"
MANIFESTS = OUT / "manifests"

SIZES = {
    "mh01": (1,3), "mh02": (1,3), "mh03": (2,3), "mh04": (2,3), "mh05": (2,3), "mh06": (1,3),
    "oh01": (2,2), "oh02": (2,3), "oh03": (1,2), "oh04": (1,1),
    "ch01": (2,3), "ch02": (2,3), "ch03": (2,3), "ch04": (2,3), "ch05": (2,3),
    "hd01": (2,1), "hd02": (2,1), "hn01": (2,1), "hn02": (2,1),
    "lg01": (2,2), "lg02": (2,2), "bp01": (2,3), "bp02": (2,3),
    "cr01": (2,2), "cr02": (2,2), "cr03": (2,2), "dg01": (1,3), "dg02": (1,1),
    "ac01": (1,1), "ac02": (1,1), "ac03": (1,1), "ac04": (1,1),
}


def main() -> None:
    base = json.loads(M20.read_text(encoding="utf-8"))["assets"]
    assets = []
    MANIFESTS.mkdir(parents=True, exist_ok=True)
    for item in base:
        suffix = item["stem"].removeprefix("aca_eq_")
        width, height = SIZES[suffix]
        stem = item["stem"]
        root = f"UnityProject/Artifacts/AcademyEquipmentFootprints32/{stem}"
        value = {**item, "asset_id": f"equipment.footprint.{stem}", "logical_cells": [width, height],
                 "delivery_size": [width * 32, height * 32], "palette_max": 12,
                 "final_path": f"UnityProject/Assets/Game/Resources/Art/FormalAcademyEquipmentFootprints/{stem}.png",
                 "staging_path": f"UnityProject/Assets/Game/Resources/Art/ValidationAcademyEquipmentFootprints/{stem}.png",
                 "source_path": f"{root}/source.png"}
        assets.append(value)
        manifest = {
            "schema": "occ-art-manifest-v1", "contract_version": 1,
            "asset_id": value["asset_id"], "role": "multi_cell_prop_32", "status": "QA_PENDING",
            "provenance": {"source_channel": "codex_builtin_imagegen", "source_descriptor": "Codex built-in image generation; independent single inventory-footprint asset; no icon stretching or board slicing", "source_path": value["source_path"], "source_sha256": "PENDING_GENERATION"},
            "delivery": {"output_path": value["staging_path"], "output_sha256": "PENDING_GENERATION", "native_output_path": None, "logical_cells": [width, height], "palette_max": 12, "required_color_families": []},
            "application": {"runtime_draw_rect": f"{width}x{height} inventory cells at 32 pixels per cell", "default_integer_scale": 2, "minimum_integer_scale": 1},
            "evidence": {"one_x": f"{root}/1x.png", "four_x": f"{root}/4x.png", "grayscale": f"{root}/grayscale.png", "checker": f"{root}/checker.png", "application_contact": "UnityProject/Artifacts/AcademyEquipmentFootprints32/contacts/PENDING.png"},
            "human_review": {"overall": "PENDING", "reviewer": "", "date": "", "silhouette": "PENDING", "material": "PENDING", "perspective": "PENDING", "style": "PENDING", "application": "PENDING", "notes": item["subject"]},
            "unity_import": None,
        }
        (MANIFESTS / f"{stem}.occ-art-manifest-v1.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    if len(assets) != 32:
        raise RuntimeError(f"Expected 32 assets, got {len(assets)}")
    (OUT / "academy_equipment_footprints_32_catalog.json").write_text(json.dumps({"schema": "occ-academy-equipment-footprints-catalog-v1", "count": 32, "assets": assets}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"count": len(assets), "sizes": sorted({tuple(v['logical_cells']) for v in assets})}))


if __name__ == "__main__":
    main()
