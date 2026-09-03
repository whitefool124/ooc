"""Author the first independent 16x16 item readability test set.

These are native 16x16 source pixels, deliberately not resizes of the 32x32
material-item studies.  They remain art-direction samples until content QA.
"""
from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).parent
OUT = ROOT / "Items16"
QA = ROOT / "QA"
OUT.mkdir(exist_ok=True)
QA.mkdir(exist_ok=True)

K = "#1c1b22"  # shared near-black outline
PALETTES = {
    "medic_herb": {"K": K, "D": "#28533b", "G": "#55a95a", "L": "#b4d66a"},
    "cinder_pear": {"K": K, "D": "#842b38", "R": "#d94b43", "L": "#f78a4e"},
    "coin": {"K": K, "D": "#99611c", "G": "#eeb126", "L": "#ffe36a"},
    "wood_cup": {"K": K, "D": "#704126", "W": "#b86a32", "L": "#e4a75a"},
    "bread": {"K": K, "D": "#8a4a27", "B": "#d98a42", "L": "#f3c66d"},
}

# Each glyph keeps a single dominant silhouette, one material cue and at most
# one highlight cluster.  All rows are exactly 16 source pixels wide.
GLYPHS = {
    "medic_herb": [
        "................",
        "................",
        ".......K........",
        "......KDK.......",
        "....KGGGGK......",
        "...KGGGGGGK.....",
        "....KGGGGK......",
        "......KDK.......",
        ".....KLDK.......",
        "....KLLDK.......",
        ".....KLDK.......",
        "......KK........",
        "................",
        "................",
        "................",
        "................",
        "................",
    ],
    "cinder_pear": [
        "................",
        "................",
        ".......K........",
        "......KDK.......",
        ".....KRRK.......",
        "....KRRRLK......",
        "....KRRRLK......",
        "...KRRRRRK......",
        "...KRRRRRK......",
        "....KRRRRK......",
        ".....KRRK.......",
        "......KK........",
        "................",
        "................",
        "................",
        "................",
        "................",
    ],
    "coin": [
        "................",
        "................",
        "......KKKK......",
        ".....KGGGGK.....",
        "....KGGLGGGK....",
        "....KGGDGGGK....",
        "....KGGDGGGK....",
        "....KGGLGGGK....",
        ".....KGGGGK.....",
        "......KDDK......",
        ".......KK.......",
        "................",
        "................",
        "................",
        "................",
        "................",
    ],
    "wood_cup": [
        "................",
        "................",
        "....KKKKKK......",
        "...KDDDDDDK.....",
        "...KWWLWWDK.....",
        "...KWWLWWDKK....",
        "...KWWLWWDLK....",
        "...KWWLWWDKK....",
        "...KWWLWWDK.....",
        "....KWWWDK......",
        ".....KDDK.......",
        "......KK........",
        "................",
        "................",
        "................",
        "................",
    ],
    "bread": [
        "................",
        "................",
        "................",
        ".....KKKKK......",
        "...KKBBBBBKK....",
        "..KBBLBBBBBBK...",
        ".KBBBBDBBBBBBK..",
        ".KBBBDBBBDBBBK..",
        ".KBBBBDBBBBBBK..",
        "..KBBBBBBBBBK...",
        "...KDDDDDDK.....",
        "....KKKKKK......",
        "................",
        "................",
        "................",
        "................",
    ],
}

LABELS = {
    "medic_herb": "Medic herb",
    "cinder_pear": "Cinder pear",
    "coin": "Coin",
    "wood_cup": "Wood cup",
    "bread": "Bread",
}


def rasterize(name: str, rows: list[str]) -> Image.Image:
    if len(rows) != 16 or any(len(row) != 16 for row in rows):
        raise ValueError(f"{name}: glyph must be 16x16")
    image = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    px = image.load()
    palette = PALETTES[name]
    for y, row in enumerate(rows):
        for x, symbol in enumerate(row):
            if symbol != ".":
                color = palette[symbol]
                px[x, y] = (*tuple(bytes.fromhex(color[1:])), 255)
    return image


def main() -> None:
    records = []
    icons = []
    for name, rows in GLYPHS.items():
        icon = rasterize(name, rows)
        path = OUT / f"occ_{name}_16_v01.png"
        icon.save(path)
        visible = [p for p in icon.getdata() if p[3] == 255]
        colors = sorted({p[:3] for p in visible})
        records.append({"file": path.name, "size": list(icon.size), "opaque_colors": len(colors), "hard_alpha": True})
        icons.append((name, icon))

    # A 1x reading strip above an enlarged nearest-neighbour inspection sheet.
    scale = 14
    cell_w, cell_h = 230, 300
    board = Image.new("RGBA", (cell_w * len(icons), cell_h), "#17171d")
    draw = ImageDraw.Draw(board)
    font = ImageFont.load_default()
    for index, (name, icon) in enumerate(icons):
        x0 = index * cell_w
        draw.text((x0 + 12, 12), LABELS[name], fill="#e6e3db", font=font)
        draw.text((x0 + 12, 32), "native 16x16 / 4 colors", fill="#a9a7b1", font=font)
        # checker beneath the exact 1x icon makes alpha and margins visible.
        one_x, one_y = x0 + 107, 65
        for y in range(16):
            for x in range(16):
                shade = "#383842" if (x + y) % 2 else "#2d2d35"
                draw.point((one_x + x, one_y + y), fill=shade)
        board.alpha_composite(icon, (one_x, one_y))
        draw.text((x0 + 12, 88), "1x readability", fill="#a9a7b1", font=font)
        enlarged = icon.resize((16 * scale, 16 * scale), Image.Resampling.NEAREST)
        board.alpha_composite(enlarged, (x0 + (cell_w - 16 * scale) // 2, 110))

    overview = QA / "items16_v01_overview.png"
    board.save(overview)
    (QA / "items16_v01_report.json").write_text(json.dumps(records, ensure_ascii=False, indent=2), encoding="utf-8")
    print(overview)
    print(json.dumps(records, ensure_ascii=False))


if __name__ == "__main__":
    main()
