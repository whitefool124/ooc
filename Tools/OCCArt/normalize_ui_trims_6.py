#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
M28 = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A28"
ARTIFACTS = ROOT / "UnityProject/Artifacts/UiTrims6"
STAGING = ROOT / "UnityProject/Assets/Game/Resources/Art/ValidationUITrims"
SIZES = {
    "binding_spine": (32, 64),
    "index_tab": (64, 32),
    "measure_ruler": (64, 32),
    "corner_clasp": (32, 32),
    "folded_corner": (64, 64),
    "status_clip": (32, 32),
}


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def normalize(source: Path, size: tuple[int, int]) -> Image.Image:
    image = Image.open(source).convert("RGBA")
    alpha = image.getchannel("A").point(lambda value: 255 if value >= 32 else 0)
    bounds = alpha.getbbox()
    if not bounds:
        raise RuntimeError(f"no visible content in {source}")
    image.putalpha(alpha)
    image = image.crop(bounds)
    inner_w, inner_h = size[0] - 2, size[1] - 2
    scale = min(inner_w / image.width, inner_h / image.height)
    resized_size = (max(1, round(image.width * scale)), max(1, round(image.height * scale)))
    image = image.resize(resized_size, Image.Resampling.BOX)
    hard_alpha = image.getchannel("A").point(lambda value: 255 if value >= 96 else 0)
    quantized = image.quantize(colors=10, method=Image.Quantize.FASTOCTREE, dither=Image.Dither.NONE).convert("RGBA")
    quantized.putalpha(hard_alpha)
    canvas = Image.new("RGBA", size, (0, 0, 0, 0))
    canvas.alpha_composite(quantized, ((size[0] - resized_size[0]) // 2, (size[1] - resized_size[1]) // 2))
    return canvas


def checker(image: Image.Image, scale: int = 4) -> Image.Image:
    enlarged = image.resize((image.width * scale, image.height * scale), Image.Resampling.NEAREST)
    result = Image.new("RGBA", enlarged.size)
    draw = ImageDraw.Draw(result)
    cell = 8
    for y in range(0, result.height, cell):
        for x in range(0, result.width, cell):
            value = 226 if (x // cell + y // cell) % 2 == 0 else 178
            draw.rectangle((x, y, x + cell - 1, y + cell - 1), fill=(value, value, value, 255))
    result.alpha_composite(enlarged)
    return result


def grayscale(image: Image.Image) -> Image.Image:
    gray = image.convert("L")
    result = Image.merge("RGBA", (gray, gray, gray, image.getchannel("A")))
    return result


def build_contact(images: dict[str, Image.Image]) -> None:
    contacts = ARTIFACTS / "contacts"
    contacts.mkdir(parents=True, exist_ok=True)
    tile_w, tile_h = 320, 300
    sheet = Image.new("RGB", (tile_w * 3, tile_h * 2), (42, 40, 35))
    draw = ImageDraw.Draw(sheet)
    for index, (stem, image) in enumerate(images.items()):
        x, y = (index % 3) * tile_w, (index // 3) * tile_h
        preview = checker(image, 4).convert("RGB")
        sheet.paste(preview, (x + (tile_w - preview.width) // 2, y + 38 + (220 - preview.height) // 2))
        draw.text((x + 12, y + 10), stem, fill=(242, 235, 221))
        draw.text((x + 12, y + 274), f"{image.width}x{image.height} | 4x checker", fill=(196, 187, 170))
    sheet.save(contacts / "ui_trims_6_review.png")


def main() -> None:
    STAGING.mkdir(parents=True, exist_ok=True)
    normalized: dict[str, Image.Image] = {}
    for stem, size in SIZES.items():
        folder = ARTIFACTS / stem
        source = folder / "source.png"
        image = normalize(source, size)
        normalized[stem] = image
        output = STAGING / f"{stem}.png"
        image.save(output)
        image.save(folder / "1x.png")
        image.resize((size[0] * 2, size[1] * 2), Image.Resampling.NEAREST).save(folder / "2x.png")
        image.resize((size[0] * 4, size[1] * 4), Image.Resampling.NEAREST).save(folder / "4x.png")
        grayscale(image).save(folder / "grayscale.png")
        checker(image).save(folder / "checker.png")

        manifest_path = M28 / "manifests" / f"trim_{stem}.occ-art-manifest-v1.json"
        manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
        manifest["provenance"]["source_sha256"] = digest(source)
        manifest["delivery"]["output_sha256"] = digest(output)
        manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    build_contact(normalized)
    print(json.dumps({"status": "PASS", "normalized": len(normalized)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
