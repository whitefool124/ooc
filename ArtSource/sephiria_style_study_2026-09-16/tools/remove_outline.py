# -*- coding: utf-8 -*-
"""去掉黑描边：只替换"贴外缘的纯黑像素"，用内侧身体色填回，剪影尺寸不变。

- 工程单位：描边是纯黑（2,2,2）/（0,0,0），maxchan=12 可精确命中，不会吃掉身体暗部（如 33,24,22）。
- 我方道具：描边色是 (20,18,24)，用 maxchan=30。
输出：去边前后对照 + 去边后的场景（方案 B：人物 4 倍、地面 6 倍）。
"""
import importlib.util
import os

from PIL import Image, ImageDraw, ImageFont

ROOT = r"E:\数据库\OCC_Codex"
STUDY = os.path.join(ROOT, "ArtSource", "sephiria_style_study_2026-09-16")
HERE = os.path.join(STUDY, "drawn")
OUT = os.path.join(STUDY, "project_assets")
os.makedirs(OUT, exist_ok=True)
FONTS = [r"C:\Windows\Fonts\msyh.ttc", r"C:\Windows\Fonts\simhei.ttf", r"C:\Windows\Fonts\arial.ttf"]

UNITS = [("pyromancer.png", 9, 7), ("raider_idle_32x64.png", 6, 6),
         ("maintenance_guard_native.png", 11, 8), ("sephiria_unit_native_grid_64.png", 8, 9),
         ("tether_hound_idle_64x32_final.png", 10, 6)]


def font(size):
    for p in FONTS:
        if os.path.exists(p):
            try:
                return ImageFont.truetype(p, size)
            except Exception:
                continue
    return ImageFont.load_default()


def find(name):
    for dp, dn, fn in os.walk(ROOT):
        if any(s in dp for s in ("\\Library\\", "\\obj\\", "\\Temp\\", "\\.git\\", "\\Logs\\")):
            continue
        if name in fn:
            return os.path.join(dp, name)
    return None


def strip_outline(im, maxchan=12, passes=2, rule="transparent", bright=70):
    """把贴外缘的近黑像素换成内侧较亮的相邻色。

    rule="transparent"：旁边有透明像素才算外缘（适用于带透明边的单位/道具）。
    rule="contrast"：旁边有亮于 bright 的像素也算外缘（适用于整块不透明、只描了一圈边的墙体）。
    """
    im = im.convert("RGBA").copy()
    for _ in range(passes):
        src = im.copy()
        px = src.load()
        out = im.load()
        W, H = im.size
        changed = 0
        for y in range(H):
            for x in range(W):
                r, g, b, a = px[x, y]
                if a == 0 or max(r, g, b) > maxchan:
                    continue
                rim = False
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if not (0 <= nx < W and 0 <= ny < H):
                        continue
                    nr, ng, nb, na = px[nx, ny]
                    if rule == "transparent" and na == 0:
                        rim = True
                        break
                    if rule == "contrast" and na > 0 and (0.2126 * nr + 0.7152 * ng + 0.0722 * nb) > bright:
                        rim = True
                        break
                if not rim:
                    continue
                best = None
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)):
                    nx, ny = x + dx, y + dy
                    if not (0 <= nx < W and 0 <= ny < H):
                        continue
                    nr, ng, nb, na = px[nx, ny]
                    if na == 0 or max(nr, ng, nb) <= maxchan:
                        continue
                    lum = 0.2126 * nr + 0.7152 * ng + 0.0722 * nb
                    if best is None or lum > best[0]:
                        best = (lum, (nr, ng, nb, na))
                if best:
                    out[x, y] = best[1]
                    changed += 1
        if not changed:
            break
    return im


def checker(w, h, s=8):
    im = Image.new("RGB", (w, h), (54, 54, 62))
    d = ImageDraw.Draw(im)
    for y in range(0, h, s):
        for x in range(0, w, s):
            if ((x // s) + (y // s)) % 2:
                d.rectangle([x, y, x + s - 1, y + s - 1], fill=(66, 66, 76))
    return im


def outline_sheet():
    pairs = []
    for name, _gx, _gy in UNITS:
        p = find(name)
        if not p:
            continue
        src = Image.open(p).convert("RGBA")
        pairs.append((name, src, strip_outline(src, 12)))
    z = 5
    cw = max(max(a.width, b.width) for _n, a, b in pairs) * z + 20
    ch = max(max(a.height, b.height) for _n, a, b in pairs) * z
    sheet = Image.new("RGB", (len(pairs) * (cw * 2) + 20, 34 + ch + 26), (20, 20, 24))
    d = ImageDraw.Draw(sheet)
    d.text((8, 8), "去黑边对照（每个：左=原图，右=去边后），同一倍率", font=font(20), fill=(235, 235, 240))
    x = 10
    for name, a, b in pairs:
        bg = checker(cw * 2, ch)
        ai = a.resize((a.width * z, a.height * z), Image.NEAREST)
        bi = b.resize((b.width * z, b.height * z), Image.NEAREST)
        bg.paste(ai, (cw // 2 - ai.width // 2, ch - ai.height), ai)
        bg.paste(bi, (cw + cw // 2 - bi.width // 2, ch - bi.height), bi)
        sheet.paste(bg, (x, 34))
        d.text((x, 34 + ch + 3), name[:26], font=font(13), fill=(180, 180, 188))
        x += cw * 2
    sheet.save(os.path.join(OUT, "outline_removed_5x.png"))
    print("saved outline_removed_5x.png", sheet.size)


def build_scene(unit_scale=4, ground_scale=6, no_outline=True, strip_walls=False, name_suffix=""):
    spec = importlib.util.spec_from_file_location("s16", os.path.join(HERE, "draw_spec16.py"))
    s16 = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(s16)
    b16 = s16.b16
    grid = s16.grid_board()
    board = b16.render_board(grid).convert("RGBA")
    wh, wv = s16.wall_h(), s16.wall_v()
    if strip_walls:
        wh, wv = strip_outline(wh, 30, rule="contrast"), strip_outline(wv, 30, rule="contrast")
    for gx, gy in s16.V_WALL:
        board.alpha_composite(wv, (gx * s16.CELL, gy * s16.CELL - 8))
    for gx, gy in s16.H_WALL:
        board.alpha_composite(wh, (gx * s16.CELL, gy * s16.CELL - 8))
    props = {"crate": s16.crate(), "barrel": s16.barrel(), "torch": s16.torch(), "chest": s16.chest()}
    for name, gx, gy in s16.PROPS:
        im = props[name]
        if no_outline:
            im = strip_outline(im, 30)
        board.alpha_composite(im, (gx * s16.CELL + (s16.CELL - im.width) // 2, (gy + 1) * s16.CELL - im.height))

    Z = ground_scale
    disp = board.resize((board.width * Z, board.height * Z), Image.NEAREST).convert("RGBA")
    for name, gx, gy in UNITS:
        p = find(name)
        if not p:
            continue
        u = Image.open(p).convert("RGBA")
        if no_outline:
            u = strip_outline(u, 12)
        big = u.resize((u.width * unit_scale, u.height * unit_scale), Image.NEAREST)
        cx = gx * s16.CELL * Z + (s16.CELL * Z - big.width) // 2
        cy = (gy + 1) * s16.CELL * Z - big.height
        disp.alpha_composite(big, (cx, cy))
    lit = s16.simulate_light(disp.convert("RGB"))
    page = Image.new("RGB", s16.CANVAS, (10, 10, 14))
    page.paste(lit, ((s16.MAP_W - lit.width) // 2, (s16.CANVAS[1] - lit.height) // 2))
    return page


def main():
    outline_sheet()
    a = build_scene(no_outline=False)
    b = build_scene(no_outline=True)
    c = build_scene(no_outline=True, strip_walls=True)
    a.save(os.path.join(OUT, "scene_B_with_outline.png"))
    b.save(os.path.join(OUT, "scene_B_no_outline.png"))
    c.save(os.path.join(OUT, "scene_B_no_outline_at_all.png"))
    sheet = Image.new("RGB", (1920, 3 * (44 + 1080)), (8, 8, 12))
    d = ImageDraw.Draw(sheet)
    rows = [("保留黑边（原样）", a), ("去掉人物与道具的黑边", b), ("连墙体黑边也去掉", c)]
    for i, (text, img) in enumerate(rows):
        y = i * (44 + 1080)
        d.rectangle([0, y, 1920, y + 44], fill=(14, 14, 18))
        d.text((12, y + 8), text, font=font(22), fill=(235, 235, 240))
        sheet.paste(img, (0, y + 44))
    sheet.save(os.path.join(OUT, "compare_outline_variants.png"))
    print("saved scene_B_with/no_outline(_at_all).png, compare_outline_variants.png")


if __name__ == "__main__":
    main()
