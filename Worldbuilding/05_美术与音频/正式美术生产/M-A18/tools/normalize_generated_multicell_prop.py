from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageOps


def main() -> None:
    parser = argparse.ArgumentParser(description="Normalize one transparent generated multi-cell prop without adding art structure.")
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--qa-dir", required=True, type=Path)
    parser.add_argument("--width", required=True, type=int)
    parser.add_argument("--height", required=True, type=int)
    parser.add_argument("--colors", type=int, default=10)
    parser.add_argument("--border", type=int, default=2)
    parser.add_argument("--anchor", choices=("center", "north"), default="center")
    parser.add_argument("--max-fitted-height", type=int, default=0)
    parser.add_argument("--preserve-cyan", action="store_true")
    args = parser.parse_args()

    source = Image.open(args.input).convert("RGBA")
    alpha = source.getchannel("A")
    bounds = alpha.getbbox()
    if bounds is None:
        raise ValueError("source has no visible pixels")
    cropped = source.crop(bounds)

    inner_width = args.width - args.border * 2
    inner_height = args.height - args.border * 2
    scale = min(inner_width / cropped.width, inner_height / cropped.height)
    fitted_size = (max(1, round(cropped.width * scale)), max(1, round(cropped.height * scale)))
    if args.max_fitted_height > 0 and fitted_size[1] > args.max_fitted_height:
        fitted_size = (fitted_size[0], args.max_fitted_height)
    fitted = cropped.resize(fitted_size, Image.Resampling.BOX)
    fitted_alpha = fitted.getchannel("A").point(lambda value: 255 if value >= 128 else 0)
    indexed = fitted.convert("RGB").quantize(
        colors=args.colors, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE
    ).convert("RGBA")
    indexed.putalpha(fitted_alpha)
    preserved_cyan = 0
    if args.preserve_cyan:
        source_pixels = list(cropped.getdata())
        cyan_pixels = [pixel for pixel in source_pixels if pixel[3] >= 128 and pixel[1] >= pixel[0] + 16 and pixel[2] >= pixel[0] + 16]
        if not cyan_pixels:
            raise ValueError("--preserve-cyan requested but source has no cyan-family pixels")
        cyan = tuple(sorted(pixel[channel] for pixel in cyan_pixels)[len(cyan_pixels) // 2] for channel in range(3)) + (255,)
        mask = Image.new("L", cropped.size)
        mask.putdata([255 if pixel[3] >= 128 and pixel[1] >= pixel[0] + 16 and pixel[2] >= pixel[0] + 16 else 0 for pixel in source_pixels])
        mask = mask.resize(fitted_size, Image.Resampling.BOX).point(lambda value: 255 if value > 0 else 0)
        target = indexed.load()
        mask_pixels = mask.load()
        alpha_pixels = fitted_alpha.load()
        for y in range(fitted.height):
            for x in range(fitted.width):
                if mask_pixels[x, y] and alpha_pixels[x, y]:
                    target[x, y] = cyan
                    preserved_cyan += 1

    output = Image.new("RGBA", (args.width, args.height), (0, 0, 0, 0))
    offset_y = 0 if args.anchor == "north" else (args.height - fitted.height) // 2
    offset = ((args.width - fitted.width) // 2, offset_y)
    output.alpha_composite(indexed, offset)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.qa_dir.mkdir(parents=True, exist_ok=True)
    output.save(args.output, optimize=True)

    asset_id = args.output.stem
    output.save(args.qa_dir / f"{asset_id}_1x.png", optimize=True)
    output.resize((args.width * 4, args.height * 4), Image.Resampling.NEAREST).save(
        args.qa_dir / f"{asset_id}_4x.png", optimize=True
    )
    ImageOps.grayscale(output).convert("RGBA").save(args.qa_dir / f"{asset_id}_grayscale.png", optimize=True)
    checker = Image.new("RGBA", output.size)
    checker.putdata([
        ((206, 206, 206, 255) if (x // 4 + y // 4) % 2 == 0 else (150, 150, 150, 255))
        for y in range(output.height) for x in range(output.width)
    ])
    checker.alpha_composite(output)
    checker.save(args.qa_dir / f"{asset_id}_checker.png", optimize=True)

    visible = [pixel for pixel in output.getdata() if pixel[3] > 0]
    report = {
        "schema": "occ-generated-multicell-normalization-v1",
        "asset_id": asset_id,
        "source_size": list(source.size),
        "source_alpha_bounds": list(bounds),
        "cropped_size": list(cropped.size),
        "delivery_size": list(output.size),
        "fitted_size": list(fitted.size),
        "fitted_offset": list(offset),
        "visible_colors": len(set(visible)),
        "alpha_values": sorted({pixel[3] for pixel in output.getdata()}),
        "source_sha256": hashlib.sha256(args.input.read_bytes()).hexdigest(),
        "output_sha256": hashlib.sha256(args.output.read_bytes()).hexdigest(),
        "normalization_only": True,
        "script_added_art_structure": False,
        "preserved_source_cyan_pixels": preserved_cyan,
        "anchor": args.anchor,
        "operations": ["alpha_bounds_crop", "box_fit", "median_cut_no_dither", "hard_alpha", "source_cyan_mask_preservation", "embed"],
    }
    (args.qa_dir / f"{asset_id}_report.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )


if __name__ == "__main__":
    main()
