#!/usr/bin/env python3
"""Author and QA OCC's small, non-character navigation icon set."""

from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "UnityProject/Assets/Game/Resources/Art/FormalNavigationIcons32"
QA = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A8"
P = {
    "clear": (0, 0, 0, 0), "ink": (8, 10, 12, 255), "steel": (64, 69, 74, 255),
    "light": (215, 216, 210, 255), "white": (240, 240, 232, 255),
    "cyan": (45, 221, 254, 255), "amber": (243, 183, 34, 255),
    "safe": (142, 176, 139, 255), "danger": (216, 91, 73, 255),
}


def canvas():
    image = Image.new("RGBA", (32, 32), P["clear"])
    return image, ImageDraw.Draw(image)


def framed(draw):
    draw.rectangle((3, 3, 28, 28), outline=P["steel"])
    for x, y in ((3, 3), (28, 3), (3, 28), (28, 28)):
        draw.point((x, y), fill=P["light"])


def arrow(draw, direction, color="cyan"):
    if direction == "right":
        draw.line((7, 16, 23, 16), fill=P["ink"], width=5); draw.line((7, 16, 23, 16), fill=P[color], width=2)
        draw.polygon(((19, 9), (27, 16), (19, 23)), fill=P[color])
    else:
        draw.line((9, 16, 25, 16), fill=P["ink"], width=5); draw.line((9, 16, 25, 16), fill=P[color], width=2)
        draw.polygon(((13, 9), (5, 16), (13, 23)), fill=P[color])


def icon(name):
    image, draw = canvas(); framed(draw)
    if name == "home":
        draw.rectangle((8, 10, 24, 25), outline=P["cyan"], width=2)
        draw.rectangle((12, 15, 20, 25), fill=P["ink"], outline=P["light"])
        draw.rectangle((15, 18, 17, 21), fill=P["amber"])
        draw.line((6, 10, 16, 6, 26, 10), fill=P["light"], width=2)
    elif name == "continue": arrow(draw, "right")
    elif name == "back": arrow(draw, "left", "amber")
    elif name == "archive":
        draw.rectangle((8, 8, 24, 24), fill=P["ink"], outline=P["amber"], width=2)
        draw.rectangle((11, 5, 21, 9), fill=P["steel"], outline=P["light"])
        for y in (13, 17, 21): draw.line((11, y, 21, y), fill=P["light"], width=1)
    elif name == "settings":
        draw.ellipse((8, 8, 24, 24), outline=P["cyan"], width=3); draw.ellipse((13, 13, 19, 19), fill=P["ink"], outline=P["white"])
        for box in ((14, 4, 18, 9), (14, 23, 18, 28), (4, 14, 9, 18), (23, 14, 28, 18)): draw.rectangle(box, fill=P["steel"], outline=P["cyan"])
    elif name == "confirm":
        draw.line((7, 16, 13, 22, 25, 9), fill=P["ink"], width=6); draw.line((7, 16, 13, 22, 25, 9), fill=P["safe"], width=3)
    elif name == "save":
        draw.rectangle((7, 6, 25, 26), fill=P["ink"], outline=P["cyan"], width=2)
        draw.rectangle((11, 7, 21, 13), fill=P["light"]); draw.rectangle((10, 17, 22, 24), outline=P["amber"], width=2)
        draw.rectangle((18, 8, 21, 12), fill=P["steel"])
    else:
        draw.line((9, 9, 23, 23), fill=P["ink"], width=6); draw.line((23, 9, 9, 23), fill=P["ink"], width=6)
        draw.line((9, 9, 23, 23), fill=P["danger"], width=3); draw.line((23, 9, 9, 23), fill=P["danger"], width=3)
    return image


def main():
    names = ("home", "continue", "archive", "settings", "back", "confirm", "save", "close")
    OUT.mkdir(parents=True, exist_ok=True); QA.mkdir(parents=True, exist_ok=True)
    records = []
    sheet = Image.new("RGBA", (512, 96), P["ink"]); draw = ImageDraw.Draw(sheet)
    for index, name in enumerate(names):
        image = icon(name); path = OUT / f"{name}.png"; image.save(path, optimize=True)
        pixels = list(image.getdata()); colors = len(set(pixels)); hard_alpha = all(pixel[3] in (0, 255) for pixel in pixels)
        records.append({"id": name, "path": str(path.relative_to(ROOT)).replace("\\", "/"), "size": [32, 32],
                        "colors": colors, "hardAlpha": hard_alpha, "sha256": hashlib.sha256(path.read_bytes()).hexdigest()})
        sheet.alpha_composite(image.resize((64, 64), Image.Resampling.NEAREST), (index * 64, 0))
        draw.text((index * 64 + 4, 72), name.upper(), fill=P["white"])
    status = "PASS" if all(record["hardAlpha"] and record["size"] == [32, 32] and record["colors"] <= 8 for record in records) else "FAIL"
    sheet.save(QA / "OCC_M-A8_通用导航图标_QA_v01.png", optimize=True)
    (QA / "OCC_M-A8_通用导航图标_QA_v01.json").write_text(json.dumps({"schema": "occ.ui.navigation.qa.v0.1", "status": status, "assetCount": len(records), "records": records}, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"generated={len(records)} status={status}")


if __name__ == "__main__":
    main()
