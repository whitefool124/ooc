# -*- coding: utf-8 -*-
"""从赛菲莉娅截图取 1:1 局部，用于人工比对材质与轮廓读法。

reference-only：这些裁切只用于观察，不进 Unity、不作为美术资产、不构成本项目素材库。
"""
import os
from collections import Counter

from PIL import Image

REFS = os.path.join(os.environ["USERPROFILE"], "Videos", "NVIDIA", "Sephiria")
HERE = os.path.dirname(os.path.abspath(__file__))
STUDY = os.path.dirname(HERE)
OUT = os.path.join(STUDY, "refs")

CROPS = [
    ("house_wood_floor", "Sephiria Screenshot 2026.09.15 - 16.08.40.41.png", (700, 360, 1084, 576)),
    ("house_wall_trim", "Sephiria Screenshot 2026.09.15 - 16.08.40.41.png", (450, 90, 834, 306)),
    ("house_door_props", "Sephiria Screenshot 2026.09.15 - 16.08.40.41.png", (820, 560, 1204, 776)),
    ("dungeon_stone_wall", "Sephiria Screenshot 2026.09.15 - 16.05.45.98.png", (1180, 130, 1564, 346)),
    ("dungeon_torch_wall", "Sephiria Screenshot 2026.09.15 - 16.05.45.98.png", (560, 90, 944, 306)),
    ("dungeon_floor_rug", "Sephiria Screenshot 2026.09.15 - 16.05.45.98.png", (400, 380, 784, 596)),
    ("dungeon_corridor", "Sephiria Screenshot 2026.09.15 - 16.05.51.05.png", (560, 300, 944, 516)),
    ("field_a", "Sephiria Screenshot 2026.09.15 - 15.54.45.42.png", (600, 300, 984, 516)),
    ("field_b", "Sephiria Screenshot 2026.09.15 - 15.55.07.96.png", (600, 300, 984, 516)),
    ("field_c", "Sephiria Screenshot 2026.09.15 - 15.50.12.42.png", (600, 300, 984, 516)),
    ("field_d", "Sephiria Screenshot 2026.09.15 - 15.57.06.09.png", (600, 300, 984, 516)),
    ("field_e", "Sephiria Screenshot 2026.09.15 - 15.39.16.27.png", (600, 300, 984, 516)),
]


def main():
    os.makedirs(OUT, exist_ok=True)
    made = []
    for label, name, box in CROPS:
        path = os.path.join(REFS, name)
        if not os.path.exists(path):
            print("missing", name)
            continue
        with Image.open(path) as im:
            im = im.convert("RGB")
            box = (box[0], box[1], min(box[2], im.width), min(box[3], im.height))
            crop = im.crop(box)
        crop.save(os.path.join(OUT, label + ".png"))
        cnt = Counter(crop.getdata())
        top = ["#%02X%02X%02X" % c for c, _v in cnt.most_common(8)]
        made.append((label, box, len(cnt), top))
    for label, box, n, top in made:
        print("%-20s box=%s colors=%d top=%s" % (label, box, n, " ".join(top)))


if __name__ == "__main__":
    main()
