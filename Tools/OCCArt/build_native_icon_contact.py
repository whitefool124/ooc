#!/usr/bin/env python3
"""Build an unscaled contact sheet from independently generated icon sources."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image, ImageDraw


def arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--columns", type=int, default=4)
    parser.add_argument("images", nargs="+", type=Path)
    return parser.parse_args()


def main() -> None:
    args = arguments()
    images = [Image.open(path).convert("RGBA") for path in args.images]
    cell_width = max(image.width for image in images)
    cell_height = max(image.height for image in images)
    margin = 48
    gap = 32
    rows = (len(images) + args.columns - 1) // args.columns
    width = margin * 2 + cell_width * args.columns + gap * (args.columns - 1)
    height = margin * 2 + cell_height * rows + gap * (rows - 1)
    board = Image.new("RGBA", (width, height), (18, 22, 22, 255))
    draw = ImageDraw.Draw(board)
    for index, icon in enumerate(images):
        column = index % args.columns
        row = index // args.columns
        x = margin + column * (cell_width + gap)
        y = margin + row * (cell_height + gap)
        draw.rounded_rectangle(
            (x, y, x + cell_width - 1, y + cell_height - 1),
            radius=24,
            fill=(35, 40, 38, 255),
            outline=(86, 80, 67, 255),
            width=4,
        )
        board.alpha_composite(icon, (x + (cell_width - icon.width) // 2, y + (cell_height - icon.height) // 2))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    board.save(args.output)
    print(f"saved {args.output} at {board.width}x{board.height}; source pixels were not resized")


if __name__ == "__main__":
    main()
