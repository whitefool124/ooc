#!/usr/bin/env python3
"""Build a nearest-neighbor contact board for pixel-density review."""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
items = [
    ("slate ground", ROOT / "UnityProject/Assets/Game/Resources/Art/CombatTestArenaGround/academy_test_ground_theme_slate_surface_64.png"),
    ("earth ground", ROOT / "UnityProject/Assets/Game/Resources/Art/CombatTestArenaGround/academy_test_ground_theme_earth_surface_64.png"),
    ("book crate", ROOT / "UnityProject/Assets/Game/Resources/Art/CombatTestArenaGround/academy_test_book_crate_intact_64.png"),
    ("heavy cover", ROOT / "UnityProject/Assets/Game/Resources/Art/CombatTestArenaGround/academy_test_heavy_cover_intact_64.png"),
    ("raider unit", ROOT / "UnityProject/Assets/Game/Resources/Art/FormalEnemyAnimations64/raider/frame_00.png"),
    ("raider candidate", ROOT / "UnityProject/Reports/CombatTestArena/raider_pixel_candidate_64.png"),
    ("hero candidate", ROOT / "UnityProject/Reports/CombatTestArena/hero_pixel_candidate_64.png"),
    ("shieldguard candidate", ROOT / "UnityProject/Reports/CombatTestArena/shieldguard_pixel_candidate_64.png"),
    ("pyromancer candidate", ROOT / "UnityProject/Reports/CombatTestArena/pyromancer_pixel_candidate_64.png"),
]
panel = 320
board = Image.new("RGBA", (panel * len(items), panel), (28, 28, 28, 255))
draw = ImageDraw.Draw(board)
for i, (label, path) in enumerate(items):
    im = Image.open(path).convert("RGBA").resize((256, 256), Image.Resampling.NEAREST)
    x = i * panel + 32
    board.alpha_composite(im, (x, 24))
    draw.text((x, 286), label, fill=(235, 235, 225, 255))
out = ROOT / "UnityProject/Reports/CombatTestArena/pixel_density_asset_contact_v1.png"
out.parent.mkdir(parents=True, exist_ok=True)
board.convert("RGB").save(out)
print(out)
