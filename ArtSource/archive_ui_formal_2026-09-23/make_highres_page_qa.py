"""Inspection images for the generated archive page; does not alter delivery art."""

from pathlib import Path

from PIL import Image, ImageOps


folder = Path(__file__).resolve().parent
source = Image.open(folder / "archive_page_panel_v1_raw.png").convert("RGBA")
source.save(folder / "archive_page_panel_v1_one_x.png")

# A detail crop at 4x reveals edge/alpha defects without a giant full-sheet file.
detail = source.crop((0, 0, 384, 256))
detail.resize((1536, 1024), Image.Resampling.NEAREST).save(
    folder / "archive_page_panel_v1_four_x_corner.png"
)

gray = ImageOps.grayscale(source.convert("RGB"))
Image.merge("RGBA", (gray, gray, gray, source.getchannel("A"))).save(
    folder / "archive_page_panel_v1_grayscale.png"
)

checker = Image.new("RGBA", source.size, (225, 225, 225, 255))
pixels = checker.load()
for y in range(source.height):
    for x in range(source.width):
        shade = 225 if ((x // 32 + y // 32) & 1) == 0 else 185
        pixels[x, y] = (shade, shade, shade, 255)
checker.alpha_composite(source)
checker.save(folder / "archive_page_panel_v1_checker.png")
