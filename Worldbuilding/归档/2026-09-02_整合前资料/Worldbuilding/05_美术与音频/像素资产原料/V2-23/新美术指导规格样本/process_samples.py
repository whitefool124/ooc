"""Normalize two independent OCC image-generation sources and emit QA evidence."""
from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).parent
NEAREST = Image.Resampling.NEAREST

JOBS = (
    {
        "name": "occ_relay_light_cover_v02",
        "source": ROOT / "Props32/raw/occ_relay_light_cover_v02_codex_raw.png",
        "output": ROOT / "Props32/occ_relay_light_cover_v02.png",
        "size": (32, 32), "palette_limit": 16, "baseline": None,
    },
    {
        "name": "occ_scout_riflewoman_v03",
        "source": ROOT / "Units64/raw/occ_scout_riflewoman_v03_codex_raw.png",
        "output": ROOT / "Units64/occ_scout_riflewoman_v03.png",
        "size": (64, 64), "palette_limit": 24, "baseline": 58,
    },
)


def remove_green(image: Image.Image) -> Image.Image:
    rgba = image.convert("RGBA")
    pixels = []
    for r, g, b, a in rgba.getdata():
        pixels.append((r, g, b, 0) if g >= 150 and g >= r * 1.8 and g >= b * 1.8 else (r, g, b, a))
    rgba.putdata(pixels)
    return rgba


def normalize(job: dict) -> dict:
    raw = Image.open(job["source"]).convert("RGBA")
    keyed = remove_green(raw)
    keyed_path = job["output"].with_name(job["output"].stem + "_keyed.png")
    keyed_path.parent.mkdir(parents=True, exist_ok=True)
    keyed.save(keyed_path)

    image = keyed.resize(job["size"], NEAREST)
    alpha = image.getchannel("A").point(lambda value: 255 if value >= 128 else 0)
    rgb = Image.new("RGB", image.size, (0, 0, 0))
    rgb.paste(image.convert("RGB"), mask=alpha)
    image = rgb.quantize(colors=job["palette_limit"], method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert("RGBA")
    image.putalpha(alpha)
    job["output"].parent.mkdir(parents=True, exist_ok=True)
    image.save(job["output"])

    visible = sorted({pixel[:3] for pixel in image.getdata() if pixel[3]})
    bounds = alpha.getbbox()
    alphas = sorted(set(alpha.getdata()))
    report = {
        "status": "PASS" if len(visible) <= job["palette_limit"] and alphas == [0, 255] else "FAIL",
        "source": str(job["source"]), "keyed": str(keyed_path), "output": str(job["output"]),
        "method": "full-canvas nearest-neighbor sampling, chroma-key removal, hard-alpha threshold and no-dither palette quantization; no crop, fit or reposition",
        "size": list(image.size), "paletteColors": len(visible), "paletteLimit": job["palette_limit"],
        "hardAlpha": alphas == [0, 255], "bounds": list(bounds or ()), "baselineY": job["baseline"],
    }
    return image, visible, report


def qa_image(image: Image.Image, report: dict, out: Path) -> None:
    scale = 4
    background = Image.new("RGBA", image.size, (39, 43, 48, 255))
    background.alpha_composite(image)
    qa = background.resize((image.width * scale, image.height * scale), NEAREST)
    draw = ImageDraw.Draw(qa)
    if image.size == (32, 32):
        for p in range(0, 33, 8):
            draw.line((p * scale, 0, p * scale, qa.height - 1), fill=(94, 105, 112, 255))
            draw.line((0, p * scale, qa.width - 1, p * scale), fill=(94, 105, 112, 255))
    if report["baselineY"] is not None:
        x, y = 32 * scale, report["baselineY"] * scale
        draw.line((x, 0, x, qa.height - 1), fill=(45, 221, 254, 255), width=1)
        draw.line((0, y, qa.width - 1, y), fill=(243, 183, 34, 255), width=1)
    qa.save(out)


def palette_image(colors: list[tuple[int, int, int]], out: Path) -> None:
    cell = 24
    sheet = Image.new("RGBA", (cell * 6, cell * max(1, (len(colors) + 5) // 6)), (0, 0, 0, 0))
    draw = ImageDraw.Draw(sheet)
    for index, color in enumerate(colors):
        x, y = (index % 6) * cell, (index // 6) * cell
        draw.rectangle((x, y, x + cell - 1, y + cell - 1), fill=(*color, 255), outline=(255, 255, 255, 255))
    sheet.resize((sheet.width * 4, sheet.height * 4), NEAREST).save(out)


def main() -> None:
    reports = []
    for job in JOBS:
        image, colors, report = normalize(job)
        qa_dir = ROOT / "QA" / job["name"]
        qa_dir.mkdir(parents=True, exist_ok=True)
        qa_image(image, report, qa_dir / "qa_4x.png")
        palette_image(colors, qa_dir / "palette_4x.png")
        (qa_dir / "report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
        reports.append(report)
        print(report["status"], job["name"], report["size"], report["paletteColors"], report["bounds"])
    (ROOT / "QA" / "summary.json").write_text(json.dumps(reports, ensure_ascii=False, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
