"""QA contact sheet for the six imported cover sprites; no asset pixels are changed."""

from pathlib import Path
from PIL import Image


ROOT = Path(__file__).resolve().parent
PROJECT = ROOT.parents[2]
PROCESSED = ROOT.parent / "processed"
FLOOR = PROJECT / "UnityProject/Assets/Game/Resources/Art/FormalAcademyIndependentFloors32/academy_block_court_a.png"
IMAGES = (
    "academy_light_planter_cover_intact_32x32.png",
    "academy_light_book_crate_intact_32x32.png",
    "academy_light_training_pad_intact_64x32.png",
    "academy_light_wood_bench_intact_64x32.png",
    "academy_heavy_training_barricade_intact_32x32.png",
    "academy_heavy_training_barricade_rubble_32x32.png",
)


def main():
    floor = Image.open(FLOOR).convert("RGBA")
    board = Image.new("RGBA", (12 * 32, 5 * 32))
    for y in range(5):
        for x in range(12):
            board.alpha_composite(floor, (x * 32, y * 32))
    for index, name in enumerate(IMAGES):
        sprite = Image.open(PROCESSED / name).convert("RGBA")
        row, col = divmod(index, 3)
        anchor_x = (2 + col * 4) * 32 + 16
        bottom = (2 + row * 2) * 32
        board.alpha_composite(sprite, (anchor_x - sprite.width // 2, bottom - sprite.height))
    board.save(ROOT / "cover_family_contact_1x.png")
    board.resize((board.width * 6, board.height * 6), Image.Resampling.NEAREST).save(
        ROOT / "cover_family_contact_6x.png")


if __name__ == "__main__":
    main()
