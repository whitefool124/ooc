#!/usr/bin/env python3
"""Normalize a low-density Image reference into a 32px side-view unit test."""

from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Worldbuilding/归档/2026-09-14_赛菲莉娅主参考确认/source/sephiria_maintenance_unit_native32_reference_v2.png"
DEST = ROOT / "UnityProject/Reports/CombatTestArena/native32_character_test"
OUTPUT = DEST / "sephiria_maintenance_unit_image_native32_v1.png"
CONTACT = DEST / "sephiria_maintenance_unit_image_native32_v1_contact.png"
GRAYSCALE = DEST / "sephiria_maintenance_unit_image_native32_v1_grayscale.png"
GROUND_CONTACT = DEST / "sephiria_maintenance_unit_image_native32_v1_ground_contact.png"
PAIR_CONTACT = DEST / "sephiria_maintenance_unit_with_chest_native32_contact.png"


def is_key_colour(pixel: tuple[int, int, int]) -> bool:
    red, green, blue = pixel
    return red > 180 and blue > 160 and green < 120 and min(red, blue) - green > 70


def replace_outermost_pixel_with_black(image: Image.Image) -> Image.Image:
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
                pixels[x, y] = (0, 0, 0, 255)
    return result


def normalize() -> Image.Image:
    source = Image.open(SOURCE).convert("RGB")
    mask = Image.new("L", source.size, 0)
    mask_pixels = mask.load()
    source_pixels = source.load()
    for y in range(source.height):
        for x in range(source.width):
            mask_pixels[x, y] = 0 if is_key_colour(source_pixels[x, y]) else 255

    bounds = mask.getbbox()
    if bounds is None:
        raise RuntimeError("No non-key character pixels found")
    crop = source.crop(bounds)
    crop_mask = mask.crop(bounds)
    target_height = 28
    target_width = max(1, round(crop.width * target_height / crop.height))
    if target_width > 14:
        target_width = 14
        target_height = max(1, round(crop.height * target_width / crop.width))
    subject = crop.resize((target_width, target_height), Image.Resampling.BOX)
    subject_mask = crop_mask.resize((target_width, target_height), Image.Resampling.BOX)
    subject_mask = subject_mask.point(lambda value: 255 if value >= 160 else 0)

    visible = [
        (x, y)
        for y in range(target_height)
        for x in range(target_width)
        if subject_mask.getpixel((x, y))
    ]
    cyan = []
    base = []
    for position in visible:
        red, green, blue = subject.getpixel(position)
        if blue > 105 and green > 95 and blue > red * 1.18:
            cyan.append(position)
        else:
            base.append(position)

    strip = Image.new("RGB", (len(base), 1))
    for index, position in enumerate(base):
        strip.putpixel((index, 0), subject.getpixel(position))
    quantized = strip.quantize(colors=12, method=Image.Quantize.MEDIANCUT).convert("RGB")

    result = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    offset_x = (32 - target_width) // 2
    offset_y = 30 - target_height
    for index, (x, y) in enumerate(base):
        colour = quantized.getpixel((index, 0))
        result.putpixel((x + offset_x, y + offset_y), (*colour, 255))
    for x, y in cyan:
        red, green, blue = subject.getpixel((x, y))
        colour = (107, 225, 229) if red + green + blue > 470 else (33, 155, 173)
        result.putpixel((x + offset_x, y + offset_y), (*colour, 255))
    return replace_outermost_pixel_with_black(result)


def checker_contact(image: Image.Image) -> Image.Image:
    zoom = image.resize((256, 256), Image.Resampling.NEAREST)
    board = Image.new("RGBA", zoom.size, (184, 184, 184, 255))
    pixels = board.load()
    for y in range(board.height):
        for x in range(board.width):
            if ((x // 16) + (y // 16)) % 2:
                pixels[x, y] = (220, 220, 220, 255)
    board.alpha_composite(zoom)
    return board


def ground_contact(image: Image.Image) -> Image.Image:
    path = ROOT / "UnityProject/Assets/Game/Resources/Art/CombatTestArenaGround/academy_test_ground_theme_slate_surface_32.png"
    ground = Image.open(path).convert("RGBA") if path.exists() else Image.new(
        "RGBA", (32, 32), (76, 82, 78, 255)
    )
    ground.alpha_composite(image)
    return ground.resize((256, 256), Image.Resampling.NEAREST)


def pair_contact(character: Image.Image) -> Image.Image:
    ground_path = ROOT / "UnityProject/Assets/Game/Resources/Art/CombatTestArenaGround/academy_test_ground_theme_slate_surface_32.png"
    ground = Image.open(ground_path).convert("RGBA") if ground_path.exists() else Image.new(
        "RGBA", (32, 32), (76, 82, 78, 255)
    )
    chest_path = ROOT / "UnityProject/Assets/Game/Resources/Art/CombatTestArenaSephiriaDebug/sephiria_loot_chest_image_native32_v7.png"
    chest = Image.open(chest_path).convert("RGBA")
    board = Image.new("RGBA", (64, 32), (0, 0, 0, 255))
    board.alpha_composite(ground, (0, 0))
    board.alpha_composite(ground, (32, 0))
    board.alpha_composite(chest, (0, 0))
    board.alpha_composite(character, (32, 0))
    return board.resize((512, 256), Image.Resampling.NEAREST)


def main() -> None:
    DEST.mkdir(parents=True, exist_ok=True)
    result = normalize()
    result.save(OUTPUT)
    checker_contact(result).save(CONTACT)
    gray = result.convert("LA").convert("RGBA")
    gray.putalpha(result.getchannel("A"))
    checker_contact(gray).save(GRAYSCALE)
    ground_contact(result).save(GROUND_CONTACT)
    pair_contact(result).save(PAIR_CONTACT)
    print(OUTPUT)


if __name__ == "__main__":
    main()
