#!/usr/bin/env python3
"""Create evidence and a REVIEW_READY manifest for an opaque 32px ground field."""

from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageOps


ROOT = Path(__file__).resolve().parents[2]
BATCH = ROOT / "ArtSource/academy_courtyard_warmbrick_ground_batch"
STEM = "ground_academy_courtyard_warmbrick_surface_a"
SOURCE = BATCH / "sources" / f"{STEM}_source.png"
OUTPUT = BATCH / "outputs" / f"{STEM}_32.png"
QA = BATCH / "qa" / f"{STEM}_pitch12_qa.json"
EVIDENCE = BATCH / "evidence"
MANIFESTS = BATCH / "manifests"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def checker(size: tuple[int, int]) -> Image.Image:
    result = Image.new("RGBA", size, (226, 216, 196, 255))
    draw = ImageDraw.Draw(result)
    for y in range(0, size[1], 4):
        for x in range(0, size[0], 4):
            if (x // 4 + y // 4) % 2:
                draw.rectangle((x, y, x + 3, y + 3), fill=(188, 173, 149, 255))
    return result


def main() -> None:
    qa = json.loads(QA.read_text(encoding="utf-8"))
    if qa["status"] != "PASS_GROUND_FIELD_MANUAL_PITCH_REVIEW_REQUIRED":
        raise SystemExit(f"ground source is not review-ready: {qa['status']}")
    tile = Image.open(OUTPUT).convert("RGBA")
    if tile.size != (32, 32):
        raise SystemExit(f"expected 32x32 output, received {tile.size}")
    EVIDENCE.mkdir(parents=True, exist_ok=True)
    MANIFESTS.mkdir(parents=True, exist_ok=True)
    one_x = EVIDENCE / f"{STEM}_1x.png"
    four_x = EVIDENCE / f"{STEM}_4x.png"
    grayscale = EVIDENCE / f"{STEM}_grayscale.png"
    checker_path = EVIDENCE / f"{STEM}_checker.png"
    contact = EVIDENCE / f"{STEM}_12x9_contact.png"
    tile.save(one_x)
    tile.resize((128, 128), Image.Resampling.NEAREST).save(four_x)
    ImageOps.grayscale(tile).convert("RGBA").save(grayscale)
    board = checker((128, 128))
    board.alpha_composite(tile.resize((128, 128), Image.Resampling.NEAREST))
    board.save(checker_path)

    cell, width, height = 64, 12, 9
    canvas = Image.new("RGBA", (width * cell, height * cell), (40, 31, 27, 255))
    scaled = tile.resize((cell, cell), Image.Resampling.NEAREST)
    for y in range(height):
        for x in range(width):
            canvas.alpha_composite(scaled, (x * cell, y * cell))
    draw = ImageDraw.Draw(canvas)
    for x in range(width + 1):
        draw.line((x * cell, 0, x * cell, height * cell - 1), fill=(71, 47, 39, 210), width=2)
    for y in range(height + 1):
        draw.line((0, y * cell, width * cell - 1, y * cell), fill=(71, 47, 39, 210), width=2)
    canvas.save(contact)

    manifest = {
        "schema": "occ-art-manifest-v1",
        "contract_version": 1,
        "asset_id": "ground.academy_courtyard_warmbrick.surface.a",
        "role": "floor_tile_32",
        "status": "REVIEW_READY",
        "provenance": {
            "source_channel": "codex_builtin_imagegen",
            "source_descriptor": "Independent warm academy brick-field source; declared observed 12px square macro-pixel pitch for a repetitive opaque ground field; no resize or repaint.",
            "source_path": SOURCE.relative_to(ROOT).as_posix(),
            "source_sha256": sha256(SOURCE),
        },
        "delivery": {
            "output_path": OUTPUT.relative_to(ROOT).as_posix(),
            "output_sha256": sha256(OUTPUT),
            "native_output_path": None,
            "logical_cells": [1, 1],
            "palette_max": 6,
            "required_color_families": [],
        },
        "application": {
            "runtime_draw_rect": "one 32x32 logical battle cell; same native tile at 1x, 2x or 4x; visible cell edge is the separate tactical-grid layer",
            "default_integer_scale": 2,
            "minimum_integer_scale": 1,
        },
        "evidence": {
            "one_x": one_x.relative_to(ROOT).as_posix(),
            "four_x": four_x.relative_to(ROOT).as_posix(),
            "grayscale": grayscale.relative_to(ROOT).as_posix(),
            "checker": checker_path.relative_to(ROOT).as_posix(),
            "application_contact": contact.relative_to(ROOT).as_posix(),
        },
        "human_review": {
            "overall": "PENDING", "reviewer": "", "date": "", "silhouette": "PENDING",
            "material": "PENDING", "perspective": "PENDING", "style": "PENDING", "application": "PENDING",
            "notes": "Manual-pitch ground route: requires explicit pixel-scale, material and application review before FORMAL_CANDIDATE.",
        },
        "unity_import": None,
        "ground_window_qa": {
            "qa_path": QA.relative_to(ROOT).as_posix(),
            "macro_pitch": 12,
            "window_bounds": qa["ground_window_bounds_on_logical_grid"],
            "selection": qa["ground_window_selection"],
        },
    }
    path = MANIFESTS / f"{STEM}.occ-art-manifest-v1.json"
    path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"manifest": path.relative_to(ROOT).as_posix(), "contact": contact.relative_to(ROOT).as_posix()}, ensure_ascii=False))


if __name__ == "__main__":
    main()
