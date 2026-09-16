#!/usr/bin/env python3
"""Normalize an independently generated unit source to the OCC 64px unit tier."""
from argparse import ArgumentParser
from pathlib import Path
from PIL import Image


def normalize(source: Path, output: Path, body_w: int, body_h: int, colors: int) -> None:
    image = Image.open(source).convert("RGBA")
    hard_alpha = image.getchannel("A").point(lambda value: 255 if value >= 128 else 0)
    bbox = hard_alpha.getbbox()
    if bbox is None:
        raise ValueError(f"no readable silhouette in {source}")
    crop = image.crop(bbox)
    rgb = crop.convert("RGB").quantize(colors=colors, method=Image.Quantize.FASTOCTREE).convert("RGB")
    fitted = rgb.resize((body_w, body_h), Image.Resampling.NEAREST).convert("RGBA")
    fitted.putalpha(hard_alpha.crop(bbox).resize((body_w, body_h), Image.Resampling.NEAREST))
    canvas = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    canvas.alpha_composite(fitted, ((64 - body_w) // 2, 55 - body_h))
    output.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(output)


def main() -> None:
    parser = ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--body-width", type=int, default=44)
    parser.add_argument("--body-height", type=int, default=48)
    parser.add_argument("--colors", type=int, default=10)
    args = parser.parse_args()
    normalize(args.source, args.output, args.body_width, args.body_height, args.colors)
    print(args.output)


if __name__ == "__main__":
    main()
