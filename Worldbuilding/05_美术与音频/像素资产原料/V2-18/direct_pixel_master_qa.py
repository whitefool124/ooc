"""Directly sample a prompted logical-pixel master without crop or rescale fitting."""
import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw


def main(source, output, qa, report, size, palette_limit, chroma_key):
    src = Image.open(source).convert("RGBA")
    image = src.resize((size, size), Image.Resampling.NEAREST)
    pixels = image.load()
    for y in range(size):
        for x in range(size):
            r, g, b, a = pixels[x, y]
            if chroma_key and g > 180 and g > r * 1.4 and g > b * 1.4:
                pixels[x, y] = (r, g, b, 0)
            else:
                pixels[x, y] = (r, g, b, 255 if a > 32 else 0)

    alpha = image.getchannel("A")
    rgb = Image.new("RGB", image.size, (0, 0, 0))
    rgb.paste(image.convert("RGB"), mask=alpha)
    image = rgb.quantize(colors=palette_limit, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert("RGBA")
    image.putalpha(alpha)
    visible_colors = {pixel[:3] for pixel in image.getdata() if pixel[3] > 0}
    mask = alpha.point(lambda value: 255 if value else 0)
    bounds = mask.getbbox()
    hard_alpha = set(alpha.getdata()).issubset({0, 255})
    status = "PASS" if image.size == (size, size) and hard_alpha and len(visible_colors) <= palette_limit else "WARN"
    Path(output).parent.mkdir(parents=True, exist_ok=True)
    Path(qa).parent.mkdir(parents=True, exist_ok=True)
    Path(report).parent.mkdir(parents=True, exist_ok=True)
    image.save(output)

    preview = image.resize((size * 4, size * 4), Image.Resampling.NEAREST)
    draw = ImageDraw.Draw(preview)
    draw.rectangle((0, 0, size * 4 - 1, size * 4 - 1), outline=(0, 255, 255, 255), width=4)
    if size == 64:
        draw.line((32 * 4, 0, 32 * 4, size * 4 - 1), fill=(255, 0, 255, 255), width=4)
        draw.line((0, 58 * 4, size * 4 - 1, 58 * 4), fill=(255, 255, 0, 255), width=4)
    preview.save(qa)
    Path(report).write_text(json.dumps({
        "status": status,
        "method": "direct nearest-neighbor sampling followed by palette quantization only; no crop, fit, or reposition",
        "source": str(source), "output": str(output), "qa": str(qa),
        "size": [size, size], "paletteColors": len(visible_colors), "paletteLimit": palette_limit,
        "hardAlpha": hard_alpha, "bounds": list(bounds) if bounds else None,
        "chromaKey": chroma_key
    }, ensure_ascii=False, indent=2), encoding="utf-8")
    print(status, image.size, len(visible_colors), bounds)


if __name__ == "__main__":
    main(*sys.argv[1:5], int(sys.argv[5]), int(sys.argv[6]), sys.argv[7].lower() == "true")
