#!/usr/bin/env python3
"""Deterministically clean an Image-generated low-density sprite to native 32px.

This tool does not redraw subject anatomy. It removes a baked light checkerboard,
fits the authored silhouette to a role budget, hardens alpha, limits the palette,
replaces the existing outermost silhouette pixels with one black pixel, and emits
machine-readable QA plus enlarged contacts.
"""

from __future__ import annotations

import argparse
import json
from collections import deque
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]


def flattened(image: Image.Image):
    getter = getattr(image, "get_flattened_data", None)
    return getter() if getter else image.getdata()


def arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--role", choices=("unit_sideview", "prop_oblique"), required=True)
    parser.add_argument("--palette", type=int, default=14)
    parser.add_argument("--contact", type=Path)
    parser.add_argument("--application-contact", type=Path)
    parser.add_argument("--four-x", type=Path)
    parser.add_argument("--grayscale", type=Path)
    parser.add_argument("--qa", type=Path)
    return parser.parse_args()


def light_neutral(pixel: tuple[int, int, int, int]) -> bool:
    red, green, blue, alpha = pixel
    return alpha > 0 and min(red, green, blue) >= 185 and max(red, green, blue) - min(red, green, blue) <= 12


def remove_baked_checker(source: Image.Image) -> Image.Image:
    """Remove only light-neutral pixels connected to the canvas edge."""
    image = source.convert("RGBA")
    width, height = image.size
    pixels = image.load()
    background: set[tuple[int, int]] = set()
    queue: deque[tuple[int, int]] = deque()
    for x in range(width):
        queue.extend(((x, 0), (x, height - 1)))
    for y in range(height):
        queue.extend(((0, y), (width - 1, y)))
    while queue:
        x, y = queue.popleft()
        if (x, y) in background or not light_neutral(pixels[x, y]):
            continue
        background.add((x, y))
        if x:
            queue.append((x - 1, y))
        if x + 1 < width:
            queue.append((x + 1, y))
        if y:
            queue.append((x, y - 1))
        if y + 1 < height:
            queue.append((x, y + 1))
    for x, y in background:
        pixels[x, y] = (0, 0, 0, 0)
    return image


def harden_alpha(image: Image.Image, threshold: int = 112) -> Image.Image:
    result = image.copy()
    alpha = result.getchannel("A").point(lambda value: 255 if value >= threshold else 0)
    result.putalpha(alpha)
    return result


def quantize_visible(image: Image.Image, colors: int) -> Image.Image:
    visible = [(r, g, b) for r, g, b, a in flattened(image) if a]
    if not visible:
        raise ValueError("source has no visible subject after background removal")
    strip = Image.new("RGB", (len(visible), 1))
    strip.putdata(visible)
    palette = strip.quantize(colors=max(2, colors), method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert("RGB")
    result = Image.new("RGBA", image.size, (0, 0, 0, 0))
    output = result.load()
    index = 0
    for y in range(image.height):
        for x in range(image.width):
            if image.getpixel((x, y))[3]:
                output[x, y] = (*palette.getpixel((index, 0)), 255)
                index += 1
    return result


def black_outermost_pixel(image: Image.Image) -> Image.Image:
    alpha = image.getchannel("A")
    result = image.copy()
    for y in range(image.height):
        for x in range(image.width):
            if not alpha.getpixel((x, y)):
                continue
            if any(
                nx < 0 or ny < 0 or nx >= image.width or ny >= image.height or not alpha.getpixel((nx, ny))
                for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1))
            ):
                result.putpixel((x, y), (0, 0, 0, 255))
    return result


def normalize(source: Image.Image, role: str, palette: int) -> Image.Image:
    cleaned = remove_baked_checker(source)
    bounds = cleaned.getchannel("A").getbbox()
    if bounds is None:
        raise ValueError("no subject bounds found")
    crop = cleaned.crop(bounds)
    if role == "unit_sideview":
        limit_w, limit_h, bottom = 20, 29, 30
    else:
        limit_w, limit_h, bottom = 28, 27, 30
    scale = min(limit_w / crop.width, limit_h / crop.height)
    size = (max(1, round(crop.width * scale)), max(1, round(crop.height * scale)))
    # Image generation already authored large, hard-edged logical pixel blocks.
    # Sampling their centers preserves those decisions; area averaging invents
    # muddy intermediate colors and visible blur at native size.
    reduced = crop.resize(size, Image.Resampling.NEAREST)
    reduced = harden_alpha(reduced)
    reduced = quantize_visible(reduced, palette)
    canvas = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    x = (32 - reduced.width) // 2
    y = bottom - reduced.height
    canvas.alpha_composite(reduced, (x, y))
    return black_outermost_pixel(canvas)


def component_count(image: Image.Image) -> int:
    alpha = image.getchannel("A")
    remaining = {(x, y) for y in range(32) for x in range(32) if alpha.getpixel((x, y))}
    count = 0
    while remaining:
        count += 1
        queue = [remaining.pop()]
        while queue:
            x, y = queue.pop()
            for neighbor in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                if neighbor in remaining:
                    remaining.remove(neighbor)
                    queue.append(neighbor)
    return count


def qa_report(image: Image.Image, role: str) -> dict[str, object]:
    alpha_values = sorted(set(flattened(image.getchannel("A"))))
    bounds = image.getchannel("A").getbbox()
    colors = {(r, g, b) for r, g, b, a in flattened(image) if a}
    width = 0 if bounds is None else bounds[2] - bounds[0]
    height = 0 if bounds is None else bounds[3] - bounds[1]
    components = component_count(image)
    checks = {
        "exact_32x32": image.size == (32, 32),
        "binary_alpha": alpha_values in ([0, 255], [255]),
        "palette_at_most_15_including_black": len(colors) <= 15,
        "single_connected_silhouette": components == 1,
        "safe_canvas_border": bool(bounds) and bounds[0] > 0 and bounds[1] > 0 and bounds[2] < 32 and bounds[3] < 32,
        "role_occupancy": height >= (24 if role == "unit_sideview" else 18) and width <= (20 if role == "unit_sideview" else 28),
    }
    return {
        "status": "PASS" if all(checks.values()) else "REJECT",
        "role": role,
        "bounds": list(bounds) if bounds else None,
        "visible_width": width,
        "visible_height": height,
        "palette_colors": len(colors),
        "components": components,
        "checks": checks,
        "note": "Pose/view correctness still requires image-model review; pixel mechanics are deterministic.",
    }


def checker_contact(image: Image.Image) -> Image.Image:
    preview = image.resize((256, 256), Image.Resampling.NEAREST)
    board = Image.new("RGBA", preview.size, (205, 205, 205, 255))
    for y in range(0, 256, 16):
        for x in range(0, 256, 16):
            if (x // 16 + y // 16) % 2:
                Image.Image.paste(board, (238, 238, 238, 255), (x, y, x + 16, y + 16))
    board.alpha_composite(preview)
    return board


def application_contact(image: Image.Image, role: str) -> Image.Image:
    ground_path = ROOT / "UnityProject/Assets/Game/Resources/Art/CombatTestArenaGround/academy_test_ground_theme_slate_surface_32.png"
    chest_path = ROOT / "UnityProject/Assets/Game/Resources/Art/CombatTestArenaSephiriaDebug/sephiria_loot_chest_image_native32_v7.png"
    ground = Image.open(ground_path).convert("RGBA") if ground_path.is_file() else Image.new("RGBA", (32, 32), (70, 75, 73, 255))
    canvas = Image.new("RGBA", (96, 64), (0, 0, 0, 255))
    for y in range(0, 64, 32):
        for x in range(0, 96, 32):
            canvas.alpha_composite(ground, (x, y))
    if role == "unit_sideview" and chest_path.is_file():
        canvas.alpha_composite(Image.open(chest_path).convert("RGBA"), (8, 30))
        canvas.alpha_composite(image, (48, 30))
    else:
        canvas.alpha_composite(image, (32, 30))
    return canvas.resize((384, 256), Image.Resampling.NEAREST)


def main() -> None:
    args = arguments()
    source = Image.open(args.source).convert("RGBA")
    output = normalize(source, args.role, args.palette)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    output.save(args.output)
    report = qa_report(output, args.role)
    qa_path = args.qa or args.output.with_suffix(".qa.json")
    qa_path.parent.mkdir(parents=True, exist_ok=True)
    qa_path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    contact_path = args.contact or args.output.with_name(args.output.stem + "_contact.png")
    contact_path.parent.mkdir(parents=True, exist_ok=True)
    checker_contact(output).save(contact_path)
    application_path = args.application_contact or args.output.with_name(args.output.stem + "_application.png")
    application_path.parent.mkdir(parents=True, exist_ok=True)
    application_contact(output, args.role).save(application_path)
    four_x_path = args.four_x or args.output.with_name(args.output.stem + "_4x.png")
    output.resize((128, 128), Image.Resampling.NEAREST).save(four_x_path)
    grayscale_path = args.grayscale or args.output.with_name(args.output.stem + "_grayscale.png")
    gray = output.convert("LA").convert("RGBA")
    gray.putalpha(output.getchannel("A"))
    gray.save(grayscale_path)
    print(json.dumps({
        "output": str(args.output),
        "four_x": str(four_x_path),
        "grayscale": str(grayscale_path),
        "contact": str(contact_path),
        "application_contact": str(application_path),
        "qa": str(qa_path),
        **report,
    }, ensure_ascii=False))


if __name__ == "__main__":
    main()
