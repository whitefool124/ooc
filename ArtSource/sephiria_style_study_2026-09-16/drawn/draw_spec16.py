# -*- coding: utf-8 -*-
"""按赛菲莉亚实测规格（PPU 16）重出战场美术，并模拟其光照层。

实测依据（见 analysis/赛菲莉娅语言提炼_2026-09-16.md）：
- PPU = 16；瓦片 16×16；墙 16×24（探出所属格上方 8px）；角色约 18×19；桶 18×24、火把 12×28、灯 16×41。
- 地面近乎平色，颜色印象来自光照；光照是 URP 2D Light2D，在放大之后按屏幕分辨率叠加。

输出在 drawn/spec16/。状态：候选原料（ArtSource 阶段），不进 UnityProject。
"""
import importlib.util
import os

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
SPEC = importlib.util.spec_from_file_location("b16", os.path.join(HERE, "draw_battle16.py"))
b16 = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(b16)
OUT = os.path.join(HERE, "spec16")
os.makedirs(OUT, exist_ok=True)

PAL, OUTLINE = b16.PAL, b16.OUTLINE
CELL, ZOOM = 16, 6
CANVAS = (1920, 1080)
MAP_W = 1440
BOARD = (15, 11)
WALL_H = 24          # 墙 sprite 高度：格 16 + 8
FONTS = [r"C:\Windows\Fonts\msyh.ttc", r"C:\Windows\Fonts\simhei.ttf", r"C:\Windows\Fonts\arial.ttf"]


def font(size):
    for p in FONTS:
        if os.path.exists(p):
            try:
                return ImageFont.truetype(p, size)
            except Exception:
                continue
    return ImageFont.load_default()


def blank(w, h):
    return Image.new("RGBA", (w, h), (0, 0, 0, 0))


# --------------------------------------------------------------------------
# 墙：16×24，底边对齐所属格底边（向上探出 8px）
# --------------------------------------------------------------------------
def wall_h(seed=3):
    """横向墙（东西走向，正面朝南）：顶部 2px 受光，下面为砖层。"""
    p = PAL["stone"]
    im = blank(CELL, WALL_H)
    px = im.load()
    for y in range(WALL_H):
        for x in range(CELL):
            if y < 2:
                tone = "light"
            elif y < 4:
                tone = "face"
            elif y >= WALL_H - 2:
                tone = "seam"
            else:
                course = (y - 4) // 4
                rel = (y - 4) % 4
                bond = (course % 2) * 4
                if (x + bond) % 8 == 7:
                    tone = "shade"
                elif rel == 0:
                    tone = "light"
                elif rel == 3:
                    tone = "base"
                else:
                    tone = "face"
            px[x, y] = p[tone] + (255,)
    return b16.outline(im)


def wall_v(seed=5):
    """纵向墙（南北走向）：西侧 1px 受光（左上光源），其余为侧面。"""
    p = PAL["stone"]
    im = blank(CELL, WALL_H)
    px = im.load()
    for y in range(WALL_H):
        for x in range(CELL):
            if x == 0:
                tone = "light"
            elif x < 3:
                tone = "face"
            elif x == CELL - 1:
                tone = "seam"
            else:
                tone = "face" if (y % 8) < 6 else "base"
                if (y % 8) == 7:
                    tone = "shade"
            px[x, y] = p[tone] + (255,)
    return b16.outline(im)


# --------------------------------------------------------------------------
# 地面之上的阴影叠加（参考里是独立 sprite，不走灯光系统）
# --------------------------------------------------------------------------
def shadow_tile(w=CELL, h=CELL, strength=0.55):
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = im.load()
    for y in range(h):
        for x in range(w):
            d = min(x, w - 1 - x, y, h - 1 - y)
            a = int(255 * strength * (1 - d / max(1, (min(w, h) / 2))))
            if a > 0:
                px[x, y] = (12, 10, 16, a)
    return im


# --------------------------------------------------------------------------
# 道具（按实测尺寸）
# --------------------------------------------------------------------------
def crate():
    im = blank(16, 16)
    px = im.load()
    w = PAL["wood"]
    for x in range(2, 14):
        for y in range(4, 6):
            px[x, y] = w["light"] + (255,)
    for x in range(1, 15):
        for y in range(6, 15):
            px[x, y] = w["face"] + (255,)
    for y in (8, 12):
        for x in range(1, 15):
            px[x, y] = (128, 126, 136, 255)
            px[x, y + 1] = (78, 76, 86, 255)
    for x in range(1, 15):
        px[x, 14] = w["shade"] + (255,)
    return b16.outline(im)


def barrel():
    """桶 18×24（实测）：顶部受光、桶箍两道、桶身竖木条。"""
    im = blank(18, 24)
    px = im.load()
    w = PAL["wood"]
    for y in range(3, 23):
        half = 8 if y < 7 or y > 18 else 9
        for x in range(9 - half, 9 + half):
            if not (0 <= x < 18):
                continue
            tone = "light" if y < 6 else ("shade" if y > 20 else "face")
            px[x, y] = w[tone] + (255,)
    for x in range(1, 17):                      # 顶盖
        px[x, 2] = w["light"] + (255,)
        px[x, 3] = w["base"] + (255,)
    for y in (8, 9, 16, 17):                    # 桶箍
        for x in range(0, 18):
            if px[x, y][3]:
                px[x, y] = (128, 126, 136, 255) if y in (8, 16) else (78, 76, 86, 255)
    for x in range(3, 16, 4):                   # 竖木条
        for y in range(4, 22):
            if px[x, y][3] and px[x, y][:3] not in ((128, 126, 136), (78, 76, 86)):
                px[x, y] = w["base"] + (255,)
    return b16.outline(im)


def torch():
    """火把 12×28（实测 12–14×26–29）：木杆 + 缠布 + 火焰（光源本体）。"""
    im = blank(12, 28)
    px = im.load()
    w = PAL["wood"]
    for y in range(9, 27):
        for x in range(4, 8):
            px[x, y] = w["shade" if y > 22 else ("base" if x < 7 else "face")] + (255,)
    for y in range(6, 9):                       # 缠布/火盆
        for x in range(3, 9):
            px[x, y] = (128, 126, 136, 255) if y < 8 else (78, 76, 86, 255)
    flame = [(5, 4, (255, 240, 170)), (6, 4, (255, 240, 170)), (4, 5, (255, 206, 100)),
             (5, 5, (255, 220, 130)), (6, 5, (255, 220, 130)), (7, 5, (255, 206, 100)),
             (5, 3, (255, 186, 80)), (6, 3, (255, 186, 80)), (5, 2, (255, 140, 50)),
             (5, 1, (255, 110, 40)), (6, 2, (255, 140, 50))]
    for x, y, c in flame:
        px[x, y] = c + (255,)
    return b16.outline(im)


def chest():
    """宝箱 16×20。"""
    im = blank(16, 20)
    px = im.load()
    w = PAL["wood"]
    for x in range(1, 15):
        for y in range(6, 19):
            px[x, y] = w["face"] + (255,)
    for x in range(2, 14):                       # 箱盖
        for y in range(3, 6):
            px[x, y] = w["light"] + (255,)
    for x in range(1, 15):
        px[x, 6] = (128, 126, 136, 255)
        px[x, 7] = (78, 76, 86, 255)
        px[x, 18] = w["shade"] + (255,)
    for y in range(8, 13):                       # 锁扣
        for x in (7, 8):
            px[x, y] = (198, 170, 90, 255)
    return b16.outline(im)


# --------------------------------------------------------------------------
# 单位：画布 32×32，主体约 18×20，底部中心锚定
# --------------------------------------------------------------------------
def unit(canvas=(32, 32)):
    """侧视人形：深色衣 + 红围巾（与参考主角配色同级），主体 18×20。"""
    im = blank(*canvas)
    px = im.load()
    body = (72, 80, 104, 255)
    dark = (40, 44, 60, 255)
    skin = (232, 186, 150, 255)
    scarf = (196, 62, 54, 255)
    ox, oy = 7, 8                                # 主体左上，底边落在 y=28
    for x in range(6):                           # 头
        for y in range(5):
            px[ox + x, oy + y] = skin
    for x in range(6):
        px[ox + x, oy] = dark                    # 头发
        px[ox + x, oy + 1] = dark
    for x in range(8):                           # 躯干
        for y in range(5, 13):
            px[ox + x - 1, oy + y] = scarf if y < 8 else body
    for x in range(3):                           # 腿
        for y in range(13, 20):
            px[ox + x, oy + y] = dark
    for x in range(3):
        for y in range(13, 20):
            px[ox + 5 + x, oy + y] = dark
    return b16.outline(im)


# --------------------------------------------------------------------------
# 场景
# --------------------------------------------------------------------------
def grid_board():
    gw, gh = BOARD
    g = [["g"] * gw for _ in range(gh)]
    for y in range(3, 10):
        for x in range(1, 6):
            g[y][x] = "d"
    for y in range(2, 7):
        for x in range(7, 13):
            g[y][x] = "s"
    for i in range(3):
        for y in range(5 - i, 7):
            if 13 + i < gw:
                g[y][13 + i] = "s"
    g[4][9] = "d"
    for y in range(8, 11):
        for x in range(11, 14):
            g[y][x] = "w"
    for x in range(8, 11):
        g[8][x] = "c"
        g[9][x] = "c"
    return ["".join(r) for r in g]


WALL_CELLS = [(8, 2), (9, 2), (10, 2), (11, 2), (12, 2), (12, 3), (12, 4), (12, 5), (12, 6),
              (7, 2), (6, 2), (5, 3), (5, 4), (5, 5), (5, 6), (5, 7)]
H_WALL = [(8, 2), (9, 2), (10, 2), (11, 2), (12, 2), (7, 2), (6, 2)]
V_WALL = [(5, 3), (5, 4), (5, 5), (5, 6), (5, 7), (12, 3), (12, 4), (12, 5), (12, 6)]
PROPS = [("crate", 9, 6), ("crate", 8, 3), ("barrel", 10, 8), ("torch", 7, 8), ("chest", 11, 5)]
LIGHTS = [(7 * CELL + 6, 8 * CELL + 8), (10 * CELL + 9, 4 * CELL + 6)]   # 火把坐标（原生像素）


def compose_native():
    """原生分辨率场景（240×176），返回 RGB 图 + 光源坐标。"""
    grid = grid_board()
    board = b16.render_board(grid).convert("RGBA")
    base = board
    # 草丛
    for gy in range(len(grid)):
        for gx in range(len(grid[0])):
            if grid[gy][gx] != "g" or b16.r01("clump", gx // 3, gy // 3) > 0.5 or b16.r01("t2", gx, gy) > 0.55:
                continue
            p = PAL["grass"]
            for x, y, tone in ((5, 5, "shade"), (6, 5, "shade"), (5, 6, "shade"), (6, 6, "base"), (6, 4, "light")):
                base.putpixel((gx * CELL + x, gy * CELL + y), p[tone] + (255,))
    # 墙（横向在前、纵向在后）
    wh, wv = wall_h(), wall_v()
    for gx, gy in V_WALL:
        base.alpha_composite(wv, (gx * CELL, gy * CELL - 8))
    for gx, gy in H_WALL:
        base.alpha_composite(wh, (gx * CELL, gy * CELL - 8))
    # 道具与单位
    pg = {"crate": crate(), "barrel": barrel(), "torch": torch(), "chest": chest()}
    for name, gx, gy in PROPS:
        im = pg[name]
        base.alpha_composite(im, (gx * CELL + (CELL - im.width) // 2, (gy + 1) * CELL - im.height))
    u = unit()
    base.alpha_composite(u, (9 * CELL - 8, (7 + 1) * CELL - 28))
    return base


def compose_display():
    native = compose_native()
    return native.resize((native.width * ZOOM, native.height * ZOOM), Image.NEAREST).convert("RGB"), native.size


def simulate_light(disp, scale=1.0):
    """按 Light2D 的方式在**放大之后**叠加光照：暗环境 + 点光径向衰减 + 暖色光池 + 光柱。"""
    w, h = disp.size
    # 光照累积图：冷色暗环境（0.11）作为底
    acc = Image.new("RGB", (w, h), (26, 30, 44))
    for (lx, ly) in LIGHTS:
        cx, cy = lx * ZOOM, ly * ZOOM
        r = int(9 * CELL * ZOOM * scale)                   # 半径约 9 格
        grad = Image.radial_gradient("L").resize((2 * r, 2 * r))
        grad = grad.point(lambda v: int((1 - v / 255.0) ** 2.4 * 255))
        warm = Image.new("RGB", (2 * r, 2 * r), (255, 205, 142))
        warm.putalpha(grad)
        paste_x, paste_y = cx - r, cy - r
        region = acc.crop((paste_x, paste_y, paste_x + 2 * r, paste_y + 2 * r)).convert("RGBA")
        region.alpha_composite(warm)
        acc.paste(region.convert("RGB"), (paste_x, paste_y))
    # 角色随身光
    ux, uy = (9 * CELL - 8 + 16) * ZOOM, (7 * CELL + 8) * ZOOM
    r = int(5 * CELL * ZOOM)
    grad = Image.radial_gradient("L").resize((2 * r, 2 * r)).point(lambda v: int((1 - v / 255.0) ** 2.2 * 150))
    cool = Image.new("RGB", (2 * r, 2 * r), (206, 224, 255))
    cool.putalpha(grad)
    region = acc.crop((ux - r, uy - r, ux + r, uy + r)).convert("RGBA")
    region.alpha_composite(cool)
    acc.paste(region.convert("RGB"), (ux - r, uy - r))
    # 相乘两次：亮处保留、暗处迅速压下去
    lit = ImageChops.multiply(disp, acc)
    lit = ImageChops.multiply(lit, acc.point(lambda v: min(255, int(v * 1.35))))
    return lit


def grid_sheet(items, scale, cols, labels, gap=12, bg=(20, 20, 24)):
    cw = max(i.width for _n, i in items) * scale
    ch = max(i.height for _n, i in items) * scale
    rows = (len(items) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * cw + (cols - 1) * gap, rows * (ch + 22) + (rows - 1) * gap), bg)
    d = ImageDraw.Draw(sheet)
    for i, (name, im) in enumerate(items):
        r, c = divmod(i, cols)
        x, y = c * (cw + gap), r * (ch + 22 + gap)
        big = im.convert("RGBA").resize((im.width * scale, im.height * scale), Image.NEAREST)
        sheet.paste(big, (x + (cw - big.width) // 2, y + (ch - big.height)), big)
        d.text((x, y + ch + 4), labels.get(name, name), font=font(14), fill=(180, 180, 188))
    return sheet


def main():
    # 素材图鉴
    ground = [("dirt", b16.TILES["d"]), ("stone", b16.TILES["s"]), ("grass", b16.TILES["g"]),
              ("wood", b16.TILES["w"]), ("carpet", b16.TILES["c"])]
    grid_sheet(ground, 8, 5, {"dirt": "泥土 16×16", "stone": "石材 16×16", "grass": "草地 16×16",
                              "wood": "木地板 16×16", "carpet": "地毯 16×16"}).save(
        os.path.join(OUT, "contact_ground_8x.png"))

    walls = [("wh", wall_h()), ("wv", wall_v()), ("shadow", shadow_tile())]
    grid_sheet(walls, 6, 3, {"wh": "横向墙 16×24", "wv": "纵向墙 16×24", "shadow": "接触阴影 16×16"}).save(
        os.path.join(OUT, "contact_wall_6x.png"))

    props = [("crate", crate()), ("barrel", barrel()), ("torch", torch()), ("chest", chest()), ("unit", unit())]
    grid_sheet(props, 6, 5, {"crate": "木箱 16×16", "barrel": "木桶 18×24", "torch": "火把 12×28",
                             "chest": "宝箱 16×20", "unit": "单位 32×32 画布"}).save(
        os.path.join(OUT, "contact_props_6x.png"))

    # 全屏：无光 / 有光
    disp, native_size = compose_display()
    canvas = Image.new("RGB", CANVAS, (18, 18, 24))
    canvas.paste(disp, ((MAP_W - disp.width) // 2, (CANVAS[1] - disp.height) // 2))
    canvas.save(os.path.join(OUT, "scene_1920x1080_nolight.png"))

    lit = simulate_light(disp)
    canvas_lit = Image.new("RGB", CANVAS, (10, 10, 14))
    canvas_lit.paste(lit, ((MAP_W - lit.width) // 2, (CANVAS[1] - lit.height) // 2))
    # 光柱（additive）
    shaft = Image.new("RGB", lit.size, (0, 0, 0))
    ds = ImageDraw.Draw(shaft)
    ds.polygon([(7 * CELL * ZOOM + 40, 8 * CELL * ZOOM), (7 * CELL * ZOOM + 96, 8 * CELL * ZOOM),
                (7 * CELL * ZOOM + 260, 8 * CELL * ZOOM + 300), (7 * CELL * ZOOM + 120, 8 * CELL * ZOOM + 300)],
               fill=(34, 16, 14))
    shaft = shaft.filter(ImageFilter.GaussianBlur(10))
    lit2 = ImageChops.add(lit, shaft)
    canvas_lit.paste(lit2, ((MAP_W - lit2.width) // 2, (CANVAS[1] - lit2.height) // 2))
    canvas_lit.save(os.path.join(OUT, "scene_1920x1080_lit.png"))
    print("native", native_size, "display", disp.size)
    print("saved spec16/contact_*.png, scene_1920x1080_nolight.png, scene_1920x1080_lit.png")


if __name__ == "__main__":
    main()
