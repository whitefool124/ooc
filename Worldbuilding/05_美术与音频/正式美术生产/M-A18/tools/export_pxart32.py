from __future__ import annotations

import argparse
import json
from pathlib import Path

from PIL import Image


PALETTES = {
    "limestone": {
        "0": (96, 91, 80, 255), "1": (117, 111, 98, 255),
        "2": (143, 137, 122, 255), "3": (160, 153, 136, 255),
        "4": (178, 169, 149, 255), "5": (148, 127, 96, 255),
    },
    "archive_warm": {
        "0": (88, 80, 67, 255), "1": (112, 101, 83, 255),
        "2": (145, 132, 108, 255), "3": (166, 151, 123, 255),
        "4": (188, 173, 143, 255), "5": (132, 106, 77, 255),
    },
    "old_slate": {
        "0": (68, 69, 65, 255), "1": (91, 92, 86, 255),
        "2": (116, 116, 106, 255), "3": (137, 136, 122, 255),
        "4": (158, 155, 136, 255), "5": (119, 99, 75, 255),
    },
    "academy_clean": {
        "0": (104, 101, 94, 255), "1": (124, 120, 110, 255),
        "2": (143, 137, 122, 255), "3": (154, 148, 132, 255),
        "4": (165, 158, 140, 255), "5": (142, 126, 103, 255),
    },
    "overlay_move": {"x": (0, 0, 0, 0), "1": (3, 143, 169, 255)},
    "overlay_attack": {"x": (0, 0, 0, 0), "1": (144, 49, 43, 255)},
    "overlay_neutral": {"x": (0, 0, 0, 0), "1": (240, 240, 232, 255)},
}


def main() -> None:
    parser = argparse.ArgumentParser(description="Export an explicitly authored OCC pixel-art text grid.")
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--qa-dir", required=True, type=Path)
    parser.add_argument("--palette", choices=sorted(PALETTES), default="limestone")
    args = parser.parse_args()
    palette = PALETTES[args.palette]

    rows = [line.replace(" ", "").strip() for line in args.input.read_text(encoding="utf-8").splitlines() if line.strip() and not line.startswith("#")]
    logical_size = len(rows)
    if logical_size not in (16, 24, 32) or any(len(row) != logical_size for row in rows):
        raise ValueError(f"Expected a 16x16, 24x24, or 32x32 logical grid, got row lengths: {[len(row) for row in rows]}")
    invalid = sorted(set("".join(rows)) - set(palette))
    if invalid:
        raise ValueError(f"Invalid palette keys: {invalid}")

    logical = Image.new("RGBA", (logical_size, logical_size))
    logical.putdata([palette[key] for row in rows for key in row])
    delivery_size = 32 if logical_size == 16 else logical_size
    image = logical.resize((delivery_size, delivery_size), Image.Resampling.NEAREST)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.qa_dir.mkdir(parents=True, exist_ok=True)
    image.save(args.output)
    image.resize((delivery_size * 4, delivery_size * 4), Image.Resampling.NEAREST).save(args.qa_dir / f"{args.output.stem}_4x.png")

    repeated = Image.new("RGBA", (8 * delivery_size, 6 * delivery_size))
    for y in range(6):
        for x in range(8):
            repeated.paste(image, (x * delivery_size, y * delivery_size))
    repeated.resize((16 * delivery_size, 12 * delivery_size), Image.Resampling.NEAREST).save(args.qa_dir / f"{args.output.stem}_repeat_2x.png")

    pixels = list(image.getdata())
    report = {
        "logical_size": [logical_size, logical_size],
        "delivery_size": [delivery_size, delivery_size],
        "nearest_scale": delivery_size // logical_size,
        "alpha_values": sorted({pixel[3] for pixel in pixels}),
        "colour_count": len(set(pixels)),
        "palette_name": args.palette,
        "palette": {key: list(value) for key, value in palette.items()},
        "authored_source": str(args.input),
        "export_only": True,
    }
    (args.qa_dir / f"{args.output.stem}_report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
