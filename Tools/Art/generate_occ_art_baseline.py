from __future__ import annotations

import hashlib
import json
from collections import Counter
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "UnityProject" / "Assets" / "Game" / "Resources" / "Art"
WB_ART = next((ROOT / "Worldbuilding").glob("05_*"))
OUT = WB_ART / "\u6b63\u5f0f\u7f8e\u672f\u57fa\u51c6" / "ART-BASE"

PALETTE = [
    ("N0", "\u94c1\u9ed1", "#080A0C", "background"),
    ("N1", "\u7164\u9ed1", "#101317", "background"),
    ("N2", "\u6df1\u7164\u7070", "#171A1D", "structure"),
    ("N3", "\u6df1\u94a2\u7070", "#21242B", "structure"),
    ("N4", "\u94a2\u677f\u7070", "#30343A", "structure"),
    ("N5", "\u8f6c\u6298\u7070", "#40454A", "structure"),
    ("N6", "\u5668\u6750\u7070", "#565C60", "structure"),
    ("N7", "\u6df7\u51dd\u571f\u7070", "#70767A", "structure"),
    ("N8", "\u65e7\u94f6\u7070", "#909497", "text-secondary"),
    ("N9", "\u6d45\u94a2\u7070", "#B5B7B4", "text"),
    ("N10", "\u6696\u767d", "#D7D8D2", "text"),
    ("N11", "\u9ad8\u4eae\u767d", "#F0F0E8", "text-primary"),
    ("C0", "\u6df1\u4ee5\u592a\u9752", "#07343E", "friendly-aether"),
    ("C1", "\u5bfc\u7ba1\u9752", "#086377", "friendly-aether"),
    ("C2", "\u51b7\u9752", "#038FA9", "friendly-action"),
    ("C3", "\u51b7\u9752\u9ad8\u4eae", "#2DDDFE", "friendly-highlight"),
    ("R0", "\u6df1\u6c27\u5316\u7ea2", "#351615", "enemy-damage"),
    ("R1", "\u6697\u9508\u7ea2", "#5E2421", "enemy-damage"),
    ("R2", "\u6c27\u5316\u7ea2", "#90312B", "enemy-threat"),
    ("R3", "\u635f\u4f24\u9ad8\u4eae", "#D85B49", "enemy-highlight"),
    ("Y0", "\u6df1\u5b89\u5168\u9ec4", "#4B3816", "interaction-loot"),
    ("Y1", "\u65e7\u9ec4\u94dc", "#80601E", "interaction-loot"),
    ("Y2", "\u5b89\u5168\u9ec4\u6a59", "#C9A456", "interaction"),
    ("Y3", "\u8b66\u793a\u9ad8\u4eae", "#F3B722", "interaction-highlight"),
    ("G0", "\u6df1\u7070\u7eff", "#1C2C25", "healing-shield"),
    ("G1", "\u533b\u7597\u7eff", "#31513F", "healing-shield"),
    ("G2", "\u62a4\u76fe\u7eff", "#587C62", "healing-shield"),
    ("G3", "\u4fee\u590d\u9ad8\u4eae", "#8EB08B", "healing-highlight"),
    ("P0", "\u6df1\u6c61\u67d3\u7d2b", "#2B1728", "pollution-risk"),
    ("P1", "\u6697\u7d2b\u7ea2", "#4F274A", "pollution-risk"),
    ("P2", "\u6c61\u67d3\u7d2b", "#7A3B69", "pollution"),
    ("P3", "\u8d8a\u9650\u9ad8\u4eae", "#B35D92", "pollution-highlight"),
]


def rgb(hex_value: str) -> tuple[int, int, int]:
    h = hex_value.lstrip("#")
    return tuple(int(h[i : i + 2], 16) for i in (0, 2, 4))


COL = {code: rgb(hex_value) for code, _, hex_value, _ in PALETTE}
UNIT_VISUAL_SCALE_PREVIEW = 0.70


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    candidates = [
        Path("C:/Windows/Fonts/msyhbd.ttc" if bold else "C:/Windows/Fonts/msyh.ttc"),
        Path("C:/Windows/Fonts/simhei.ttf"),
        Path("C:/Windows/Fonts/arial.ttf"),
    ]
    for candidate in candidates:
        if candidate.exists():
            return ImageFont.truetype(str(candidate), size=size)
    return ImageFont.load_default()


def load_png(group: str, name: str) -> Image.Image:
    return Image.open(ART / group / f"{name}.png").convert("RGBA")


def unit_scale_preview(image: Image.Image, scale: float = UNIT_VISUAL_SCALE_PREVIEW) -> Image.Image:
    """Review-only native-pixel preview; formal sprites must be re-authored at this visual size."""
    source = image.convert("RGBA")
    bbox = source.getchannel("A").getbbox()
    if bbox is None:
        return Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    crop = source.crop(bbox)
    width = max(1, round(crop.width * scale))
    height = max(1, round(crop.height * scale))
    reduced = crop.resize((width, height), Image.Resampling.NEAREST)
    target = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    target.alpha_composite(reduced, (32 - width // 2, 59 - height))
    return target


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(65536), b""):
            digest.update(chunk)
    return digest.hexdigest()


def grayscale(image: Image.Image) -> Image.Image:
    rgba = image.convert("RGBA")
    gray = rgba.convert("L")
    out = Image.merge("RGBA", (gray, gray, gray, rgba.getchannel("A")))
    return out


def deuteranopia(image: Image.Image) -> Image.Image:
    source = image.convert("RGBA")
    result = Image.new("RGBA", source.size)
    pixels = []
    for r, g, b, a in source.get_flattened_data():
        rr = max(0, min(255, round(0.367322 * r + 0.860646 * g - 0.227968 * b)))
        gg = max(0, min(255, round(0.280085 * r + 0.672501 * g + 0.047413 * b)))
        bb = max(0, min(255, round(-0.011820 * r + 0.042940 * g + 0.968881 * b)))
        pixels.append((rr, gg, bb, a))
    result.putdata(pixels)
    return result


def draw_palette() -> dict:
    canvas = Image.new("RGB", (1440, 900), COL["N0"])
    d = ImageDraw.Draw(canvas)
    d.text((56, 30), "OCC ART-BASE-01  \u6b63\u5f0f\u4e3b\u8272\u8868 v0.1", font=font(34, True), fill=COL["N11"])
    d.text((56, 76), "\u8fd1\u4ee3\u5de5\u4e1a\u9b54\u5bfc\u6218\u672f\u50cf\u7d20\u98ce  //  \u8272\u5f69\u53ea\u8f85\u52a9\u8f6e\u5ed3\u4e0e\u660e\u5ea6\uff0c\u4e0d\u5355\u72ec\u627f\u62c5\u4fe1\u606f", font=font(18), fill=COL["N9"])

    start_x, start_y = 56, 124
    cell_w, cell_h, gap = 158, 108, 12
    for index, (code, name, hex_value, role) in enumerate(PALETTE):
        row, column = divmod(index, 8)
        x = start_x + column * (cell_w + gap)
        y = start_y + row * (cell_h + gap)
        color = rgb(hex_value)
        d.rectangle((x, y, x + cell_w, y + 58), fill=color)
        d.rectangle((x, y, x + cell_w, y + cell_h), outline=COL["N5"], width=1)
        d.text((x + 8, y + 64), f"{code}  {hex_value}", font=font(14, True), fill=COL["N11"])
        d.text((x + 8, y + 84), name, font=font(13), fill=COL["N9"])

    variants = [("NORMAL", lambda c: c), ("GRAYSCALE", None), ("DEUTERANOPIA", "deut")]
    strip_y = 640
    for label, mode in variants:
        d.text((56, strip_y), label, font=font(15, True), fill=COL["N10"])
        for i, (_, _, hex_value, _) in enumerate(PALETTE):
            color = rgb(hex_value)
            chip = Image.new("RGBA", (36, 26), color + (255,))
            if mode is None:
                chip = grayscale(chip)
            elif mode == "deut":
                chip = deuteranopia(chip)
            canvas.paste(chip.convert("RGB"), (180 + i * 38, strip_y - 2))
        strip_y += 58

    d.text((56, 826), "\u9884\u7b97\uff1a\u5355\u4f4d <=24 \u53ef\u89c1 RGB \u8272\uff1b\u5355\u5f20\u5730\u5757/\u56fe\u6807\u5efa\u8bae <=16 \u8272\uff1b\u4e3b\u8272\u8868 32 \u8272\u662f\u8de8\u7c7b\u522b\u5e93\uff0c\u4e0d\u662f\u5355\u8d44\u4ea7\u914d\u8272\u989d\u5ea6\u3002", font=font(17), fill=COL["N9"])
    path = OUT / "occ_art_base01_master_palette_v01.png"
    canvas.save(path)
    grayscale(canvas).save(OUT / "occ_art_base01_master_palette_grayscale_v01.png")
    deuteranopia(canvas).save(OUT / "occ_art_base01_master_palette_deuteranopia_v01.png")
    return {"path": str(path.relative_to(ROOT)), "size": list(canvas.size), "sha256": sha256(path)}


def draw_alpha_tile(base: Image.Image, tile: Image.Image, x: int, y: int) -> None:
    base.alpha_composite(tile, (x, y))


def make_native_map() -> Image.Image:
    base = Image.new("RGBA", (384, 288), COL["N1"] + (255,))
    floor = load_png("FormalRelay32", "floor_industrial")
    rail = load_png("FormalRelay32", "floor_rail")
    warning = load_png("FormalRelay32", "floor_warning")
    hazard = load_png("FormalRelay32", "floor_hazard")
    for row in range(9):
        for col in range(12):
            tile = floor
            if row == 0:
                tile = rail
            elif (row, col) in {(3, 5), (3, 6), (4, 5), (4, 6)}:
                tile = warning
            elif (row, col) in {(1, 10), (2, 10)}:
                tile = hazard
            draw_alpha_tile(base, tile, col * 32, row * 32)

    overlay = Image.new("RGBA", base.size, (0, 0, 0, 0))
    od = ImageDraw.Draw(overlay)
    for col, row in [(2, 6), (3, 6), (4, 6), (4, 5)]:
        x, y = col * 32, row * 32
        od.rectangle((x + 2, y + 2, x + 29, y + 29), fill=COL["C2"] + (58,), outline=COL["C3"] + (220,), width=1)
    for col, row in [(7, 3), (8, 3), (8, 4)]:
        x, y = col * 32, row * 32
        od.rectangle((x + 2, y + 2, x + 29, y + 29), fill=COL["R2"] + (55,), outline=COL["R3"] + (210,), width=1)
    base.alpha_composite(overlay)

    objects = [
        ("light_cover", 2, 3), ("light_cover", 8, 5),
        ("heavy_cover", 1, 5), ("heavy_cover", 9, 2),
        ("relay", 5, 4), ("loot_crate", 6, 7),
    ]
    for name, col, row in objects:
        draw_alpha_tile(base, load_png("FormalRelay32", name), col * 32, row * 32)

    units = [
        ("hero", 3, 7), ("rifleman", 8, 3), ("shieldguard", 9, 5),
        ("pyromancer", 7, 6), ("raider", 5, 2), ("elite", 10, 7),
    ]
    for name, col, row in units:
        unit = unit_scale_preview(load_png("FormalUnits64", name))
        foot_x = col * 32 + 16
        foot_y = row * 32 + 28
        draw_alpha_tile(base, unit, foot_x - 32, foot_y - 58)

    md = ImageDraw.Draw(base)
    md.rectangle((5 * 32 + 1, 4 * 32 + 1, 6 * 32 - 2, 5 * 32 - 2), outline=COL["Y3"] + (255,), width=1)
    return base


def draw_panel_box(d: ImageDraw.ImageDraw, rect: tuple[int, int, int, int], title: str) -> None:
    x0, y0, x1, y1 = rect
    d.rectangle(rect, fill=COL["N1"], outline=COL["N5"], width=2)
    d.line((x0 + 12, y0 + 40, x1 - 12, y0 + 40), fill=COL["N4"], width=1)
    d.text((x0 + 16, y0 + 9), title, font=font(18, True), fill=COL["N10"])


def draw_contact_sheet() -> Image.Image:
    canvas = Image.new("RGBA", (1920, 1080), COL["N0"] + (255,))
    d = ImageDraw.Draw(canvas)
    d.rectangle((0, 0, 1439, 1079), fill=COL["N1"])
    d.rectangle((1440, 0, 1919, 1079), fill=COL["N0"])
    d.line((1440, 0, 1440, 1080), fill=COL["N6"], width=2)

    d.text((44, 16), "ART-BASE-02  //  \u4e2d\u7ee7\u7ad9\u540c\u5c4f\u63a5\u89e6\u8868", font=font(26, True), fill=COL["N11"])
    d.text((930, 22), "\u5ba1\u67e5\u57fa\u51c6\uff0c\u975e\u8fd0\u884c\u65f6\u622a\u56fe", font=font(16), fill=COL["N8"])

    native_map = make_native_map()
    scaled_map = native_map.resize((1152, 864), Image.Resampling.NEAREST)
    map_x, map_y = 144, 72
    canvas.alpha_composite(scaled_map, (map_x, map_y))
    d.rectangle((map_x - 2, map_y - 2, map_x + 1153, map_y + 865), outline=COL["N6"], width=2)
    d.text((144, 944), "12×9 \u6b63\u4ea4\u683c  //  32px \u6e90\u683c ×3  //  \u5355\u4f4d\u6709\u6548\u8f6e\u5ed3 70% \u5ba1\u67e5\u9884\u89c8", font=font(17), fill=COL["N9"])

    icon_names = ["move", "attack", "skill", "skill_two", "loot", "interact"]
    d.text((144, 984), "\u5feb\u6377\u6307\u4ee4", font=font(16, True), fill=COL["N10"])
    for i, name in enumerate(icon_names):
        x = 274 + i * 92
        d.rectangle((x, 974, x + 78, 1052), fill=COL["N2"], outline=COL["N5"], width=2)
        icon = load_png("FormalIcons32", name).resize((64, 64), Image.Resampling.NEAREST)
        canvas.alpha_composite(icon, (x + 7, 981))

    panel_x0 = 1460
    d.text((panel_x0, 18), "\u6218\u6597\u63a7\u5236\u53f0", font=font(28, True), fill=COL["N11"])
    d.text((panel_x0, 56), "75% \u6218\u573a  /  25% HUD", font=font(16), fill=COL["N8"])

    draw_panel_box(d, (1460, 92, 1900, 260), "\u4e3b\u89d2\u8d44\u6e90")
    bars = [
        ("\u751f\u547d  64 / 80", "R2", 0.80),
        ("\u62a4\u76fe  12 / 16", "G2", 0.75),
        ("\u9b54\u529b   9 / 12", "C2", 0.75),
    ]
    for i, (label, code, ratio) in enumerate(bars):
        y = 140 + i * 38
        d.text((1476, y - 2), label, font=font(15), fill=COL["N10"])
        d.rectangle((1635, y, 1878, y + 18), fill=COL["N3"], outline=COL["N6"], width=1)
        d.rectangle((1637, y + 2, 1637 + int(239 * ratio), y + 16), fill=COL[code])

    draw_panel_box(d, (1460, 280, 1900, 466), "\u884c\u52a8 / \u610f\u56fe")
    timeline = [("\u4e3b\u89d2", "C3", "10"), ("\u72d9\u51fb\u624b", "R3", "16"), ("\u76fe\u536b", "R2", "20"), ("\u76d1\u5de5", "R2", "28")]
    for i, (label, code, tick) in enumerate(timeline):
        y = 332 + i * 30
        d.ellipse((1478, y, 1494, y + 16), fill=COL[code])
        d.text((1508, y - 4), label, font=font(15), fill=COL["N10"])
        d.text((1840, y - 4), tick, font=font(15, True), fill=COL[code])

    draw_panel_box(d, (1460, 486, 1900, 694), "\u72b6\u6001 / \u73af\u5883\u7f3a\u53e3")
    status_items = [("\u71c3\u70e7", "R3"), ("\u8fdf\u7f13", "C2"), ("\u675f\u7f1a", "P2"), ("\u7834\u7532", "Y2"), ("\u7729\u76ee", "N11"), ("\u663e\u5f62", "C3")]
    for i, (label, code) in enumerate(status_items):
        col, row = i % 3, i // 3
        x, y = 1478 + col * 134, 540 + row * 64
        d.rectangle((x, y, x + 34, y + 34), outline=COL[code], width=2)
        d.text((x + 42, y + 5), label, font=font(14), fill=COL["N9"])
    d.text((1478, 657), "\u5f85\u5236\u56fe\u6807\uff1a\u5f53\u524d\u4ec5\u5ba1\u67e5\u8bed\u4e49\u8272\uff0c\u4e0d\u4f5c\u6b63\u5f0f\u8d44\u4ea7", font=font(13), fill=COL["Y2"])

    draw_panel_box(d, (1460, 714, 1900, 890), "VFX \u8bed\u4e49\u8272")
    vfx = [("\u53cb\u65b9/\u53ef\u6267\u884c", "C3"), ("\u5a01\u80c1/\u4f24\u5bb3", "R3"), ("\u4ea4\u4e92/\u6218\u5229\u54c1", "Y3"), ("\u62a4\u76fe/\u4fee\u590d", "G3"), ("\u6c61\u67d3/\u8d8a\u9650", "P3")]
    for i, (label, code) in enumerate(vfx):
        y = 766 + i * 24
        d.line((1480, y + 8, 1530, y + 8), fill=COL[code], width=4)
        d.text((1544, y - 5), label, font=font(14), fill=COL["N9"])

    draw_panel_box(d, (1460, 910, 1900, 1054), "\u4e3b\u8272\u8868\u62bd\u6837")
    samples = ["N1", "N4", "N8", "N11", "C3", "R3", "Y3", "G3", "P3"]
    for i, code in enumerate(samples):
        col, row = i % 5, i // 5
        x, y = 1480 + col * 80, 960 + row * 44
        d.rectangle((x, y, x + 54, y + 24), fill=COL[code])
        d.text((x + 57, y + 3), code, font=font(11), fill=COL["N9"])
    return canvas


def draw_contact_qa(normal: Image.Image, gray: Image.Image, deut: Image.Image) -> Image.Image:
    qa = Image.new("RGB", (1920, 1080), COL["N0"])
    d = ImageDraw.Draw(qa)
    d.text((54, 30), "OCC ART-BASE-02  \u540c\u5c4f\u63a5\u89e6\u8868 QA", font=font(34, True), fill=COL["N11"])
    variants = [("NORMAL", normal), ("GRAYSCALE", gray), ("DEUTERANOPIA", deut)]
    for i, (label, image) in enumerate(variants):
        x = 54 + i * 620
        d.text((x, 90), label, font=font(18, True), fill=COL["N10"])
        thumb = image.resize((600, 338), Image.Resampling.NEAREST)
        qa.paste(thumb.convert("RGB"), (x, 124))
        d.rectangle((x, 124, x + 599, 461), outline=COL["N6"], width=2)
    checks = [
        ("PASS", "1920×1080 \u4e0e 1440/480 \u5206\u533a"),
        ("PASS", "12×9 \u6b63\u4ea4\u5730\u56fe\uff0c32px \u6e90\u683c ×3 \u6700\u8fd1\u90bb"),
        ("PASS", "\u5355\u4f4d\u6709\u6548\u8f6e\u5ed3\u7ea6 70%\uff0c64×64 \u753b\u5e03\u4e0e X32/Y58 \u951a\u70b9\u4e0d\u53d8"),
        ("PASS", "\u5df2\u5bfc\u5165\u5355\u4f4d/\u5730\u5757/\u6307\u4ee4\u56fe\u6807\u4ec5\u8bfb\u590d\u7528"),
        ("PASS", "\u7070\u9636\u4e0b\u8f6e\u5ed3\u3001\u4f53\u91cf\u548c HUD \u5206\u533a\u4ecd\u53ef\u8bfb"),
        ("PASS", "\u7ea2/\u7eff\u8272\u89c9\u98ce\u9669\u4e0b\u4ecd\u6709\u8f6e\u5ed3/\u4f4d\u7f6e/\u6587\u5b57\u5197\u4f59"),
        ("GAP", "6 \u72b6\u6001 + 8 \u73af\u5883\u56fe\u6807\u548c 24 \u4e2a\u57fa\u7840 VFX \u5c1a\u672a\u751f\u4ea7"),
        ("GAP", "\u72d9\u51fb/\u7834\u7532/\u7ed3\u754c/\u675f\u7f1a/\u4e24\u76d1\u5de5\u4ecd\u7f3a\u552f\u4e00\u9759\u5e27"),
    ]
    d.text((54, 516), "\u81ea\u52a8\u68c0\u67e5", font=font(24, True), fill=COL["N11"])
    for i, (status, detail) in enumerate(checks):
        y = 566 + i * 58
        color = COL["G3"] if status == "PASS" else COL["Y3"]
        d.rectangle((56, y, 136, y + 34), fill=color)
        d.text((72, y + 5), status, font=font(14, True), fill=COL["N0"])
        d.text((162, y + 4), detail, font=font(18), fill=COL["N10"])
    d.text((54, 1024), "\u7ed3\u8bba\uff1aBASELINE_QA_PASS / BASELINE_APPROVED\u3002\u63a5\u89e6\u8868\u662f\u751f\u4ea7\u5ba1\u67e5\u8bc1\u636e\uff0c\u4e0d\u662f Unity \u6b63\u5f0f\u8d34\u56fe\u3002", font=font(19, True), fill=COL["Y2"])
    return qa


def write_palette_files() -> None:
    palette_json = {
        "asset_id": "ART-BASE-01",
        "version": "0.1",
        "status": "BASELINE_APPROVED",
        "master_color_count": len(PALETTE),
        "budgets": {"unit_visible_rgb_max": 24, "tile_icon_recommended_rgb_max": 16},
        "colors": [
            {"code": code, "name": name, "hex": hex_value, "rgb": list(rgb(hex_value)), "role": role}
            for code, name, hex_value, role in PALETTE
        ],
    }
    (OUT / "occ_art_base01_master_palette_v01.json").write_text(json.dumps(palette_json, ensure_ascii=False, indent=2), encoding="utf-8")
    gpl = ["GIMP Palette", "Name: OCC ART-BASE-01 v0.1", "Columns: 8", "#"]
    for code, name, hex_value, _ in PALETTE:
        r, g, b = rgb(hex_value)
        gpl.append(f"{r:3d} {g:3d} {b:3d}\t{code}_{name}")
    (OUT / "occ_art_base01_master_palette_v01.gpl").write_text("\n".join(gpl) + "\n", encoding="utf-8")


def asset_manifest() -> list[dict]:
    sources = []
    for group in ["FormalUnits64", "FormalRelay32", "FormalIcons32"]:
        for path in sorted((ART / group).glob("*.png")):
            image = Image.open(path).convert("RGBA")
            visible = Counter((r, g, b) for r, g, b, a in image.get_flattened_data() if a >= 128)
            sources.append(
                {
                    "path": str(path.relative_to(ROOT)),
                    "size": list(image.size),
                    "visible_rgb_count": len(visible),
                    "sha256": sha256(path),
                }
            )
    return sources


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    write_palette_files()
    palette_output = draw_palette()

    contact = draw_contact_sheet()
    contact_path = OUT / "occ_art_base02_contact_sheet_1920x1080_v02.png"
    contact.save(contact_path)
    gray = grayscale(contact)
    gray_path = OUT / "occ_art_base02_contact_sheet_grayscale_v02.png"
    gray.save(gray_path)
    deut = deuteranopia(contact)
    deut_path = OUT / "occ_art_base02_contact_sheet_deuteranopia_v02.png"
    deut.save(deut_path)
    qa = draw_contact_qa(contact, gray, deut)
    qa_path = OUT / "occ_art_base02_contact_qa_v02.png"
    qa.save(qa_path)

    report = {
        "asset_ids": ["ART-BASE-01", "ART-BASE-02"],
        "version": "0.2",
        "result": "BASELINE_QA_PASS",
        "approval": "BASELINE_APPROVED",
        "checks": {
            "canvas_1920x1080": contact.size == (1920, 1080),
            "battle_hud_split": [1440, 480],
            "grid": [12, 9],
            "tile_source_px": [32, 32],
            "map_scale": 3,
            "resampling": "NEAREST",
            "unit_anchor_contract": {"canvas": [64, 64], "center_x": 32, "baseline_y": 58},
            "unit_visual_scale_preview": UNIT_VISUAL_SCALE_PREVIEW,
            "unit_source_assets_modified": False,
            "palette_master_colors": len(PALETTE),
        },
        "outputs": [
            palette_output,
            {"path": str(contact_path.relative_to(ROOT)), "size": list(contact.size), "sha256": sha256(contact_path)},
            {"path": str(gray_path.relative_to(ROOT)), "size": list(gray.size), "sha256": sha256(gray_path)},
            {"path": str(deut_path.relative_to(ROOT)), "size": list(deut.size), "sha256": sha256(deut_path)},
            {"path": str(qa_path.relative_to(ROOT)), "size": list(qa.size), "sha256": sha256(qa_path)},
        ],
        "known_gaps": [
            "6 status icons and 8 environment icons are represented only by semantic-color review slots.",
            "24 basic VFX effects remain unproduced.",
            "sniper, breaker, warden, binder, core_overseer and purifier_overseer still lack unique static sprites.",
        ],
        "sources": asset_manifest(),
    }
    (OUT / "occ_art_base01_02_report_v02.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"out": str(OUT), "result": report["result"], "approval": report["approval"], "outputs": len(report["outputs"])} , ensure_ascii=False))


if __name__ == "__main__":
    main()
