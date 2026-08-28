#!/usr/bin/env python3
from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A26/element_resources_24_catalog.json"
OUT = ROOT / "UnityProject/Artifacts/ElementResources24/contacts"


def checker(size: tuple[int, int], block: int = 8) -> Image.Image:
    result = Image.new("RGB", size)
    pixels = result.load()
    for y in range(size[1]):
        for x in range(size[0]):
            value = 54 if (x // block + y // block) % 2 == 0 else 82
            pixels[x, y] = (value, value, value)
    return result


def main() -> None:
    assets = json.loads(CATALOG.read_text(encoding="utf-8"))["assets"]
    cell_w, cell_h, cols = 220, 194, 4
    rows = (len(assets) + cols - 1) // cols
    canvas = Image.new("RGB", (cell_w * cols, 56 + cell_h * rows), (25, 23, 20))
    draw = ImageDraw.Draw(canvas)
    draw.text((16, 18), "M-A26 ELEMENT + RESOURCE ICONS — OLD / NEW 4x + NEW 1x", fill=(242, 235, 221))
    for index, asset in enumerate(assets):
        x = (index % cols) * cell_w
        y = 56 + (index // cols) * cell_h
        draw.rectangle((x + 4, y + 4, x + cell_w - 5, y + cell_h - 5), outline=(91, 84, 73))
        final = ROOT / asset["final_path"]
        staged = ROOT / asset["staging_path"]
        old = Image.open(final).convert("RGBA")
        new = Image.open(staged).convert("RGBA")
        old_bg = checker((96, 96)); old_bg.paste(old.resize((96, 96), Image.Resampling.NEAREST), (0, 0), old.resize((96, 96), Image.Resampling.NEAREST))
        new_bg = checker((96, 96)); new_bg.paste(new.resize((96, 96), Image.Resampling.NEAREST), (0, 0), new.resize((96, 96), Image.Resampling.NEAREST))
        canvas.paste(old_bg, (x + 10, y + 12)); canvas.paste(new_bg, (x + 114, y + 12))
        native = checker((32, 32), 4); native.paste(new, (0, 0), new)
        canvas.paste(native, (x + 10, y + 116))
        draw.text((x + 50, y + 120), asset["stem"], fill=(229, 222, 207))
        draw.text((x + 10, y + 154), "OLD", fill=(160, 151, 137)); draw.text((x + 114, y + 154), "NEW", fill=(118, 190, 161))
    OUT.mkdir(parents=True, exist_ok=True)
    path = OUT / "element_resources_24_old_new_review.png"
    canvas.save(path, optimize=True)
    print(path)


if __name__ == "__main__":
    main()
