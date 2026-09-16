#!/usr/bin/env python3
"""Assemble existing academy art only; this is an offline contact, not game content."""
from pathlib import Path
from PIL import Image
from PIL import ImageDraw

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "UnityProject/Assets/Game/Resources/Art"
OUT = ROOT / "UnityProject/Reports/ArtReview/academy_environment_modular_voxel_contact_v7_perimeter.png"
CELL, COLS, ROWS = 32, 12, 9


def image(relative: str) -> Image.Image:
    return Image.open(ART / relative).convert("RGBA")


reference = Image.open(ROOT / "ArtSource/academy_courtyard_warm_ground_batch/decoded/academy_courtyard_background_relaxed_decoded.png").convert("RGBA")
margin = CELL
board_x, board_y = margin, margin
canvas = Image.new("RGBA", (COLS * CELL + margin * 2, ROWS * CELL + margin * 2 + 8), (91, 70, 49, 255))

# Each logical cell is assembled from one unscaled 32px sample; the reference
# supplies only shared material pixels, never a baked board inside the map.
surface_samples = [
    reference.crop((32, 40, 64, 72)),
    reference.crop((64, 40, 96, 72)),
    reference.crop((96, 40, 128, 72)),
    reference.crop((128, 40, 160, 72)),
]
for y in range(ROWS):
    for x in range(COLS):
        canvas.alpha_composite(surface_samples[(x * 3 + y * 5) % len(surface_samples)],
                               (board_x + x * CELL, board_y + y * CELL))

# South-edge modules are individual 32x40 samples: 32px walkable top plus
# 8px front facade. They are not a board-wide frame or a scaled background.
edge_samples = [reference.crop((x, 80, x + CELL, 120)) for x in (32, 64, 96, 128)]
for x in range(COLS):
    canvas.alpha_composite(edge_samples[x % len(edge_samples)],
                           (board_x + x * CELL, board_y + (ROWS - 1) * CELL))

# Each side is authored from a different source strip; no rotated lighting or
# facade is used. These are environment modules surrounding, not replacing,
# the 12×9 logical board.
north_samples = [reference.crop((x, 0, x + CELL, CELL)) for x in (32, 64, 96, 128)]
west_samples = [reference.crop((0, y, CELL, y + CELL)) for y in (32, 64, 96)]
east_samples = [reference.crop((160, y, 192, y + CELL)) for y in (32, 64, 96)]
for x in range(COLS):
    canvas.alpha_composite(north_samples[x % len(north_samples)], (board_x + x * CELL, 0))
for y in range(ROWS):
    canvas.alpha_composite(west_samples[y % len(west_samples)], (0, board_y + y * CELL))
    canvas.alpha_composite(east_samples[y % len(east_samples)], (board_x + COLS * CELL, board_y + y * CELL))

# Tactical readability comes from one native-pixel warm grout seam every logical
# 32px cell, not from a busy masonry texture or a black card outline.
grid = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
draw = ImageDraw.Draw(grid)
for x in range(COLS + 1):
    px = board_x + x * CELL
    draw.line((px, board_y, px, board_y + ROWS * CELL), fill=(151, 119, 80, 54), width=1)
for y in range(ROWS + 1):
    py = board_y + y * CELL
    draw.line((board_x, py, board_x + COLS * CELL, py), fill=(151, 119, 80, 54), width=1)
canvas.alpha_composite(grid)


def place(relative: str, x: int, y: int) -> None:
    canvas.alpha_composite(image(relative), (x * CELL, y * CELL))


canvas.alpha_composite(image("FormalAcademyStructures32/academy_north_dais_6x2.png"),
                       (board_x + 3 * CELL, board_y))
canvas.alpha_composite(Image.open(ROOT / "ArtSource/academy_courtyard_warm_props/outputs/academy_heavy_barricade_relaxed.png").convert("RGBA"),
                       (board_x + CELL, board_y + 4 * CELL))
canvas.alpha_composite(Image.open(ROOT / "ArtSource/academy_courtyard_warm_props/outputs/academy_water_channel_relaxed.png").convert("RGBA"),
                       (board_x + 5 * CELL, board_y + 3 * CELL))
planter = Image.open(ROOT / "ArtSource/academy_courtyard_warm_props/outputs/academy_lamp_vine_planter_relaxed.png").convert("RGBA")
canvas.alpha_composite(planter, (board_x + 4 * CELL, board_y + 5 * CELL))
canvas.alpha_composite(planter, (board_x + 5 * CELL, board_y + 5 * CELL))
canvas.alpha_composite(planter, (board_x + 4 * CELL, board_y + 6 * CELL))
canvas.alpha_composite(Image.open(ROOT / "ArtSource/academy_courtyard_warm_props/outputs/academy_aether_fixture_relaxed.png").convert("RGBA"),
                       (board_x + 9 * CELL, board_y + 5 * CELL))

OUT.parent.mkdir(parents=True, exist_ok=True)
canvas.resize((canvas.width * 4, canvas.height * 4), Image.Resampling.NEAREST).save(OUT)
print(OUT)
