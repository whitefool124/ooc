"""Read-only pitch diagnostics for the saved second-batch intent sources."""

from pathlib import Path
import sys

import numpy as np
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "Tools/OCCArt"))
from decode_generated_pixel_grid import edge_profile_score, remove_baked_checker_pixels  # noqa: E402

paths = sorted((ROOT / "ArtSource/intent_icon_reset_20260928/sources").glob("*.png"))
for path in paths:
    name = path.stem
    clean = remove_baked_checker_pixels(Image.open(path))
    bounds = clean.getchannel("A").getbbox()
    crop = clean.crop(bounds)
    smooth = np.asarray(crop.convert("RGB").filter(ImageFilter.GaussianBlur(1.4)), dtype=np.float32)
    alpha = np.asarray(crop.getchannel("A")) > 0
    edge_x = np.abs(smooth[:, 1:] - smooth[:, :-1]).sum(axis=2)
    edge_y = np.abs(smooth[1:] - smooth[:-1]).sum(axis=2)
    edge_x *= alpha[:, 1:] & alpha[:, :-1]
    edge_y *= alpha[1:] & alpha[:-1]
    profile_x = edge_x.sum(axis=0)
    profile_y = edge_y.sum(axis=1)
    scores = []
    for pitch in range(8, 101):
        score_x, phase_x, coverage_x = edge_profile_score(profile_x, pitch)
        score_y, phase_y, coverage_y = edge_profile_score(profile_y, pitch)
        score = score_x * score_y * min(coverage_x, coverage_y) ** 0.15
        scores.append((score, pitch, phase_x, phase_y))
    print(name, "bounds", (crop.width, crop.height), "top", sorted(scores, reverse=True)[:8])
