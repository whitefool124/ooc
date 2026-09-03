from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "UnityProject/Assets/Game/Resources/Art"
BOARD_DIR = ART / "FormalMapBoard"
FRAME_DIR = ART / "FormalMapNodeFrames77x39"
REGION_DIR = ART / "FormalMapRegionIcons32"
ROUTE_DIR = ART / "FormalMapRoute8"
QA_DIR = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A14"

CLEAR = (0, 0, 0, 0)
INK = (5, 9, 12, 255)
DEEP = (11, 20, 25, 255)
SLATE = (20, 34, 40, 255)
STEEL = (43, 65, 72, 255)
PALE = (144, 169, 173, 255)
WHITE = (216, 230, 230, 255)
CYAN = (67, 184, 205, 255)
AMBER = (220, 157, 62, 255)
SAFE = (69, 157, 130, 255)
DANGER = (177, 67, 58, 255)


def board() -> Image.Image:
    image = Image.new("RGBA", (670, 393), DEEP)
    draw = ImageDraw.Draw(image)
    draw.rectangle((0, 0, 669, 392), fill=INK)
    draw.rectangle((3, 3, 666, 389), fill=DEEP, outline=STEEL)
    draw.rectangle((7, 7, 662, 385), outline=(31, 52, 59, 255))

    # Six deliberately quiet operational sectors; routes and nodes remain dominant.
    sectors = (
        ((10, 10, 276, 112), (18, 35, 42, 255), CYAN),
        ((282, 10, 500, 112), (31, 32, 35, 255), AMBER),
        ((506, 10, 660, 382), (29, 25, 30, 255), DANGER),
        ((10, 118, 276, 240), (20, 31, 37, 255), PALE),
        ((10, 246, 330, 382), (28, 31, 28, 255), SAFE),
        ((336, 118, 500, 382), (18, 31, 34, 255), CYAN),
    )
    for index, (box, fill, accent) in enumerate(sectors):
        draw.rectangle(box, fill=fill)
        draw.line((box[0], box[1], box[2], box[1]), fill=accent, width=2)
        draw.rectangle((box[0] + 8, box[1] + 8, box[0] + 14, box[1] + 14), outline=accent)
        draw.line((box[0] + 18, box[1] + 11, box[0] + 42, box[1] + 11), fill=STEEL)
        # Sparse survey ticks, not decoration noise.
        for x in range(box[0] + 18, box[2] - 8, 32):
            draw.point((x, box[3] - 8), fill=STEEL)
        draw.rectangle((box[2] - 17, box[1] + 8, box[2] - 10, box[1] + 15), fill=INK, outline=accent)
        draw.point((box[2] - 13, box[1] + 11), fill=WHITE)

    # Aether surveying lattice and industrial registration marks.
    for x in range(20, 650, 40):
        draw.line((x, 18, x, 374), fill=(23, 42, 48, 255))
    for y in range(22, 375, 35):
        draw.line((18, y, 650, y), fill=(23, 42, 48, 255))
    for x, y in ((18, 18), (645, 18), (18, 368), (645, 368)):
        draw.rectangle((x, y, x + 8, y + 8), fill=INK, outline=AMBER)
        draw.rectangle((x + 3, y + 3, x + 5, y + 5), fill=WHITE)
    draw.line((12, 376, 210, 376), fill=STEEL)
    for x in range(12, 211, 10):
        draw.line((x, 374, x, 378), fill=PALE)
    draw.rectangle((226, 372, 330, 380), fill=INK, outline=STEEL)
    for x in (232, 242, 252, 270, 280, 290, 308, 318):
        draw.rectangle((x, 375, x + 4, 377), fill=CYAN)
    return image


def node_frame(name: str) -> Image.Image:
    image = Image.new("RGBA", (77, 39), CLEAR)
    draw = ImageDraw.Draw(image)
    styles = {
        "current": (CYAN, (18, 43, 49, 255)),
        "available": (CYAN, (14, 34, 40, 255)),
        "locked": (DANGER, (31, 23, 25, 255)),
        "cleared": (SAFE, (19, 35, 31, 255)),
        "visited": (SAFE, (18, 30, 30, 255)),
        "known": (AMBER, (35, 31, 23, 255)),
        "unknown": (STEEL, (15, 23, 27, 255)),
    }
    accent, fill = styles[name]
    draw.polygon(((3, 0), (73, 0), (76, 3), (76, 35), (72, 38), (3, 38), (0, 35), (0, 3)), fill=INK)
    draw.polygon(((4, 2), (71, 2), (74, 4), (74, 33), (70, 36), (4, 36), (2, 34), (2, 4)), fill=fill)
    draw.line((5, 3, 68, 3), fill=accent)
    draw.line((3, 5, 3, 31), fill=STEEL)
    draw.rectangle((68, 7, 72, 11), fill=INK, outline=accent)
    draw.point((70, 9), fill=WHITE)
    if name == "current":
        draw.rectangle((6, 32, 64, 34), fill=CYAN)
        for x in range(8, 65, 8): draw.rectangle((x, 31, x + 3, 35), fill=WHITE)
    elif name == "available":
        for x in range(7, 65, 8): draw.rectangle((x, 33, x + 4, 34), fill=CYAN)
    elif name == "locked":
        draw.line((7, 33, 64, 33), fill=DANGER)
    elif name == "cleared":
        draw.rectangle((7, 32, 64, 34), fill=SAFE)
    elif name == "visited":
        for x in range(7, 65, 12): draw.rectangle((x, 33, x + 6, 34), fill=SAFE)
    elif name == "known":
        draw.line((7, 33, 64, 33), fill=AMBER)
    else:
        for x in range(7, 65, 10): draw.rectangle((x, 33, x + 2, 34), fill=STEEL)
        draw.line((8, 8, 63, 29), fill=(32, 47, 52, 255))
    return image


def region_icon(name: str) -> Image.Image:
    image = Image.new("RGBA", (32, 32), CLEAR)
    d = ImageDraw.Draw(image)
    d.polygon(((16, 2), (27, 7), (29, 20), (22, 28), (10, 28), (3, 20), (5, 7)), fill=INK)
    d.polygon(((16, 5), (24, 9), (26, 19), (20, 25), (12, 25), (6, 19), (8, 9)), fill=SLATE, outline=STEEL)
    if name == "courtyard_dormitory":
        d.rectangle((10, 11, 22, 22), fill=PALE); d.rectangle((13, 15, 15, 22), fill=DEEP); d.rectangle((18, 15, 20, 22), fill=DEEP); d.rectangle((14, 7, 18, 11), fill=CYAN)
    elif name == "teaching_archive":
        d.polygon(((9, 10), (16, 7), (23, 10), (23, 22), (16, 25), (9, 22)), fill=AMBER); d.line((16, 9, 16, 23), fill=DEEP, width=2); d.line((11, 13, 14, 12), fill=WHITE)
    elif name == "training_workshop":
        d.ellipse((9, 9, 23, 23), fill=STEEL, outline=PALE); d.rectangle((14, 6, 18, 26), fill=INK); d.rectangle((6, 14, 26, 18), fill=INK); d.rectangle((14, 14, 18, 18), fill=AMBER)
    elif name == "market_infirmary":
        d.rectangle((9, 12, 23, 21), fill=SAFE); d.rectangle((14, 7, 18, 26), fill=WHITE); d.rectangle((7, 14, 25, 18), fill=WHITE); d.rectangle((15, 15, 17, 17), fill=DANGER)
    elif name == "campus_wilds":
        d.polygon(((16, 7), (24, 15), (19, 16), (24, 22), (17, 21), (16, 26), (14, 21), (8, 23), (12, 16), (7, 15)), fill=SAFE); d.line((16, 10, 16, 24), fill=WHITE)
    elif name == "sealed_tower":
        d.rectangle((11, 9, 21, 24), fill=DANGER); d.rectangle((9, 7, 23, 11), fill=PALE); d.rectangle((13, 13, 19, 24), fill=INK); d.rectangle((15, 15, 17, 18), fill=CYAN)
    else:
        raise KeyError(name)
    return image


def route_joint() -> Image.Image:
    image = Image.new("RGBA", (8, 8), CLEAR); d = ImageDraw.Draw(image)
    d.rectangle((2, 2, 5, 5), fill=INK); d.rectangle((0, 3, 7, 4), fill=CYAN); d.rectangle((3, 0, 4, 7), fill=CYAN); d.rectangle((3, 3, 4, 4), fill=WHITE)
    return image


def generate() -> list[dict]:
    for folder in (BOARD_DIR, FRAME_DIR, REGION_DIR, ROUTE_DIR, QA_DIR): folder.mkdir(parents=True, exist_ok=True)
    assets: dict[str, tuple[Path, Image.Image, tuple[int, int], int]] = {
        "map_board.academy_network": (BOARD_DIR / "academy_network_board.png", board(), (670, 393), 32),
        **{f"map_node_frame.{name}": (FRAME_DIR / f"{name}.png", node_frame(name), (77, 39), 32) for name in ("current", "available", "locked", "cleared", "visited", "known", "unknown")},
        **{f"map_region.{name}": (REGION_DIR / f"{name}.png", region_icon(name), (32, 32), 32) for name in ("courtyard_dormitory", "teaching_archive", "training_workshop", "market_infirmary", "campus_wilds", "sealed_tower")},
        "map_route.joint": (ROUTE_DIR / "route_joint.png", route_joint(), (8, 8), 32),
    }
    records = []
    for asset_id, (path, image, expected, ppu) in assets.items():
        image.save(path, optimize=True)
        alpha = sorted(set(image.getchannel("A").getdata()))
        colors = len(set(image.getdata()))
        passed = image.size == expected and all(value in (0, 255) for value in alpha)
        records.append({"id": asset_id, "path": str(path.relative_to(ROOT)).replace("\\", "/"), "size": list(image.size), "colors": colors, "alphaValues": alpha, "ppu": ppu, "sha256": hashlib.sha256(path.read_bytes()).hexdigest(), "status": "PASS" if passed else "FAIL"})

    sheet = Image.new("RGBA", (1340, 980), INK); d = ImageDraw.Draw(sheet)
    sheet.alpha_composite(assets["map_board.academy_network"][1].resize((1340, 786), Image.Resampling.NEAREST), (0, 0))
    for index, name in enumerate(("current", "available", "locked", "cleared", "visited", "known", "unknown")):
        x = 10 + index * 188
        sheet.alpha_composite(assets[f"map_node_frame.{name}"][1].resize((154, 78), Image.Resampling.NEAREST), (x, 800)); d.text((x, 882), name, fill=WHITE)
    for index, name in enumerate(("courtyard_dormitory", "teaching_archive", "training_workshop", "market_infirmary", "campus_wilds", "sealed_tower")):
        x = 36 + index * 215
        sheet.alpha_composite(assets[f"map_region.{name}"][1].resize((64, 64), Image.Resampling.NEAREST), (x, 906)); d.text((x + 70, 930), name.split("_")[0], fill=WHITE)
    sheet.save(QA_DIR / "OCC_M-A14_学院大地图正式资产_QA_v01.png", optimize=True)
    report = {"schema": "occ.formal.academy-map-art.v0.1", "status": "QA_PASS" if all(r["status"] == "PASS" for r in records) else "QA_FAIL", "assetCount": len(records), "rules": {"hardAlpha": True, "filter": "Point", "wrap": "Clamp", "mipmap": False, "ppu": 32, "boardDisplayScale": {"1920x1080": 2, "960x540": 1}}, "records": records}
    (QA_DIR / "OCC_M-A14_学院大地图正式资产_QA_v01.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    return records


if __name__ == "__main__":
    result = generate()
    print(f"generated={len(result)} passed={sum(r['status'] == 'PASS' for r in result)} qa={QA_DIR}")
