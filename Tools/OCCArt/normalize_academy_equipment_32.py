#!/usr/bin/env python3
"""Normalize M-A20 independent sources without adding visual content."""

from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A20/academy_equipment_32_catalog.json"
MANIFESTS = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A20/manifests"


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def normalize(path: Path) -> Image.Image:
    source = Image.open(path).convert("RGBA")
    alpha = source.getchannel("A").point(lambda value: 255 if value >= 128 else 0)
    source.putalpha(alpha)
    bounds = alpha.getbbox()
    if bounds is None:
        raise ValueError(f"empty source: {path}")
    crop = source.crop(bounds)
    available = 28
    scale = min(available / crop.width, available / crop.height)
    size = (max(1, round(crop.width * scale)), max(1, round(crop.height * scale)))
    fitted = crop.resize(size, Image.Resampling.NEAREST)
    fitted_alpha = fitted.getchannel("A").point(lambda value: 255 if value >= 128 else 0)
    rgb = fitted.convert("RGB").quantize(colors=10, method=Image.Quantize.MEDIANCUT).convert("RGB")
    fitted = rgb.convert("RGBA")
    fitted.putalpha(fitted_alpha)
    canvas = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    canvas.alpha_composite(fitted, ((32 - fitted.width) // 2, (32 - fitted.height) // 2))
    return canvas


def checker() -> Image.Image:
    image = Image.new("RGBA", (128, 128))
    draw = ImageDraw.Draw(image)
    for y in range(0, 128, 8):
        for x in range(0, 128, 8):
            value = 76 if ((x // 8) + (y // 8)) % 2 else 122
            draw.rectangle((x, y, x + 7, y + 7), fill=(value, value, value, 255))
    return image


def main() -> None:
    assets = json.loads(CATALOG.read_text(encoding="utf-8"))["assets"]
    for value in assets:
        source_path = ROOT / value["source_path"]
        output_path = ROOT / value["staging_path"]
        output_path.parent.mkdir(parents=True, exist_ok=True)
        icon = normalize(source_path)
        icon.save(output_path)

        evidence = ROOT / f"UnityProject/Artifacts/AcademyEquipment32/{value['stem']}"
        icon.save(evidence / "1x.png")
        icon.resize((128, 128), Image.Resampling.NEAREST).save(evidence / "4x.png")
        gray = icon.convert("LA").convert("RGBA")
        gray.putalpha(icon.getchannel("A"))
        gray.save(evidence / "grayscale.png")
        board = checker()
        board.alpha_composite(icon.resize((128, 128), Image.Resampling.NEAREST))
        board.save(evidence / "checker.png")

        manifest_path = MANIFESTS / f"{value['stem']}.occ-art-manifest-v1.json"
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        manifest["provenance"]["source_sha256"] = digest(source_path)
        manifest["delivery"]["output_sha256"] = digest(output_path)
        manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"normalized": len(assets), "size": [32, 32], "palette_max": 10}))


if __name__ == "__main__":
    main()
