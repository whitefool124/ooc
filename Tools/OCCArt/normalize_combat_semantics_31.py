#!/usr/bin/env python3
"""Normalize M-A24 combat semantic icons and emit per-asset QA evidence."""
from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A24/combat_semantics_31_catalog.json"
MANIFESTS = CATALOG.parent / "manifests"


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def normalize(path: Path, width: int, height: int, palette_max: int) -> Image.Image:
    image = Image.open(path).convert("RGBA")
    alpha = image.getchannel("A").point(lambda value: 255 if value >= 128 else 0)
    image.putalpha(alpha)
    bounds = alpha.getbbox()
    if bounds is None:
        raise ValueError(f"empty source: {path}")

    crop = image.crop(bounds)
    safe_border = 1 if width == 16 else 2
    scale = min(
        (width - safe_border * 2) / crop.width,
        (height - safe_border * 2) / crop.height,
    )
    fitted_size = (
        max(1, round(crop.width * scale)),
        max(1, round(crop.height * scale)),
    )
    fitted = crop.resize(fitted_size, Image.Resampling.NEAREST)
    fitted_alpha = fitted.getchannel("A").point(lambda value: 255 if value >= 128 else 0)
    quantized = fitted.convert("RGB").quantize(
        colors=palette_max,
        method=Image.Quantize.MEDIANCUT,
    ).convert("RGBA")
    quantized.putalpha(fitted_alpha)

    output = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    output.alpha_composite(
        quantized,
        ((width - quantized.width) // 2, (height - quantized.height) // 2),
    )
    return output


def checker(width: int, height: int) -> Image.Image:
    image = Image.new("RGBA", (width, height))
    draw = ImageDraw.Draw(image)
    cell = 4 if width <= 64 else 8
    for y in range(0, height, cell):
        for x in range(0, width, cell):
            value = 76 if ((x // cell) + (y // cell)) % 2 else 122
            draw.rectangle((x, y, x + cell - 1, y + cell - 1), fill=(value, value, value, 255))
    return image


def main() -> None:
    assets = json.loads(CATALOG.read_text(encoding="utf-8"))["assets"]
    normalized: list[str] = []
    missing: list[str] = []
    for asset in assets:
        source = ROOT / asset["source_path"]
        if not source.exists():
            missing.append(asset["asset_id"])
            continue

        width, height = asset["delivery_size"]
        output = ROOT / asset["staging_path"]
        output.parent.mkdir(parents=True, exist_ok=True)
        image = normalize(source, width, height, asset["palette_max"])
        image.save(output)

        evidence = source.parent
        image.save(evidence / "1x.png")
        enlarged = image.resize((width * 4, height * 4), Image.Resampling.NEAREST)
        enlarged.save(evidence / "4x.png")
        grayscale = image.convert("LA").convert("RGBA")
        grayscale.putalpha(image.getchannel("A"))
        grayscale.save(evidence / "grayscale.png")
        checkerboard = checker(width * 4, height * 4)
        checkerboard.alpha_composite(enlarged)
        checkerboard.save(evidence / "checker.png")

        manifest_path = MANIFESTS / f"{asset['group']}_{asset['stem']}.occ-art-manifest-v1.json"
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        manifest["provenance"]["source_sha256"] = digest(source)
        manifest["delivery"]["output_sha256"] = digest(output)
        manifest_path.write_text(
            json.dumps(manifest, ensure_ascii=False, indent=2) + "\n",
            encoding="utf-8",
        )
        normalized.append(asset["asset_id"])

    print(json.dumps({"normalized": len(normalized), "missing": missing}, ensure_ascii=False))


if __name__ == "__main__":
    main()
