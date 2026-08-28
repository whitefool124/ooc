#!/usr/bin/env python3
"""Build review contacts for M-A20 normalized equipment icons."""

from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A20/academy_equipment_32_catalog.json"
CONTACTS = ROOT / "UnityProject/Artifacts/AcademyEquipment32/contacts"


def font(size: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    for path in [Path("C:/Windows/Fonts/msyh.ttc"), Path("C:/Windows/Fonts/arial.ttf")]:
        if path.exists():
            return ImageFont.truetype(str(path), size)
    return ImageFont.load_default()


def build(width: int, height: int) -> Path:
    assets = json.loads(CATALOG.read_text(encoding="utf-8"))["assets"]
    canvas = Image.new("RGB", (width, height), (31, 29, 26))
    draw = ImageDraw.Draw(canvas)
    columns, rows = 8, 4
    margin_x, margin_y = 34, 88
    cell_w = (width - margin_x * 2) // columns
    cell_h = (height - margin_y - 30) // rows
    icon_scale = 4 if width >= 1600 else 2
    title_font = font(30 if width >= 1600 else 18)
    label_font = font(16 if width >= 1600 else 10)
    draw.text((margin_x, 26), "OCC 学院装备 32 件 · M-A20 REVIEW CONTACT", fill=(231, 218, 189), font=title_font)
    for index, value in enumerate(assets):
        col, row = index % columns, index // columns
        x, y = margin_x + col * cell_w, margin_y + row * cell_h
        draw.rounded_rectangle((x + 4, y + 4, x + cell_w - 8, y + cell_h - 8), radius=8,
                               fill=(221, 210, 184), outline=(102, 92, 75), width=2)
        icon = Image.open(ROOT / value["staging_path"]).convert("RGBA")
        scaled = icon.resize((32 * icon_scale, 32 * icon_scale), Image.Resampling.NEAREST)
        px = x + (cell_w - scaled.width) // 2
        py = y + max(10, (cell_h - scaled.height) // 2 - 12)
        canvas.paste(scaled, (px, py), scaled)
        draw.text((x + 12, y + cell_h - 50), value["runtime_id"], fill=(62, 56, 48), font=label_font)
        draw.text((x + 12, y + cell_h - 29), value["name"], fill=(38, 35, 31), font=label_font)
    CONTACTS.mkdir(parents=True, exist_ok=True)
    path = CONTACTS / f"normalized_contact_{width}x{height}.png"
    canvas.save(path)
    return path


def main() -> None:
    print(json.dumps({"contacts": [str(build(1920, 1080)), str(build(960, 540))]}, ensure_ascii=False))


if __name__ == "__main__":
    main()
