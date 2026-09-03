#!/usr/bin/env python3
"""Build labeled and unlabelled M-A19 review contacts from normalized staging icons."""

from __future__ import annotations

import argparse
import json
import math
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A19/icon_regen_143_catalog.json"
OUT = ROOT / "UnityProject/Artifacts/IconRegen143/contacts"


def checker(size: tuple[int, int], block: int = 8) -> Image.Image:
    image = Image.new("RGBA", size)
    draw = ImageDraw.Draw(image)
    for y in range(0, size[1], block):
        for x in range(0, size[0], block):
            value = 58 if ((x // block) + (y // block)) % 2 else 88
            draw.rectangle((x, y, x + block - 1, y + block - 1), fill=(value, value, value, 255))
    return image


def build(group: str, assets: list[dict]) -> None:
    assets = [value for value in assets if value["group"] == group and (ROOT / value["staging_path"]).exists()]
    if not assets:
        return
    columns, cell, label = 8, 128, 24
    rows = math.ceil(len(assets) / columns)
    unlabeled = checker((columns * cell, rows * cell), 16)
    labeled = Image.new("RGBA", (columns * cell, rows * (cell + label)), (22, 25, 30, 255))
    draw = ImageDraw.Draw(labeled)
    for index, value in enumerate(assets):
        x, y = (index % columns) * cell, (index // columns) * cell
        icon = Image.open(ROOT / value["staging_path"]).convert("RGBA").resize((cell, cell), Image.Resampling.NEAREST)
        unlabeled.alpha_composite(icon, (x, y))
        ly = (index // columns) * (cell + label)
        tile = checker((cell, cell), 16)
        tile.alpha_composite(icon)
        labeled.alpha_composite(tile, (x, ly))
        draw.text((x + 4, ly + cell + 4), value["stem"], fill=(225, 230, 235, 255))
    OUT.mkdir(parents=True, exist_ok=True)
    unlabeled.save(OUT / f"{group}_unlabelled.png")
    labeled.save(OUT / f"{group}_labelled.png")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--group")
    args = parser.parse_args()
    assets = json.loads(CATALOG.read_text(encoding="utf-8"))["assets"]
    groups = [args.group] if args.group else sorted({value["group"] for value in assets})
    for group in groups:
        build(group, assets)
    print(json.dumps({"groups": groups}))


if __name__ == "__main__":
    main()
