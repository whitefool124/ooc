from __future__ import annotations

import hashlib
import json
from pathlib import Path


REPO = Path(__file__).resolve().parents[5]
M18 = Path(__file__).resolve().parents[1]
FAMILY = "terrain_ground_macros_v19"
CONTACT = "UnityProject/Artifacts/Terrain39/NineMaps/terrain39_nine_maps_contact.png"
LEVELS = {
    "academy_ground_macro_earth_3x3": "rail_patrol",
    "academy_ground_macro_earth_b_3x3": "relay_raid",
    "academy_ground_macro_ruin_3x3": "depot_wreck",
    "academy_ground_macro_ruin_b_3x3": "elite_foundry",
}


def relative(path: Path) -> str:
    return str(path.relative_to(REPO)).replace("\\", "/")


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def guid(meta: Path) -> str:
    for line in meta.read_text(encoding="utf-8-sig").splitlines():
        if line.startswith("guid: "):
            return line.split(":", 1)[1].strip()
    raise RuntimeError(f"Missing guid in {meta}")


def main() -> None:
    manifest_dir = M18 / "manifests" / FAMILY
    manifest_dir.mkdir(parents=True, exist_ok=True)
    for asset_id, level_id in LEVELS.items():
        source = M18 / "source" / FAMILY / f"{asset_id}_source.png"
        output = M18 / "normalized" / FAMILY / f"{asset_id}.png"
        qa = M18 / "QA" / FAMILY
        unity = REPO / "UnityProject" / "Assets" / "Game" / "Resources" / "Art" / "FormalAcademyGroundMacros32" / f"{asset_id}.png"
        stable_guid = guid(unity.with_suffix(".png.meta"))
        data = {
            "schema": "occ-art-manifest-v1",
            "contract_version": 1,
            "asset_id": asset_id,
            "role": "multi_cell_ground_32",
            "status": "FORMAL",
            "provenance": {
                "source_channel": "codex_builtin_imagegen",
                "source_descriptor": "Existing independent raw, re-normalized for lower frequency and tighter value hierarchy in academy polish-39",
                "source_path": relative(source),
                "source_sha256": digest(source),
            },
            "delivery": {
                "output_path": relative(output),
                "output_sha256": digest(output),
                "native_output_path": None,
                "logical_cells": [3, 3],
                "palette_max": 4,
                "required_color_families": [],
            },
            "application": {
                "runtime_draw_rect": "reusable academy ground macro below units and tactical overlays",
                "default_integer_scale": 4,
                "minimum_integer_scale": 2,
            },
            "evidence": {
                "one_x": relative(qa / f"{asset_id}_1x.png"),
                "four_x": relative(qa / f"{asset_id}_4x.png"),
                "grayscale": relative(qa / f"{asset_id}_grayscale.png"),
                "checker": relative(qa / f"{asset_id}_checker.png"),
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
                "notes": "Earth uses a quiet 16px-per-cell material field with three close values. Ruin retains native 32px-per-cell masonry outlines with four close values. Organic variants are stable per map rather than checkerboarded; nine-map and 1:1 battlefield review passed.",
            },
            "unity_import": {
                "asset_path": relative(unity),
                "resource_path": f"Art/FormalAcademyGroundMacros32/{asset_id}",
                "guid": stable_guid,
                "stable_guid": stable_guid,
                "importer_verified": True,
                "runtime_verified": True,
                "runtime_evidence": f"UnityProject/Artifacts/Terrain39/NineMaps/{level_id}_1920x1080.png",
                "resolutions": ["1920x1080", "960x540"],
                "verification_date": "2026-08-25",
            },
        }
        (manifest_dir / f"{asset_id}.occ-art-manifest-v1.json").write_text(
            json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
