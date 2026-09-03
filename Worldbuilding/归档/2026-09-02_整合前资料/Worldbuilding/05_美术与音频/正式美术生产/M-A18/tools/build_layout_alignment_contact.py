from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


def main() -> None:
    parser = argparse.ArgumentParser(description="Build a QA-only 12x9 coordinate overlay for a 384x288 battlefield layout.")
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()

    image = Image.open(args.input).convert("RGBA").resize((1536, 1152), Image.Resampling.NEAREST)
    overlay = Image.new("RGBA", image.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay)
    cell = 128
    for x in range(13):
        draw.line((x * cell, 0, x * cell, image.height), fill=(0, 220, 235, 190), width=3)
    for y in range(10):
        draw.line((0, y * cell, image.width, y * cell), fill=(0, 220, 235, 190), width=3)
    font = ImageFont.load_default(size=22)
    for y in range(9):
        for x in range(12):
            draw.rectangle((x * cell + 5, y * cell + 5, x * cell + 72, y * cell + 34), fill=(22, 19, 23, 210))
            draw.text((x * cell + 10, y * cell + 8), f"{x},{y}", fill=(244, 239, 218, 255), font=font)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    Image.alpha_composite(image, overlay).save(args.output, optimize=True)


if __name__ == "__main__":
    main()
