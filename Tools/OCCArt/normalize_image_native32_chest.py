#!/usr/bin/env python3
"""Normalize the image-designed low-density chest directly to a 32px asset.

This pass preserves the generated 24:20 subject composition. It removes the
flat key background, samples the complete design once onto the target grid,
quantizes visible colours together, hardens alpha and replaces only the
outermost opaque texels with black. It does not locally redraw the chest.
"""

from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Worldbuilding/归档/2026-09-14_赛菲莉娅主参考确认/source/sephiria_loot_chest_native32_reference_v7.png"
DEST = ROOT / "UnityProject/Assets/Game/Resources/Art/CombatTestArenaSephiriaDebug"
EVIDENCE = ROOT / "UnityProject/Reports/CombatTestArena/sephiria_debug_evidence"
OUTPUT = DEST / "sephiria_loot_chest_image_native32_v7.png"
CONTACT = EVIDENCE / "sephiria_loot_chest_image_native32_v7_contact.png"
GRAYSCALE = EVIDENCE / "sephiria_loot_chest_image_native32_v7_grayscale.png"
GROUND_CONTACT = EVIDENCE / "sephiria_loot_chest_image_native32_v7_ground_contact.png"


def is_key_colour(pixel: tuple[int, int, int]) -> bool:
    red, green, blue = pixel
    return red > 180 and blue > 160 and green < 120 and min(red, blue) - green > 70


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
        raise RuntimeError("No non-key subject pixels found")
    subject = source.crop(bounds).resize((28, 23), Image.Resampling.BOX)
    subject_mask = mask.crop(bounds).resize((28, 23), Image.Resampling.BOX)
    subject_mask = subject_mask.point(lambda value: 255 if value >= 160 else 0)

    # Quantize only visible samples, keeping the transparent key out of palette fitting.
    visible_positions = [
        (x, y)
        for y in range(23)
        for x in range(28)
        if subject_mask.getpixel((x, y))
    ]
    accent_positions = []
    base_positions = []
    for position in visible_positions:
        red, green, blue = subject.getpixel(position)
        if red > 100 and green / red > 0.78 and blue / red < 0.65:
            accent_positions.append(position)
        else:
            base_positions.append(position)

    strip = Image.new("RGB", (len(base_positions), 1))
    for index, position in enumerate(base_positions):
        strip.putpixel((index, 0), subject.getpixel(position))
    quantized = strip.quantize(colors=7, method=Image.Quantize.MEDIANCUT).convert("RGB")

    result = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    for index, (x, y) in enumerate(base_positions):
        colour = quantized.getpixel((index, 0))
        result.putpixel((x + 2, y + 4), (*colour, 255))
    for x, y in accent_positions:
        red, green, blue = subject.getpixel((x, y))
        colour = (216, 174, 70) if red + green + blue >= 430 else (145, 105, 35)
        result.putpixel((x + 2, y + 4), (*colour, 255))
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
    ground_path = ROOT / "UnityProject/Assets/Game/Resources/Art/CombatTestArenaGround/academy_test_ground_theme_slate_surface_32.png"
    ground = Image.open(ground_path).convert("RGBA") if ground_path.exists() else Image.new(
        "RGBA", (32, 32), (76, 82, 78, 255)
    )
    ground.alpha_composite(image)
    return ground.resize((256, 256), Image.Resampling.NEAREST)


def main() -> None:
    DEST.mkdir(parents=True, exist_ok=True)
    EVIDENCE.mkdir(parents=True, exist_ok=True)
    result = normalize()
    result.save(OUTPUT)
    checker_contact(result).save(CONTACT)
    gray = result.convert("LA").convert("RGBA")
    gray.putalpha(result.getchannel("A"))
    checker_contact(gray).save(GRAYSCALE)
    ground_contact(result).save(GROUND_CONTACT)
    print(OUTPUT)


if __name__ == "__main__":
    main()
