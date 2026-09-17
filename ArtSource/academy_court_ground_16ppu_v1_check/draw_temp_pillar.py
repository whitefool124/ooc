# -*- coding: utf-8 -*-
"""临时占位：不可破坏地形柱体 32×32（铺满整格）。

只用现有墙系的两种石色（浅石帽 / 深石身），粗像素、硬边、无渐变无抖色。
用途：Codex 恢复前的临时素材。程序绘制，**不能作为正式资产**（正式资产须走生图流程）。
"""
import os
from collections import Counter
from PIL import Image, ImageDraw, ImageFont

ROOT = r"E:\数据库\OCC_Codex"
OUT = os.path.join(ROOT, "ArtSource", "temp_placeholder_permanent_pillar")
os.makedirs(OUT, exist_ok=True)
FONTS = [r"C:\Windows\Fonts\msyh.ttc", r"C:\Windows\Fonts\simhei.ttf"]

# 调色板：6 色（对齐现有学院墙系 academy_wall_straight 的两色语言）
EDGE = (0x33, 0x2A, 0x25)   # 格边（本材质暗色，非纯黑）
BODY_MID = (0x52, 0x44, 0x3C)   # 柱身侧面
BODY_DARK = (0x49, 0x3D, 0x36)  # 柱身正面
BODY_LIP = (0x5E, 0x4E, 0x44)   # 正面顶沿受光
TOP_BASE = (0xC7, 0xAF, 0x90)   # 顶面
TOP_LIGHT = (0xDE, 0xC6, 0xA2)  # 顶面受光（左上）
TOP_RIM = (0x8A, 0x7A, 0x66)    # 顶面暗沿（右下）

Z = 6


def font(size):
    for p in FONTS:
        if os.path.exists(p):
            try:
                return ImageFont.truetype(p, size)
            except Exception:
                continue
    return ImageFont.load_default()


def draw_pillar():
    im = Image.new("RGBA", (32, 32), EDGE + (255,))
    d = ImageDraw.Draw(im)

    def rect(x0, y0, x1, y1, c):
        d.rectangle([x0, y0, x1, y1], fill=c + (255,))

    rect(1, 1, 30, 30, BODY_MID)          # 柱身整体
    rect(1, 17, 30, 30, BODY_DARK)        # 正面（朝屏幕下方）
    rect(1, 16, 30, 16, BODY_MID)         # 正面顶沿受光，读出厚度
    rect(2, 2, 29, 15, TOP_BASE)          # 顶面
    rect(2, 2, 29, 2, TOP_LIGHT)          # 左上受光边
    rect(2, 2, 2, 15, TOP_LIGHT)
    rect(3, 15, 29, 15, TOP_RIM)          # 右下暗沿
    rect(29, 3, 29, 15, TOP_RIM)
    return im


def main():
    im = draw_pillar()
    p1 = os.path.join(OUT, "temp_permanent_pillar_32.png")
    im.save(p1)
    im.resize((32 * Z, 32 * Z), Image.NEAREST).save(os.path.join(OUT, "temp_permanent_pillar_32_at6x.png"))

    # 一堵墙：4 块连排 + 旁边两块地面方格，看接缝与对比
    tile = Image.open(os.path.join(ROOT, "UnityProject", "Assets", "Game", "Resources", "Art",
                                   "FormalAcademyIndependentFloors32", "academy_block_court_a.png")).convert("RGBA")
    from PIL import Image as I
    row = I.new("RGBA", (6 * 32, 32), (0, 0, 0, 0))
    for i in range(4):
        row.alpha_composite(im, (i * 32, 0))
    for i in range(4, 6):
        row.alpha_composite(tile, (i * 32, 0))
    row.resize((row.width * Z, row.height * Z), Image.NEAREST).convert("RGB") \
        .save(os.path.join(OUT, "temp_pillar_wall_run_6x.png"))

    # 3×3 全柱，检查四边是否无缝
    grid = I.new("RGBA", (3 * 32, 3 * 32), (0, 0, 0, 0))
    for y in range(3):
        for x in range(3):
            grid.alpha_composite(im, (x * 32, y * 32))
    grid.resize((grid.width * Z, grid.height * Z), Image.NEAREST).convert("RGB") \
        .save(os.path.join(OUT, "temp_pillar_3x3_6x.png"))

    # 自检
    px = list(im.getdata())
    colors = Counter(c[:3] for c in px)
    alpha = {c[3] for c in px}
    isolated = 0
    for y in range(32):
        for x in range(32):
            c = im.getpixel((x, y))
            if not any(0 <= x + dx < 32 and 0 <= y + dy < 32 and im.getpixel((x + dx, y + dy)) == c
                       for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                isolated += 1
    print("saved:", p1)
    print(f"尺寸 32x32 / Alpha {sorted(alpha)} / 色数 {len(colors)} / 孤立像素 {isolated}")
    for c, n in colors.most_common():
        print("   #%02X%02X%02X  %4.1f%%" % (c[0], c[1], c[2], 100.0 * n / 1024))


if __name__ == "__main__":
    main()
