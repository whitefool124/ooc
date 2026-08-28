"""Normalize one independently generated material pickup to native 24px and a 32px UI delivery canvas.

This tool performs alpha isolation, whole-silhouette reduction, palette limiting,
centering and QA only. It does not draw, repair or assemble asset structure.
"""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw


def arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", required=True, type=Path)
    parser.add_argument("--output24", required=True, type=Path)
    parser.add_argument("--output32", required=True, type=Path)
    parser.add_argument("--qa-dir", required=True, type=Path)
    parser.add_argument("--name", required=True)
    parser.add_argument("--max-width", default=20, type=int)
    parser.add_argument("--max-height", default=20, type=int)
    parser.add_argument("--colors", default=8, type=int)
    return parser.parse_args()


def hard_isolate(source: Image.Image) -> Image.Image:
    rgba = source.convert("RGBA")
    alpha = rgba.getchannel("A").point(lambda value: 255 if value >= 96 else 0)
    rgba.putalpha(alpha)
    return rgba


def palette_limit(image: Image.Image, colors: int) -> Image.Image:
    alpha = image.getchannel("A").point(lambda value: 255 if value else 0)
    rgb = Image.new("RGB", image.size)
    rgb.paste(image.convert("RGB"), mask=alpha)
    limited = rgb.quantize(colors=colors, dither=Image.Dither.NONE).convert("RGBA")
    limited.putalpha(alpha)
    return limited


def checkerboard(image: Image.Image, scale: int) -> Image.Image:
    enlarged = image.resize((image.width * scale, image.height * scale), Image.Resampling.NEAREST)
    board = Image.new("RGBA", enlarged.size)
    draw = ImageDraw.Draw(board)
    block = 8
    for y in range(0, board.height, block):
        for x in range(0, board.width, block):
            color = (206, 198, 181, 255) if ((x // block) + (y // block)) % 2 == 0 else (116, 109, 98, 255)
            draw.rectangle((x, y, x + block - 1, y + block - 1), fill=color)
    board.alpha_composite(enlarged)
    return board


def main() -> None:
    args = arguments()
    args.output24.parent.mkdir(parents=True, exist_ok=True)
    args.output32.parent.mkdir(parents=True, exist_ok=True)
    args.qa_dir.mkdir(parents=True, exist_ok=True)

    isolated = hard_isolate(Image.open(args.source))
    source_bounds = isolated.getchannel("A").getbbox()
    if not source_bounds:
        raise RuntimeError("No opaque pickup remains after alpha isolation")
    cropped = isolated.crop(source_bounds)
    scale = min(args.max_width / cropped.width, args.max_height / cropped.height)
    target = (max(1, round(cropped.width * scale)), max(1, round(cropped.height * scale)))
    reduced = cropped.resize(target, Image.Resampling.BOX)
    reduced.putalpha(reduced.getchannel("A").point(lambda value: 255 if value >= 96 else 0))
    reduced = palette_limit(reduced, args.colors)

    native = Image.new("RGBA", (24, 24), (0, 0, 0, 0))
    native_offset = ((24 - target[0]) // 2, (24 - target[1]) // 2)
    native.alpha_composite(reduced, native_offset)
    delivery = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    delivery.alpha_composite(native, (4, 4))
    native.save(args.output24)
    delivery.save(args.output32)

    native.save(args.qa_dir / f"{args.name}_24_1x.png")
    native.resize((144, 144), Image.Resampling.NEAREST).save(args.qa_dir / f"{args.name}_24_6x.png")
    delivery.save(args.qa_dir / f"{args.name}_ui32_1x.png")
    delivery.resize((128, 128), Image.Resampling.NEAREST).save(args.qa_dir / f"{args.name}_ui32_4x.png")
    delivery.convert("LA").convert("RGBA").resize((128, 128), Image.Resampling.NEAREST).save(
        args.qa_dir / f"{args.name}_ui32_grayscale_4x.png")
    checkerboard(delivery, 4).save(args.qa_dir / f"{args.name}_ui32_checker_4x.png")

    alpha24 = native.getchannel("A")
    alpha32 = delivery.getchannel("A")
    bounds24 = alpha24.getbbox()
    bounds32 = alpha32.getbbox()
    colors = {pixel[:3] for pixel in native.get_flattened_data() if pixel[3]}
    safe24 = bool(bounds24 and bounds24[0] > 0 and bounds24[1] > 0 and bounds24[2] < 24 and bounds24[3] < 24)
    exact_embed = delivery.crop((4, 4, 28, 28)).tobytes() == native.tobytes()
    report = {
        "status": "PASS" if safe24 and exact_embed and len(colors) <= args.colors else "FAIL",
        "name": args.name,
        "source": str(args.source),
        "sourceSha256": hashlib.sha256(args.source.read_bytes()).hexdigest(),
        "nativeCanvas": [24, 24],
        "nativeBounds": list(bounds24) if bounds24 else None,
        "deliveryCanvas": [32, 32],
        "deliveryBounds": list(bounds32) if bounds32 else None,
        "deliveryOffset": [4, 4],
        "deliveryUsesNoScale": exact_embed,
        "hardAlpha24": set(alpha24.get_flattened_data()).issubset({0, 255}),
        "hardAlpha32": set(alpha32.get_flattened_data()).issubset({0, 255}),
        "colors": len(colors),
        "transparentSafetyEdge24": safe24,
        "normalization": "alpha crop + whole-silhouette BOX reduction + hard alpha + palette limit + native 24px centering + unscaled 32px UI embed; no structural drawing",
    }
    (args.qa_dir / f"{args.name}_report.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    if report["status"] != "PASS":
        raise RuntimeError(json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()
