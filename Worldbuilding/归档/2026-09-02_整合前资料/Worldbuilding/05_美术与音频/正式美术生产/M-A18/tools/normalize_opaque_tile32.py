from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image


STONE_RAMP = [
    (105, 102, 92),
    (125, 120, 108),
    (143, 137, 122),
    (158, 151, 134),
    (174, 166, 146),
]
REPAIR = (148, 129, 102)


def lock_stone_palette(indexed: Image.Image) -> Image.Image:
    palette = indexed.getpalette()
    counts = indexed.getcolors() or []
    active = [index for _, index in counts]
    colours = {index: tuple(palette[index * 3:index * 3 + 3]) for index in active}
    repair_index = max(active, key=lambda index: colours[index][0] - colours[index][2])
    stone_indices = sorted(
        [index for index in active if index != repair_index],
        key=lambda index: 0.2126 * colours[index][0] + 0.7152 * colours[index][1] + 0.0722 * colours[index][2],
    )
    mapping = {repair_index: REPAIR}
    for index, colour in zip(stone_indices, STONE_RAMP):
        mapping[index] = colour
    rgba = Image.new("RGBA", indexed.size)
    rgba.putdata([mapping[index] + (255,) for index in indexed.getdata()])
    return rgba


def remove_isolated_pixels(image: Image.Image, passes: int = 2) -> Image.Image:
    current = image.copy()
    for _ in range(passes):
        source = current.load()
        cleaned = current.copy()
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
                same = sum(pixel == source[x, y] for pixel in neighbours)
                if same <= 1:
                    target[x, y] = max(set(neighbours), key=neighbours.count)
        current = cleaned
    return current


def main() -> None:
    parser = argparse.ArgumentParser(description="Pure technical normalization for one opaque 32x32 terrain tile.")
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--qa-dir", required=True, type=Path)
    parser.add_argument("--logical-size", type=int, choices=(16, 32), default=32,
                        help="Normalize at this logical pixel density before nearest-neighbour 32px delivery.")
    args = parser.parse_args()

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.qa_dir.mkdir(parents=True, exist_ok=True)
    source = Image.open(args.input).convert("RGB")
    reduced = source.resize((args.logical_size, args.logical_size), Image.Resampling.NEAREST)
    indexed = reduced.quantize(colors=6, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
    logical = remove_isolated_pixels(lock_stone_palette(indexed))
    tile = logical.resize((32, 32), Image.Resampling.NEAREST)
    tile.save(args.output)

    tile.resize((128, 128), Image.Resampling.NEAREST).save(args.qa_dir / f"{args.output.stem}_4x.png")
    repeated = Image.new("RGBA", (8 * 32, 6 * 32))
    for y in range(6):
        for x in range(8):
            repeated.paste(tile, (x * 32, y * 32))
    repeated.resize((512, 384), Image.Resampling.NEAREST).save(args.qa_dir / f"{args.output.stem}_repeat_2x.png")

    rgba = list(tile.getdata())
    report = {
        "size": list(tile.size),
        "mode": tile.mode,
        "alpha_values": sorted({pixel[3] for pixel in rgba}),
        "colour_count": len(set(rgba)),
        "palette": [list(pixel[:3]) for pixel in sorted(set(rgba))],
        "sha256": hashlib.sha256(args.output.read_bytes()).hexdigest(),
        "normalization_only": True,
        "script_added_art_structure": False,
        "logical_size": args.logical_size,
        "nearest_delivery_scale": 32 // args.logical_size,
        "isolated_pixel_cleanup_passes": 2,
    }
    (args.qa_dir / f"{args.output.stem}_report.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8"
    )


if __name__ == "__main__":
    main()
