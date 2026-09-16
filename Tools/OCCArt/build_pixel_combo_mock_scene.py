#!/usr/bin/env python3
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
art = ROOT / "UnityProject/Assets/Game/Resources/Art/CombatTestArenaGround"
report = ROOT / "UnityProject/Reports/CombatTestArena"
slate = Image.open(art / "academy_test_ground_theme_slate_surface_64.png").convert("RGBA")
earth = Image.open(art / "academy_test_ground_theme_earth_surface_64.png").convert("RGBA")
hero = Image.open(report / "hero_pixel_candidate_64.png").convert("RGBA")
raider = Image.open(report / "raider_pixel_candidate_64.png").convert("RGBA")
shieldguard = Image.open(report / "shieldguard_pixel_candidate_64.png").convert("RGBA")
pyromancer = Image.open(report / "pyromancer_pixel_candidate_64.png").convert("RGBA")
crate = Image.open(art / "academy_test_book_crate_intact_64.png").convert("RGBA")
heavy = Image.open(art / "academy_test_heavy_cover_intact_64.png").convert("RGBA")

cols, rows, cell = 12, 7, 64
board = Image.new("RGBA", (cols * cell, rows * cell), (22, 22, 22, 255))
for y in range(rows):
    for x in range(cols):
        board.alpha_composite(slate if x < 6 else earth, (x * cell, y * cell))

def place(image: Image.Image, x: int, y: int) -> None:
    board.alpha_composite(image, (x * cell, y * cell))

place(hero, 2, 2)
place(raider, 9, 1)
place(raider, 10, 4)
place(shieldguard, 5, 1)
place(pyromancer, 6, 5)
place(crate, 4, 4)
place(heavy, 7, 3)
place(heavy, 8, 5)

out = report / "pixel_density_combo_mock_scene_v1.png"
out.parent.mkdir(parents=True, exist_ok=True)
board.save(out)
print(out)
