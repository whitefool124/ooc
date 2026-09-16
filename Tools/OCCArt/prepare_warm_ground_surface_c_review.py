#!/usr/bin/env python3
"""Make non-destructive review evidence for the approved warm ground field.

The tactical grid is deliberately drawn only into the application-contact image.
The reusable 32px floor texture remains an unpainted material field.
"""

from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageOps


ROOT = Path(__file__).resolve().parents[2]
BATCH = ROOT / "ArtSource/academy_courtyard_warm_ground_batch"
SOURCE = BATCH / "sources/ground_academy_courtyard_warm_surface_c_source.png"
OUTPUT = BATCH / "outputs/ground_academy_courtyard_warm_surface_c_groundfield_32.png"
QA = BATCH / "qa/ground_academy_courtyard_warm_surface_c_groundfield_qa.json"
STEM = "ground_academy_courtyard_warm_surface_c"


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
    if qa["status"] != "PASS_GROUND_WINDOW":
        raise SystemExit(f"ground source is not review-ready: {qa['status']}")
    tile = Image.open(OUTPUT).convert("RGBA")
    if tile.size != (32, 32):
        raise SystemExit(f"expected 32x32 output, received {tile.size}")

    evidence = BATCH / "evidence"
    manifests = BATCH / "manifests"
    evidence.mkdir(parents=True, exist_ok=True)
    manifests.mkdir(parents=True, exist_ok=True)
    one_x = evidence / f"{STEM}_1x.png"
    four_x = evidence / f"{STEM}_4x.png"
    grayscale = evidence / f"{STEM}_grayscale.png"
    checker_path = evidence / f"{STEM}_checker.png"
    contact = evidence / f"{STEM}_tactical_grid_12x9_contact.png"
    tile.save(one_x)
    tile.resize((128, 128), Image.Resampling.NEAREST).save(four_x)
    ImageOps.grayscale(tile).convert("RGBA").save(grayscale)
    board = checker((128, 128))
    board.alpha_composite(tile.resize((128, 128), Image.Resampling.NEAREST))
    board.save(checker_path)

    # 2 screen pixels equals one native pixel at this 2x review scale.
    cell, columns, rows = 64, 12, 9
    canvas = Image.new("RGBA", (columns * cell, rows * cell), (42, 31, 27, 255))
    scaled = tile.resize((cell, cell), Image.Resampling.NEAREST)
    for y in range(rows):
        for x in range(columns):
            canvas.alpha_composite(scaled, (x * cell, y * cell))
    grid = ImageDraw.Draw(canvas)
    line = (75, 54, 45, 255)  # deep warm umber, never baked into the floor asset
    for x in range(columns + 1):
        grid.line((x * cell, 0, x * cell, rows * cell - 1), fill=line, width=2)
    for y in range(rows + 1):
        grid.line((0, y * cell, columns * cell - 1, y * cell), fill=line, width=2)
    canvas.save(contact)

    manifest = {
        "schema": "occ-art-manifest-v1",
        "contract_version": 1,
        "asset_id": "ground.academy_courtyard_warm.surface.c",
        "role": "floor_tile_32",
        "status": "REVIEW_READY",
        "provenance": {
            "source_channel": "codex_builtin_imagegen",
            "source_descriptor": "Independent opaque warm academy stone-material source; 39px observed macro-pixel pitch decoded as a lossless 32x32 ground-field window, with no resize, interpolation, repaint or new colours.",
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
            "runtime_draw_rect": "one 32x32 logical battle cell; dark 1-native-pixel cell boundary is a separate tactical-grid overlay, not a floor-brick seam",
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
            "notes": "Review the warm stone material together with its separate dark tactical-grid overlay. Approval is required before FORMAL_CANDIDATE or Unity import.",
        },
        "unity_import": None,
        "ground_window_qa": {
            "qa_path": QA.relative_to(ROOT).as_posix(),
            "macro_pitch": qa["inferred_subject_pixel_pitch"],
            "window_bounds": qa["ground_window_bounds_on_logical_grid"],
            "selection": qa["ground_window_selection"],
        },
    }
    path = manifests / f"{STEM}.occ-art-manifest-v1.json"
    path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"manifest": path.relative_to(ROOT).as_posix(), "contact": contact.relative_to(ROOT).as_posix()}, ensure_ascii=False))


if __name__ == "__main__":
    main()
