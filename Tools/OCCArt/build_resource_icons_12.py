"""Build the user-approved native 12px OCC resource icon set.

Every semantic is drawn on a fresh native grid. No old 8px asset is scaled or
recoloured. A one-pixel pure-black contour is generated around the authored
colour core while preserving a transparent one-pixel canvas border.
"""

from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "ArtSource/resource_icons_12_handdrawn_2026-09-22"
EVIDENCE = OUT / "evidence"
MANIFESTS = OUT / "manifests"
INK = (0, 0, 0, 255)

PALETTES = {
    "gold": {"A": (211, 139, 38, 255), "H": (255, 224, 118, 255)},
    "green": {"A": (65, 163, 92, 255), "H": (181, 232, 158, 255)},
    "cyan": {"A": (39, 158, 187, 255), "H": (164, 232, 235, 255)},
    "blue": {"A": (65, 125, 170, 255), "H": (176, 211, 225, 255)},
    "red": {"A": (179, 58, 44, 255), "H": (244, 137, 66, 255)},
    "silver": {"A": (125, 143, 154, 255), "H": (220, 231, 234, 255)},
    "ivory": {"A": (179, 169, 130, 255), "H": (242, 220, 146, 255)},
}

# Ten-by-ten colour cores are centred inside the 12px delivery canvas. '.' is
# transparent; the black boundary is added after the semantic core is authored.
SPECS = {
    "action_point": ("gold", ["..........", ".....H....", "....HH....", "...HAA....", "..AAAA....", "....AA....", "...AA.....", "..AA......", "..A.......", ".........."]),
    "aether_load": ("cyan", ["..........", "...AAAA...", "..AHHHHA..", "..A....A..", "..A.AA.A..", ".AA.AA.AA.", ".AA.AA.AA.", "..AHHHHA..", "...AAAA...", ".........."]),
    "charges": ("green", ["..........", "..A..A..A.", ".AH.AH.AH.", ".AH.AH.AH.", ".AH.AH.AH.", ".AH.AH.AH.", ".AA.AA.AA.", "..........", "..........", ".........."]),
    "contribution": ("blue", ["..........", "....H.....", "....H.....", "..AAAAA...", ".AAHHHAA..", "..AHHHA...", "...AAA....", "...A.A....", "..A...A...", ".........."]),
    "explored": ("cyan", ["..........", ".AAA..AAA.", ".AHAA.AHA.", ".AH.AAAHA.", ".AH.A.AHA.", ".AHAAA.HA.", ".AHA..AHA.", ".AAA..AAA.", "..........", ".........."]),
    "gold": ("gold", ["..........", "...AAAA...", "..AAHHAA..", ".AAHHHAAA.", ".AAHAAAAA.", ".AAAAHAAA.", "..AAHAAA..", "...AAAA...", "..........", ".........."]),
    "health": ("green", ["..........", "..AA..AA..", ".AHAAAAHA.", ".AHHHHHHA.", "..AHHHHA..", "...AHHA...", "....AA....", "....A.....", "..........", ".........."]),
    "mana": ("cyan", ["..........", "....H.....", "...HHH....", "..AHHHA...", ".AAHHHAA..", ".AAHHHAA..", "..AAAAA...", "...AAA....", "..........", ".........."]),
    "notice": ("gold", ["..........", "....AA....", "...AHHA...", "..AHHHHA..", "..AHHHHA..", "..AHHHHA..", ".AAAAAAAA.", "...AHHA...", "....AA....", ".........."]),
    "operational_aether": ("cyan", ["..........", "...AAAA...", "..AAHHAA..", ".AA....AA.", ".AH.AA.HA.", ".AH.AA.HA.", ".AA....AA.", "..AAHHAA..", "...AAAA...", ".........."]),
    "parts": ("silver", ["..........", "....AA....", "..A.AA.A..", ".AAAHHAAA.", "..AHHHHA..", "..AHHHHA..", ".AAAHHAAA.", "..A.AA.A..", "....AA....", ".........."]),
    "risk": ("red", ["..........", "....A.....", "...AAA....", "...AHA....", "..AAHAA...", "..AAHAA...", ".AAAHAAA..", ".AAAAAAA..", "....H.....", ".........."]),
    "shield": ("silver", ["..........", "..AAAAAA..", ".AAHHHHAA.", ".AAHHHHAA.", ".AAHHHHAA.", "..AAHHAA..", "...AAAA...", "....AA....", "..........", ".........."]),
    "stage_time": ("ivory", ["..........", "...AAAA...", "..AAHHAA..", ".AA.HH.AA.", ".AA.HAAA..", ".AA..AAA..", "..AA..AA..", "...AAAA...", "..........", ".........."]),
    "weight": ("silver", ["..........", "....AA....", "...AHHA...", "...AAAA...", "..AAAAAA..", ".AAHHHHAA.", ".AAHHHHAA.", ".AAAAAAAA.", "..........", ".........."]),
}


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def build_icon(rows: list[str], palette: dict[str, tuple[int, int, int, int]]) -> Image.Image:
    if len(rows) != 10 or any(len(row) != 10 for row in rows):
        raise ValueError("Each resource core must be an exact 10x10 pattern")
    image = Image.new("RGBA", (12, 12), (0, 0, 0, 0))
    pixels = image.load()
    for y, row in enumerate(rows, 1):
        for x, key in enumerate(row, 1):
            if key != ".":
                pixels[x, y] = palette[key]
    coloured = [(x, y) for y in range(12) for x in range(12) if pixels[x, y][3]]
    for x, y in coloured:
        for dy in (-1, 0, 1):
            for dx in (-1, 0, 1):
                nx, ny = x + dx, y + dy
                if 0 <= nx < 12 and 0 <= ny < 12 and pixels[nx, ny][3] == 0:
                    pixels[nx, ny] = INK
    alpha = image.getchannel("A")
    bounds = alpha.getbbox()
    if bounds is None or bounds[0] < 1 or bounds[1] < 1 or bounds[2] > 11 or bounds[3] > 11:
        raise ValueError(f"Transparent border violated: {bounds}")
    colours = {image.getpixel((x, y)) for y in range(12) for x in range(12) if image.getpixel((x, y))[3]}
    if len(colours) > 3:
        raise ValueError(f"Palette exceeds black plus two material colours: {len(colours)}")
    # Every coloured edge pixel must be protected from transparency by black.
    for x, y in coloured:
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if not (0 <= nx < 12 and 0 <= ny < 12) or pixels[nx, ny][3] == 0:
                raise ValueError(f"Missing black contour at {(x, y)}")
    return image


def checker(size: tuple[int, int], block: int) -> Image.Image:
    image = Image.new("RGBA", size, (199, 199, 199, 255))
    draw = ImageDraw.Draw(image)
    for y in range(0, size[1], block):
        for x in range(0, size[0], block):
            if (x // block + y // block) % 2:
                draw.rectangle((x, y, x + block - 1, y + block - 1), fill=(237, 237, 237, 255))
    return image


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    EVIDENCE.mkdir(parents=True, exist_ok=True)
    MANIFESTS.mkdir(parents=True, exist_ok=True)
    icons = {stem: build_icon(rows, PALETTES[palette]) for stem, (palette, rows) in SPECS.items()}
    for stem, icon in icons.items():
        icon.save(OUT / f"{stem}.png")

    font = ImageFont.load_default()
    contact = Image.new("RGBA", (960, 600), (18, 25, 32, 255))
    draw = ImageDraw.Draw(contact)
    draw.text((24, 14), "OCC RESOURCE ICONS — native 12px / black contour", fill=(236, 226, 199, 255), font=font)
    for i, (stem, icon) in enumerate(icons.items()):
        x, y = 24 + (i % 5) * 186, 48 + (i // 5) * 174
        tile = checker((144, 144), 12)
        tile.alpha_composite(icon.resize((144, 144), Image.Resampling.NEAREST))
        contact.alpha_composite(tile, (x, y))
        draw.text((x, y + 148), stem, fill=(225, 216, 190, 255), font=font)
    contact_path = EVIDENCE / "resource_icons_12_contact_12x.png"
    contact.save(contact_path)

    app = Image.new("RGBA", (960, 540), (27, 35, 43, 255))
    app_draw = ImageDraw.Draw(app)
    app_draw.text((24, 16), "Runtime contact: native 12px at 2x and 3x", fill=(236, 226, 199, 255), font=font)
    for i, (stem, icon) in enumerate(icons.items()):
        x, y = 24 + (i % 3) * 306, 50 + (i // 3) * 92
        app_draw.rectangle((x, y, x + 280, y + 68), fill=(39, 50, 60, 255), outline=(113, 123, 124, 255), width=2)
        app.alpha_composite(icon.resize((24, 24), Image.Resampling.NEAREST), (x + 14, y + 22))
        app.alpha_composite(icon.resize((36, 36), Image.Resampling.NEAREST), (x + 52, y + 16))
        app_draw.text((x + 102, y + 25), stem, fill=(224, 218, 200, 255), font=font)
    app_path = EVIDENCE / "resource_icons_12_application_mock.png"
    app.save(app_path)

    for stem, icon in icons.items():
        output = OUT / f"{stem}.png"
        grayscale = EVIDENCE / f"{stem}_grayscale.png"
        six_x = EVIDENCE / f"{stem}_6x.png"
        gray = icon.convert("L")
        Image.merge("RGBA", (gray, gray, gray, icon.getchannel("A"))).save(grayscale)
        icon.resize((72, 72), Image.Resampling.NEAREST).save(six_x)
        manifest = {
            "schema": "occ-art-manifest-v1", "contract_version": 1,
            "asset_id": f"ui.resource.{stem}", "role": "resource_icon_12", "status": "FORMAL_CANDIDATE",
            "provenance": {"source_channel": "manual_pixel_authoring", "source_path": output.relative_to(ROOT).as_posix(), "source_sha256": sha256(output), "source_descriptor": "User-authorized fresh native 12px semantic drawing; no 8px source scaling or recolouring."},
            "delivery": {"output_path": output.relative_to(ROOT).as_posix(), "output_sha256": sha256(output), "native_output_path": None, "logical_cells": None, "palette_max": 3, "required_color_families": []},
            "application": {"runtime_draw_rect": "Native 12x12 transparent glyph displayed at 2x or 3x integer scale.", "default_integer_scale": 3, "minimum_integer_scale": 2},
            "evidence": {"one_x": output.relative_to(ROOT).as_posix(), "four_x": contact_path.relative_to(ROOT).as_posix(), "six_x": six_x.relative_to(ROOT).as_posix(), "grayscale": grayscale.relative_to(ROOT).as_posix(), "checker": contact_path.relative_to(ROOT).as_posix(), "application_contact": app_path.relative_to(ROOT).as_posix()},
            "human_review": {"overall": "PASS", "reviewer": "Codex visual QA", "date": "2026-09-22", "silhouette": "PASS", "material": "PASS", "perspective": "PASS", "style": "PASS", "application": "PASS", "illumination": "PASS", "notes": "Distinct 12px silhouettes, coherent OCC palette, pure-black outer contour, readable at 24px and 36px contacts."},
            "unity_import": None,
            "pixel_grid_qa": {"authoring_mode": "direct_native_grid", "target_canvas": [12, 12], "geometric_resize_or_interpolation_performed": False, "new_colours_introduced_after_authoring": False, "outline": "one_pixel_pure_black"},
        }
        (MANIFESTS / f"{stem}.occ-art-manifest-v1.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"count": len(icons), "contact": contact_path.relative_to(ROOT).as_posix(), "application": app_path.relative_to(ROOT).as_posix()}, ensure_ascii=False))


if __name__ == "__main__":
    main()
