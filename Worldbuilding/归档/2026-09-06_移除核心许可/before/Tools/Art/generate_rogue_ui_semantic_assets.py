from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "UnityProject/Assets/Game/Resources/Art"
RESOURCE_DIR = ART / "FormalResourceIcons32"
SLOT_DIR = ART / "FormalEquipmentSlotIcons32"
STATE_DIR = ART / "FormalMapStateIcons16"
QA_DIR = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A11"

CLEAR = (0, 0, 0, 0)
INK = (5, 8, 11, 255)
DEEP = (12, 25, 33, 255)
STEEL = (48, 78, 91, 255)
LIGHT = (190, 207, 211, 255)
WHITE = (230, 239, 242, 255)
CYAN = (77, 199, 224, 255)
AMBER = (250, 184, 71, 255)
SAFE = (82, 184, 154, 255)
DANGER = (209, 87, 71, 255)


def canvas() -> tuple[Image.Image, ImageDraw.ImageDraw]:
    image = Image.new("RGBA", (32, 32), CLEAR)
    return image, ImageDraw.Draw(image)


def resource_icon(name: str) -> Image.Image:
    image, draw = canvas()
    if name == "gold":
        for box in ((5, 17, 20, 24), (11, 11, 26, 18), (6, 5, 21, 12)):
            draw.ellipse(box, fill=AMBER, outline=INK)
            draw.line((box[0] + 3, box[1] + 3, box[2] - 3, box[1] + 3), fill=WHITE)
    elif name == "contribution":
        draw.polygon(((16, 3), (24, 7), (27, 16), (23, 25), (16, 29), (9, 25), (5, 16), (8, 7)), fill=INK)
        draw.polygon(((16, 6), (22, 9), (24, 16), (21, 22), (16, 26), (11, 22), (8, 16), (10, 9)), fill=SAFE)
        draw.rectangle((12, 12, 14, 20), fill=WHITE); draw.rectangle((16, 9, 18, 20), fill=WHITE); draw.rectangle((20, 14, 22, 20), fill=WHITE)
    elif name == "stage_time":
        draw.ellipse((4, 4, 27, 27), fill=INK)
        draw.ellipse((7, 7, 24, 24), fill=DEEP, outline=CYAN)
        for x, y in ((15, 8), (23, 15), (15, 23), (8, 15)): draw.rectangle((x, y, x + 1, y + 1), fill=LIGHT)
        draw.line((16, 16, 16, 10), fill=WHITE, width=2); draw.line((16, 16, 21, 18), fill=AMBER, width=2)
    elif name == "explored":
        draw.polygon(((4, 7), (12, 4), (20, 7), (28, 4), (28, 25), (20, 28), (12, 25), (4, 28)), fill=INK)
        draw.polygon(((7, 9), (12, 7), (19, 10), (25, 7), (25, 22), (20, 25), (12, 22), (7, 25)), fill=STEEL)
        draw.line((12, 7, 12, 22), fill=LIGHT); draw.line((20, 10, 20, 25), fill=LIGHT)
        draw.line((9, 20, 15, 15, 22, 18), fill=CYAN, width=2)
        for x, y in ((9, 20), (15, 15), (22, 18)): draw.rectangle((x - 1, y - 1, x + 1, y + 1), fill=WHITE)
    elif name == "core_permit":
        draw.polygon(((4, 8), (8, 8), (10, 5), (26, 5), (28, 8), (28, 24), (24, 27), (8, 27), (4, 23)), fill=INK)
        draw.polygon(((7, 10), (11, 10), (12, 8), (24, 8), (25, 10), (25, 22), (22, 24), (9, 24), (7, 22)), fill=AMBER)
        draw.rectangle((11, 12, 21, 14), fill=DEEP); draw.rectangle((11, 17, 18, 19), fill=DEEP); draw.rectangle((20, 17, 22, 19), fill=WHITE)
    elif name == "risk":
        draw.polygon(((16, 3), (30, 27), (2, 27)), fill=INK)
        draw.polygon(((16, 7), (26, 24), (6, 24)), fill=DANGER)
        draw.rectangle((15, 12, 17, 18), fill=WHITE); draw.rectangle((15, 21, 17, 23), fill=WHITE)
    elif name == "weight":
        draw.rectangle((12, 4, 20, 7), fill=INK); draw.rectangle((10, 7, 22, 10), fill=LIGHT, outline=INK)
        draw.polygon(((8, 10), (24, 10), (28, 27), (4, 27)), fill=INK)
        draw.polygon(((10, 13), (22, 13), (24, 24), (8, 24)), fill=STEEL)
        draw.rectangle((14, 16, 18, 20), fill=WHITE)
    elif name == "aether_load":
        draw.rectangle((5, 8, 27, 24), fill=INK)
        draw.rectangle((8, 11, 24, 21), fill=STEEL)
        draw.rectangle((11, 8, 13, 24), fill=LIGHT); draw.rectangle((19, 8, 21, 24), fill=LIGHT)
        draw.rectangle((14, 12, 18, 20), fill=CYAN); draw.rectangle((15, 14, 17, 18), fill=WHITE)
    elif name == "charges":
        draw.rectangle((10, 3, 22, 6), fill=INK); draw.rectangle((8, 6, 24, 28), fill=INK)
        draw.rectangle((11, 9, 21, 25), fill=DEEP)
        for y in (11, 16, 21): draw.rectangle((12, y, 20, y + 2), fill=SAFE)
        draw.rectangle((14, 4, 18, 6), fill=LIGHT)
    else:
        raise KeyError(name)
    return image


def slot_icon(name: str) -> Image.Image:
    image, draw = canvas()
    if name == "main_hand":
        draw.polygon(((23, 3), (28, 4), (13, 21), (10, 18)), fill=LIGHT, outline=INK)
        draw.rectangle((8, 18, 16, 22), fill=AMBER, outline=INK); draw.polygon(((8, 21), (12, 25), (8, 29), (4, 25)), fill=STEEL, outline=INK)
    elif name == "off_hand":
        draw.polygon(((16, 3), (27, 7), (25, 21), (16, 29), (7, 21), (5, 7)), fill=INK)
        draw.polygon(((16, 6), (24, 9), (22, 19), (16, 25), (10, 19), (8, 9)), fill=STEEL)
        draw.line((16, 8, 16, 23), fill=LIGHT, width=2)
    elif name == "head":
        draw.rectangle((5, 11, 27, 20), fill=INK); draw.rectangle((8, 13, 14, 18), fill=CYAN); draw.rectangle((18, 13, 24, 18), fill=CYAN)
        draw.rectangle((14, 15, 18, 16), fill=LIGHT); draw.rectangle((9, 7, 23, 10), fill=STEEL, outline=INK)
    elif name == "chest":
        draw.polygon(((10, 4), (15, 8), (17, 8), (22, 4), (27, 10), (24, 28), (8, 28), (5, 10)), fill=INK)
        draw.polygon(((10, 8), (14, 11), (18, 11), (22, 8), (24, 11), (21, 25), (11, 25), (8, 11)), fill=STEEL)
        draw.rectangle((15, 12, 17, 24), fill=LIGHT)
    elif name == "hands":
        draw.polygon(((8, 5), (12, 5), (13, 13), (15, 7), (18, 8), (17, 17), (14, 25), (8, 24), (5, 17)), fill=INK)
        draw.polygon(((9, 8), (11, 8), (11, 17), (14, 12), (15, 13), (14, 18), (12, 22), (9, 21), (8, 16)), fill=LIGHT)
        draw.rectangle((14, 24, 26, 28), fill=STEEL, outline=INK)
    elif name == "legs":
        draw.polygon(((8, 4), (24, 4), (23, 16), (27, 26), (19, 28), (16, 18), (13, 28), (5, 26), (9, 16)), fill=INK)
        draw.polygon(((11, 7), (21, 7), (20, 16), (23, 24), (20, 25), (16, 15), (12, 25), (9, 24), (12, 16)), fill=STEEL)
    elif name == "backpack":
        draw.rectangle((8, 6, 24, 28), fill=INK); draw.rectangle((11, 9, 21, 24), fill=STEEL)
        draw.rectangle((12, 4, 20, 8), fill=LIGHT, outline=INK); draw.rectangle((13, 13, 19, 18), fill=AMBER)
        draw.line((6, 10, 6, 25), fill=LIGHT, width=2); draw.line((26, 10, 26, 25), fill=LIGHT, width=2)
    elif name == "aether_core":
        draw.polygon(((16, 3), (26, 9), (26, 22), (16, 29), (6, 22), (6, 9)), fill=INK)
        draw.polygon(((16, 7), (22, 11), (22, 20), (16, 24), (10, 20), (10, 11)), fill=STEEL)
        draw.rectangle((13, 12, 19, 19), fill=CYAN); draw.rectangle((15, 14, 17, 17), fill=WHITE)
    elif name == "conduit":
        draw.ellipse((7, 3, 25, 17), fill=INK); draw.ellipse((10, 6, 22, 14), fill=DEEP, outline=CYAN)
        draw.rectangle((14, 14, 18, 28), fill=INK); draw.rectangle((15, 16, 17, 25), fill=LIGHT); draw.rectangle((12, 25, 20, 28), fill=AMBER)
    elif name == "accessory_1":
        draw.line((9, 5, 16, 11, 23, 5), fill=LIGHT, width=2)
        draw.ellipse((10, 10, 22, 22), fill=INK); draw.ellipse((13, 13, 19, 19), fill=AMBER)
        draw.polygon(((16, 20), (21, 25), (16, 29), (11, 25)), fill=STEEL, outline=INK)
    elif name == "accessory_2":
        draw.ellipse((5, 7, 22, 24), fill=INK); draw.ellipse((9, 11, 18, 20), fill=DEEP, outline=LIGHT)
        draw.rectangle((19, 12, 27, 19), fill=INK); draw.rectangle((21, 14, 25, 17), fill=SAFE)
        draw.rectangle((8, 23, 19, 27), fill=STEEL, outline=INK)
    else:
        raise KeyError(name)
    return image


def state_icon(name: str) -> Image.Image:
    image = Image.new("RGBA", (16, 16), CLEAR); draw = ImageDraw.Draw(image)
    if name == "current":
        draw.rectangle((6, 1, 9, 14), fill=CYAN); draw.rectangle((1, 6, 14, 9), fill=CYAN)
        draw.rectangle((5, 5, 10, 10), fill=INK); draw.rectangle((7, 7, 8, 8), fill=WHITE)
    elif name == "available":
        draw.polygon(((3, 2), (12, 8), (3, 14), (3, 10), (7, 8), (3, 6)), fill=INK)
        draw.polygon(((5, 4), (11, 8), (5, 12), (5, 10), (8, 8), (5, 6)), fill=CYAN)
    elif name == "cleared":
        draw.line((2, 8, 6, 12, 14, 3), fill=INK, width=4); draw.line((2, 8, 6, 12, 14, 3), fill=SAFE, width=2)
    elif name == "visited":
        draw.line((2, 11, 6, 7, 10, 9, 14, 4), fill=STEEL, width=2)
        for x, y in ((2, 11), (6, 7), (10, 9), (14, 4)): draw.rectangle((x - 1, y - 1, x + 1, y + 1), fill=SAFE)
    elif name == "locked":
        draw.rectangle((3, 7, 13, 14), fill=INK); draw.rectangle((5, 9, 11, 13), fill=DANGER)
        draw.arc((5, 1, 11, 9), 180, 360, fill=LIGHT, width=2); draw.rectangle((7, 10, 9, 12), fill=WHITE)
    elif name == "known":
        draw.polygon(((1, 8), (5, 4), (11, 4), (15, 8), (11, 12), (5, 12)), fill=INK)
        draw.polygon(((3, 8), (6, 6), (10, 6), (13, 8), (10, 10), (6, 10)), fill=AMBER)
        draw.rectangle((7, 7, 9, 9), fill=WHITE)
    elif name == "unknown":
        for box in ((2, 2, 5, 5), (10, 2, 13, 5), (2, 10, 5, 13), (10, 10, 13, 13)): draw.rectangle(box, fill=STEEL)
        draw.rectangle((7, 7, 8, 8), fill=LIGHT)
    else:
        raise KeyError(name)
    return image


def save() -> list[dict]:
    RESOURCE_DIR.mkdir(parents=True, exist_ok=True); SLOT_DIR.mkdir(parents=True, exist_ok=True); STATE_DIR.mkdir(parents=True, exist_ok=True); QA_DIR.mkdir(parents=True, exist_ok=True)
    assets = {**{f"resource.{name}": (RESOURCE_DIR / f"{name}.png", resource_icon(name)) for name in (
        "gold", "contribution", "stage_time", "explored", "core_permit", "risk", "weight", "aether_load", "charges")},
        **{f"equipment_slot.{name}": (SLOT_DIR / f"{name}.png", slot_icon(name)) for name in (
            "main_hand", "off_hand", "head", "chest", "hands", "legs", "backpack", "aether_core", "conduit", "accessory_1", "accessory_2")},
        **{f"map_state.{name}": (STATE_DIR / f"{name}.png", state_icon(name)) for name in (
            "current", "available", "cleared", "visited", "locked", "known", "unknown")}}
    records = []
    for asset_id, (path, image) in assets.items():
        image.save(path, optimize=True)
        colors = len(set(image.getdata())); alpha = sorted(set(image.getchannel("A").getdata()))
        expected = (16, 16) if asset_id.startswith("map_state.") else (32, 32)
        passed = image.size == expected and alpha == [0, 255] and colors <= 7
        records.append({"id": asset_id, "path": str(path.relative_to(ROOT)).replace("\\", "/"), "size": list(image.size),
                        "colors": colors, "alphaValues": alpha, "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
                        "status": "PASS" if passed else "FAIL"})
    rows = (len(assets) + 4) // 5; sheet = Image.new("RGBA", (5 * 144, rows * 164), INK); draw = ImageDraw.Draw(sheet)
    for index, (asset_id, (path, image)) in enumerate(assets.items()):
        x = (index % 5) * 144; y = (index // 5) * 164
        sheet.alpha_composite(image.resize((128, 128), Image.Resampling.NEAREST), (x + 8, y + 8))
        draw.text((x + 8, y + 140), asset_id.split(".", 1)[1], fill=WHITE)
    sheet.save(QA_DIR / "OCC_M-A11_肉鸽UI语义资产_QA_v01.png", optimize=True)
    report = {"schema": "occ.rogue.ui.semantic-assets.v0.1", "status": "QA_PASS" if all(r["status"] == "PASS" for r in records) else "QA_FAIL",
              "assetCount": len(records), "rules": {"mainCell": [32, 32], "mapStateCell": [16, 16], "hardAlpha": True, "maxColorsIncludingClear": 7, "filter": "Point", "wrap": "Clamp", "ppu": {"main": 32, "mapState": 16}}, "records": records}
    (QA_DIR / "OCC_M-A11_肉鸽UI语义资产_QA_v01.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    return records


if __name__ == "__main__":
    result = save()
    print(f"generated={len(result)} passed={sum(row['status'] == 'PASS' for row in result)} qa={QA_DIR}")
