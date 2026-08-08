#!/usr/bin/env python3
"""OCC M-A3 formal pixel-art templates and deterministic QA.

This tool never authors character motion. It creates production templates and
machine-checks independently authored/generated pixel assets before Unity import.
"""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
from typing import Iterable

from PIL import Image, ImageDraw


ART_BASE_03 = "ART-BASE-03"
NEAREST = Image.Resampling.NEAREST


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def visible_colors(image: Image.Image) -> list[tuple[int, int, int]]:
    rgba = image.convert("RGBA")
    return sorted({pixel[:3] for pixel in rgba.getdata() if pixel[3] > 0})


def alpha_values(image: Image.Image) -> set[int]:
    return {pixel[3] for pixel in image.convert("RGBA").getdata()}


def alpha_bbox(image: Image.Image):
    return image.convert("RGBA").getchannel("A").getbbox()


def checkerboard(size: tuple[int, int], cell: int = 4) -> Image.Image:
    result = Image.new("RGBA", size, (38, 42, 46, 255))
    draw = ImageDraw.Draw(result)
    for y in range(0, size[1], cell):
        for x in range(0, size[0], cell):
            if (x // cell + y // cell) % 2:
                draw.rectangle((x, y, x + cell - 1, y + cell - 1), fill=(67, 72, 76, 255))
    return result


def save_palette(colors: Iterable[tuple[int, int, int]], path: Path, scale: int = 12) -> None:
    colors = list(colors)
    width = max(1, min(8, len(colors)))
    height = max(1, (len(colors) + width - 1) // width)
    image = Image.new("RGBA", (width * scale, height * scale), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    for index, color in enumerate(colors):
        x = index % width * scale
        y = index // width * scale
        draw.rectangle((x, y, x + scale - 1, y + scale - 1), fill=(*color, 255))
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path)


def grayscale(image: Image.Image) -> Image.Image:
    rgba = image.convert("RGBA")
    gray = Image.new("RGBA", rgba.size)
    out = []
    for r, g, b, a in rgba.getdata():
        value = round(0.2126 * r + 0.7152 * g + 0.0722 * b)
        out.append((value, value, value, a))
    gray.putdata(out)
    return gray


def qa_asset(input_path: Path, output_dir: Path, spec: dict) -> dict:
    image = Image.open(input_path).convert("RGBA")
    expected = tuple(spec["size"])
    colors = visible_colors(image)
    alphas = alpha_values(image)
    bbox = alpha_bbox(image)
    failures = []
    if image.size != expected:
        failures.append(f"size:{image.size[0]}x{image.size[1]} expected:{expected[0]}x{expected[1]}")
    if not alphas.issubset({0, 255}):
        failures.append("alpha:not_hard")
    if len(colors) > int(spec["palette_limit"]):
        failures.append(f"palette:{len(colors)}>{spec['palette_limit']}")
    if spec.get("transparent_corners"):
        pixels = image.load()
        corners = [(0, 0), (image.width - 1, 0), (0, image.height - 1), (image.width - 1, image.height - 1)]
        if any(pixels[x, y][3] != 0 for x, y in corners):
            failures.append("alpha:corners_not_transparent")
    if bbox and spec.get("max_bottom_y") is not None and bbox[3] - 1 > int(spec["max_bottom_y"]):
        failures.append(f"anchor:bottom_y={bbox[3]-1}>{spec['max_bottom_y']}")

    output_dir.mkdir(parents=True, exist_ok=True)
    scale = int(spec.get("qa_scale", 4))
    board = checkerboard(image.size)
    board.alpha_composite(image)
    preview = board.resize((image.width * scale, image.height * scale), NEAREST)
    draw = ImageDraw.Draw(preview)
    if "center_x" in spec:
        x = int(spec["center_x"]) * scale
        draw.line((x, 0, x, preview.height - 1), fill=(82, 214, 255, 255), width=1)
    if "baseline_y" in spec:
        y = int(spec["baseline_y"]) * scale
        draw.line((0, y, preview.width - 1, y), fill=(240, 182, 58, 255), width=1)
    preview.save(output_dir / "qa_4x.png")
    grayscale(image).resize((image.width * scale, image.height * scale), NEAREST).save(output_dir / "grayscale_4x.png")
    save_palette(colors, output_dir / "palette.png")
    report = {
        "schema": "occ.pixel.qa.v0.1",
        "asset": input_path.name,
        "source": str(input_path),
        "sha256": sha256(input_path),
        "size": list(image.size),
        "mode": image.mode,
        "visible_color_count": len(colors),
        "alpha_values": sorted(alphas),
        "alpha_bbox": list(bbox) if bbox else None,
        "spec": spec,
        "failures": failures,
        "result": "PASS" if not failures else "FAIL",
    }
    (output_dir / "report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    return report


def guide_image(size: tuple[int, int], center_x=None, baseline_y=None, nine_slice=None) -> Image.Image:
    image = Image.new("RGBA", size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.rectangle((0, 0, size[0] - 1, size[1] - 1), outline=(120, 128, 134, 255))
    if center_x is not None:
        draw.line((center_x, 0, center_x, size[1] - 1), fill=(82, 214, 255, 255))
    if baseline_y is not None:
        draw.line((0, baseline_y, size[0] - 1, baseline_y), fill=(240, 182, 58, 255))
    if nine_slice:
        left, top, right, bottom = nine_slice
        draw.rectangle((left, top, size[0] - right - 1, size[1] - bottom - 1), outline=(231, 86, 66, 255))
    return image


def generate_templates(output_dir: Path) -> None:
    output_dir.mkdir(parents=True, exist_ok=True)
    specs = {
        "icon32": {"size": [32, 32], "palette_limit": 16, "transparent_corners": True, "qa_scale": 4},
        "tile32": {"size": [32, 32], "palette_limit": 16, "transparent_corners": False, "qa_scale": 4},
        "unit64": {"size": [64, 64], "palette_limit": 24, "transparent_corners": True, "center_x": 32, "baseline_y": 58, "max_bottom_y": 59, "standard_body_height": [32, 38], "elite_body_height": [38, 44], "qa_scale": 4},
        "unit_action_strip": {"cell": [64, 64], "center_x": 32, "baseline_y": 58, "frame_counts": {"idle": 4, "move": 6, "attack_or_cast": [6, 8], "hit": [3, 4], "defeat": [6, 8]}, "directions": ["left", "right"], "palette_limit": 24},
        "vfx32": {"cell": [32, 32], "frame_count": [4, 10], "palette_limit": 16, "pivot": [0.5, 0.5], "hard_alpha": True},
        "ui_9slice": {"size": [32, 32], "border": [4, 4, 4, 4], "palette_limit": 8, "style": "minimal industrial 1-2px lines"},
        "unity_import": {"texture_type": "Sprite", "filter_mode": "Point", "wrap_mode": "Clamp", "mipmap_enabled": False, "alpha_is_transparency": True, "pixels_per_unit": 32, "unit_pivot": [0.5, 0.09375]},
    }
    for key, spec in specs.items():
        (output_dir / f"{key}.json").write_text(json.dumps(spec, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    guide_image((32, 32)).save(output_dir / "icon32_template.png")
    guide_image((32, 32)).save(output_dir / "tile32_template.png")
    guide_image((64, 64), 32, 58).save(output_dir / "unit64_template.png")
    guide_image((32, 32), nine_slice=(4, 4, 4, 4)).save(output_dir / "ui_9slice_template.png")
    report = {
        "asset_id": ART_BASE_03,
        "result": "TEMPLATE_QA_PASS",
        "files": sorted(path.name for path in output_dir.iterdir()),
        "rules": ["32x32 tile/icon", "64x64 unit X=32 Y=58", "hard alpha", "nearest-neighbor", "Point/Clamp/no mipmap"],
    }
    (output_dir / "occ_art_base03_report_v01.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest="command", required=True)
    templates = sub.add_parser("templates")
    templates.add_argument("--out", type=Path, required=True)
    qa = sub.add_parser("qa")
    qa.add_argument("--input", type=Path, required=True)
    qa.add_argument("--spec", type=Path, required=True)
    qa.add_argument("--out", type=Path, required=True)
    args = parser.parse_args()
    if args.command == "templates":
        generate_templates(args.out)
    else:
        spec = json.loads(args.spec.read_text(encoding="utf-8"))
        report = qa_asset(args.input, args.out, spec)
        print(json.dumps(report, ensure_ascii=False))
        raise SystemExit(0 if report["result"] == "PASS" else 2)


if __name__ == "__main__":
    main()
