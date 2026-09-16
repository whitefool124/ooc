#!/usr/bin/env python3
"""Redraw the image-guided chest with a rigid box silhouette and clear planes."""

from pathlib import Path

from PIL import Image, ImageDraw

from draw_imageguided_loot_chest_v2 import (
    BLACK,
    BRASS,
    BRASS_DARK,
    IRON_DARK,
    IRON_LIGHT,
    IRON_MID,
    WOOD_DARK,
    WOOD_LIGHT,
    WOOD_MID,
    WOOD_SHADOW,
    checker_contact,
    ground_contact,
    replace_outermost_pixel_with_black,
)


ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / "UnityProject/Assets/Game/Resources/Art/CombatTestArenaSephiriaDebug"
EVIDENCE = ROOT / "UnityProject/Reports/CombatTestArena/sephiria_debug_evidence"


def draw_chest_64() -> Image.Image:
    image = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)

    # Rigid rectangular silhouette: short stepped perspective corners only.
    draw.rectangle((9, 15, 55, 55), fill=IRON_DARK)

    # Plane 1: a light, flat top surface with a straight rear and front edge.
    draw.polygon(
        [(13, 19), (17, 17), (47, 17), (51, 19),
         (51, 24), (48, 27), (16, 27), (13, 24)],
        fill=WOOD_LIGHT,
    )
    draw.rectangle((18, 18, 46, 20), fill=WOOD_LIGHT)
    draw.rectangle((15, 24, 49, 26), fill=WOOD_MID)

    # Plane 2: the lid's vertical front is distinctly darker than its top.
    draw.rectangle((11, 27, 53, 35), fill=WOOD_DARK)
    draw.rectangle((14, 28, 50, 32), fill=WOOD_MID)
    draw.rectangle((14, 28, 50, 29), fill=WOOD_LIGHT)
    draw.rectangle((14, 33, 50, 35), fill=WOOD_SHADOW)

    # A single iron hinge/rim makes the change of plane unambiguous.
    draw.rectangle((10, 35, 54, 40), fill=IRON_DARK)
    draw.rectangle((13, 36, 51, 37), fill=IRON_LIGHT)
    draw.rectangle((13, 38, 51, 39), fill=IRON_MID)

    # Plane 3: lower box front, calmer and darkest at the base.
    draw.rectangle((11, 40, 53, 53), fill=WOOD_DARK)
    draw.rectangle((14, 41, 50, 49), fill=WOOD_MID)
    draw.rectangle((14, 41, 50, 43), fill=WOOD_LIGHT)
    draw.rectangle((14, 49, 50, 52), fill=WOOD_SHADOW)

    # Straight structural straps; none of them alters the rectangular contour.
    for left in (17, 42):
        draw.rectangle((left, 15, left + 5, 53), fill=IRON_DARK)
        draw.rectangle((left + 2, 17, left + 3, 50), fill=IRON_MID)
        draw.rectangle((left + 1, 16, left + 4, 18), fill=IRON_LIGHT)
        draw.rectangle((left - 1, 14, left + 6, 18), fill=IRON_DARK)
        draw.rectangle((left + 1, 15, left + 4, 16), fill=IRON_LIGHT)
        draw.rectangle((left - 1, 50, left + 6, 54), fill=IRON_DARK)

    # Narrow corner guards, deliberately secondary to the wood box.
    draw.rectangle((9, 22, 13, 52), fill=IRON_DARK)
    draw.rectangle((11, 25, 11, 48), fill=IRON_MID)
    draw.rectangle((51, 22, 55, 52), fill=IRON_DARK)
    draw.rectangle((53, 25, 53, 48), fill=IRON_MID)

    # One U-lock is the only loot-identifying accent.
    draw.rectangle((27, 34, 37, 41), fill=IRON_DARK)
    draw.rectangle((29, 35, 35, 39), fill=BRASS_DARK)
    draw.rectangle((30, 35, 34, 36), fill=BRASS)
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

    draw.rectangle((4, 7, 27, 27), fill=IRON_DARK)
    draw.polygon(
        [(6, 10), (8, 8), (24, 8), (26, 10),
         (26, 12), (24, 14), (8, 14), (6, 12)],
        fill=WOOD_LIGHT,
    )
    draw.rectangle((8, 12, 24, 13), fill=WOOD_MID)
    draw.rectangle((6, 14, 26, 17), fill=WOOD_DARK)
    draw.rectangle((7, 15, 25, 16), fill=WOOD_MID)
    draw.rectangle((5, 17, 27, 20), fill=IRON_DARK)
    draw.rectangle((7, 18, 25, 18), fill=IRON_LIGHT)
    draw.rectangle((6, 20, 26, 27), fill=WOOD_DARK)
    draw.rectangle((7, 21, 25, 24), fill=WOOD_MID)
    draw.rectangle((7, 25, 25, 26), fill=WOOD_SHADOW)

    for left in (8, 21):
        draw.rectangle((left, 8, left + 3, 26), fill=IRON_DARK)
        draw.rectangle((left + 1, 9, left + 2, 24), fill=IRON_MID)
        draw.rectangle((left - 1, 7, left + 4, 9), fill=IRON_DARK)
        draw.rectangle((left - 1, 24, left + 4, 27), fill=IRON_DARK)

    draw.rectangle((4, 11, 6, 26), fill=IRON_DARK)
    draw.rectangle((25, 11, 27, 26), fill=IRON_DARK)
    draw.rectangle((13, 17, 19, 21), fill=IRON_DARK)
    draw.rectangle((14, 18, 18, 20), fill=BRASS_DARK)
    draw.rectangle((15, 18, 17, 18), fill=BRASS)
    draw.rectangle((14, 20, 15, 24), fill=BRASS_DARK)
    draw.rectangle((17, 20, 18, 24), fill=BRASS_DARK)
    draw.rectangle((15, 23, 17, 25), fill=BRASS)

    return replace_outermost_pixel_with_black(image)


def main() -> None:
    DEST.mkdir(parents=True, exist_ok=True)
    EVIDENCE.mkdir(parents=True, exist_ok=True)
    master = draw_chest_64()
    companion = draw_chest_32()
    master_path = DEST / "sephiria_loot_chest_imageguided_v3_64.png"
    companion_path = DEST / "sephiria_loot_chest_imageguided_v3_32.png"
    master.save(master_path)
    companion.save(companion_path)
    checker_contact(master, 4).save(EVIDENCE / "sephiria_loot_chest_imageguided_v3_64_contact.png")
    checker_contact(companion, 8).save(EVIDENCE / "sephiria_loot_chest_imageguided_v3_32_contact.png")
    gray = master.convert("LA").convert("RGBA")
    gray.putalpha(master.getchannel("A"))
    checker_contact(gray, 4).save(EVIDENCE / "sephiria_loot_chest_imageguided_v3_64_grayscale.png")
    ground_contact(master).save(EVIDENCE / "sephiria_loot_chest_imageguided_v3_64_ground_contact.png")
    print(master_path)


if __name__ == "__main__":
    main()
