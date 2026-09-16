#!/usr/bin/env python3
"""Draw an original, clean, single-cell OCC heavy-cover pixel asset locally.

This is a native-grid redraw: no generated image is sampled, resized, traced or
recoloured. Geometry for the 64px master is authored directly on integer pixels.
"""

from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / "UnityProject/Assets/Game/Resources/Art/CombatTestArenaSephiriaDebug"
EVIDENCE = ROOT / "UnityProject/Reports/CombatTestArena/sephiria_debug_evidence"
SCALE_STUDY = ROOT / "UnityProject/Reports/CombatTestArena/local_redraw_scale_study"

BLACK = (0, 0, 0, 255)
INK = (29, 31, 37, 255)
STONE_SHADOW = (72, 68, 68, 255)
STONE_DARK = (91, 84, 80, 255)
STONE_MID = (119, 108, 100, 255)
STONE_LIGHT = (151, 137, 124, 255)
STONE_HIGHLIGHT = (184, 167, 148, 255)
IRON_DARK = (47, 51, 61, 255)
IRON_MID = (68, 74, 87, 255)
IRON_LIGHT = (100, 109, 124, 255)
BRASS = (142, 105, 59, 255)


def replace_outermost_pixel_with_black(image: Image.Image) -> Image.Image:
    image = image.convert("RGBA")
    alpha = image.getchannel("A")
    alpha_pixels = alpha.load(); result = image.copy(); pixels = result.load()
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


def draw_cover() -> Image.Image:
    image = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)

    # One screen-axis silhouette with stepped depth sides. The outer boundary
    # stays inside the eight-pixel safety margin.
    silhouette = [
        (14, 16), (21, 16), (21, 18), (43, 18), (43, 16), (50, 16),
        (50, 19), (53, 19), (53, 25), (55, 25), (55, 53), (52, 53),
        (52, 55), (12, 55), (12, 53), (8, 53), (8, 25), (11, 25),
        (11, 19), (14, 19),
    ]
    draw.polygon(silhouette, fill=STONE_DARK)

    # Broad top plane: only large connected value masses.
    draw.polygon(
        [(14, 20), (18, 17), (46, 17), (50, 20), (53, 28), (49, 34),
         (15, 34), (11, 28)],
        fill=STONE_MID,
    )
    draw.polygon([(16, 20), (20, 18), (44, 18), (48, 20), (50, 25),
                  (47, 27), (17, 27), (14, 25)], fill=STONE_LIGHT)
    draw.rectangle((18, 19, 45, 20), fill=STONE_HIGHLIGHT)
    draw.rectangle((16, 28, 48, 33), fill=STONE_MID)
    draw.rectangle((17, 28, 47, 29), fill=STONE_LIGHT)
    draw.rectangle((31, 18, 32, 33), fill=STONE_DARK)

    # Front facade: two quiet stone masses and one restrained iron brace.
    draw.rectangle((10, 34, 53, 52), fill=STONE_DARK)
    draw.rectangle((13, 35, 50, 47), fill=STONE_MID)
    draw.rectangle((14, 35, 49, 37), fill=STONE_LIGHT)
    draw.rectangle((14, 46, 49, 51), fill=STONE_SHADOW)
    draw.rectangle((31, 38, 32, 51), fill=INK)

    draw.rectangle((12, 39, 51, 44), fill=IRON_DARK)
    draw.rectangle((14, 40, 49, 41), fill=IRON_LIGHT)
    draw.rectangle((14, 42, 49, 43), fill=IRON_MID)
    draw.rectangle((28, 37, 35, 46), fill=IRON_DARK)
    draw.rectangle((29, 38, 34, 44), fill=IRON_MID)
    draw.rectangle((30, 39, 33, 40), fill=IRON_LIGHT)
    draw.rectangle((31, 42, 32, 43), fill=BRASS)

    # Two broad buttresses. Their highlights explain volume without texture.
    draw.rectangle((8, 31, 16, 53), fill=IRON_DARK)
    draw.rectangle((10, 32, 14, 51), fill=IRON_MID)
    draw.rectangle((11, 33, 13, 39), fill=IRON_LIGHT)
    draw.rectangle((8, 48, 18, 55), fill=STONE_DARK)
    draw.rectangle((10, 48, 16, 52), fill=STONE_MID)

    draw.rectangle((47, 31, 55, 53), fill=IRON_DARK)
    draw.rectangle((49, 32, 53, 51), fill=IRON_MID)
    draw.rectangle((50, 33, 52, 39), fill=IRON_LIGHT)
    draw.rectangle((45, 48, 55, 55), fill=STONE_DARK)
    draw.rectangle((47, 48, 53, 52), fill=STONE_MID)

    # Rear corner caps are the only raised secondary form.
    draw.rectangle((14, 16, 21, 22), fill=IRON_DARK)
    draw.rectangle((15, 17, 20, 19), fill=IRON_LIGHT)
    draw.rectangle((16, 18, 19, 20), fill=IRON_MID)
    draw.rectangle((43, 16, 50, 22), fill=IRON_DARK)
    draw.rectangle((44, 17, 49, 19), fill=IRON_LIGHT)
    draw.rectangle((45, 18, 48, 20), fill=IRON_MID)

    # Sparse, connected wear marks only; no single-pixel texture field.
    draw.rectangle((22, 23, 25, 24), fill=STONE_HIGHLIGHT)
    draw.rectangle((39, 30, 42, 31), fill=STONE_DARK)
    draw.rectangle((19, 48, 23, 49), fill=STONE_MID)

    return replace_outermost_pixel_with_black(image)


def draw_cover_low() -> Image.Image:
    """Independently authored low-resolution companion for the same one cell."""
    image = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    silhouette = [
        (7, 8), (11, 8), (11, 9), (21, 9), (21, 8), (25, 8),
        (25, 10), (27, 12), (27, 26), (25, 26), (25, 27),
        (7, 27), (7, 26), (4, 26), (4, 12), (6, 12), (6, 10), (7, 10),
    ]
    draw.polygon(silhouette, fill=STONE_DARK)
    draw.polygon([(7, 11), (9, 9), (23, 9), (25, 11), (26, 15),
                  (24, 17), (8, 17), (6, 15)], fill=STONE_MID)
    draw.polygon([(8, 11), (10, 10), (22, 10), (24, 11),
                  (24, 13), (8, 13)], fill=STONE_LIGHT)
    draw.rectangle((9, 10, 22, 10), fill=STONE_HIGHLIGHT)
    draw.rectangle((15, 10, 16, 16), fill=STONE_DARK)
    draw.rectangle((5, 17, 26, 25), fill=STONE_DARK)
    draw.rectangle((7, 18, 24, 22), fill=STONE_MID)
    draw.rectangle((7, 23, 24, 25), fill=STONE_SHADOW)
    draw.rectangle((6, 19, 25, 22), fill=IRON_DARK)
    draw.rectangle((7, 20, 24, 20), fill=IRON_LIGHT)
    draw.rectangle((14, 18, 17, 23), fill=IRON_DARK)
    draw.rectangle((15, 19, 16, 21), fill=IRON_MID)
    draw.point((15, 21), fill=BRASS)
    draw.rectangle((4, 16, 8, 27), fill=IRON_DARK)
    draw.rectangle((5, 17, 6, 24), fill=IRON_MID)
    draw.rectangle((4, 24, 9, 27), fill=STONE_DARK)
    draw.rectangle((23, 16, 27, 27), fill=IRON_DARK)
    draw.rectangle((25, 17, 26, 24), fill=IRON_MID)
    draw.rectangle((22, 24, 27, 27), fill=STONE_DARK)
    draw.rectangle((7, 8, 10, 11), fill=IRON_DARK)
    draw.rectangle((8, 9, 9, 9), fill=IRON_LIGHT)
    draw.rectangle((21, 8, 24, 11), fill=IRON_DARK)
    draw.rectangle((22, 9, 23, 9), fill=IRON_LIGHT)
    return replace_outermost_pixel_with_black(image)


def draw_cover_24() -> Image.Image:
    """Native 24px readability study; independently composed, never resized."""
    image = Image.new("RGBA", (24, 24), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)

    # At this tier the cover is one top mass, one front mass and two supports.
    draw.polygon(
        [(5, 5), (8, 5), (8, 6), (16, 6), (16, 5), (19, 5),
         (19, 7), (21, 9), (21, 19), (19, 19), (19, 21),
         (5, 21), (5, 20), (3, 20), (3, 9), (5, 7)],
        fill=STONE_DARK,
    )
    draw.polygon(
        [(5, 8), (7, 6), (17, 6), (19, 8), (20, 11),
         (18, 13), (6, 13), (4, 11)],
        fill=STONE_MID,
    )
    draw.polygon(
        [(6, 8), (8, 7), (16, 7), (18, 8), (18, 10), (6, 10)],
        fill=STONE_LIGHT,
    )
    draw.rectangle((7, 7, 16, 7), fill=STONE_HIGHLIGHT)
    draw.rectangle((5, 13, 19, 19), fill=STONE_DARK)
    draw.rectangle((6, 14, 18, 16), fill=STONE_MID)
    draw.rectangle((6, 17, 18, 19), fill=STONE_SHADOW)
    draw.rectangle((5, 14, 19, 16), fill=IRON_DARK)
    draw.rectangle((6, 14, 18, 14), fill=IRON_LIGHT)
    draw.rectangle((10, 13, 13, 17), fill=IRON_DARK)
    draw.rectangle((11, 14, 12, 16), fill=IRON_LIGHT)
    draw.rectangle((3, 12, 6, 20), fill=IRON_DARK)
    draw.rectangle((4, 13, 4, 18), fill=IRON_LIGHT)
    draw.rectangle((17, 12, 21, 20), fill=IRON_DARK)
    draw.rectangle((19, 13, 19, 18), fill=IRON_LIGHT)
    draw.rectangle((5, 5, 7, 8), fill=IRON_DARK)
    draw.rectangle((16, 5, 18, 8), fill=IRON_DARK)
    return replace_outermost_pixel_with_black(image)


def draw_cover_16() -> Image.Image:
    """Native 16px silhouette study using only four visible colours."""
    image = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)

    # No texture or fittings survive here: volume and gameplay weight come first.
    draw.polygon(
        [(4, 3), (6, 3), (6, 4), (10, 4), (10, 3), (12, 3),
         (12, 5), (14, 6), (14, 13), (12, 13), (12, 14),
         (4, 14), (4, 13), (2, 13), (2, 6), (4, 5)],
        fill=STONE_DARK,
    )
    draw.polygon(
        [(4, 5), (5, 4), (11, 4), (12, 5), (13, 7),
         (12, 8), (4, 8), (3, 7)],
        fill=STONE_MID,
    )
    draw.rectangle((5, 5, 11, 6), fill=STONE_LIGHT)
    draw.rectangle((3, 8, 13, 12), fill=STONE_DARK)
    draw.rectangle((4, 9, 12, 10), fill=STONE_MID)
    draw.rectangle((2, 8, 4, 13), fill=STONE_DARK)
    draw.rectangle((11, 8, 14, 13), fill=STONE_DARK)
    return replace_outermost_pixel_with_black(image)


def checker_contact(image: Image.Image, scale=4) -> Image.Image:
    zoom = image.resize((image.width * scale, image.height * scale), Image.Resampling.NEAREST)
    board = Image.new("RGBA", zoom.size, (184, 184, 184, 255))
    pixels = board.load()
    for y in range(board.height):
        for x in range(board.width):
            if ((x // 16) + (y // 16)) % 2:
                pixels[x, y] = (220, 220, 220, 255)
    board.alpha_composite(zoom)
    return board


def placement_contact(image: Image.Image) -> Image.Image:
    """Show four independent single-cell placements; not a multi-cell asset."""
    ground_path = DEST / "sephiria_ground_64.png"
    ground = Image.open(ground_path).convert("RGBA") if ground_path.exists() else Image.new(
        "RGBA", (64, 64), (86, 105, 83, 255)
    )
    board = Image.new("RGBA", (128, 128), (0, 0, 0, 255))
    for y in (0, 64):
        for x in (0, 64):
            board.alpha_composite(ground, (x, y))
            board.alpha_composite(image, (x, y))
    return board.resize((512, 512), Image.Resampling.NEAREST)


def main() -> None:
    DEST.mkdir(parents=True, exist_ok=True)
    EVIDENCE.mkdir(parents=True, exist_ok=True)
    SCALE_STUDY.mkdir(parents=True, exist_ok=True)
    asset = draw_cover()
    low = draw_cover_low()
    study_24 = draw_cover_24()
    study_16 = draw_cover_16()
    asset_path = DEST / "sephiria_heavy_cover_local_redraw_64.png"
    asset.save(asset_path)
    low.save(DEST / "sephiria_heavy_cover_local_redraw_32.png")
    low.save(SCALE_STUDY / "sephiria_heavy_cover_local_redraw_32.png")
    study_24.save(SCALE_STUDY / "sephiria_heavy_cover_local_redraw_24.png")
    study_16.save(SCALE_STUDY / "sephiria_heavy_cover_local_redraw_16.png")
    checker_contact(asset).save(EVIDENCE / "sephiria_heavy_cover_local_redraw_64_contact.png")
    checker_contact(low).save(EVIDENCE / "sephiria_heavy_cover_local_redraw_32_contact.png")
    checker_contact(low, scale=8).save(SCALE_STUDY / "sephiria_heavy_cover_local_redraw_32_contact.png")
    checker_contact(study_24, scale=8).save(SCALE_STUDY / "sephiria_heavy_cover_local_redraw_24_contact.png")
    checker_contact(study_16, scale=8).save(SCALE_STUDY / "sephiria_heavy_cover_local_redraw_16_contact.png")
    gray = asset.convert("LA").convert("RGBA"); gray.putalpha(asset.getchannel("A"))
    checker_contact(gray).save(EVIDENCE / "sephiria_heavy_cover_local_redraw_64_grayscale.png")
    placement_contact(asset).save(EVIDENCE / "sephiria_heavy_cover_local_redraw_2x2_placement.png")
    print(asset_path)


if __name__ == "__main__":
    main()
