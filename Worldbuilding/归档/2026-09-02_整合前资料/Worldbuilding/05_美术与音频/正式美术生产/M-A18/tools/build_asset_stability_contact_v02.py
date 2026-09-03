"""Build the application-contact sheet for the staff and two-cell bed test.

Only existing normalized assets and current OCC runtime backgrounds are
composited at integer scales. No asset structure is drawn or repaired here.
"""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


def arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--staff", required=True, type=Path)
    parser.add_argument("--bed", required=True, type=Path)
    parser.add_argument("--tiles", required=True, type=Path)
    parser.add_argument("--slot", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    return parser.parse_args()


def load(path: Path, expected: tuple[int, int]) -> Image.Image:
    image = Image.open(path).convert("RGBA")
    if image.size != expected:
        raise ValueError(f"{path} is {image.size}, expected {expected}")
    return image


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


def main() -> None:
    args = arguments()
    staff = load(args.staff, (32, 32))
    bed = load(args.bed, (64, 32))
    slot = load(args.slot, (32, 32))
    tiles = [load(args.tiles / f"academy_courtyard_{suffix}.png", (32, 32)) for suffix in "abcd"]

    map_patch = Image.new("RGBA", (128, 96))
    for row in range(3):
        for column in range(4):
            map_patch.alpha_composite(tiles[(row * 4 + column) % len(tiles)], (column * 32, row * 32))
    map_patch.alpha_composite(bed, (32, 32))

    slot_with_staff = slot.copy()
    slot_with_staff.alpha_composite(staff)

    sheet = checker((900, 470))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    draw.text((24, 18), "OCC STABILITY V02 / APPLICATION CONTACT", fill=(236, 224, 197, 255), font=font)
    draw.text((24, 44), "BED / 64x32 / two logical cells / runtime 4x", fill=(200, 188, 164, 255), font=font)
    sheet.alpha_composite(nearest(map_patch, 4), (24, 68))

    draw.text((568, 44), "STAFF / native 32px", fill=(200, 188, 164, 255), font=font)
    sheet.alpha_composite(nearest(staff, 4), (568, 68))
    draw.text((716, 44), "IN EQUIPMENT SLOT / 4x", fill=(200, 188, 164, 255), font=font)
    sheet.alpha_composite(nearest(slot_with_staff, 4), (716, 68))

    draw.text((568, 222), "1x truth", fill=(200, 188, 164, 255), font=font)
    sheet.alpha_composite(bed, (568, 246))
    sheet.alpha_composite(staff, (648, 246))
    draw.text((568, 302), "2x low-resolution runtime", fill=(200, 188, 164, 255), font=font)
    sheet.alpha_composite(nearest(bed, 2), (568, 326))
    sheet.alpha_composite(nearest(slot_with_staff, 2), (712, 326))

    args.output.parent.mkdir(parents=True, exist_ok=True)
    sheet.convert("RGB").save(args.output, optimize=True)


if __name__ == "__main__":
    main()
