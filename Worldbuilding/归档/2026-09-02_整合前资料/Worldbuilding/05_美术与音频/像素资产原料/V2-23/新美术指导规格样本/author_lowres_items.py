from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parent
OUT = ROOT / "Items32"
QA = ROOT / "QA"

# Each source is an authored 16x16 logical icon. It is expanded exactly 2x;
# no texture, no generated micro-detail, and every decision is inspectable.
COLORS = {
    ".": (0, 0, 0, 0),
    "K": (25, 29, 35, 255),
    "D": (56, 61, 70, 255),
    "M": (101, 108, 118, 255),
    "Y": (220, 183, 51, 255),
    "O": (202, 78, 37, 255),
    "C": (47, 205, 218, 255),
}

ITEMS = {
    # cap / pressure collar / body / yellow legal-safety band / status window
    "occ_fire_canister_v04": [
        "................", "................", "......KKKK......", ".....KOOOOK.....",
        ".....KOOOOK.....", "....KKDDDDKK....", "...KDDDDDDDDK...", "...KDDDDDDDDK...",
        "...KYYYYYYYYK...", "...KYYYYYYYYK...", "...KDDKODDKK....", "...KDDKODDKK....",
        "....KDDDDDDK....", ".....KDDDDK.....", "......KKKK......", "................",
    ],
    # square hard case / central cold-cyan discharge socket / simple service latch
    "occ_grid_disposer_v04": [
        "................", "..KKKKKKKKKKKK..", ".KDDDDDDDDDDDDK.", ".KDDDDDDDDDDDDK.",
        ".KDDKKKKKKKKDDK.", ".KDDKMMCCMMKDDK.", ".KDDKMC..CMKDDK.", ".KDDKMC..CMKDDK.",
        ".KDDKMC..CMKDDK.", ".KDDKMMCCMMKDDK.", ".KDDKKKKKKKKDDK.", ".KDDDDYYYYDDDDK.",
        ".KDDDDKYYKDDDDK.", ".KDDDDDDDDDDDDK.", "..KKKKKKKKKKKK..", "................",
    ],
    # low tripod: lens, sensor head, broad three-foot base; no mast or radio waves
    "occ_recon_beacon_v04": [
        "................", "................", "................", "......KKKK......",
        ".....KCCCK......", "......KCCK......", "......KMMK......", ".....KMMMMK.....",
        "....KMMYYMMK....", "...KMMMKKMMMK...", "..KMMMKKKKMMMK..", ".KMMMKK..KKMMMK.",
        "KMMMKK....KKMMMK", "KKKK........KKKK", "................", "................",
    ],
}


def check_rows(name: str, rows: list[str]) -> None:
    if len(rows) != 16 or any(len(row) != 16 for row in rows):
        raise ValueError(f"{name} must be 16 by 16")
    unknown = {token for row in rows for token in row} - COLORS.keys()
    if unknown:
        raise ValueError(f"{name} uses unknown colors: {unknown}")


def make_qa(image: Image.Image, name: str) -> None:
    scale = 12
    board = Image.new("RGB", (384, 384), (42, 48, 59))
    draw = ImageDraw.Draw(board)
    for y in range(0, 384, 48):
        for x in range(0, 384, 48):
            if (x // 48 + y // 48) % 2:
                draw.rectangle((x, y, x + 47, y + 47), fill=(54, 61, 74))
    enlarged = image.resize((384, 384), Image.Resampling.NEAREST)
    board.paste(enlarged, (0, 0), enlarged)
    for x in range(0, 385, 24):
        draw.line((x, 0, x, 384), fill=(255, 255, 255, 28))
    for y in range(0, 385, 24):
        draw.line((0, y, 384, y), fill=(255, 255, 255, 28))
    target = QA / name / "qa_16logical_2x.png"
    target.parent.mkdir(parents=True, exist_ok=True)
    board.save(target)


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    for name, rows in ITEMS.items():
        check_rows(name, rows)
        logical = Image.new("RGBA", (16, 16))
        logical.putdata([COLORS[c] for row in rows for c in row])
        output = logical.resize((32, 32), Image.Resampling.NEAREST)
        output.save(OUT / f"{name}.png")
        make_qa(output, name)


if __name__ == "__main__":
    main()
