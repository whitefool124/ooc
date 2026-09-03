#!/usr/bin/env python3
"""Normalize direct-generated OCC semantic icons into reviewable 32x32 candidates."""

from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Worldbuilding/05_美术与音频/概念参考/M-A9_simple_v04"
OUT = SOURCE / "normalized32"

PALETTES = {
    "action": (
        (10, 16, 18, 255),       # outline
        (232, 226, 202, 255),    # command token
        (14, 181, 201, 255),     # execute chevron
    ),
    "aether": (
        (5, 18, 43, 255),        # outline
        (12, 198, 203, 255),     # aether body
        (235, 233, 205, 255),    # core
    ),
    "notice": (
        (10, 15, 17, 255),       # outline
        (242, 176, 28, 255),     # attention mark
    ),
}


def distance_sq(rgb: tuple[int, int, int], color: tuple[int, int, int, int]) -> int:
    return sum((rgb[index] - color[index]) ** 2 for index in range(3))


def normalized_icon(name: str) -> Image.Image:
    source = Image.open(SOURCE / f"{name}.png").convert("RGBA")
    alpha = source.getchannel("A")
    hard_mask = alpha.point(lambda value: 255 if value >= 128 else 0)
    bounds = hard_mask.getbbox()
    if bounds is None:
        raise RuntimeError(f"{name}: no opaque subject")

    cropped = source.crop(bounds)
    width, height = cropped.size
    scale = min(24 / width, 24 / height)
    target_size = (max(1, round(width * scale)), max(1, round(height * scale)))
    reduced = cropped.resize(target_size, Image.Resampling.BOX)

    result = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    offset_x = (32 - target_size[0]) // 2
    offset_y = (32 - target_size[1]) // 2
    palette = PALETTES[name]
    for y in range(target_size[1]):
        for x in range(target_size[0]):
            pixel = reduced.getpixel((x, y))
            if pixel[3] < 128:
                continue
            result.putpixel(
                (offset_x + x, offset_y + y),
                min(palette, key=lambda color: distance_sq(pixel[:3], color)),
            )
    return result


def record(name: str, path: Path) -> dict:
    image = Image.open(path).convert("RGBA")
    pixels = list(image.get_flattened_data())
    opaque = [(x, y) for y in range(32) for x in range(32) if image.getpixel((x, y))[3] == 255]
    bounds = [min(x for x, _ in opaque), min(y for _, y in opaque),
              max(x for x, _ in opaque), max(y for _, y in opaque)]
    return {
        "id": name,
        "path": str(path.relative_to(ROOT)).replace("\\", "/"),
        "size": list(image.size),
        "colorsIncludingTransparency": len(set(pixels)),
        "hardAlpha": all(pixel[3] in (0, 255) for pixel in pixels),
        "boundsInclusive": bounds,
        "opaqueCoverage": round(len(opaque) / 1024, 3),
        "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
    }


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    names = ("action", "aether", "notice")
    for name in names:
        normalized_icon(name).save(OUT / f"{name}_32.png", optimize=True)

    records = [record(name, OUT / f"{name}_32.png") for name in names]
    status = "PASS" if all(
        item["size"] == [32, 32]
        and item["hardAlpha"]
        and item["colorsIncludingTransparency"] <= 4
        for item in records
    ) else "FAIL"

    sheet = Image.new("RGBA", (432, 184), (14, 18, 23, 255))
    draw = ImageDraw.Draw(sheet)
    for y in range(sheet.height):
        for x in range(sheet.width):
            if ((x // 8) + (y // 8)) % 2 == 0:
                sheet.putpixel((x, y), (22, 27, 33, 255))
    for index, name in enumerate(names):
        icon = Image.open(OUT / f"{name}_32.png").convert("RGBA")
        x = 8 + index * 144
        sheet.alpha_composite(icon.resize((128, 128), Image.Resampling.NEAREST), (x, 8))
        sheet.alpha_composite(icon, (x + 48, 140))
        draw.text((x + 64, 174), name.upper(), fill=(232, 226, 202, 255), anchor="mm")
    sheet.save(OUT / "OCC_M-A9_simple_v04_QA.png", optimize=True)

    report = {
        "schema": "occ.ui.semantic-icons.normalization.v0.1",
        "status": status,
        "source": "direct image generation; simple approved semantic content",
        "operations": ["alpha crop", "fit inside 24x24", "area reduction", "semantic palette mapping", "hard alpha", "center in 32x32"],
        "records": records,
    }
    (OUT / "OCC_M-A9_simple_v04_report.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"normalized={len(records)} status={status}")


if __name__ == "__main__":
    main()
