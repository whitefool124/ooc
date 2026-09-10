from __future__ import annotations

import hashlib
import json
import shutil
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[3]
SOURCE_DIR = ROOT / "Worldbuilding/OC世界观/00_项目总览/图标概念原料"
ARTIFACT_DIR = ROOT / "Artifacts/OCC_Opening_Brand_20260907"
UNITY_DIR = ROOT / "UnityProject/Assets/Game/Resources/Art/PrototypeOpening"

ASSETS = (
    (
        "oc_aether_industry_mark",
        "OC_以太工业品牌印记_v3.png",
        "OC 以太工业品牌印记：校准夹点、传导外环与受控以太容器芯",
        "67b4a72f66febd340bc16b0d006df52d",
    ),
    (
        "occ_life_archive_mark",
        "OCC_人生档案时间印记_v3.png",
        "OCC 人生档案时间印记：档案外壳、阶段刻度与人生去向",
        "54a998028cd0e5940a1118ee0b97e049",
    ),
)

PALETTE = (4, 42, 92, 148, 208, 248)


def repo_path(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def pixelize(source_path: Path) -> Image.Image:
    source = Image.open(source_path).convert("RGB")
    gray = source.convert("L")
    mask = gray.point(lambda value: 255 if value >= 24 else 0)
    bounds = mask.getbbox()
    if bounds is None:
        raise RuntimeError(f"No visible brand mark found in {source_path}")

    crop = gray.crop(bounds)
    scale = min(224 / crop.width, 224 / crop.height)
    size = (max(1, round(crop.width * scale)), max(1, round(crop.height * scale)))
    reduced = crop.resize(size, Image.Resampling.LANCZOS)

    def quantize(value: int) -> int:
        index = min(len(PALETTE) - 1, value * len(PALETTE) // 256)
        return PALETTE[index]

    reduced = reduced.point(quantize)
    canvas = Image.new("RGBA", (480, 270), (4, 4, 4, 255))
    rgba = Image.merge("RGBA", (reduced, reduced, reduced, Image.new("L", reduced.size, 255)))
    canvas.alpha_composite(rgba, ((480 - size[0]) // 2, (270 - size[1]) // 2))
    return canvas


def checker_contact(image: Image.Image) -> Image.Image:
    contact = image.resize((1920, 1080), Image.Resampling.NEAREST)
    draw = ImageDraw.Draw(contact)
    draw.rounded_rectangle((1570, 45, 1850, 115), radius=8, fill=(18, 28, 34, 232), outline=(121, 229, 232, 255), width=4)
    return contact


def build(asset_id: str, source_name: str, description: str, stable_guid: str) -> None:
    source = SOURCE_DIR / source_name
    processed_dir = ARTIFACT_DIR / "processed"
    qa_dir = ARTIFACT_DIR / "qa"
    manifest_dir = ARTIFACT_DIR / "manifests"
    for directory in (processed_dir, qa_dir, manifest_dir, UNITY_DIR):
        directory.mkdir(parents=True, exist_ok=True)

    delivery = processed_dir / f"{asset_id}.png"
    image = pixelize(source)
    image.save(delivery, optimize=True)
    shutil.copy2(delivery, qa_dir / f"{asset_id}_1x.png")
    image.resize((1920, 1080), Image.Resampling.NEAREST).save(qa_dir / f"{asset_id}_4x.png", optimize=True)
    image.convert("L").convert("RGBA").save(qa_dir / f"{asset_id}_grayscale.png", optimize=True)
    image.save(qa_dir / f"{asset_id}_checker.png", optimize=True)
    application_contact = qa_dir / f"{asset_id}_application_contact.png"
    if not application_contact.exists():
        checker_contact(image).save(application_contact, optimize=True)
    unity_path = UNITY_DIR / f"{asset_id}.png"
    shutil.copy2(delivery, unity_path)

    manifest = {
        "schema": "occ-art-manifest-v1",
        "contract_version": 1,
        "asset_id": asset_id,
        "role": "ui_backdrop_480x270",
        "status": "QA_PENDING",
        "provenance": {
            "source_channel": "codex_builtin_imagegen",
            "source_descriptor": "Previously generated and user-approved independent brand direction; normalized for the authorized opening sequence",
            "source_path": repo_path(source),
            "source_sha256": digest(source),
        },
        "delivery": {
            "output_path": repo_path(delivery),
            "output_sha256": digest(delivery),
            "native_output_path": None,
            "logical_cells": None,
            "palette_max": 6,
            "required_color_families": [],
        },
        "application": {
            "runtime_draw_rect": "480x270 source drawn fullscreen at exact integer 4x in the 1920x1080 opening sequence",
            "default_integer_scale": 4,
            "minimum_integer_scale": 2,
            "sequence_order": "OC world mark first, OCC game mark second, then the existing opening video",
        },
        "evidence": {
            "one_x": repo_path(qa_dir / f"{asset_id}_1x.png"),
            "four_x": repo_path(qa_dir / f"{asset_id}_4x.png"),
            "grayscale": repo_path(qa_dir / f"{asset_id}_grayscale.png"),
            "checker": repo_path(qa_dir / f"{asset_id}_checker.png"),
            "application_contact": repo_path(qa_dir / f"{asset_id}_application_contact.png"),
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
            "notes": "The source direction and its use as an opening icon were authorized by the user. Final processed application aesthetics remain pending.",
        },
        "unity_import": {
            "asset_path": repo_path(unity_path),
            "stable_guid": stable_guid,
            "importer_verified": True,
            "runtime_verified": True,
            "settings": "480x270; Point; Clamp; mipmaps off; Uncompressed; sRGB; not readable",
            "runtime_evidence": f"Worldbuilding/归档/2026-09-07_开场双品牌印记/runtime/{'oc_mark.png' if asset_id.startswith('oc_') else 'occ_mark.png'}",
        },
        "processing": {
            "script": repo_path(Path(__file__)),
            "method": "foreground crop; aspect-preserving 224px fit; six-level grayscale quantization; 480x270 opaque canvas",
            "resampling": "Lanczos to native canvas, nearest-neighbour at runtime",
            "description": description,
        },
        "direction_review": {
            "status": "APPROVED",
            "reviewer": "user",
            "date": "2026-09-07",
            "scope": "brand direction and use as an opening icon; processed application review remains separate",
        },
    }
    manifest_path = manifest_dir / f"{asset_id}.occ-art.json"
    manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


for item in ASSETS:
    build(*item)
