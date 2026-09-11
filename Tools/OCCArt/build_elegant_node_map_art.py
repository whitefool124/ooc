#!/usr/bin/env python3
"""Build the aesthetic-first OCC node map exploration requested on 2026-09-10."""

from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageEnhance


ROOT = Path(__file__).resolve().parents[2]
BATCH = ROOT / "Artifacts/OCC_NodeMapArtElegant_20260910"
RAW = BATCH / "raw"
OUT = BATCH / "processed"
QA = BATCH / "qa"
NAMES = ("normal_combat", "elite_combat", "boss", "workshop", "infirmary", "shop", "event")


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def crop_with_padding(image: Image.Image, fraction: float = 0.035) -> Image.Image:
    alpha = image.getchannel("A")
    # Imagegen leaves a very faint, wide alpha haze around otherwise clean cutouts.
    # Ignore that haze when determining the shared visual fit, then harden alpha later.
    box = alpha.point(lambda value: 255 if value >= 16 else 0).getbbox()
    if box is None:
        raise ValueError("source has no visible pixels")
    pad = round(max(box[2] - box[0], box[3] - box[1]) * fraction)
    return image.crop((max(0, box[0] - pad), max(0, box[1] - pad), min(image.width, box[2] + pad), min(image.height, box[3] + pad)))


def fit_icon(source: Path, size: int, palette: int) -> Image.Image:
    image = crop_with_padding(Image.open(source).convert("RGBA"))
    scale = min((size - 4) / image.width, (size - 4) / image.height)
    fitted = image.resize((max(1, round(image.width * scale)), max(1, round(image.height * scale))), Image.Resampling.LANCZOS)
    alpha = fitted.getchannel("A").point(lambda value: 255 if value >= 96 else 0)
    rgb = fitted.convert("RGB").quantize(colors=palette, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert("RGBA")
    rgb.putalpha(alpha)
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    canvas.alpha_composite(rgb, ((size - rgb.width) // 2, (size - rgb.height) // 2))
    return canvas


def crop_16_9(image: Image.Image) -> Image.Image:
    target = 16 / 9
    if image.width / image.height > target:
        width = round(image.height * target); left = (image.width - width) // 2
        return image.crop((left, 0, left + width, image.height))
    height = round(image.width / target); top = (image.height - height) // 2
    return image.crop((0, top, image.width, top + height))


def build_background() -> Image.Image:
    image = crop_16_9(Image.open(RAW / "map_background.png").convert("RGB"))
    image = image.resize((960, 540), Image.Resampling.LANCZOS)
    image = ImageEnhance.Contrast(image).enhance(0.92)
    return image


def ringed_node(icon: Image.Image, state: str = "open") -> Image.Image:
    node = Image.new("RGBA", (80, 80), (0, 0, 0, 0)); d = ImageDraw.Draw(node)
    shadow = (18, 29, 38, 210); copper = (185, 112, 77, 255); ivory = (242, 230, 201, 255); cyan = (126, 205, 202, 255)
    d.ellipse((4, 5, 76, 77), fill=shadow, outline=copper, width=4)
    d.ellipse((9, 10, 71, 72), outline=ivory if state != "current" else cyan, width=2)
    if state == "current": d.arc((1, 2, 79, 80), 195, 345, fill=cyan, width=4)
    node.alpha_composite(icon, (16, 15))
    return node


def build_application(background: Image.Image, icons: dict[str, Image.Image]) -> Image.Image:
    canvas = background.resize((1920, 1080), Image.Resampling.LANCZOS).convert("RGBA")
    veil = Image.new("RGBA", canvas.size, (25, 34, 42, 24)); canvas = Image.alpha_composite(canvas, veil)
    d = ImageDraw.Draw(canvas)
    points = [(210, 680), (450, 580), (690, 640), (920, 500), (1180, 585), (1435, 445), (1690, 315)]
    for a, b in zip(points, points[1:]):
        d.line((a, b), fill=(60, 50, 48, 150), width=14)
        d.line((a, b), fill=(238, 222, 191, 255), width=7)
        d.line((a, b), fill=(188, 113, 77, 255), width=3)
    for index, (name, point) in enumerate(zip(NAMES, points)):
        node = ringed_node(icons[name], "current" if index == 3 else "open")
        canvas.alpha_composite(node, (point[0] - 40, point[1] - 40))
    return canvas.convert("RGB")


def checker(image: Image.Image, cell: int = 16) -> Image.Image:
    board = Image.new("RGBA", image.size, (232, 226, 214, 255)); d = ImageDraw.Draw(board)
    for y in range(0, image.height, cell):
        for x in range(0, image.width, cell):
            if (x // cell + y // cell) % 2:
                d.rectangle((x, y, x + cell - 1, y + cell - 1), fill=(180, 188, 185, 255))
    board.alpha_composite(image)
    return board.convert("RGB")


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True); QA.mkdir(parents=True, exist_ok=True)
    icons = {}
    for name in NAMES:
        icon = fit_icon(RAW / f"{name}.png", 48, 32); icons[name] = icon
        icon.save(OUT / f"node_{name}_48.png")
        enlarged = icon.resize((192, 192), Image.Resampling.NEAREST)
        enlarged.save(QA / f"node_{name}_4x.png")
        enlarged.convert("LA").convert("RGBA").save(QA / f"node_{name}_grayscale_4x.png")
        checker(enlarged, 16).save(QA / f"node_{name}_checker.png")
    background = build_background(); background.save(OUT / "map_background_960x540.png")
    contact = build_application(background, icons); contact.save(QA / "elegant_node_map_application_1920x1080.png")

    board = Image.new("RGB", (1920, 1400), (22, 30, 38)); board.paste(contact, (0, 0))
    d = ImageDraw.Draw(board)
    for index, name in enumerate(NAMES):
        x = 42 + index * 270
        tile = checker(icons[name].resize((192, 192), Image.Resampling.NEAREST), 16)
        board.paste(tile, (x, 1130)); d.text((x, 1332), name, fill=(242, 230, 201))
    board.save(QA / "elegant_node_map_review_board.png")

    record = {
        "schema": "occ-art-aesthetic-exploration-v1",
        "status": "REVIEW_READY",
        "direction": "academy enamel cartography",
        "user_authorized_spec_exception": True,
        "final_icon_size": [48, 48],
        "final_icon_palette_max": 32,
        "intentional_deviations": [
            "node icons use the user-selected 48x48 tier and 32 colors instead of semantic_icon_16",
            "icons retain decorative material rendering and fine internal shading",
            "map backdrop uses 960x540 and a broad painterly palette instead of ui_backdrop_480x270/24 colors",
        ],
        "invariants_kept": ["seven valid node types", "independent source per icon", "transparent icons", "no baked route/node/text in background", "no Unity import"],
        "assets": [],
    }
    for name in NAMES:
        source = RAW / f"{name}.png"; output = OUT / f"node_{name}_48.png"
        record["assets"].append({"id": name, "source": source.relative_to(ROOT).as_posix(), "source_sha256": sha256(source), "output": output.relative_to(ROOT).as_posix(), "output_sha256": sha256(output)})
    source = RAW / "map_background.png"; output = OUT / "map_background_960x540.png"
    record["assets"].append({"id": "map_background", "source": source.relative_to(ROOT).as_posix(), "source_sha256": sha256(source), "output": output.relative_to(ROOT).as_posix(), "output_sha256": sha256(output)})
    (BATCH / "batch_record.json").write_text(json.dumps(record, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"status": "PASS", "icons": len(icons), "background": list(background.size)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
