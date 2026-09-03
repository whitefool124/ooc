from __future__ import annotations

import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[5]
PACK = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A18"
FAMILY = "academy_modules_v21"

ASSETS = [
    ("academy_floor_drain_grate", "single_cell_prop_32", [1, 1], 10),
    ("academy_floor_maintenance_hatch", "single_cell_prop_32", [1, 1], 10),
    ("academy_floor_repair_plate", "single_cell_prop_32", [1, 1], 10),
    ("academy_floor_convergence_scribe", "single_cell_prop_32", [1, 1], 10),
    ("academy_prop_wood_crate", "single_cell_prop_32", [1, 1], 10),
    ("academy_prop_iron_crate", "single_cell_prop_32", [1, 1], 10),
    ("academy_prop_instrument_rack", "single_cell_prop_32", [1, 1], 10),
    ("academy_prop_potion_case", "single_cell_prop_32", [1, 1], 10),
    ("academy_prop_maintenance_lamp", "single_cell_prop_32", [1, 1], 10),
    ("academy_prop_stone_bollard", "single_cell_prop_32", [1, 1], 10),
    ("academy_workbench_2x1", "multi_cell_prop_32", [2, 1], 12),
    ("academy_archive_cabinet_2x1", "multi_cell_prop_32", [2, 1], 12),
    ("academy_pipe_service_rack_2x1", "multi_cell_prop_32", [2, 1], 12),
    ("academy_aether_device_2x2", "multi_cell_prop_32", [2, 2], 12),
    ("academy_wall_end_n", "modular_structure_32", [1, 1], 12),
    ("academy_wall_end_e", "modular_structure_32", [1, 1], 12),
    ("academy_wall_end_s", "modular_structure_32", [1, 1], 12),
    ("academy_wall_end_w", "modular_structure_32", [1, 1], 12),
]


def rel(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def main() -> None:
    manifest_dir = PACK / "manifests" / FAMILY
    manifest_dir.mkdir(parents=True, exist_ok=True)
    for asset_id, role, cells, palette_max in ASSETS:
        source = PACK / "source" / FAMILY / f"{asset_id}_source.png"
        output = PACK / "normalized" / FAMILY / f"{asset_id}.png"
        qa = PACK / "QA" / FAMILY
        manifest = {
            "schema": "occ-art-manifest-v1",
            "contract_version": 1,
            "asset_id": asset_id,
            "role": role,
            "status": "QA_PENDING",
            "provenance": {
                "source_channel": "codex_builtin_imagegen",
                "source_descriptor": "Independent single-asset generation under ART-ACADEMY-MODULES-43 brief; no board slicing",
                "source_path": rel(source),
                "source_sha256": "PENDING_GENERATION",
            },
            "delivery": {
                "output_path": rel(output),
                "output_sha256": "PENDING_NORMALIZATION",
                "native_output_path": None,
                "logical_cells": cells,
                "palette_max": palette_max,
                "required_color_families": [],
            },
            "application": {
                "runtime_draw_rect": "reusable academy visual module below units and tactical overlays; no logical-cell mutation",
                "default_integer_scale": 4,
                "minimum_integer_scale": 2,
            },
            "evidence": {
                "one_x": rel(qa / f"{asset_id}_1x.png"),
                "four_x": rel(qa / f"{asset_id}_4x.png"),
                "grayscale": rel(qa / f"{asset_id}_grayscale.png"),
                "checker": rel(qa / f"{asset_id}_checker.png"),
                "application_contact": "UnityProject/Artifacts/ArtModules43/module43_three_maps_contact.png",
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
                "notes": "",
            },
            "unity_import": None,
        }
        target = manifest_dir / f"{asset_id}.occ-art-manifest-v1.json"
        if target.exists():
            raise FileExistsError(target)
        target.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
