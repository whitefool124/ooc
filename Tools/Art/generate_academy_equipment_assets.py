from __future__ import annotations

import hashlib
import json
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "UnityProject/Assets/Game/Resources/Art"
ICON_DIR = ART / "FormalAcademyEquipmentIcons32"
FOOTPRINT_DIR = ART / "FormalAcademyEquipmentFootprints"
QA_DIR = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A12"

CLEAR = (0, 0, 0, 0)
INK = (6, 9, 12, 255)
DEEP = (17, 28, 34, 255)
STEEL = (65, 82, 87, 255)
LIGHT = (184, 198, 196, 255)
CYAN = (72, 197, 216, 255)
AMBER = (238, 172, 62, 255)
DANGER = (186, 76, 57, 255)


@dataclass(frozen=True)
class Equipment:
    id: str
    name: str
    width: int
    height: int
    kind: str
    variant: int

    @property
    def slug(self) -> str:
        return self.id.lower().replace("-", "_")


ITEMS = (
    Equipment("ACA-EQ-MH01", "学院练习剑", 1, 3, "blade", 1),
    Equipment("ACA-EQ-MH02", "钩刃长枪", 1, 3, "polearm", 2),
    Equipment("ACA-EQ-MH03", "刻印战锤", 2, 3, "hammer", 3),
    Equipment("ACA-EQ-MH04", "猎团短弓", 2, 3, "bow", 4),
    Equipment("ACA-EQ-MH05", "绞盘重弩", 2, 3, "crossbow", 5),
    Equipment("ACA-EQ-MH06", "灰炉导杖", 1, 3, "staff", 6),
    Equipment("ACA-EQ-OH01", "学院圆盾", 2, 2, "round_shield", 1),
    Equipment("ACA-EQ-OH02", "石闸长盾", 2, 3, "tower_shield", 2),
    Equipment("ACA-EQ-OH03", "反握短刃", 1, 2, "dagger", 3),
    Equipment("ACA-EQ-OH04", "导流副环", 1, 1, "ring", 4),
    Equipment("ACA-EQ-CH01", "夹棉练习衣", 2, 3, "coat", 1),
    Equipment("ACA-EQ-CH02", "补强巡行衣", 2, 3, "coat", 2),
    Equipment("ACA-EQ-CH03", "塔卫承压带", 2, 3, "harness", 3),
    Equipment("ACA-EQ-CH04", "轻装传令衣", 2, 3, "coat", 4),
    Equipment("ACA-EQ-CH05", "封存巡检袍", 2, 3, "robe", 5),
    Equipment("ACA-EQ-HD01", "测距护目镜", 2, 1, "goggles", 1),
    Equipment("ACA-EQ-HD02", "低压回路护额", 2, 1, "circlet", 2),
    Equipment("ACA-EQ-HN01", "行进握带", 2, 1, "wrap", 1),
    Equipment("ACA-EQ-HN02", "回授护臂", 2, 1, "bracer", 2),
    Equipment("ACA-EQ-LG01", "石路行靴", 2, 2, "boots", 1),
    Equipment("ACA-EQ-LG02", "定锚胫甲", 2, 2, "greaves", 2),
    Equipment("ACA-EQ-BP01", "勘验背架", 2, 3, "frame", 1),
    Equipment("ACA-EQ-BP02", "快挂整备架", 2, 3, "frame", 2),
    Equipment("ACA-EQ-CR01", "学院储能芯", 2, 2, "core", 1),
    Equipment("ACA-EQ-CR02", "余焰回收芯", 2, 2, "core", 2),
    Equipment("ACA-EQ-CR03", "塔心并联芯", 2, 2, "core", 3),
    Equipment("ACA-EQ-DG01", "远投定距杖", 1, 3, "conduit", 1),
    Equipment("ACA-EQ-DG02", "接触耦合环", 1, 1, "coupler", 2),
    Equipment("ACA-EQ-AC01", "余烬珠", 1, 1, "bead", 1),
    Equipment("ACA-EQ-AC02", "空槽魔力计", 1, 1, "meter", 2),
    Equipment("ACA-EQ-AC03", "贴身守誓牌", 1, 1, "plate", 3),
    Equipment("ACA-EQ-AC04", "灰线行程扣", 1, 1, "buckle", 4),
)


def box(draw: ImageDraw.ImageDraw, xy: tuple[int, int, int, int], fill, outline=INK) -> None:
    draw.rectangle(xy, fill=fill, outline=outline)


def equipment_art(item: Equipment, size: tuple[int, int], compact: bool) -> Image.Image:
    """Draw one item directly on its target logical canvas; no source icon stretching."""
    image = Image.new("RGBA", size, CLEAR)
    draw = ImageDraw.Draw(image)
    w, h = size
    margin = 1 if compact else 3
    x0, y0, x1, y1 = margin, margin, w - margin - 1, h - margin - 1
    cx, cy = w // 2, h // 2
    accent = DANGER if item.variant in (2, 5) and item.kind in ("core", "crossbow") else CYAN

    if item.kind in ("blade", "dagger"):
        draw.polygon(((x0 + 2, y1 - 1), (x0, y1 - 3), (x1 - 2, y0), (x1, y0 + 2)), fill=LIGHT, outline=INK)
        grip_y = min(y1 - 5, cy + 2)
        box(draw, (cx - 3, grip_y, cx + 3, grip_y + 2), AMBER)
        draw.polygon(((cx - 1, grip_y + 2), (cx + 2, grip_y + 4), (cx - 1, min(y1, grip_y + 6)), (cx - 4, grip_y + 4)), fill=STEEL, outline=INK)
    elif item.kind == "polearm":
        draw.line((cx - 2, y1, cx + 2, y0 + 5), fill=LIGHT, width=max(1, w // 16))
        draw.polygon(((cx + 2, y0 + 7), (x1, y0), (x1 - 2, y0 + 8), (cx + 1, y0 + 11)), fill=STEEL, outline=INK)
        draw.line((cx, y0 + 10, x0 + 1, y0 + 6), fill=INK, width=2)
    elif item.kind == "hammer":
        draw.line((cx - 3, y1, cx + 2, y0 + h // 4), fill=LIGHT, width=max(2, w // 14))
        box(draw, (x0 + 1, y0 + 2, x1 - 1, y0 + h // 4), STEEL)
        box(draw, (cx - 2, y0 + 3, cx + 2, y0 + h // 4 + 1), accent)
    elif item.kind == "bow":
        draw.arc((x0, y0, x1, y1), 250, 110, fill=AMBER, width=max(2, w // 15))
        draw.line((x0 + 3, cy, x1 - 3, y0 + 2), fill=LIGHT, width=1)
        draw.line((x0 + 3, cy, x1 - 3, y1 - 2), fill=LIGHT, width=1)
        draw.line((x0 + 2, cy, x1 - 2, cy), fill=STEEL, width=2)
    elif item.kind == "crossbow":
        box(draw, (cx - 3, y0 + 4, cx + 3, y1), STEEL)
        draw.arc((x0, y0, x1, y0 + h // 2), 195, 345, fill=LIGHT, width=max(2, w // 15))
        draw.line((x0 + 2, y0 + h // 4, x1 - 2, y0 + h // 4), fill=INK, width=1)
        box(draw, (cx - 4, cy - 3, cx + 4, cy + 3), accent)
    elif item.kind in ("staff", "conduit"):
        draw.line((cx - 2, y1, cx + 1, y0 + 5), fill=LIGHT, width=max(2, w // 13))
        draw.ellipse((cx - 5, y0, cx + 5, y0 + 10), fill=INK)
        draw.ellipse((cx - 3, y0 + 2, cx + 3, y0 + 8), fill=accent)
        if item.kind == "staff": box(draw, (cx - 4, cy, cx + 3, cy + 4), DANGER)
    elif item.kind == "round_shield":
        draw.ellipse((x0, y0, x1, y1), fill=INK)
        draw.ellipse((x0 + 3, y0 + 3, x1 - 3, y1 - 3), fill=STEEL, outline=LIGHT)
        draw.line((cx, y0 + 4, cx, y1 - 4), fill=AMBER, width=max(2, w // 15))
        box(draw, (cx - 3, cy - 3, cx + 3, cy + 3), CYAN)
    elif item.kind == "tower_shield":
        draw.polygon(((x0 + 3, y0), (x1 - 3, y0), (x1, y1 - 5), (cx, y1), (x0, y1 - 5)), fill=INK)
        draw.polygon(((x0 + 5, y0 + 3), (x1 - 5, y0 + 3), (x1 - 3, y1 - 7), (cx, y1 - 3), (x0 + 3, y1 - 7)), fill=STEEL)
        draw.line((cx, y0 + 4, cx, y1 - 5), fill=LIGHT, width=max(2, w // 14))
        box(draw, (cx - 4, cy - 3, cx + 4, cy + 3), AMBER)
    elif item.kind in ("ring", "coupler"):
        draw.ellipse((x0 + 1, y0 + 1, x1 - 1, y1 - 1), fill=INK)
        draw.ellipse((x0 + 4, y0 + 4, x1 - 4, y1 - 4), fill=DEEP, outline=accent)
        box(draw, (cx - 2, y0, cx + 2, y0 + 4), AMBER)
    elif item.kind in ("coat", "robe", "harness"):
        draw.polygon(((cx - 4, y0), (x0 + 3, y0 + 7), (x0, y1 - 3), (cx - 2, y1), (cx, cy), (cx + 2, y1), (x1, y1 - 3), (x1 - 3, y0 + 7), (cx + 4, y0)), fill=INK)
        draw.polygon(((cx - 3, y0 + 3), (x0 + 6, y0 + 9), (x0 + 4, y1 - 5), (cx, cy - 2), (x1 - 4, y1 - 5), (x1 - 6, y0 + 9), (cx + 3, y0 + 3)), fill=STEEL)
        draw.line((cx, y0 + 5, cx, y1 - 4), fill=LIGHT, width=1)
        if item.kind == "harness":
            draw.line((x0 + 5, y0 + 8, x1 - 5, y1 - 6), fill=AMBER, width=2)
            draw.line((x1 - 5, y0 + 8, x0 + 5, y1 - 6), fill=AMBER, width=2)
        elif item.kind == "robe": box(draw, (cx - 3, cy - 3, cx + 3, cy + 3), CYAN)
        elif item.variant == 4: draw.line((x0 + 5, cy, x1 - 5, cy), fill=AMBER, width=2)
    elif item.kind in ("goggles", "circlet"):
        draw.line((x0, cy, x1, cy), fill=LIGHT, width=max(2, h // 8))
        if item.kind == "goggles":
            box(draw, (x0 + 2, cy - 4, cx - 2, cy + 4), CYAN)
            box(draw, (cx + 2, cy - 4, x1 - 2, cy + 4), CYAN)
        else:
            draw.polygon(((cx, y0), (cx + 4, cy), (cx, y1), (cx - 4, cy)), fill=CYAN, outline=INK)
    elif item.kind in ("wrap", "bracer"):
        draw.polygon(((x0, cy - 3), (cx - 3, y0), (x1, cy - 1), (cx + 3, y1)), fill=STEEL, outline=INK)
        draw.line((x0 + 3, cy - 3, x1 - 3, cy + 3), fill=AMBER if item.kind == "wrap" else CYAN, width=max(2, h // 10))
        if item.kind == "bracer": box(draw, (cx - 3, cy - 3, cx + 3, cy + 3), LIGHT)
    elif item.kind in ("boots", "greaves"):
        for left in (True, False):
            bx = x0 + 2 if left else cx + 1
            ex = cx - 2 if left else x1 - 2
            draw.polygon(((bx + 2, y0), (ex, y0 + 2), (ex - 1, y1 - 4), (ex + 2, y1), (bx, y1), (bx + 1, cy)), fill=STEEL, outline=INK)
        if item.kind == "greaves":
            draw.line((x0 + 4, cy, cx - 2, cy), fill=AMBER, width=2)
            draw.line((cx + 2, cy, x1 - 3, cy), fill=AMBER, width=2)
    elif item.kind == "frame":
        box(draw, (x0 + 3, y0 + 2, x1 - 3, y1), DEEP)
        draw.line((x0 + 4, y0 + 3, x1 - 4, y1 - 2), fill=LIGHT, width=max(2, w // 16))
        draw.line((x1 - 4, y0 + 3, x0 + 4, y1 - 2), fill=LIGHT, width=max(2, w // 16))
        for yy in (y0 + h // 3, y0 + 2 * h // 3):
            box(draw, (x0 + 1, yy - 3, x0 + 7, yy + 3), AMBER if item.variant == 2 else STEEL)
    elif item.kind == "core":
        draw.polygon(((cx, y0), (x1, cy - 4), (x1 - 3, y1), (x0 + 3, y1), (x0, cy - 4)), fill=INK)
        draw.polygon(((cx, y0 + 3), (x1 - 4, cy - 2), (x1 - 6, y1 - 4), (x0 + 6, y1 - 4), (x0 + 4, cy - 2)), fill=STEEL)
        draw.ellipse((cx - 5, cy - 5, cx + 5, cy + 5), fill=accent, outline=LIGHT)
        if item.variant == 3:
            draw.line((cx - 6, cy, cx + 6, cy), fill=AMBER, width=2)
            draw.line((cx, cy - 6, cx, cy + 6), fill=AMBER, width=2)
    elif item.kind in ("bead", "meter", "plate", "buckle"):
        if item.kind == "bead":
            draw.ellipse((x0 + 2, y0 + 2, x1 - 2, y1 - 2), fill=DANGER, outline=INK)
            box(draw, (cx - 2, y0, cx + 2, y0 + 4), AMBER)
        elif item.kind == "meter":
            draw.ellipse((x0, y0, x1, y1), fill=STEEL, outline=INK)
            draw.arc((x0 + 3, y0 + 3, x1 - 3, y1 - 3), 190, 350, fill=CYAN, width=2)
            draw.line((cx, cy, x1 - 5, y0 + 6), fill=LIGHT, width=1)
        elif item.kind == "plate":
            draw.polygon(((cx, y0), (x1, y0 + 4), (x1 - 3, y1), (x0 + 3, y1), (x0, y0 + 4)), fill=STEEL, outline=INK)
            draw.line((cx, y0 + 4, cx, y1 - 3), fill=AMBER, width=2)
        else:
            box(draw, (x0 + 1, y0 + 3, x1 - 1, y1 - 3), STEEL)
            box(draw, (x0 + 4, y0 + 4, x1 - 4, y1 - 4), DEEP, outline=CYAN)
    else:
        raise KeyError(item.kind)
    return image


def nearest2(image: Image.Image) -> Image.Image:
    return image.resize((image.width * 2, image.height * 2), Image.Resampling.NEAREST)


def record(item: Equipment, kind: str, path: Path, image: Image.Image, expected: tuple[int, int]) -> dict:
    image.save(path, optimize=True)
    alpha = sorted(set(image.getchannel("A").getdata()))
    colors = len(set(image.getdata()))
    boundary_clear = all(image.getpixel((x, y))[3] == 0 for x in range(image.width) for y in (0, image.height - 1)) and all(
        image.getpixel((x, y))[3] == 0 for y in range(image.height) for x in (0, image.width - 1))
    passed = image.size == expected and alpha == [0, 255] and colors <= 7 and boundary_clear
    return {"id": item.id, "name": item.name, "kind": kind, "path": str(path.relative_to(ROOT)).replace("\\", "/"),
            "size": list(image.size), "colors": colors, "alphaValues": alpha, "boundaryClear": boundary_clear,
            "sha256": hashlib.sha256(path.read_bytes()).hexdigest(), "status": "PASS" if passed else "FAIL"}


def save() -> list[dict]:
    ICON_DIR.mkdir(parents=True, exist_ok=True)
    FOOTPRINT_DIR.mkdir(parents=True, exist_ok=True)
    QA_DIR.mkdir(parents=True, exist_ok=True)
    records: list[dict] = []
    previews: list[tuple[Equipment, Image.Image, Image.Image]] = []
    for item in ITEMS:
        icon = nearest2(equipment_art(item, (16, 16), compact=True))
        footprint = nearest2(equipment_art(item, (item.width * 16, item.height * 16), compact=False))
        records.append(record(item, "content-icon", ICON_DIR / f"{item.slug}.png", icon, (32, 32)))
        records.append(record(item, "inventory-footprint", FOOTPRINT_DIR / f"{item.slug}.png", footprint, (item.width * 32, item.height * 32)))
        previews.append((item, icon, footprint))

    sheet = Image.new("RGBA", (4 * 248, 8 * 260), INK)
    draw = ImageDraw.Draw(sheet)
    for index, (item, icon, footprint) in enumerate(previews):
        x, y = (index % 4) * 248, (index // 4) * 260
        sheet.alpha_composite(icon.resize((128, 128), Image.Resampling.NEAREST), (x + 8, y + 8))
        fit = min(112 / footprint.width, 112 / footprint.height)
        fp = footprint.resize((max(1, int(footprint.width * fit)), max(1, int(footprint.height * fit))), Image.Resampling.NEAREST)
        sheet.alpha_composite(fp, (x + 136 + (104 - fp.width) // 2, y + 12 + (112 - fp.height) // 2))
        draw.text((x + 8, y + 142), item.id, fill=CYAN)
        draw.text((x + 8, y + 162), item.name, fill=LIGHT)
        draw.text((x + 8, y + 184), f"icon 32x32 | footprint {item.width * 32}x{item.height * 32}", fill=AMBER)
        draw.rectangle((x + 8, y + 208, x + 232, y + 248), outline=STEEL)
        draw.text((x + 16, y + 220), f"{item.kind} / v{item.variant}", fill=LIGHT)
    sheet.save(QA_DIR / "OCC_M-A12_学院装备双分辨率资产_QA_v01.png", optimize=True)

    report = {"schema": "occ.academy-equipment.art.v0.1", "status": "QA_PASS" if all(row["status"] == "PASS" for row in records) else "QA_FAIL",
              "equipmentCount": len(ITEMS), "assetCount": len(records),
              "rules": {"contentIcon": [32, 32], "footprintPixelsPerCell": 32, "hardAlpha": True,
                         "maxColorsIncludingClear": 7, "transparentSafetyBoundary": True, "filter": "Point", "wrap": "Clamp", "ppu": 32},
              "records": records}
    (QA_DIR / "OCC_M-A12_学院装备双分辨率资产_QA_v01.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    return records


if __name__ == "__main__":
    result = save()
    print(f"generated={len(result)} passed={sum(row['status'] == 'PASS' for row in result)} qa={QA_DIR}")
