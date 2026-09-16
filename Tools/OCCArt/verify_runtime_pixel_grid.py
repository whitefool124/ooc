# -*- coding: utf-8 -*-
"""实机像素栅格检查（FORMAL 门禁工具）。

判定截图是否为整数倍放大、倍率是多少、栅格是否与原点对齐，并可选地比较多个区域的
倍率是否一致（用于发现"地面和物件像素大小不同"这类问题）。

判据说明：
- 正确的倍率 p 会让 p×p 区块内部颜色完全一致（块内方差 = 0）。
- p 的所有约数同样满格（6 的约数 2、3 也满格），因此**真实倍率 = 一致率达标的最大 p**。
- 栅格偏移：真实像素边界应落在图像坐标 0, p, 2p… 若最佳偏移不是 (0,0)，说明存在
  半像素位移或画布原点未对齐。

用法：
  python verify_runtime_pixel_grid.py --capture shot.png --expect-tier 6 \
      [--region name:x0,y0,x1,y1 ...] [--json-out result.json] [--quiet]

退出码：0 = 通过；1 = 未通过（倍率不符或偏移非零）；2 = 输入错误。
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from PIL import Image

MAX_PITCH = 16
UNIFORM_THRESHOLD = 0.995
MIN_DISTINCT_COLORS = 3


def uniform_fraction(img: Image.Image, box: tuple[int, int, int, int], pitch: int, offset: tuple[int, int] = (0, 0)) -> tuple[float, int]:
    x0, y0, x1, y1 = box
    px = img.load()
    total = same = 0
    ox, oy = offset
    y = y0 + ((oy - y0) % pitch)
    while y + pitch <= y1:
        x = x0 + ((ox - x0) % pitch)
        while x + pitch <= x1:
            first = px[x, y]
            ok = True
            for dy in range(pitch):
                row_y = y + dy
                for dx in range(pitch):
                    if px[x + dx, row_y] != first:
                        ok = False
                        break
                if not ok:
                    break
            total += 1
            if ok:
                same += 1
            x += pitch
        y += pitch
    return ((same / total) if total else 0.0), total


def distinct_colors(img: Image.Image, box: tuple[int, int, int, int], cap: int = 4096) -> int:
    x0, y0, x1, y1 = box
    px = img.load()
    seen: set[tuple[int, int, int]] = set()
    for y in range(y0, y1):
        for x in range(x0, x1):
            seen.add(px[x, y])
            if len(seen) >= cap:
                return cap
    return len(seen)


def detect_pitch(img: Image.Image, box: tuple[int, int, int, int]) -> tuple[int, float]:
    """返回 (真实倍率, 该倍率下的一致率)；取一致率达标的最大 p。"""
    best_pitch, best_fraction = 1, 1.0
    for pitch in range(2, MAX_PITCH + 1):
        fraction, blocks = uniform_fraction(img, box, pitch)
        if blocks < 4:
            continue
        if fraction >= UNIFORM_THRESHOLD:
            best_pitch, best_fraction = pitch, fraction
    return best_pitch, best_fraction


def best_offset(img: Image.Image, box: tuple[int, int, int, int], pitch: int) -> tuple[tuple[int, int], float]:
    best = ((0, 0), -1.0)
    for oy in range(pitch):
        for ox in range(pitch):
            fraction, _ = uniform_fraction(img, box, pitch, (ox, oy))
            if fraction > best[1]:
                best = ((ox, oy), fraction)
    return best


def parse_region(text: str) -> tuple[str, tuple[int, int, int, int]]:
    name, _, coords = text.partition(":")
    values = [int(v) for v in coords.split(",")]
    if len(values) != 4:
        raise ValueError(f"region needs name:x0,y0,x1,y1, got {text!r}")
    return name, (values[0], values[1], values[2], values[3])


def main() -> int:
    parser = argparse.ArgumentParser(description="Verify the runtime native-pixel lattice of a capture")
    parser.add_argument("--capture", required=True, help="screenshot png of the running game")
    parser.add_argument("--expect-tier", type=int, default=6, help="expected screen px per native px (canonical 6)")
    parser.add_argument("--region", action="append", default=[], help="name:x0,y0,x1,y1 (repeatable)")
    parser.add_argument("--json-out", default=None, help="write the machine result here")
    parser.add_argument("--quiet", action="store_true")
    args = parser.parse_args()

    path = Path(args.capture)
    if not path.is_file():
        print(f"capture not found: {path}", file=sys.stderr)
        return 2

    img = Image.open(path).convert("RGB")
    width, height = img.size
    try:
        regions = [parse_region(r) for r in args.region] or [("whole", (16, 16, width - 16, height - 16))]
    except ValueError as error:
        print(f"bad --region value: {error}", file=sys.stderr)
        return 2

    per_region: dict[str, dict[str, object]] = {}
    pitches: set[int] = set()
    for name, box in regions:
        colors = distinct_colors(img, box)
        if colors < MIN_DISTINCT_COLORS:
            per_region[name] = {"pitch": None, "uniform_fraction": None, "note": "too few distinct colors to detect a lattice", "colors": colors}
            continue
        pitch, fraction = detect_pitch(img, box)
        offset, offset_fraction = best_offset(img, box, pitch)
        per_region[name] = {
            "pitch": pitch,
            "uniform_fraction": round(fraction, 4),
            "offset": list(offset),
            "offset_uniform_fraction": round(offset_fraction, 4),
            "colors": colors,
            "box": list(box),
        }
        pitches.add(pitch)

    detected = sorted(pitches)
    uniform_across_regions = len(detected) == 1
    primary = None
    if uniform_across_regions and detected:
        primary = detected[0]
    offset_ok = all(
        entry.get("offset") == [0, 0]
        for entry in per_region.values()
        if entry.get("offset") is not None
    )
    tier_ok = uniform_across_regions and primary == args.expect_tier
    passed = bool(tier_ok and offset_ok)

    result = {
        "schema": "occ-runtime-pixel-grid-v1",
        "capture": str(path).replace("\\", "/"),
        "capture_size": [width, height],
        "expected_tier": args.expect_tier,
        "detected_tiers": detected,
        "uniform_across_regions": uniform_across_regions,
        "lattice_offset_zero": offset_ok,
        "regions": per_region,
        "pass": passed,
    }

    if args.json_out:
        Path(args.json_out).write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")

    if not args.quiet:
        print(f"capture {width}x{height}  expected tier = {args.expect_tier}")
        for name, entry in per_region.items():
            if entry.get("pitch") is None:
                print(f"  {name:28s} {entry['note']}")
            else:
                print(f"  {name:28s} tier={entry['pitch']:>2}  uniform={entry['uniform_fraction']:.3%}  offset={tuple(entry['offset'])}")
        print(f"uniform across regions : {uniform_across_regions}")
        print(f"lattice offset zero    : {offset_ok}")
        print(f"RESULT                 : {'PASS' if passed else 'FAIL'}")

    return 0 if passed else 1


if __name__ == "__main__":
    raise SystemExit(main())
