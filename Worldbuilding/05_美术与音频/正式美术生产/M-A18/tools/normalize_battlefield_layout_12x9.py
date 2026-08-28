from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageOps


def crop_four_three(image: Image.Image) -> Image.Image:
    width, height = image.size
    target = 4 / 3
    ratio = width / height
    if ratio > target:
        new_width = round(height * target)
        left = (width - new_width) // 2
        return image.crop((left, 0, left + new_width, height))
    new_height = round(width / target)
    top = (height - new_height) // 2
    return image.crop((0, top, width, top + new_height))


def normalize(source: Image.Image) -> Image.Image:
    # The source owns every shape. This pass only reframes to the fixed board ratio,
    # reduces resolution/palette, and performs the contract's nearest-neighbour 2x.
    logical = crop_four_three(source.convert("RGB")).resize((192, 144), Image.Resampling.BOX)
    indexed = logical.quantize(colors=12, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
    return indexed.convert("RGBA").resize((384, 288), Image.Resampling.NEAREST)


def checker_underlay(size: tuple[int, int]) -> Image.Image:
    image = Image.new("RGBA", size)
    pixels = []
    for y in range(size[1]):
        for x in range(size[0]):
            shade = 206 if (x // 8 + y // 8) % 2 == 0 else 150
            pixels.append((shade, shade, shade, 255))
    image.putdata(pixels)
    return image


def main() -> None:
    parser = argparse.ArgumentParser(description="Normalize one independently generated 12x9 battlefield layout source.")
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--qa-dir", required=True, type=Path)
    args = parser.parse_args()

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.qa_dir.mkdir(parents=True, exist_ok=True)
    field = normalize(Image.open(args.input))
    field.save(args.output, optimize=True)

    asset_id = args.output.stem
    field.save(args.qa_dir / f"{asset_id}_1x.png", optimize=True)
    field.resize((1536, 1152), Image.Resampling.NEAREST).save(
        args.qa_dir / f"{asset_id}_4x.png", optimize=True
    )
    ImageOps.grayscale(field).convert("RGBA").save(
        args.qa_dir / f"{asset_id}_grayscale.png", optimize=True
    )
    checker = checker_underlay(field.size)
    checker.alpha_composite(field)
    checker.save(args.qa_dir / f"{asset_id}_checker.png", optimize=True)

    pixels = list(field.getdata())
    report = {
        "schema": "occ-battlefield-layout-normalization-v1",
        "asset_id": asset_id,
        "size": list(field.size),
        "logical_cells": [12, 9],
        "logical_source_size": [192, 144],
        "visible_colors": len(set(pixels)),
        "alpha_values": sorted({pixel[3] for pixel in pixels}),
        "sha256": hashlib.sha256(args.output.read_bytes()).hexdigest(),
        "normalization_only": True,
        "script_added_art_structure": False,
        "operations": ["center_crop_4_3", "box_downsample_192x144", "median_cut_12_no_dither", "nearest_2x"],
    }
    (args.qa_dir / f"{asset_id}_report.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )


if __name__ == "__main__":
    main()
