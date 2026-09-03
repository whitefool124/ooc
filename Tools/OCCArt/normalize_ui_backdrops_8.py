#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
M27 = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A27"
ARTIFACTS = ROOT / "UnityProject/Artifacts/UiBackdrops8"
STAGING = ROOT / "UnityProject/Assets/Game/Resources/Art/ValidationUIBackdrops"
FORMAL = ROOT / "UnityProject/Assets/Game/Resources/Art/FormalUIBackdrops"
STEMS = ("startup", "landing", "map", "briefing", "inventory", "settlement", "archive", "settings")


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def crop_16_9(image: Image.Image) -> Image.Image:
    target = 16 / 9
    ratio = image.width / image.height
    if ratio > target:
        width = round(image.height * target)
        left = (image.width - width) // 2
        return image.crop((left, 0, left + width, image.height))
    height = round(image.width / target)
    top = (image.height - height) // 2
    return image.crop((0, top, image.width, top + height))


def normalize(source: Path) -> Image.Image:
    image = crop_16_9(Image.open(source).convert("RGB"))
    image = image.resize((480, 270), Image.Resampling.BOX)
    image = image.quantize(colors=24, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert("RGBA")
    image.putalpha(255)
    return image


def build_contact(path: Path, use_old: bool) -> None:
    canvas = Image.new("RGB", (1920, 1080), "#171a1e")
    draw = ImageDraw.Draw(canvas)
    for index, stem in enumerate(STEMS):
        col, row = index % 4, index // 4
        x, y = 24 + col * 474, 48 + row * 510
        new = Image.open(STAGING / f"{stem}.png").convert("RGB").resize((450, 253), Image.Resampling.NEAREST)
        canvas.paste(new, (x, y + (240 if use_old else 30)))
        draw.text((x, y + (500 if use_old else 290)), stem.upper(), fill="#eadfc8")
        if use_old:
            old_path = FORMAL / f"{stem}.png"
            old = Image.open(old_path).convert("RGB").resize((450, 253), Image.Resampling.NEAREST) if old_path.is_file() else Image.new("RGB", (450, 253), "#101419")
            canvas.paste(old, (x, y - 24))
            draw.text((x, y + 216), "OLD", fill="#9e7660")
            draw.text((x + 395, y + 470), "NEW", fill="#7ba0a1")
    path.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(path)


def main() -> None:
    STAGING.mkdir(parents=True, exist_ok=True)
    for stem in STEMS:
        folder = ARTIFACTS / stem
        source = folder / "source.png"
        output = STAGING / f"{stem}.png"
        image = normalize(source)
        image.save(output)
        image.save(folder / "1x.png")
        image.resize((960, 540), Image.Resampling.NEAREST).save(folder / "2x.png")
        image.resize((1920, 1080), Image.Resampling.NEAREST).save(folder / "4x.png")
        gray = image.convert("L").convert("RGBA"); gray.putalpha(255); gray.save(folder / "grayscale.png")
        image.save(folder / "checker.png")
        manifest_path = M27 / "manifests" / f"backdrop_{stem}.occ-art-manifest-v1.json"
        manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
        manifest["provenance"]["source_sha256"] = sha256(source)
        manifest["delivery"]["output_sha256"] = sha256(output)
        manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    contacts = ARTIFACTS / "contacts"
    build_contact(contacts / "ui_backdrops_8_new_review.png", use_old=False)
    build_contact(contacts / "ui_backdrops_8_old_new_review.png", use_old=True)
    print(json.dumps({"status": "PASS", "normalized": len(STEMS)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
