from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[3]
UNITY_DIR = ROOT / "UnityProject/Assets/Game/Resources/Art/FormalUnits64"
QA_DIR = Path(__file__).resolve().parent / "final_qa"
ASSETS = {
    "shieldguard": {"file": "shieldguard", "baseline": 58, "min_height": 32, "max_height": 46, "max_colors": 24},
    "pyromancer": {"file": "pyromancer", "baseline": 58, "min_height": 32, "max_height": 46, "max_colors": 24},
    "raider": {"file": "raider", "baseline": 58, "min_height": 32, "max_height": 46, "max_colors": 24},
    "elite_vanguard": {"file": "elite", "baseline": 58, "min_height": 32, "max_height": 46, "max_colors": 24},
    "sigil_mauler": {"file": "sigil_mauler", "baseline": 58, "min_height": 32, "max_height": 46, "max_colors": 24},
    "barrier_mender": {"file": "barrier_mender", "baseline": 58, "min_height": 32, "max_height": 46, "max_colors": 24},
    "tether_hound": {"file": "tether_hound", "baseline": 58, "min_height": 32, "max_height": 46, "max_colors": 24},
    "stone_snare": {"file": "stone_snare", "baseline": 58, "min_height": 32, "max_height": 46, "max_colors": 24},
    "lantern_revealer": {"file": "lantern_revealer", "baseline": 58, "min_height": 32, "max_height": 46, "max_colors": 24},
    "rune_arbalist": {"file": "rune_arbalist", "baseline": 58, "min_height": 32, "max_height": 46, "max_colors": 24},
}


def checkerboard(size: tuple[int, int]) -> Image.Image:
    image = Image.new("RGBA", size, (39, 43, 47, 255))
    draw = ImageDraw.Draw(image)
    for y in range(0, size[1], 4):
        for x in range(0, size[0], 4):
            if ((x // 4) + (y // 4)) % 2:
                draw.rectangle((x, y, x + 3, y + 3), fill=(67, 72, 76, 255))
    return image


def scaled_panel(sprite: Image.Image, mode: str, baseline: int) -> Image.Image:
    if mode == "grayscale":
        alpha = sprite.getchannel("A")
        view = sprite.convert("L").convert("RGBA")
        view.putalpha(alpha)
    elif mode == "silhouette":
        view = Image.new("RGBA", sprite.size, (0, 0, 0, 0))
        view.paste((235, 238, 232, 255), mask=sprite.getchannel("A"))
    else:
        view = sprite.copy()
    panel = checkerboard(sprite.size)
    panel.alpha_composite(view)
    draw = ImageDraw.Draw(panel)
    draw.line((32, 0, 32, 63), fill=(51, 221, 238, 255), width=1)
    draw.line((0, baseline, 63, baseline), fill=(255, 199, 55, 255), width=1)
    return panel.resize((256, 256), Image.Resampling.NEAREST)


def palette_panel(colors: list[tuple[int, int, int]], size: int = 256) -> Image.Image:
    panel = Image.new("RGBA", (size, size), (20, 22, 25, 255))
    draw = ImageDraw.Draw(panel)
    swatch = 40
    for index, color in enumerate(colors):
        x = 8 + (index % 6) * swatch
        y = 8 + (index // 6) * swatch
        draw.rectangle((x, y, x + 31, y + 31), fill=(*color, 255))
    return panel


def audit(asset_id: str, contract: dict[str, int]) -> dict[str, object]:
    path = UNITY_DIR / f"{contract.get('file', asset_id)}.png"
    sprite = Image.open(path).convert("RGBA")
    alpha_values = sorted(set(sprite.getchannel("A").get_flattened_data()))
    alpha_bbox = sprite.getchannel("A").getbbox()
    colors = sorted({pixel[:3] for pixel in sprite.get_flattened_data() if pixel[3] > 0})
    if alpha_bbox:
        left, top, right, bottom = alpha_bbox
        body_height = bottom - top
        bottom_pixel = bottom - 1
        center = (left + right - 1) / 2
    else:
        left = top = right = bottom = body_height = bottom_pixel = 0
        center = 0.0
    checks = {
        "size_64": sprite.size == (64, 64),
        "hard_alpha": set(alpha_values).issubset({0, 255}),
        "palette_limit": len(colors) <= contract["max_colors"],
        "body_height": contract["min_height"] <= body_height <= contract["max_height"],
        "baseline": contract["baseline"] - 1 <= bottom_pixel <= contract["baseline"] + 1,
        "center": 29 <= center <= 35,
        "transparent_corners": all(sprite.getpixel(position)[3] == 0 for position in ((0, 0), (63, 0), (0, 63), (63, 63))),
    }
    panels = [scaled_panel(sprite, mode, contract["baseline"]) for mode in ("color", "grayscale", "silhouette")]
    panels.append(palette_panel(colors))
    contact = Image.new("RGBA", (1024, 256), (20, 22, 25, 255))
    for index, panel in enumerate(panels):
        contact.alpha_composite(panel, (index * 256, 0))
    QA_DIR.mkdir(parents=True, exist_ok=True)
    contact.save(QA_DIR / f"{asset_id}_qa_4x.png")
    return {
        "asset_id": asset_id,
        "source": str(path.relative_to(ROOT)).replace("\\", "/"),
        "qa": str((QA_DIR / f"{asset_id}_qa_4x.png").relative_to(ROOT)).replace("\\", "/"),
        "size": list(sprite.size),
        "bounds": [left, top, right, bottom],
        "body_height": body_height,
        "bottom_pixel": bottom_pixel,
        "visual_center_x": center,
        "visible_rgb_colors": len(colors),
        "alpha_values": alpha_values,
        "checks": checks,
        "status": "PASS" if all(checks.values()) else "FAIL",
    }


def main() -> None:
    reports = [audit(asset_id, contract) for asset_id, contract in ASSETS.items()]
    summary = {"status": "PASS" if all(item["status"] == "PASS" for item in reports) else "FAIL", "assets": reports}
    QA_DIR.mkdir(parents=True, exist_ok=True)
    (QA_DIR / "enemy_pack_final_qa.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(summary, ensure_ascii=False, indent=2))
    if summary["status"] != "PASS":
        raise SystemExit(1)


if __name__ == "__main__":
    main()
