#!/usr/bin/env python3
"""Normalize M-A21 footprint sources into exact W*32 by H*32 canvases."""

from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A21/academy_equipment_footprints_32_catalog.json"
MANIFESTS = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A21/manifests"


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def normalize(source_path: Path, width: int, height: int) -> Image.Image:
    source = Image.open(source_path).convert("RGBA")
    alpha = source.getchannel("A").point(lambda value: 255 if value >= 128 else 0)
    source.putalpha(alpha)
    bounds = alpha.getbbox()
    if bounds is None:
        raise ValueError(f"empty source: {source_path}")
    crop = source.crop(bounds)
    available_w, available_h = width - 4, height - 4
    scale = min(available_w / crop.width, available_h / crop.height)
    fitted_size = (max(1, round(crop.width * scale)), max(1, round(crop.height * scale)))
    fitted = crop.resize(fitted_size, Image.Resampling.NEAREST)
    fitted_alpha = fitted.getchannel("A").point(lambda value: 255 if value >= 128 else 0)
    rgb = fitted.convert("RGB").quantize(colors=12, method=Image.Quantize.MEDIANCUT).convert("RGB")
    fitted = rgb.convert("RGBA"); fitted.putalpha(fitted_alpha)
    canvas = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    canvas.alpha_composite(fitted, ((width - fitted.width) // 2, (height - fitted.height) // 2))
    return canvas


def checker(width: int, height: int) -> Image.Image:
    image = Image.new("RGBA", (width, height)); draw = ImageDraw.Draw(image)
    for y in range(0, height, 8):
        for x in range(0, width, 8):
            value = 76 if ((x // 8) + (y // 8)) % 2 else 122
            draw.rectangle((x, y, x + 7, y + 7), fill=(value, value, value, 255))
    return image


def main() -> None:
    assets = json.loads(CATALOG.read_text(encoding="utf-8"))["assets"]
    for value in assets:
        width, height = value["delivery_size"]
        source_path = ROOT / value["source_path"]
        output_path = ROOT / value["staging_path"]
        output_path.parent.mkdir(parents=True, exist_ok=True)
        image = normalize(source_path, width, height)
        image.save(output_path)
        evidence = ROOT / f"UnityProject/Artifacts/AcademyEquipmentFootprints32/{value['stem']}"
        image.save(evidence / "1x.png")
        image.resize((width * 4, height * 4), Image.Resampling.NEAREST).save(evidence / "4x.png")
        gray = image.convert("LA").convert("RGBA"); gray.putalpha(image.getchannel("A")); gray.save(evidence / "grayscale.png")
        board = checker(width * 4, height * 4); board.alpha_composite(image.resize(board.size, Image.Resampling.NEAREST)); board.save(evidence / "checker.png")
        manifest_path = MANIFESTS / f"{value['stem']}.occ-art-manifest-v1.json"
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        manifest["provenance"]["source_sha256"] = digest(source_path)
        manifest["delivery"]["output_sha256"] = digest(output_path)
        manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"normalized": len(assets), "sizes": sorted({tuple(v['delivery_size']) for v in assets})}))


if __name__ == "__main__":
    main()
