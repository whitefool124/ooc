from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parent
RAW = ROOT / "raw_prompt_nature_v02"
OUT = ROOT / "Items32"
QA = ROOT / "QA"
SOURCES = {"occ_cinder_pear_prompt_v02": RAW / "cinder_pear_raw.png", "occ_medic_herb_prompt_v02": RAW / "medic_herb_raw.png"}


def key_to_alpha(image: Image.Image) -> Image.Image:
    result = image.convert("RGBA")
    px = result.load()
    for y in range(result.height):
        for x in range(result.width):
            r, g, b, _ = px[x, y]
            green = g > 165 and g > r * 1.45 and g > b * 1.45
            magenta = r > 130 and b > 90 and g < 130 and abs(r - b) < 115
            if green or magenta:
                px[x, y] = (0, 0, 0, 0)
    return result


def normalize(source: Path, logical: int) -> Image.Image:
    image = key_to_alpha(Image.open(source))
    image = image.resize((logical, logical), Image.Resampling.NEAREST)
    rgb = image.convert("RGB").quantize(colors=6, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert("RGB")
    alpha = image.getchannel("A").point(lambda value: 255 if value >= 128 else 0)
    image = Image.merge("RGBA", (*rgb.split(), alpha))
    final = Image.new("RGBA", (32, 32))
    final.alpha_composite(image, ((32 - logical) // 2, (32 - logical) // 2))
    return final


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    results = []
    for name, source in SOURCES.items():
        for logical in (24, 32):
            asset = normalize(source, logical)
            output = OUT / f"{name}_{logical}logical.png"
            asset.save(output)
            results.append((name, logical, asset))
    board = Image.new("RGB", (1536, 768), (24, 28, 35))
    for index, (name, logical, asset) in enumerate(results):
        x = (index % 2) * 768 + 16
        y = (index // 2) * 384
        panel = Image.new("RGB", (352, 352), (41, 47, 57))
        d = ImageDraw.Draw(panel)
        for yy in range(0, 352, 44):
            for xx in range(0, 352, 44):
                if (xx // 44 + yy // 44) % 2:
                    d.rectangle((xx, yy, xx + 43, yy + 43), fill=(54, 61, 74))
        enlarged = asset.resize((352, 352), Image.Resampling.NEAREST)
        panel.paste(enlarged, (0, 0), enlarged)
        board.paste(panel, (x, y))
        label = ImageDraw.Draw(board)
        label.text((x, y + 355), f"{name} — {logical} logical", fill=(230, 234, 241))
        label.text((x, y + 373), "6 colors max | hard alpha | 32x32 output", fill=(154, 171, 190))
    board.save(QA / "nature_prompt_v02_resolution_overview.png")


if __name__ == "__main__":
    main()
