#!/usr/bin/env python3
"""Build the M-A24 focused before/after review from HEAD and formal outputs."""
from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A24/combat_semantics_31_catalog.json"
BEFORE_ROOT = ROOT / "UnityProject/Artifacts/CombatSemantics31/before_sources"
OUTPUT = ROOT / "UnityProject/Artifacts/CombatSemantics31/contacts/combat_semantics_before_after_1920x1080.png"


def font(size: int) -> ImageFont.ImageFont:
    for path in (Path("C:/Windows/Fonts/consola.ttf"), Path("C:/Windows/Fonts/arial.ttf")):
        if path.exists():
            return ImageFont.truetype(str(path), size)
    return ImageFont.load_default()


def before_path(asset: dict) -> Path:
    folder = {
        "command": "FormalIcons32",
        "intent": "FormalIntentIcons16",
        "status": "FormalStatusIcons32",
        "feedback": "FormalFeedbackIcons32",
    }[asset["group"]]
    return BEFORE_ROOT / f"UnityProject/Assets/Game/Resources/Art/{folder}/{asset['stem']}.png"


def draw_panel(canvas: Image.Image, x0: int, title: str, before: bool, assets: list[dict]) -> None:
    draw = ImageDraw.Draw(canvas)
    title_font, group_font, label_font = font(28), font(16), font(11)
    draw.rounded_rectangle((x0, 18, x0 + 920, 1060), radius=8, fill="#182227", outline="#46565c", width=2)
    draw.text((x0 + 28, 36), title, fill="#eee6d1", font=title_font)
    layouts = {"command": (104, 6), "intent": (284, 5), "status": (464, 6), "feedback": (652, 7)}
    for group, (y0, columns) in layouts.items():
        selected = [asset for asset in assets if asset["group"] == group]
        draw.text((x0 + 28, y0 - 28), group.upper(), fill="#c7a65e", font=group_font)
        card_w, card_h, gap = 116, 118, 10
        for index, asset in enumerate(selected):
            row, column = divmod(index, columns)
            x = x0 + 28 + column * (card_w + gap)
            y = y0 + row * (card_h + gap)
            draw.rounded_rectangle((x, y, x + card_w, y + card_h), radius=5, fill="#202c31", outline="#3b4a50")
            path = before_path(asset) if before else ROOT / asset["final_path"]
            icon = Image.open(path).convert("RGBA")
            target = 64
            factor = max(1, target // max(icon.width, icon.height))
            icon = icon.resize((icon.width * factor, icon.height * factor), Image.Resampling.NEAREST)
            canvas.paste(icon, (x + (card_w - icon.width) // 2, y + 10), icon)
            label = asset["stem"]
            box = draw.textbbox((0, 0), label, font=label_font)
            draw.text((x + (card_w - box[2] + box[0]) // 2, y + 94), label, fill="#d7d1c2", font=label_font)


def main() -> None:
    assets = json.loads(CATALOG.read_text(encoding="utf-8"))["assets"]
    canvas = Image.new("RGB", (1920, 1080), "#0d1418")
    draw_panel(canvas, 20, "BEFORE — previous formal icons", True, assets)
    draw_panel(canvas, 980, "AFTER — M-A24 semantic system", False, assets)
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(OUTPUT)
    print(OUTPUT)


if __name__ == "__main__":
    main()
