from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
OUT_DIR = ROOT / "UnityProject/Assets/Game/Resources/Art/FormalAcademyAtlas"
MARKER_DIR = ROOT / "UnityProject/Assets/Game/Resources/Art/FormalMapNodeMarkers48"
QA_DIR = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A15"

SCALE = 2
WORK_SIZE = (800, 450)
FINAL_SIZE = (1600, 900)

INK = (7, 11, 13, 255)
WATER_DARK = (10, 25, 31, 255)
WATER = (15, 39, 47, 255)
WATER_LIGHT = (25, 62, 70, 255)
CLIFF = (39, 46, 44, 255)
CLIFF_LIGHT = (66, 72, 66, 255)
GROUND = (52, 55, 48, 255)
GROUND_DARK = (38, 43, 39, 255)
GRASS = (43, 59, 45, 255)
GRASS_LIGHT = (57, 76, 53, 255)
ROAD_EDGE = (38, 40, 38, 255)
ROAD = (83, 82, 72, 255)
ROAD_LIGHT = (109, 105, 90, 255)
STONE = (93, 97, 91, 255)
STONE_LIGHT = (133, 136, 125, 255)
WALL = (74, 77, 72, 255)
ROOF = (39, 46, 49, 255)
ROOF_LIGHT = (63, 72, 73, 255)
ROOF_GREEN = (38, 62, 59, 255)
WOOD = (78, 57, 39, 255)
RUST = (113, 57, 40, 255)
AMBER = (213, 150, 56, 255)
CYAN = (57, 176, 198, 255)
CYAN_LIGHT = (150, 224, 225, 255)
RED = (162, 55, 48, 255)
WHITE = (197, 205, 197, 255)
CLEAR = (0, 0, 0, 0)


ANCHORS = (
    ((250, 150), (420, 135), (590, 175), (770, 145), (950, 165), (1110, 125), (1280, 175), (1450, 160)),
    ((220, 300), (400, 280), (575, 325), (760, 285), (930, 310), (1100, 275), (1280, 320), (1450, 300)),
    ((260, 455), (430, 430), (610, 470), (790, 425), (960, 455), (1130, 420), (1310, 465), (1460, 440)),
    ((230, 620), (410, 595), (585, 645), (760, 600), (940, 625), (1120, 585), (1295, 635), (1450, 605)),
    ((260, 770), (445, 750), (620, 795), (800, 745), (980, 780), (1150, 740), (1320, 785), (1470, 755)),
)


def line(draw: ImageDraw.ImageDraw, points, fill, width=1):
    draw.line(tuple((int(x), int(y)) for x, y in points), fill=fill, width=width, joint="curve")


def road(draw: ImageDraw.ImageDraw, points, width=15):
    line(draw, points, INK, width + 6)
    line(draw, points, ROAD_EDGE, width + 3)
    line(draw, points, ROAD, width)
    line(draw, points, ROAD_LIGHT, 2)


def rail(draw: ImageDraw.ImageDraw, points):
    line(draw, points, INK, 9)
    line(draw, points, WALL, 6)
    line(draw, points, ROOF_LIGHT, 1)
    for index in range(len(points) - 1):
        x1, y1 = points[index]; x2, y2 = points[index + 1]
        steps = max(1, int(((x2 - x1) ** 2 + (y2 - y1) ** 2) ** .5 // 8))
        for step in range(steps + 1):
            t = step / steps
            x = int(x1 + (x2 - x1) * t); y = int(y1 + (y2 - y1) * t)
            draw.rectangle((x - 2, y - 2, x + 2, y + 2), fill=WOOD)


def building(draw: ImageDraw.ImageDraw, box, roof=ROOF, ridge="h", windows=True):
    x1, y1, x2, y2 = box
    draw.rectangle((x1 + 4, y1 + 5, x2 + 5, y2 + 6), fill=INK)
    draw.rectangle((x1 + 2, y1 + 3, x2 + 3, y2 + 4), fill=WALL)
    draw.rectangle((x1, y1, x2, y2), fill=roof, outline=INK)
    if ridge == "h":
        mid = (y1 + y2) // 2
        draw.polygon(((x1 + 2, mid), (x1 + 7, y1 + 2), (x2 - 7, y1 + 2), (x2 - 2, mid)), fill=ROOF_LIGHT)
        line(draw, ((x1 + 3, mid), (x2 - 3, mid)), STONE_LIGHT)
    else:
        mid = (x1 + x2) // 2
        draw.polygon(((mid, y1 + 2), (x2 - 2, y1 + 7), (x2 - 2, y2 - 7), (mid, y2 - 2)), fill=ROOF_LIGHT)
        line(draw, ((mid, y1 + 3), (mid, y2 - 3)), STONE_LIGHT)
    if windows:
        if x2 - x1 >= y2 - y1:
            for x in range(x1 + 8, x2 - 4, 12): draw.rectangle((x, y2 + 1, x + 3, y2 + 3), fill=AMBER)
        else:
            for y in range(y1 + 8, y2 - 4, 12): draw.rectangle((x2 + 1, y, x2 + 3, y + 3), fill=AMBER)


def tree(draw: ImageDraw.ImageDraw, x, y, size=5):
    draw.rectangle((x - 1, y, x + 1, y + size + 2), fill=WOOD)
    draw.rectangle((x - size, y - size, x + size, y + size), fill=INK)
    draw.polygon(((x, y - size - 2), (x + size, y), (x + 2, y + size), (x - size, y + 2)), fill=GRASS)
    draw.rectangle((x - 1, y - size + 1, x + 2, y - 2), fill=GRASS_LIGHT)


def aether_device(draw: ImageDraw.ImageDraw, x, y):
    draw.rectangle((x - 5, y - 5, x + 5, y + 5), fill=INK)
    draw.rectangle((x - 3, y - 3, x + 3, y + 3), fill=CYAN)
    draw.rectangle((x - 1, y - 1, x + 1, y + 1), fill=CYAN_LIGHT)
    for dx, dy in ((0, -8), (8, 0), (0, 8), (-8, 0)): draw.rectangle((x + dx - 1, y + dy - 1, x + dx + 1, y + dy + 1), fill=STONE_LIGHT)


def draw_atlas() -> Image.Image:
    image = Image.new("RGBA", WORK_SIZE, WATER_DARK)
    d = ImageDraw.Draw(image)

    # Ocean: broad readable bands, no single-pixel noise.
    d.rectangle((0, 40, 799, 449), fill=WATER)
    for y in range(50, 450, 24):
        offset = (y // 24 % 3) * 7
        for x in range(-20 + offset, 800, 58):
            d.rectangle((x, y, x + 18, y + 1), fill=WATER_LIGHT)
            d.rectangle((x + 5, y + 3, x + 27, y + 4), fill=WATER_DARK)

    coast = ((62, 48), (190, 30), (340, 38), (470, 24), (640, 38), (744, 75), (785, 142), (776, 246),
             (792, 330), (748, 414), (634, 430), (538, 415), (445, 432), (334, 416), (250, 438), (151, 421),
             (82, 380), (48, 315), (58, 246), (34, 175))
    d.polygon(coast, fill=INK)
    inner = tuple((x + (4 if x < 400 else -3), y + (5 if y < 240 else -5)) for x, y in coast)
    d.polygon(inner, fill=CLIFF)
    land = ((72, 57), (194, 40), (338, 48), (470, 35), (630, 48), (730, 84), (770, 146), (760, 240),
            (775, 326), (736, 400), (630, 416), (540, 400), (445, 418), (335, 402), (250, 424), (157, 407),
            (94, 370), (61, 310), (72, 245), (49, 180))
    d.polygon(land, fill=GROUND)

    # Cliff faces and sea walls.
    for x, y in ((55, 170), (60, 245), (65, 315), (105, 380), (185, 414), (270, 417), (360, 405), (455, 410), (555, 405), (650, 410), (735, 380), (770, 315)):
        d.rectangle((x, y, x + 18, y + 4), fill=CLIFF_LIGHT)
        d.rectangle((x + 3, y + 5, x + 15, y + 7), fill=INK)

    # Regional ground masses.
    d.polygon(((62, 62), (320, 44), (335, 205), (84, 225)), fill=(49, 53, 49, 255))
    d.polygon(((320, 48), (585, 42), (590, 196), (330, 205)), fill=(58, 54, 46, 255))
    d.polygon(((70, 225), (350, 204), (410, 350), (90, 380)), fill=(54, 53, 44, 255))
    d.polygon(((335, 205), (590, 195), (670, 380), (410, 350)), fill=GRASS)
    d.ellipse((575, 45, 775, 235), fill=(47, 43, 43, 255), outline=RED, width=3)

    # Campus roads and cross-region loops.
    road(d, ((82, 350), (150, 325), (220, 280), (300, 240), (395, 225), (485, 230), (575, 210), (650, 175)), 17)
    road(d, ((150, 325), (205, 365), (300, 380), (405, 365), (510, 390), (645, 370), (735, 330)), 13)
    road(d, ((220, 280), (205, 210), (190, 145), (250, 105)), 13)
    road(d, ((300, 240), (320, 165), (400, 105), (505, 95)), 14)
    road(d, ((395, 225), (390, 305), (405, 365)), 17)
    road(d, ((485, 230), (515, 160), (575, 120), (650, 120)), 12)
    road(d, ((510, 390), (565, 325), (635, 285), (705, 280), (735, 330)), 11)
    road(d, ((650, 175), (690, 205), (705, 280)), 12)
    road(d, ((250, 105), (400, 105), (505, 95), (575, 120)), 10)
    road(d, ((205, 365), (185, 285), (205, 210)), 10)

    # Coastal railway and workshop service spur.
    rail(d, ((36, 390), (78, 365), (115, 330), (145, 285), (170, 245), (220, 225)))
    rail(d, ((355, 65), (440, 62), (525, 68), (590, 92)))
    d.rectangle((28, 395, 110, 406), fill=INK); d.rectangle((33, 397, 105, 403), fill=WALL)

    # Northwest teaching and archive quarter.
    building(d, (75, 70, 165, 92), ROOF, "h")
    building(d, (92, 108, 180, 132), ROOF_GREEN, "h")
    building(d, (205, 62, 282, 86), ROOF, "h")
    building(d, (238, 105, 305, 132), ROOF, "v")
    building(d, (95, 158, 158, 181), ROOF, "h")
    d.ellipse((165, 76, 213, 124), fill=INK); d.ellipse((171, 82, 207, 118), fill=STONE); d.ellipse((179, 89, 199, 109), fill=ROOF_GREEN)
    d.rectangle((184, 72, 194, 82), fill=ROOF_LIGHT)
    for x in (78, 118, 278): tree(d, x, 142, 4)

    # Central dormitories, gate and main hall.
    building(d, (235, 190, 320, 213), ROOF, "h")
    building(d, (250, 265, 330, 288), ROOF, "h")
    building(d, (330, 168, 380, 200), ROOF_GREEN, "v")
    building(d, (415, 160, 505, 190), ROOF, "h")
    building(d, (430, 270, 505, 296), ROOF_GREEN, "h")
    d.rectangle((175, 258, 216, 290), fill=INK); d.rectangle((180, 262, 211, 286), fill=WALL)
    d.rectangle((188, 270, 203, 286), fill=GROUND_DARK); d.rectangle((193, 270, 198, 286), fill=ROAD_LIGHT)
    d.ellipse((345, 208, 430, 276), fill=ROAD_EDGE, outline=INK, width=3)
    d.ellipse((352, 214, 423, 269), fill=ROAD)
    d.rectangle((382, 226, 393, 252), fill=INK); d.rectangle((385, 222, 390, 250), fill=STONE_LIGHT)
    aether_device(d, 388, 238)

    # Northern training grounds and calibration workshops.
    building(d, (350, 82, 420, 104), ROOF, "h")
    building(d, (455, 88, 532, 113), ROOF, "h")
    building(d, (500, 135, 566, 159), ROOF_GREEN, "h")
    d.rectangle((365, 120, 440, 150), fill=ROAD_EDGE, outline=INK)
    for x in range(373, 435, 15): d.rectangle((x, 125, x + 8, 144), fill=RUST, outline=INK)
    d.rectangle((535, 60, 585, 85), fill=INK); d.rectangle((540, 64, 580, 81), fill=WALL)
    for x in (530, 562): aether_device(d, x, 177)

    # South market and infirmary.
    building(d, (150, 320, 215, 342), ROOF_GREEN, "h")
    building(d, (260, 330, 327, 354), ROOF, "h")
    building(d, (330, 375, 400, 398), ROOF_GREEN, "h")
    d.rectangle((220, 345, 252, 361), fill=INK); d.rectangle((222, 347, 250, 358), fill=WOOD)
    d.rectangle((226, 343, 246, 347), fill=AMBER)
    for x in range(205, 305, 22):
        d.rectangle((x, 388, x + 14, 397), fill=WOOD, outline=INK)
        d.rectangle((x + 2, 385, x + 12, 388), fill=AMBER)
    d.rectangle((405, 340, 438, 370), fill=INK); d.rectangle((409, 344, 434, 366), fill=WHITE)
    d.rectangle((419, 347, 424, 363), fill=GRASS_LIGHT); d.rectangle((414, 352, 429, 358), fill=GRASS_LIGHT)

    # Southeast wilds, drainage and abandoned practice grounds.
    line(d, ((445, 410), (490, 365), (540, 340), (585, 300), (635, 285), (675, 310), (720, 365)), WATER_DARK, 11)
    line(d, ((445, 410), (490, 365), (540, 340), (585, 300), (635, 285), (675, 310), (720, 365)), WATER_LIGHT, 4)
    for x, y in ((470, 320), (495, 300), (540, 285), (570, 360), (610, 330), (660, 350), (700, 390), (730, 300), (520, 405), (620, 400)):
        tree(d, x, y, 6)
    d.rectangle((545, 260, 595, 285), fill=GROUND_DARK, outline=INK)
    d.rectangle((552, 266, 570, 279), fill=CLIFF_LIGHT)
    d.rectangle((610, 245, 660, 268), fill=GROUND_DARK, outline=INK)
    d.line((615, 250, 654, 264), fill=RUST, width=3)
    d.ellipse((665, 345, 728, 395), outline=STONE, width=5)
    d.arc((672, 352, 721, 388), 20, 210, fill=ROAD_LIGHT, width=3)

    # Sealed tower ring and final landmark.
    d.ellipse((584, 54, 764, 228), fill=INK)
    d.ellipse((590, 60, 758, 222), fill=WALL)
    d.ellipse((600, 70, 748, 212), fill=GROUND_DARK, outline=RED, width=3)
    for angle_point in ((620, 82), (676, 66), (728, 92), (742, 145), (720, 196), (660, 211), (610, 180)):
        x, y = angle_point; d.rectangle((x - 7, y - 7, x + 7, y + 7), fill=INK); d.rectangle((x - 4, y - 4, x + 4, y + 4), fill=STONE)
    d.ellipse((650, 92, 704, 178), fill=INK)
    d.ellipse((656, 99, 698, 170), fill=ROOF)
    d.rectangle((661, 69, 693, 142), fill=INK)
    d.rectangle((666, 74, 688, 139), fill=ROOF_LIGHT)
    d.rectangle((671, 48, 683, 112), fill=INK)
    d.rectangle((674, 52, 680, 108), fill=RED)
    for y in (66, 84, 102, 126): d.rectangle((669, y, 685, y + 3), fill=STONE_LIGHT)
    d.rectangle((642, 180, 708, 194), fill=INK); d.rectangle((648, 183, 702, 190), fill=WALL)

    # Outer walls, gates and sparse maintained aether.
    wall_segments = (((70, 60), (50, 180)), ((50, 180), (68, 245)), ((70, 370), (150, 407)), ((160, 407), (248, 424)),
                     ((340, 402), (445, 418)), ((540, 400), (630, 416)), ((736, 400), (775, 326)), ((770, 146), (730, 84)),
                     ((730, 84), (630, 48)), ((470, 35), (340, 48)), ((194, 40), (72, 57)))
    for segment in wall_segments:
        line(d, segment, INK, 7); line(d, segment, STONE, 4); line(d, segment, STONE_LIGHT, 1)
    for x, y in ((72, 58), (51, 180), (70, 370), (150, 407), (248, 424), (445, 418), (630, 416), (775, 326), (770, 146), (730, 84), (630, 48), (470, 35), (340, 48), (194, 40)):
        d.rectangle((x - 4, y - 4, x + 4, y + 4), fill=INK); d.rectangle((x - 2, y - 2, x + 2, y + 2), fill=STONE_LIGHT)
    aether_device(d, 118, 342)
    aether_device(d, 575, 120)

    # Deliberate clustered paving marks, never random noise.
    for x, y in ((290, 228), (315, 250), (450, 215), (470, 250), (545, 205), (185, 305), (335, 315), (475, 330), (600, 230), (720, 250)):
        d.rectangle((x, y, x + 8, y + 2), fill=ROAD_LIGHT)
        d.rectangle((x + 3, y + 5, x + 12, y + 6), fill=ROAD_EDGE)

    return image.resize(FINAL_SIZE, Image.Resampling.NEAREST)


def node_marker(name: str) -> Image.Image:
    image = Image.new("RGBA", (48, 48), CLEAR); d = ImageDraw.Draw(image)
    accents = {"current": CYAN, "available": CYAN, "cleared": GRASS_LIGHT, "visited": GRASS_LIGHT,
               "locked": RED, "known": AMBER, "unknown": WALL}
    fills = {"current": (17, 48, 53, 255), "available": (15, 38, 43, 255), "cleared": (25, 48, 38, 255),
             "visited": (27, 42, 37, 255), "locked": (48, 26, 27, 255), "known": (49, 42, 27, 255), "unknown": (22, 29, 30, 255)}
    accent = accents[name]
    shape = ((16, 1), (32, 1), (46, 14), (46, 34), (34, 46), (14, 46), (1, 34), (1, 14))
    inner = ((17, 5), (31, 5), (42, 16), (42, 32), (32, 42), (16, 42), (5, 32), (5, 16))
    d.polygon(shape, fill=INK)
    d.polygon(inner, fill=fills[name], outline=accent)
    d.rectangle((9, 9, 13, 13), fill=INK, outline=accent)
    d.point((11, 11), fill=WHITE)
    if name == "current":
        d.rectangle((20, 4, 27, 43), fill=accent); d.rectangle((4, 20, 43, 27), fill=accent)
        d.rectangle((16, 16, 31, 31), fill=INK); d.rectangle((21, 21, 26, 26), fill=CYAN_LIGHT)
    elif name == "available":
        d.polygon(((17, 12), (36, 24), (17, 36), (17, 29), (25, 24), (17, 19)), fill=accent)
        d.polygon(((19, 17), (30, 24), (19, 31), (19, 27), (24, 24), (19, 21)), fill=WHITE)
    elif name == "cleared":
        line(d, ((10, 25), (19, 34), (38, 13)), INK, 7); line(d, ((10, 25), (19, 34), (38, 13)), accent, 4)
    elif name == "visited":
        line(d, ((9, 32), (17, 20), (27, 27), (38, 14)), accent, 4)
        for x, y in ((9, 32), (17, 20), (27, 27), (38, 14)): d.rectangle((x - 2, y - 2, x + 2, y + 2), fill=WHITE)
    elif name == "locked":
        d.rectangle((14, 22, 34, 36), fill=RED, outline=INK, width=3); d.arc((16, 10, 32, 28), 180, 360, fill=WHITE, width=3)
        d.rectangle((22, 27, 26, 33), fill=INK)
    elif name == "known":
        d.polygon(((8, 24), (16, 16), (32, 16), (40, 24), (32, 32), (16, 32)), fill=AMBER, outline=INK)
        d.rectangle((21, 21, 27, 27), fill=WHITE)
    else:
        for x, y in ((14, 14), (28, 14), (14, 28), (28, 28)): d.rectangle((x, y, x + 5, y + 5), fill=WALL)
        d.rectangle((22, 22, 25, 25), fill=STONE_LIGHT)
    return image


def generate() -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True); MARKER_DIR.mkdir(parents=True, exist_ok=True); QA_DIR.mkdir(parents=True, exist_ok=True)
    atlas = draw_atlas()
    path = OUT_DIR / "academy_coastal_atlas.png"
    atlas.save(path, optimize=True)

    qa = atlas.copy(); qd = ImageDraw.Draw(qa)
    for row in ANCHORS:
        for x, y in row:
            qd.rectangle((x - 9, y - 9, x + 9, y + 9), outline=CYAN_LIGHT, width=3)
            qd.rectangle((x - 2, y - 2, x + 2, y + 2), fill=CYAN)
    qa.save(QA_DIR / "OCC_M-A15_学院沿海大地图_40锚点QA_v01.png", optimize=True)

    alpha = sorted(set(atlas.getchannel("A").getdata()))
    colors = len(set(atlas.getdata()))
    marker_records = []
    marker_sheet = Image.new("RGBA", (7 * 112, 136), INK)
    for index, name in enumerate(("current", "available", "cleared", "visited", "locked", "known", "unknown")):
        marker = node_marker(name); marker_path = MARKER_DIR / f"{name}.png"; marker.save(marker_path, optimize=True)
        marker_alpha = sorted(set(marker.getchannel("A").getdata()))
        marker_records.append({"id": name, "size": list(marker.size), "alphaValues": marker_alpha,
                               "colors": len(set(marker.getdata())), "sha256": hashlib.sha256(marker_path.read_bytes()).hexdigest(),
                               "status": "PASS" if marker.size == (48, 48) and marker_alpha == [0, 255] else "FAIL"})
        marker_sheet.alpha_composite(marker.resize((96, 96), Image.Resampling.NEAREST), (index * 112 + 8, 8))
        ImageDraw.Draw(marker_sheet).text((index * 112 + 8, 110), name, fill=WHITE)
    marker_sheet.save(QA_DIR / "OCC_M-A15_地图节点标记_QA_v01.png", optimize=True)

    report = {
        "schema": "occ.formal.academy-coastal-atlas.v0.1",
        "status": "QA_PASS" if atlas.size == FINAL_SIZE and alpha == [255] and colors <= 32 and all(row["status"] == "PASS" for row in marker_records) else "QA_FAIL",
        "asset": str(path.relative_to(ROOT)).replace("\\", "/"),
        "size": list(atlas.size),
        "workingSize": list(WORK_SIZE),
        "nearestScale": SCALE,
        "colors": colors,
        "alphaValues": alpha,
        "anchorCount": sum(len(row) for row in ANCHORS),
        "markerCount": len(marker_records),
        "markers": marker_records,
        "import": {"textureType": "Sprite", "filter": "Point", "wrap": "Clamp", "mipmap": False, "ppu": 32},
        "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
    }
    (QA_DIR / "OCC_M-A15_学院沿海大地图_QA_v01.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"status": report["status"], "size": report["size"], "colors": colors, "anchors": report["anchorCount"], "markers": report["markerCount"]}, ensure_ascii=False))


if __name__ == "__main__":
    generate()
