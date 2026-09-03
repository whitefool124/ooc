"""Hand-author the rustic 32px academy battlefield family.

This is an offline editable pixel-art source. It emits fixed native 32x32 PNGs and QA
evidence only; it is never called by the game at runtime.
"""
from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "normalized" / "battlefield_reset_v08"
QA = ROOT / "QA" / "battlefield_reset_v08"

INK = (34, 30, 26, 255)
MORTAR = (72, 68, 61, 255)
STONE_D = (105, 100, 89, 255)
STONE = (143, 136, 119, 255)
STONE_L = (176, 166, 142, 255)
STONE_WARM = (154, 142, 119, 255)
EARTH_D = (83, 61, 42, 255)
EARTH = (119, 88, 57, 255)
EARTH_L = (148, 111, 70, 255)
WOOD_D = (68, 42, 27, 255)
WOOD = (111, 69, 39, 255)
WOOD_L = (157, 105, 56, 255)
IRON = (61, 57, 51, 255)
IRON_L = (91, 86, 76, 255)
PARCHMENT = (199, 181, 139, 255)
MOSS_D = (50, 70, 39, 255)
MOSS = (76, 101, 53, 255)
HERB = (100, 125, 65, 255)
COPPER_D = (111, 70, 39, 255)
COPPER = (170, 105, 53, 255)
CYAN_D = (29, 104, 109, 255)
CYAN = (51, 174, 178, 255)
GOLD = (218, 151, 53, 255)
TRANSPARENT = (0, 0, 0, 0)


def canvas(fill=TRANSPARENT):
    return Image.new("RGBA", (32, 32), fill)


def line(draw, points, fill, width=1):
    draw.line(points, fill=fill, width=width)


def courtyard(variant: int):
    im = canvas(STONE)
    d = ImageDraw.Draw(im)
    seams = ((15, 16), (14, 17), (16, 15), (13, 18))[variant]
    vertical, horizontal = seams
    d.rectangle((0, horizontal, 31, horizontal + 1), fill=MORTAR)
    d.rectangle((vertical, 0, vertical + 1, horizontal - 1), fill=MORTAR)
    d.rectangle((vertical + 3, horizontal + 2, vertical + 4, 31), fill=STONE_D)
    d.line((0, horizontal - 1, 31, horizontal - 1), fill=STONE_L)
    d.line((vertical - 1, 0, vertical - 1, horizontal - 2), fill=STONE_L)
    chips = (((3, 4), (25, 23)), ((7, 27), (26, 5)), ((4, 22), (22, 8)), ((9, 5), (27, 25)))[variant]
    for x, y in chips:
        d.point((x, y), fill=STONE_WARM)
        if (x + 1 < 32): d.point((x + 1, y), fill=STONE_WARM)
    return im


def stone_road(variant: int):
    im = canvas(STONE_D)
    d = ImageDraw.Draw(im)
    rows = (0, 7, 15, 23, 32)
    offsets = ((0, 8, 3, 11), (4, 0, 9, 2), (9, 2, 12, 5), (2, 11, 5, 13))[variant]
    for y in rows[1:-1]:
        d.rectangle((0, y, 31, y + 1), fill=MORTAR)
        d.line((0, y - 1, 31, y - 1), fill=STONE_L)
    for row, (y0, y1) in enumerate(zip(rows, rows[1:])):
        start = offsets[row]
        for x in range(start, 32, 11):
            d.rectangle((x, y0, min(31, x + 1), y1 - 1), fill=MORTAR)
            if x > 0: d.point((x - 1, min(31, y0 + 1)), fill=STONE_L)
    wear = (((5, 11), (19, 27)), ((12, 4), (27, 18)), ((4, 27), (22, 11)), ((15, 19), (29, 4)))[variant]
    for x, y in wear:
        d.line((x, y, x + 2, y), fill=STONE_WARM)
    return im


def ruins(variant: int):
    im = courtyard(variant)
    d = ImageDraw.Draw(im)
    cracks = (
        ((4, 2, 6, 5, 5, 8), (23, 20, 20, 22, 22, 26)),
        ((26, 2, 23, 5, 24, 9), (8, 20, 11, 23, 9, 27)),
        ((5, 25, 8, 22, 7, 19), (19, 3, 21, 6, 20, 10)),
        ((2, 10, 5, 12, 4, 16), (28, 21, 25, 23, 26, 28)),
    )[variant]
    for points in cracks:
        line(d, points, MORTAR)
        d.point((points[-2] + 1, points[-1]), fill=INK)
    moss = (((0, 27, 5, 31), (27, 0, 31, 3)), ((0, 0, 4, 3), (25, 27, 31, 31)),
            ((0, 18, 3, 25), (19, 0, 24, 2)), ((28, 8, 31, 15), (7, 29, 13, 31)))[variant]
    for rect in moss:
        d.rectangle(rect, fill=MOSS_D)
        x0, y0, x1, y1 = rect
        d.point(((x0 + x1) // 2, (y0 + y1) // 2), fill=MOSS)
    return im


def packed_earth(variant: int):
    im = canvas(EARTH)
    d = ImageDraw.Draw(im)
    paths = (
        ((0, 9, 7, 8, 15, 10, 23, 9, 31, 11), (3, 25, 10, 23, 18, 25, 28, 24)),
        ((0, 20, 8, 18, 16, 20, 24, 19, 31, 17), (5, 3, 12, 5, 21, 4, 29, 6)),
        ((0, 14, 8, 15, 16, 13, 24, 14, 31, 12), (4, 28, 13, 27, 21, 29, 30, 27)),
    )[variant]
    for points in paths:
        line(d, points, EARTH_D)
    stones = (((5, 4), (25, 16), (13, 27)), ((3, 12), (19, 25), (28, 8)), ((7, 23), (17, 5), (27, 20)))[variant]
    for x, y in stones:
        d.rectangle((x, y, x + 2, y + 1), fill=STONE_D)
        d.point((x, y), fill=STONE_L)
    return im


def grass_edge(direction: str):
    im = packed_earth("nesw".index(direction) % 3)
    d = ImageDraw.Draw(im)
    if direction in "ns":
        base = 0 if direction == "n" else 31
        step = 1 if direction == "n" else -1
        for x in range(32):
            depth = (x * 7 + 3) % 5 + 3
            y0, y1 = sorted((base, base + step * depth))
            d.line((x, y0, x, y1), fill=MOSS_D)
            if x % 3 == 0: d.point((x, base + step * max(1, depth - 1)), fill=MOSS)
    else:
        base = 31 if direction == "e" else 0
        step = -1 if direction == "e" else 1
        for y in range(32):
            depth = (y * 5 + 2) % 5 + 3
            x0, x1 = sorted((base, base + step * depth))
            d.line((x0, y, x1, y), fill=MOSS_D)
            if y % 3 == 0: d.point((base + step * max(1, depth - 1), y), fill=MOSS)
    return im


def inlay(variant: int):
    im = courtyard(variant)
    d = ImageDraw.Draw(im)
    patterns = (
        ((16, 0, 16, 31),),
        ((0, 16, 13, 16), (18, 16, 31, 16)),
        ((4, 0, 4, 12, 11, 19, 11, 31),),
        ((27, 0, 27, 10, 18, 19, 18, 31),),
    )[variant]
    for points in patterns:
        line(d, points, COPPER_D, 3)
        line(d, points, COPPER, 1)
    sparks = (((16, 7), (16, 24)), ((7, 16), (25, 16)), ((4, 6), (11, 24)), ((27, 5), (18, 25)))[variant]
    for x, y in sparks: d.rectangle((x, y, x, min(31, y + 2)), fill=CYAN)
    return im


def connector(kind: str):
    im = courtyard(("straight", "corner", "tee", "cross").index(kind))
    d = ImageDraw.Draw(im)
    arms = {"straight": ("n", "s"), "corner": ("n", "e"),
            "tee": ("w", "e", "s"), "cross": ("n", "e", "s", "w")}[kind]
    center = (16, 16)
    ends = {"n": (16, 0), "e": (31, 16), "s": (16, 31), "w": (0, 16)}
    for arm in arms:
        line(d, (center, ends[arm]), COPPER_D, 3)
        line(d, (center, ends[arm]), COPPER, 1)
    d.rectangle((13, 13, 19, 19), fill=COPPER_D)
    d.rectangle((15, 15, 17, 17), fill=CYAN_D)
    d.point((16, 16), fill=CYAN)
    return im


def bench(state: str):
    im = canvas(); d = ImageDraw.Draw(im)
    if state == "rubble":
        d.polygon(((3, 25), (9, 20), (14, 22), (12, 28), (4, 29)), fill=STONE_D)
        d.polygon(((16, 23), (25, 20), (29, 25), (26, 29), (17, 28)), fill=STONE)
        d.rectangle((10, 27, 21, 29), fill=MORTAR)
        d.point((7, 22), fill=STONE_L); d.point((24, 22), fill=STONE_L)
        return im
    # Tactical three-quarter projection: the upper slab is the dominant readable plane;
    # the shortened front face and two feet only establish height inside the tile.
    d.polygon(((3, 15), (8, 11), (27, 11), (30, 15), (25, 21), (7, 21)), fill=INK)
    d.polygon(((5, 15), (9, 12), (25, 12), (28, 15), (24, 18), (8, 18)), fill=STONE_L)
    d.polygon(((8, 18), (24, 18), (24, 22), (8, 22)), fill=STONE)
    d.rectangle((8, 22, 11, 27), fill=STONE_D); d.rectangle((21, 22, 24, 27), fill=STONE_D)
    d.point((10, 13), fill=STONE_WARM); d.line((13, 19, 18, 19), fill=MORTAR)
    if state == "damaged":
        d.polygon(((20, 11), (27, 11), (30, 15), (25, 20), (21, 17)), fill=TRANSPARENT)
        line(d, (18, 13, 16, 16, 17, 21), MORTAR)
        d.rectangle((25, 24, 29, 28), fill=STONE_D)
    return im


def planter(state: str):
    im = canvas(); d = ImageDraw.Draw(im)
    if state == "rubble":
        d.polygon(((3, 24), (11, 20), (15, 24), (13, 29), (4, 29)), fill=STONE_D)
        d.polygon(((17, 23), (27, 21), (29, 27), (22, 29)), fill=STONE)
        d.rectangle((9, 26, 24, 30), fill=EARTH_D)
        d.line((19, 22, 23, 17), fill=MOSS_D); d.point((24, 16), fill=HERB)
        return im
    d.polygon(((3, 15), (8, 11), (27, 11), (30, 15), (26, 27), (6, 27)), fill=INK)
    d.polygon(((6, 15), (9, 12), (25, 12), (28, 15), (24, 20), (9, 20)), fill=STONE_L)
    d.polygon(((9, 14), (24, 14), (26, 16), (23, 18), (10, 18), (7, 16)), fill=EARTH_D)
    d.polygon(((9, 20), (24, 20), (24, 25), (8, 25)), fill=STONE)
    stems = ((11, 15, 9, 8), (15, 14, 16, 6), (20, 14, 23, 9), (23, 15, 25, 11))
    for x0, y0, x1, y1 in stems: line(d, (x0, y0, x1, y1), MOSS_D)
    for x, y in ((8, 7), (16, 5), (23, 7), (26, 10), (13, 9)): d.rectangle((x, y, x + 1, y + 1), fill=HERB)
    if state == "damaged":
        d.polygon(((3, 15), (8, 12), (11, 17), (7, 22), (3, 20)), fill=TRANSPARENT)
        line(d, (12, 18, 14, 21, 13, 25), MORTAR)
        d.rectangle((3, 25, 7, 29), fill=STONE_D)
    return im


def archive_stack(state: str):
    im = canvas(); d = ImageDraw.Draw(im)
    if state == "rubble":
        d.polygon(((3, 25), (8, 18), (15, 22), (16, 29), (4, 29)), fill=WOOD_D)
        d.polygon(((14, 21), (25, 19), (29, 25), (26, 30), (15, 28)), fill=WOOD)
        for x, y in ((7, 23), (18, 24), (23, 21)): d.rectangle((x, y, x + 5, y + 1), fill=PARCHMENT)
        return im
    # Waist-high archive bundles, not a front-facing bookcase façade.
    d.polygon(((4, 10), (10, 5), (25, 5), (29, 10), (26, 28), (6, 28)), fill=INK)
    d.polygon(((7, 10), (11, 7), (23, 7), (26, 10), (23, 15), (10, 15)), fill=WOOD_L)
    d.polygon(((8, 15), (24, 15), (23, 26), (8, 26)), fill=WOOD)
    d.line((9, 19, 23, 19), fill=WOOD_D, width=2); d.line((9, 23, 23, 23), fill=WOOD_D, width=2)
    for rect in ((11, 8, 14, 11), (16, 8, 20, 12), (10, 16, 15, 18), (17, 16, 22, 18), (10, 21, 13, 22), (15, 21, 20, 22)):
        d.rectangle(rect, fill=PARCHMENT)
        d.line((rect[0], rect[1], rect[2], rect[1]), fill=WOOD_L)
    d.line((7, 12, 8, 25), fill=IRON, width=2); d.line((24, 12, 23, 25), fill=IRON, width=2)
    if state == "damaged":
        d.polygon(((20, 5), (27, 8), (29, 13), (24, 17), (20, 13)), fill=TRANSPARENT)
        d.line((18, 16, 24, 23), fill=INK, width=2)
        d.rectangle((24, 26, 29, 29), fill=WOOD_D)
        d.rectangle((20, 28, 24, 29), fill=PARCHMENT)
    return im


def masonry_screen(state: str):
    im = canvas(); d = ImageDraw.Draw(im)
    if state == "rubble":
        d.polygon(((2, 27), (7, 20), (13, 22), (15, 29), (3, 30)), fill=STONE_D)
        d.polygon(((14, 25), (20, 18), (29, 23), (28, 30), (15, 30)), fill=STONE)
        d.line((8, 24, 12, 27), fill=MORTAR); d.point((21, 21), fill=STONE_L)
        return im
    # Low thick training wall with a visible cap; avoids the read of a vertical building elevation.
    d.polygon(((2, 12), (8, 7), (25, 7), (30, 12), (27, 27), (5, 27)), fill=INK)
    d.polygon(((5, 12), (9, 9), (24, 9), (27, 12), (24, 16), (9, 16)), fill=STONE_L)
    d.polygon(((7, 16), (25, 16), (24, 25), (7, 25)), fill=STONE)
    d.line((8, 20, 24, 20), fill=MORTAR, width=2)
    d.line((13, 10, 13, 15), fill=MORTAR); d.line((20, 10, 20, 15), fill=MORTAR)
    d.line((12, 17, 12, 20), fill=MORTAR); d.line((19, 21, 19, 25), fill=MORTAR)
    d.polygon(((14, 12), (16, 10), (18, 12), (16, 14)), fill=COPPER_D)
    if state == "damaged":
        d.polygon(((20, 7), (27, 9), (30, 13), (25, 17), (21, 14)), fill=TRANSPARENT)
        line(d, (19, 14, 17, 18, 19, 21, 16, 25), MORTAR)
        d.rectangle((25, 25, 30, 29), fill=STONE_D)
    return im


def aether_pillar(state: str):
    im = canvas(); d = ImageDraw.Draw(im)
    if state == "rubble":
        d.polygon(((5, 25), (12, 20), (17, 23), (16, 29), (5, 29)), fill=STONE_D)
        d.polygon(((15, 23), (23, 18), (28, 24), (26, 29), (17, 29)), fill=STONE)
        d.rectangle((17, 20, 19, 25), fill=COPPER_D); d.point((18, 21), fill=CYAN_D)
        return im
    d.polygon(((8, 10), (16, 5), (24, 10), (23, 25), (27, 28), (5, 28), (9, 25)), fill=INK)
    d.polygon(((11, 10), (16, 7), (21, 10), (18, 14), (14, 14)), fill=STONE_L)
    d.polygon(((12, 14), (20, 14), (19, 24), (13, 24)), fill=STONE)
    d.polygon(((15, 11), (17, 11), (18, 14), (17, 22), (15, 22), (14, 14)), fill=COPPER_D)
    d.rectangle((15, 14, 17, 20), fill=CYAN_D); d.rectangle((16, 15, 16, 18), fill=CYAN)
    d.polygon(((9, 24), (23, 24), (26, 27), (22, 29), (10, 29), (6, 27)), fill=STONE_D)
    d.line((10, 25, 22, 25), fill=STONE_L)
    if state == "damaged":
        d.polygon(((17, 6), (23, 9), (22, 15), (18, 17), (16, 12)), fill=TRANSPARENT)
        d.line((15, 16, 12, 20, 14, 24), fill=MORTAR)
        d.rectangle((23, 24, 28, 29), fill=STONE_D)
    return im


def seal_plinth(state: str):
    im = canvas(); d = ImageDraw.Draw(im)
    if state == "rubble":
        d.polygon(((3, 25), (9, 19), (16, 22), (15, 29), (4, 30)), fill=STONE_D)
        d.polygon(((15, 24), (24, 19), (29, 25), (27, 30), (16, 29)), fill=STONE)
        d.line((10, 23, 18, 26), fill=COPPER_D)
        return im
    d.polygon(((4, 17), (10, 11), (23, 11), (28, 17), (27, 26), (23, 29), (9, 29), (4, 26)), fill=INK)
    d.polygon(((7, 17), (11, 13), (22, 13), (25, 17), (22, 22), (10, 22)), fill=STONE_L)
    d.polygon(((9, 22), (23, 22), (23, 26), (9, 26)), fill=STONE)
    d.polygon(((11, 17), (16, 14), (21, 17), (19, 21), (13, 21)), fill=COPPER_D)
    d.polygon(((14, 18), (16, 16), (18, 18), (17, 20), (15, 20)), fill=CYAN_D)
    d.point((16, 18), fill=CYAN); d.line((9, 23, 23, 23), fill=STONE_L)
    if state == "damaged":
        d.polygon(((21, 13), (26, 16), (29, 24), (25, 27), (20, 23)), fill=TRANSPARENT)
        d.line((17, 19, 15, 23, 17, 27), fill=MORTAR)
        d.rectangle((24, 26, 29, 29), fill=STONE_D)
    return im


def loot_chest(state: str):
    im = canvas(); d = ImageDraw.Draw(im)
    if state == "empty":
        d.polygon(((4, 17), (8, 12), (24, 12), (28, 17), (26, 28), (6, 28)), fill=INK)
        d.polygon(((7, 18), (9, 15), (23, 15), (25, 18), (23, 25), (9, 25)), fill=WOOD_D)
        d.rectangle((10, 17, 22, 23), fill=INK); d.line((11, 18, 21, 18), fill=IRON_L)
        return im
    d.polygon(((4, 14), (9, 8), (24, 8), (29, 14), (26, 27), (6, 27)), fill=INK)
    d.polygon(((7, 14), (10, 10), (23, 10), (26, 14), (23, 18), (10, 18)), fill=WOOD_L)
    d.polygon(((8, 18), (24, 18), (23, 24), (9, 24)), fill=WOOD)
    d.rectangle((9, 21, 23, 23), fill=WOOD_D)
    d.rectangle((10, 9, 11, 24), fill=IRON); d.rectangle((21, 9, 22, 24), fill=IRON)
    d.rectangle((14, 17, 18, 21), fill=COPPER_D); d.rectangle((15, 18, 17, 20), fill=GOLD)
    if state == "open":
        d.polygon(((5, 11), (9, 4), (23, 4), (27, 11), (24, 14), (8, 14)), fill=INK)
        d.polygon(((8, 10), (10, 6), (22, 6), (24, 10), (22, 12), (10, 12)), fill=WOOD)
        d.rectangle((9, 15, 23, 22), fill=INK); d.rectangle((11, 16, 21, 17), fill=GOLD)
    return im


def save_asset(asset_id: str, im: Image.Image, kind: str, colour_limit: int):
    OUT.mkdir(parents=True, exist_ok=True); QA.mkdir(parents=True, exist_ok=True)
    path = OUT / f"{asset_id}.png"; im.save(path)
    pixels = list(im.getdata()); visible = [p for p in pixels if p[3]]
    alpha_values = sorted({p[3] for p in pixels}); colours = len({p for p in visible})
    bounds = im.getchannel("A").getbbox()
    floor = kind == "floor"
    margin_ok = floor or bounds is not None and bounds[0] >= 1 and bounds[1] >= 1 and bounds[2] <= 31 and bounds[3] <= 31
    ok = im.size == (32, 32) and set(alpha_values).issubset({0, 255}) and colours <= colour_limit and margin_ok
    return {"asset_id": asset_id, "kind": kind, "size": [32, 32], "alpha_values": alpha_values,
            "visible_colours": colours, "colour_limit": colour_limit, "bounds": list(bounds) if bounds else None,
            "safety_margin": margin_ok, "status": "PASS" if ok else "FAIL"}


def contact_sheet(ids, name, columns=6, scale=4):
    cell = 32 * scale; label_h = 18
    rows = (len(ids) + columns - 1) // columns
    sheet = Image.new("RGBA", (columns * cell, rows * (cell + label_h)), (30, 28, 25, 255))
    draw = ImageDraw.Draw(sheet)
    for index, asset_id in enumerate(ids):
        x = index % columns * cell; y = index // columns * (cell + label_h)
        im = Image.open(OUT / f"{asset_id}.png").resize((cell, cell), Image.Resampling.NEAREST)
        checker = Image.new("RGBA", (cell, cell), (72, 68, 61, 255))
        cd = ImageDraw.Draw(checker)
        for yy in range(0, cell, 16):
            for xx in range(0, cell, 16):
                if (xx // 16 + yy // 16) % 2: cd.rectangle((xx, yy, xx + 15, yy + 15), fill=(91, 86, 76, 255))
        checker.alpha_composite(im); sheet.alpha_composite(checker, (x, y))
        draw.text((x + 2, y + cell + 2), asset_id.replace("academy_", "")[:25], fill=(230, 221, 195, 255))
    sheet.save(QA / name)


def mixed_battlefield(floor_ids, prop_ids):
    cell = 64; board = Image.new("RGBA", (12 * cell, 9 * cell), (0, 0, 0, 255))
    for y in range(9):
        for x in range(12):
            floor_id = floor_ids[(x * 3 + y * 5) % len(floor_ids)]
            tile = Image.open(OUT / f"{floor_id}.png").resize((cell, cell), Image.Resampling.NEAREST)
            board.alpha_composite(tile, (x * cell, y * cell))
    placements = ((1, 2), (3, 5), (5, 2), (7, 5), (9, 2), (10, 6), (2, 7), (6, 7))
    for asset_id, (x, y) in zip(prop_ids, placements):
        prop = Image.open(OUT / f"{asset_id}.png").resize((cell, cell), Image.Resampling.NEAREST)
        board.alpha_composite(prop, (x * cell, y * cell))
    board.save(QA / "battlefield_mixed_12x9_2x.png")


def main():
    entries = []
    ids = []
    for family, maker, variants in (
        ("academy_stone_road", stone_road, range(4)),
        ("academy_courtyard", courtyard, range(4)),
        ("academy_ruins", ruins, range(4)),
        ("academy_aether_inlay", inlay, range(4)),
    ):
        for variant in variants:
            asset_id = f"{family}_{chr(97 + variant)}"; ids.append(asset_id)
            entries.append(save_asset(asset_id, maker(variant), "floor", 8))
    for variant in range(3):
        asset_id = f"academy_packed_earth_{chr(97 + variant)}"; ids.append(asset_id)
        entries.append(save_asset(asset_id, packed_earth(variant), "floor", 8))
    for direction in "nesw":
        asset_id = f"academy_grass_edge_{direction}"; ids.append(asset_id)
        entries.append(save_asset(asset_id, grass_edge(direction), "floor", 8))
    for kind in ("straight", "corner", "tee", "cross"):
        asset_id = f"academy_aether_line_{kind}"; ids.append(asset_id)
        entries.append(save_asset(asset_id, connector(kind), "floor", 9))

    prop_families = (("academy_light_stone_bench", bench), ("academy_light_planter", planter),
                     ("academy_heavy_archive_stack", archive_stack), ("academy_heavy_masonry_screen", masonry_screen),
                     ("academy_aether_pillar", aether_pillar), ("academy_seal_plinth", seal_plinth))
    for family, maker in prop_families:
        for state in ("intact", "damaged", "rubble"):
            asset_id = f"{family}_{state}"; ids.append(asset_id)
            entries.append(save_asset(asset_id, maker(state), "prop", 12))
    for state in ("closed", "open", "empty"):
        asset_id = f"academy_loot_chest_{state}"; ids.append(asset_id)
        entries.append(save_asset(asset_id, loot_chest(state), "prop", 12))

    contact_sheet(ids, "candidate_48_contact_4x.png")
    floor_ids = [entry["asset_id"] for entry in entries if entry["kind"] == "floor" and "grass_edge" not in entry["asset_id"]]
    prop_ids = ["academy_light_stone_bench_intact", "academy_light_planter_intact",
                "academy_heavy_archive_stack_intact", "academy_heavy_masonry_screen_intact",
                "academy_aether_pillar_intact", "academy_seal_plinth_intact",
                "academy_loot_chest_closed", "academy_light_stone_bench_damaged"]
    mixed_battlefield(floor_ids[:16], prop_ids)
    report = {"batch": "battlefield_reset_v08", "source": str(Path(__file__)), "asset_count": len(entries),
              "status": "PASS" if all(entry["status"] == "PASS" for entry in entries) else "FAIL", "entries": entries}
    (QA / "battlefield_reset_v08_report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"assets": len(entries), "status": report["status"],
                      "failures": [entry["asset_id"] for entry in entries if entry["status"] != "PASS"]}))


if __name__ == "__main__":
    main()
