from __future__ import annotations

import hashlib
import importlib.util
import json
import re
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A16"
FORMAL_MARKER_DIR = ROOT / "UnityProject/Assets/Game/Resources/Art/FormalMapNodeMarkers32"
SOURCE_SCRIPT = ROOT / "Tools/Art/generate_academy_coastal_atlas.py"
CATALOG_SOURCE = ROOT / "UnityProject/Assets/Game/Runtime/Campaign/RogueliteMapRun.cs"
NODE_ICON_DIR = ROOT / "UnityProject/Assets/Game/Resources/Art/FormalNodeIcons32/types"

ATLAS_SIZE = (1536, 864)
WORK_SIZE = (768, 432)
MOCKUP_SIZE = (1920, 1080)

INK = (6, 10, 12, 255)
SURFACE = (12, 20, 23, 248)
RAISED = (20, 31, 34, 244)
LINE = (55, 70, 72, 255)
TEXT = (215, 222, 218, 255)
MUTED = (139, 154, 153, 255)
CYAN = (55, 181, 204, 255)
CYAN_LIGHT = (158, 230, 229, 255)
AMBER = (220, 156, 58, 255)
SAFE = (99, 160, 119, 255)
RED = (177, 65, 56, 255)


def load_source_module():
    spec = importlib.util.spec_from_file_location("academy_coastal_v01", SOURCE_SCRIPT)
    module = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(module)
    return module


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    candidates = [
        Path("C:/Windows/Fonts/msyhbd.ttc" if bold else "C:/Windows/Fonts/msyh.ttc"),
        Path("C:/Windows/Fonts/simhei.ttf"),
    ]
    for candidate in candidates:
        if candidate.exists():
            return ImageFont.truetype(str(candidate), size=size)
    return ImageFont.load_default()


def recolor_atlas(image: Image.Image, module) -> Image.Image:
    replacements = {
        module.WATER_DARK: (7, 22, 29, 255),
        module.WATER: (12, 40, 50, 255),
        module.WATER_LIGHT: (24, 69, 78, 255),
        module.GROUND: (62, 64, 56, 255),
        module.GROUND_DARK: (39, 46, 42, 255),
        module.ROAD: (103, 98, 82, 255),
        module.ROAD_LIGHT: (142, 132, 105, 255),
        module.ROOF: (31, 42, 47, 255),
        module.ROOF_LIGHT: (69, 82, 82, 255),
        module.ROOF_GREEN: (35, 66, 62, 255),
        module.STONE: (101, 106, 99, 255),
        module.STONE_LIGHT: (151, 153, 139, 255),
        module.GRASS: (40, 65, 47, 255),
        module.GRASS_LIGHT: (64, 90, 61, 255),
    }
    pixels = [replacements.get(pixel, pixel) for pixel in image.getdata()]
    result = Image.new("RGBA", image.size)
    result.putdata(pixels)
    return result


def build_atlas(module) -> Image.Image:
    # Normalize the previous approved geography onto a 768x432 logical grid,
    # then restore exact 2x pixel clusters for the formal 1536x864 overview.
    source = module.draw_atlas().resize(WORK_SIZE, Image.Resampling.NEAREST)
    source = recolor_atlas(source, module)
    return source.resize(ATLAS_SIZE, Image.Resampling.NEAREST)


NODE_PATTERN = re.compile(
    r'new RogueliteMapNode\("(?P<id>[^"]+)", RogueliteMapNodeType\.(?P<type>\w+),.*?, '
    r'(?P<x>\d+), (?P<y>\d+), \d+, \d+(?P<next>.*?)\),?$'
)


def parse_nodes() -> dict[str, dict]:
    nodes: dict[str, dict] = {}
    for raw in CATALOG_SOURCE.read_text(encoding="utf-8").splitlines():
        match = NODE_PATTERN.search(raw.strip())
        if not match:
            continue
        node_id = match.group("id")
        next_ids = re.findall(r'"([^"]+)"', match.group("next"))
        nodes[node_id] = {
            "id": node_id,
            "type": match.group("type").lower(),
            "x": int(match.group("x")),
            "y": int(match.group("y")),
            "next": next_ids,
        }
    if len(nodes) != 40:
        raise RuntimeError(f"Expected 40 map nodes, parsed {len(nodes)}")
    return nodes


def scaled_anchors(module) -> dict[tuple[int, int], tuple[int, int]]:
    result: dict[tuple[int, int], tuple[int, int]] = {}
    for y, row in enumerate(module.ANCHORS):
        for x, (source_x, source_y) in enumerate(row):
            result[(x, y)] = (round(source_x * ATLAS_SIZE[0] / 1600), round(source_y * ATLAS_SIZE[1] / 900))
    return result


def dashed_line(draw: ImageDraw.ImageDraw, a: tuple[int, int], b: tuple[int, int], fill, width: int = 2, dash: int = 9, gap: int = 8):
    ax, ay = a; bx, by = b
    dx = bx - ax; dy = by - ay
    length = max(1.0, (dx * dx + dy * dy) ** 0.5)
    step = dash + gap
    position = 0.0
    while position < length:
        end = min(length, position + dash)
        p0 = (round(ax + dx * position / length), round(ay + dy * position / length))
        p1 = (round(ax + dx * end / length), round(ay + dy * end / length))
        draw.line((p0, p1), fill=fill, width=width)
        position += step


def marker(state: str, node_type: str | None = None) -> Image.Image:
    image = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    accent = {"current": CYAN, "available": CYAN_LIGHT, "cleared": SAFE, "visited": SAFE,
              "locked": RED, "known": AMBER, "unknown": MUTED}[state]
    outer = ((10, 0), (22, 0), (31, 9), (31, 22), (22, 31), (9, 31), (0, 22), (0, 9))
    inner = ((11, 3), (21, 3), (28, 10), (28, 21), (21, 28), (10, 28), (3, 21), (3, 10))
    draw.polygon(outer, fill=INK)
    draw.polygon(inner, fill=(17, 26, 29, 255), outline=accent)
    if state == "current":
        draw.rectangle((13, 1, 18, 30), fill=accent)
        draw.rectangle((1, 13, 30, 18), fill=accent)
        draw.rectangle((7, 7, 24, 24), fill=INK)
    elif state == "available":
        draw.rectangle((1, 1, 5, 5), fill=accent)
        draw.rectangle((26, 1, 30, 5), fill=accent)
        draw.rectangle((1, 26, 5, 30), fill=accent)
        draw.rectangle((26, 26, 30, 30), fill=accent)
    elif state == "cleared":
        draw.line((9, 17, 14, 22, 23, 10), fill=accent, width=3)
    elif state == "visited":
        draw.rectangle((9, 14, 22, 17), fill=accent)
        draw.rectangle((14, 9, 17, 22), fill=accent)
    elif state == "locked":
        draw.rectangle((11, 17, 21, 24), fill=RED)
        draw.arc((11, 8, 21, 20), 180, 360, fill=TEXT, width=2)
    icon_path = NODE_ICON_DIR / f"{node_type}.png"
    if node_type and state not in {"unknown", "locked"} and icon_path.exists():
        icon = Image.open(icon_path).convert("RGBA").resize((16, 16), Image.Resampling.NEAREST)
        image.alpha_composite(icon, (8, 8))
    elif state == "unknown":
        for px, py in ((10, 10), (18, 10), (10, 18), (18, 18)):
            draw.rectangle((px, py, px + 3, py + 3), fill=(75, 88, 88, 255))
    return image


def panel(draw: ImageDraw.ImageDraw, box, fill=SURFACE, outline=LINE, width=2):
    draw.rectangle(box, fill=fill, outline=outline, width=width)
    x1, y1, x2, y2 = box
    draw.line((x1 + 8, y1 + 4, x2 - 8, y1 + 4), fill=(34, 126, 145, 255), width=2)


def compose_mockup(atlas: Image.Image, module, nodes: dict[str, dict]) -> Image.Image:
    image = Image.new("RGBA", MOCKUP_SIZE, (4, 10, 12, 255))
    draw = ImageDraw.Draw(image)
    # Sparse industrial background bands.
    for x in range(0, 1920, 320):
        draw.rectangle((x, 0, x + 2, 1080), fill=(9, 25, 29, 255))
    for y in range(72, 1080, 150):
        draw.rectangle((0, y, 1920, y + 2), fill=(9, 25, 29, 255))

    panel(draw, (24, 16, 1896, 66), fill=(7, 13, 16, 252))
    draw.text((42, 23), "学院首区 · 半岛全貌", font=font(28, True), fill=TEXT)
    draw.text((1640, 29), "正常入学期   ·   石路巡哨", font=font(18), fill=MUTED)

    panel(draw, (24, 78, 1896, 170), fill=(8, 16, 19, 250))
    metrics = (("生命", "18 / 18", SAFE), ("魔力", "12 / 12", CYAN), ("金币", "11", AMBER),
               ("贡献", "2", SAFE), ("时序", "1 / 28", CYAN), ("探索", "1 / 12", CYAN), ("许可", "0 / 2", AMBER))
    chip_width = 250
    for index, (label, value, accent) in enumerate(metrics):
        x = 38 + index * 264
        draw.rectangle((x, 94, x + chip_width, 154), fill=RAISED, outline=(42, 56, 58, 255), width=1)
        draw.rectangle((x, 94, x + 4, 154), fill=accent)
        draw.text((x + 18, 102), label, font=font(16), fill=MUTED)
        value_box = draw.textbbox((0, 0), value, font=font(22, True))
        draw.text((x + chip_width - (value_box[2] - value_box[0]) - 16, 112), value, font=font(22, True), fill=TEXT)

    viewport = (24, 182, 1896, 1056)
    panel(draw, viewport, fill=(7, 13, 15, 255), outline=(50, 75, 77, 255), width=2)
    atlas_x = 24 + (1872 - ATLAS_SIZE[0]) // 2
    atlas_y = 182 + (874 - ATLAS_SIZE[1]) // 2
    image.alpha_composite(atlas, (atlas_x, atlas_y))

    anchors = scaled_anchors(module)
    route_layer = Image.new("RGBA", MOCKUP_SIZE, (0, 0, 0, 0))
    routes = ImageDraw.Draw(route_layer)
    drawn: set[tuple[str, str]] = set()
    current_id = "start"
    available = set(nodes[current_id]["next"])
    for source_id, node in nodes.items():
        for target_id in node["next"]:
            key = tuple(sorted((source_id, target_id)))
            if key in drawn or target_id not in nodes:
                continue
            drawn.add(key)
            source = (atlas_x + anchors[(node["x"], node["y"])][0], atlas_y + anchors[(node["x"], node["y"])][1])
            target_node = nodes[target_id]
            target = (atlas_x + anchors[(target_node["x"], target_node["y"])][0], atlas_y + anchors[(target_node["x"], target_node["y"])][1])
            if source_id == current_id and target_id in available or target_id == current_id and source_id in available:
                routes.line((source, target), fill=(55, 181, 204, 230), width=4)
            else:
                dashed_line(routes, source, target, (105, 122, 120, 72), width=2, dash=8, gap=10)
    image = Image.alpha_composite(image, route_layer)

    for node_id, node in nodes.items():
        if node_id == current_id:
            state = "current"
        elif node_id in available:
            state = "available"
        elif node["x"] <= 2 and node["y"] <= 2:
            state = "known"
        elif node["x"] >= 6 and node["y"] <= 2:
            state = "locked"
        else:
            state = "unknown"
        anchor = anchors[(node["x"], node["y"])]
        plate = marker(state, node["type"])
        image.alpha_composite(plate, (atlas_x + anchor[0] - 16, atlas_y + anchor[1] - 16))

    draw = ImageDraw.Draw(image)
    # Three first-layer controls only.
    for index, (symbol, accent) in enumerate((("◎", CYAN), ("−", TEXT), ("+", TEXT))):
        x = 1714 + index * 54
        draw.rectangle((x, 198, x + 46, 244), fill=(10, 21, 24, 238), outline=accent, width=2)
        box = draw.textbbox((0, 0), symbol, font=font(26, True))
        draw.text((x + (46 - (box[2] - box[0])) // 2, 203), symbol, font=font(26, True), fill=accent)

    # Four essential state cues, not seven text cards.
    legend_x, legend_y = 44, 986
    legend_items = (("current", "当前位置"), ("available", "可前往"), ("cleared", "已清理"), ("locked", "权限门"))
    draw.rectangle((legend_x - 10, legend_y - 8, legend_x + 420, legend_y + 52), fill=(6, 13, 16, 220), outline=(43, 61, 62, 255))
    for index, (state, label) in enumerate(legend_items):
        x = legend_x + index * 104
        image.alpha_composite(marker(state, "start").resize((24, 24), Image.Resampling.NEAREST), (x, legend_y + 4))
        draw.text((x + 30, legend_y + 5), label, font=font(14), fill=MUTED)

    # Compact selection card; long details remain on demand.
    info = (1400, 908, 1876, 1036)
    draw.rectangle(info, fill=(7, 15, 18, 232), outline=CYAN, width=2)
    draw.text((1420, 924), "石路巡哨", font=font(24, True), fill=TEXT)
    draw.text((1420, 960), "普通战   ·   +2 时序   ·   当前可进入", font=font(16), fill=CYAN_LIGHT)
    draw.text((1420, 990), "选择后查看敌情、恢复与奖励", font=font(15), fill=MUTED)
    return image


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    FORMAL_MARKER_DIR.mkdir(parents=True, exist_ok=True)
    module = load_source_module()
    nodes = parse_nodes()
    atlas = build_atlas(module)
    atlas_path = OUT / "OCC_M-A16_学院半岛全貌地图底图_v01.png"
    atlas.save(atlas_path, optimize=True)
    marker_states = ("current", "available", "cleared", "visited", "locked", "known", "unknown")
    marker_paths = []
    for state in marker_states:
        marker_path = FORMAL_MARKER_DIR / f"{state}.png"
        marker(state).save(marker_path, optimize=True)
        marker_paths.append(marker_path)
    mockup = compose_mockup(atlas, module, nodes)
    mockup_path = OUT / "OCC_M-A16_学院半岛地图正式界面构图_1920x1080_v01.png"
    mockup.save(mockup_path, optimize=True)
    mockup.resize((960, 540), Image.Resampling.NEAREST).save(
        OUT / "OCC_M-A16_学院半岛地图正式界面构图_960x540_v01.png", optimize=True)

    alpha = sorted(set(atlas.getchannel("A").getdata()))
    colors = len(set(atlas.getdata()))
    report = {
        "schema": "occ.formal.academy-map-redesign-review.v0.1",
        "status": "QA_PASS" if atlas.size == ATLAS_SIZE and alpha == [255] and colors <= 32 and len(nodes) == 40 else "QA_FAIL",
        "atlas": str(atlas_path.relative_to(ROOT)).replace("\\", "/"),
        "formalMarkers": [str(path.relative_to(ROOT)).replace("\\", "/") for path in marker_paths],
        "atlasSize": list(atlas.size),
        "logicalWorkSize": list(WORK_SIZE),
        "nearestScale": 2,
        "colors": colors,
        "alphaValues": alpha,
        "nodeCount": len(nodes),
        "markerCount": len(marker_paths),
        "mockupSize": list(mockup.size),
        "sha256": hashlib.sha256(atlas_path.read_bytes()).hexdigest(),
    }
    (OUT / "OCC_M-A16_学院半岛地图离线QA_v01.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()
