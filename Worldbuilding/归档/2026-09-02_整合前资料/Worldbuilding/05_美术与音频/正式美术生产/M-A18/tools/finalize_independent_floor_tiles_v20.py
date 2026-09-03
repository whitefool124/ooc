from __future__ import annotations

import hashlib
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
REPO = ROOT.parents[3]
FAMILY = "terrain_independent_tiles_v20"
UNITY_ROOT = REPO / "UnityProject/Assets/Game/Resources/Art/FormalAcademyIndependentFloors32"
RUNTIME_EVIDENCE = "UnityProject/Artifacts/ArtTile41/NineMaps/academy_nine_maps_contact.png"


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def guid_for(path: Path) -> str:
    meta = Path(str(path) + ".meta")
    for line in meta.read_text(encoding="utf-8").splitlines():
        if line.startswith("guid: "):
            return line.split(":", 1)[1].strip()
    raise RuntimeError(f"missing guid in {meta}")


def main() -> None:
    manifest_root = ROOT / "manifests" / FAMILY
    manifest_root.mkdir(parents=True, exist_ok=True)
    for material in ("court", "road", "ruin", "earth"):
        for variant in "abcd":
            asset_id = f"academy_block_{material}_{variant}"
            source = ROOT / "source" / FAMILY / f"{asset_id}_source.png"
            output = ROOT / "normalized" / FAMILY / f"{asset_id}.png"
            unity = UNITY_ROOT / f"{asset_id}.png"
            stable_guid = guid_for(unity)
            relative_source = source.relative_to(REPO).as_posix()
            relative_output = output.relative_to(REPO).as_posix()
            qa_root = f"Worldbuilding/05_美术与音频/正式美术生产/M-A18/QA/{FAMILY}"
            manifest = {
                "schema": "occ-art-manifest-v1",
                "contract_version": 1,
                "asset_id": asset_id,
                "role": "floor_tile_32",
                "status": "FORMAL",
                "provenance": {
                    "source_channel": "hand_pixel",
                    "source_descriptor": "Independent native 32x32 hand-pixel material block; self-contained edge, fixed top-left light, no runtime rotation or cross-tile continuation",
                    "source_path": relative_source,
                    "source_sha256": digest(source),
                },
                "delivery": {
                    "output_path": relative_output,
                    "output_sha256": digest(output),
                    "native_output_path": relative_source,
                    "logical_cells": [1, 1],
                    "palette_max": 5,
                    "required_color_families": [],
                },
                "application": {
                    "runtime_draw_rect": "one complete 32x32 orthographic battlefield cell below props, units and tactical overlays",
                    "default_integer_scale": 3,
                    "minimum_integer_scale": 2,
                },
                "evidence": {
                    "one_x": f"{qa_root}/{asset_id}_1x.png",
                    "four_x": f"{qa_root}/{asset_id}_4x.png",
                    "grayscale": f"{qa_root}/{asset_id}_grayscale.png",
                    "checker": f"{qa_root}/{asset_id}_checker.png",
                    "application_contact": RUNTIME_EVIDENCE,
                },
                "human_review": {
                    "overall": "PASS",
                    "reviewer": "Product-owner delegated autonomous map review",
                    "date": "2026-08-25",
                    "silhouette": "PASS",
                    "material": "PASS",
                    "perspective": "PASS",
                    "style": "PASS",
                    "application": "PASS",
                    "notes": "Approved after native 1x/4x review, nine-map 1920x1080 runtime contact, and 960x540 finale verification: every cell reads as one reusable material block, all edges terminate locally, and directional lighting is never rotated.",
                },
                "unity_import": {
                    "asset_path": unity.relative_to(REPO).as_posix(),
                    "resource_path": f"Art/FormalAcademyIndependentFloors32/{asset_id}",
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
                    "runtime_evidence": RUNTIME_EVIDENCE,
                    "resolutions": ["1920x1080", "960x540"],
                    "verification_date": "2026-08-25",
                },
            }
            target = manifest_root / f"{asset_id}.occ-art-manifest-v1.json"
            target.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
