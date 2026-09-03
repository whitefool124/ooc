"""Build an application-context QA sheet for the M-A18 asset stability test.

This script only composites already-normalized review assets over existing OCC
runtime backgrounds at exact integer scales. It does not draw or repair art.
"""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


def arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--table", required=True, type=Path)
    parser.add_argument("--bottle", required=True, type=Path)
    parser.add_argument("--tiles", required=True, type=Path)
    parser.add_argument("--slot", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    return parser.parse_args()


def nearest(image: Image.Image, scale: int) -> Image.Image:
    return image.resize((image.width * scale, image.height * scale), Image.Resampling.NEAREST)


def checker(size: tuple[int, int], block: int = 12) -> Image.Image:
    result = Image.new("RGBA", size)
    draw = ImageDraw.Draw(result)
    colors = ((31, 28, 25, 255), (41, 37, 32, 255))
    for y in range(0, size[1], block):
        for x in range(0, size[0], block):
            draw.rectangle(
                (x, y, min(x + block - 1, size[0] - 1), min(y + block - 1, size[1] - 1)),
                fill=colors[((x // block) + (y // block)) % 2],
            )
    return result


def load_rgba(path: Path, expected: tuple[int, int]) -> Image.Image:
    image = Image.open(path).convert("RGBA")
    if image.size != expected:
        raise ValueError(f"{path} is {image.size}, expected {expected}")
    return image


def main() -> None:
    args = arguments()
    table = load_rgba(args.table, (32, 32))
    bottle = load_rgba(args.bottle, (32, 32))
    slot = load_rgba(args.slot, (32, 32))

    tile_paths = [args.tiles / f"academy_courtyard_{suffix}.png" for suffix in "abcd"]
    tiles = [load_rgba(path, (32, 32)) for path in tile_paths]

    map_patch = Image.new("RGBA", (96, 96))
    for row in range(3):
        for column in range(3):
            map_patch.alpha_composite(tiles[(row * 3 + column) % len(tiles)], (column * 32, row * 32))
    map_patch.alpha_composite(table, (32, 32))

    slot_with_bottle = slot.copy()
    slot_with_bottle.alpha_composite(bottle)

    sheet = checker((760, 460))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    draw.text((24, 18), "OCC ASSET STABILITY / APPLICATION CONTACT", fill=(236, 224, 197, 255), font=font)
    draw.text((24, 44), "TABLE / 32px cell / runtime 4x", fill=(200, 188, 164, 255), font=font)
    sheet.alpha_composite(nearest(map_patch, 4), (24, 68))

    draw.text((438, 44), "BOTTLE / native 24px", fill=(200, 188, 164, 255), font=font)
    sheet.alpha_composite(nearest(bottle, 4), (438, 68))
    draw.text((586, 44), "IN UI SLOT / 4x", fill=(200, 188, 164, 255), font=font)
    sheet.alpha_composite(nearest(slot_with_bottle, 4), (586, 68))

    draw.text((438, 222), "1x truth", fill=(200, 188, 164, 255), font=font)
    sheet.alpha_composite(table, (438, 246))
    sheet.alpha_composite(bottle, (486, 246))
    draw.text((438, 302), "2x low-resolution runtime", fill=(200, 188, 164, 255), font=font)
    sheet.alpha_composite(nearest(table, 2), (438, 326))
    sheet.alpha_composite(nearest(slot_with_bottle, 2), (526, 326))

    args.output.parent.mkdir(parents=True, exist_ok=True)
    sheet.convert("RGB").save(args.output, optimize=True)


if __name__ == "__main__":
    main()
