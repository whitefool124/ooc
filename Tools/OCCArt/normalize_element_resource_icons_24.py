#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
PRODUCTION = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A26"
CATALOG = PRODUCTION / "element_resources_24_catalog.json"
MANIFESTS = PRODUCTION / "manifests"


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def checker(size: int, block: int = 8) -> Image.Image:
    image = Image.new("RGBA", (size, size))
    draw = ImageDraw.Draw(image)
    for y in range(0, size, block):
        for x in range(0, size, block):
            value = 72 if ((x // block) + (y // block)) % 2 else 118
            draw.rectangle((x, y, x + block - 1, y + block - 1), fill=(value, value, value, 255))
    return image


def normalize(source: Path) -> Image.Image:
    image = Image.open(source).convert("RGBA")
    alpha = image.getchannel("A").point(lambda value: 255 if value >= 128 else 0)
    image.putalpha(alpha)
    bounds = alpha.getbbox()
    if bounds is None:
        raise ValueError(f"empty source: {source}")
    crop = image.crop(bounds)
    scale = min(28 / crop.width, 28 / crop.height)
    fitted = crop.resize((max(1, round(crop.width * scale)), max(1, round(crop.height * scale))), Image.Resampling.NEAREST)
    fitted_alpha = fitted.getchannel("A").point(lambda value: 255 if value >= 128 else 0)
    quantized = fitted.convert("RGB").quantize(colors=10, method=Image.Quantize.MEDIANCUT).convert("RGBA")
    quantized.putalpha(fitted_alpha)
    output = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    output.alpha_composite(quantized, ((32 - quantized.width) // 2, (32 - quantized.height) // 2))
    return output


def main() -> None:
    assets = json.loads(CATALOG.read_text(encoding="utf-8"))["assets"]
    missing = []
    for asset in assets:
        source = ROOT / asset["source_path"]
        if not source.exists():
            missing.append(asset["asset_id"])
            continue
        output = ROOT / asset["staging_path"]
        output.parent.mkdir(parents=True, exist_ok=True)
        image = normalize(source)
        image.save(output)
        evidence = source.parent
        image.save(evidence / "1x.png")
        four = image.resize((128, 128), Image.Resampling.NEAREST)
        four.save(evidence / "4x.png")
        grayscale = image.convert("LA").convert("RGBA")
        grayscale.putalpha(image.getchannel("A"))
        grayscale.save(evidence / "grayscale.png")
        checked = checker(128)
        checked.alpha_composite(four)
        checked.save(evidence / "checker.png")

        manifest_path = MANIFESTS / f"{asset['group']}_{asset['stem']}.occ-art-manifest-v1.json"
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        manifest["provenance"]["source_sha256"] = digest(source)
        manifest["delivery"]["output_sha256"] = digest(output)
        manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"normalized": len(assets) - len(missing), "missing": missing}, ensure_ascii=False))


if __name__ == "__main__":
    main()
