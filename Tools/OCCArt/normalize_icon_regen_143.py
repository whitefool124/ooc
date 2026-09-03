#!/usr/bin/env python3
"""Normalize generated M-A19 icon sources and refresh their QA_PENDING manifests."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A19/icon_regen_143_catalog.json"
MANIFESTS = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A19/manifests"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def hard_crop(source: Image.Image) -> Image.Image:
    rgba = source.convert("RGBA")
    alpha = rgba.getchannel("A").point(lambda value: 255 if value >= 128 else 0)
    rgba.putalpha(alpha)
    box = alpha.getbbox()
    if not box:
        raise ValueError("source has no visible pixels")
    return rgba.crop(box)


def normalize(source: Image.Image, size: int, palette_max: int) -> Image.Image:
    border = 1 if size == 16 else 2
    crop = hard_crop(source)
    limit = size - border * 2
    scale = min(limit / crop.width, limit / crop.height)
    fitted = crop.resize((max(1, round(crop.width * scale)), max(1, round(crop.height * scale))), Image.Resampling.LANCZOS)
    alpha = fitted.getchannel("A").point(lambda value: 255 if value >= 128 else 0)
    rgb = fitted.convert("RGB").quantize(colors=palette_max, method=Image.Quantize.MEDIANCUT).convert("RGB")
    fitted = rgb.convert("RGBA")
    fitted.putalpha(alpha)
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    canvas.alpha_composite(fitted, ((size - fitted.width) // 2, (size - fitted.height) // 2))
    return canvas


def checker(size: tuple[int, int], block: int) -> Image.Image:
    image = Image.new("RGBA", size)
    draw = ImageDraw.Draw(image)
    for y in range(0, size[1], block):
        for x in range(0, size[0], block):
            value = 74 if ((x // block) + (y // block)) % 2 else 116
            draw.rectangle((x, y, x + block - 1, y + block - 1), fill=(value, value, value, 255))
    return image


def write_evidence(icon: Image.Image, evidence_root: Path) -> None:
    evidence_root.mkdir(parents=True, exist_ok=True)
    icon.save(evidence_root / "1x.png")
    icon.resize((icon.width * 4, icon.height * 4), Image.Resampling.NEAREST).save(evidence_root / "4x.png")
    grayscale = icon.convert("LA").convert("RGBA")
    grayscale.putalpha(icon.getchannel("A"))
    grayscale.save(evidence_root / "grayscale.png")
    background = checker((icon.width * 4, icon.height * 4), 8)
    background.alpha_composite(icon.resize(background.size, Image.Resampling.NEAREST))
    background.save(evidence_root / "checker.png")


def process(value: dict) -> bool:
    group, stem = value["group"], value["stem"]
    evidence_root = ROOT / f"UnityProject/Artifacts/IconRegen143/{group}/{stem}"
    source_path = evidence_root / "source.png"
    if not source_path.exists():
        return False
    output_path = ROOT / value["staging_path"]
    output_path.parent.mkdir(parents=True, exist_ok=True)
    size = int(value["delivery_size"][0])
    icon = normalize(Image.open(source_path), size, int(value["palette_max"]))
    icon.save(output_path)
    write_evidence(icon, evidence_root)

    manifest_path = MANIFESTS / group / f"{stem}.occ-art-manifest-v1.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    manifest["provenance"]["source_sha256"] = sha256(source_path)
    manifest["delivery"]["output_sha256"] = sha256(output_path)
    manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    return True


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--asset-id")
    args = parser.parse_args()
    values = json.loads(CATALOG.read_text(encoding="utf-8"))["assets"]
    if args.asset_id:
        values = [value for value in values if value["asset_id"] == args.asset_id]
        if not values:
            raise SystemExit(f"unknown asset id: {args.asset_id}")
    count = sum(process(value) for value in values)
    print(json.dumps({"normalized": count, "requested": len(values)}))


if __name__ == "__main__":
    main()
