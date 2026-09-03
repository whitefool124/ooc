"""Turn the approved material-rich direction sheet into a reviewable 16px study.

This is intentionally a *directional reduction*, not an importable final asset.
It lets art review judge whether material clusters survive before redrawing each
selected object pixel-by-pixel for production.
"""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


SOURCE = Path(r"C:\Users\FNHF\.codex\generated_images\019ff714-5e0a-7ed0-8cc2-b42cf7aa433c\exec-07ad0aaf-33a4-430b-b974-97f4f99cc8b7.png")
ROOT = Path(__file__).parent
OUT = ROOT / "Items16"
QA = ROOT / "QA"
OUT.mkdir(exist_ok=True)
QA.mkdir(exist_ok=True)

# Tight source regions around the five isolated objects (on the 2048x1152 sheet).
REGIONS = {
    "medic_herb": (55, 270, 470, 845),
    "cinder_pear": (445, 240, 745, 850),
    "coin": (810, 300, 1110, 825),
    "wood_cup": (1170, 245, 1570, 850),
    "bread": (1605, 330, 1995, 800),
}
LABELS = {
    "medic_herb": "Medic herb",
    "cinder_pear": "Cinder pear",
    "coin": "Coin",
    "wood_cup": "Wood cup",
    "bread": "Bread",
}


def remove_white(image: Image.Image) -> Image.Image:
    rgba = image.convert("RGBA")
    pixels = rgba.load()
    for y in range(rgba.height):
        for x in range(rgba.width):
            r, g, b, _ = pixels[x, y]
            # The concept sheet's background is pure/near white; retain bright gold
            # and bread highlights that are not sufficiently neutral to meet this test.
            if min(r, g, b) > 245 and max(r, g, b) - min(r, g, b) < 8:
                pixels[x, y] = (0, 0, 0, 0)
    return rgba


def make_icon(source: Image.Image, region: tuple[int, int, int, int]) -> Image.Image:
    image = remove_white(source.crop(region))
    box = image.getbbox()
    if not box:
        raise RuntimeError("empty crop")
    image = image.crop(box)
    # Keep a 1px transparent safety border. BOX consolidates whole source clusters,
    # then the strict 6-color quantization restores flat, intentional pixel groups.
    max_size = (14, 14)
    image.thumbnail(max_size, Image.Resampling.BOX)
    rgb = image.convert("RGB").quantize(colors=6, method=Image.Quantize.MEDIANCUT).convert("RGBA")
    alpha = image.getchannel("A")
    rgba = Image.new("RGBA", image.size)
    rgba.paste(rgb, mask=alpha)
    canvas = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    canvas.alpha_composite(rgba, ((16 - rgba.width) // 2, (16 - rgba.height) // 2))
    return canvas


def main() -> None:
    source = Image.open(SOURCE)
    icons: list[tuple[str, Image.Image]] = []
    for name, region in REGIONS.items():
        icon = make_icon(source, region)
        icon.save(OUT / f"occ_{name}_16_v02_directional.png")
        icons.append((name, icon))

    scale, cell_w, cell_h = 16, 220, 310
    board = Image.new("RGBA", (cell_w * len(icons), cell_h), "#17171d")
    draw = ImageDraw.Draw(board)
    font = ImageFont.load_default()
    for i, (name, icon) in enumerate(icons):
        x0 = i * cell_w
        draw.text((x0 + 12, 12), LABELS[name], font=font, fill="#e6e3db")
        draw.text((x0 + 12, 32), "16x16 directional study", font=font, fill="#a9a7b1")
        one_x, one_y = x0 + 102, 64
        for y in range(16):
            for x in range(16):
                draw.point((one_x + x, one_y + y), fill="#353540" if (x + y) % 2 else "#292932")
        board.alpha_composite(icon, (one_x, one_y))
        board.alpha_composite(icon.resize((16 * scale, 16 * scale), Image.Resampling.NEAREST), (x0 - 18, 100))
    board.save(QA / "items16_v02_directional_overview.png")


if __name__ == "__main__":
    main()
