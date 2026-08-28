from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[5]
ARTIFACTS = ROOT / "UnityProject" / "Artifacts" / "Terrain39" / "NineMaps"
LEVELS = [
    "rail_patrol", "depot_wreck", "relay_raid",
    "signal_hub", "gatehouse", "transmission_tower",
    "elite_foundry", "core_approach", "core_finale",
]


def main() -> None:
    # Labels occupy a dedicated strip and never cover units, walls or map corners.
    contact = Image.new("RGB", (1536, 936), (18, 18, 17))
    draw = ImageDraw.Draw(contact)
    for index, level_id in enumerate(LEVELS):
        source = Image.open(ARTIFACTS / f"{level_id}_1920x1080.png").convert("RGB")
        source.resize((960, 540), Image.Resampling.LANCZOS).save(
            ARTIFACTS / f"{level_id}_960x540.png", optimize=True)
        x = (index % 3) * 512
        y = (index // 3) * 312
        draw.text((x + 10, y + 6), level_id, fill=(235, 229, 212))
        contact.paste(source.resize((512, 288), Image.Resampling.LANCZOS), (x, y + 24))
    contact.save(ARTIFACTS / "terrain39_nine_maps_contact.png", optimize=True)

    # 1:1 crops keep the four gameplay corners and central material junctions reviewable.
    detail = Image.new("RGB", (1440, 810), (18, 18, 17))
    crop_boxes = {
        "rail_patrol": (0, 80, 1440, 890),
        "depot_wreck": (0, 80, 1440, 890),
        "relay_raid": (0, 80, 1440, 890),
        "signal_hub": (0, 80, 1440, 890),
        "gatehouse": (0, 80, 1440, 890),
        "transmission_tower": (0, 80, 1440, 890),
        "elite_foundry": (0, 80, 1440, 890),
        "core_approach": (0, 80, 1440, 890),
        "core_finale": (0, 80, 1440, 890),
    }
    # A full-resolution battlefield-only evidence file is more useful than overlay labels.
    for level_id, box in crop_boxes.items():
        source = Image.open(ARTIFACTS / f"{level_id}_1920x1080.png").convert("RGB")
        source.crop(box).save(ARTIFACTS / f"{level_id}_battlefield_detail.png", optimize=True)


if __name__ == "__main__":
    main()
