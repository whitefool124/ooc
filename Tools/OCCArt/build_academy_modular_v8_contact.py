"""Build a review-only 12x9 assembly from independently generated terrain sources.

This is deliberately not a Unity importer or a production asset exporter.  It keeps
the scene as discrete 32px cells and puts each cardinal edge in its own, unrotated
source region so the contact sheet can expose perspective and scale problems early.
"""

from pathlib import Path
from PIL import Image


ROOT = Path(r"E:\数据库\OCC_Codex")
SOURCE_DIR = ROOT / "ArtSource" / "academy_courtyard_modular_v8" / "sources"
OUTPUT = ROOT / "UnityProject" / "Reports" / "ArtReview" / "academy_environment_modular_v13_warm_map_assembly.png"

CELL = 32
COLS = 12
ROWS = 9
DISPLAY_SCALE = 4


def nearest_quarter(source: Image.Image) -> Image.Image:
    """The two sources were generated on the same 4x macro lattice.

    Nearest-only reduction is preview scaffolding, never a formal asset transform.
    It lets this contact sheet inspect a single apparent native pixel size.
    """
    return source.convert("RGBA").resize((source.width // 4, source.height // 4), Image.Resampling.NEAREST)


def put(canvas: Image.Image, image: Image.Image, xy: tuple[int, int]) -> None:
    canvas.alpha_composite(image, xy)


def main() -> None:
    terrain = nearest_quarter(Image.open(SOURCE_DIR / "academy_platform_terrain_source.png"))
    sides = nearest_quarter(Image.open(SOURCE_DIR / "academy_platform_side_edges_source.png"))
    assert terrain.size == (384, 256), terrain.size
    assert sides.size == (384, 256), sides.size

    # The board starts below the continuous north/backdrop strip.  It is always made
    # from individual cells, rather than retaining a baked centre rectangle.
    edge = 8
    board_x, board_y = CELL, CELL * 2
    board_w, board_h = COLS * CELL, ROWS * CELL
    canvas = Image.new("RGBA", (board_w + CELL * 2, board_h + CELL * 3), (111, 105, 61, 255))

    # Fixed, continuous background layer: visible only outside the platform.
    put(canvas, terrain.crop((0, 0, 384, 64)), (board_x, 0))
    put(canvas, terrain.crop((0, 224, 384, 256)), (board_x, board_y + board_h))

    # The left/right sides have separate source art.  We never rotate an edge.
    # Side gardens are a background layer, kept behind the directional edge strip.
    left_garden = sides.crop((0, 0, 32, 256))
    right_garden = sides.crop((352, 0, 384, 256))
    for y in range(board_y, board_y + board_h, 256):
        height = min(256, board_y + board_h - y)
        put(canvas, left_garden.crop((0, 0, CELL, height)), (0, y))
        put(canvas, right_garden.crop((0, 0, CELL, height)), (board_x + board_w, y))

    # Directional edges show only the contract's 8px facade depth.  The previous
    # version treated a wide decorative wall as an edge and made the map a fence.
    left_face = sides.crop((56, 64, 64, 72))
    right_face = sides.crop((320, 64, 328, 72))
    for y in range(board_y, board_y + board_h, edge):
        put(canvas, left_face, (board_x - edge, y))
        put(canvas, right_face, (board_x + board_w, y))

    # Six source cells are enough to judge continuity, while retaining a true 12x9
    # assembly.  The visible slab seam is the gameplay grid; no debug black frame.
    # Keep every row as a contiguous piece from the source.  The former random tile
    # sampling made otherwise-valid slab seams terminate halfway through a neighbour.
    source_rows = [terrain.crop((0, sy, 384, sy + CELL)) for sy in (64, 96, 128, 160, 192)]
    for y in range(ROWS):
        put(canvas, source_rows[y % len(source_rows)], (board_x, board_y + y * CELL))

    # Independent directional edge treatment.  North is a rear parapet, south gets
    # a visible front facade, and east/west retain their own wall-facing sources.
    put(canvas, terrain.crop((0, 32, 384, 64)), (board_x, board_y - CELL))
    south = terrain.crop((0, 224, 384, 232))
    put(canvas, south, (board_x, board_y + board_h))

    # Existing relaxed candidates are intentionally sparse.  This is an application
    # contact test only: their logical placement remains one cell even when a canvas
    # extends into a neighbour for the silhouette.
    prop_dir = ROOT / "ArtSource" / "academy_courtyard_warm_props" / "outputs"
    planter = Image.open(prop_dir / "academy_lamp_vine_planter_relaxed.png").convert("RGBA")
    for col, row in ((2, 1), (3, 1), (3, 6)):
        put(canvas, planter, (board_x + col * CELL, board_y + row * CELL))

    # Render at review scale with nearest-neighbour only.
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    rendered = canvas.resize((canvas.width * DISPLAY_SCALE, canvas.height * DISPLAY_SCALE), Image.Resampling.NEAREST)
    rendered.save(OUTPUT)
    print(OUTPUT)


if __name__ == "__main__":
    main()
