from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image


def expand_row(encoded: str, width: int) -> list[str]:
    values: list[str] = []
    for token in encoded.split(","):
        key, count = token.split(":", 1)
        values.extend([key] * int(count))
    if len(values) != width:
        raise ValueError(f"row expands to {len(values)} pixels, expected {width}: {encoded}")
    return values


def expand_rows(spec: dict) -> list[list[str]]:
    rows: list[list[str]] = []
    for entry in spec["rows"]:
        if isinstance(entry, str):
            encoded, repeat = entry, 1
        else:
            encoded, repeat = entry["row"], int(entry.get("repeat", 1))
        row = expand_row(encoded, int(spec["width"]))
        rows.extend([row.copy() for _ in range(repeat)])
    if len(rows) != int(spec["height"]):
        raise ValueError(f"source expands to {len(rows)} rows, expected {spec['height']}")
    return rows


def rgba(value: str) -> tuple[int, int, int, int]:
    value = value.removeprefix("#")
    if len(value) == 6:
        value += "FF"
    if len(value) != 8:
        raise ValueError(f"expected RRGGBB or RRGGBBAA, got {value}")
    return tuple(int(value[index:index + 2], 16) for index in range(0, 8, 2))


def checker_underlay(size: tuple[int, int]) -> Image.Image:
    image = Image.new("RGBA", size)
    pixels = []
    for y in range(size[1]):
        for x in range(size[0]):
            shade = 206 if (x // 4 + y // 4) % 2 == 0 else 150
            pixels.append((shade, shade, shade, 255))
    image.putdata(pixels)
    return image


def main() -> None:
    parser = argparse.ArgumentParser(description="Render an explicitly authored RLE pixel matrix without adding art structure.")
    parser.add_argument("--spec", required=True, type=Path)
    parser.add_argument("--source-png", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--qa-dir", required=True, type=Path)
    args = parser.parse_args()

    spec = json.loads(args.spec.read_text(encoding="utf-8"))
    width, height = int(spec["width"]), int(spec["height"])
    palette = {key: rgba(value) for key, value in spec["palette"].items()}
    rows = expand_rows(spec)
    source = Image.new("RGBA", (width, height))
    source.putdata([palette[key] for row in rows for key in row])

    delivery_size = tuple(spec.get("delivery_size", [width, height]))
    delivery = source.resize(delivery_size, Image.Resampling.NEAREST)
    args.source_png.parent.mkdir(parents=True, exist_ok=True)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.qa_dir.mkdir(parents=True, exist_ok=True)
    source.save(args.source_png, optimize=True)
    delivery.save(args.output, optimize=True)

    asset_id = str(spec["asset_id"])
    delivery.save(args.qa_dir / f"{asset_id}_1x.png", optimize=True)
    delivery.resize((delivery.width * 4, delivery.height * 4), Image.Resampling.NEAREST).save(
        args.qa_dir / f"{asset_id}_4x.png", optimize=True
    )
    grayscale = delivery.convert("L").convert("RGBA")
    grayscale.save(args.qa_dir / f"{asset_id}_grayscale.png", optimize=True)
    checker = checker_underlay(delivery.size)
    checker.alpha_composite(delivery)
    checker.save(args.qa_dir / f"{asset_id}_checker.png", optimize=True)

    pixels = list(delivery.getdata())
    report = {
        "schema": "occ-hand-pixel-render-v1",
        "asset_id": asset_id,
        "source_spec": args.spec.as_posix(),
        "source_size": list(source.size),
        "delivery_size": list(delivery.size),
        "palette": [list(pixel) for pixel in sorted(set(pixels))],
        "colour_count": len(set(pixels)),
        "alpha_values": sorted({pixel[3] for pixel in pixels}),
        "source_sha256": hashlib.sha256(args.source_png.read_bytes()).hexdigest(),
        "output_sha256": hashlib.sha256(args.output.read_bytes()).hexdigest(),
        "renderer_added_art_structure": False,
        "authorship": "explicit per-pixel RLE rows stored in the source spec",
    }
    (args.qa_dir / f"{asset_id}_render_report.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )


if __name__ == "__main__":
    main()
