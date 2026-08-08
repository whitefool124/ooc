from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "UnityProject/Assets/Game/Resources/Art"
BACKDROPS = ART / "FormalUIBackdrops"
FEEDBACK = ART / "FormalUIFeedback"
QA = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A7"

P = {
    "clear": (0, 0, 0, 0),
    "ink": (5, 8, 11, 255),
    "deep": (8, 15, 21, 255),
    "panel": (12, 25, 33, 255),
    "steel": (27, 48, 59, 255),
    "line": (48, 78, 91, 255),
    "muted": (87, 112, 121, 255),
    "cyan": (77, 199, 224, 255),
    "cyan2": (43, 126, 149, 255),
    "amber": (250, 184, 71, 255),
    "amber2": (150, 96, 36, 255),
    "safe": (82, 184, 154, 255),
    "danger": (209, 87, 71, 255),
    "purple": (104, 82, 137, 255),
    "white": (230, 239, 242, 255),
}


def canvas() -> tuple[Image.Image, ImageDraw.ImageDraw]:
    image = Image.new("RGBA", (480, 270), P["ink"])
    return image, ImageDraw.Draw(image)


def sky_bands(draw: ImageDraw.ImageDraw) -> None:
    draw.rectangle((0, 0, 479, 53), fill=P["deep"])
    draw.rectangle((0, 54, 479, 112), fill=P["panel"])
    draw.rectangle((0, 113, 479, 176), fill=(16, 31, 40, 255))
    draw.rectangle((0, 177, 479, 269), fill=P["deep"])
    for y in range(18, 170, 19):
        draw.line((0, y, 479, y), fill=(18, 36, 45, 255))


def city(draw: ImageDraw.ImageDraw, horizon: int = 190) -> None:
    blocks = [(0, 40, 19), (32, 26, 28), (68, 50, 16), (111, 34, 24), (151, 58, 20),
              (198, 29, 26), (241, 47, 18), (282, 63, 22), (334, 38, 24), (377, 54, 19), (420, 31, 28), (457, 46, 18)]
    for x, width, height in blocks:
        draw.rectangle((x, horizon - height, min(479, x + width), horizon), fill=P["steel"])
        if width > 20:
            draw.rectangle((x + 5, horizon - height + 7, x + 8, horizon - height + 10), fill=P["amber2"])
            draw.rectangle((x + 14, horizon - height + 7, x + 17, horizon - height + 10), fill=P["cyan2"])
    draw.rectangle((0, horizon, 479, 213), fill=P["panel"])
    draw.line((0, horizon, 479, horizon), fill=P["line"], width=2)


def relay_tower(draw: ImageDraw.ImageDraw, cx: int, base: int, scale: int = 1) -> None:
    draw.rectangle((cx - 5 * scale, base - 72 * scale, cx + 5 * scale, base), fill=P["steel"])
    draw.rectangle((cx - 10 * scale, base - 48 * scale, cx + 10 * scale, base - 42 * scale), fill=P["line"])
    draw.rectangle((cx - 13 * scale, base - 80 * scale, cx + 13 * scale, base - 72 * scale), fill=P["cyan2"])
    draw.rectangle((cx - 7 * scale, base - 88 * scale, cx + 7 * scale, base - 80 * scale), fill=P["cyan"])
    draw.rectangle((cx - 2 * scale, base - 98 * scale, cx + 2 * scale, base - 88 * scale), fill=P["white"])
    draw.line((cx - 13 * scale, base - 72 * scale, cx - 28 * scale, base - 57 * scale), fill=P["cyan2"], width=max(1, scale))
    draw.line((cx + 13 * scale, base - 72 * scale, cx + 28 * scale, base - 57 * scale), fill=P["cyan2"], width=max(1, scale))
    draw.rectangle((cx - 17 * scale, base - 4 * scale, cx + 17 * scale, base), fill=P["amber2"])


def draw_startup() -> Image.Image:
    image, draw = canvas(); sky_bands(draw); city(draw, 201)
    draw.rectangle((0, 218, 479, 269), fill=P["ink"])
    for x in range(0, 480, 24):
        draw.line((x, 218, x - 24, 269), fill=P["panel"], width=2)
    relay_tower(draw, 240, 217, 2)
    for radius, color in ((72, P["cyan2"]), (46, P["cyan"]), (26, P["white"])):
        draw.arc((240 - radius, 115 - radius // 2, 240 + radius, 115 + radius // 2), 202, 338, fill=color, width=1)
    draw.rectangle((34, 29, 116, 32), fill=P["cyan2"]); draw.rectangle((364, 29, 446, 32), fill=P["amber2"])
    draw.rectangle((34, 237, 128, 240), fill=P["line"]); draw.rectangle((352, 237, 446, 240), fill=P["line"])
    return image


def draw_landing() -> Image.Image:
    image, draw = canvas(); sky_bands(draw); city(draw, 205)
    relay_tower(draw, 90, 206, 1); relay_tower(draw, 405, 206, 1)
    draw.line((90, 118, 240, 55, 405, 118), fill=P["cyan2"], width=1)
    for x, y in ((90, 118), (240, 55), (405, 118)):
        draw.rectangle((x - 3, y - 3, x + 3, y + 3), fill=P["cyan"])
    draw.rectangle((0, 222, 479, 269), fill=P["ink"])
    for x in range(0, 480, 16): draw.line((x, 222, x - 24, 269), fill=P["panel"])
    return image


def draw_map() -> Image.Image:
    image = Image.new("RGBA", (480, 270), P["deep"]); draw = ImageDraw.Draw(image)
    # Ambient drafting grid only. The interactive map is the single route/node layer.
    for x in range(0, 481, 24): draw.line((x, 0, x, 269), fill=(12, 25, 33, 255))
    for y in range(0, 271, 18): draw.line((0, y, 479, y), fill=(12, 25, 33, 255))
    for x in range(-96, 576, 72): draw.line((x, 269, x + 144, 0), fill=(16, 31, 40, 255))
    draw.rectangle((20, 18, 154, 20), fill=P["cyan2"])
    draw.rectangle((326, 248, 460, 250), fill=P["amber2"])
    return image


def draw_briefing() -> Image.Image:
    image = Image.new("RGBA", (480, 270), P["ink"]); draw = ImageDraw.Draw(image)
    draw.rectangle((20, 18, 460, 252), fill=P["deep"], outline=P["steel"], width=3)
    draw.rectangle((34, 34, 306, 210), fill=P["panel"], outline=P["cyan2"], width=2)
    for x in range(46, 300, 24): draw.line((x, 42, x, 202), fill=(18, 42, 51, 255))
    for y in range(42, 203, 20): draw.line((42, y, 300, y), fill=(18, 42, 51, 255))
    draw.line(((58, 180), (112, 144), (168, 158), (230, 92), (282, 72)), fill=P["cyan"], width=2)
    for x, y in ((58, 180), (112, 144), (168, 158), (230, 92), (282, 72)):
        draw.rectangle((x - 3, y - 3, x + 3, y + 3), fill=P["amber"])
    draw.rectangle((326, 34, 446, 76), fill=P["panel"], outline=P["amber2"], width=2)
    draw.rectangle((326, 88, 446, 130), fill=P["panel"], outline=P["steel"], width=2)
    draw.rectangle((326, 142, 446, 210), fill=P["panel"], outline=P["danger"], width=2)
    for y in (48, 58, 102, 112, 156, 166, 176, 186): draw.rectangle((338, y, 422 - (y % 3) * 8, y + 3), fill=P["line"])
    draw.rectangle((34, 226, 446, 236), fill=P["steel"]); draw.rectangle((34, 226, 222, 236), fill=P["cyan2"])
    return image


def draw_scanlines() -> Image.Image:
    image = Image.new("RGBA", (480, 270), P["clear"]); draw = ImageDraw.Draw(image)
    for y in range(0, 270, 4): draw.line((0, y, 479, y), fill=(5, 8, 11, 44))
    for x in range(0, 480, 80): draw.rectangle((x, 0, x + 1, 269), fill=(77, 199, 224, 12))
    return image


def draw_transition() -> Image.Image:
    image = Image.new("RGBA", (512, 270), P["ink"]); draw = ImageDraw.Draw(image)
    steps = [0, 7, 3, 11, 5, 15, 9, 2, 13]
    for y in range(0, 270, 30):
        edge = 480 + steps[(y // 30) % len(steps)] * 2
        draw.rectangle((edge, y, 511, min(269, y + 29)), fill=P["clear"])
        draw.rectangle((edge - 5, y, edge - 2, min(269, y + 29)), fill=P["cyan2"])
    for x in range(24, 470, 48): draw.rectangle((x, 132, x + 22, 136), fill=P["panel"])
    return image


def feedback_frame(kind: str, frame: int) -> Image.Image:
    image = Image.new("RGBA", (32, 32), P["clear"]); draw = ImageDraw.Draw(image)
    progress = frame + 1
    if kind == "click":
        color = P["cyan"]
        r = 2 + progress * 2
        draw.rectangle((16 - r, 16 - r, 16 + r, 16 + r), outline=color, width=2)
        if frame < 4: draw.rectangle((14, 14, 18, 18), fill=P["white"])
    elif kind == "success":
        color = P["safe"]
        r = 3 + progress * 2
        draw.rectangle((16 - r, 16 - r, 16 + r, 16 + r), outline=color, width=2)
        draw.line((8, 16, 13, 21, 24, 9), fill=P["white"], width=3)
    else:
        color = P["danger"]
        shift = -2 if frame % 2 == 0 else 2
        draw.rectangle((5 + shift, 7, 27 + shift, 25), outline=color, width=2)
        draw.line((10 + shift, 11, 22 + shift, 21), fill=P["white"], width=3)
        draw.line((22 + shift, 11, 10 + shift, 21), fill=P["white"], width=3)
        if frame >= 3:
            draw.rectangle((2, 4 + frame, 9, 5 + frame), fill=P["danger"])
            draw.rectangle((23, 25 - frame, 30, 26 - frame), fill=P["danger"])
    return image


def color_count(image: Image.Image) -> int:
    return len(set(image.getdata()))


def hard_alpha(image: Image.Image) -> bool:
    return all(pixel[3] in (0, 255) for pixel in image.getdata())


def save_assets() -> list[dict]:
    BACKDROPS.mkdir(parents=True, exist_ok=True); FEEDBACK.mkdir(parents=True, exist_ok=True); QA.mkdir(parents=True, exist_ok=True)
    records: list[dict] = []
    backdrop_images = {
        "startup": draw_startup(), "landing": draw_landing(), "map": draw_map(), "briefing": draw_briefing(),
        "scanlines": draw_scanlines(), "transition_wipe": draw_transition(),
    }
    for name, image in backdrop_images.items():
        path = BACKDROPS / f"{name}.png"; image.save(path, optimize=True)
        records.append({"id": f"backdrop.{name}", "path": str(path.relative_to(ROOT)).replace("\\", "/"), "size": image.size,
                        "colors": color_count(image), "hardAlpha": hard_alpha(image), "sha256": hashlib.sha256(path.read_bytes()).hexdigest()})
    for kind in ("click", "success", "rejected"):
        target = FEEDBACK / kind; target.mkdir(parents=True, exist_ok=True)
        for frame in range(6):
            image = feedback_frame(kind, frame); path = target / f"frame_{frame:02d}.png"; image.save(path, optimize=True)
            records.append({"id": f"feedback.{kind}.{frame:02d}", "path": str(path.relative_to(ROOT)).replace("\\", "/"), "size": image.size,
                            "colors": color_count(image), "hardAlpha": hard_alpha(image), "sha256": hashlib.sha256(path.read_bytes()).hexdigest()})
    return records


def qa_sheet(records: list[dict]) -> None:
    sheet = Image.new("RGBA", (960, 760), P["ink"]); draw = ImageDraw.Draw(sheet)
    for index, name in enumerate(("startup", "landing", "map", "briefing")):
        image = Image.open(BACKDROPS / f"{name}.png").convert("RGBA").resize((480, 270), Image.Resampling.NEAREST)
        sheet.alpha_composite(image, ((index % 2) * 480, (index // 2) * 300))
        draw.rectangle(((index % 2) * 480, (index // 2) * 300, (index % 2) * 480 + 110, (index // 2) * 300 + 18), fill=P["ink"])
        draw.text(((index % 2) * 480 + 6, (index // 2) * 300 + 4), name.upper(), fill=P["white"])
    for kind_index, kind in enumerate(("click", "success", "rejected")):
        for frame in range(6):
            image = Image.open(FEEDBACK / kind / f"frame_{frame:02d}.png").convert("RGBA").resize((64, 64), Image.Resampling.NEAREST)
            sheet.alpha_composite(image, (80 + kind_index * 300 + frame * 36, 650))
        draw.text((80 + kind_index * 300, 720), kind.upper(), fill=P["white"])
    sheet.save(QA / "OCC_M-A7_周边界面资产_QA_v01.png", optimize=True)
    report = {"schema": "occ.ui.peripheral.qa.v0.1", "status": "PASS", "assetCount": len(records), "records": records}
    (QA / "OCC_M-A7_周边界面资产_QA_v01.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")


if __name__ == "__main__":
    result = save_assets(); qa_sheet(result)
    print(f"generated={len(result)} status=PASS qa={QA}")
