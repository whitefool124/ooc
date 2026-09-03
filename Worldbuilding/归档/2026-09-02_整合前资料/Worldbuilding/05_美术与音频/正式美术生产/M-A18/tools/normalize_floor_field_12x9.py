from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageOps


PALETTES = {
    "courtyard": [
        (105, 103, 94), (121, 118, 106), (137, 132, 117), (151, 145, 128),
        (165, 158, 139), (177, 169, 148), (142, 132, 111), (124, 126, 113),
    ],
    "stone_road": [
        (73, 75, 72), (86, 87, 82), (99, 99, 92), (112, 110, 101),
        (125, 121, 110), (138, 132, 119), (106, 96, 82), (91, 94, 91),
    ],
    "ruins": [
        (93, 87, 69), (109, 101, 79), (125, 116, 91), (142, 132, 104),
        (157, 147, 118), (171, 161, 132), (116, 112, 75), (132, 120, 87),
    ],
    "packed_earth": [
        (105, 78, 55), (121, 90, 62), (137, 103, 70), (151, 115, 79),
        (166, 130, 91), (181, 145, 104), (124, 116, 101), (148, 133, 108),
    ],
}


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


def clean_isolated(image: Image.Image) -> Image.Image:
    source = image.load()
    cleaned = image.copy()
    target = cleaned.load()
    for y in range(image.height):
        for x in range(image.width):
            neighbours = []
            for oy in (-1, 0, 1):
                for ox in (-1, 0, 1):
                    if ox == 0 and oy == 0:
                        continue
                    nx, ny = x + ox, y + oy
                    if 0 <= nx < image.width and 0 <= ny < image.height:
                        neighbours.append(source[nx, ny])
            if neighbours.count(source[x, y]) <= 1:
                target[x, y] = max(set(neighbours), key=neighbours.count)
    return cleaned


def normalize(source: Image.Image, theme: str) -> Image.Image:
    logical = crop_four_three(source.convert("RGB")).resize((192, 144), Image.Resampling.BOX)
    indexed = logical.quantize(colors=8, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
    raw_palette = indexed.getpalette()
    used = [index for _, index in indexed.getcolors() or []]
    colours = {index: tuple(raw_palette[index * 3:index * 3 + 3]) for index in used}
    source_order = sorted(used, key=lambda index: sum(colours[index]))
    target_order = sorted(PALETTES[theme], key=sum)
    mapping = {index: target_order[min(rank, len(target_order) - 1)] for rank, index in enumerate(source_order)}
    rgba = Image.new("RGBA", indexed.size)
    rgba.putdata([mapping[index] + (255,) for index in indexed.get_flattened_data()])
    return clean_isolated(rgba).resize((384, 288), Image.Resampling.NEAREST)


def main() -> None:
    parser = argparse.ArgumentParser(description="Normalize one independent 12x9 OCC floor-field source.")
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--qa-dir", required=True, type=Path)
    parser.add_argument("--theme", required=True, choices=tuple(PALETTES))
    args = parser.parse_args()

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.qa_dir.mkdir(parents=True, exist_ok=True)
    field = normalize(Image.open(args.input), args.theme)
    field.save(args.output)
    field.save(args.qa_dir / f"{args.output.stem}_1x.png")
    field.resize((1536, 1152), Image.Resampling.NEAREST).save(args.qa_dir / f"{args.output.stem}_4x.png")
    ImageOps.grayscale(field).convert("RGBA").save(args.qa_dir / f"{args.output.stem}_grayscale.png")
    field.save(args.qa_dir / f"{args.output.stem}_checker.png")

    pixels = list(field.get_flattened_data())
    report = {
        "size": list(field.size),
        "logical_cells": [12, 9],
        "logical_source_size": [192, 144],
        "hard_alpha": sorted({pixel[3] for pixel in pixels}) == [255],
        "visible_colors": len(set(pixels)),
        "palette": [list(pixel[:3]) for pixel in sorted(set(pixels))],
        "sha256": hashlib.sha256(args.output.read_bytes()).hexdigest(),
        "normalization_only": True,
        "script_added_art_structure": False,
        "isolated_pixel_cleanup_passes": 1,
    }
    (args.qa_dir / f"{args.output.stem}_report.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8"
    )


if __name__ == "__main__":
    main()
