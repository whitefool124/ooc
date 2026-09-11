#!/usr/bin/env python3
"""Build the OCC node-map art review batch from independent imagegen sources."""

from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageEnhance


ROOT = Path(__file__).resolve().parents[2]
BATCH = ROOT / "Artifacts/OCC_NodeMapArtBatch_20260910"
RAW = BATCH / "raw"
PROCESSED = BATCH / "processed"
QA = BATCH / "qa"
MANIFESTS = BATCH / "manifests"

INK = (24, 33, 42, 255)
IVORY = (229, 216, 184, 255)
COPPER = (167, 101, 63, 255)
CYAN = (113, 185, 181, 255)
TRANSPARENT = (0, 0, 0, 0)

ICON_NAMES = (
    "normal_combat",
    "elite_combat",
    "boss",
    "workshop",
    "infirmary",
    "shop",
    "event",
)


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def px(draw: ImageDraw.ImageDraw, x: int, y: int, color, w: int = 1, h: int = 1) -> None:
    draw.rectangle((x, y, x + w - 1, y + h - 1), fill=color)


def line(draw: ImageDraw.ImageDraw, points, color, width: int = 1) -> None:
    draw.line(points, fill=color, width=width)


def icon_normal_combat() -> Image.Image:
    im = Image.new("RGBA", (16, 16), TRANSPARENT); d = ImageDraw.Draw(im)
    line(d, [(3, 3), (12, 12)], INK, 3); line(d, [(4, 3), (13, 12)], IVORY, 1)
    line(d, [(12, 3), (3, 12)], INK, 3); line(d, [(11, 3), (2, 12)], CYAN, 1)
    px(d, 2, 12, COPPER, 3, 2); px(d, 11, 12, COPPER, 3, 2)
    return im


def icon_elite_combat() -> Image.Image:
    im = icon_normal_combat(); d = ImageDraw.Draw(im)
    px(d, 5, 1, INK, 6, 3); px(d, 6, 1, IVORY); px(d, 8, 1, IVORY); px(d, 10, 1, IVORY)
    px(d, 6, 2, COPPER, 5, 1); px(d, 8, 1, COPPER)
    return im


def icon_boss() -> Image.Image:
    im = Image.new("RGBA", (16, 16), TRANSPARENT); d = ImageDraw.Draw(im)
    px(d, 2, 2, INK, 3, 3); px(d, 11, 2, INK, 3, 3)
    px(d, 2, 1, COPPER, 2, 3); px(d, 12, 1, COPPER, 2, 3)
    px(d, 4, 3, INK, 8, 10); px(d, 3, 5, INK, 10, 6)
    px(d, 5, 4, IVORY, 6, 8); px(d, 4, 6, IVORY, 8, 4)
    px(d, 6, 5, CYAN, 4, 4); px(d, 7, 6, INK, 2, 2)
    px(d, 5, 10, COPPER, 2, 2); px(d, 9, 10, COPPER, 2, 2); px(d, 7, 11, INK, 2, 3)
    return im


def icon_workshop() -> Image.Image:
    im = Image.new("RGBA", (16, 16), TRANSPARENT); d = ImageDraw.Draw(im)
    line(d, [(4, 3), (11, 10)], INK, 4); line(d, [(4, 3), (11, 10)], COPPER, 2)
    px(d, 2, 2, INK, 6, 4); px(d, 3, 2, IVORY, 4, 2)
    px(d, 8, 11, INK, 6, 3); px(d, 9, 11, IVORY, 4, 1)
    px(d, 11, 8, CYAN); px(d, 13, 9, CYAN); px(d, 12, 7, CYAN)
    return im


def icon_infirmary() -> Image.Image:
    im = Image.new("RGBA", (16, 16), TRANSPARENT); d = ImageDraw.Draw(im)
    px(d, 3, 4, INK, 10, 9); px(d, 4, 5, COPPER, 8, 7)
    px(d, 6, 2, INK, 4, 3); px(d, 7, 2, COPPER, 2, 2)
    px(d, 7, 6, CYAN, 2, 5); px(d, 5, 8, CYAN, 6, 2)
    return im


def icon_shop() -> Image.Image:
    im = Image.new("RGBA", (16, 16), TRANSPARENT); d = ImageDraw.Draw(im)
    px(d, 2, 3, INK, 12, 4); px(d, 3, 3, IVORY, 2, 3); px(d, 5, 3, CYAN, 2, 3)
    px(d, 7, 3, IVORY, 2, 3); px(d, 9, 3, CYAN, 2, 3); px(d, 11, 3, IVORY, 2, 3)
    px(d, 3, 7, INK, 2, 6); px(d, 11, 7, INK, 2, 6); px(d, 3, 12, INK, 10, 2)
    px(d, 7, 8, COPPER, 3, 3); px(d, 8, 9, IVORY)
    return im


def icon_event() -> Image.Image:
    im = Image.new("RGBA", (16, 16), TRANSPARENT); d = ImageDraw.Draw(im)
    px(d, 3, 2, INK, 10, 12); px(d, 4, 3, IVORY, 8, 10); px(d, 10, 3, COPPER, 2, 2)
    line(d, [(8, 5), (8, 10)], CYAN, 2); line(d, [(8, 8), (5, 6)], CYAN, 2); line(d, [(8, 9), (11, 6)], CYAN, 2)
    px(d, 4, 5, INK); px(d, 10, 5, INK); px(d, 7, 10, INK, 2, 2)
    return im


ICON_BUILDERS = {
    "normal_combat": icon_normal_combat,
    "elite_combat": icon_elite_combat,
    "boss": icon_boss,
    "workshop": icon_workshop,
    "infirmary": icon_infirmary,
    "shop": icon_shop,
    "event": icon_event,
}


def crop_16_9(im: Image.Image) -> Image.Image:
    ratio = im.width / im.height
    if ratio > 16 / 9:
        width = round(im.height * 16 / 9); left = (im.width - width) // 2
        return im.crop((left, 0, left + width, im.height))
    height = round(im.width * 9 / 16); top = (im.height - height) // 2
    return im.crop((0, top, im.width, top + height))


def build_background() -> Image.Image:
    source = Image.open(RAW / "map_background_source.png").convert("RGB")
    image = crop_16_9(source).resize((480, 270), Image.Resampling.BOX)
    image = ImageEnhance.Color(image).enhance(0.68)
    image = ImageEnhance.Contrast(image).enhance(0.58)
    image = image.quantize(colors=24, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert("RGBA")
    image.putalpha(255)
    return image


def checker_for(image: Image.Image, scale: int = 4) -> Image.Image:
    up = image.resize((image.width * scale, image.height * scale), Image.Resampling.NEAREST)
    board = Image.new("RGBA", up.size, (218, 211, 196, 255)); d = ImageDraw.Draw(board)
    cell = 8
    for y in range(0, board.height, cell):
        for x in range(0, board.width, cell):
            if (x // cell + y // cell) % 2:
                d.rectangle((x, y, x + cell - 1, y + cell - 1), fill=(166, 172, 169, 255))
    board.alpha_composite(up)
    return board.convert("RGB")


def write_icon_evidence(name: str, image: Image.Image) -> None:
    image.save(QA / f"node_{name}_1x.png")
    image.resize((64, 64), Image.Resampling.NEAREST).save(QA / f"node_{name}_4x.png")
    gray = image.convert("LA").convert("RGBA").resize((64, 64), Image.Resampling.NEAREST)
    gray.save(QA / f"node_{name}_grayscale_4x.png")
    checker_for(image).save(QA / f"node_{name}_checker_4x.png")


def build_application_contact(background: Image.Image, icons: dict[str, Image.Image]) -> Image.Image:
    canvas = background.resize((960, 540), Image.Resampling.NEAREST).convert("RGBA")
    overlay = Image.new("RGBA", canvas.size, (15, 21, 27, 34)); canvas = Image.alpha_composite(canvas, overlay)
    d = ImageDraw.Draw(canvas)
    points = [(120, 370), (245, 300), (360, 350), (480, 250), (610, 305), (730, 220), (845, 145)]
    for a, b in zip(points, points[1:]):
        d.line((a, b), fill=(229, 216, 184, 255), width=6)
        d.line((a, b), fill=(63, 80, 87, 255), width=2)
    for index, (name, point) in enumerate(zip(ICON_NAMES, points)):
        x, y = point
        d.ellipse((x - 31, y - 31, x + 31, y + 31), fill=(24, 33, 42, 255), outline=(229, 216, 184, 255), width=4)
        sprite = icons[name].resize((48, 48), Image.Resampling.NEAREST)
        canvas.alpha_composite(sprite, (x - 24, y - 24))
        if index == 3:
            d.rectangle((x - 36, y - 36, x + 36, y + 36), outline=(113, 185, 181, 255), width=4)
    return canvas.convert("RGB")


def manifest(asset_id: str, role: str, source_name: str, output_name: str, evidence_prefix: str, contact: str, notes: str) -> dict:
    source = RAW / source_name; output = PROCESSED / output_name
    return {
        "schema": "occ-art-manifest-v1", "contract_version": 1,
        "asset_id": asset_id, "role": role, "status": "QA_PENDING",
        "provenance": {
            "source_channel": "codex_builtin_imagegen",
            "source_descriptor": "Independent single-asset imagegen source; icon silhouettes were redrawn on the native 16px grid, while the backdrop was normalized directly.",
            "source_path": source.relative_to(ROOT).as_posix(), "source_sha256": sha256(source),
        },
        "delivery": {
            "output_path": output.relative_to(ROOT).as_posix(), "output_sha256": sha256(output),
            "native_output_path": None, "logical_cells": None,
            "palette_max": 4 if role == "semantic_icon_16" else 24,
            "required_color_families": [],
        },
        "application": {
            "runtime_draw_rect": "16x16 node-type symbol inside a runtime-drawn node state ring" if role == "semantic_icon_16" else "480x270 map-page backdrop behind runtime-drawn nodes, routes, selection and reachability states",
            "default_integer_scale": 4, "minimum_integer_scale": 2,
        },
        "evidence": {
            "one_x": f"Artifacts/OCC_NodeMapArtBatch_20260910/qa/{evidence_prefix}_1x.png",
            "four_x": f"Artifacts/OCC_NodeMapArtBatch_20260910/qa/{evidence_prefix}_4x.png",
            "grayscale": f"Artifacts/OCC_NodeMapArtBatch_20260910/qa/{evidence_prefix}_grayscale_4x.png",
            "checker": f"Artifacts/OCC_NodeMapArtBatch_20260910/qa/{evidence_prefix}_checker_4x.png",
            "application_contact": f"Artifacts/OCC_NodeMapArtBatch_20260910/qa/{contact}",
        },
        "human_review": {
            "overall": "PENDING", "reviewer": "", "date": "",
            "silhouette": "PENDING", "material": "PENDING", "perspective": "PENDING",
            "style": "PENDING", "application": "PENDING", "notes": notes,
        },
        "unity_import": None,
    }


def main() -> None:
    for folder in (PROCESSED, QA, MANIFESTS): folder.mkdir(parents=True, exist_ok=True)
    icons = {name: builder() for name, builder in ICON_BUILDERS.items()}
    for name, image in icons.items():
        image.save(PROCESSED / f"node_{name}_16.png")
        write_icon_evidence(name, image)
    background = build_background(); background.save(PROCESSED / "map_background_480x270.png")
    background.save(QA / "map_background_1x.png")
    background_4x = background.resize((1920, 1080), Image.Resampling.NEAREST)
    background_4x.save(QA / "map_background_4x.png")
    background_4x.convert("L").convert("RGBA").save(QA / "map_background_grayscale_4x.png")
    background_4x.save(QA / "map_background_checker_4x.png")
    contact = build_application_contact(background, icons); contact.save(QA / "node_map_application_contact.png")
    review = Image.new("RGB", (960, 720), (24, 33, 42)); d = ImageDraw.Draw(review)
    review.paste(contact.resize((800, 450), Image.Resampling.NEAREST), (80, 24))
    for i, name in enumerate(ICON_NAMES):
        x = 64 + i * 128; review.paste(checker_for(icons[name]), (x, 530)); d.text((x, 600), name, fill=(229, 216, 184))
    review.save(QA / "node_map_batch_review_board.png")

    for name in ICON_NAMES:
        data = manifest(f"ui.node.{name}", "semantic_icon_16", f"node_{name}_source.png", f"node_{name}_16.png", f"node_{name}", "node_map_application_contact.png", "Review at 1x and in the shared map contact; runtime rings carry current, reachable and completed states.")
        (MANIFESTS / f"node_{name}.occ-art.json").write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    data = manifest("ui.map.background", "ui_backdrop_480x270", "map_background_source.png", "map_background_480x270.png", "map_background", "node_map_application_contact.png", "Low-contrast regional field only; no baked nodes, routes, labels or state indicators.")
    (MANIFESTS / "map_background.occ-art.json").write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"status": "PASS", "icons": len(icons), "backgrounds": 1}, ensure_ascii=False))


if __name__ == "__main__":
    main()
