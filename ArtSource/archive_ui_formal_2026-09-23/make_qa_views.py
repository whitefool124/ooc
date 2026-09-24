"""Make inspection views only; never changes the decoded art candidates."""

from pathlib import Path

from PIL import Image, ImageOps


ROOT = Path(__file__).resolve().parent
ASSETS = {
    "page_frame": "page_frame_02_pitch38_candidate.png",
    "primary_scroll": "primary_scroll_02_candidate.png",
    "nav_tab": "nav_tab_01_palette_candidate.png",
}


def checker(size: tuple[int, int], cell: int = 8) -> Image.Image:
    result = Image.new("RGBA", size)
    pixels = result.load()
    for y in range(size[1]):
        for x in range(size[0]):
            tone = 232 if ((x // cell + y // cell) & 1) == 0 else 196
            pixels[x, y] = (tone, tone, tone, 255)
    return result


def nine_slice(source: Image.Image, size: tuple[int, int], insets: tuple[int, int, int, int]) -> Image.Image:
    left, top, right, bottom = insets
    sw, sh = source.size
    dw, dh = size
    out = Image.new("RGBA", size)
    xs = [0, left, sw - right, sw]
    ys = [0, top, sh - bottom, sh]
    dx = [0, left, dw - right, dw]
    dy = [0, top, dh - bottom, dh]
    for row in range(3):
        for col in range(3):
            part = source.crop((xs[col], ys[row], xs[col + 1], ys[row + 1]))
            dest_size = (dx[col + 1] - dx[col], dy[row + 1] - dy[row])
            if part.size != dest_size:
                part = part.resize(dest_size, Image.Resampling.NEAREST)
            out.alpha_composite(part, (dx[col], dy[row]))
    return out


for name, filename in ASSETS.items():
    image = Image.open(ROOT / filename).convert("RGBA")
    image.save(ROOT / f"{name}_one_x.png")
    image.resize((image.width * 4, image.height * 4), Image.Resampling.NEAREST).save(
        ROOT / f"{name}_four_x.png"
    )
    gray = ImageOps.grayscale(image.convert("RGB"))
    Image.merge("RGBA", (gray, gray, gray, image.getchannel("A"))).save(ROOT / f"{name}_grayscale.png")
    background = checker(image.size)
    background.alpha_composite(image)
    background.resize((image.width * 4, image.height * 4), Image.Resampling.NEAREST).save(
        ROOT / f"{name}_checker.png"
    )

board = Image.new("RGBA", (960, 540), (238, 225, 195, 255))
frame = nine_slice(Image.open(ROOT / ASSETS["page_frame"]).convert("RGBA"), (480, 364), (8, 8, 8, 9))
board.alpha_composite(frame, (36, 58))
scroll = nine_slice(Image.open(ROOT / ASSETS["primary_scroll"]).convert("RGBA"), (352, 66), (18, 9, 18, 9))
board.alpha_composite(scroll, (556, 128))
board.alpha_composite(scroll, (556, 216))
tab = nine_slice(Image.open(ROOT / ASSETS["nav_tab"]).convert("RGBA"), (184, 54), (20, 9, 12, 9))
board.alpha_composite(tab, (556, 330))
board.alpha_composite(tab, (752, 330))
board.convert("RGB").save(ROOT / "qa_layout_mock_960x540.png")
