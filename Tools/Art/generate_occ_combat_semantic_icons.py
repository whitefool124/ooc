#!/usr/bin/env python3
"""Deploy and QA the approved 16x16 OCC semantic micro-icons."""

from __future__ import annotations

import hashlib
import json
import shutil
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
MASTER = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A9/masters16"
OUT = ROOT / "UnityProject/Assets/Game/Resources/Art/FormalResourceIcons32"
QA = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A9"
FILES = (
    ("action", "action_16.png", "action_point.png"),
    ("aether", "aether_16.png", "mana.png"),
    ("notice", "notice_16.png", "notice.png"),
)


def record(runtime_id: str, path: Path) -> dict:
    image = Image.open(path).convert("RGBA")
    pixels = list(image.get_flattened_data())
    opaque = [(x, y) for y in range(16) for x in range(16) if image.getpixel((x, y))[3] == 255]
    return {
        "id": runtime_id,
        "path": str(path.relative_to(ROOT)).replace("\\", "/"),
        "size": list(image.size),
        "colorsIncludingTransparency": len(set(pixels)),
        "hardAlpha": all(pixel[3] in (0, 255) for pixel in pixels),
        "opaquePixels": len(opaque),
        "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
    }


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    QA.mkdir(parents=True, exist_ok=True)
    for _, source_name, runtime_name in FILES:
        shutil.copyfile(MASTER / source_name, OUT / runtime_name)

    records = [record(runtime_id, OUT / runtime_name) for runtime_id, _, runtime_name in FILES]
    status = "PASS" if all(
        item["size"] == [16, 16]
        and item["hardAlpha"]
        and item["colorsIncludingTransparency"] <= 4
        for item in records
    ) else "FAIL"

    sheet = Image.new("RGBA", (336, 152), (14, 18, 23, 255))
    draw = ImageDraw.Draw(sheet)
    for y in range(sheet.height):
        for x in range(sheet.width):
            if ((x // 8) + (y // 8)) % 2 == 0:
                sheet.putpixel((x, y), (22, 27, 33, 255))
    for index, (runtime_id, _, runtime_name) in enumerate(FILES):
        image = Image.open(OUT / runtime_name).convert("RGBA")
        x = 8 + index * 112
        sheet.alpha_composite(image.resize((96, 96), Image.Resampling.NEAREST), (x, 8))
        sheet.alpha_composite(image, (x + 40, 112))
        draw.text((x + 48, 142), runtime_id.upper(), fill=(232, 226, 202, 255), anchor="mm")
    sheet.save(QA / "OCC_M-A9_战斗语义图标_QA_v03_16px.png", optimize=True)

    report = {
        "schema": "occ.ui.combat-semantics.qa.v0.3",
        "status": status,
        "sourceSize": [16, 16],
        "displayContract": "2x at 1920x1080; 1x at 960x540",
        "method": "direct generation, chroma removal, semantic palette normalization, hard-alpha 16x16 master",
        "assetCount": len(records),
        "records": records,
    }
    (QA / "OCC_M-A9_战斗语义图标_QA_v03_16px.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"deployed={len(records)} status={status}")


if __name__ == "__main__":
    main()
