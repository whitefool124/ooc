#!/usr/bin/env python3
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
src = Image.open(ROOT / "UnityProject/Reports/CombatTestArena/raw_raider_pixel_v2.png").convert("RGBA")
alpha = src.getchannel("A").point(lambda v: 255 if v >= 128 else 0)
bbox = alpha.getbbox()
if bbox is None:
    raise SystemExit("candidate has no opaque silhouette")
crop = src.crop(bbox)
rgb = crop.convert("RGB").quantize(colors=10, method=Image.Quantize.FASTOCTREE).convert("RGB")
target = (44, 48)
fitted = rgb.resize(target, Image.Resampling.NEAREST).convert("RGBA")
fitted.putalpha(alpha.crop(bbox).resize(target, Image.Resampling.NEAREST))
canvas = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
canvas.alpha_composite(fitted, ((64-target[0])//2, 55-target[1]))
out = ROOT / "UnityProject/Reports/CombatTestArena/raider_pixel_candidate_64.png"
canvas.save(out)
print(out)
