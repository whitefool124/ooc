#!/usr/bin/env python3
"""Create a nearest-neighbor board crop for pixel-density review."""
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
src = ROOT / "UnityProject/Reports/CombatTestArena/pixel_density_combo_v5_1920x1080.png"
dst = ROOT / "UnityProject/Reports/CombatTestArena/pixel_density_combo_v5_board_zoom_2x.png"

im = Image.open(src).convert("RGB")
crop = im.crop((320, 160, 1120, 780))
crop.resize((crop.width * 2, crop.height * 2), Image.Resampling.NEAREST).save(dst)
print(dst)
