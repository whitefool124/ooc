from pathlib import Path
import json

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "UnityProject/Assets/Game/Resources/Art/FormalIntentIcons16"
QA = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A10"

INK = (15, 22, 26, 255)
LIGHT = (226, 239, 232, 255)
CYAN = (70, 207, 210, 255)
AMBER = (245, 184, 66, 255)
RED = (233, 83, 67, 255)


def canvas():
    return Image.new("RGBA", (16, 16), (0, 0, 0, 0))


def attack():
    im = canvas()
    d = ImageDraw.Draw(im)
    # One-handed sword: a single hostile-red silhouette with blade, guard and short grip.
    d.polygon([(3, 10), (10, 3), (14, 1), (12, 5), (5, 12)], fill=RED)
    d.line([(2, 9), (6, 13)], fill=RED, width=2)
    d.line([(3, 12), (1, 14)], fill=RED, width=2)
    d.point((1, 15), fill=RED)
    return im


def cast():
    im = canvas()
    d = ImageDraw.Draw(im)
    d.polygon([(8, 1), (10, 6), (15, 8), (10, 10), (8, 15), (6, 10), (1, 8), (6, 6)], fill=INK)
    d.polygon([(8, 3), (9, 7), (13, 8), (9, 9), (8, 13), (7, 9), (3, 8), (7, 7)], fill=CYAN)
    d.point((8, 8), fill=LIGHT)
    return im


def move():
    im = canvas()
    d = ImageDraw.Draw(im)
    d.polygon([(2, 6), (9, 6), (9, 3), (15, 8), (9, 13), (9, 10), (2, 10)], fill=INK)
    d.polygon([(3, 7), (10, 7), (10, 5), (13, 8), (10, 11), (10, 9), (3, 9)], fill=CYAN)
    return im


def defend():
    im = canvas()
    d = ImageDraw.Draw(im)
    d.polygon([(8, 1), (14, 4), (13, 10), (8, 15), (3, 10), (2, 4)], fill=INK)
    d.polygon([(8, 3), (12, 5), (11, 9), (8, 12), (5, 9), (4, 5)], fill=CYAN)
    d.line([(8, 3), (8, 12)], fill=LIGHT, width=1)
    return im


def interact_destroy():
    im = canvas()
    d = ImageDraw.Draw(im)
    # Destruction: one compact blast silhouette, deliberately distinct from weapons and tools.
    d.polygon([
        (8, 1), (10, 5), (14, 3), (12, 7),
        (15, 8), (12, 9), (14, 13), (10, 11),
        (8, 15), (6, 11), (2, 13), (4, 9),
        (1, 8), (4, 7), (2, 3), (6, 5),
    ], fill=RED)
    return im


def stats(image):
    colors = image.getcolors(maxcolors=256) or []
    opaque = sum(count for count, color in colors if color[3] == 255)
    invalid_alpha = sum(count for count, color in colors if color[3] not in (0, 255))
    return {
        "width": image.width,
        "height": image.height,
        "opaque_pixels": opaque,
        "coverage": round(opaque / 256, 4),
        "solid_color_count": sum(1 for _, color in colors if color[3] == 255),
        "invalid_alpha_pixels": invalid_alpha,
        "pass": image.size == (16, 16) and invalid_alpha == 0 and 0.12 <= opaque / 256 <= 0.55,
    }


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    QA.mkdir(parents=True, exist_ok=True)
    icons = {
        "attack": attack(),
        "cast": cast(),
        "move": move(),
        "defend": defend(),
        "interact_destroy": interact_destroy(),
    }
    report = {"asset_set": "OCC_M-A10_enemy_intent_icons16_v02", "target_size": [16, 16], "icons": {}}
    sheet = Image.new("RGBA", (5 * 80, 112), (20, 27, 31, 255))
    for index, (name, image) in enumerate(icons.items()):
        image.save(OUT / f"{name}.png", optimize=True)
        report["icons"][name] = stats(image)
        preview = image.resize((64, 64), Image.Resampling.NEAREST)
        sheet.alpha_composite(preview, (index * 80 + 8, 8))
    report["pass_count"] = sum(1 for item in report["icons"].values() if item["pass"])
    report["total_count"] = len(icons)
    sheet.save(QA / "OCC_M-A10_敌人意图图标16_QA_v02.png", optimize=True)
    (QA / "OCC_M-A10_敌人意图图标16_QA_v02.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    if report["pass_count"] != report["total_count"]:
        raise SystemExit("Enemy intent icon QA failed")


if __name__ == "__main__":
    main()
