#!/usr/bin/env python3
"""Build a labeled integer-scale contact sheet for native 32 PPU assets."""

from __future__ import annotations

import argparse
import math
from pathlib import Path

from PIL import Image, ImageDraw


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--ground", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--scale", type=int, default=4)
    parser.add_argument("assets", nargs="+", help="LABEL=PATH")
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    entries: list[tuple[str, Path]] = []
    for value in args.assets:
        label, separator, raw_path = value.partition("=")
        if not separator:
            raise SystemExit(f"asset must be LABEL=PATH: {value}")
        entries.append((label, Path(raw_path)))

    columns = min(3, len(entries))
    rows = math.ceil(len(entries) / columns)
    panel_width, panel_height, label_height = 160, 128, 16
    native = Image.new("RGBA", (columns * panel_width, rows * panel_height), (20, 23, 27, 255))
    draw = ImageDraw.Draw(native)
    ground = Image.open(args.ground).convert("RGBA")
    if ground.size != (32, 32):
        raise SystemExit(f"ground must be 32x32, got {ground.size}")

    for index, (label, path) in enumerate(entries):
        column, row = index % columns, index // columns
        left, top = column * panel_width, row * panel_height
        for y in range(top + label_height, top + panel_height, 32):
            for x in range(left, left + panel_width, 32):
                native.alpha_composite(ground, (x, y))

        sprite = Image.open(path).convert("RGBA")
        anchor_x = left + panel_width // 2
        baseline_y = top + 112
        native.alpha_composite(sprite, (anchor_x - sprite.width // 2, baseline_y - sprite.height))
        draw.rectangle((anchor_x - 16, baseline_y - 32, anchor_x + 15, baseline_y - 1), outline=(209, 167, 68, 255))
        draw.text((left + 3, top + 3), label, fill=(235, 225, 199, 255))

    if args.scale < 1:
        raise SystemExit("scale must be positive")
    result = native.resize(
        (native.width * args.scale, native.height * args.scale),
        Image.Resampling.NEAREST,
    )
    args.output.parent.mkdir(parents=True, exist_ok=True)
    result.save(args.output)
    print(f"saved {args.output} size={result.size} assets={len(entries)} scale={args.scale}x")


if __name__ == "__main__":
    main()
