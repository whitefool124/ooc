"""Build the approved hand-authored OCC 8px resource-icon candidate set.

Each glyph is declared directly on its native 8x8 grid.  The script never
rescales delivery art; nearest-neighbour scaling is used only for QA evidence.
"""

from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "ArtSource/resource_icons_8_handdrawn_2026-09-22"
EVIDENCE = OUT / "evidence"
MANIFESTS = OUT / "manifests"

INK = (22, 31, 40, 255)

SPECS = {
    "action_point": {
        "label": "Action point",
        "palette": {"D": INK, "Y": (238, 185, 60, 255), "H": (255, 233, 142, 255)},
        "rows": ["........", "...H....", "..YY....", ".YYYY...", "...YY...", "..YY....", "..D.....", "........"],
    },
    "aether_load": {
        "label": "Aether load",
        "palette": {"D": INK, "C": (35, 169, 190, 255), "H": (158, 238, 239, 255)},
        "rows": ["........", "..DDD...", "..C.C...", "..C.C...", ".DCCCD..", ".DCHCD..", "..DDD...", "........"],
    },
    "charges": {
        "label": "Charges",
        "palette": {"D": INK, "C": (84, 177, 145, 255), "H": (190, 235, 178, 255)},
        "rows": ["........", ".D.D.D..", ".C.C.C..", ".C.C.C..", ".H.H.H..", ".D.D.D..", "........", "........"],
    },
    "contribution": {
        "label": "Contribution",
        "palette": {"D": INK, "C": (77, 150, 191, 255), "H": (184, 218, 230, 255)},
        "rows": ["........", "...H....", ".DCCCD..", "..CCC...", ".DCCCD..", "...D....", "........", "........"],
    },
    "explored": {
        "label": "Explored",
        "palette": {"D": INK, "W": (218, 210, 177, 255), "C": (71, 151, 162, 255)},
        "rows": ["........", ".D.D.D..", ".WWCWW..", ".W.C.W..", ".W.C.W..", ".WWCWW..", ".D.D.D..", "........"],
    },
    "gold": {
        "label": "Gold",
        "palette": {"D": (91, 59, 25, 255), "Y": (221, 159, 43, 255), "H": (255, 224, 117, 255)},
        "rows": ["........", "..DDD...", ".DYYHD..", ".DYYYD..", ".DYYYD..", "..DDD...", "........", "........"],
    },
    "health": {
        "label": "Health",
        "palette": {"D": (28, 74, 52, 255), "G": (76, 181, 102, 255), "H": (181, 235, 168, 255)},
        "rows": ["........", "..G.G...", ".GGGGG..", ".GGHGG..", "..GGG...", "...G....", "...D....", "........"],
    },
    "mana": {
        "label": "Mana",
        "palette": {"D": (20, 65, 82, 255), "C": (42, 185, 211, 255), "H": (177, 241, 239, 255)},
        "rows": ["........", "...H....", "..CCC...", ".CCCCC..", "..CCC...", "...D....", "........", "........"],
    },
    "notice": {
        "label": "Notice",
        "palette": {"D": (74, 53, 26, 255), "Y": (221, 171, 64, 255), "H": (255, 229, 153, 255)},
        "rows": ["........", ".DDDDD..", ".DHHHD..", ".DHYHD..", ".DHYHD..", ".DHHHD..", ".DDYDD..", "........"],
    },
    "operational_aether": {
        "label": "Operational aether",
        "palette": {"D": INK, "C": (35, 169, 190, 255), "H": (158, 238, 239, 255)},
        "rows": ["........", "..DDD...", ".DCCCD..", ".DC.CD..", ".DC.CD..", ".DCCCD..", "..DHD...", "........"],
    },
    "parts": {
        "label": "Parts",
        "palette": {"D": INK, "S": (138, 145, 143, 255), "H": (210, 210, 194, 255)},
        "rows": ["........", "..D.D...", ".DSSSD..", ".SS.HSS.", ".DSSSD..", "..D.D...", "........", "........"],
    },
    "risk": {
        "label": "Risk",
        "palette": {"D": (73, 31, 27, 255), "R": (191, 61, 45, 255), "H": (248, 143, 68, 255)},
        "rows": ["........", "...D....", "..DRD...", "..RRR...", ".RRHRR..", ".DDDDD..", "........", "........"],
    },
    "shield": {
        "label": "Shield",
        "palette": {"D": (42, 54, 65, 255), "S": (151, 166, 178, 255), "H": (226, 235, 238, 255)},
        "rows": ["........", "..DDD...", ".DSSSD..", ".DSHSD..", ".DSSSD..", "..DSD...", "...D....", "........"],
    },
    "stage_time": {
        "label": "Stage time",
        "palette": {"D": INK, "W": (190, 181, 145, 255), "H": (246, 215, 112, 255)},
        "rows": ["........", ".DDDDD..", ".DWWWD..", "..DHD...", "...D....", "..DWD...", ".DDDDD..", "........"],
    },
    "weight": {
        "label": "Weight",
        "palette": {"D": INK, "S": (109, 120, 124, 255), "H": (190, 199, 196, 255)},
        "rows": ["........", "...D....", "..DHD...", ".DSSSD..", ".DSSSD..", ".DSSSD..", ".DDDDD..", "........"],
    },
}


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def checker(size: tuple[int, int], block: int) -> Image.Image:
    image = Image.new("RGBA", size, (205, 205, 205, 255))
    draw = ImageDraw.Draw(image)
    for y in range(0, size[1], block):
        for x in range(0, size[0], block):
            if (x // block + y // block) % 2:
                draw.rectangle((x, y, x + block - 1, y + block - 1), fill=(238, 238, 238, 255))
    return image


def build_icon(rows: list[str], palette: dict[str, tuple[int, int, int, int]]) -> Image.Image:
    if len(rows) != 8 or any(len(row) != 8 for row in rows):
        raise ValueError("Every resource icon must be an exact 8x8 pattern")
    image = Image.new("RGBA", (8, 8), (0, 0, 0, 0))
    pixels = image.load()
    for y, row in enumerate(rows):
        for x, key in enumerate(row):
            if key != ".":
                pixels[x, y] = palette[key]
    alpha = image.getchannel("A")
    bounds = alpha.getbbox()
    if bounds is None or bounds[0] < 1 or bounds[1] < 1 or bounds[2] > 7 or bounds[3] > 7:
        raise ValueError(f"Transparent one-pixel border violated: {bounds}")
    if any(alpha.getpixel((x, y)) not in (0, 255) for y in range(8) for x in range(8)):
        raise ValueError("Alpha must be binary")
    if len({image.getpixel((x, y)) for y in range(8) for x in range(8) if image.getpixel((x, y))[3]}) > 4:
        raise ValueError("Visible palette exceeds four colours")
    return image


def save_evidence(stem: str, icon: Image.Image) -> dict[str, str]:
    asset_evidence = EVIDENCE / stem
    asset_evidence.mkdir(parents=True, exist_ok=True)
    one_x = asset_evidence / f"{stem}_1x.png"
    four_x = asset_evidence / f"{stem}_4x.png"
    grayscale = asset_evidence / f"{stem}_grayscale.png"
    checker_path = asset_evidence / f"{stem}_checker.png"
    icon.save(one_x)
    icon.resize((32, 32), Image.Resampling.NEAREST).save(four_x)
    alpha = icon.getchannel("A")
    gray = icon.convert("L")
    gray_rgba = Image.merge("RGBA", (gray, gray, gray, alpha))
    gray_rgba.save(grayscale)
    board = checker((64, 64), 8)
    enlarged = icon.resize((64, 64), Image.Resampling.NEAREST)
    board.alpha_composite(enlarged)
    board.save(checker_path)
    return {
        "one_x": one_x.relative_to(ROOT).as_posix(),
        "four_x": four_x.relative_to(ROOT).as_posix(),
        "grayscale": grayscale.relative_to(ROOT).as_posix(),
        "checker": checker_path.relative_to(ROOT).as_posix(),
    }


def build_contacts(icons: dict[str, Image.Image]) -> tuple[Path, Path]:
    font = ImageFont.load_default()
    review = Image.new("RGBA", (960, 540), (18, 25, 32, 255))
    draw = ImageDraw.Draw(review)
    draw.text((24, 14), "OCC RESOURCE ICONS 8px — 16x QA PREVIEW", fill=(236, 226, 199, 255), font=font)
    for index, (stem, icon) in enumerate(icons.items()):
        column, row = index % 5, index // 5
        x, y = 24 + column * 186, 48 + row * 158
        tile = checker((128, 128), 16)
        tile.alpha_composite(icon.resize((128, 128), Image.Resampling.NEAREST))
        review.alpha_composite(tile, (x, y))
        draw.text((x, y + 132), stem, fill=(225, 216, 190, 255), font=font)
    review_path = EVIDENCE / "resource_icons_8_contact_16x.png"
    review.save(review_path)

    application = Image.new("RGBA", (960, 540), (27, 35, 43, 255))
    app_draw = ImageDraw.Draw(application)
    app_draw.text((24, 16), "32px runtime chips (native 8px ×4)", fill=(236, 226, 199, 255), font=font)
    for index, (stem, icon) in enumerate(icons.items()):
        column, row = index % 3, index // 3
        x, y = 24 + column * 306, 52 + row * 92
        app_draw.rounded_rectangle((x, y, x + 280, y + 64), radius=4, fill=(39, 50, 60, 255), outline=(113, 123, 124, 255), width=2)
        application.alpha_composite(icon.resize((32, 32), Image.Resampling.NEAREST), (x + 16, y + 16))
        app_draw.text((x + 60, y + 17), stem, fill=(224, 218, 200, 255), font=font)
        app_draw.text((x + 236, y + 17), "×12", fill=(246, 224, 165, 255), font=font)
    application_path = EVIDENCE / "resource_icons_8_application_mock.png"
    application.save(application_path)
    return review_path, application_path


def write_manifest(stem: str, label: str, output: Path, evidence: dict[str, str], application: Path) -> Path:
    manifest = {
        "schema": "occ-art-manifest-v1",
        "contract_version": 1,
        "asset_id": f"ui.resource.{stem}",
        "role": "resource_icon_8",
        "status": "QA_PENDING",
        "provenance": {
            "source_channel": "manual_pixel_authoring",
            "source_descriptor": f"User-authorized direct native-grid authoring for the {label} resource semantic. No generated source, geometric resize, interpolation, dithering, or opaque backing tile.",
            "source_path": output.relative_to(ROOT).as_posix(),
            "source_sha256": sha256(output),
        },
        "delivery": {
            "output_path": output.relative_to(ROOT).as_posix(),
            "output_sha256": sha256(output),
            "native_output_path": None,
            "logical_cells": None,
            "palette_max": 4,
            "required_color_families": [],
        },
        "application": {
            "runtime_draw_rect": "Native 8x8 transparent resource glyph displayed only at 2x, 3x, or 4x integer scale.",
            "default_integer_scale": 4,
            "minimum_integer_scale": 2,
        },
        "evidence": {
            **evidence,
            "application_contact": application.relative_to(ROOT).as_posix(),
        },
        "human_review": {
            "overall": "PENDING",
            "reviewer": "",
            "date": "",
            "silhouette": "PENDING",
            "material": "PENDING",
            "perspective": "PENDING",
            "style": "PENDING",
            "application": "PENDING",
            "notes": "Review at native 1x, 16x checker contact, and the 32px runtime mock. Unity import and runtime replacement remain blocked until aesthetic approval.",
        },
        "unity_import": None,
        "pixel_grid_qa": {
            "authoring_mode": "direct_native_grid",
            "target_canvas": [8, 8],
            "geometric_resize_or_interpolation_performed": False,
            "new_colours_introduced_after_authoring": False,
        },
    }
    path = MANIFESTS / f"{stem}.occ-art-manifest-v1.json"
    path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    return path


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    EVIDENCE.mkdir(parents=True, exist_ok=True)
    MANIFESTS.mkdir(parents=True, exist_ok=True)
    icons: dict[str, Image.Image] = {}
    evidence_by_stem: dict[str, dict[str, str]] = {}
    for stem, spec in SPECS.items():
        icon = build_icon(spec["rows"], spec["palette"])
        output = OUT / f"{stem}.png"
        icon.save(output)
        icons[stem] = icon
        evidence_by_stem[stem] = save_evidence(stem, icon)
    review, application = build_contacts(icons)
    manifests = []
    for stem, spec in SPECS.items():
        manifests.append(write_manifest(stem, spec["label"], OUT / f"{stem}.png", evidence_by_stem[stem], application))
    print(json.dumps({
        "count": len(icons),
        "review": review.relative_to(ROOT).as_posix(),
        "application": application.relative_to(ROOT).as_posix(),
        "manifests": [path.relative_to(ROOT).as_posix() for path in manifests],
    }, ensure_ascii=False))


if __name__ == "__main__":
    main()
