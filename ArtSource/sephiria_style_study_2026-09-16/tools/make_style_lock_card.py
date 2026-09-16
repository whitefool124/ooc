# -*- coding: utf-8 -*-
"""一页式风格定案卡：定案画面 + 规格 + 调色板 + 规则要点。"""
import os

from PIL import Image, ImageDraw, ImageFont

STUDY = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PA = os.path.join(STUDY, "project_assets")
OUT = os.path.join(STUDY, "风格定案卡_v1.png")
FONTS = [r"C:\Windows\Fonts\msyh.ttc", r"C:\Windows\Fonts\msyhbd.ttc",
         r"C:\Windows\Fonts\simhei.ttf", r"C:\Windows\Fonts\arial.ttf"]

PALETTE = [
    ("泥土", ["#6E5430", "#7E6038", "#92713E", "#A17F44", "#B09155"]),
    ("石材", ["#3A3A42", "#4A4A54", "#565660", "#63636E", "#74747F", "#868A90"]),
    ("草", ["#345E42", "#40784E", "#4C905A", "#5AA868", "#70BC7A", "#88CC8E"]),
    ("木", ["#6E422C", "#83533B", "#9B6744", "#B98960", "#C79B76", "#D7B28C"]),
    ("地毯", ["#482220", "#562523", "#632826", "#6C2A28", "#7E3632", "#924840"]),
]
SPECS = [
    ("原生画布 / 放大", "320×180 逻辑 · 6 倍 → 1920×1080"),
    ("1 原生像素", "6 屏幕像素（与参考一致）"),
    ("战场视口 / 一屏", "1408×768 · 7×4 玩法格"),
    ("玩法格", "32×32 原生像素（物件与单位网格）"),
    ("地面美术", "16 px 子网格，每玩法格 2×2 张"),
    ("瓦片 / 墙 / 单位", "16×16（平色） / 16×24 / 32×64"),
    ("光照", "不画进素材；运行时 URP 2D Light2D"),
]
RULES = [
    "地面近乎平色，材质差异靠色相；砖层只在墙上",
    "材质 4–6 色，主色 ≥45%（通常 50–70%）",
    "16 PPU：砖距 4px、缝 1px；32 PPU：砖距 8px、缝 2px",
    "交界 2–4px 长阶梯 + 本材质暗色接触线，不用纯黑勾地面",
    "无抗锯齿 / 渐变 / 抖色 / 孤立单像素 / 每格烘焙边框",
    "摆放：底边中心对齐；允许上左右探出，不得越过占格底边",
    "层次：地面→叠加→墙→大结构→小道具→单位；层内按底边 y 升序",
    "素材自带描边保留；光与阴影不进素材",
]


def font(size, bold=False):
    order = FONTS if not bold else [r"C:\Windows\Fonts\msyhbd.ttc"] + FONTS
    for p in order:
        if os.path.exists(p):
            try:
                return ImageFont.truetype(p, size)
            except Exception:
                continue
    return ImageFont.load_default()


def hex2rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def main():
    W = 1920
    scene = Image.open(os.path.join(PA, "scene_A2_rules_1920x1080.png")).convert("RGB")
    sw, sh = 1320, 742
    scene = scene.resize((sw, sh), Image.LANCZOS)

    y = 0
    H = 92 + sh + 30 + 210 + 300 + 40
    card = Image.new("RGB", (W, H), (18, 18, 22))
    d = ImageDraw.Draw(card)

    # 标题
    d.rectangle([0, 0, W, 92], fill=(26, 26, 32))
    d.text((28, 16), "OCC 战场画面风格定案 v1", font=font(38, True), fill=(240, 240, 244))
    d.text((30, 60), "1920×1080 · 原生 320×180 放大 6 倍 · 地面 16 PPU + 物件/单位 32 PPU · 静态画面不加光照",
           font=font(20), fill=(168, 168, 178))
    y = 92

    # 定案画面
    d.text((30, y + 4), "定案画面（7×4 玩法格，无光照与暗区）", font=font(20, True), fill=(220, 220, 228))
    card.paste(scene, (30, y + 34))
    # 规格表在右侧
    sx = 30 + sw + 34
    d.text((sx, y + 4), "规格", font=font(20, True), fill=(220, 220, 228))
    ty = y + 38
    for k, v in SPECS:
        d.text((sx, ty), k, font=font(16), fill=(150, 150, 160))
        d.text((sx, ty + 20), v, font=font(17), fill=(232, 232, 238))
        ty += 50
    y += sh + 52

    # 调色板
    d.text((30, y), "调色板（材质的暗 → 亮）", font=font(20, True), fill=(220, 220, 228))
    y += 32
    for name, colors in PALETTE:
        d.text((30, y + 6), name, font=font(17), fill=(190, 190, 198))
        x = 110
        for c in colors:
            d.rectangle([x, y, x + 62, y + 40], fill=hex2rgb(c), outline=(70, 70, 78))
            d.text((x + 4, y + 44), c.upper(), font=font(13), fill=(160, 160, 170))
            x += 70
        y += 66
    y += 8

    # 规则
    d.text((30, y), "规则要点", font=font(20, True), fill=(220, 220, 228))
    y += 32
    for i, r in enumerate(RULES):
        d.text((30, y), "· " + r, font=font(17), fill=(206, 206, 214))
        y += 28

    card.save(OUT)
    print("saved", OUT, card.size)


if __name__ == "__main__":
    main()
