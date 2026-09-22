"""Finalize the approved enemy-destination diamond and timeline-hover arrow."""

from __future__ import annotations

import hashlib
import json
from datetime import date
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
RUNTIME_CAPTURE = ROOT / "UnityProject/Reports/CombatTestArena/combat_markers_formal_1920x1080.png"

ASSETS = {
    "enemy_intent_destination_v2": {
        "asset_id": "ui.combat.enemy_intent_destination",
        "role": "ui_trim_square_32",
        "source": "ArtSource/combat_ui_refresh_2026-09-22/enemy_intent_destination_v2/source.png",
        "candidate": "ArtSource/combat_ui_refresh_2026-09-22/enemy_intent_destination_v2/candidate_32.png",
        "final": "UnityProject/Assets/Game/Resources/Art/FormalTacticalOverlays32V2/enemy_intent_diamond.png",
        "qa": "ArtSource/combat_ui_refresh_2026-09-22/enemy_intent_destination_v2/qa.json",
        "guid": "a0b0a84f1bb98364990cb44c6edae23e",
        "ppu": 32,
        "palette": 10,
        "tier": 2,
        "runtime": "32x32 transparent destination glyph in a 64x64 rect at the 192px battlefield tier",
        "descriptor": "User-approved second-pass bright-orange enemy destination diamond; smaller 14x14 subject on a 32x32 transparent canvas.",
    },
    "timeline_hover_silver_arrow_v3": {
        "asset_id": "ui.combat.timeline_hover_arrow",
        "role": "combat_indicator_16",
        "source": "ArtSource/combat_ui_refresh_2026-09-22/timeline_hover_silver_arrow_v3/source.png",
        "candidate": "ArtSource/combat_ui_refresh_2026-09-22/timeline_hover_silver_arrow_v3/candidate_16.png",
        "final": "UnityProject/Assets/Game/Resources/Art/FormalCombatIndicators16/timeline_hover_arrow.png",
        "qa": "ArtSource/combat_ui_refresh_2026-09-22/timeline_hover_silver_arrow_v3/qa.json",
        "guid": "4cfdbed454e847144a80844ea749c164",
        "ppu": 16,
        "palette": 8,
        "tier": 3,
        "runtime": "16x16 silver-white metal arrow displayed at exact 3x above the timeline-hovered unit",
        "descriptor": "User-approved third-pass silver-white metal down arrow; 12x14 subject on a native 16x16 transparent canvas.",
    },
}


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def relative(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def checker_contact(image: Image.Image, scale: int = 6) -> Image.Image:
    size = (image.width * scale, image.height * scale)
    board = Image.new("RGBA", size, (205, 205, 205, 255))
    draw = ImageDraw.Draw(board)
    block = max(4, scale * 2)
    for y in range(0, size[1], block):
        for x in range(0, size[0], block):
            if (x // block + y // block) % 2:
                draw.rectangle((x, y, x + block - 1, y + block - 1), fill=(238, 238, 238, 255))
    board.alpha_composite(image.resize(size, Image.Resampling.NEAREST))
    return board


def write_evidence(folder: Path, stem: str, image: Image.Image) -> dict[str, str]:
    evidence = folder / "evidence"
    evidence.mkdir(parents=True, exist_ok=True)
    one = evidence / f"{stem}_1x.png"
    four = evidence / f"{stem}_4x.png"
    six = evidence / f"{stem}_6x.png"
    gray = evidence / f"{stem}_grayscale.png"
    check = evidence / f"{stem}_checker.png"
    image.save(one)
    image.resize((image.width * 4, image.height * 4), Image.Resampling.NEAREST).save(four)
    image.resize((image.width * 6, image.height * 6), Image.Resampling.NEAREST).save(six)
    alpha = image.getchannel("A")
    luminosity = image.convert("L")
    Image.merge("RGBA", (luminosity, luminosity, luminosity, alpha)).save(gray)
    checker_contact(image).save(check)
    return {
        "one_x": relative(one),
        "four_x": relative(four),
        "six_x": relative(six),
        "grayscale": relative(gray),
        "checker": relative(check),
        "application_contact": relative(RUNTIME_CAPTURE),
    }


def main() -> None:
    if not RUNTIME_CAPTURE.is_file():
        raise FileNotFoundError(RUNTIME_CAPTURE)
    outputs = []
    for stem, spec in ASSETS.items():
        source = ROOT / spec["source"]
        candidate = ROOT / spec["candidate"]
        final = ROOT / spec["final"]
        qa_path = ROOT / spec["qa"]
        if candidate.read_bytes() != final.read_bytes():
            raise RuntimeError(f"Unity delivery differs from approved candidate: {stem}")
        image = Image.open(candidate).convert("RGBA")
        folder = candidate.parent
        evidence = write_evidence(folder, stem, image)
        qa = json.loads(qa_path.read_text(encoding="utf-8"))
        manifest = {
            "schema": "occ-art-manifest-v1",
            "contract_version": 1,
            "asset_id": spec["asset_id"],
            "role": spec["role"],
            "status": "FORMAL",
            "provenance": {
                "source_channel": "codex_builtin_imagegen",
                "source_descriptor": spec["descriptor"] + " Losslessly decoded without resize, interpolation, repaint, dithering, or new colours.",
                "source_path": spec["source"],
                "source_sha256": digest(source),
            },
            "delivery": {
                "output_path": spec["final"],
                "output_sha256": digest(final),
                "native_output_path": None,
                "logical_cells": None,
                "palette_max": spec["palette"],
                "required_color_families": [],
            },
            "application": {
                "runtime_draw_rect": spec["runtime"],
                "default_integer_scale": spec["tier"],
                "minimum_integer_scale": 1,
            },
            "evidence": evidence,
            "human_review": {
                "overall": "PASS",
                "reviewer": "User approval and Codex OCC runtime review",
                "date": str(date.today()),
                "silhouette": "PASS",
                "material": "PASS",
                "perspective": "PASS",
                "style": "PASS",
                "application": "PASS",
                "illumination": "PASS",
                "notes": "The user explicitly requested implementation after the final size/material revisions. Runtime readback confirmed both textures loaded; the hover arrow avoids all active intent badges after global annotation placement.",
            },
            "unity_import": {
                "asset_path": spec["final"].removeprefix("UnityProject/"),
                "stable_guid": spec["guid"],
                "texture_type": "Sprite",
                "pixels_per_unit": spec["ppu"],
                "filter_mode": "Point",
                "mesh_type": "Full Rect",
                "compression": "None",
                "mipmaps": False,
                "wrap_mode": "Clamp",
                "atlas_padding": 0,
                "atlas_rotation": False,
                "atlas_tight_packing": False,
                "atlas_extrude": 0,
                "importer_verified": True,
                "runtime_verified": True,
                "runtime_lattice": {
                    "pass": True,
                    "tier": spec["tier"],
                    "capture": relative(RUNTIME_CAPTURE),
                    "notes": "Actual CombatTestArena runtime capture plus exact importer and live object readback.",
                },
            },
            "pixel_grid_qa": {
                "qa_path": spec["qa"],
                "inferred_subject_pixel_pitch": qa["inferred_subject_pixel_pitch"],
                "subject_lattice_confidence": qa["subject_lattice_confidence"],
                "subject_logical_size": qa["subject_logical_size"],
                "target_canvas": qa["target_canvas"],
                "decoded_visible_colours": qa["decoded_visible_colours_after_cleanup"],
                "geometric_resize_or_interpolation_performed": qa["geometric_resize_or_interpolation_performed"],
                "new_colours_introduced": qa["new_colours_introduced"],
            },
        }
        manifest_path = folder / f"{stem}.occ-art-manifest-v1.json"
        manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        outputs.append(relative(manifest_path))
    print(json.dumps({"status": "FORMAL", "manifests": outputs}, ensure_ascii=False))


if __name__ == "__main__":
    main()
