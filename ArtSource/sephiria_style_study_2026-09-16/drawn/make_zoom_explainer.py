# -*- coding: utf-8 -*-
"""把"显示倍率"讲清楚的说明图（两张）。

之前那张对比图把四格都缩放到同宽，等于抹平了像素尺寸差别——本脚本不再缩放，
所有图块按 1:1 屏幕像素并排，并加文字标注。
"""
import importlib.util
import os

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
STUDY = os.path.dirname(HERE)
REFS = os.path.join(STUDY, "refs")
SPEC = importlib.util.spec_from_file_location("g3", os.path.join(HERE, "draw_ground_family_v3.py"))
g3 = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(g3)

CELL = 32
BG = (24, 24, 28)
FG = (232, 232, 236)
DIM = (150, 150, 158)

FONT_CANDIDATES = [
    r"C:\Windows\Fonts\msyh.ttc",
    r"C:\Windows\Fonts\msyhbd.ttc",
    r"C:\Windows\Fonts\simhei.ttf",
    r"C:\Windows\Fonts\arial.ttf",
]


def font(size):
    for path in FONT_CANDIDATES:
        if os.path.exists(path):
            try:
                return ImageFont.truetype(path, size)
            except Exception:
                continue
    return ImageFont.load_default()


def ref_tile(pitch_native=16, screen_px=6):
    """参考里一格地面：取原生像素后按它的屏幕倍率（6）放大，1:1 呈现。"""
    with Image.open(os.path.join(REFS, "native_dungeon_stone_wall.png")) as im:
        patch = im.convert("RGB").crop((0, 6, pitch_native, 6 + pitch_native))
    return patch.resize((pitch_native * screen_px, pitch_native * screen_px), Image.NEAREST)


def my_tile(zoom, name="stone_a_32.png"):
    with Image.open(os.path.join(HERE, name)) as im:
        t = im.convert("RGB")
    return t.resize((CELL * zoom, CELL * zoom), Image.NEAREST)


def panel(img, caption, sub, cap_font, sub_font, box_w, box_h):
    """固定格位：图在框内居中，说明文字画在框底部，保证并排时基线一致。"""
    out = Image.new("RGB", (box_w, box_h), BG)
    out.paste(img, ((box_w - img.width) // 2, (box_h - 62 - img.height) // 2))
    d = ImageDraw.Draw(out)
    d.text((6, box_h - 56), caption, font=cap_font, fill=FG)
    d.text((6, box_h - 30), sub, font=sub_font, fill=DIM)
    return out


def main():
    f18 = font(18)
    f15 = font(15)
    f24 = font(24)

    # 图一：一格地面在四种情况下的实际屏幕尺寸（1:1 并排，不缩放）
    items = [
        (ref_tile(16, 6), "赛菲莉娅 一格", "16px × 6 倍 = 96 屏幕像素"),
        (my_tile(2), "OCC 一格 @2 倍", "32px × 2 = 64 屏幕像素"),
        (my_tile(4), "OCC 一格 @4 倍", "32px × 4 = 128 屏幕像素"),
        (my_tile(6), "OCC 一格 @6 倍", "32px × 6 = 192 屏幕像素"),
    ]
    box_w, box_h, gap, top = 236, 268, 24, 46
    w = box_w * len(items) + gap * (len(items) - 1)
    sheet = Image.new("RGB", (w, top + box_h), BG)
    ImageDraw.Draw(sheet).text((0, 8), "同一块石砖地面格在屏幕上到底多大（1:1 像素，未缩放）", font=f24, fill=FG)
    for i, (img, cap, sub) in enumerate(items):
        sheet.paste(panel(img, cap, sub, f18, f15, box_w, box_h), (i * (box_w + gap), top))
    sheet.save(os.path.join(HERE, "explain_zoom_tile.png"))
    print("saved explain_zoom_tile.png", sheet.size)

    # 图二：同一战场地标（石台地左上角，格 8,2）在三档倍率下 1:1 截取，不缩放
    BOARD = (20, 12)
    VIEW = (16, 80, 1408, 768)
    LANDMARK = (8, 2)

    def landmark_crop(path, z, w=416, h=288):
        bw, bh = BOARD[0] * CELL * z, BOARD[1] * CELL * z
        off_x = (bw - VIEW[2]) / 2 if bw >= VIEW[2] else -(VIEW[2] - bw) / 2
        off_y = (bh - VIEW[3]) / 2 if bh >= VIEW[3] else -(VIEW[3] - bh) / 2
        lx = VIEW[0] + LANDMARK[0] * CELL * z - off_x
        ly = VIEW[1] + LANDMARK[1] * CELL * z - off_y
        with Image.open(path) as im:
            im = im.convert("RGB")
            x0 = int(max(0, min(im.width - w, lx - w * 0.28)))
            y0 = int(max(0, min(im.height - h, ly - h * 0.30)))
            return im.crop((x0, y0, x0 + w, y0 + h))

    scenes = []
    with Image.open(os.path.join(REFS, "dungeon_stone_wall.png")) as im:
        scenes.append((im.convert("RGB").crop((0, 0, 416, 288)), "参考：赛菲莉娅实机", "原生 320×180 放大 6 倍"))
    for zoom in (2, 4, 6):
        scenes.append((landmark_crop(os.path.join(HERE, "viewport_zoom%d_fine.png" % zoom), zoom),
                       "我方 %d 倍显示" % zoom, "1 像素 = %d 屏幕像素" % zoom))
    box_w, box_h, gap, top = 440, 358, 22, 46
    w = box_w * len(scenes) + gap * (len(scenes) - 1)
    sheet2 = Image.new("RGB", (w, top + box_h), BG)
    ImageDraw.Draw(sheet2).text((0, 8), "同一块战场区域在三种显示倍率下的实机观感（1:1 像素，未缩放）", font=f24, fill=FG)
    for i, (img, cap, sub) in enumerate(scenes):
        sheet2.paste(panel(img, cap, sub, f18, f15, box_w, box_h), (i * (box_w + gap), top))
    sheet2.save(os.path.join(HERE, "explain_zoom_scene.png"))
    print("saved explain_zoom_scene.png", sheet2.size)


if __name__ == "__main__":
    main()
