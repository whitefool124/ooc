# -*- coding: utf-8 -*-
"""从截图验证赛菲莉亚的光照实现方式（不猜，用数据）。

判据：
1) 像素画先按整数倍放大（原生 6 倍）。如果光照是"画进素材"的，放大后的 6×6 块
   内部必然完全一致（块内方差 = 0）；如果光照是**放大之后另算的一层**，亮处块内就会
   出现平滑变化（方差 > 0），暗处仍然为 0。
2) 沿一条扫描线看亮度变化：按 6px 台阶跳 = 画进素材；逐屏幕像素平滑变化 = 后加的光照层。

参考截图仅用于观察。
"""
import json
import os

from PIL import Image, ImageDraw, ImageFont

STUDY = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REFS = os.path.join(STUDY, "refs")
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "b16")
PITCH = 6
FONTS = [r"C:\Windows\Fonts\msyh.ttc", r"C:\Windows\Fonts\simhei.ttf", r"C:\Windows\Fonts\arial.ttf"]


def font(size):
    for p in FONTS:
        if os.path.exists(p):
            try:
                return ImageFont.truetype(p, size)
            except Exception:
                continue
    return ImageFont.load_default()


def block_variance(im, box):
    """区域内 6×6 宏像素块的块内方差均值（0 = 素材原样，>0 = 被后加的一层影响）。"""
    g = im.convert("L").crop(box)
    w, h = g.size
    px = g.load()
    total, n = 0.0, 0
    for by in range(0, h - PITCH + 1, PITCH):
        for bx in range(0, w - PITCH + 1, PITCH):
            vals = [px[bx + i, by + j] for i in range(PITCH) for j in range(PITCH)]
            m = sum(vals) / len(vals)
            total += sum((v - m) ** 2 for v in vals) / len(vals)
            n += 1
    return round(total / max(n, 1), 2)


def profile(im, y, x0, x1):
    g = im.convert("L")
    px = g.load()
    return [px[x, y] for x in range(x0, x1)]


def intra_block_diff(im, box):
    """块内相邻屏幕像素不相同的比例。0 = 素材原样（放大后块内必一致）；
    >0 = 有一层平滑的东西盖在上面，说明光照是放大后另加的。"""
    g = im.convert("L").crop(box)
    w, h = g.size
    px = g.load()
    diff = tot = 0
    for y in range(h):
        for x in range(w - 1):
            if x % PITCH == PITCH - 1:      # 跨宏像素边界，跳过
                continue
            tot += 1
            if px[x, y] != px[x + 1, y]:
                diff += 1
    return round(diff / max(tot, 1), 3)


def main():
    name = "Sephiria Screenshot 2026.09.15 - 16.05.45.98.png"
    with Image.open(os.path.join(os.environ["USERPROFILE"], "Videos", "NVIDIA", "Sephiria", name)) as im:
        shot = im.convert("RGB")

    # 火把中心约 (655,120) 与 (1040,120)；下方地毯暗区约 (900,600)；走廊暗区 (700,760)
    regions = {
        "torch_lit_floor": (900, 700, 1020, 760),
        "torch_lit_wall": (600, 60, 720, 120),
        "unlit_wall": (1420, 60, 1540, 120),
        "unlit_floor": (60, 700, 180, 760),
        "carpet_lit": (700, 380, 820, 440),
    }
    res = {k: {"block_variance": block_variance(shot, v),
               "intra_block_diff": intra_block_diff(shot, v),
               "mean_lum": round(sum(profile(shot, (v[1] + v[3]) // 2, v[0], v[2])) / (v[2] - v[0]), 1)}
           for k, v in regions.items()}

    # 扫描线：穿过火把光照区，看亮度是 6px 台阶还是逐像素平滑
    line_y = 250
    line = profile(shot, line_y, 980, 1120)
    steps = [abs(line[i + 1] - line[i]) for i in range(len(line) - 1)]
    res["scanline"] = {
        "y": line_y, "x0": 980, "x1": 1120,
        "values": line,
        "max_step_1px": max(steps),
        "max_step_over_6px": max(abs(line[i + PITCH] - line[i]) for i in range(len(line) - PITCH)),
    }

    # 光照颜色：同一面墙在火把旁 vs 远离火把
    def mean_rgb(box):
        c = shot.crop(box)
        px = list(c.getdata())
        return [round(sum(p[i] for p in px) / len(px)) for i in range(3)]
    res["color"] = {
        "wall_near_torch": mean_rgb((600, 60, 720, 120)),
        "wall_far_from_torch": mean_rgb((1420, 60, 1540, 120)),
    }

    with open(os.path.join(OUT, "lighting_analysis.json"), "w", encoding="utf-8") as fh:
        json.dump(res, fh, ensure_ascii=False, indent=1)

    # 证据图：左=1:1 裁切，右=扫描线亮度曲线
    crop = shot.crop((560, 60, 1160, 420))
    W, H = 1200, 420
    fig = Image.new("RGB", (W, H), (18, 18, 22))
    fig.paste(crop, (0, 40))
    d = ImageDraw.Draw(fig)
    d.text((6, 10), "参考截图 1:1（火把 + 墙 + 地面）", font=font(18), fill=(235, 235, 240))
    gx0, gy0, gw, gh = 620, 60, 560, 300
    d.rectangle([gx0, gy0, gx0 + gw, gy0 + gh], fill=(26, 26, 32))
    d.text((gx0, 40), "扫描线亮度（y=250, x=980..1120）", font=font(16), fill=(200, 200, 208))
    lo, hi = min(line), max(line)
    pts = [(gx0 + i * gw / len(line), gy0 + gh - (v - lo) / max(1, hi - lo) * gh) for i, v in enumerate(line)]
    d.line(pts, fill=(255, 210, 120), width=2)
    for i in range(0, len(line), PITCH):     # 每 6px 画一条细竖线：素材台阶位置
        x = gx0 + i * gw / len(line)
        d.line([(x, gy0), (x, gy0 + gh)], fill=(70, 70, 80))
    d.text((gx0, gy0 + gh + 6), "竖线 = 每 6 屏幕像素（一个原生像素）；曲线若平滑穿过竖线，说明光照是放大后另加的",
           font=font(14), fill=(170, 170, 178))
    fig.save(os.path.join(OUT, "lighting_evidence.png"))

    print(json.dumps({k: v for k, v in res.items() if k != "scanline"}, ensure_ascii=False, indent=1))
    print("scanline: max 1px step = %s, max 6px step = %s" % (res["scanline"]["max_step_1px"], res["scanline"]["max_step_over_6px"]))
    print("saved", os.path.join(OUT, "lighting_evidence.png"))


if __name__ == "__main__":
    main()
