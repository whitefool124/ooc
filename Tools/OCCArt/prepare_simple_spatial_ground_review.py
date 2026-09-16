#!/usr/bin/env python3
"""Prepare non-destructive review evidence for the simple spatial ground field."""

from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageOps


ROOT = Path(__file__).resolve().parents[2]
BATCH = ROOT / "ArtSource/academy_courtyard_simple_spatial_batch"
SOURCE = BATCH / "sources/ground_academy_courtyard_simple_spatial_surface_a_source.png"
OUTPUT = BATCH / "outputs/review_ground_academy_courtyard_simple_spatial_origin0_32.png"
QA = BATCH / "qa/review_ground_academy_courtyard_simple_spatial_origin0_qa.json"
STEM = "ground_academy_courtyard_simple_spatial_surface_a"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> None:
    qa = json.loads(QA.read_text(encoding="utf-8"))
    if qa["status"] != "PASS_GROUND_FIELD_MANUAL_PITCH_REVIEW_REQUIRED":
        raise SystemExit(f"ground source is not review-ready: {qa['status']}")
    tile = Image.open(OUTPUT).convert("RGBA")
    if tile.size != (32, 32):
        raise SystemExit(f"expected 32x32 output, got {tile.size}")
    evidence, manifests = BATCH / "evidence", BATCH / "manifests"
    evidence.mkdir(parents=True, exist_ok=True)
    manifests.mkdir(parents=True, exist_ok=True)
    one, four = evidence / f"{STEM}_1x.png", evidence / f"{STEM}_4x.png"
    gray, check = evidence / f"{STEM}_grayscale.png", evidence / f"{STEM}_checker.png"
    contact = evidence / f"{STEM}_tactical_grid_12x9_contact.png"
    tile.save(one)
    tile.resize((128, 128), Image.Resampling.NEAREST).save(four)
    ImageOps.grayscale(tile).convert("RGBA").save(gray)
    board = Image.new("RGBA", (128, 128), (226, 216, 196, 255))
    board.alpha_composite(tile.resize((128, 128), Image.Resampling.NEAREST))
    board.save(check)
    scale, cols, rows = 64, 12, 9
    image = Image.new("RGBA", (scale * cols, scale * rows), (68, 48, 38, 255))
    enlarged = tile.resize((scale, scale), Image.Resampling.NEAREST)
    for y in range(rows):
        for x in range(cols):
            image.alpha_composite(enlarged, (x * scale, y * scale))
    draw = ImageDraw.Draw(image)
    for x in range(cols + 1):
        draw.line((x * scale, 0, x * scale, rows * scale - 1), fill=(91, 62, 48, 255), width=2)
    for y in range(rows + 1):
        draw.line((0, y * scale, cols * scale - 1, y * scale), fill=(91, 62, 48, 255), width=2)
    image.save(contact)
    manifest = {
        "schema": "occ-art-manifest-v1", "contract_version": 1,
        "asset_id": "ground.academy_courtyard_simple_spatial.surface.a", "role": "floor_tile_32", "status": "REVIEW_READY",
        "provenance": {"source_channel": "codex_builtin_imagegen", "source_descriptor": "Independent opaque warm ivory academy ground field, using declared observed 26px macro-pixel pitch. A lossless phase-aligned 32x32 window is selected; no resize, interpolation, repaint or new colours.", "source_path": SOURCE.relative_to(ROOT).as_posix(), "source_sha256": sha256(SOURCE)},
        "delivery": {"output_path": OUTPUT.relative_to(ROOT).as_posix(), "output_sha256": sha256(OUTPUT), "native_output_path": None, "logical_cells": [1, 1], "palette_max": 6, "required_color_families": []},
        "application": {"runtime_draw_rect": "one 32x32 logical battle cell; visible border is a separate one-native-pixel warm-umber tactical grid, never baked into this reusable ground material", "default_integer_scale": 2, "minimum_integer_scale": 1},
        "evidence": {"one_x": one.relative_to(ROOT).as_posix(), "four_x": four.relative_to(ROOT).as_posix(), "grayscale": gray.relative_to(ROOT).as_posix(), "checker": check.relative_to(ROOT).as_posix(), "application_contact": contact.relative_to(ROOT).as_posix()},
        "human_review": {"overall": "PENDING", "reviewer": "", "date": "", "silhouette": "PENDING", "material": "PENDING", "perspective": "PENDING", "style": "PENDING", "application": "PENDING", "notes": "Manual-pitch ground route. Confirm the intentionally quiet ivory material and the independent tactical-grid contact before FORMAL_CANDIDATE."},
        "unity_import": None,
        "ground_window_qa": {"qa_path": QA.relative_to(ROOT).as_posix(), "macro_pitch": 26, "window_bounds": qa["ground_window_bounds_on_logical_grid"], "selection": qa["ground_window_selection"]}
    }
    path = manifests / f"{STEM}.occ-art-manifest-v1.json"
    path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"contact": contact.relative_to(ROOT).as_posix(), "manifest": path.relative_to(ROOT).as_posix()}))


if __name__ == "__main__":
    main()
