"""Pure normalization and QA for independently sourced M-A18 battlefield props.

This tool does not draw, repair, or assemble art. It removes a declared chroma
background, scales the complete isolated silhouette into an application-derived
content box, limits the palette, and emits review evidence.
"""

import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw


def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--qa-dir", required=True)
    parser.add_argument("--name", required=True)
    parser.add_argument("--max-width", type=int, required=True)
    parser.add_argument("--max-height", type=int, required=True)
    parser.add_argument("--baseline", type=int, default=28)
    parser.add_argument("--colors", type=int, default=10)
    return parser.parse_args()


def remove_green_or_keep_alpha(image):
    rgba = image.convert("RGBA")
    pixels = []
    for r, g, b, a in rgba.get_flattened_data():
        chroma = g > 100 and g > r * 1.18 and g > b * 1.18 and g - max(r, b) > 28
        pixels.append((r, g, b, 0 if chroma else (255 if a > 32 else 0)))
    rgba.putdata(pixels)
    return rgba


def quantize_rgba(image, colors):
    alpha = image.getchannel("A").point(lambda value: 255 if value > 0 else 0)
    rgb = Image.new("RGB", image.size)
    rgb.paste(image.convert("RGB"), mask=alpha)
    result = rgb.quantize(colors=colors, dither=Image.Dither.NONE).convert("RGBA")
    result.putalpha(alpha)
    return result


def checkerboard(image, scale=4):
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


def main():
    args = parse_args()
    source_path = Path(args.source)
    output_path = Path(args.output)
    qa_dir = Path(args.qa_dir)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    qa_dir.mkdir(parents=True, exist_ok=True)

    isolated = remove_green_or_keep_alpha(Image.open(source_path))
    bounds = isolated.getchannel("A").getbbox()
    if not bounds:
        raise RuntimeError("No isolated object remains after alpha/chroma cleanup")
    cropped = isolated.crop(bounds)
    scale = min(args.max_width / cropped.width, args.max_height / cropped.height)
    target_width = max(1, round(cropped.width * scale))
    target_height = max(1, round(cropped.height * scale))
    resized = cropped.resize((target_width, target_height), Image.Resampling.NEAREST)

    canvas = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    x = (32 - target_width) // 2
    y = args.baseline - target_height
    canvas.alpha_composite(resized, (x, y))
    canvas = quantize_rgba(canvas, args.colors)
    canvas.save(output_path)

    one_x = qa_dir / f"{args.name}_1x.png"
    four_x = qa_dir / f"{args.name}_4x.png"
    gray = qa_dir / f"{args.name}_grayscale_4x.png"
    check = qa_dir / f"{args.name}_checker_4x.png"
    canvas.save(one_x)
    canvas.resize((128, 128), Image.Resampling.NEAREST).save(four_x)
    canvas.convert("LA").convert("RGBA").resize((128, 128), Image.Resampling.NEAREST).save(gray)
    checkerboard(canvas).save(check)

    alpha = canvas.getchannel("A")
    final_bounds = alpha.getbbox()
    used_colors = sorted({pixel[:3] for pixel in canvas.get_flattened_data() if pixel[3]})
    touches_edge = final_bounds is None or final_bounds[0] <= 0 or final_bounds[1] <= 0 or final_bounds[2] >= 32 or final_bounds[3] >= 32
    report = {
        "status": "PASS" if not touches_edge and len(used_colors) <= args.colors else "FAIL",
        "name": args.name,
        "source": str(source_path),
        "sourceSha256": hashlib.sha256(source_path.read_bytes()).hexdigest(),
        "applicationContract": {
            "logicalCells": [1, 1],
            "runtimeDrawRect": "full cell",
            "physicalScale1920x1080": 4,
            "physicalScale960x540": 2,
            "maxContent": [args.max_width, args.max_height],
            "baselineY": args.baseline,
        },
        "canvas": [32, 32],
        "bounds": list(final_bounds) if final_bounds else None,
        "hardAlpha": set(alpha.get_flattened_data()).issubset({0, 255}),
        "colors": len(used_colors),
        "transparentSafetyEdge": not touches_edge,
        "normalization": "chroma/alpha isolation + whole-silhouette nearest resize + palette limit; no structural drawing",
    }
    (qa_dir / f"{args.name}_report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    if report["status"] != "PASS":
        raise RuntimeError(json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()
