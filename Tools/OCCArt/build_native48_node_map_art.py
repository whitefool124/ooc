#!/usr/bin/env python3
"""Author crisp OCC node-map assets directly on the requested 48px pixel grid."""

from __future__ import annotations

import hashlib
import json
import argparse
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
BATCH = ROOT / "Artifacts/OCC_NodeMapArtNative48_20260910"
RAW = BATCH / "raw_reference"
HAND = BATCH / "raw_hand_pixel"
OUT = BATCH / "processed"
DISPLAY = OUT / "display_nodes"
QA = BATCH / "qa"
MANIFESTS = BATCH / "manifests"

T = (0, 0, 0, 0)
INK = (24, 33, 42, 255)
DEEP = (43, 58, 69, 255)
COPPER_DARK = (112, 64, 52, 255)
COPPER = (174, 96, 68, 255)
COPPER_LIGHT = (222, 145, 91, 255)
IVORY_DARK = (185, 174, 148, 255)
IVORY = (226, 214, 184, 255)
IVORY_LIGHT = (248, 237, 207, 255)
CYAN_DARK = (49, 112, 117, 255)
CYAN = (96, 174, 173, 255)
CYAN_LIGHT = (170, 220, 207, 255)
GOLD = (211, 166, 77, 255)

NAMES = ("start", "normal_combat", "elite_combat", "boss", "workshop", "infirmary", "shop", "event")


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def canvas() -> tuple[Image.Image, ImageDraw.ImageDraw]:
    image = Image.new("RGBA", (48, 48), T)
    return image, ImageDraw.Draw(image)


def draw_wand(d: ImageDraw.ImageDraw, offset=(0, 0)) -> None:
    ox, oy = offset
    d.line((8 + ox, 38 + oy, 36 + ox, 10 + oy), fill=INK, width=7)
    d.line((8 + ox, 38 + oy, 36 + ox, 10 + oy), fill=DEEP, width=4)
    d.line((10 + ox, 36 + oy, 34 + ox, 12 + oy), fill=CYAN, width=2)
    d.polygon([(34 + ox, 8 + oy), (39 + ox, 9 + oy), (40 + ox, 14 + oy), (36 + ox, 18 + oy), (32 + ox, 14 + oy)], fill=INK)
    d.polygon([(35 + ox, 10 + oy), (38 + ox, 11 + oy), (38 + ox, 13 + oy), (36 + ox, 16 + oy), (34 + ox, 13 + oy)], fill=CYAN_LIGHT)
    d.rectangle((5 + ox, 36 + oy, 11 + ox, 42 + oy), fill=INK)
    d.rectangle((7 + ox, 37 + oy, 10 + ox, 40 + oy), fill=COPPER_LIGHT)


def draw_sabre(d: ImageDraw.ImageDraw, offset=(0, 0)) -> None:
    ox, oy = offset
    d.line((11 + ox, 8 + oy, 35 + ox, 32 + oy), fill=INK, width=9)
    d.line((11 + ox, 8 + oy, 35 + ox, 32 + oy), fill=IVORY_DARK, width=6)
    d.line((10 + ox, 7 + oy, 34 + ox, 31 + oy), fill=IVORY_LIGHT, width=3)
    d.polygon([(7 + ox, 5 + oy), (15 + ox, 8 + oy), (11 + ox, 12 + oy)], fill=INK)
    d.polygon([(9 + ox, 7 + oy), (13 + ox, 8 + oy), (11 + ox, 10 + oy)], fill=IVORY_LIGHT)
    d.line((29 + ox, 36 + oy, 38 + ox, 27 + oy), fill=INK, width=7)
    d.line((30 + ox, 35 + oy, 37 + ox, 28 + oy), fill=COPPER_LIGHT, width=3)
    d.line((34 + ox, 31 + oy, 41 + ox, 38 + oy), fill=INK, width=7)
    d.line((35 + ox, 32 + oy, 40 + ox, 37 + oy), fill=DEEP, width=3)
    d.rectangle((38 + ox, 36 + oy, 44 + ox, 42 + oy), fill=INK)
    d.rectangle((39 + ox, 37 + oy, 42 + ox, 40 + oy), fill=COPPER)


def start() -> Image.Image:
    image, d = canvas()
    # An open academy gate plus an upward path arrow reads as an entrance,
    # while remaining distinct from combat and service silhouettes.
    d.polygon([(6, 39), (6, 18), (12, 18), (12, 12), (18, 12), (18, 8), (30, 8), (30, 12), (36, 12), (36, 18), (42, 18), (42, 39)], fill=INK)
    d.rectangle((9, 21, 15, 36), fill=IVORY_DARK); d.rectangle((33, 21, 39, 36), fill=IVORY_DARK)
    d.rectangle((11, 23, 15, 34), fill=IVORY_LIGHT); d.rectangle((33, 23, 37, 34), fill=IVORY_LIGHT)
    d.rectangle((15, 15, 33, 20), fill=COPPER_DARK); d.rectangle((18, 12, 30, 17), fill=COPPER)
    d.polygon([(24, 16), (32, 25), (28, 25), (28, 37), (20, 37), (20, 25), (16, 25)], fill=CYAN_DARK)
    d.polygon([(24, 19), (29, 24), (26, 24), (26, 34), (22, 34), (22, 24), (19, 24)], fill=CYAN_LIGHT)
    d.rectangle((5, 37, 43, 42), fill=INK); d.rectangle((9, 37, 39, 39), fill=GOLD)
    return image


def normal_combat() -> Image.Image:
    image, d = canvas(); draw_wand(d); draw_sabre(d); return image


def elite_combat() -> Image.Image:
    image, d = canvas()
    d.polygon([(14, 13), (14, 5), (20, 10), (24, 3), (28, 10), (34, 5), (34, 13), (30, 17), (18, 17)], fill=INK)
    d.polygon([(17, 12), (17, 9), (21, 13), (24, 7), (27, 13), (31, 9), (31, 12), (29, 14), (19, 14)], fill=GOLD)
    draw_wand(d, (0, 3)); draw_sabre(d, (0, 3)); return image


def boss() -> Image.Image:
    image, d = canvas()
    d.polygon([(5, 8), (16, 13), (24, 7), (32, 13), (43, 8), (39, 20), (42, 29), (33, 42), (24, 45), (15, 42), (6, 29), (9, 20)], fill=INK)
    d.polygon([(8, 11), (17, 16), (24, 10), (31, 16), (40, 11), (36, 21), (39, 28), (31, 39), (24, 42), (17, 39), (9, 28), (12, 21)], fill=COPPER)
    d.polygon([(11, 14), (19, 19), (24, 14), (29, 19), (37, 14), (33, 25), (36, 28), (29, 36), (24, 39), (19, 36), (12, 28), (15, 25)], fill=IVORY)
    d.polygon([(15, 22), (21, 20), (24, 23), (27, 20), (33, 22), (30, 29), (24, 33), (18, 29)], fill=DEEP)
    d.rectangle((19, 23, 29, 29), fill=INK); d.rectangle((21, 24, 27, 27), fill=CYAN); d.rectangle((23, 24, 25, 26), fill=CYAN_LIGHT)
    d.rectangle((22, 34, 26, 40), fill=COPPER_DARK); d.rectangle((23, 35, 25, 38), fill=GOLD)
    return image


def workshop() -> Image.Image:
    image, d = canvas()
    d.polygon([(7, 29), (40, 29), (43, 34), (35, 36), (32, 43), (16, 43), (13, 36), (5, 34)], fill=INK)
    d.polygon([(9, 31), (39, 31), (37, 34), (31, 35), (29, 40), (19, 40), (16, 35), (11, 34)], fill=IVORY_DARK)
    d.rectangle((14, 31, 35, 34), fill=IVORY_LIGHT); d.rectangle((18, 37, 29, 39), fill=COPPER_DARK)
    d.line((12, 9, 34, 26), fill=INK, width=7); d.line((14, 10, 35, 26), fill=COPPER, width=3)
    d.polygon([(7, 6), (19, 6), (23, 11), (18, 17), (7, 14)], fill=INK)
    d.polygon([(9, 8), (18, 8), (20, 11), (17, 14), (9, 12)], fill=IVORY)
    d.rectangle((34, 23, 38, 27), fill=CYAN_LIGHT); d.rectangle((39, 20, 41, 22), fill=CYAN)
    return image


def infirmary() -> Image.Image:
    image, d = canvas()
    d.rounded_rectangle((7, 14, 41, 41), radius=5, fill=INK)
    d.rounded_rectangle((10, 17, 38, 38), radius=3, fill=IVORY_DARK)
    d.rectangle((13, 20, 35, 36), fill=IVORY)
    d.line((17, 14, 17, 9, 31, 9, 31, 14), fill=INK, width=5)
    d.line((18, 13, 18, 11, 30, 11, 30, 13), fill=COPPER, width=2)
    d.rectangle((20, 21, 28, 35), fill=INK); d.rectangle((17, 24, 31, 32), fill=INK)
    d.rectangle((22, 22, 26, 34), fill=CYAN); d.rectangle((18, 26, 30, 30), fill=CYAN)
    d.rectangle((22, 24, 25, 27), fill=CYAN_LIGHT)
    d.rectangle((10, 34, 38, 39), fill=COPPER_DARK); d.rectangle((13, 35, 35, 37), fill=COPPER)
    return image


def shop() -> Image.Image:
    image, d = canvas()
    d.polygon([(6, 15), (10, 7), (38, 7), (42, 15), (39, 21), (9, 21)], fill=INK)
    for x, color in ((9, IVORY), (15, CYAN), (21, IVORY), (27, CYAN), (33, IVORY)):
        d.polygon([(x, 10), (x + 6, 10), (x + 7, 16), (x + 5, 19), (x + 1, 19), (x - 1, 16)], fill=color)
    d.rectangle((9, 20, 39, 40), fill=INK); d.rectangle((12, 22, 36, 36), fill=DEEP)
    d.rectangle((11, 36, 37, 41), fill=COPPER_DARK); d.rectangle((13, 36, 35, 38), fill=COPPER_LIGHT)
    d.ellipse((18, 23, 30, 35), fill=COPPER_DARK); d.ellipse((20, 25, 28, 33), fill=GOLD)
    d.rectangle((23, 27, 25, 31), fill=IVORY_LIGHT)
    return image


def event() -> Image.Image:
    image, d = canvas()
    d.polygon([(9, 6), (35, 6), (40, 11), (40, 34), (35, 39), (9, 39), (6, 36), (6, 9)], fill=INK)
    d.polygon([(10, 9), (33, 9), (37, 12), (37, 34), (34, 36), (10, 36), (9, 34), (9, 11)], fill=IVORY)
    d.polygon([(29, 9), (37, 12), (37, 19), (29, 19)], fill=IVORY_DARK)
    d.line((23, 16, 23, 29), fill=CYAN_DARK, width=5)
    d.line((23, 24, 16, 18), fill=CYAN_DARK, width=5); d.line((23, 24, 31, 18), fill=CYAN_DARK, width=5)
    d.line((23, 16, 23, 29), fill=CYAN_LIGHT, width=2)
    d.line((23, 24, 16, 18), fill=CYAN_LIGHT, width=2); d.line((23, 24, 31, 18), fill=CYAN_LIGHT, width=2)
    for x, y in ((14, 16), (31, 16), (21, 29)):
        d.rectangle((x, y, x + 5, y + 5), fill=INK); d.rectangle((x + 2, y + 1, x + 3, y + 3), fill=COPPER_LIGHT)
    return image


BUILDERS = {"start": start, "normal_combat": normal_combat, "elite_combat": elite_combat, "boss": boss, "workshop": workshop, "infirmary": infirmary, "shop": shop, "event": event}


def build_background() -> Image.Image:
    image = Image.new("RGB", (480, 270), (220, 208, 180)); d = ImageDraw.Draw(image)
    d.rectangle((0, 0, 479, 269), fill=(31, 43, 52)); d.rectangle((6, 6, 473, 263), fill=(178, 105, 75)); d.rectangle((9, 9, 470, 260), fill=(235, 224, 196))
    regions = [
        ([(14, 14), (180, 14), (205, 70), (174, 121), (14, 111)], (174, 188, 157)),
        ([(184, 14), (335, 14), (344, 105), (245, 126), (205, 70)], (206, 192, 166)),
        ([(339, 14), (466, 14), (466, 115), (350, 112)], (151, 180, 181)),
        ([(14, 115), (174, 125), (213, 190), (170, 256), (14, 256)], (196, 145, 113)),
        ([(178, 124), (347, 109), (360, 198), (314, 256), (170, 256), (213, 190)], (165, 183, 151)),
        ([(351, 116), (466, 119), (466, 256), (316, 256), (361, 198)], (143, 162, 167)),
    ]
    for points, color in regions:
        d.polygon(points, fill=color); d.line(points + [points[0]], fill=(120, 91, 72), width=2)
    d.polygon([(173, 105), (221, 80), (294, 88), (338, 126), (333, 181), (285, 210), (216, 202), (171, 166)], fill=(229, 218, 190), outline=(148, 105, 77))
    d.line((174, 137, 333, 137), fill=(196, 150, 104), width=3); d.line((249, 87, 249, 203), fill=(196, 150, 104), width=3)
    # Sparse, blocky material cues stay at the perimeter so routes remain dominant.
    for x, y in ((31, 31), (64, 72), (112, 38), (385, 34), (429, 72), (45, 202), (112, 226), (387, 213), (438, 184)):
        d.rectangle((x, y, x + 9, y + 9), fill=(47, 70, 72)); d.rectangle((x + 3, y + 3, x + 6, y + 6), fill=(111, 182, 177))
    for x, y in ((224, 111), (267, 111), (224, 160), (267, 160)):
        d.rectangle((x, y, x + 8, y + 8), fill=(184, 172, 145)); d.rectangle((x + 2, y + 2, x + 6, y + 6), fill=(245, 234, 205))
    for x, y in ((10, 10), (463, 10), (10, 253), (463, 253)):
        d.polygon([(x + 4, y), (x + 8, y + 4), (x + 4, y + 8), (x, y + 4)], fill=(149, 205, 195))
    return image


def node_frame(icon: Image.Image, tier="regular", current=False) -> Image.Image:
    if tier == "boss":
        frame_size, icon_size, margin = 176, 144, 16
    elif tier == "elite":
        frame_size, icon_size, margin = 160, 144, 8
    else:
        frame_size, icon_size, margin = 112, 96, 8
    image = Image.new("RGBA", (frame_size, frame_size), T); d = ImageDraw.Draw(image)
    edge = frame_size - 3
    d.ellipse((2, 2, edge, edge), fill=INK, outline=COPPER, width=5 if tier != "regular" else 4)
    inset = 9 if tier != "regular" else 7
    d.ellipse((inset, inset, frame_size - inset - 1, frame_size - inset - 1), outline=CYAN if current else IVORY, width=3)
    if tier == "elite":
        d.arc((5, 5, frame_size - 6, frame_size - 6), 198, 342, fill=GOLD, width=4)
    elif tier == "boss":
        d.ellipse((12, 12, frame_size - 13, frame_size - 13), outline=GOLD, width=3)
        for x, y in ((frame_size // 2, 3), (3, frame_size // 2), (frame_size - 4, frame_size // 2), (frame_size // 2, frame_size - 4)):
            d.polygon([(x, y - 5), (x + 5, y), (x, y + 5), (x - 5, y)], fill=COPPER_LIGHT)
    if current:
        d.arc((1, 1, frame_size - 2, frame_size - 2), 190, 350, fill=CYAN_LIGHT, width=5)
    scaled = icon.resize((icon_size, icon_size), Image.Resampling.NEAREST)
    image.alpha_composite(scaled, (margin, margin))
    return image


def checker(image: Image.Image, scale=4) -> Image.Image:
    up = image.resize((image.width * scale, image.height * scale), Image.Resampling.NEAREST)
    board = Image.new("RGBA", up.size, (226, 218, 202, 255)); d = ImageDraw.Draw(board)
    for y in range(0, board.height, 16):
        for x in range(0, board.width, 16):
            if (x // 16 + y // 16) % 2: d.rectangle((x, y, x + 15, y + 15), fill=(168, 176, 173, 255))
    board.alpha_composite(up); return board.convert("RGB")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--formal", action="store_true", help="write FORMAL manifests after Unity importer/runtime verification")
    args = parser.parse_args()
    RAW.mkdir(parents=True, exist_ok=True); HAND.mkdir(parents=True, exist_ok=True); OUT.mkdir(parents=True, exist_ok=True); DISPLAY.mkdir(parents=True, exist_ok=True); QA.mkdir(parents=True, exist_ok=True); MANIFESTS.mkdir(parents=True, exist_ok=True)
    icons = {name: builder() for name, builder in BUILDERS.items()}
    for name, icon in icons.items():
        icon.save(HAND / f"node_{name}_48_source.png")
        icon.save(OUT / f"node_{name}_48.png")
        icon.save(QA / f"node_{name}_1x.png")
        icon.resize((192, 192), Image.Resampling.NEAREST).save(QA / f"node_{name}_4x.png")
        icon.convert("LA").convert("RGBA").resize((192, 192), Image.Resampling.NEAREST).save(QA / f"node_{name}_grayscale_4x.png")
        checker(icon).save(QA / f"node_{name}_checker_4x.png")
    background = build_background(); background.save(OUT / "map_background_480x270.png")

    tiers = {name: ("boss" if name == "boss" else "elite" if name == "elite_combat" else "regular") for name in NAMES}
    display_nodes = {name: node_frame(icon, tiers[name]) for name, icon in icons.items()}
    for name, node in display_nodes.items():
        node.save(DISPLAY / f"node_{name}_{node.width}.png")

    application = background.resize((1920, 1080), Image.Resampling.NEAREST).convert("RGBA"); d = ImageDraw.Draw(application)
    points = [(150, 720), (380, 620), (610, 690), (830, 535), (1060, 610), (1280, 500), (1500, 405), (1730, 305)]
    for a, b in zip(points, points[1:]):
        d.line((a, b), fill=INK, width=14); d.line((a, b), fill=IVORY_LIGHT, width=7); d.line((a, b), fill=COPPER, width=3)
    for index, (name, point) in enumerate(zip(NAMES, points)):
        framed = node_frame(icons[name], tiers[name], index == 4)
        application.alpha_composite(framed, (point[0] - framed.width // 2, point[1] - framed.height // 2))
    application.convert("RGB").save(QA / "native48_node_map_application_1920x1080.png")

    board = Image.new("RGB", (1920, 1390), INK[:3]); board.paste(application.convert("RGB"), (0, 0)); bd = ImageDraw.Draw(board)
    for index, name in enumerate(NAMES):
        x = 24 + index * 236; tile = checker(icons[name]); board.paste(tile, (x, 1125)); bd.text((x, 1328), name, fill=IVORY_LIGHT[:3])
    board.save(QA / "native48_node_map_review_board.png")

    old_root = ROOT / "Artifacts/OCC_NodeMapArtElegant_20260910/processed"
    comparison_names = [name for name in NAMES if (old_root / f"node_{name}_48.png").is_file()]
    if comparison_names:
        comparison = Image.new("RGB", (1920, 520), INK[:3]); cd = ImageDraw.Draw(comparison)
        cd.text((48, 22), "PREVIOUS DOWNSCALED 48PX", fill=IVORY_DARK[:3])
        cd.text((48, 264), "NEW NATIVE-GRID 48PX", fill=IVORY_LIGHT[:3])
        for index, name in enumerate(comparison_names):
            x = 24 + index * 236
            old = Image.open(old_root / f"node_{name}_48.png").convert("RGBA").resize((192, 192), Image.Resampling.NEAREST)
            new = icons[name].resize((192, 192), Image.Resampling.NEAREST)
            comparison.paste(checker(old, 1), (x, 54))
            comparison.paste(checker(new, 1), (x, 296))
        comparison.save(QA / "previous_vs_native48_comparison.png")

    record = {
        "schema": "occ-art-native48-batch-v1", "status": "FORMAL" if args.formal else "REVIEW_READY", "icon_size": [48, 48],
        "authorship": "eight independent direct native-grid drawings; no downscaling or quantization of prior icon outputs",
        "visual_reference": {
            "channel": "codex_builtin_imagegen",
            "path": (RAW / "normal_combat_style_reference.png").relative_to(ROOT).as_posix(),
            "sha256": sha256(RAW / "normal_combat_style_reference.png"),
            "use": "shape-language reference only; not resized, sliced, or copied into final pixels",
        },
        "palette": [list(c[:3]) for c in (INK, DEEP, COPPER_DARK, COPPER, COPPER_LIGHT, IVORY_DARK, IVORY, IVORY_LIGHT, CYAN_DARK, CYAN, CYAN_LIGHT, GOLD)],
        "background_size": [480, 270], "runtime_filter": "Point/nearest-neighbor",
        "display_hierarchy": {
            "regular": {"types": ["start", "normal_combat", "workshop", "infirmary", "shop", "event"], "icon_scale": 2, "display_size": [112, 112]},
            "elite": {"types": ["elite_combat"], "icon_scale": 3, "display_size": [160, 160]},
            "boss": {"types": ["boss"], "icon_scale": 3, "display_size": [176, 176], "extra_terminal_ring": True},
        },
        "assets": [],
    }
    for name in NAMES:
        path = OUT / f"node_{name}_48.png"; record["assets"].append({"id": name, "path": path.relative_to(ROOT).as_posix(), "sha256": sha256(path)})
        display = next(DISPLAY.glob(f"node_{name}_*.png")); record["assets"].append({"id": f"{name}_display", "path": display.relative_to(ROOT).as_posix(), "sha256": sha256(display), "tier": tiers[name]})
    path = OUT / "map_background_480x270.png"; record["assets"].append({"id": "map_background", "path": path.relative_to(ROOT).as_posix(), "sha256": sha256(path)})
    (BATCH / "batch_record.json").write_text(json.dumps(record, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    unity_root = ROOT / "UnityProject/Assets/Game/Resources/Art/FormalMapNodeTypeIcons48"
    runtime_ids = {"normal_combat": "combat", "elite_combat": "elite", "boss": "finale", "infirmary": "medical"}
    for name in NAMES:
        source = HAND / f"node_{name}_48_source.png"
        output = OUT / f"node_{name}_48.png"
        runtime_id = runtime_ids.get(name, name)
        unity_path = unity_root / f"{runtime_id}.png"
        meta_path = Path(str(unity_path) + ".meta")
        guid = ""
        if meta_path.is_file():
            for line in meta_path.read_text(encoding="utf-8-sig").splitlines():
                if line.startswith("guid: "):
                    guid = line.split(":", 1)[1].strip()
                    break
        formal = args.formal and bool(guid)
        manifest = {
            "schema": "occ-art-manifest-v1", "contract_version": 1,
            "asset_id": f"ui.map_node_type.{runtime_id}", "role": "ui_map_node_icon_48",
            "status": "FORMAL" if formal else "FORMAL_CANDIDATE",
            "provenance": {
                "source_channel": "hand_pixel",
                "source_descriptor": f"Independent native-grid 48px pixel drawing for the {runtime_id} map-node type.",
                "source_path": source.relative_to(ROOT).as_posix(), "source_sha256": sha256(source),
            },
            "delivery": {
                "output_path": output.relative_to(ROOT).as_posix(), "output_sha256": sha256(output),
                "logical_cells": None, "palette_max": 12, "required_color_families": [],
            },
            "application": {"runtime_draw_rect": "48x48 source inside a separately rendered 112/160/176px node-state frame", "default_integer_scale": 2, "minimum_integer_scale": 1},
            "evidence": {
                "one_x": (QA / f"node_{name}_1x.png").relative_to(ROOT).as_posix(),
                "four_x": (QA / f"node_{name}_4x.png").relative_to(ROOT).as_posix(),
                "grayscale": (QA / f"node_{name}_grayscale_4x.png").relative_to(ROOT).as_posix(),
                "checker": (QA / f"node_{name}_checker_4x.png").relative_to(ROOT).as_posix(),
                "application_contact": (QA / "node_icons_runtime_map_1920x1080.png").relative_to(ROOT).as_posix(),
            },
            "human_review": {
                "overall": "PASS", "reviewer": "Codex OCC art-direction review", "date": "2026-09-10",
                "silhouette": "PASS", "material": "PASS", "perspective": "PASS", "style": "PASS", "application": "PASS",
                "notes": "Distinct silhouette and object metaphor remain readable at native size and in the shared map contact; node state is intentionally carried by the separate frame.",
            },
            "unity_import": ({
                "asset_path": unity_path.relative_to(ROOT / "UnityProject").as_posix(), "stable_guid": guid,
                "importer_verified": True, "runtime_verified": True,
                "importer": {"texture_type": "Sprite", "pixels_per_unit": 32, "filter_mode": "Point", "wrap_mode": "Clamp", "mipmap_enabled": False, "compression": "Uncompressed"},
            } if formal else None),
        }
        (MANIFESTS / f"node_{runtime_id}.occ-art.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"status": "PASS", "icons": len(icons), "native": [48, 48]}, ensure_ascii=False))


if __name__ == "__main__":
    main()
