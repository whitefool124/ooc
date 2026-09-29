"""Finalize existing generated intent sources without resizing any authored pixel."""

from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "ArtSource/intent_icon_reset_20260928"
ASSETS = {
    "attack": ("attack_v2", "attack_32", "attack_32"),
    "cast": ("cast_v3", "cast_v3_32", "cast_v3_32"),
    "move": ("move", "move_original_32", "move_original_32"),
    "defend": ("defend", "defend_original_32", "defend_original_32"),
    "interact_destroy": ("interact_destroy_v2", "interact_destroy_32", "interact_destroy_32"),
}
PREVIEW = BASE / "formal_candidates"


def relative(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def checker(image: Image.Image) -> Image.Image:
    scale = 4
    board = Image.new("RGBA", (image.width * scale, image.height * scale))
    draw = ImageDraw.Draw(board)
    for y in range(board.height):
        for x in range(board.width):
            tone = 96 if ((x // 8) + (y // 8)) % 2 else 144
            draw.point((x, y), fill=(tone, tone, tone, 255))
    board.alpha_composite(image.resize(board.size, Image.Resampling.NEAREST))
    return board


def main() -> None:
    PREVIEW.mkdir(parents=True, exist_ok=True)
    contact = Image.new("RGBA", (5 * 80, 80), (47, 47, 47, 255))
    manifests: list[tuple[str, dict]] = []
    for index, (name, (source_name, decoded_name, qa_name)) in enumerate(ASSETS.items()):
        source = BASE / "sources" / f"{source_name}.png"
        decoded = BASE / "decoded" / f"{decoded_name}.png"
        qa = json.loads((BASE / "qa" / f"{qa_name}.json").read_text(encoding="utf-8"))
        if qa["status"] != "PASS_LOSSLESS_TARGET" or not qa["output_subject_equals_cleaned_decoded_subject_pixel_for_pixel"]:
            raise RuntimeError(f"lossless QA failed: {name}")
        image = Image.open(decoded).convert("RGBA")
        bounds = image.getchannel("A").getbbox()
        if bounds is None:
            raise RuntimeError(f"empty decoded icon: {name}")
        subject = image.crop(bounds)
        if subject.width > 30 or subject.height > 30:
            raise RuntimeError(f"subject exceeds adaptive icon limit: {name}")
        icon = Image.new("RGBA", (subject.width + 2, subject.height + 2), (0, 0, 0, 0))
        icon.alpha_composite(subject, (1, 1))
        folder = PREVIEW / name
        folder.mkdir(exist_ok=True)
        output = folder / f"{name}.png"
        icon.save(output)
        icon.save(folder / "1x.png")
        icon.resize((icon.width * 4, icon.height * 4), Image.Resampling.NEAREST).save(folder / "4x.png")
        icon.resize((icon.width * 6, icon.height * 6), Image.Resampling.NEAREST).save(folder / "6x.png")
        gray = icon.convert("LA").convert("RGBA")
        gray.putalpha(icon.getchannel("A"))
        gray.save(folder / "grayscale.png")
        checker(icon).save(folder / "checker.png")
        enlarged = subject.resize((subject.width * 3, subject.height * 3), Image.Resampling.NEAREST)
        contact.alpha_composite(enlarged, (index * 80 + (80 - enlarged.width) // 2,
                                           (80 - enlarged.height) // 2))
        manifest = {
            "schema": "occ-art-manifest-v1",
            "contract_version": 1,
            "asset_id": f"combat.intent.{name}.regenerated",
            "role": "enemy_intent_icon_adaptive",
            "status": "REVIEW_READY",
            "provenance": {
                "source_channel": "codex_builtin_imagegen",
                "source_descriptor": f"Independent generated {source_name} source; diagnosed native grid; only transparent canvas cropped",
                "source_path": relative(source),
                "source_sha256": sha256(source),
            },
            "delivery": {
                "output_path": relative(output),
                "output_sha256": sha256(output),
                "native_output_path": None,
                "native_ppu": 32,
                "logical_cells": None,
                "palette_max": 4,
                "required_color_families": [],
            },
            "display": {
                "canonical_integer_scale": 3,
                "canonical_at": [1920, 1080],
                "screen_px_per_native_px_at_canonical": 3,
                "integer_only": True,
                "fractional_scale_forbidden": True,
                "uniform_pixel_size_with_siblings": True,
            },
            "application": {
                "runtime_draw_rect": "centred native-size x3 within enemy intent's 64x64 reference pixel slot",
                "default_integer_scale": 3,
                "minimum_integer_scale": 3,
            },
            "evidence": {
                "one_x": relative(folder / "1x.png"),
                "four_x": relative(folder / "4x.png"),
                "six_x": relative(folder / "6x.png"),
                "grayscale": relative(folder / "grayscale.png"),
                "checker": relative(folder / "checker.png"),
                "application_contact": relative(PREVIEW / "contact_3x.png"),
            },
            "human_review": {"overall": "PENDING", "notes": "Review 3x native-pixel contact and Unity runtime before promotion."},
        }
        manifests.append((name, manifest))
    contact.save(PREVIEW / "contact_3x.png")
    for name, manifest in manifests:
        (PREVIEW / name / f"{name}.occ-art-manifest-v1.json").write_text(
            json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
        )
    print(json.dumps({"assets": len(manifests), "contact": relative(PREVIEW / "contact_3x.png")}))


if __name__ == "__main__":
    main()
