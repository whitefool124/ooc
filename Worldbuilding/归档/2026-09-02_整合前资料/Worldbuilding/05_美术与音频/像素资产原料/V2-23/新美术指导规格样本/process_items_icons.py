from __future__ import annotations

import json
from collections import deque
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parent
ITEMS = [
    ("occ_fire_canister_v03", ROOT / "raw_v03" / "01_fire_canister.png", 32, 16),
    ("occ_grid_disposer_v03", ROOT / "raw_v03" / "02_grid_disposer.png", 32, 16),
    ("occ_recon_beacon_v03", ROOT / "raw_v03" / "03_recon_beacon.png", 32, 16),
]
AUTHORED_ICONS = [
    ("occ_action_v03", ROOT / "Icons16" / "occ_action_v03.png", 16, 4),
    ("occ_aether_v03", ROOT / "Icons16" / "occ_aether_v03.png", 16, 4),
    ("occ_enemy_ranged_v03", ROOT / "Icons16" / "occ_enemy_ranged_v03.png", 16, 4),
]


def flood_remove_border_background(image: Image.Image) -> Image.Image:
    """Preserve native alpha; otherwise remove dark, edge-connected backdrop only."""
    rgba = image.convert("RGBA")
    pixels = rgba.load()
    width, height = rgba.size
    if any(pixels[x, y][3] < 250 for y in range(height) for x in range(width)):
        return rgba

    # Remove the exact neon-green or magenta chroma key requested for this batch.
    keyed = 0
    for y in range(height):
        for x in range(width):
            r, g, b, _ = pixels[x, y]
            green_key = g > 180 and g > r * 1.65 and g > b * 1.65
            magenta_key = r > 70 and b > 60 and g < 105 and abs(r - b) < 100
            if green_key or magenta_key:
                pixels[x, y] = (0, 0, 0, 0)
                keyed += 1
    if keyed:
        return rgba

    # The image prompts request a dark neutral key background. It is safe to remove only
    # pixels connected to an outer edge, leaving separated dark outlines intact.
    def is_backdrop(x: int, y: int) -> bool:
        r, g, b, _ = pixels[x, y]
        return max(r, g, b) <= 52 and max(r, g, b) - min(r, g, b) <= 26

    queue: deque[tuple[int, int]] = deque()
    seen: set[tuple[int, int]] = set()
    for x in range(width):
        queue.extend(((x, 0), (x, height - 1)))
    for y in range(height):
        queue.extend(((0, y), (width - 1, y)))
    while queue:
        x, y = queue.popleft()
        if (x, y) in seen or not (0 <= x < width and 0 <= y < height) or not is_backdrop(x, y):
            continue
        seen.add((x, y))
        pixels[x, y] = (0, 0, 0, 0)
        queue.extend(((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)))
    return rgba


def quantize_rgba(image: Image.Image, max_colors: int) -> Image.Image:
    alpha = image.getchannel("A")
    rgb = image.convert("RGB").quantize(colors=max_colors, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert("RGB")
    result = Image.merge("RGBA", (*rgb.split(), alpha))
    result.putalpha(result.getchannel("A").point(lambda a: 255 if a >= 128 else 0))
    return result


def bounds(image: Image.Image):
    return image.getchannel("A").getbbox()


def colors(image: Image.Image) -> int:
    return len({px[:3] for px in image.getdata() if px[3] > 0})


def make_qa(image: Image.Image, destination: Path, label: str) -> None:
    scale = 12 if image.width == 32 else 20
    canvas = Image.new("RGBA", (image.width * scale, image.height * scale), (33, 37, 45, 255))
    draw = ImageDraw.Draw(canvas)
    cell = scale * 2
    for y in range(0, canvas.height, cell):
        for x in range(0, canvas.width, cell):
            if ((x // cell) + (y // cell)) % 2 == 0:
                draw.rectangle((x, y, x + cell - 1, y + cell - 1), fill=(48, 54, 65, 255))
    enlarged = image.resize(canvas.size, Image.Resampling.NEAREST)
    canvas.alpha_composite(enlarged)
    grid = ImageDraw.Draw(canvas)
    for x in range(0, canvas.width + 1, scale):
        grid.line((x, 0, x, canvas.height), fill=(255, 255, 255, 38), width=1)
    for y in range(0, canvas.height + 1, scale):
        grid.line((0, y, canvas.width, y), fill=(255, 255, 255, 38), width=1)
    destination.parent.mkdir(parents=True, exist_ok=True)
    canvas.convert("RGB").save(destination)


def run_job(name: str, source: Path, logical_size: int, max_colors: int):
    original = Image.open(source).convert("RGBA")
    cleaned = flood_remove_border_background(original)
    normalized = cleaned.resize((logical_size, logical_size), Image.Resampling.NEAREST)
    normalized = quantize_rgba(normalized, max_colors)
    kind = "Items32" if logical_size == 32 else "Icons16"
    output = ROOT / kind / f"{name}.png"
    output.parent.mkdir(parents=True, exist_ok=True)
    normalized.save(output)
    qa = ROOT / "QA" / name / "qa_pixel_grid.png"
    make_qa(normalized, qa, name)
    value = {
        "name": name,
        "source": str(source.relative_to(ROOT)),
        "asset": str(output.relative_to(ROOT)),
        "qa": str(qa.relative_to(ROOT)),
        "logicalSize": [logical_size, logical_size],
        "paletteColors": colors(normalized),
        "hardAlpha": all(a in (0, 255) for a in normalized.getchannel("A").getdata()),
        "visibleBounds": list(bounds(normalized)) if bounds(normalized) else None,
        "machineStatus": "PASS" if colors(normalized) <= max_colors and bounds(normalized) else "FAIL",
        "manualStatus": "QA_PENDING",
    }
    print(json.dumps(value, ensure_ascii=False))
    return value


def run_authored_icon(name: str, source: Path, logical_size: int, max_colors: int):
    image = Image.open(source).convert("RGBA")
    qa = ROOT / "QA" / name / "qa_pixel_grid.png"
    make_qa(image, qa, name)
    value = {
        "name": name,
        "source": "hand-authored 16x16 glyph",
        "asset": str(source.relative_to(ROOT)),
        "qa": str(qa.relative_to(ROOT)),
        "logicalSize": [logical_size, logical_size],
        "paletteColors": colors(image),
        "hardAlpha": all(a in (0, 255) for a in image.getchannel("A").getdata()),
        "visibleBounds": list(bounds(image)) if bounds(image) else None,
        "machineStatus": "PASS" if image.size == (16, 16) and colors(image) <= max_colors and bounds(image) else "FAIL",
        "manualStatus": "READY_FOR_REVIEW",
    }
    print(json.dumps(value, ensure_ascii=False))
    return value


def main():
    report = [run_job(*job) for job in ITEMS] + [run_authored_icon(*job) for job in AUTHORED_ICONS]
    (ROOT / "QA" / "items_icons_report.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    board = Image.new("RGB", (1536, 960), (24, 28, 35))
    for index, entry in enumerate(report):
        asset = Image.open(ROOT / entry["asset"]).convert("RGBA")
        cell_x = (index % 3) * 512
        cell_y = (index // 3) * 480
        checker = Image.new("RGB", (480, 480), (40, 46, 57))
        checker_draw = ImageDraw.Draw(checker)
        for y in range(0, 480, 48):
            for x in range(0, 480, 48):
                if (x // 48 + y // 48) % 2:
                    checker_draw.rectangle((x, y, x + 47, y + 47), fill=(52, 59, 72))
        enlarged = asset.resize((480, 480), Image.Resampling.NEAREST)
        checker.paste(enlarged, (0, 0), enlarged)
        board.paste(checker, (cell_x + 16, cell_y))
        label = ImageDraw.Draw(board)
        label.text((cell_x + 16, cell_y + 452), entry["name"], fill=(220, 225, 235))
        label.text((cell_x + 16, cell_y + 468), f'{entry["logicalSize"][0]}px | {entry["paletteColors"]} colors | {entry["machineStatus"]}', fill=(152, 170, 190))
    board.save(ROOT / "QA" / "items_icons_overview.png")


if __name__ == "__main__":
    main()
