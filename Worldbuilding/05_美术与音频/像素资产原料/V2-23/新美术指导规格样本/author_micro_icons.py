from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parent
OUT = ROOT / "Icons16"

# These are authored semantic glyphs, not AI-shrunk miniature objects.
# Characters: . transparent, K charcoal outline, W warm white, G pale grey,
# C cyan, R rust red. Every glyph is exactly a 16x16 logical pixel canvas.
GLYPHS = {
    "occ_action_v03": (".KW", [
        "................", "................", "................", ".....KK.........",
        ".....KWWK.......", ".....KWWWWK.....", "..KKWWWWWWWWK...", "..KWWWWWWWWWWK..",
        "..KWWWWWWWWWWK..", "..KKWWWWWWWWK...", ".....KWWWWK.....", ".....KWWK.......",
        ".....KK.........", "................", "................", "................",
    ]),
    "occ_aether_v03": (".KGC", [
        "................", "................", ".......KK.......", "......KGGK......",
        ".....KGCCGK.....", ".....KGCCGK.....",
        ".....KGCCGK.....", ".....KGCCGK.....", ".....KGCCGK.....", ".....KGCCGK.....",
        ".....KGCCGK.....", ".....KGCCGK.....",
        "......KGGK......", ".......KK.......", "................", "................",
    ]),
    "occ_enemy_ranged_v03": (".KR", [
        "................", "................", ".......KK.......", ".......RR.......",
        ".......RR.......", ".....KKRRKK.....", ".....RR..RR.....", "..KKRR....RRKK..",
        "..KKRR....RRKK..", ".....RR..RR.....", ".....KKRRKK.....", ".......RR.......",
        ".......RR.......", ".......KK.......", "................", "................",
    ]),
}

COLORS = {".": (0, 0, 0, 0), "K": (24, 29, 36, 255), "W": (239, 231, 210, 255), "G": (151, 178, 186, 255), "C": (46, 211, 224, 255), "R": (177, 62, 38, 255)}


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for name, (_, rows) in GLYPHS.items():
        if len(rows) != 16 or any(len(row) != 16 for row in rows):
            raise ValueError(f"{name} must be exactly 16x16")
        image = Image.new("RGBA", (16, 16))
        image.putdata([COLORS[ch] for row in rows for ch in row])
        image.save(OUT / f"{name}.png")


if __name__ == "__main__":
    main()
