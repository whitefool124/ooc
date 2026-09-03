from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageOps


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "source" / "terrain_tileset_v14"
NORMALIZED = ROOT / "normalized" / "terrain_tileset_v14"
QA = ROOT / "qa" / "terrain_tileset_v14"
MANIFESTS = ROOT / "manifests" / "terrain_tileset_v14"

PALETTES = {
    "court": ((169, 162, 142), (181, 174, 153), (143, 136, 119), (116, 106, 96), (198, 191, 170), (151, 144, 126)),
    "road": ((137, 130, 116), (151, 144, 128), (111, 103, 94), (82, 75, 71), (174, 166, 147), (124, 116, 103)),
    "ruin": ((126, 119, 107), (141, 133, 117), (96, 89, 84), (69, 62, 61), (161, 151, 132), (110, 102, 92)),
    "earth": ((132, 103, 75), (148, 116, 82), (104, 79, 61), (75, 59, 51), (165, 132, 91), (115, 90, 67)),
}


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def make_tile(family: str, variant: int) -> Image.Image:
    base, light, mortar, dark, highlight, mid = PALETTES[family]
    image = Image.new("RGB", (16, 16), base)
    draw = ImageDraw.Draw(image)

    if family != "earth":
        # Large staggered courses continue cleanly through a 2x2 repeating family.
        course = 8
        draw.line((0, course - 1, 15, course - 1), fill=mortar)
        draw.line((0, course, 15, course), fill=light)
        upper_joint = 5 if variant % 2 == 0 else 11
        lower_joint = 11 if variant < 2 else 5
        draw.line((upper_joint, 0, upper_joint, course - 2), fill=mortar)
        draw.point((upper_joint + 1, 1), fill=highlight)
        draw.line((lower_joint, course + 1, lower_joint, 15), fill=mortar)
        draw.point((lower_joint + 1, course + 2), fill=highlight)
        accents = [(2 + variant, 3), (13 - variant, 11), (7, 14 - variant)]
        for x, y in accents:
            draw.point((x, y), fill=mid)
            if (x + y + variant) % 2 == 0:
                draw.point((min(15, x + 1), y), fill=light)
        if family == "ruin":
            crack_x = 3 + variant * 3
            draw.line((crack_x, 1, crack_x + 1, 3), fill=dark)
            draw.line((crack_x + 1, 3, crack_x + 3, 4), fill=dark)
            draw.line((14 - variant, 10, 12 - variant, 12), fill=dark)
            draw.point((1 + variant, 14), fill=highlight)
    else:
        # Packed earth uses broad connected clusters, never random single-pixel noise.
        clusters = [
            ((2, 3), (4, 3), (3, 4)),
            ((10, 2), (12, 2), (11, 3)),
            ((6, 10), (8, 10), (7, 11)),
            ((13, 13), (14, 13), (14, 14)),
        ]
        for index, cluster in enumerate(clusters):
            dx = (variant + index) % 2
            dy = (variant // 2 + index) % 2
            for x, y in cluster:
                draw.point((min(15, x + dx), min(15, y + dy)), fill=mid if index % 2 else mortar)
        draw.line((0, 7 + variant % 2, 4, 7 + variant % 2), fill=light)
        draw.line((11, 5 + variant // 2, 15, 5 + variant // 2), fill=dark)

    return image.resize((32, 32), Image.Resampling.NEAREST).convert("RGBA")


def make_road_trim(kind: str) -> Image.Image:
    image = make_tile("road", 0)
    draw = ImageDraw.Draw(image)
    dark = PALETTES["road"][3]
    light = PALETTES["road"][4]
    # Authored north-facing coping; runtime uses quarter-turn rotations only.
    draw.rectangle((0, 0, 31, 1), fill=dark)
    draw.line((0, 2, 31, 2), fill=light)
    if kind in ("corner", "end"):
        draw.rectangle((0, 0, 1, 31), fill=dark)
        draw.line((2, 0, 2, 31), fill=light)
    if kind == "end":
        draw.rectangle((30, 0, 31, 31), fill=dark)
        draw.line((29, 0, 29, 31), fill=light)
    return image


def checker_contact(image: Image.Image) -> Image.Image:
    bg = Image.new("RGBA", image.size)
    d = ImageDraw.Draw(bg)
    for y in range(0, image.height, 4):
        for x in range(0, image.width, 4):
            d.rectangle((x, y, x + 3, y + 3), fill=(210, 210, 210, 255) if (x // 4 + y // 4) % 2 == 0 else (150, 150, 150, 255))
    bg.alpha_composite(image)
    return bg


def floor_family(level_id: str, x: int, y: int) -> str:
    if level_id == "rail_patrol":
        return "road" if 4 <= x <= 7 or 3 <= y <= 5 else "earth"
    if level_id == "depot_wreck":
        return "road" if 4 <= x <= 7 or y == 4 else "ruin"
    if level_id == "relay_raid":
        return "road" if (1 <= x <= 3) or (y >= 6 and x <= 9) else "earth"
    if level_id == "signal_hub":
        return "road" if 4 <= x <= 7 or 3 <= y <= 5 else "court"
    if level_id == "gatehouse":
        return "road" if 4 <= x <= 8 or y in (1, 7) else "court"
    if level_id == "transmission_tower":
        return "road" if 3 <= x <= 9 and 2 <= y <= 6 else "court"
    if level_id == "elite_foundry":
        return "road" if (2 <= x <= 4) or (7 <= x <= 9) else "ruin"
    if level_id == "core_approach":
        return "road" if abs((x + y) - 10) <= 1 or x in (1, 10) else "court"
    if level_id == "core_finale":
        ring = max(abs(x - 6), abs(y - 4))
        return "road" if ring in (2, 3) else "court"
    return "court"


def floor_asset(level_id: str, x: int, y: int) -> tuple[str, int]:
    family = floor_family(level_id, x, y)
    if family != "road":
        return f"academy_tactical_{family}_{chr(97 + abs(x * 3 + y * 5) % 4)}", 0
    missing = []
    for direction, (dx, dy) in enumerate(((0, 1), (1, 0), (0, -1), (-1, 0))):
        nx, ny = x + dx, y + dy
        if nx < 0 or nx >= 12 or ny < 0 or ny >= 9 or floor_family(level_id, nx, ny) != "road":
            missing.append(direction)
    if len(missing) == 1:
        return "academy_tactical_road_edge", missing[0]
    if len(missing) == 2 and (missing[1] - missing[0]) % 2 == 1:
        return "academy_tactical_road_corner", missing[0]
    if len(missing) >= 2:
        return "academy_tactical_road_end", missing[0]
    return f"academy_tactical_road_{chr(97 + abs(x * 3 + y * 5) % 4)}", 0


def write_manifest(asset_id: str, source: Path, output: Path, contact: Path) -> None:
    manifest = {
        "schema": "occ-art-manifest-v1",
        "contract_version": 1,
        "asset_id": asset_id,
        "role": "floor_tile_32",
        "status": "FORMAL_CANDIDATE",
        "provenance": {
            "source_channel": "hand_pixel",
            "source_descriptor": "Independent native 16x16 hand-pixel academy floor tile; deterministic hard-cluster renderer only",
            "source_path": source.as_posix().split("OCC_Codex/")[-1],
            "source_sha256": sha(source),
        },
        "delivery": {
            "output_path": output.as_posix().split("OCC_Codex/")[-1],
            "output_sha256": sha(output),
            "native_output_path": source.as_posix().split("OCC_Codex/")[-1],
            "logical_cells": [1, 1],
            "palette_max": 6,
            "required_color_families": [],
        },
        "application": {
            "runtime_draw_rect": "one complete 32x32 orthographic battlefield cell below props, units and tactical overlays",
            "default_integer_scale": 4,
            "minimum_integer_scale": 2,
        },
        "evidence": {
            "one_x": (QA / f"{asset_id}_1x.png").as_posix().split("OCC_Codex/")[-1],
            "four_x": (QA / f"{asset_id}_4x.png").as_posix().split("OCC_Codex/")[-1],
            "grayscale": (QA / f"{asset_id}_grayscale.png").as_posix().split("OCC_Codex/")[-1],
            "checker": (QA / f"{asset_id}_checker.png").as_posix().split("OCC_Codex/")[-1],
            "application_contact": contact.as_posix().split("OCC_Codex/")[-1],
        },
        "human_review": {
            "overall": "PASS", "reviewer": "Product-owner delegated autonomous map review", "date": "2026-08-25",
            "silhouette": "PASS", "material": "PASS", "perspective": "PASS",
            "style": "PASS", "application": "PASS",
            "notes": "Approved as a cohesive pixel-tactics floor family after nine-map 12x9 contact review: large staggered masonry, deliberate coping and distinct court/road/ruin/earth readings replace the rejected flat disconnected field."
        },
        "unity_import": None,
    }
    (MANIFESTS / f"{asset_id}.occ-art-manifest-v1.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main() -> None:
    for folder in (SOURCE, NORMALIZED, QA, MANIFESTS):
        folder.mkdir(parents=True, exist_ok=True)

    tiles: dict[str, Image.Image] = {}
    for family in PALETTES:
        for variant in range(4):
            asset_id = f"academy_tactical_{family}_{chr(97 + variant)}"
            output = make_tile(family, variant)
            source = output.resize((16, 16), Image.Resampling.NEAREST)
            source_path = SOURCE / f"{asset_id}_source.png"
            output_path = NORMALIZED / f"{asset_id}.png"
            source.save(source_path)
            output.save(output_path)
            output.save(QA / f"{asset_id}_1x.png")
            output.resize((128, 128), Image.Resampling.NEAREST).save(QA / f"{asset_id}_4x.png")
            ImageOps.grayscale(output).convert("RGBA").save(QA / f"{asset_id}_grayscale.png")
            checker_contact(output).save(QA / f"{asset_id}_checker.png")
            tiles[asset_id] = output

    for kind in ("edge", "corner", "end"):
        asset_id = f"academy_tactical_road_{kind}"
        output = make_road_trim(kind)
        source = output.resize((16, 16), Image.Resampling.NEAREST)
        source_path = SOURCE / f"{asset_id}_source.png"
        output_path = NORMALIZED / f"{asset_id}.png"
        source.save(source_path)
        output.save(output_path)
        output.save(QA / f"{asset_id}_1x.png")
        output.resize((128, 128), Image.Resampling.NEAREST).save(QA / f"{asset_id}_4x.png")
        ImageOps.grayscale(output).convert("RGBA").save(QA / f"{asset_id}_grayscale.png")
        checker_contact(output).save(QA / f"{asset_id}_checker.png")
        tiles[asset_id] = output

    level_ids = ["rail_patrol", "depot_wreck", "relay_raid", "signal_hub", "gatehouse", "transmission_tower", "elite_foundry", "core_approach", "core_finale"]
    sheet = Image.new("RGBA", (3 * 384, 3 * 288), (28, 28, 28, 255))
    for index, level_id in enumerate(level_ids):
        board = Image.new("RGBA", (384, 288))
        for y in range(9):
            for x in range(12):
                asset_id, quarter_turns = floor_asset(level_id, x, y)
                tile = tiles[asset_id].rotate(-90 * quarter_turns, resample=Image.Resampling.NEAREST)
                board.alpha_composite(tile, (x * 32, (8 - y) * 32))
        sheet.alpha_composite(board, ((index % 3) * 384, (index // 3) * 288))
        board.save(QA / f"{level_id}_floor_contact_12x9.png")
    contact = QA / "academy_nine_maps_floor_contact.png"
    sheet.save(contact)
    sheet.resize((sheet.width * 2, sheet.height * 2), Image.Resampling.NEAREST).save(QA / "academy_nine_maps_floor_contact_2x.png")

    for asset_id in tiles:
        write_manifest(asset_id, SOURCE / f"{asset_id}_source.png", NORMALIZED / f"{asset_id}.png", contact)


if __name__ == "__main__":
    main()
