"""Build review-only contact sheets from normalized battlefield prop assets."""

from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "normalized" / "battlefield_props_v09"
FLOORS = ROOT / "normalized" / "terrain_runtime_clean32_v07"
QA = ROOT / "QA" / "battlefield_props_v09" / "family"
QA.mkdir(parents=True, exist_ok=True)

FAMILIES = [
    ("academy_light_stone_bench", ("intact", "damaged", "rubble")),
    ("academy_light_planter", ("intact", "damaged", "rubble")),
    ("academy_heavy_archive_stack", ("intact", "damaged", "rubble")),
    ("academy_heavy_masonry_screen", ("intact", "damaged", "rubble")),
    ("academy_aether_pillar", ("intact", "damaged", "rubble")),
    ("academy_seal_plinth", ("intact", "damaged", "rubble")),
    ("academy_loot_chest", ("closed", "open", "empty")),
]


def floor(index):
    suffix = "abcd"[index % 4]
    return Image.open(FLOORS / f"academy_courtyard_{suffix}.png").convert("RGBA")


contact = Image.new("RGBA", (96, len(FAMILIES) * 32), (0, 0, 0, 0))
for row, (family, states) in enumerate(FAMILIES):
    for column, state in enumerate(states):
        contact.alpha_composite(floor(row + column), (column * 32, row * 32))
        prop = Image.open(ASSETS / f"{family}_{state}.png").convert("RGBA")
        contact.alpha_composite(prop, (column * 32, row * 32))

contact.save(QA / "battlefield_props_21_on_v07_1x.png")
contact.resize((384, len(FAMILIES) * 128), Image.Resampling.NEAREST).save(
    QA / "battlefield_props_21_on_v07_4x.png")
contact.convert("L").convert("RGBA").resize((384, len(FAMILIES) * 128), Image.Resampling.NEAREST).save(
    QA / "battlefield_props_21_on_v07_grayscale_4x.png")

scene = Image.new("RGBA", (12 * 32, 9 * 32), (0, 0, 0, 0))
for y in range(9):
    for x in range(12):
        scene.alpha_composite(floor(x * 3 + y * 5), (x * 32, y * 32))

placements = [
    (1, 2, "academy_light_stone_bench", "intact"),
    (4, 1, "academy_light_planter", "damaged"),
    (8, 2, "academy_heavy_archive_stack", "intact"),
    (10, 4, "academy_heavy_masonry_screen", "damaged"),
    (6, 4, "academy_aether_pillar", "intact"),
    (3, 6, "academy_seal_plinth", "rubble"),
    (8, 6, "academy_loot_chest", "closed"),
    (1, 7, "academy_light_planter", "rubble"),
    (10, 7, "academy_heavy_archive_stack", "rubble"),
]
for x, y, family, state in placements:
    scene.alpha_composite(Image.open(ASSETS / f"{family}_{state}.png").convert("RGBA"), (x * 32, y * 32))

scene.resize((768, 576), Image.Resampling.NEAREST).save(QA / "battlefield_props_v09_mixed_12x9_2x.png")
