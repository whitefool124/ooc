#!/usr/bin/env python3
"""Normalize the regenerated pixel-density repair sources to OCC ground tiers."""

from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Worldbuilding/归档/2026-09-13_像素密度统一修复/source"
DEST = ROOT / "UnityProject/Assets/Game/Resources/Art/CombatTestArenaGround"
EVIDENCE = ROOT / "Worldbuilding/归档/2026-09-13_像素密度统一修复/evidence"


def checker(size: tuple[int, int], block: int = 8) -> Image.Image:
    image = Image.new("RGBA", size, (38, 38, 38, 255))
    pixels = image.load()
    for y in range(size[1]):
        for x in range(size[0]):
            if ((x // block) + (y // block)) % 2:
                pixels[x, y] = (70, 70, 70, 255)
    return image


def save_evidence(image: Image.Image, stem: str, scale: int) -> None:
    image.save(EVIDENCE / f"{stem}_1x.png")
    image.resize((image.width * scale, image.height * scale), Image.Resampling.NEAREST).save(EVIDENCE / f"{stem}_4x.png")
    gray = image.convert("LA").convert("RGBA")
    gray.putalpha(image.getchannel("A"))
    gray.resize((image.width * scale, image.height * scale), Image.Resampling.NEAREST).save(EVIDENCE / f"{stem}_grayscale.png")
    board = checker((image.width * scale, image.height * scale), max(2, scale * 2))
    board.alpha_composite(image.resize(board.size, Image.Resampling.NEAREST))
    board.save(EVIDENCE / f"{stem}_checker.png")


def quantized(image: Image.Image, colors: int) -> Image.Image:
    alpha = image.getchannel("A").point(lambda value: 255 if value >= 128 else 0)
    # Quantize RGB separately; Pillow's RGBA quantizer can move the visible
    # silhouette when transparent halo pixels are present.
    reduced = image.convert("RGB").quantize(colors=colors, method=Image.Quantize.FASTOCTREE).convert("RGB")
    result = reduced.convert("RGBA")
    result.putalpha(alpha)
    return result


def fit_transparent(source: Image.Image, size: int, body: int | tuple[int, int], contact_y: int, colors: int) -> Image.Image:
    # Ignore the model's faint glow/halo when registering the readable body.
    # Otherwise a wide transparent aura makes a wall collapse to a shallow
    # 17px silhouette after fitting.
    alpha = source.getchannel("A").point(lambda value: 255 if value >= 128 else 0)
    bbox = alpha.getbbox()
    if bbox is None:
        raise ValueError("transparent source has no visible subject")
    crop = source.crop(bbox)
    if isinstance(body, tuple):
        # Structural battlefield props use a deliberate readable rectangle:
        # the source model may produce a wide, shallow wall, but the tactical
        # contract needs a stable 44x32 silhouette beside 46px units.
        target = body
    else:
        scale = min(body / crop.width, body / crop.height)
        target = (max(1, round(crop.width * scale)), max(1, round(crop.height * scale)))
    # Quantize the opaque RGB before resizing; carry the alpha mask separately
    # so transparent halos cannot distort the final contact position.
    rgb = crop.convert("RGB").quantize(colors=colors, method=Image.Quantize.FASTOCTREE).convert("RGB")
    fitted = rgb.resize(target, Image.Resampling.NEAREST).convert("RGBA")
    fitted.putalpha(alpha.crop(bbox).resize(target, Image.Resampling.NEAREST))
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    fitted_bbox = fitted.getchannel("A").getbbox()
    if fitted_bbox is None:
        raise ValueError("scaled transparent source has no visible subject")
    x = (size - (fitted_bbox[2] - fitted_bbox[0])) // 2 - fitted_bbox[0]
    y = contact_y + 1 - fitted_bbox[3]
    canvas.alpha_composite(fitted, (x, y))
    return quantized(canvas, colors)


def main() -> None:
    EVIDENCE.mkdir(parents=True, exist_ok=True)
    for theme in ("slate", "earth"):
        source = Image.open(SOURCE / f"{theme}_surface_v4_1024_source.png").convert("RGBA")
        # Lock the shared ground grammar to a coarse 16px semantic grid before
        # delivering 64px. This prevents pseudo-pixel 1024px sources from
        # becoming finer than the 64px unit sprites after nearest scaling.
        coarse = source.resize((16, 16), Image.Resampling.NEAREST)
        master = quantized(coarse.resize((64, 64), Image.Resampling.NEAREST), 12)
        companion = quantized(coarse.resize((32, 32), Image.Resampling.NEAREST), 10)
        master.save(DEST / f"academy_test_ground_theme_{theme}_surface_64.png")
        companion.save(DEST / f"academy_test_ground_theme_{theme}_surface_32.png")
        # Directional south edges keep their independently generated facade,
        # while the logical top cell must be byte-identical to the surface.
        edge64_path = DEST / f"academy_test_ground_theme_{theme}_edge_s_64.png"
        edge32_path = DEST / f"academy_test_ground_theme_{theme}_edge_s_32.png"
        edge64 = Image.open(edge64_path).convert("RGBA")
        edge32 = Image.open(edge32_path).convert("RGBA")
        edge64.paste(master, (0, 0))
        edge32.paste(companion, (0, 0))
        edge64.save(edge64_path)
        edge32.save(edge32_path)
        save_evidence(master, f"{theme}_surface", 4)
        companion.resize((128, 128), Image.Resampling.NEAREST).save(EVIDENCE / f"{theme}_surface_32_4x.png")

    # The model source has a broad transparent halo; fit against the visible
    # silhouette so the in-game body reaches the intended ~44px / ~22px read.
    heavy_master = fit_transparent(
        Image.open(SOURCE / "heavy_cover_v7_64_source.png").convert("RGBA"), 64, (44, 32), 55, 12)
    heavy_companion = fit_transparent(
        Image.open(SOURCE / "heavy_cover_v7_32_source.png").convert("RGBA"), 32, (22, 16), 27, 10)
    heavy_master.save(DEST / "academy_test_heavy_cover_intact_64.png")
    heavy_companion.save(DEST / "academy_test_heavy_cover_intact_32.png")
    save_evidence(heavy_master, "heavy_cover", 4)
    heavy_companion.resize((128, 128), Image.Resampling.NEAREST).save(EVIDENCE / "heavy_cover_32_4x.png")

    crate_master = fit_transparent(
        Image.open(SOURCE / "book_crate_v2_64_source.png").convert("RGBA"), 64, 36, 55, 12)
    crate_companion = fit_transparent(
        Image.open(SOURCE / "book_crate_v2_32_source.png").convert("RGBA"), 32, 20, 27, 10)
    crate_master.save(DEST / "academy_test_book_crate_intact_64.png")
    crate_companion.save(DEST / "academy_test_book_crate_intact_32.png")
    save_evidence(crate_master, "book_crate", 4)
    crate_companion.resize((128, 128), Image.Resampling.NEAREST).save(EVIDENCE / "book_crate_32_4x.png")
    print("normalized quiet slate/earth surfaces and square heavy-cover companions")


if __name__ == "__main__":
    main()
