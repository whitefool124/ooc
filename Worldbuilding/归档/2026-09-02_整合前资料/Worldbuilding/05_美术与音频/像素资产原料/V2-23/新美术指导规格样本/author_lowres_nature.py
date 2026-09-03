from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parent
OUT = ROOT / "Items32"
QA = ROOT / "QA"

# Natural items use the locked 16x16 logical grid and a 2x nearest-neighbour
# export. Green is descriptive plant material here, not the HUD healing colour.
COLORS = {
    ".": (0, 0, 0, 0),
    "K": (25, 29, 35, 255),
    "D": (46, 61, 48, 255),
    "G": (83, 123, 75, 255),
    "L": (142, 177, 83, 255),
    "B": (105, 72, 41, 255),
    "R": (187, 63, 45, 255),
    "O": (222, 128, 43, 255),
    "Y": (229, 195, 72, 255),
}

NATURE = {
    # Player reading: a compact medicinal leaf tuft, gathered as a field herb.
    "occ_medic_leaf_tuft_v01": [
        "................", "................", ".......K........", "......KLGK......",
        "....KKLGGGK.....", "...KLLGGGGGK....", "....KGGGGGLK....", ".....KGGGK......",
        "......KGGK......", ".....KGBGK......", "....KGBBBGK.....", "...KBBBBBBGK....",
        "....KKBBBBK.....", "......KKKK......", "................", "................",
    ],
    # Player reading: an amber seed pod; leaf crown plus an oversized capsule.
    "occ_amber_seedpod_v01": [
        "................", "................", "......KKKK......", ".....KLGGLK.....",
        "......KGGK......", ".......KK.......", "......KOOOK.....", ".....KOYYYOK....",
        ".....KOYYYOK....", ".....KOYYYOK....", ".....KOYYYOK....", "......KOOOK.....",
        ".......KK.......", "................", "................", "................",
    ],
    # Player reading: one robust ration fruit, stem / red flesh / small warm shine.
    "occ_cinder_apple_v01": [
        "................", ".......KK.......", "......KGGK......", ".......KK.......",
        ".....KKRRKK.....", "....KRRRRRRK....", "...KRRRYRRRRK...", "...KRRRRRRRRK...",
        "...KRRRRRRRRK...", "....KRRRRRRK....", ".....KRRRRK.....", "......KRRK......",
        ".......KK.......", "................", "................", "................",
    ],
    # Player reading: a small three-berry cluster with leaves, suitable as a forage item.
    "occ_sourberry_cluster_v01": [
        "................", ".......KK.......", "......KLGK......", ".....KLGGGK.....",
        "......KGGK......", "....KK..KK......", "...KRRK.KRRK....", "...KRRK.KRRK....",
        "....KK...KK.....", ".....KRRK.......", ".....KRRK.......", "......KK........",
        "................", "................", "................", "................",
    ],
}


def export(name: str, rows: list[str]) -> None:
    if len(rows) != 16 or any(len(row) != 16 for row in rows):
        raise ValueError(f"{name} is not 16x16")
    logical = Image.new("RGBA", (16, 16))
    logical.putdata([COLORS[pixel] for row in rows for pixel in row])
    output = logical.resize((32, 32), Image.Resampling.NEAREST)
    output.save(OUT / f"{name}.png")


def overview() -> None:
    names = list(NATURE)
    board = Image.new("RGB", (1536, 420), (24, 28, 35))
    for i, name in enumerate(names):
        asset = Image.open(OUT / f"{name}.png").convert("RGBA")
        x0 = i * 384
        panel = Image.new("RGB", (352, 352), (41, 47, 57))
        draw = ImageDraw.Draw(panel)
        for y in range(0, 352, 44):
            for x in range(0, 352, 44):
                if (x // 44 + y // 44) % 2:
                    draw.rectangle((x, y, x + 43, y + 43), fill=(54, 61, 74))
        enlarged = asset.resize((352, 352), Image.Resampling.NEAREST)
        panel.paste(enlarged, (0, 0), enlarged)
        board.paste(panel, (x0 + 16, 0))
        label = ImageDraw.Draw(board)
        label.text((x0 + 16, 366), name, fill=(230, 234, 241))
        label.text((x0 + 16, 385), "16x16 logical -> 2x -> 32x32", fill=(154, 171, 190))
    target = QA / "nature_lowres_v01_overview.png"
    target.parent.mkdir(parents=True, exist_ok=True)
    board.save(target)


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    for name, rows in NATURE.items():
        export(name, rows)
    overview()


if __name__ == "__main__":
    main()
