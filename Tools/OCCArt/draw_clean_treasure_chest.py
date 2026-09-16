#!/usr/bin/env python3
"""Draw a clean OCC single-cell loot chest on native 64px and 32px grids.

The 64px delivery deliberately uses a coarse, roughly 32px visual language:
large two-pixel structural clusters and very few one-pixel accents. The 32px
companion is authored independently rather than resized from the master.
"""

from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / "UnityProject/Assets/Game/Resources/Art/CombatTestArenaSephiriaDebug"
EVIDENCE = ROOT / "UnityProject/Reports/CombatTestArena/sephiria_debug_evidence"

BLACK = (0, 0, 0, 255)
WOOD_SHADOW = (66, 45, 38, 255)
WOOD_DARK = (88, 57, 43, 255)
WOOD_MID = (126, 82, 54, 255)
WOOD_LIGHT = (163, 112, 70, 255)
WOOD_HIGHLIGHT = (194, 145, 91, 255)
IRON_DARK = (43, 47, 55, 255)
IRON_MID = (67, 72, 82, 255)
IRON_LIGHT = (102, 108, 116, 255)
BRASS_DARK = (116, 76, 35, 255)
BRASS = (181, 127, 51, 255)


def replace_outermost_pixel_with_black(image: Image.Image) -> Image.Image:
    image = image.convert("RGBA")
    alpha = image.getchannel("A")
    alpha_pixels = alpha.load()
    result = image.copy()
    pixels = result.load()
    for y in range(image.height):
        for x in range(image.width):
            if not alpha_pixels[x, y]:
                continue
            if any(
                nx < 0 or ny < 0 or nx >= image.width or ny >= image.height
                or not alpha_pixels[nx, ny]
                for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1))
            ):
                pixels[x, y] = BLACK
    return result


def draw_chest_64() -> Image.Image:
    image = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)

    # One compact, screen-axis-aligned silhouette inside the single-cell margin.
    silhouette = [
        (16, 14), (48, 14), (48, 16), (51, 16), (51, 19),
        (53, 19), (53, 29), (55, 29), (55, 53), (52, 53),
        (52, 55), (12, 55), (12, 54), (9, 54), (9, 29),
        (11, 29), (11, 20), (13, 20), (13, 17), (16, 17),
    ]
    draw.polygon(silhouette, fill=WOOD_DARK)

    # Broad visible lid plane. Coordinates mostly move in two-pixel steps.
    draw.polygon(
        [(14, 20), (18, 16), (46, 16), (50, 20), (52, 27),
         (48, 32), (16, 32), (12, 27)],
        fill=WOOD_MID,
    )
    draw.polygon(
        [(17, 20), (20, 18), (44, 18), (47, 20), (49, 25),
         (46, 27), (18, 27), (15, 25)],
        fill=WOOD_LIGHT,
    )
    draw.rectangle((20, 18, 43, 19), fill=WOOD_HIGHLIGHT)
    draw.rectangle((18, 27, 46, 31), fill=WOOD_DARK)
    draw.rectangle((20, 27, 44, 28), fill=WOOD_MID)

    # Quiet front face: two wood masses divided by a single iron rim.
    draw.rectangle((11, 32, 53, 53), fill=WOOD_DARK)
    draw.rectangle((14, 35, 50, 49), fill=WOOD_MID)
    draw.rectangle((14, 35, 50, 38), fill=WOOD_LIGHT)
    draw.rectangle((14, 47, 50, 52), fill=WOOD_SHADOW)
    draw.rectangle((11, 31, 53, 36), fill=IRON_DARK)
    draw.rectangle((14, 32, 50, 33), fill=IRON_LIGHT)
    draw.rectangle((14, 34, 50, 35), fill=IRON_MID)

    # Three structural bands are large blocks, not fine decoration.
    draw.rectangle((15, 34, 20, 53), fill=IRON_DARK)
    draw.rectangle((17, 36, 18, 49), fill=IRON_MID)
    draw.rectangle((44, 34, 49, 53), fill=IRON_DARK)
    draw.rectangle((46, 36, 47, 49), fill=IRON_MID)
    draw.rectangle((28, 31, 36, 45), fill=IRON_DARK)
    draw.rectangle((30, 34, 34, 42), fill=BRASS_DARK)
    draw.rectangle((31, 35, 33, 39), fill=BRASS)
    draw.rectangle((31, 40, 33, 42), fill=IRON_LIGHT)

    # Chunky feet anchor the chest to the floor without a painted soft shadow.
    draw.rectangle((12, 51, 21, 55), fill=IRON_DARK)
    draw.rectangle((43, 51, 52, 55), fill=IRON_DARK)
    draw.rectangle((15, 51, 20, 52), fill=IRON_MID)
    draw.rectangle((44, 51, 49, 52), fill=IRON_MID)

    return replace_outermost_pixel_with_black(image)


def draw_chest_32() -> Image.Image:
    """Independently authored companion preserving the same identity."""
    image = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)

    silhouette = [
        (8, 7), (24, 7), (24, 8), (26, 9), (26, 14), (27, 15),
        (27, 27), (26, 27), (6, 27),
        (4, 27), (4, 15), (6, 14), (6, 10), (8, 8),
    ]
    draw.polygon(silhouette, fill=WOOD_DARK)
    draw.polygon(
        [(7, 10), (9, 8), (23, 8), (25, 10), (26, 13),
         (24, 16), (8, 16), (6, 13)],
        fill=WOOD_MID,
    )
    draw.polygon(
        [(8, 10), (10, 9), (22, 9), (24, 10), (24, 12), (8, 12)],
        fill=WOOD_LIGHT,
    )
    draw.rectangle((10, 9, 21, 9), fill=WOOD_HIGHLIGHT)
    draw.rectangle((8, 14, 24, 15), fill=WOOD_DARK)
    draw.rectangle((5, 16, 27, 26), fill=WOOD_DARK)
    draw.rectangle((7, 18, 25, 24), fill=WOOD_MID)
    draw.rectangle((7, 23, 25, 26), fill=WOOD_SHADOW)
    draw.rectangle((5, 15, 27, 18), fill=IRON_DARK)
    draw.rectangle((7, 16, 25, 16), fill=IRON_LIGHT)
    draw.rectangle((8, 17, 10, 27), fill=IRON_DARK)
    draw.rectangle((22, 17, 24, 27), fill=IRON_DARK)
    draw.rectangle((14, 15, 18, 22), fill=IRON_DARK)
    draw.rectangle((15, 17, 17, 20), fill=BRASS_DARK)
    draw.point((16, 18), fill=BRASS)
    draw.rectangle((6, 25, 11, 27), fill=IRON_DARK)
    draw.rectangle((21, 25, 26, 27), fill=IRON_DARK)
    return replace_outermost_pixel_with_black(image)


def checker_contact(image: Image.Image, scale: int) -> Image.Image:
    zoom = image.resize((image.width * scale, image.height * scale), Image.Resampling.NEAREST)
    board = Image.new("RGBA", zoom.size, (184, 184, 184, 255))
    pixels = board.load()
    for y in range(board.height):
        for x in range(board.width):
            if ((x // 16) + (y // 16)) % 2:
                pixels[x, y] = (220, 220, 220, 255)
    board.alpha_composite(zoom)
    return board


def ground_contact(image: Image.Image) -> Image.Image:
    ground_path = DEST / "sephiria_ground_64.png"
    if ground_path.exists():
        ground = Image.open(ground_path).convert("RGBA")
    else:
        ground = Image.new("RGBA", (64, 64), (76, 82, 78, 255))
    ground.alpha_composite(image)
    return ground.resize((256, 256), Image.Resampling.NEAREST)


def main() -> None:
    DEST.mkdir(parents=True, exist_ok=True)
    EVIDENCE.mkdir(parents=True, exist_ok=True)
    master = draw_chest_64()
    companion = draw_chest_32()
    master_path = DEST / "sephiria_loot_chest_local_redraw_64.png"
    companion_path = DEST / "sephiria_loot_chest_local_redraw_32.png"
    master.save(master_path)
    companion.save(companion_path)
    checker_contact(master, 4).save(EVIDENCE / "sephiria_loot_chest_local_redraw_64_contact.png")
    checker_contact(companion, 8).save(EVIDENCE / "sephiria_loot_chest_local_redraw_32_contact.png")
    gray = master.convert("LA").convert("RGBA")
    gray.putalpha(master.getchannel("A"))
    checker_contact(gray, 4).save(EVIDENCE / "sephiria_loot_chest_local_redraw_64_grayscale.png")
    ground_contact(master).save(EVIDENCE / "sephiria_loot_chest_local_redraw_64_ground_contact.png")
    print(master_path)


if __name__ == "__main__":
    main()
