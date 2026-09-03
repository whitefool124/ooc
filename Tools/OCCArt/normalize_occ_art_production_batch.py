#!/usr/bin/env python3
"""Mechanically normalize an OCC production batch and build QA evidence.

This script may resize, quantize, harden alpha, hash and compose review sheets.
It never draws or repairs the asset subject itself.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from normalize_m_a32_trials import (
    checker,
    crop_ratio,
    fit_transparent,
    quantize_visible,
    transparent_source,
    write_evidence,
)


ROOT = Path(__file__).resolve().parents[2]
CONTRACT = ROOT / "Tools/OCCArt/occ_art_contract_v1.json"


def arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--batch", required=True)
    parser.add_argument("--catalog", required=True)
    parser.add_argument("--contact", required=True)
    return parser.parse_args()


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def fit_adjacency_overlay(source: Image.Image, stem: str, palette_max: int, inset_border: int = 0) -> Image.Image:
    """Resize an authored adjacency overlay and mechanically anchor its named edge(s)."""
    inset_border = max(0, min(15, inset_border))
    target_size = 32 - inset_border * 2
    bounds = source.getchannel("A").getbbox()
    if bounds is None:
        raise ValueError(f"empty adjacency source: {stem}")
    crop = source.crop(bounds)
    scale = min(target_size / crop.width, target_size / crop.height)
    fitted_size = (max(1, round(crop.width * scale)), max(1, round(crop.height * scale)))
    fitted = crop.resize(fitted_size, Image.Resampling.LANCZOS)
    fitted = quantize_visible(fitted, palette_max)

    horizontal = "center"
    vertical = "center"
    if stem.endswith(("_east", "_ne", "_se")):
        horizontal = "end"
    elif stem.endswith(("_west", "_nw", "_sw")):
        horizontal = "start"
    if stem.endswith(("_north", "_ne", "_nw")):
        vertical = "start"
    elif stem.endswith(("_south", "_se", "_sw")):
        vertical = "end"

    x = inset_border if horizontal == "start" else 32 - inset_border - fitted.width if horizontal == "end" else (32 - fitted.width) // 2
    y = inset_border if vertical == "start" else 32 - inset_border - fitted.height if vertical == "end" else (32 - fitted.height) // 2
    canvas = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    canvas.alpha_composite(fitted, (x, y))
    return canvas


def normalize_asset(batch: Path, asset: dict, role: dict) -> tuple[Image.Image, Image.Image | None]:
    source_path = batch / "raw" / asset["stem"] / "source.png"
    role_name = asset["role"]
    palette_max = int(asset["palette_max"])

    if role_name == "floor_tile_32":
        source = crop_ratio(Image.open(source_path).convert("RGB"), 1.0)
        output = source.resize((32, 32), Image.Resampling.BOX)
        output = output.quantize(colors=palette_max, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert("RGBA")
        output.putalpha(255)
        return output, None

    if role_name == "ui_backdrop_480x270":
        source = crop_ratio(Image.open(source_path).convert("RGB"), 16 / 9)
        output = source.resize((480, 270), Image.Resampling.BOX)
        output = output.quantize(colors=palette_max, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert("RGBA")
        output.putalpha(255)
        return output, None

    source = transparent_source(source_path)
    border = int(role.get("transparent_border_min", 0))
    if role_name == "terrain_adjacency_overlay_32":
        return fit_adjacency_overlay(source, asset["stem"], palette_max,
                                     int(asset.get("inset_border", 0))), None
    if role_name == "material_pickup_24_to_ui32":
        native = fit_transparent(source, (24, 24), palette_max, 1)
        output = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
        output.alpha_composite(native, (4, 4))
        return output, native
    if role_name == "tactical_unit_64":
        return fit_transparent(source, (64, 64), palette_max, 1, occupancy=1.0, bottom_y=58), None
    if role_name == "character_portrait_b_384x576":
        return fit_transparent(source, (384, 576), palette_max, border, occupancy=0.84), None
    if role_name == "character_performance_c_192x288":
        return fit_transparent(source, (192, 288), palette_max, border, occupancy=0.88), None

    cells = asset.get("logical_cells")
    if "delivery_size" in role:
        size = tuple(int(value) for value in role["delivery_size"])
    elif cells:
        size = (int(cells[0]) * 32, int(cells[1]) * 32)
    else:
        raise ValueError(f"no delivery size for {asset['stem']}")
    return fit_transparent(source, size, palette_max, border), None


def build_overall_contact(batch: Path, assets: list[dict], filename: str) -> Path:
    cols = 5
    rows = math.ceil(len(assets) / cols)
    cell_w, cell_h = 360, 300
    canvas = Image.new("RGB", (cols * cell_w, rows * cell_h), "#282721")
    draw = ImageDraw.Draw(canvas)
    font = ImageFont.load_default()
    normalized = batch / "normalized"
    for index, asset in enumerate(assets):
        col, row = index % cols, index // cols
        x0, y0 = col * cell_w, row * cell_h
        draw.rectangle((x0 + 8, y0 + 8, x0 + cell_w - 8, y0 + cell_h - 8), fill="#e8dfcf", outline="#5f5a51", width=3)
        image = Image.open(normalized / f"{asset['stem']}.png").convert("RGBA")
        limit_w, limit_h = cell_w - 48, cell_h - 74
        scale = min(limit_w / image.width, limit_h / image.height)
        if max(image.size) <= 128:
            scale = max(1, int(scale))
        target = (max(1, round(image.width * scale)), max(1, round(image.height * scale)))
        preview = image.resize(target, Image.Resampling.NEAREST)
        board = checker(target, 12)
        board.alpha_composite(preview)
        px = x0 + (cell_w - target[0]) // 2
        py = y0 + 36 + (limit_h - target[1]) // 2
        canvas.paste(board.convert("RGB"), (px, py))
        draw.text((x0 + 16, y0 + 16), asset["asset_id"], fill="#2a2823", font=font)
        draw.text((x0 + 16, y0 + cell_h - 26), asset["role"], fill="#5f5a51", font=font)
    output = batch / "QA" / "contacts" / filename
    output.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(output)
    return output


def build_floor_contact(batch: Path, floor_assets: list[dict]) -> Path:
    tile_size = 128
    canvas = Image.new("RGB", (1536, 1024), "#282721")
    draw = ImageDraw.Draw(canvas)
    font = ImageFont.load_default()
    images = [Image.open(batch / "normalized" / f"{asset['stem']}.png").convert("RGB") for asset in floor_assets]
    draw.text((24, 18), f"{batch.name} FLOOR FAMILY / 1x logical cells enlarged 4x", fill="#e8dfcf", font=font)
    for index, (asset, image) in enumerate(zip(floor_assets, images)):
        preview = image.resize((tile_size, tile_size), Image.Resampling.NEAREST)
        x = 24 + index * 180
        canvas.paste(preview, (x, 56))
        draw.text((x, 190), asset["stem"], fill="#e8dfcf", font=font)
    origin_x, origin_y = 24, 250
    rng = random.Random(3301)
    layout = [rng.randrange(len(images)) for _ in range(12 * 9)]
    for y in range(9):
        for x in range(12):
            image = images[layout[y * 12 + x]].resize((64, 64), Image.Resampling.NEAREST)
            canvas.paste(image, (origin_x + x * 64, origin_y + y * 64))
    draw.rectangle((origin_x, origin_y, origin_x + 12 * 64 - 1, origin_y + 9 * 64 - 1), outline="#e8dfcf", width=2)
    output = batch / "QA" / "contacts" / f"{batch.name.lower().replace('-', '_')}_floor_family_contact.png"
    output.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(output)
    return output


def build_small_asset_contact(batch: Path, assets: list[dict]) -> Path:
    canvas = Image.new("RGB", (1280, 640), "#282721")
    draw = ImageDraw.Draw(canvas)
    font = ImageFont.load_default()
    for index, asset in enumerate(assets):
        col, row = index % 4, index // 4
        x0, y0 = col * 320, row * 320
        draw.rectangle((x0 + 10, y0 + 10, x0 + 310, y0 + 310), fill="#e8dfcf", outline="#5f5a51", width=3)
        image = Image.open(batch / "normalized" / f"{asset['stem']}.png").convert("RGBA")
        preview = image.resize((256, 256), Image.Resampling.NEAREST)
        board = checker(preview.size, 24)
        board.alpha_composite(preview)
        canvas.paste(board.convert("RGB"), (x0 + 32, y0 + 35))
        draw.text((x0 + 18, y0 + 18), asset["asset_id"], fill="#2a2823", font=font)
    output = batch / "QA" / "contacts" / f"{batch.name.lower().replace('-', '_')}_semantic_material_contact.png"
    output.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(output)
    return output


def build_vfx_contact(batch: Path, assets: list[dict]) -> Path:
    cell_w, cell_h = 180, 230
    canvas = Image.new("RGB", (len(assets) * cell_w, cell_h), "#282721")
    draw = ImageDraw.Draw(canvas)
    font = ImageFont.load_default()
    for index, asset in enumerate(assets):
        x0 = index * cell_w
        draw.rectangle((x0 + 8, 8, x0 + cell_w - 8, cell_h - 8), fill="#e8dfcf", outline="#5f5a51", width=3)
        image = Image.open(batch / "normalized" / f"{asset['stem']}.png").convert("RGBA")
        preview = image.resize((128, 128), Image.Resampling.NEAREST)
        board = checker(preview.size, 16)
        board.alpha_composite(preview)
        canvas.paste(board.convert("RGB"), (x0 + 26, 42))
        draw.text((x0 + 14, 16), f"{index + 1:02d} {asset['stem']}", fill="#2a2823", font=font)
        draw.text((x0 + 14, 186), "native 32 / review 4x", fill="#5f5a51", font=font)
    output = batch / "QA" / "contacts" / f"{batch.name.lower().replace('-', '_')}_vfx_sequence_contact.png"
    output.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(output)
    return output


def build_character_contact(batch: Path, assets: list[dict]) -> Path:
    canvas = Image.new("RGB", (1500, 720), "#282721")
    draw = ImageDraw.Draw(canvas)
    font = ImageFont.load_default()
    x = 24
    for asset in assets:
        image = Image.open(batch / "normalized" / f"{asset['stem']}.png").convert("RGBA")
        if asset["role"] == "tactical_unit_64":
            scale = 4
        else:
            scale = 1
        preview = image.resize((image.width * scale, image.height * scale), Image.Resampling.NEAREST)
        board = checker(preview.size, 16)
        board.alpha_composite(preview)
        canvas.paste(board.convert("RGB"), (x, 58))
        draw.text((x, 30), asset["stem"], fill="#e8dfcf", font=font)
        x += preview.width + 28
    output = batch / "QA" / "contacts" / f"{batch.name.lower().replace('-', '_')}_character_identity_contact.png"
    output.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(output)
    return output


def build_adjacency_contact(batch: Path, assets: list[dict]) -> Path:
    canvas = Image.new("RGB", (1440, 760), "#282721")
    draw = ImageDraw.Draw(canvas)
    font = ImageFont.load_default()
    for index, asset in enumerate(assets):
        x0 = 18 + index * 176
        image = Image.open(batch / "normalized" / f"{asset['stem']}.png").convert("RGBA")
        preview = image.resize((128, 128), Image.Resampling.NEAREST)
        board = checker(preview.size, 16)
        board.alpha_composite(preview)
        canvas.paste(board.convert("RGB"), (x0, 44))
        draw.text((x0, 20), asset["stem"], fill="#e8dfcf", font=font)

    floor_path = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A33/normalized/harbor_inspection_floor_a.png"
    floor = Image.open(floor_path).convert("RGBA") if floor_path.is_file() else Image.new("RGBA", (32, 32), (120, 112, 95, 255))
    by_suffix = {asset["stem"].removeprefix("harbor_quay_"): asset for asset in assets}
    layout = [
        ["corner_nw", "edge_north", "corner_ne"],
        ["edge_west", None, "edge_east"],
        ["corner_sw", "edge_south", "corner_se"],
    ]
    origin_x, origin_y, cell = 430, 260, 128
    for row, keys in enumerate(layout):
        for col, key in enumerate(keys):
            composed = floor.copy()
            if key and key in by_suffix:
                overlay = Image.open(batch / "normalized" / f"{by_suffix[key]['stem']}.png").convert("RGBA")
                composed.alpha_composite(overlay)
            preview = composed.resize((cell, cell), Image.Resampling.NEAREST)
            canvas.paste(preview.convert("RGB"), (origin_x + col * cell, origin_y + row * cell))
    draw.rectangle((origin_x, origin_y, origin_x + cell * 3 - 1, origin_y + cell * 3 - 1), outline="#e8dfcf", width=2)
    draw.text((origin_x, origin_y - 24), "3x3 adjacency contact on M-A33 floor A / review 4x", fill="#e8dfcf", font=font)
    output = batch / "QA" / "contacts" / f"{batch.name.lower().replace('-', '_')}_adjacency_contact.png"
    output.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(output)
    return output


def contact_floor(batch: Path, floor_assets: list[dict] | None = None) -> tuple[Image.Image, str]:
    if floor_assets:
        stem = floor_assets[0]["stem"]
        path = batch / "normalized" / f"{stem}.png"
        if path.is_file():
            return Image.open(path).convert("RGBA"), f"{batch.name} {stem}"
    fallback = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A33/normalized/harbor_inspection_floor_a.png"
    if fallback.is_file():
        return Image.open(fallback).convert("RGBA"), "M-A33 harbor floor A"
    return Image.new("RGBA", (32, 32), (120, 112, 95, 255)), "neutral fallback floor"


def build_prop_footprint_contact(batch: Path, assets: list[dict], floor_assets: list[dict] | None = None) -> Path:
    panel_w, panel_h = 760, 430
    cols = 2
    rows = math.ceil(len(assets) / cols)
    canvas = Image.new("RGB", (cols * panel_w, rows * panel_h), "#282721")
    draw = ImageDraw.Draw(canvas)
    font = ImageFont.load_default()
    floor, floor_label = contact_floor(batch, floor_assets)

    for index, asset in enumerate(assets):
        col, row = index % cols, index // cols
        x0, y0 = col * panel_w, row * panel_h
        draw.rectangle((x0 + 10, y0 + 10, x0 + panel_w - 10, y0 + panel_h - 10), fill="#e8dfcf", outline="#5f5a51", width=3)
        prop = Image.open(batch / "normalized" / f"{asset['stem']}.png").convert("RGBA")
        cells = asset.get("logical_cells") or [max(1, prop.width // 32), max(1, prop.height // 32)]
        contact = Image.new("RGBA", prop.size, (0, 0, 0, 0))
        for cell_y in range(int(cells[1])):
            for cell_x in range(int(cells[0])):
                contact.alpha_composite(floor, (cell_x * 32, cell_y * 32))
        contact.alpha_composite(prop)
        scale = min(6, max(2, min((panel_w - 90) // contact.width, (panel_h - 105) // contact.height)))
        preview = contact.resize((contact.width * scale, contact.height * scale), Image.Resampling.NEAREST)
        px = x0 + (panel_w - preview.width) // 2
        py = y0 + 54 + (panel_h - 115 - preview.height) // 2
        canvas.paste(preview.convert("RGB"), (px, py))
        for grid_x in range(int(cells[0]) + 1):
            gx = px + grid_x * 32 * scale
            draw.line((gx, py, gx, py + preview.height), fill="#2a2823", width=2)
        for grid_y in range(int(cells[1]) + 1):
            gy = py + grid_y * 32 * scale
            draw.line((px, gy, px + preview.width, gy), fill="#2a2823", width=2)
        draw.text((x0 + 24, y0 + 24), f"{asset['stem']} / {cells[0]}x{cells[1]}", fill="#2a2823", font=font)
        draw.text((x0 + 24, y0 + panel_h - 34), f"{floor_label} / logical footprint contact / nearest-neighbor", fill="#5f5a51", font=font)

    output = batch / "QA" / "contacts" / f"{batch.name.lower().replace('-', '_')}_prop_footprint_contact.png"
    output.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(output)
    return output


def build_region_map_contact(batch: Path, floor_assets: list[dict], prop_assets: list[dict]) -> Path:
    """Compose a non-Unity 12x9 application contact from authored assets only."""
    if len(floor_assets) < 4 or not prop_assets:
        raise ValueError("region map contact needs at least four floors and one prop")
    scale = 2
    cell = 32 * scale
    map_w, map_h = 12 * cell, 9 * cell
    header_h = 72
    canvas = Image.new("RGB", (map_w + 420, map_h + header_h + 30), "#282721")
    draw = ImageDraw.Draw(canvas)
    font = ImageFont.load_default()
    draw.text((20, 18), f"{batch.name} / REG-ORH-01 / NON-UNITY 12x9 APPLICATION CONTACT", fill="#e8dfcf", font=font)
    draw.text((20, 40), "native 32px cells shown 2x; authored assets only; cyan corners are temporary tactical-review overlay", fill="#aaa294", font=font)

    floor_images = {
        asset["stem"]: Image.open(batch / "normalized" / f"{asset['stem']}.png").convert("RGBA")
        for asset in floor_assets
    }
    ordered = [asset["stem"] for asset in floor_assets]
    origin_x, origin_y = 20, header_h
    for y in range(9):
        for x in range(12):
            if y in {4, 5} or (x in {5, 6} and y in {2, 3, 6}):
                stem = ordered[(x + y) % 2]
            elif (x < 4 and y > 5) or (x > 9 and y < 3):
                stem = ordered[3]
            else:
                stem = ordered[2]
            tile = floor_images[stem].resize((cell, cell), Image.Resampling.NEAREST)
            canvas.paste(tile.convert("RGB"), (origin_x + x * cell, origin_y + y * cell))

    by_stem = {asset["stem"]: asset for asset in prop_assets}
    placements = [
        ("outer_ring_trail_marker_post", 1, 3),
        ("outer_ring_wattle_fence_intact_2x1", 3, 2),
        ("outer_ring_wattle_fence_damaged_2x1", 7, 2),
        ("outer_ring_livestock_trough_2x1", 8, 4),
        ("outer_ring_stone_culvert_2x1", 3, 6),
        ("outer_ring_farm_bucket_rack", 1, 7),
        ("outer_ring_wattle_fence_debris_2x1", 6, 7),
        ("outer_ring_hunter_waystation_2x2", 9, 6),
    ]
    for stem, x, y in sorted(placements, key=lambda item: item[2]):
        if stem not in by_stem:
            continue
        prop = Image.open(batch / "normalized" / f"{stem}.png").convert("RGBA")
        preview = prop.resize((prop.width * scale, prop.height * scale), Image.Resampling.NEAREST)
        px = origin_x + x * cell
        py = origin_y + y * cell
        canvas.paste(preview, (px, py), preview)

    unit_path = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A34/normalized/anonymous_field_apprentice_a.png"
    if unit_path.is_file():
        unit = Image.open(unit_path).convert("RGBA").resize((128, 128), Image.Resampling.NEAREST)
        anchor_x = origin_x + 5 * cell - 32
        anchor_y = origin_y + 5 * cell - 64
        canvas.paste(unit, (anchor_x, anchor_y), unit)

    for x, y in [(4, 4), (5, 4), (6, 4), (5, 5)]:
        left, top = origin_x + x * cell, origin_y + y * cell
        color = "#7ab7b5"
        length = 12
        for dx, dy, sx, sy in [(0, 0, 1, 1), (cell - 1, 0, -1, 1), (0, cell - 1, 1, -1), (cell - 1, cell - 1, -1, -1)]:
            draw.line((left + dx, top + dy, left + dx + sx * length, top + dy), fill=color, width=2)
            draw.line((left + dx, top + dy, left + dx, top + dy + sy * length), fill=color, width=2)

    draw.rectangle((origin_x, origin_y, origin_x + map_w - 1, origin_y + map_h - 1), outline="#e8dfcf", width=2)
    legend_x = origin_x + map_w + 24
    draw.text((legend_x, origin_y), "APPLICATION CHECK", fill="#e8dfcf", font=font)
    for index, text in enumerate([
        "- 12x9 orthographic board",
        "- floor family mixed at 2x",
        "- intact/damaged/debris fence",
        "- 1x1, 2x1 and 2x2 footprints",
        "- M-A34 anonymous unit for contrast",
        "- temporary range corners stay readable",
        "- no Unity asset import",
    ]):
        draw.text((legend_x, origin_y + 30 + index * 22), text, fill="#aaa294", font=font)
    output = batch / "QA" / "contacts" / f"{batch.name.lower().replace('-', '_')}_region_12x9_contact.png"
    output.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(output)
    return output


def build_task_item_contact(batch: Path, icon_assets: list[dict], footprint_assets: list[dict]) -> Path:
    """Test independently authored task-item icons and 1x1 inventory footprints together."""
    icon_by_pair = {asset.get("pair_id"): asset for asset in icon_assets if asset.get("pair_id")}
    footprint_by_pair = {asset.get("pair_id"): asset for asset in footprint_assets if asset.get("pair_id")}
    pair_ids = [pair_id for pair_id in icon_by_pair if pair_id in footprint_by_pair]
    if not pair_ids:
        raise ValueError("task item contact needs matching pair_id values")

    canvas = Image.new("RGB", (1320, 760), "#282721")
    draw = ImageDraw.Draw(canvas)
    font = ImageFont.load_default()
    draw.text((24, 20), f"{batch.name} / TASK ITEM ICON + FOOTPRINT / NON-UNITY APPLICATION CONTACT", fill="#e8dfcf", font=font)
    draw.text((24, 43), "left: 6x10 inventory excerpt at 2x; right: independent 32px icon and footprint at 4x", fill="#aaa294", font=font)

    origin_x, origin_y, cell = 24, 88, 64
    for y in range(10):
        for x in range(6):
            shade = "#c7b79a" if (x + y) % 2 == 0 else "#b8a88d"
            draw.rectangle((origin_x + x * cell, origin_y + y * cell, origin_x + (x + 1) * cell - 1, origin_y + (y + 1) * cell - 1), fill=shade, outline="#5d5548", width=2)
    placements = [(0, 0), (3, 2), (1, 5), (4, 7), (2, 8), (0, 7)]
    for index, pair_id in enumerate(pair_ids):
        asset = footprint_by_pair[pair_id]
        footprint = Image.open(batch / "normalized" / f"{asset['stem']}.png").convert("RGBA")
        preview = footprint.resize((64, 64), Image.Resampling.NEAREST)
        x, y = placements[index % len(placements)]
        canvas.paste(preview, (origin_x + x * cell, origin_y + y * cell), preview)
    draw.rectangle((origin_x, origin_y, origin_x + 6 * cell - 1, origin_y + 10 * cell - 1), outline="#e8dfcf", width=2)

    panel_x = 450
    for index, pair_id in enumerate(pair_ids):
        y0 = 88 + index * 160
        icon_asset = icon_by_pair[pair_id]
        footprint_asset = footprint_by_pair[pair_id]
        draw.rectangle((panel_x, y0, 1288, y0 + 142), fill="#e8dfcf", outline="#5f5a51", width=3)
        icon = Image.open(batch / "normalized" / f"{icon_asset['stem']}.png").convert("RGBA").resize((128, 128), Image.Resampling.NEAREST)
        footprint = Image.open(batch / "normalized" / f"{footprint_asset['stem']}.png").convert("RGBA").resize((128, 128), Image.Resampling.NEAREST)
        icon_board = checker((128, 128), 16)
        icon_board.alpha_composite(icon)
        footprint_board = checker((128, 128), 16)
        footprint_board.alpha_composite(footprint)
        canvas.paste(icon_board.convert("RGB"), (panel_x + 12, y0 + 7))
        canvas.paste(footprint_board.convert("RGB"), (panel_x + 154, y0 + 7))
        draw.text((panel_x + 300, y0 + 22), pair_id, fill="#2a2823", font=font)
        draw.text((panel_x + 300, y0 + 48), f"ICON: {icon_asset['stem']}", fill="#5f5a51", font=font)
        draw.text((panel_x + 300, y0 + 70), f"FOOTPRINT: {footprint_asset['stem']}", fill="#5f5a51", font=font)
        draw.text((panel_x + 300, y0 + 104), "independent sources / exact 32px deliveries", fill="#5f5a51", font=font)

    draw.text((24, 736), "temporary QA layout only; no gameplay stats, no Unity import", fill="#aaa294", font=font)
    output = batch / "QA" / "contacts" / f"{batch.name.lower().replace('-', '_')}_task_item_inventory_contact.png"
    output.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(output)
    return output


def build_outer_ring_expansion_contact(batch: Path, floor_assets: list[dict], prop_assets: list[dict]) -> Path:
    """Compose M-A45 with the accepted M-A43 base vocabulary on a 12x9 board."""
    scale = 2
    cell = 32 * scale
    map_w, map_h = 12 * cell, 9 * cell
    header_h = 72
    canvas = Image.new("RGB", (map_w + 420, map_h + header_h + 30), "#282721")
    draw = ImageDraw.Draw(canvas)
    font = ImageFont.load_default()
    draw.text((20, 18), f"{batch.name} / REG-ORH-01 EXPANSION / NON-UNITY 12x9 CONTACT", fill="#e8dfcf", font=font)
    draw.text((20, 40), "M-A45 authored expansion on accepted M-A43 authored base; native 32px cells shown 2x", fill="#aaa294", font=font)

    base = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A43/normalized"
    floor_paths = [
        base / "outer_ring_packed_trail_a.png",
        base / "outer_ring_field_verge.png",
        base / "outer_ring_drainage_mud.png",
    ] + [batch / "normalized" / f"{asset['stem']}.png" for asset in floor_assets]
    floors = [Image.open(path).convert("RGBA") for path in floor_paths]
    origin_x, origin_y = 20, header_h
    for y in range(9):
        for x in range(12):
            if y in {3, 4}:
                index = 0
            elif (x < 3 and y > 5) or (x > 9 and y < 3):
                index = 2
            elif (x + 2 * y) % 5 == 0:
                index = 3
            elif (2 * x + y) % 7 == 0:
                index = 4
            else:
                index = 1
            tile = floors[index].resize((cell, cell), Image.Resampling.NEAREST)
            canvas.paste(tile.convert("RGB"), (origin_x + x * cell, origin_y + y * cell))

    current = {asset["stem"]: batch / "normalized" / f"{asset['stem']}.png" for asset in prop_assets}
    context = {
        "outer_ring_wattle_fence_intact_2x1": base / "outer_ring_wattle_fence_intact_2x1.png",
        "outer_ring_hunter_waystation_2x2": base / "outer_ring_hunter_waystation_2x2.png",
    }
    paths = {**context, **current}
    placements = [
        ("outer_ring_wattle_fence_intact_2x1", 2, 2),
        ("outer_ring_wattle_field_gate_2x1", 4, 2),
        ("outer_ring_farm_handcart_2x1", 8, 3),
        ("outer_ring_drainage_plank_crossing_2x1", 1, 6),
        ("outer_ring_field_boundary_stone", 7, 5),
        ("outer_ring_sapling_guard", 10, 1),
        ("outer_ring_hunter_waystation_2x2", 1, 0),
        ("outer_ring_covered_hayrick_2x2", 9, 6),
    ]
    for stem, x, y in sorted(placements, key=lambda item: item[2]):
        path = paths.get(stem)
        if path is None or not path.is_file():
            continue
        prop = Image.open(path).convert("RGBA")
        preview = prop.resize((prop.width * scale, prop.height * scale), Image.Resampling.NEAREST)
        canvas.paste(preview, (origin_x + x * cell, origin_y + y * cell), preview)
    draw.rectangle((origin_x, origin_y, origin_x + map_w - 1, origin_y + map_h - 1), outline="#e8dfcf", width=2)

    legend_x = origin_x + map_w + 24
    draw.text((legend_x, origin_y), "EXPANSION CHECK", fill="#e8dfcf", font=font)
    for index, line in enumerate([
        "- M-A43 + M-A45 floor family",
        "- gate adjacent to accepted wattle fence",
        "- 1x1, 2x1 and 2x2 footprints",
        "- two distinct region anchors",
        "- no interaction or loot overlays",
        "- no Unity asset import",
    ]):
        draw.text((legend_x, origin_y + 30 + index * 22), line, fill="#aaa294", font=font)
    output = batch / "QA" / "contacts" / f"{batch.name.lower().replace('-', '_')}_outer_ring_expansion_contact.png"
    output.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(output)
    return output


def main() -> None:
    args = arguments()
    batch = ROOT / "Worldbuilding/05_美术与音频/正式美术生产" / args.batch
    assets = json.loads((batch / args.catalog).read_text(encoding="utf-8"))["assets"]
    roles = json.loads(CONTRACT.read_text(encoding="utf-8"))["roles"]
    normalized = batch / "normalized"
    normalized.mkdir(parents=True, exist_ok=True)

    for asset in assets:
        output, native = normalize_asset(batch, asset, roles[asset["role"]])
        output_path = normalized / f"{asset['stem']}.png"
        output.save(output_path)
        if native is not None:
            native.save(normalized / f"{asset['stem']}_native24.png")
        write_evidence(output, batch / "QA" / asset["stem"])

        manifest_path = batch / "manifests" / f"{asset['stem']}.occ-art-manifest-v1.json"
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        source_path = batch / "raw" / asset["stem"] / "source.png"
        manifest["provenance"]["source_sha256"] = digest(source_path)
        manifest["delivery"]["output_sha256"] = digest(output_path)
        manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    overall = build_overall_contact(batch, assets, args.contact)
    floor_assets = [asset for asset in assets if asset["role"] == "floor_tile_32"]
    if floor_assets:
        build_floor_contact(batch, floor_assets)
    small_assets = [asset for asset in assets if asset["role"] in {"semantic_icon_16", "material_pickup_24_to_ui32"}]
    if small_assets:
        build_small_asset_contact(batch, small_assets)
    vfx_assets = [asset for asset in assets if asset["role"] == "vfx_frame_32"]
    if vfx_assets:
        build_vfx_contact(batch, vfx_assets)
    character_assets = [asset for asset in assets if asset["role"] in {"tactical_unit_64", "character_portrait_b_384x576", "character_performance_c_192x288"}]
    if character_assets:
        build_character_contact(batch, character_assets)
    adjacency_assets = [asset for asset in assets if asset["role"] == "terrain_adjacency_overlay_32"]
    if adjacency_assets:
        build_adjacency_contact(batch, adjacency_assets)
    prop_assets = [asset for asset in assets if asset["role"] in {"single_cell_prop_32", "multi_cell_prop_32", "modular_structure_32"}]
    if prop_assets:
        build_prop_footprint_contact(batch, prop_assets, floor_assets)
    if len(floor_assets) >= 4 and prop_assets:
        build_region_map_contact(batch, floor_assets, prop_assets)
    if batch.name == "M-A45" and floor_assets and prop_assets:
        build_outer_ring_expansion_contact(batch, floor_assets, prop_assets)
    icon_assets = [asset for asset in assets if asset["role"] == "equipment_icon_32" and asset.get("pair_id")]
    task_footprints = [asset for asset in prop_assets if asset.get("pair_id")]
    if icon_assets and task_footprints:
        build_task_item_contact(batch, icon_assets, task_footprints)
    print(json.dumps({"batch": args.batch, "normalized": len(assets), "contact": overall.relative_to(ROOT).as_posix()}, ensure_ascii=False))


if __name__ == "__main__":
    main()
