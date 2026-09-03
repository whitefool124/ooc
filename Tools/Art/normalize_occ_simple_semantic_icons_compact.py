#!/usr/bin/env python3
"""Produce 16x16 and 12x12 comparisons from direct-generated semantic icons."""

from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Worldbuilding/05_美术与音频/概念参考/M-A9_simple_v04"
OUT = SOURCE / "compact"
PALETTES = {
    "action": ((10, 16, 18, 255), (232, 226, 202, 255), (14, 181, 201, 255)),
    "aether": ((5, 18, 43, 255), (12, 198, 203, 255), (235, 233, 205, 255)),
    "notice": ((10, 15, 17, 255), (242, 176, 28, 255)),
}


def distance_sq(rgb: tuple[int, int, int], color: tuple[int, int, int, int]) -> int:
    return sum((rgb[index] - color[index]) ** 2 for index in range(3))


def normalize(name: str, canvas: int, subject_limit: int) -> Image.Image:
    source = Image.open(SOURCE / f"{name}.png").convert("RGBA")
    mask = source.getchannel("A").point(lambda value: 255 if value >= 128 else 0)
    bounds = mask.getbbox()
    if bounds is None:
        raise RuntimeError(f"{name}: no opaque pixels")
    cropped = source.crop(bounds)
    width, height = cropped.size
    scale = min(subject_limit / width, subject_limit / height)
    size = (max(1, round(width * scale)), max(1, round(height * scale)))
    reduced = cropped.resize(size, Image.Resampling.BOX)
    result = Image.new("RGBA", (canvas, canvas), (0, 0, 0, 0))
    origin = ((canvas - size[0]) // 2, (canvas - size[1]) // 2)
    palette = PALETTES[name]
    for y in range(size[1]):
        for x in range(size[0]):
            pixel = reduced.getpixel((x, y))
            if pixel[3] < 128:
                continue
            result.putpixel(
                (origin[0] + x, origin[1] + y),
                min(palette, key=lambda color: distance_sq(pixel[:3], color)),
            )
    return result


def record(name: str, canvas: int, path: Path) -> dict:
    image = Image.open(path).convert("RGBA")
    pixels = list(image.get_flattened_data())
    opaque = [(x, y) for y in range(canvas) for x in range(canvas) if image.getpixel((x, y))[3] == 255]
    return {
        "id": name,
        "canvas": canvas,
        "colorsIncludingTransparency": len(set(pixels)),
        "hardAlpha": all(pixel[3] in (0, 255) for pixel in pixels),
        "opaquePixels": len(opaque),
        "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
    }


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    names = ("action", "aether", "notice")
    variants = ((16, 12), (12, 9))
    records = []
    for canvas, subject_limit in variants:
        for name in names:
            path = OUT / f"{name}_{canvas}.png"
            normalize(name, canvas, subject_limit).save(path, optimize=True)
            records.append(record(name, canvas, path))

    # Large comparison: 16px at 6x and 12px at 8x both occupy 96px.
    sheet = Image.new("RGBA", (384, 260), (14, 18, 23, 255))
    draw = ImageDraw.Draw(sheet)
    for y in range(sheet.height):
        for x in range(sheet.width):
            if ((x // 8) + (y // 8)) % 2 == 0:
                sheet.putpixel((x, y), (22, 27, 33, 255))
    for row, (canvas, scale) in enumerate(((16, 6), (12, 8))):
        y = 24 + row * 120
        draw.text((8, y + 42), f"{canvas}px", fill=(232, 226, 202, 255))
        for index, name in enumerate(names):
            icon = Image.open(OUT / f"{name}_{canvas}.png").convert("RGBA")
            x = 72 + index * 104
            sheet.alpha_composite(icon.resize((canvas * scale, canvas * scale), Image.Resampling.NEAREST), (x, y))
            sheet.alpha_composite(icon, (x + 40, y + 98))
    sheet.save(OUT / "OCC_M-A9_compact_16_12_QA.png", optimize=True)

    status = "PASS" if all(item["hardAlpha"] and item["colorsIncludingTransparency"] <= 4 for item in records) else "FAIL"
    report = {
        "schema": "occ.ui.semantic-icons.compact-comparison.v0.1",
        "status": status,
        "recommendedSourceSize": 16,
        "displayContract": "16px source; 2x at 1920x1080, 1x at 960x540",
        "records": records,
    }
    (OUT / "OCC_M-A9_compact_16_12_report.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"variants={len(records)} status={status}")


if __name__ == "__main__":
    main()
