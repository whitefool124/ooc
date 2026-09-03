"""Normalize one independent review asset to an application-derived pixel canvas.

The script performs alpha isolation, whole-silhouette reduction, palette
limiting, centering and QA only. It never draws or repairs asset structure.
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
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--qa-dir", required=True, type=Path)
    parser.add_argument("--name", required=True)
    parser.add_argument("--canvas-width", required=True, type=int)
    parser.add_argument("--canvas-height", required=True, type=int)
    parser.add_argument("--max-width", required=True, type=int)
    parser.add_argument("--max-height", required=True, type=int)
    parser.add_argument("--colors", required=True, type=int)
    parser.add_argument("--preserve-blue-colors", default=0, type=int)
    parser.add_argument("--logical-width", required=True, type=int)
    parser.add_argument("--logical-height", required=True, type=int)
    return parser.parse_args()


def isolate(image: Image.Image) -> Image.Image:
    rgba = image.convert("RGBA")
    rgba.putalpha(rgba.getchannel("A").point(lambda value: 255 if value >= 96 else 0))
    return rgba


def quantized_palette(pixels: list[tuple[int, int, int]], colors: int) -> list[tuple[int, int, int]]:
    if not pixels or colors <= 0:
        return []
    row = Image.new("RGB", (len(pixels), 1))
    row.putdata(pixels)
    quantized = row.quantize(colors=min(colors, len(set(pixels))), dither=Image.Dither.NONE).convert("RGB")
    return sorted(set(quantized.get_flattened_data()))


def nearest_color(pixel: tuple[int, int, int], palette: list[tuple[int, int, int]]) -> tuple[int, int, int]:
    return min(palette, key=lambda color: sum((pixel[index] - color[index]) ** 2 for index in range(3)))


def palette_limit(image: Image.Image, colors: int, preserve_blue_colors: int) -> Image.Image:
    rgba = image.convert("RGBA")
    opaque = [pixel for pixel in rgba.get_flattened_data() if pixel[3]]
    blue = [pixel[:3] for pixel in opaque if pixel[2] > pixel[0] * 1.15 and pixel[2] > pixel[1] * 1.08]
    neutral = [pixel[:3] for pixel in opaque if not (pixel[2] > pixel[0] * 1.15 and pixel[2] > pixel[1] * 1.08)]
    blue_budget = min(preserve_blue_colors, colors - 1) if blue else 0
    neutral_budget = colors - blue_budget
    blue_palette = quantized_palette(blue, blue_budget)
    neutral_palette = quantized_palette(neutral, neutral_budget)
    if not neutral_palette:
        neutral_palette = blue_palette

    mapped = []
    for red, green, blue_value, alpha in rgba.get_flattened_data():
        if not alpha:
            mapped.append((0, 0, 0, 0))
            continue
        original = (red, green, blue_value)
        is_blue = blue_value > red * 1.15 and blue_value > green * 1.08
        palette = blue_palette if is_blue and blue_palette else neutral_palette
        mapped.append((*nearest_color(original, palette), 255))
    result = Image.new("RGBA", rgba.size)
    result.putdata(mapped)
    return result


def checkerboard(image: Image.Image, scale: int) -> Image.Image:
    enlarged = image.resize((image.width * scale, image.height * scale), Image.Resampling.NEAREST)
    board = Image.new("RGBA", enlarged.size)
    draw = ImageDraw.Draw(board)
    block = 8
    colors = ((206, 198, 181, 255), (116, 109, 98, 255))
    for y in range(0, board.height, block):
        for x in range(0, board.width, block):
            draw.rectangle(
                (x, y, min(x + block - 1, board.width - 1), min(y + block - 1, board.height - 1)),
                fill=colors[((x // block) + (y // block)) % 2],
            )
    board.alpha_composite(enlarged)
    return board


def main() -> None:
    args = arguments()
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.qa_dir.mkdir(parents=True, exist_ok=True)

    isolated = isolate(Image.open(args.source))
    source_bounds = isolated.getchannel("A").getbbox()
    if not source_bounds:
        raise RuntimeError("No opaque object remains after alpha isolation")
    cropped = isolated.crop(source_bounds)
    scale = min(args.max_width / cropped.width, args.max_height / cropped.height)
    target = (max(1, round(cropped.width * scale)), max(1, round(cropped.height * scale)))
    reduced = cropped.resize(target, Image.Resampling.BOX)
    reduced.putalpha(reduced.getchannel("A").point(lambda value: 255 if value >= 96 else 0))
    reduced = palette_limit(reduced, args.colors, args.preserve_blue_colors)

    canvas = Image.new("RGBA", (args.canvas_width, args.canvas_height), (0, 0, 0, 0))
    offset = ((args.canvas_width - target[0]) // 2, (args.canvas_height - target[1]) // 2)
    canvas.alpha_composite(reduced, offset)
    canvas.save(args.output)

    canvas.save(args.qa_dir / f"{args.name}_1x.png")
    canvas.resize((args.canvas_width * 2, args.canvas_height * 2), Image.Resampling.NEAREST).save(
        args.qa_dir / f"{args.name}_2x.png")
    canvas.resize((args.canvas_width * 4, args.canvas_height * 4), Image.Resampling.NEAREST).save(
        args.qa_dir / f"{args.name}_4x.png")
    canvas.convert("LA").convert("RGBA").resize(
        (args.canvas_width * 4, args.canvas_height * 4), Image.Resampling.NEAREST
    ).save(args.qa_dir / f"{args.name}_grayscale_4x.png")
    checkerboard(canvas, 4).save(args.qa_dir / f"{args.name}_checker_4x.png")

    alpha = canvas.getchannel("A")
    bounds = alpha.getbbox()
    colors = {pixel[:3] for pixel in canvas.get_flattened_data() if pixel[3]}
    safe = bool(
        bounds
        and bounds[0] > 0
        and bounds[1] > 0
        and bounds[2] < args.canvas_width
        and bounds[3] < args.canvas_height
    )
    report = {
        "status": "PASS" if safe and len(colors) <= args.colors else "FAIL",
        "name": args.name,
        "source": str(args.source),
        "sourceSha256": hashlib.sha256(args.source.read_bytes()).hexdigest(),
        "applicationContract": {
            "logicalCells": [args.logical_width, args.logical_height],
            "runtimeDrawRect": "full logical footprint",
            "physicalScale1920x1080": 4,
            "physicalScale960x540": 2,
            "maxContent": [args.max_width, args.max_height],
        },
        "canvas": [args.canvas_width, args.canvas_height],
        "bounds": list(bounds) if bounds else None,
        "hardAlpha": set(alpha.get_flattened_data()).issubset({0, 255}),
        "colors": len(colors),
        "preservedBlueColors": args.preserve_blue_colors,
        "transparentSafetyEdge": safe,
        "normalization": "alpha crop + whole-silhouette BOX reduction + hard alpha + palette limit + centering; no structural drawing",
    }
    (args.qa_dir / f"{args.name}_report.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    if report["status"] != "PASS":
        raise RuntimeError(json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()
