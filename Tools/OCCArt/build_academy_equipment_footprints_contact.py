#!/usr/bin/env python3
"""Build an all-item M-A21 footprint review sheet."""

from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A21/academy_equipment_footprints_32_catalog.json"
OUT = ROOT / "UnityProject/Artifacts/AcademyEquipmentFootprints32/contacts"


def get_font(size: int):
    path = Path("C:/Windows/Fonts/msyh.ttc")
    return ImageFont.truetype(str(path), size) if path.exists() else ImageFont.load_default()


def build(width: int, height: int) -> Path:
    assets = json.loads(CATALOG.read_text(encoding="utf-8"))["assets"]
    canvas = Image.new("RGB", (width, height), (31, 29, 26)); draw = ImageDraw.Draw(canvas)
    margin_x, top, gap = width * 0.02, height * 0.075, width * 0.004
    cell_w = (width - margin_x * 2 - gap * 7) / 8
    cell_h = (height - top - height * 0.025 - gap * 3) / 4
    title = get_font(28 if width >= 1600 else 15); label = get_font(13 if width >= 1600 else 8)
    draw.text((margin_x, 20), "OCC M-A21 学院装备多格占格 · 32 件", fill=(233, 220, 190), font=title)
    scale = 2 if width >= 1600 else 1
    for index, value in enumerate(assets):
        col, row = index % 8, index // 8
        x = int(margin_x + col * (cell_w + gap)); y = int(top + row * (cell_h + gap))
        draw.rectangle((x, y, int(x + cell_w), int(y + cell_h)), fill=(220, 209, 183), outline=(79, 71, 59), width=3)
        icon = Image.open(ROOT / value["staging_path"]).convert("RGBA")
        scaled = icon.resize((icon.width * scale, icon.height * scale), Image.Resampling.NEAREST)
        max_w, max_h = int(cell_w - 14), int(cell_h - 40)
        if scaled.width > max_w or scaled.height > max_h:
            shrink = min(max_w / scaled.width, max_h / scaled.height)
            shrink = max(1 / scale, int(shrink * scale) / scale)
            scaled = icon.resize((max(1, round(icon.width * shrink)), max(1, round(icon.height * shrink))), Image.Resampling.NEAREST)
        canvas.paste(scaled, (x + int((cell_w - scaled.width) / 2), y + 8 + int((cell_h - 42 - scaled.height) / 2)), scaled)
        cells = value["logical_cells"]
        draw.text((x + 8, int(y + cell_h - 31)), f"{value['runtime_id']}  {cells[0]}×{cells[1]}", fill=(55, 50, 43), font=label)
        draw.text((x + 8, int(y + cell_h - 16)), value["name"], fill=(35, 32, 28), font=label)
    OUT.mkdir(parents=True, exist_ok=True)
    path = OUT / f"normalized_contact_{width}x{height}.png"; canvas.save(path); return path


def main() -> None:
    print(json.dumps({"contacts": [str(build(1920,1080)), str(build(960,540))]}, ensure_ascii=False))


if __name__ == "__main__":
    main()
