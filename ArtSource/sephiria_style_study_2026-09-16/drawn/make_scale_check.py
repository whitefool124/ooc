# -*- coding: utf-8 -*-
"""比例尺对照：同一个"人站在墙边"的场景，在两种设定长度下与参考并排（1:1 不缩放）。

A：现有规格——逻辑格 32px、单位 64px 高、墙面砖距 16px、6 倍显示。
B：赛菲莉娅比例——逻辑格 16px、单位 16px 高、墙面砖距 8px、6 倍显示（用 32px 素材降采样 2 倍近似）。
参考：赛菲莉娅实机 1:1。
"""
import importlib.util
import os

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
STUDY = os.path.dirname(HERE)
REFS = os.path.join(STUDY, "refs")
SPEC = importlib.util.spec_from_file_location("gw", os.path.join(HERE, "draw_wall_v1.py"))
gw = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(gw)

PANEL = (512, 384)
BG = (24, 24, 28)
FG = (232, 232, 236)
DIM = (152, 152, 160)
FONTS = [r"C:\Windows\Fonts\msyh.ttc", r"C:\Windows\Fonts\simhei.ttf", r"C:\Windows\Fonts\arial.ttf"]


def font(size):
    for p in FONTS:
        if os.path.exists(p):
            try:
                return ImageFont.truetype(p, size)
            except Exception:
                continue
    return ImageFont.load_default()


def unit_block(h_native, w_native=32):
    """单位占位块：1px 黑边 + 深色填充，只用于判断比例。"""
    im = Image.new("RGBA", (w_native, h_native), (46, 50, 62, 255))
    px = im.load()
    for x in range(w_native):
        px[x, 0] = (20, 18, 24, 255)
        px[x, h_native - 1] = (20, 18, 24, 255)
    for y in range(h_native):
        px[0, y] = (20, 18, 24, 255)
        px[w_native - 1, y] = (20, 18, 24, 255)
    return im


def scene(zoom, downscale, unit_h, wall_cells, w_cells, h_cells):
    """拼一块场景：地面 + 墙 + 单位占位块；downscale=2 时把 32px 素材降到 16px 口径。"""
    floor_name = "stone_a_32.png"
    wall = gw.build(wall_cells, w_cells, h_cells, shadow=True, floor_name=floor_name, with_floor=True)
    if downscale != 1:
        wall = wall.resize((wall.width // downscale, wall.height // downscale), Image.NEAREST)
        unit_src = unit_block(unit_h * downscale, 32)
        unit = unit_src.resize((unit_src.width // downscale, unit_src.height // downscale), Image.NEAREST)
    else:
        unit = unit_block(unit_h, 32)
    cell_native = gw.CELL // downscale
    canvas_native = (w_cells * cell_native, h_cells * cell_native + unit_h + cell_native)
    comp = Image.new("RGBA", canvas_native, (0, 0, 0, 0))
    comp.alpha_composite(wall, (0, 0))
    ux = 1 * cell_native + 2
    uy = canvas_native[1] - unit_h
    comp.alpha_composite(unit, (ux, uy))
    return comp.resize((comp.width * zoom, comp.height * zoom), Image.NEAREST)


def panel(img, caption, sub, box=PANEL):
    out = Image.new("RGB", box, BG)
    w = min(box[0], img.width)
    crop = img.crop((0, 0, w, min(box[1], img.height)))
    out.paste(crop, ((box[0] - w) // 2, (box[1] - crop.height) // 2))
    d = ImageDraw.Draw(out)
    d.rectangle([0, box[1] - 46, box[0], box[1]], fill=(16, 16, 20))
    d.text((8, box[1] - 42), caption, font=font(18), fill=FG)
    d.text((8, box[1] - 22), sub, font=font(14), fill=DIM)
    return out


def main():
    # 参考：赛菲莉娅室内（角色 ≈ 96 屏幕像素高，砖 24 屏幕像素）
    with Image.open(os.path.join(REFS, "house_wood_floor.png")) as im:
        ref = im.convert("RGB")
    # A：现有规格 32px 格 / 64px 单位
    a = scene(6, 1, 64, {(0, 0), (1, 0), (1, 1)}, 2, 2)
    # B：赛菲莉娅比例 16px 格 / 16px 单位（32px 素材降采样 2 倍近似）
    b = scene(6, 2, 16, {(0, 0), (1, 0), (1, 1)}, 2, 2)

    panels = [
        panel(ref, "参考：赛菲莉娅实机 1:1", "角色约 96 屏幕像素高 · 砖 24 屏幕像素"),
        panel(a, "A 现有规格：格 32px · 单位 64px", "6 倍下单位 384 屏幕像素（比参考大 4 倍）"),
        panel(b, "B 赛菲莉娅比例：格 16px · 单位 16px", "6 倍下单位 96 屏幕像素（与参考同级）"),
    ]
    gap = 18
    w = PANEL[0] * len(panels) + gap * (len(panels) - 1)
    sheet = Image.new("RGB", (w, PANEL[1] + 44), BG)
    ImageDraw.Draw(sheet).text((0, 6), "比例尺对照：一个像素代表多长的世界（1:1 像素，未缩放）",
                               font=font(22), fill=FG)
    for i, p in enumerate(panels):
        sheet.paste(p, (i * (PANEL[0] + gap), 44))
    sheet.save(os.path.join(HERE, "scale_check.png"))
    print("saved scale_check.png", sheet.size)


if __name__ == "__main__":
    main()
