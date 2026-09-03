"""Normalize the V2-22 B/C identity assets without cropping or fitting."""
import json
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).parent
JOBS = (
    ("立绘/raw/occ_hero_template_portrait_v01_codex_raw.png", "立绘/occ_hero_template_portrait_v01.png", "QA/portrait", 384, 576, 40),
    ("演出/raw/occ_hero_template_performance_resolve_v01_codex_raw.png", "演出/occ_hero_template_performance_resolve_v01.png", "QA/performance_resolve", 512, 768, 48),
)


def process(source_rel, output_rel, qa_rel, width, height, palette_limit):
    source = ROOT / source_rel
    output = ROOT / output_rel
    qa_dir = ROOT / qa_rel
    output.parent.mkdir(parents=True, exist_ok=True)
    qa_dir.mkdir(parents=True, exist_ok=True)

    source_image = Image.open(source).convert("RGBA")
    image = source_image.resize((width, height), Image.Resampling.NEAREST)
    alpha = image.getchannel("A")
    rgb = Image.new("RGB", image.size, (0, 0, 0))
    rgb.paste(image.convert("RGB"), mask=alpha)
    image = rgb.quantize(colors=palette_limit, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert("RGBA")
    image.putalpha(alpha)
    visible_colors = []
    for pixel in image.getdata():
        if pixel[3] and pixel[:3] not in visible_colors:
            visible_colors.append(pixel[:3])
    hard_alpha = set(alpha.getdata()).issubset({0, 255})
    image.save(output)

    qa = image.resize((width * 2, height * 2), Image.Resampling.NEAREST)
    ImageDraw.Draw(qa).rectangle((0, 0, qa.width - 1, qa.height - 1), outline=(0, 255, 255, 255), width=2)
    qa_path = qa_dir / "qa_2x.png"
    qa.save(qa_path)

    swatches = Image.new("RGBA", (384, 384), (0, 0, 0, 0))
    draw = ImageDraw.Draw(swatches)
    for index, color in enumerate(visible_colors):
        x = (index % 6) * 64
        y = (index // 6) * 48
        draw.rectangle((x, y, x + 63, y + 47), fill=(*color, 255))
        draw.rectangle((x, y, x + 63, y + 47), outline=(255, 255, 255, 255), width=1)
    palette_path = qa_dir / "palette.png"
    swatches.save(palette_path)

    report = {
        "status": "PASS" if image.size == (width, height) and len(visible_colors) <= palette_limit and hard_alpha else "WARN",
        "method": "full-canvas nearest-neighbor sampling followed by palette quantization only; no crop, fit, or reposition",
        "source": str(source), "output": str(output), "qa": str(qa_path), "palette": str(palette_path),
        "size": [width, height], "paletteColors": len(visible_colors), "paletteLimit": palette_limit,
        "hardAlpha": hard_alpha, "bounds": list(alpha.point(lambda value: 255 if value else 0).getbbox() or ()),
    }
    (qa_dir / "report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(report["status"], output.name, report["size"], report["paletteColors"])


for job in JOBS:
    process(*job)
