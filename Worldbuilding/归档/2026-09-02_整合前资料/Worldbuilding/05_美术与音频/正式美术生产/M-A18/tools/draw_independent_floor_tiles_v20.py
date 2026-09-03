from __future__ import annotations

from pathlib import Path
import shutil

from PIL import Image, ImageDraw


M18 = Path(__file__).resolve().parents[1]
REPO = Path(__file__).resolve().parents[5]
FAMILY = "terrain_independent_tiles_v20"
SOURCE = M18 / "source" / FAMILY
NORMALIZED = M18 / "normalized" / FAMILY
QA = M18 / "QA" / FAMILY
UNITY = REPO / "UnityProject" / "Assets" / "Game" / "Resources" / "Art" / "FormalAcademyIndependentFloors32"

PALETTES = {
    "court": [(102, 100, 89), (157, 152, 132), (180, 174, 150), (198, 190, 164), (137, 134, 118)],
    "road": [(58, 59, 55), (91, 89, 81), (119, 115, 103), (139, 133, 117), (75, 75, 70)],
    "ruin": [(74, 76, 65), (119, 119, 96), (151, 149, 119), (174, 170, 137), (98, 99, 82)],
    "earth": [(91, 67, 49), (132, 96, 69), (151, 112, 82), (168, 127, 94), (113, 82, 59)],
}


def stone(
    draw: ImageDraw.ImageDraw,
    box: tuple[int, int, int, int],
    colors: list[tuple[int, int, int]],
    chip: int = 0,
) -> None:
    x0, y0, x1, y1 = box
    joint, base, light, _, shade = colors
    draw.rectangle(box, fill=base)
    if x1 - x0 >= 3:
        draw.line((x0, y0, x1, y0), fill=light)
        draw.line((x0, y0, x0, y1), fill=light)
        draw.line((x0, y1, x1, y1), fill=shade)
        draw.line((x1, y0, x1, y1), fill=shade)
    draw.point((x0, y1), fill=joint)
    draw.point((x1, y0), fill=joint)
    if chip == 1 and x1 - x0 > 5:
        draw.point((x1, y1), fill=joint)
        draw.point((x1 - 1, y1), fill=joint)
        draw.point((x1, y1 - 1), fill=joint)
    elif chip == 2 and x1 - x0 > 6:
        draw.point((x0, y0), fill=joint)
        draw.point((x0 + 1, y0), fill=joint)
        draw.point((x0, y0 + 1), fill=joint)


def court_tile(variant: int) -> Image.Image:
    colors = PALETTES["court"]
    image = Image.new("RGB", (32, 32), colors[0])
    draw = ImageDraw.Draw(image)
    stone(draw, (1, 1, 30, 30), colors, chip=1 if variant == 2 else 0)
    marks = [((6, 22, 13, 22),), ((19, 7, 25, 7),), ((7, 9, 11, 9), (20, 24, 25, 24)), ((8, 24, 15, 24),)][variant]
    for mark in marks:
        draw.line(mark, fill=colors[4])
        draw.point((mark[0], mark[1] - 1), fill=colors[2])
    flecks = [((8, 7), (22, 16)), ((6, 18), (24, 25)), ((18, 6), (8, 21)), ((12, 12), (24, 19))][variant]
    for x, y in flecks:
        draw.point((x, y), fill=colors[2])
        draw.point((x + 1, y), fill=colors[4])
    return image


def road_tile(variant: int) -> Image.Image:
    colors = PALETTES["road"]
    image = Image.new("RGB", (32, 32), colors[0])
    draw = ImageDraw.Draw(image)
    stone(draw, (1, 1, 30, 30), colors, chip=variant % 3)
    marks = [((5, 10, 13, 10), (19, 22, 26, 22)), ((8, 24, 17, 24),),
             ((20, 8, 26, 8), (6, 20, 12, 20)), ((7, 7, 15, 7), (18, 25, 24, 25))][variant]
    for mark in marks:
        draw.line(mark, fill=colors[4])
        draw.point((mark[0], mark[1] - 1), fill=colors[2])
    flecks = [((8, 6), (23, 16), (12, 26)), ((6, 15), (18, 8), (25, 24)),
              ((10, 24), (20, 14), (25, 6)), ((7, 19), (17, 5), (23, 14))][variant]
    for index, (x, y) in enumerate(flecks):
        draw.point((x, y), fill=colors[2] if index == 0 else colors[4])
        draw.point((x + 1, y), fill=colors[1])
        if index == 1:
            draw.point((x, y + 1), fill=colors[4])
    return image


RUIN_CRACKS = [
    [(6, 8), (11, 12), (9, 17), (15, 21)],
    [(24, 5), (20, 10), (22, 15), (16, 19), (18, 25)],
    [(5, 23), (10, 19), (8, 14), (14, 10), (12, 6)],
    [(7, 6), (12, 11), (17, 10), (20, 16), (26, 19)],
]


def ruin_tile(variant: int) -> Image.Image:
    colors = PALETTES["ruin"]
    image = Image.new("RGB", (32, 32), colors[0])
    draw = ImageDraw.Draw(image)
    stone(draw, (1, 1, 30, 30), colors, chip=1 + variant % 2)
    points = RUIN_CRACKS[variant]
    draw.line(points, fill=colors[4])
    draw.point(points[len(points) // 2], fill=colors[0])
    for index in range(1, len(points) - 1, 2):
        x, y = points[index]
        draw.line((x, y, x + (1 if variant % 2 == 0 else -1), y + 1), fill=colors[0])
    draw.line((5 + variant * 2, 26, 11 + variant * 2, 26), fill=colors[3])
    return image


EARTH_SCARS = [
    [((4, 7), (10, 6), (14, 8), (12, 11), (6, 11)), ((18, 22), (25, 21), (28, 23), (24, 25), (19, 25))],
    [((7, 4), (15, 4), (18, 6), (14, 9), (8, 8)), ((3, 24), (10, 21), (15, 23), (13, 26), (6, 27))],
    [((18, 5), (26, 6), (28, 9), (23, 11), (17, 9)), ((5, 18), (11, 17), (15, 20), (12, 23), (6, 22))],
    [((4, 9), (8, 6), (14, 7), (16, 10), (11, 12), (6, 12)), ((17, 23), (22, 20), (28, 22), (27, 25), (20, 26))],
]


def earth_tile(variant: int) -> Image.Image:
    colors = PALETTES["earth"]
    image = Image.new("RGB", (32, 32), colors[0])
    draw = ImageDraw.Draw(image)
    draw.rectangle((1, 1, 30, 30), fill=colors[1])
    draw.line((1, 1, 30, 1), fill=colors[2])
    draw.line((1, 1, 1, 30), fill=colors[2])
    draw.line((1, 30, 30, 30), fill=colors[4])
    draw.line((30, 1, 30, 30), fill=colors[4])
    for index, points in enumerate(EARTH_SCARS[variant]):
        draw.polygon(points, fill=colors[2] if index == 0 else colors[4])
        x0, y0 = points[0]
        x1, y1 = points[1]
        draw.line((x0, y0, x1, y1), fill=colors[3] if index == 0 else colors[1])
    for index, (x, y) in enumerate(((4, 16), (20, 15), (14, 28), (27, 15))):
        if index != variant:
            draw.line((x, y, x + 3, y), fill=colors[4])
    return image


def main() -> None:
    for directory in (SOURCE, NORMALIZED, QA, UNITY):
        directory.mkdir(parents=True, exist_ok=True)
    makers = {"court": court_tile, "road": road_tile, "ruin": ruin_tile, "earth": earth_tile}
    contact = Image.new("RGB", (512, 512), (25, 25, 23))
    clean_contact = Image.new("RGB", (512, 512), (25, 25, 23))
    draw = ImageDraw.Draw(contact)
    for family_index, (family, maker) in enumerate(makers.items()):
        for variant in range(4):
            asset_id = f"academy_block_{family}_{chr(97 + variant)}"
            image = maker(variant)
            source = SOURCE / f"{asset_id}_source.png"
            output = NORMALIZED / f"{asset_id}.png"
            image.save(source, optimize=True)
            image.save(output, optimize=True)
            shutil.copyfile(output, UNITY / output.name)
            image.save(QA / f"{asset_id}_1x.png", optimize=True)
            image.resize((128, 128), Image.Resampling.NEAREST).save(QA / f"{asset_id}_4x.png", optimize=True)
            image.convert("L").save(QA / f"{asset_id}_grayscale.png", optimize=True)
            checker = Image.new("RGB", image.size, (194, 194, 194)); checker.paste(image, (0, 0))
            checker.save(QA / f"{asset_id}_checker.png", optimize=True)
            x, y = variant * 128, family_index * 128
            contact.paste(image.resize((128, 128), Image.Resampling.NEAREST), (x, y))
            clean_contact.paste(image.resize((128, 128), Image.Resampling.NEAREST), (x, y))
            draw.text((x + 4, y + 4), asset_id, fill=(235, 229, 212), stroke_width=1, stroke_fill=(25, 25, 23))
    contact.save(QA / "academy_independent_tiles_v20_contact.png", optimize=True)
    clean_contact.save(QA / "academy_independent_tiles_v20_contact_clean.png", optimize=True)


if __name__ == "__main__":
    main()
