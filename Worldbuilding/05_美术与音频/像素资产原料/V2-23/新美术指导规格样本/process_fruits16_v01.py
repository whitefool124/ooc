"""Normalize the approved fruit direction into separate, exact 16x16 review PNGs.

The source sheet is only a colour/composition reference. This script makes no
claim that the source itself is 16px artwork: each emitted item is a standalone
16x16 RGBA file and the QA board shows both its literal 1x reading and a
nearest-neighbour enlargement.
"""
from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

SOURCE = Path(r"C:\Users\FNHF\.codex\generated_images\019ff714-5e0a-7ed0-8cc2-b42cf7aa433c\exec-5a68f502-d2ae-4e48-a53f-6e5fc3452ed3.png")
ROOT = Path(__file__).parent
OUT = ROOT / "Items16" / "Fruits_v01"
QA = ROOT / "QA"
OUT.mkdir(parents=True, exist_ok=True)
QA.mkdir(exist_ok=True)

# Regions are deliberately individual source concepts, then normalized one by one.
REGIONS = {
    "apple": (45, 265, 365, 805),
    "citrus": (390, 305, 700, 800),
    "plum": (705, 255, 1035, 805),
    "pear": (1035, 185, 1370, 805),
    "blueberries": (1370, 250, 1690, 800),
    "strawberry": (1710, 245, 2010, 800),
}


def alpha_white(im: Image.Image) -> Image.Image:
    """Remove neutral white sheet background, preserving coloured highlights."""
    rgba = im.convert("RGBA")
    px = rgba.load()
    for y in range(rgba.height):
        for x in range(rgba.width):
            r, g, b, a = px[x, y]
            if min(r, g, b) > 242 and max(r, g, b) - min(r, g, b) < 12:
                px[x, y] = (0, 0, 0, 0)
    return rgba


def make_16(region: tuple[int, int, int, int], source: Image.Image) -> Image.Image:
    raw = alpha_white(source.crop(region))
    bbox = raw.getbbox()
    if bbox is None:
        raise ValueError("empty source region")
    raw = raw.crop(bbox)
    # 14px max content yields the mandatory one-pixel transparent safety border.
    raw.thumbnail((14, 14), Image.Resampling.BOX)
    # Discrete clusters: quantize colour while retaining the strict cut alpha.
    rgb = raw.convert("RGB").quantize(colors=8, method=Image.Quantize.MEDIANCUT).convert("RGBA")
    rgba = Image.new("RGBA", raw.size, (0, 0, 0, 0))
    rgba.paste(rgb, mask=raw.getchannel("A"))
    result = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    result.alpha_composite(rgba, ((16 - rgba.width) // 2, (16 - rgba.height) // 2))
    return result


def main() -> None:
    source = Image.open(SOURCE)
    records, items = [], []
    for name, region in REGIONS.items():
        icon = make_16(region, source)
        path = OUT / f"occ_fruit_{name}_16_v01.png"
        icon.save(path)
        opaque = [p for p in icon.getdata() if p[3] == 255]
        records.append({"file": path.name, "size": [16, 16], "opaque_colors": len(set(p[:3] for p in opaque)), "hard_alpha": True})
        items.append((name, icon))

    scale, col_w, board_h = 16, 190, 330
    board = Image.new("RGBA", (col_w * len(items), board_h), "#16161c")
    draw = ImageDraw.Draw(board)
    font = ImageFont.load_default()
    for index, (name, icon) in enumerate(items):
        x0 = index * col_w
        draw.text((x0 + 10, 10), name, fill="#e9e5dc", font=font)
        draw.text((x0 + 10, 28), "native 16x16", fill="#aaa7b1", font=font)
        # literal 1x on checkerboard
        ox, oy = x0 + 87, 55
        for y in range(16):
            for x in range(16):
                draw.point((ox + x, oy + y), fill="#383842" if (x + y) % 2 else "#2b2b34")
        board.alpha_composite(icon, (ox, oy))
        board.alpha_composite(icon.resize((16 * scale, 16 * scale), Image.Resampling.NEAREST), (x0 - 33, 85))

    board.save(QA / "fruits16_v01_overview.png")
    (QA / "fruits16_v01_report.json").write_text(json.dumps(records, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
