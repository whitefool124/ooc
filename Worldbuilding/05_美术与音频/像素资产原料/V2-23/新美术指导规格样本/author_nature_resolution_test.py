from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parent
OUT = ROOT / "Items32"
QA = ROOT / "QA"
PALETTE = {
    ".": (0, 0, 0, 0), "K": (24, 28, 34, 255), "D": (52, 63, 51, 255),
    "G": (86, 128, 76, 255), "L": (150, 184, 85, 255), "B": (106, 72, 43, 255),
    "R": (190, 61, 44, 255), "S": (223, 105, 53, 255), "Y": (234, 202, 80, 255),
}


def canvas(size: int) -> Image.Image:
    return Image.new("RGBA", (size, size), PALETTE["."])


def px(image: Image.Image, x: int, y: int, token: str) -> None:
    image.putpixel((x, y), PALETTE[token])


def filled_rect(image, box, token):
    ImageDraw.Draw(image).rectangle(box, fill=PALETTE[token])


def fruit(size: int) -> Image.Image:
    """One apple silhouette, rendered at native logical resolution."""
    image = canvas(size)
    s = size / 32
    def rect(a, b, c, d, color):
        filled_rect(image, tuple(round(v * s) for v in (a, b, c, d)), color)
    rect(15, 2, 17, 4, "K"); rect(15, 3, 17, 5, "G"); rect(17, 3, 21, 5, "K"); rect(18, 3, 22, 5, "L")
    rect(10, 6, 21, 7, "K"); rect(7, 8, 25, 10, "K"); rect(5, 11, 27, 20, "K"); rect(7, 21, 25, 23, "K"); rect(10, 24, 21, 26, "K"); rect(13, 27, 18, 28, "K")
    rect(11, 8, 20, 9, "S"); rect(8, 10, 24, 12, "R"); rect(7, 13, 25, 19, "R"); rect(9, 20, 23, 22, "R"); rect(11, 23, 20, 24, "R"); rect(14, 25, 17, 26, "R")
    rect(9, 12, 11, 18, "S"); rect(12, 10, 14, 12, "S"); rect(12, 11, 13, 12, "Y")
    return image


def herb(size: int) -> Image.Image:
    """One field herb silhouette: leaf crown, stem and root bundle."""
    image = canvas(size)
    s = size / 32
    def rect(a, b, c, d, color):
        filled_rect(image, tuple(round(v * s) for v in (a, b, c, d)), color)
    rect(14, 3, 17, 6, "K"); rect(11, 5, 20, 8, "K"); rect(8, 8, 23, 11, "K"); rect(5, 11, 26, 14, "K"); rect(9, 14, 23, 17, "K")
    rect(13, 17, 19, 23, "K"); rect(10, 22, 22, 26, "K"); rect(7, 25, 25, 28, "K")
    rect(14, 4, 16, 6, "L"); rect(12, 6, 19, 8, "G"); rect(9, 9, 22, 10, "L"); rect(6, 12, 24, 13, "G"); rect(10, 14, 21, 16, "G")
    rect(15, 17, 17, 22, "G"); rect(12, 22, 20, 24, "B"); rect(9, 25, 23, 27, "B"); rect(14, 23, 17, 25, "D")
    return image


def export(name: str, image: Image.Image) -> None:
    # 24px stays native and is centered within a 32px inventory canvas.
    final = canvas(32)
    offset = (32 - image.width) // 2
    final.alpha_composite(image, (offset, offset))
    final.save(OUT / f"{name}.png")


def make_board(entries):
    board = Image.new("RGB", (1536, 768), (24, 28, 35))
    for i, (name, image, kind) in enumerate(entries):
        x = (i % 2) * 768 + 16
        y = (i // 2) * 384
        panel = Image.new("RGB", (352, 352), (41, 47, 57))
        d = ImageDraw.Draw(panel)
        for yy in range(0, 352, 44):
            for xx in range(0, 352, 44):
                if (xx // 44 + yy // 44) % 2:
                    d.rectangle((xx, yy, xx + 43, yy + 43), fill=(54, 61, 74))
        big = image.resize((352, 352), Image.Resampling.NEAREST)
        panel.paste(big, (0, 0), big)
        board.paste(panel, (x, y))
        label = ImageDraw.Draw(board)
        label.text((x, y + 355), name, fill=(230, 234, 241))
        label.text((x, y + 373), kind, fill=(154, 171, 190))
    board.save(QA / "nature_resolution_test_overview.png")


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    generated = []
    for subject, draw_fn in (("fruit", fruit), ("herb", herb)):
        for logical in (24, 32):
            native = draw_fn(logical)
            name = f"occ_{subject}_{logical}logical_v01"
            export(name, native)
            final = Image.open(OUT / f"{name}.png").convert("RGBA")
            generated.append((name, final, f"{logical}x{logical} logical, native pixels in 32x32"))
    make_board(generated)


if __name__ == "__main__":
    main()
