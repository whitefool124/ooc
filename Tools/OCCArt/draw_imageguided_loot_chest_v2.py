#!/usr/bin/env python3
"""Native-grid redraw of an OCC loot chest using an image-model concept.

The concept contributes proportions and the double-strap/U-lock identity only.
No source pixels are sampled, traced, resized, quantized or recoloured.
"""

from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / "UnityProject/Assets/Game/Resources/Art/CombatTestArenaSephiriaDebug"
EVIDENCE = ROOT / "UnityProject/Reports/CombatTestArena/sephiria_debug_evidence"

BLACK = (0, 0, 0, 255)
WOOD_SHADOW = (57, 38, 34, 255)
WOOD_DARK = (79, 49, 38, 255)
WOOD_MID = (116, 70, 45, 255)
WOOD_LIGHT = (151, 98, 58, 255)
WOOD_HIGHLIGHT = (183, 128, 76, 255)
IRON_SHADOW = (35, 38, 46, 255)
IRON_DARK = (48, 53, 64, 255)
IRON_MID = (72, 79, 93, 255)
IRON_LIGHT = (108, 116, 131, 255)
BRASS_DARK = (116, 76, 28, 255)
BRASS = (178, 124, 43, 255)
BRASS_LIGHT = (217, 169, 70, 255)


def replace_outermost_pixel_with_black(image: Image.Image) -> Image.Image:
    image = image.convert("RGBA")
    alpha = image.getchannel("A")
    source = alpha.load()
    result = image.copy()
    pixels = result.load()
    for y in range(image.height):
        for x in range(image.width):
            if not source[x, y]:
                continue
            if any(
                nx < 0 or ny < 0 or nx >= image.width or ny >= image.height
                or not source[nx, ny]
                for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1))
            ):
                pixels[x, y] = BLACK
    return result


def draw_chest_64() -> Image.Image:
    image = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)

    silhouette = [
        (15, 15), (49, 15), (49, 17), (52, 17), (52, 20),
        (54, 20), (54, 27), (55, 27), (55, 53), (53, 53),
        (53, 55), (11, 55), (11, 53), (8, 53), (8, 27),
        (10, 27), (10, 20), (12, 20), (12, 17), (15, 17),
    ]
    draw.polygon(silhouette, fill=IRON_DARK)

    # Visible lid top: broad, quiet and brighter than the front face.
    draw.polygon(
        [(13, 20), (17, 17), (47, 17), (51, 20), (53, 25),
         (49, 29), (15, 29), (11, 25)],
        fill=WOOD_MID,
    )
    draw.polygon(
        [(15, 20), (19, 18), (45, 18), (49, 20), (50, 23),
         (47, 25), (17, 25), (14, 23)],
        fill=WOOD_LIGHT,
    )
    draw.rectangle((20, 18, 43, 19), fill=WOOD_HIGHLIGHT)
    draw.rectangle((16, 25, 48, 28), fill=WOOD_DARK)

    # Lid front and body use only large connected wood planes.
    draw.rectangle((10, 29, 54, 37), fill=WOOD_DARK)
    draw.rectangle((13, 30, 51, 34), fill=WOOD_MID)
    draw.rectangle((13, 30, 51, 31), fill=WOOD_LIGHT)
    draw.rectangle((10, 37, 54, 53), fill=IRON_SHADOW)
    draw.rectangle((14, 40, 50, 51), fill=WOOD_MID)
    draw.rectangle((14, 40, 50, 42), fill=WOOD_LIGHT)
    draw.rectangle((14, 49, 50, 52), fill=WOOD_SHADOW)

    # Strong continuous rim separating lid and body.
    draw.rectangle((9, 34, 55, 40), fill=IRON_DARK)
    draw.rectangle((12, 35, 52, 36), fill=IRON_LIGHT)
    draw.rectangle((12, 37, 52, 39), fill=IRON_MID)

    # Image-guided identity: paired vertical straps with chunky corner caps.
    for left in (17, 42):
        draw.rectangle((left, 17, left + 5, 52), fill=IRON_DARK)
        draw.rectangle((left + 2, 19, left + 3, 48), fill=IRON_MID)
        draw.rectangle((left + 1, 17, left + 4, 19), fill=IRON_LIGHT)
        draw.rectangle((left - 1, 15, left + 6, 19), fill=IRON_DARK)
        draw.rectangle((left + 1, 16, left + 4, 17), fill=IRON_LIGHT)
        draw.rectangle((left - 1, 49, left + 6, 54), fill=IRON_DARK)
        draw.rectangle((left + 1, 49, left + 4, 50), fill=IRON_LIGHT)

    # Corner guards frame the silhouette but leave wood dominant.
    draw.rectangle((8, 29, 13, 52), fill=IRON_DARK)
    draw.rectangle((10, 32, 11, 48), fill=IRON_MID)
    draw.rectangle((51, 29, 55, 52), fill=IRON_DARK)
    draw.rectangle((53, 32, 54, 48), fill=IRON_MID)

    # One recognisable U-lock replaces all generated micro-decoration.
    draw.rectangle((27, 34, 37, 41), fill=IRON_DARK)
    draw.rectangle((29, 35, 35, 39), fill=BRASS_DARK)
    draw.rectangle((30, 35, 34, 36), fill=BRASS_LIGHT)
    draw.rectangle((28, 39, 31, 48), fill=BRASS_DARK)
    draw.rectangle((34, 39, 37, 48), fill=BRASS_DARK)
    draw.rectangle((30, 46, 35, 50), fill=BRASS_DARK)
    draw.rectangle((30, 40, 31, 45), fill=BRASS)
    draw.rectangle((34, 40, 35, 45), fill=BRASS)
    draw.rectangle((31, 47, 34, 48), fill=BRASS)

    return replace_outermost_pixel_with_black(image)


def draw_chest_32() -> Image.Image:
    image = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)

    silhouette = [
        (7, 7), (25, 7), (25, 8), (27, 9), (27, 27),
        (25, 27), (7, 27), (4, 27), (4, 14),
        (5, 14), (5, 10), (7, 8),
    ]
    draw.polygon(silhouette, fill=IRON_DARK)
    draw.polygon(
        [(7, 10), (9, 8), (23, 8), (25, 10), (26, 12),
         (24, 14), (8, 14), (6, 12)],
        fill=WOOD_MID,
    )
    draw.polygon(
        [(8, 10), (10, 9), (22, 9), (24, 10), (24, 11), (8, 11)],
        fill=WOOD_LIGHT,
    )
    draw.rectangle((10, 9, 21, 9), fill=WOOD_LIGHT)
    draw.rectangle((6, 14, 26, 18), fill=WOOD_DARK)
    draw.rectangle((7, 15, 25, 16), fill=WOOD_MID)
    draw.rectangle((5, 17, 27, 20), fill=IRON_DARK)
    draw.rectangle((7, 18, 25, 18), fill=IRON_LIGHT)
    draw.rectangle((7, 20, 25, 26), fill=WOOD_MID)
    draw.rectangle((7, 24, 25, 27), fill=WOOD_SHADOW)

    for left in (8, 21):
        draw.rectangle((left, 8, left + 3, 26), fill=IRON_DARK)
        draw.rectangle((left + 1, 9, left + 2, 24), fill=IRON_MID)
        draw.rectangle((left - 1, 7, left + 4, 9), fill=IRON_DARK)
        draw.rectangle((left + 1, 8, left + 2, 8), fill=IRON_LIGHT)
        draw.rectangle((left - 1, 24, left + 4, 27), fill=IRON_DARK)

    draw.rectangle((4, 14, 8, 27), fill=IRON_DARK)
    draw.rectangle((5, 16, 6, 24), fill=IRON_MID)
    draw.rectangle((4, 24, 9, 27), fill=IRON_DARK)
    draw.rectangle((24, 14, 27, 27), fill=IRON_DARK)
    draw.rectangle((25, 16, 26, 24), fill=IRON_MID)
    draw.rectangle((23, 24, 27, 27), fill=IRON_DARK)

    draw.rectangle((13, 16, 19, 20), fill=IRON_DARK)
    draw.rectangle((14, 17, 18, 19), fill=BRASS_DARK)
    draw.rectangle((15, 17, 17, 17), fill=BRASS)
    draw.rectangle((14, 19, 15, 24), fill=BRASS_DARK)
    draw.rectangle((17, 19, 18, 24), fill=BRASS_DARK)
    draw.rectangle((15, 23, 17, 25), fill=BRASS)

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
    ground = Image.open(ground_path).convert("RGBA") if ground_path.exists() else Image.new(
        "RGBA", (64, 64), (76, 82, 78, 255)
    )
    ground.alpha_composite(image)
    return ground.resize((256, 256), Image.Resampling.NEAREST)


def main() -> None:
    DEST.mkdir(parents=True, exist_ok=True)
    EVIDENCE.mkdir(parents=True, exist_ok=True)
    master = draw_chest_64()
    companion = draw_chest_32()
    master_path = DEST / "sephiria_loot_chest_imageguided_v2_64.png"
    companion_path = DEST / "sephiria_loot_chest_imageguided_v2_32.png"
    master.save(master_path)
    companion.save(companion_path)
    checker_contact(master, 4).save(EVIDENCE / "sephiria_loot_chest_imageguided_v2_64_contact.png")
    checker_contact(companion, 8).save(EVIDENCE / "sephiria_loot_chest_imageguided_v2_32_contact.png")
    gray = master.convert("LA").convert("RGBA")
    gray.putalpha(master.getchannel("A"))
    checker_contact(gray, 4).save(EVIDENCE / "sephiria_loot_chest_imageguided_v2_64_grayscale.png")
    ground_contact(master).save(EVIDENCE / "sephiria_loot_chest_imageguided_v2_64_ground_contact.png")
    print(master_path)


if __name__ == "__main__":
    main()
