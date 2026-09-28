"""Native-grid Academy low-wall study; the generated image is material reference only."""

from pathlib import Path
from PIL import Image, ImageDraw, ImageOps


ROOT = Path(__file__).resolve().parent
PROJECT = ROOT.parents[2]
FLOOR = PROJECT / "UnityProject/Assets/Game/Resources/Art/FormalAcademyIndependentFloors32/academy_block_court_a.png"
HEAVY = ROOT.parent / "processed/academy_heavy_training_barricade_intact_32x32.png"
LIGHT = ROOT.parent / "processed/academy_light_planter_cover_intact_32x32.png"

BLACK = (0, 0, 0, 255)
IVORY = (234, 218, 194, 255)
LIMESTONE = (207, 190, 168, 255)
ASH = (157, 149, 140, 255)
SHADE = (117, 111, 106, 255)
JOINT = (81, 77, 73, 255)


def rect(draw, xy, color):
    draw.rectangle(xy, fill=color)


def make_wall():
    image = Image.new("RGBA", (32, 64))
    d = ImageDraw.Draw(image)

    # A 42 px wall rises 10 px north of its cell. The cap steps inward only at
    # the exposed top corners; the body and foot fill the entire logical width.
    for y, left in ((22, 5), (23, 4), (24, 3), (25, 2), (26, 1)):
        rect(d, (left, y, 31 - left, y), LIMESTONE)
    rect(d, (0, 27, 31, 63), SHADE)

    # Broad warm-stone top plane and low rim. A few low-contrast colour planes
    # describe an imperfect slab without one-pixel noise or a painted shadow.
    rect(d, (3, 27, 28, 34), IVORY)
    rect(d, (5, 24, 26, 28), IVORY)
    rect(d, (8, 23, 23, 24), IVORY)
    rect(d, (2, 29, 7, 34), LIMESTONE)
    rect(d, (21, 30, 29, 34), LIMESTONE)
    rect(d, (7, 27, 21, 28), IVORY)
    rect(d, (1, 35, 30, 35), LIMESTONE)
    rect(d, (1, 36, 30, 37), ASH)
    rect(d, (2, 37, 28, 37), LIMESTONE)
    rect(d, (1, 38, 30, 38), SHADE)

    # Two staggered courses of unequal ashlar blocks. Narrow joints remain
    # quiet; the irregular face and subtle upper-left highlights carry volume.
    rect(d, (1, 39, 17, 49), ASH)
    rect(d, (19, 39, 30, 50), ASH)
    rect(d, (2, 39, 10, 40), LIMESTONE)
    rect(d, (20, 40, 27, 41), LIMESTONE)
    rect(d, (13, 44, 17, 48), SHADE)
    rect(d, (28, 45, 30, 49), SHADE)
    rect(d, (18, 39, 18, 50), SHADE)
    rect(d, (1, 50, 9, 50), SHADE)
    rect(d, (10, 51, 24, 51), SHADE)
    rect(d, (25, 50, 30, 50), SHADE)
    rect(d, (1, 51, 9, 59), ASH)
    rect(d, (10, 52, 24, 59), ASH)
    rect(d, (25, 51, 30, 59), ASH)
    rect(d, (2, 51, 6, 52), LIMESTONE)
    rect(d, (11, 52, 20, 53), LIMESTONE)
    rect(d, (26, 51, 28, 52), LIMESTONE)
    rect(d, (7, 55, 9, 59), SHADE)
    rect(d, (21, 56, 24, 59), SHADE)
    rect(d, (10, 53, 10, 59), SHADE)
    rect(d, (25, 52, 25, 59), SHADE)

    # Continuous, grounded toe; the outside black pixels replace the existing
    # silhouette ring without enlarging alpha.
    rect(d, (0, 60, 31, 63), SHADE)
    rect(d, (1, 60, 30, 60), LIMESTONE)
    rect(d, (2, 61, 18, 62), ASH)
    rect(d, (20, 61, 29, 62), ASH)
    rect(d, (19, 61, 19, 62), JOINT)

    alpha = image.getchannel("A")
    pixels = image.load()
    for y in range(22, 64):
        for x in range(32):
            if not alpha.getpixel((x, y)):
                continue
            if any(nx < 0 or nx >= 32 or ny < 0 or ny >= 64 or not alpha.getpixel((nx, ny))
                   for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1))):
                pixels[x, y] = BLACK
    return image


def main():
    wall = make_wall()
    assert wall.getchannel("A").getbbox() == (0, 22, 32, 64)
    assert len({p for p in wall.getdata() if p[3]}) == 6
    wall.save(ROOT / "academy_permanent_wall_isolated_master_32x64.png")
    wall.resize((128, 256), Image.Resampling.NEAREST).save(ROOT / "isolated_master_4x.png")
    wall.resize((192, 384), Image.Resampling.NEAREST).save(ROOT / "isolated_master_6x.png")
    ImageOps.grayscale(wall).save(ROOT / "isolated_master_grayscale.png")
    checker = Image.new("RGBA", (32, 64))
    cd = ImageDraw.Draw(checker)
    for y in range(0, 64, 8):
        for x in range(0, 32, 8):
            rect(cd, (x, y, x + 7, y + 7), (205, 205, 205, 255) if (x // 8 + y // 8) % 2 else (245, 245, 245, 255))
    checker.alpha_composite(wall)
    checker.resize((128, 256), Image.Resampling.NEAREST).save(ROOT / "isolated_master_checker_4x.png")

    floor = Image.open(FLOOR).convert("RGBA")
    board = Image.new("RGBA", (6 * 32, 4 * 32))
    for y in range(4):
        for x in range(6):
            board.alpha_composite(floor, (x * 32, y * 32))
    board.alpha_composite(wall, (2 * 32, 0))
    board.alpha_composite(Image.open(HEAVY).convert("RGBA"), (1 * 32, 2 * 32))
    board.alpha_composite(Image.open(LIGHT).convert("RGBA"), (4 * 32, 2 * 32))
    board.save(ROOT / "isolated_master_contact_1x.png")
    board.resize((board.width * 6, board.height * 6), Image.Resampling.NEAREST).save(ROOT / "isolated_master_contact_6x.png")


if __name__ == "__main__":
    main()
