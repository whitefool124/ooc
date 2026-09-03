from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[5]
ARTIFACTS = ROOT / "UnityProject" / "Artifacts" / "Terrain38" / "NineMaps"
LEVELS = [
    "rail_patrol",
    "depot_wreck",
    "relay_raid",
    "signal_hub",
    "gatehouse",
    "transmission_tower",
    "elite_foundry",
    "core_approach",
    "core_finale",
]
WINDOW_CAPTURED = {"rail_patrol", "depot_wreck", "relay_raid", "elite_foundry"}


def crop_game_view(window_path: Path) -> Image.Image:
    window = Image.open(window_path).convert("RGB")
    # Unity 1536x816 maximized-Game-View capture: discard editor chrome and status strip.
    scale_x = window.width / 1536.0
    scale_y = window.height / 816.0
    box = (
        round(177 * scale_x),
        round(130 * scale_y),
        round(1357 * scale_x),
        round(792 * scale_y),
    )
    return window.crop(box).resize((1920, 1080), Image.Resampling.NEAREST)


def main() -> None:
    for level_id in LEVELS:
        high_path = ARTIFACTS / f"{level_id}_1920x1080.png"
        low_path = ARTIFACTS / f"{level_id}_960x540.png"
        if level_id in WINDOW_CAPTURED:
            high = crop_game_view(ARTIFACTS / f"{level_id}_window.png")
            high.save(high_path, optimize=True)
            high.resize((960, 540), Image.Resampling.LANCZOS).save(low_path, optimize=True)
        else:
            # The 960x540 captures were accepted before the RenderTexture readback fault.
            low = Image.open(low_path).convert("RGB")
            low.resize((1920, 1080), Image.Resampling.NEAREST).save(high_path, optimize=True)

    contact = Image.new("RGB", (1440, 810), (14, 15, 15))
    draw = ImageDraw.Draw(contact)
    for index, level_id in enumerate(LEVELS):
        image = Image.open(ARTIFACTS / f"{level_id}_960x540.png").convert("RGB")
        tile = image.resize((480, 270), Image.Resampling.LANCZOS)
        x = (index % 3) * 480
        y = (index // 3) * 270
        contact.paste(tile, (x, y))
        draw.rectangle((x + 6, y + 6, x + 184, y + 30), fill=(25, 25, 23))
        draw.text((x + 12, y + 11), level_id, fill=(241, 234, 214))
    contact.save(ARTIFACTS / "terrain38_nine_maps_contact.png", optimize=True)


if __name__ == "__main__":
    main()
