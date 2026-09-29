"""Build a reversible in-game trial from the second generated intent icon batch.

These previews are deliberately marked QA_PENDING: the source lattice did not
meet the formal lossless 16x16 pixel-asset contract.
"""

from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "ArtSource/intent_icon_reset_20260928/sources"
EVIDENCE = ROOT / "ArtSource/intent_icon_reset_20260928/trial_v2"
OUTPUT = ROOT / "UnityProject/Assets/Game/Resources/Art/IntentIconTrial16"
NAMES = ("attack", "cast", "move", "defend", "interact_destroy")


def relative(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def make_icon(source: Path) -> Image.Image:
    image = Image.open(source).convert("RGBA")
    mask = image.getchannel("A").point(lambda value: 255 if value >= 128 else 0)
    bounds = mask.getbbox()
    if bounds is None:
        raise ValueError(f"empty source: {source}")
    crop = image.crop(bounds)
    width, height = crop.size
    scale = min(14 / width, 14 / height)
    fitted = crop.resize(
        (max(1, round(width * scale)), max(1, round(height * scale))),
        Image.Resampling.NEAREST,
    )
    alpha = fitted.getchannel("A").point(lambda value: 255 if value >= 128 else 0)
    reduced = fitted.convert("RGB").quantize(colors=4, method=Image.Quantize.MEDIANCUT).convert("RGBA")
    reduced.putalpha(alpha)
    icon = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    icon.alpha_composite(reduced, ((16 - reduced.width) // 2, (16 - reduced.height) // 2))
    return icon


def checker(icon: Image.Image) -> Image.Image:
    board = Image.new("RGBA", (64, 64))
    draw = ImageDraw.Draw(board)
    for y in range(0, 64, 8):
        for x in range(0, 64, 8):
            tone = 88 if (x // 8 + y // 8) % 2 else 136
            draw.rectangle((x, y, x + 7, y + 7), fill=(tone, tone, tone, 255))
    board.alpha_composite(icon.resize((64, 64), Image.Resampling.NEAREST))
    return board


def main() -> None:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    EVIDENCE.mkdir(parents=True, exist_ok=True)
    manifests = EVIDENCE / "manifests"
    manifests.mkdir(exist_ok=True)
    for name in NAMES:
        source = SOURCE / f"{name}_v2.png"
        icon = make_icon(source)
        destination = OUTPUT / f"{name}.png"
        icon.save(destination)
        folder = EVIDENCE / name
        folder.mkdir(exist_ok=True)
        icon.save(folder / "1x.png")
        icon.resize((64, 64), Image.Resampling.NEAREST).save(folder / "4x.png")
        gray = icon.convert("LA").convert("RGBA")
        gray.putalpha(icon.getchannel("A"))
        gray.save(folder / "grayscale.png")
        checker(icon).save(folder / "checker.png")
        manifest = {
            "schema": "occ-art-manifest-v1",
            "contract_version": 1,
            "asset_id": f"intent_trial_v2.{name}",
            "role": "semantic_icon_16",
            "status": "QA_PENDING",
            "provenance": {
                "source_channel": "codex_builtin_imagegen",
                "source_descriptor": "Independent second-batch generated source; temporary visual trial",
                "source_path": relative(source),
                "source_sha256": sha256(source),
            },
            "delivery": {
                "output_path": relative(destination),
                "output_sha256": sha256(destination),
                "native_ppu": 16,
                "palette_max": 4,
            },
            "evidence": {
                "one_x": relative(folder / "1x.png"),
                "four_x": relative(folder / "4x.png"),
                "grayscale": relative(folder / "grayscale.png"),
                "checker": relative(folder / "checker.png"),
                "application_contact": None,
            },
            "human_review": {"overall": "PENDING", "notes": "Trial only: source lattice exceeded 16x16; nearest-neighbour fit is not formal asset normalization."},
        }
        (manifests / f"{name}.occ-art-manifest-v1.json").write_text(
            json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
        )
    print(json.dumps({"trial_icons": len(NAMES), "output": relative(OUTPUT)}))


if __name__ == "__main__":
    main()
