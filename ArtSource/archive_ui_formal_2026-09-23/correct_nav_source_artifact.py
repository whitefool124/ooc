"""Merge four stray red texels into an observed source brown medoid."""

from pathlib import Path

from PIL import Image


root = Path(__file__).resolve().parent
source = Image.open(root / "nav_tab_01_candidate.png").convert("RGBA")
red = (127, 0, 0, 255)
brown = (86, 57, 33, 255)
assert brown in set(source.get_flattened_data())
locations = [(x, y) for y in range(source.height) for x in range(source.width) if source.getpixel((x, y)) == red]
assert locations == [(10, 6), (11, 6), (12, 6), (13, 6)]
for location in locations:
    source.putpixel(location, brown)
source.save(root / "nav_tab_01_palette_candidate.png")
