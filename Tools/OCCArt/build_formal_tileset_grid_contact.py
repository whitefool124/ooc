"""Review-only assembly of existing native 32px terrain assets.

This deliberately consumes only the established FormalAcademyCombat32 sprites.
No source image is resized, cropped, recoloured, or used as a material substitute.
The one-pixel grid is a tactical readability overlay, not a replacement texture.
"""

from pathlib import Path
from PIL import Image, ImageDraw


ROOT = Path(r"E:\数据库\OCC_Codex")
ART = ROOT / "UnityProject" / "Assets" / "Game" / "Resources" / "Art" / "FormalAcademyCombat32"
OUTPUT = ROOT / "UnityProject" / "Reports" / "ArtReview" / "academy_existing_tileset_grid_contract_v1.png"

CELL, COLS, ROWS, SCALE = 32, 12, 9, 4


def load(name: str) -> Image.Image:
    image = Image.open(ART / name).convert("RGBA")
    assert image.size == (CELL, CELL), (name, image.size)
    return image


def put(canvas: Image.Image, image: Image.Image, x: int, y: int) -> None:
    canvas.alpha_composite(image, (x, y))


def main() -> None:
    ground = [load(f"academy_courtyard_{suffix}.png") for suffix in "abcd"]
    edges = {direction: load(f"academy_grass_edge_{direction}.png") for direction in "nsew"}

    origin_x = CELL
    origin_y = CELL
    canvas = Image.new("RGBA", ((COLS + 2) * CELL, (ROWS + 2) * CELL), (103, 92, 63, 255))

    # Existing directional edge assets form the environmental ring without rotation.
    for col in range(COLS):
        put(canvas, edges["n"], origin_x + col * CELL, 0)
        put(canvas, edges["s"], origin_x + col * CELL, origin_y + ROWS * CELL)
    for row in range(ROWS):
        put(canvas, edges["w"], 0, origin_y + row * CELL)
        put(canvas, edges["e"], origin_x + COLS * CELL, origin_y + row * CELL)

    # Exact native 32px terrain cells; their variation never changes grid geometry.
    for row in range(ROWS):
        for col in range(COLS):
            put(canvas, ground[(col * 3 + row) % len(ground)], origin_x + col * CELL, origin_y + row * CELL)

    # Explicit gameplay grid: exactly one native pixel, consistently drawn on every
    # cell boundary.  It remains a separate review overlay until runtime approval.
    draw = ImageDraw.Draw(canvas)
    grid = (102, 81, 55, 190)
    for col in range(COLS + 1):
        x = origin_x + col * CELL
        draw.line((x, origin_y, x, origin_y + ROWS * CELL), fill=grid, width=1)
    for row in range(ROWS + 1):
        y = origin_y + row * CELL
        draw.line((origin_x, y, origin_x + COLS * CELL, y), fill=grid, width=1)

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    canvas.resize((canvas.width * SCALE, canvas.height * SCALE), Image.Resampling.NEAREST).save(OUTPUT)
    print(OUTPUT)


if __name__ == "__main__":
    main()
