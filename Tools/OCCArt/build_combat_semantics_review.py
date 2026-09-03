#!/usr/bin/env python3
"""Build offline M-A24 review sheets without touching Unity."""
from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A24/combat_semantics_31_catalog.json"
OUTPUT = ROOT / "UnityProject/Artifacts/CombatSemantics31/contacts"


def font(size: int) -> ImageFont.ImageFont:
    candidates = (
        Path("C:/Windows/Fonts/consola.ttf"),
        Path("C:/Windows/Fonts/arial.ttf"),
    )
    for candidate in candidates:
        if candidate.exists():
            return ImageFont.truetype(str(candidate), size)
    return ImageFont.load_default()


def render(width: int, height: int) -> Path:
    scale = width / 1920
    canvas = Image.new("RGB", (width, height), "#10161b")
    draw = ImageDraw.Draw(canvas)
    title_font = font(max(14, round(28 * scale)))
    group_font = font(max(11, round(18 * scale)))
    label_font = font(max(8, round(13 * scale)))
    margin = round(52 * scale)
    draw.text((margin, round(26 * scale)), "OCC M-A24 COMBAT SEMANTICS — OFFLINE REVIEW", fill="#eee6d1", font=title_font)
    draw.text((margin, round(65 * scale)), "native 16px commands/intents  |  native 32px statuses/feedback", fill="#8d9ca3", font=label_font)

    groups = {"command": [], "intent": [], "status": [], "feedback": []}
    for asset in json.loads(CATALOG.read_text(encoding="utf-8"))["assets"]:
        groups[asset["group"]].append(asset)

    layout = {
        "command": (margin, round(112 * scale), 6),
        "intent": (margin, round(290 * scale), 5),
        "status": (margin, round(468 * scale), 6),
        "feedback": (margin, round(680 * scale), 8),
    }
    card_w = round(174 * scale)
    card_h = round(122 * scale)
    gap = round(14 * scale)
    icon_box = round(72 * scale)

    for group, assets in groups.items():
        start_x, start_y, columns = layout[group]
        draw.text((start_x, start_y - round(30 * scale)), group.upper(), fill="#c8a96b", font=group_font)
        for index, asset in enumerate(assets):
            row, column = divmod(index, columns)
            x = start_x + column * (card_w + gap)
            y = start_y + row * (card_h + gap)
            draw.rounded_rectangle((x, y, x + card_w, y + card_h), radius=max(2, round(6 * scale)), fill="#1b252b", outline="#34434a", width=max(1, round(scale)))
            path = ROOT / asset["staging_path"]
            if path.exists():
                icon = Image.open(path).convert("RGBA")
                base_scale = 4 if asset["delivery_size"][0] == 16 else 2
                native_scale = max(1, round(base_scale * scale))
                icon = icon.resize((icon.width * native_scale, icon.height * native_scale), Image.Resampling.NEAREST)
                px = x + (card_w - icon.width) // 2
                py = y + round(9 * scale)
                canvas.paste(icon, (px, py), icon)
            else:
                cx = x + card_w // 2
                cy = y + icon_box // 2
                draw.line((cx - 15, cy - 15, cx + 15, cy + 15), fill="#b75b4c", width=max(1, round(3 * scale)))
                draw.line((cx + 15, cy - 15, cx - 15, cy + 15), fill="#b75b4c", width=max(1, round(3 * scale)))
            label = asset["stem"]
            bbox = draw.textbbox((0, 0), label, font=label_font)
            draw.text((x + (card_w - (bbox[2] - bbox[0])) // 2, y + card_h - round(27 * scale)), label, fill="#d9d4c5", font=label_font)

    OUTPUT.mkdir(parents=True, exist_ok=True)
    result = OUTPUT / f"offline_review_{width}x{height}.png"
    canvas.save(result)
    return result


def main() -> None:
    print(render(1920, 1080))
    print(render(960, 540))


if __name__ == "__main__":
    main()
