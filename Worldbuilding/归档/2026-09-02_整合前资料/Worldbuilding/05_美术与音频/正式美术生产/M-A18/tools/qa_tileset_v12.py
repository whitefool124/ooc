from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image


IDS = (
    "academy_courtyard_base_a",
    "academy_courtyard_base_b",
    "academy_aisle_base_a",
    "academy_aisle_edge_straight",
    "academy_aisle_edge_outer_corner",
    "academy_aisle_edge_inner_corner",
    "academy_earth_base_a",
    "academy_earth_edge_straight",
    "academy_earth_edge_outer_corner",
    "academy_earth_edge_inner_corner",
)
PROP_IDS = ("academy_seal_court_2x2",)


def rotated(image: Image.Image, quarter_turns_ccw: int) -> Image.Image:
    return image.rotate(90 * quarter_turns_ccw, resample=Image.Resampling.NEAREST, expand=False)


def edge(image: Image.Image, side: str) -> tuple[tuple[int, int, int, int], ...]:
    pixels = image.load()
    if side == "n":
        return tuple(pixels[x, 0] for x in range(image.width))
    if side == "e":
        return tuple(pixels[image.width - 1, y] for y in range(image.height))
    if side == "s":
        return tuple(pixels[x, image.height - 1] for x in range(image.width))
    if side == "w":
        return tuple(pixels[0, y] for y in range(image.height))
    raise KeyError(side)


def mismatch(first, second) -> int:
    return sum(a != b for a, b in zip(first, second))


def main() -> None:
    parser = argparse.ArgumentParser(description="Build the strict 12x9 Academy P0 contact map and tileset connection report.")
    parser.add_argument("--input-dir", required=True, type=Path)
    parser.add_argument("--qa-dir", required=True, type=Path)
    parser.add_argument("--unity-art-root", type=Path)
    args = parser.parse_args()
    args.qa_dir.mkdir(parents=True, exist_ok=True)

    tiles = {asset_id: Image.open(args.input_dir / f"{asset_id}.png").convert("RGBA") for asset_id in IDS}
    props = {asset_id: Image.open(args.input_dir / f"{asset_id}.png").convert("RGBA") for asset_id in PROP_IDS}
    for asset_id, image in tiles.items():
        if image.size != (32, 32):
            raise ValueError(f"{asset_id} is {image.size}, expected 32x32")
    for asset_id, image in props.items():
        if image.size != (64, 64):
            raise ValueError(f"{asset_id} is {image.size}, expected 64x64")

    base_a = tiles["academy_courtyard_base_a"]
    base_b = tiles["academy_courtyard_base_b"]
    earth = tiles["academy_earth_base_a"]
    board = Image.new("RGBA", (12 * 32, 9 * 32))
    for y in range(9):
        for x in range(12):
            board.paste(base_a, (x * 32, y * 32))

    aisle_base = tiles["academy_aisle_base_a"]
    aisle_edge = tiles["academy_aisle_edge_straight"]
    aisle_outer = tiles["academy_aisle_edge_outer_corner"]
    for x in range(4, 9):
        board.paste(aisle_edge, (x * 32, 3 * 32))
        board.paste(aisle_base, (x * 32, 4 * 32))
        board.paste(rotated(aisle_edge, 2), (x * 32, 5 * 32))
    board.paste(aisle_outer, (3 * 32, 3 * 32))
    board.paste(rotated(aisle_outer, -1), (9 * 32, 3 * 32))
    board.paste(rotated(aisle_edge, 1), (3 * 32, 4 * 32))
    board.paste(rotated(aisle_edge, -1), (9 * 32, 4 * 32))
    board.paste(rotated(aisle_outer, 1), (3 * 32, 5 * 32))
    board.paste(rotated(aisle_outer, 2), (9 * 32, 5 * 32))

    for y in range(7, 9):
        for x in range(4):
            board.paste(earth, (x * 32, y * 32))
    earth_edge = tiles["academy_earth_edge_straight"]
    for x in range(4):
        board.paste(earth_edge, (x * 32, 6 * 32))
    board.paste(rotated(tiles["academy_earth_edge_outer_corner"], -1), (4 * 32, 6 * 32))
    for y in range(7, 9):
        board.paste(rotated(earth_edge, -1), (4 * 32, y * 32))
    floor_board = board.copy()
    board.alpha_composite(props["academy_seal_court_2x2"], (6 * 32, 1 * 32))
    application_board = board.copy()
    if args.unity_art_root:
        academy = args.unity_art_root / "FormalAcademyCombat32"
        units = args.unity_art_root / "FormalUnits64"
        overlays = args.unity_art_root / "FormalTacticalOverlays32"
        placements = (
            (academy / "academy_light_stone_bench_intact.png", 1 * 32, 2 * 32),
            (academy / "academy_light_planter_intact.png", 10 * 32, 1 * 32),
            (academy / "academy_heavy_archive_stack_intact.png", 10 * 32, 7 * 32),
            (academy / "academy_aether_pillar_intact.png", 1 * 32, 7 * 32),
            (academy / "academy_loot_chest_closed.png", 5 * 32, 7 * 32),
            (overlays / "move_range.png", 5 * 32, 4 * 32),
        )
        for path, x, y in placements:
            if path.is_file():
                application_board.alpha_composite(Image.open(path).convert("RGBA"), (x, y))
        unit_placements = (
            (units / "hero.png", 5 * 32 - 16, 5 * 32 - 32),
            (units / "shieldguard.png", 8 * 32 - 16, 2 * 32 - 32),
            (units / "pyromancer.png", 9 * 32 - 16, 7 * 32 - 32),
        )
        for path, x, y in unit_placements:
            if path.is_file():
                application_board.alpha_composite(Image.open(path).convert("RGBA"), (x, y))

    contact_path = args.qa_dir / "academy_p0_contact_12x9.png"
    board.save(contact_path, optimize=True)
    application_board.save(args.qa_dir / "academy_p0_application_contact_12x9.png", optimize=True)
    for cell_size in (64, 96, 128, 160):
        board.resize((12 * cell_size, 9 * cell_size), Image.Resampling.NEAREST).save(
            args.qa_dir / f"academy_p0_contact_cell{cell_size}.png", optimize=True
        )

    gallery_ids = IDS + PROP_IDS
    gallery = Image.new("RGBA", (len(gallery_ids) * 128, 128), (51, 48, 43, 255))
    for index, asset_id in enumerate(gallery_ids):
        image = tiles.get(asset_id) or props[asset_id]
        gallery.alpha_composite(image.resize((128, 128), Image.Resampling.NEAREST), (index * 128, 0))
    gallery.save(args.qa_dir / "academy_p0_tileset_4x.png", optimize=True)

    horizontal_seam_mismatches = 0
    vertical_seam_mismatches = 0
    for y in range(floor_board.height):
        for x in range(32, floor_board.width, 32):
            if floor_board.getpixel((x - 1, y)) != floor_board.getpixel((x, y)):
                horizontal_seam_mismatches += 1
    for y in range(32, floor_board.height, 32):
        for x in range(floor_board.width):
            if floor_board.getpixel((x, y - 1)) != floor_board.getpixel((x, y)):
                vertical_seam_mismatches += 1
    checks = {
        "map_horizontal_tile_seams": horizontal_seam_mismatches,
        "map_vertical_tile_seams": vertical_seam_mismatches,
        "courtyard_a_to_b_n": mismatch(edge(base_a, "n"), edge(base_b, "n")),
        "courtyard_a_to_b_e": mismatch(edge(base_a, "e"), edge(base_b, "e")),
    }
    report = {
        "schema": "occ-academy-tileset-contact-v1",
        "status": "PASS" if all(value == 0 for value in checks.values()) else "FAIL",
        "board_size": list(board.size),
        "logical_map": [12, 9],
        "tile_size": [32, 32],
        "asset_count": len(tiles) + len(props),
        "connection_mismatches": checks,
        "contact_sha256": hashlib.sha256(contact_path.read_bytes()).hexdigest(),
    }
    (args.qa_dir / "academy_p0_tileset_report.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    print(json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()
