#!/usr/bin/env python3
"""Normalize independent M-A32 trial sources and build review evidence.

This script only performs mechanical background cleanup, resizing, palette
limiting, hard-alpha conversion and review-sheet composition. It does not draw
or repair asset content.
"""

from __future__ import annotations

import hashlib
import json
from collections import deque
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
BATCH = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A32"
CATALOG = BATCH / "m_a32_trial_catalog.json"
CONTRACT = ROOT / "Tools/OCCArt/occ_art_contract_v1.json"
MANIFESTS = BATCH / "manifests"
NORMALIZED = BATCH / "normalized"
QA = BATCH / "QA"


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def remove_connected_light_checker(image: Image.Image) -> Image.Image:
    """Remove a light neutral checkerboard connected to the image boundary."""
    rgb = image.convert("RGB")
    width, height = rgb.size
    pixels = rgb.load()
    visited = bytearray(width * height)
    queue: deque[tuple[int, int]] = deque()

    def candidate(x: int, y: int) -> bool:
        r, g, b = pixels[x, y]
        return min(r, g, b) >= 205 and max(r, g, b) - min(r, g, b) <= 14

    for x in range(width):
        if candidate(x, 0):
            queue.append((x, 0))
        if candidate(x, height - 1):
            queue.append((x, height - 1))
    for y in range(height):
        if candidate(0, y):
            queue.append((0, y))
        if candidate(width - 1, y):
            queue.append((width - 1, y))

    while queue:
        x, y = queue.popleft()
        index = y * width + x
        if visited[index] or not candidate(x, y):
            continue
        visited[index] = 1
        if x:
            queue.append((x - 1, y))
        if x + 1 < width:
            queue.append((x + 1, y))
        if y:
            queue.append((x, y - 1))
        if y + 1 < height:
            queue.append((x, y + 1))

    rgba = rgb.convert("RGBA")
    alpha = Image.new("L", (width, height), 255)
    alpha_pixels = alpha.load()
    for y in range(height):
        for x in range(width):
            if visited[y * width + x]:
                alpha_pixels[x, y] = 0
    rgba.putalpha(alpha)
    return rgba


def hard_alpha(image: Image.Image, threshold: int = 128) -> Image.Image:
    rgba = image.convert("RGBA")
    alpha = rgba.getchannel("A").point(lambda value: 255 if value >= threshold else 0)
    rgba.putalpha(alpha)
    return rgba


def transparent_source(path: Path) -> Image.Image:
    source = Image.open(path)
    alpha = source.convert("RGBA").getchannel("A")
    if alpha.getextrema() == (255, 255):
        source = remove_connected_light_checker(source)
    source = hard_alpha(source)
    if source.getchannel("A").getbbox() is None:
        raise ValueError(f"no visible source pixels: {path}")
    return source


def quantize_visible(image: Image.Image, palette_max: int) -> Image.Image:
    rgba = hard_alpha(image)
    alpha = rgba.getchannel("A")
    rgb = rgba.convert("RGB").quantize(
        colors=palette_max,
        method=Image.Quantize.MEDIANCUT,
        dither=Image.Dither.NONE,
    ).convert("RGB")
    output = rgb.convert("RGBA")
    output.putalpha(alpha)
    return output


def fit_transparent(
    source: Image.Image,
    size: tuple[int, int],
    palette_max: int,
    border: int,
    occupancy: float = 1.0,
    bottom_y: int | None = None,
) -> Image.Image:
    alpha = source.getchannel("A")
    bounds = alpha.getbbox()
    if bounds is None:
        raise ValueError("empty transparent source")
    crop = source.crop(bounds)
    available_w = max(1, round((size[0] - border * 2) * occupancy))
    available_h = max(1, round((size[1] - border * 2) * occupancy))
    scale = min(available_w / crop.width, available_h / crop.height)
    fitted_size = (max(1, round(crop.width * scale)), max(1, round(crop.height * scale)))
    fitted = crop.resize(fitted_size, Image.Resampling.LANCZOS)
    fitted = quantize_visible(fitted, palette_max)
    canvas = Image.new("RGBA", size, (0, 0, 0, 0))
    x = (size[0] - fitted.width) // 2
    y = (size[1] - fitted.height) // 2 if bottom_y is None else bottom_y - fitted.height + 1
    x = max(border, min(x, size[0] - border - fitted.width))
    y = max(border, min(y, size[1] - border - fitted.height))
    canvas.alpha_composite(fitted, (x, y))
    return canvas


def crop_ratio(image: Image.Image, ratio: float) -> Image.Image:
    width, height = image.size
    current = width / height
    if current > ratio:
        new_width = round(height * ratio)
        left = (width - new_width) // 2
        return image.crop((left, 0, left + new_width, height))
    new_height = round(width / ratio)
    top = (height - new_height) // 2
    return image.crop((0, top, width, top + new_height))


def normalize_asset(asset: dict, role: dict) -> tuple[Image.Image, Image.Image | None]:
    source_path = BATCH / "raw" / asset["stem"] / "source.png"
    role_name = asset["role"]
    palette_max = int(asset["palette_max"])

    if role_name == "floor_tile_32":
        source = crop_ratio(Image.open(source_path).convert("RGB"), 1.0)
        output = source.resize((32, 32), Image.Resampling.BOX)
        output = output.quantize(colors=palette_max, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert("RGBA")
        output.putalpha(255)
        return output, None

    if role_name == "ui_backdrop_480x270":
        source = crop_ratio(Image.open(source_path).convert("RGB"), 16 / 9)
        output = source.resize((480, 270), Image.Resampling.BOX)
        output = output.quantize(colors=palette_max, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert("RGBA")
        output.putalpha(255)
        return output, None

    source = transparent_source(source_path)
    border = int(role.get("transparent_border_min", 0))

    if role_name == "material_pickup_24_to_ui32":
        native = fit_transparent(source, (24, 24), palette_max, 1)
        output = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
        output.alpha_composite(native, (4, 4))
        return output, native

    if role_name == "tactical_unit_64":
        output = fit_transparent(source, (64, 64), palette_max, 1, occupancy=1.0, bottom_y=58)
        return output, None

    if role_name == "character_portrait_b_384x576":
        return fit_transparent(source, (384, 576), palette_max, border, occupancy=0.84), None

    if role_name == "character_performance_c_192x288":
        return fit_transparent(source, (192, 288), palette_max, border, occupancy=0.88), None

    logical_cells = asset.get("logical_cells")
    if "delivery_size" in role:
        size = tuple(int(value) for value in role["delivery_size"])
    elif logical_cells:
        size = (int(logical_cells[0]) * 32, int(logical_cells[1]) * 32)
    else:
        raise ValueError(f"no output size: {asset['stem']}")
    return fit_transparent(source, size, palette_max, border), None


def checker(size: tuple[int, int], block: int) -> Image.Image:
    image = Image.new("RGBA", size)
    draw = ImageDraw.Draw(image)
    for y in range(0, size[1], block):
        for x in range(0, size[0], block):
            value = 78 if ((x // block) + (y // block)) % 2 else 126
            draw.rectangle((x, y, min(size[0] - 1, x + block - 1), min(size[1] - 1, y + block - 1)), fill=(value, value, value, 255))
    return image


def write_evidence(image: Image.Image, folder: Path) -> None:
    folder.mkdir(parents=True, exist_ok=True)
    image.save(folder / "1x.png")
    image.resize((image.width * 4, image.height * 4), Image.Resampling.NEAREST).save(folder / "4x.png")
    grayscale = image.convert("LA").convert("RGBA")
    grayscale.putalpha(image.getchannel("A"))
    grayscale.save(folder / "grayscale.png")
    scale = 4 if max(image.size) <= 128 else 1
    preview = image.resize((image.width * scale, image.height * scale), Image.Resampling.NEAREST)
    board = checker(preview.size, max(4, 8 * scale))
    board.alpha_composite(preview)
    board.save(folder / "checker.png")


def build_contact(assets: list[dict]) -> Path:
    width, height = 1600, 960
    cols, rows = 4, 3
    cell_w, cell_h = width // cols, height // rows
    canvas = Image.new("RGB", (width, height), "#282721")
    draw = ImageDraw.Draw(canvas)
    font = ImageFont.load_default()
    for index, asset in enumerate(assets):
        col, row = index % cols, index // cols
        x0, y0 = col * cell_w, row * cell_h
        draw.rectangle((x0 + 8, y0 + 8, x0 + cell_w - 8, y0 + cell_h - 8), fill="#e8dfcf", outline="#5f5a51", width=3)
        image = Image.open(NORMALIZED / f"{asset['stem']}.png").convert("RGBA")
        limit_w, limit_h = cell_w - 48, cell_h - 70
        scale = min(limit_w / image.width, limit_h / image.height)
        if max(image.size) <= 128:
            scale = max(1, int(scale))
        target = (max(1, round(image.width * scale)), max(1, round(image.height * scale)))
        preview = image.resize(target, Image.Resampling.NEAREST)
        board = checker(target, 12)
        board.alpha_composite(preview)
        px = x0 + (cell_w - target[0]) // 2
        py = y0 + 34 + (limit_h - target[1]) // 2
        canvas.paste(board.convert("RGB"), (px, py))
        draw.text((x0 + 18, y0 + 15), asset["asset_id"], fill="#2a2823", font=font)
        draw.text((x0 + 18, y0 + cell_h - 28), f"{asset['role']} / QA_PENDING", fill="#5f5a51", font=font)
    output = QA / "contacts" / "m_a32_trial_contact.png"
    output.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(output)
    return output


def main() -> None:
    assets = json.loads(CATALOG.read_text(encoding="utf-8"))["assets"]
    roles = json.loads(CONTRACT.read_text(encoding="utf-8"))["roles"]
    NORMALIZED.mkdir(parents=True, exist_ok=True)
    for asset in assets:
        role = roles[asset["role"]]
        output, native = normalize_asset(asset, role)
        output_path = NORMALIZED / f"{asset['stem']}.png"
        output.save(output_path)
        native_path = None
        if native is not None:
            native_path = NORMALIZED / f"{asset['stem']}_native24.png"
            native.save(native_path)
        write_evidence(output, QA / asset["stem"])

        manifest_path = MANIFESTS / f"{asset['stem']}.occ-art-manifest-v1.json"
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        manifest["provenance"]["source_sha256"] = digest(BATCH / "raw" / asset["stem"] / "source.png")
        manifest["delivery"]["output_sha256"] = digest(output_path)
        if native_path is not None:
            manifest["delivery"]["native_output_path"] = native_path.relative_to(ROOT).as_posix()
        manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    contact = build_contact(assets)
    print(json.dumps({"normalized": len(assets), "contact": contact.relative_to(ROOT).as_posix()}, ensure_ascii=False))


if __name__ == "__main__":
    main()
