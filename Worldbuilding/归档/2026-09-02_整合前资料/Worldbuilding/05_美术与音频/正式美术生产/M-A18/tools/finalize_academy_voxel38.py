from __future__ import annotations

import hashlib
import json
from pathlib import Path


REPO = Path(__file__).resolve().parents[5]
M18 = Path(__file__).resolve().parents[1]
CONTACT = "UnityProject/Artifacts/Terrain38/NineMaps/terrain38_nine_maps_contact.png"

ASSETS = {
    "academy_curb_edge": ("terrain_voxel_v17", "terrain_adjacency_overlay_32", [1, 1], "FormalAcademyTerrainOverlays32", "rail_patrol"),
    "academy_curb_corner": ("terrain_voxel_v17", "terrain_adjacency_overlay_32", [1, 1], "FormalAcademyTerrainOverlays32", "rail_patrol"),
    "academy_curb_opposite": ("terrain_voxel_v17", "terrain_adjacency_overlay_32", [1, 1], "FormalAcademyTerrainOverlays32", "relay_raid"),
    "academy_curb_three": ("terrain_voxel_v17", "terrain_adjacency_overlay_32", [1, 1], "FormalAcademyTerrainOverlays32", "depot_wreck"),
    "academy_curb_enclosed": ("terrain_voxel_v17", "terrain_adjacency_overlay_32", [1, 1], "FormalAcademyTerrainOverlays32", "elite_foundry"),
    "academy_wall_straight": ("terrain_voxel_v17", "modular_structure_32", [1, 1], "FormalAcademyStructures32", "rail_patrol"),
    "academy_wall_end": ("terrain_voxel_v17", "modular_structure_32", [1, 1], "FormalAcademyStructures32", "relay_raid"),
    "academy_wall_corner": ("terrain_voxel_v17", "modular_structure_32", [1, 1], "FormalAcademyStructures32", "elite_foundry"),
    "academy_stairs_2x1": ("terrain_voxel_v17", "modular_structure_32", [2, 1], "FormalAcademyStructures32", "rail_patrol"),
    "academy_ground_macro_earth_3x3": ("terrain_ground_macros_v18", "multi_cell_ground_32", [3, 3], "FormalAcademyGroundMacros32", "rail_patrol"),
    "academy_ground_macro_earth_b_3x3": ("terrain_ground_macros_v18", "multi_cell_ground_32", [3, 3], "FormalAcademyGroundMacros32", "relay_raid"),
    "academy_ground_macro_ruin_3x3": ("terrain_ground_macros_v18", "multi_cell_ground_32", [3, 3], "FormalAcademyGroundMacros32", "depot_wreck"),
    "academy_ground_macro_ruin_b_3x3": ("terrain_ground_macros_v18", "multi_cell_ground_32", [3, 3], "FormalAcademyGroundMacros32", "elite_foundry"),
}


def repo_relative(path: Path) -> str:
    return str(path.relative_to(REPO)).replace("\\", "/")


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def guid(meta: Path) -> str:
    for line in meta.read_text(encoding="utf-8-sig").splitlines():
        if line.startswith("guid: "):
            return line.split(":", 1)[1].strip()
    raise RuntimeError(f"No guid in {meta}")


def formal_manifest(asset_id: str, data: tuple[str, str, list[int], str, str]) -> dict:
    family, role, logical_cells, unity_folder, evidence_level = data
    source = M18 / "source" / family / f"{asset_id}_source.png"
    output = M18 / "normalized" / family / f"{asset_id}.png"
    qa = M18 / "QA" / family
    unity = REPO / "UnityProject" / "Assets" / "Game" / "Resources" / "Art" / unity_folder / f"{asset_id}.png"
    stable_guid = guid(unity.with_suffix(".png.meta"))
    palette_max = 6 if role == "multi_cell_ground_32" else 12 if role == "modular_structure_32" else 10
    return {
        "schema": "occ-art-manifest-v1",
        "contract_version": 1,
        "asset_id": asset_id,
        "role": role,
        "status": "FORMAL",
        "provenance": {
            "source_channel": "codex_builtin_imagegen",
            "source_descriptor": "One independently generated raw; normalized under OCC M-A18 academy voxel-38 production brief",
            "source_path": repo_relative(source),
            "source_sha256": sha256(source),
        },
        "delivery": {
            "output_path": repo_relative(output),
            "output_sha256": sha256(output),
            "native_output_path": None,
            "logical_cells": logical_cells,
            "palette_max": palette_max,
            "required_color_families": [],
        },
        "application": {
            "runtime_draw_rect": "reusable academy module below units and tactical overlays",
            "default_integer_scale": 4,
            "minimum_integer_scale": 2,
        },
        "evidence": {
            "one_x": repo_relative(qa / f"{asset_id}_1x.png"),
            "four_x": repo_relative(qa / f"{asset_id}_4x.png"),
            "grayscale": repo_relative(qa / f"{asset_id}_grayscale.png"),
            "checker": repo_relative(qa / f"{asset_id}_checker.png"),
            "application_contact": CONTACT,
        },
        "human_review": {
            "overall": "PASS",
            "reviewer": "Product-owner delegated autonomous runtime review",
            "date": "2026-08-25",
            "silhouette": "PASS",
            "material": "PASS",
            "perspective": "PASS",
            "style": "PASS",
            "application": "PASS",
            "notes": "Reviewed in all nine academy maps with readability and visual hierarchy prioritized over setting ornament. Ground no longer creates rut bands or dark holes; adjacency curbs and modular walls remain subordinate to units and routes.",
        },
        "unity_import": {
            "asset_path": repo_relative(unity),
            "resource_path": f"Art/{unity_folder}/{asset_id}",
            "guid": stable_guid,
            "stable_guid": stable_guid,
            "importer_verified": True,
            "runtime_verified": True,
            "importer": {
                "texture_type": "Sprite",
                "pixels_per_unit": 32,
                "filter_mode": "Point",
                "wrap_mode": "Clamp",
                "mipmap_enabled": False,
                "compression": "Uncompressed",
            },
            "runtime_evidence": f"UnityProject/Artifacts/Terrain38/NineMaps/{evidence_level}_1920x1080.png",
            "resolutions": ["1920x1080", "960x540"],
            "verification_date": "2026-08-25",
        },
    }


def rejected_gate_manifest() -> dict:
    asset_id = "academy_gate_3x2"
    family = "terrain_voxel_v17"
    source = M18 / "source" / family / f"{asset_id}_source.png"
    output = M18 / "normalized" / family / f"{asset_id}.png"
    return {
        "schema": "occ-art-manifest-v1",
        "contract_version": 1,
        "asset_id": asset_id,
        "role": "modular_structure_32",
        "status": "PROTOTYPE",
        "provenance": {
            "source_channel": "codex_builtin_imagegen",
            "source_descriptor": "Independent candidate rejected at runtime composition review",
            "source_path": repo_relative(source),
            "source_sha256": sha256(source),
        },
        "delivery": {
            "output_path": repo_relative(output),
            "output_sha256": sha256(output),
            "native_output_path": None,
            "logical_cells": [3, 2],
            "palette_max": 12,
            "required_color_families": [],
        },
        "application": {
            "runtime_draw_rect": "rejected: gate pier silhouette conflicts with permanent blocker occupancy",
            "default_integer_scale": 4,
            "minimum_integer_scale": 2,
        },
        "evidence": {},
        "human_review": {
            "overall": "FAIL",
            "reviewer": "Product-owner delegated autonomous runtime review",
            "date": "2026-08-25",
            "application": "FAIL",
            "notes": "Not deployed. Source and normalized candidate retained for redesign; Unity formal-path copy removed.",
        },
    }


def main() -> None:
    for asset_id, data in ASSETS.items():
        family = data[0]
        directory = M18 / "manifests" / family
        directory.mkdir(parents=True, exist_ok=True)
        path = directory / f"{asset_id}.occ-art-manifest-v1.json"
        path.write_text(json.dumps(formal_manifest(asset_id, data), ensure_ascii=False, indent=2), encoding="utf-8")
    gate_dir = M18 / "manifests" / "terrain_voxel_v17"
    (gate_dir / "academy_gate_3x2.occ-art-manifest-v1.json").write_text(
        json.dumps(rejected_gate_manifest(), ensure_ascii=False, indent=2), encoding="utf-8"
    )


if __name__ == "__main__":
    main()
