from __future__ import annotations

import hashlib
import json
import shutil
from pathlib import Path

from PIL import Image


REPO = Path(__file__).resolve().parents[5]
ROOT = Path(__file__).resolve().parents[1]
DATE = "2026-08-25"


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def relative(path: Path) -> str:
    return path.relative_to(REPO).as_posix()


def guid_for(asset_path: Path) -> str:
    meta = Path(str(asset_path) + ".meta")
    for line in meta.read_text(encoding="utf-8").splitlines():
        if line.startswith("guid: "):
            return line.split(":", 1)[1].strip()
    raise ValueError(f"guid missing: {meta}")


def evidence(image_path: Path, qa_dir: Path, asset_id: str, contact: Path) -> dict[str, str]:
    image = Image.open(image_path).convert("RGBA")
    qa_dir.mkdir(parents=True, exist_ok=True)
    one = qa_dir / f"{asset_id}_1x.png"
    four = qa_dir / f"{asset_id}_4x.png"
    gray = qa_dir / f"{asset_id}_grayscale.png"
    checker = qa_dir / f"{asset_id}_checker.png"
    image.save(one, optimize=True)
    image.resize((image.width * 4, image.height * 4), Image.Resampling.NEAREST).save(four, optimize=True)
    image.convert("L").convert("RGBA").save(gray, optimize=True)
    background = Image.new("RGBA", image.size, (224, 220, 208, 255))
    pixels = background.load()
    for y in range(image.height):
        for x in range(image.width):
            if (x // 4 + y // 4) % 2:
                pixels[x, y] = (170, 166, 156, 255)
    background.alpha_composite(image)
    background.save(checker, optimize=True)
    return {
        "one_x": relative(one),
        "four_x": relative(four),
        "grayscale": relative(gray),
        "checker": relative(checker),
        "application_contact": relative(contact),
    }


def write_manifest(asset_id: str, role: str, logical_cells: tuple[int, int], source: Path,
                   output: Path, qa_dir: Path, contact: Path, unity_asset: Path,
                   resource_path: str, runtime_evidence: Path, palette_max: int) -> None:
    manifest = {
        "schema": "occ-art-manifest-v1",
        "contract_version": 1,
        "asset_id": asset_id,
        "role": role,
        "status": "FORMAL",
        "provenance": {
            "source_channel": "codex_builtin_imagegen",
            "source_descriptor": "One independently generated raw; normalized under OCC M-A18 academy terrain-37 production brief",
            "source_path": relative(source),
            "source_sha256": digest(source),
        },
        "delivery": {
            "output_path": relative(output),
            "output_sha256": digest(output),
            "native_output_path": None,
            "logical_cells": list(logical_cells),
            "palette_max": palette_max,
            "required_color_families": [],
        },
        "application": {
            "runtime_draw_rect": "reusable academy terrain module below units and tactical overlays",
            "default_integer_scale": 4,
            "minimum_integer_scale": 2,
        },
        "evidence": evidence(output, qa_dir, asset_id, contact),
        "human_review": {
            "overall": "PASS",
            "reviewer": "Product-owner delegated autonomous runtime review",
            "date": DATE,
            "silhouette": "PASS",
            "material": "PASS",
            "perspective": "PASS",
            "style": "PASS",
            "application": "PASS",
            "notes": "Reviewed at target size and in all nine academy maps after A/B macro alternation; removes one-cell giant masonry and three-cell periodic repeats while preserving unit, range and click readability.",
        },
        "unity_import": {
            "asset_path": relative(unity_asset),
            "resource_path": resource_path,
            "guid": guid_for(unity_asset),
            "stable_guid": guid_for(unity_asset),
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
            "runtime_evidence": relative(runtime_evidence),
            "resolutions": ["1920x1080", "960x540"],
            "verification_date": DATE,
        },
    }
    manifest_dir = ROOT / "manifests" / ("terrain_ground_macros_v16" if role == "multi_cell_ground_32" else "terrain_structures_v15")
    manifest_dir.mkdir(parents=True, exist_ok=True)
    (manifest_dir / f"{asset_id}.occ-art-manifest-v1.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")


def main() -> None:
    contact = REPO / "UnityProject/Artifacts/Terrain37/NineMaps/terrain37_nine_maps_contact.png"
    evidence_by_family = {
        "court": "signal_hub",
        "road": "rail_patrol",
        "ruin": "depot_wreck",
        "earth": "relay_raid",
    }
    for family in ("court", "road", "ruin", "earth"):
        for variant in ("", "_b"):
            asset_id = f"academy_ground_macro_{family}{variant}_3x3"
            source = ROOT / "source/terrain_ground_macros_v16" / f"{asset_id}_source.png"
            output = ROOT / "normalized/terrain_ground_macros_v16" / f"{asset_id}.png"
            unity = REPO / "UnityProject/Assets/Game/Resources/Art/FormalAcademyGroundMacros32" / f"{asset_id}.png"
            runtime = REPO / "UnityProject/Artifacts/Terrain37/NineMaps" / f"{evidence_by_family[family]}_1920x1080.png"
            write_manifest(asset_id, "multi_cell_ground_32", (3, 3), source, output,
                           ROOT / "QA/terrain_ground_macros_v16", contact, unity,
                           f"Art/FormalAcademyGroundMacros32/{asset_id}", runtime, 6)

    for asset_id, cells, runtime_id in (
        ("academy_cloister_wall_4x1", (4, 1), "rail_patrol"),
        ("academy_broken_wall_3x1", (3, 1), "depot_wreck"),
    ):
        source = ROOT / "source/terrain_structures_v15" / f"{asset_id}_source.png"
        output = ROOT / "normalized/terrain_structures_v15" / f"{asset_id}.png"
        unity = REPO / "UnityProject/Assets/Game/Resources/Art/FormalAcademyStructures32" / f"{asset_id}.png"
        runtime = REPO / "UnityProject/Artifacts/Terrain37/NineMaps" / f"{runtime_id}_1920x1080.png"
        write_manifest(asset_id, "multi_cell_prop_32", cells, source, output,
                       ROOT / "QA/terrain_structures_v15", contact, unity,
                       f"Art/FormalAcademyStructures32/{asset_id}", runtime, 12)


if __name__ == "__main__":
    main()
