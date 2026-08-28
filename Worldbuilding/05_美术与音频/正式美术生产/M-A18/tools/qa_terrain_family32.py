from __future__ import annotations

import argparse
import hashlib
import json
import random
from pathlib import Path

from PIL import Image


def luma(pixel: tuple[int, int, int, int]) -> float:
    r, g, b, _ = pixel
    return 0.2126 * r + 0.7152 * g + 0.0722 * b


def exact_two_by_two_blocks(image: Image.Image) -> bool:
    if image.size != (32, 32):
        return False
    pixels = image.load()
    for y in range(0, 32, 2):
        for x in range(0, 32, 2):
            block = {pixels[x + ox, y + oy] for oy in (0, 1) for ox in (0, 1)}
            if len(block) != 1:
                return False
    return True


def isolated_pixel_count(image: Image.Image) -> int:
    pixels = image.load()
    isolated = 0
    for y in range(1, image.height - 1):
        for x in range(1, image.width - 1):
            value = pixels[x, y]
            neighbours = (pixels[x - 1, y], pixels[x + 1, y], pixels[x, y - 1], pixels[x, y + 1])
            if all(neighbour != value for neighbour in neighbours):
                isolated += 1
    return isolated


def main() -> None:
    parser = argparse.ArgumentParser(description="Create contact and mixed-map QA for four same-size square terrain variants.")
    parser.add_argument("--input-dir", required=True, type=Path)
    parser.add_argument("--prefix", required=True)
    parser.add_argument("--qa-dir", required=True, type=Path)
    args = parser.parse_args()
    args.qa_dir.mkdir(parents=True, exist_ok=True)

    images = {}
    report = {"tiles": {}}
    for variant in "abcd":
        path = args.input_dir / f"{args.prefix}_{variant}.png"
        image = Image.open(path).convert("RGBA")
        if image.width != image.height:
            raise ValueError(f"Expected square terrain tile, got {image.size}: {path}")
        if images and image.size != next(iter(images.values())).size:
            raise ValueError(f"Terrain family size mismatch: {image.size}: {path}")
        pixels = list(image.getdata())
        surface_inset = 3
        surface_pixels = [
            image.getpixel((x, y))
            for y in range(0, image.height - surface_inset)
            for x in range(0, image.width - surface_inset)
        ]
        images[variant] = image
        report["tiles"][variant] = {
            "size": list(image.size),
            "alpha_values": sorted({pixel[3] for pixel in pixels}),
            "colour_count": len(set(pixels)),
            "clean_surface_colour_count": len(set(surface_pixels)),
            "isolated_pixel_count": isolated_pixel_count(image),
            "top_edge_nonbase_count": sum(image.getpixel((x, 0)) != image.getpixel((0, 0)) for x in range(image.width)),
            "left_edge_nonbase_count": sum(image.getpixel((0, y)) != image.getpixel((0, 0)) for y in range(image.height)),
            "exact_16x16_nearest_2x": exact_two_by_two_blocks(image),
            "mean_luma": round(sum(luma(pixel) for pixel in pixels) / len(pixels), 2),
            "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
        }

    tile_size = next(iter(images.values())).width
    contact = Image.new("RGBA", (4 * tile_size, tile_size))
    for index, variant in enumerate("abcd"):
        contact.paste(images[variant], (index * tile_size, 0))
    contact.resize((16 * tile_size, 4 * tile_size), Image.Resampling.NEAREST).save(args.qa_dir / f"{args.prefix}_abcd_contact_4x.png")

    rng = random.Random(20260820)
    board = Image.new("RGBA", (12 * tile_size, 9 * tile_size))
    for y in range(9):
        for x in range(12):
            variant = "abcd"[rng.randrange(4)]
            board.paste(images[variant], (x * tile_size, y * tile_size))
    board.resize((24 * tile_size, 18 * tile_size), Image.Resampling.NEAREST).save(args.qa_dir / f"{args.prefix}_mixed_12x9_2x.png")
    if tile_size == 32:
        report["integer_cell_scale_previews"] = {}
        for cell_size in (64, 96, 128, 160):
            ratio = cell_size // tile_size
            filename = f"{args.prefix}_mixed_cell{cell_size}_{ratio}x.png"
            board.resize((12 * cell_size, 9 * cell_size), Image.Resampling.NEAREST).save(args.qa_dir / filename)
            report["integer_cell_scale_previews"][str(cell_size)] = {"ratio": ratio, "file": filename}

    means = [tile["mean_luma"] for tile in report["tiles"].values()]
    report["family_mean_luma_delta"] = round(max(means) - min(means), 2)
    report["unique_hash_count"] = len({tile["sha256"] for tile in report["tiles"].values()})
    edge_matrix = {}
    for left_name, left_image in images.items():
        for right_name, right_image in images.items():
            horizontal_mismatches = sum(
                left_image.getpixel((tile_size - 1, y)) != right_image.getpixel((0, y))
                for y in range(tile_size)
            )
            vertical_mismatches = sum(
                left_image.getpixel((x, tile_size - 1)) != right_image.getpixel((x, 0))
                for x in range(tile_size)
            )
            edge_matrix[f"{left_name}->{right_name}"] = {
                "horizontal_mismatches": horizontal_mismatches,
                "vertical_mismatches": vertical_mismatches,
            }
    report["edge_compatibility"] = edge_matrix
    report["all_variant_edges_exact"] = all(
        value["horizontal_mismatches"] == 0 and value["vertical_mismatches"] == 0
        for value in edge_matrix.values()
    )
    (args.qa_dir / f"{args.prefix}_family_report.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8"
    )


if __name__ == "__main__":
    main()
