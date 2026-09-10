from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


OUT = Path(__file__).resolve().parent
FONT = Path(__file__).resolve().parents[3] / "UnityProject/Assets/Game/Resources/Fonts/FusionPixel12ProportionalZhHans.ttf"

W, H = 1180, 820
GRID_X, GRID_Y, CELL = 92, 118, 68
BG = "#20272b"
FLOOR = "#bdb3a0"
GRID = "#625f56"
INK = "#f3eadb"
MUTED = "#c8bead"
WATER = "#608b9b"
VINE = "#4e6845"
VINE_DARK = "#263d2e"
CRYSTAL = "#72c5d0"
CRYSTAL_DARK = "#356f78"
COVER = "#a97942"
BLOCK = "#373c3d"
PLAYER = "#3bc2cf"
ENEMY = "#bd5359"
GOLD = "#d6a957"
ROUTE = "#e0b361"


def font(size):
    return ImageFont.truetype(str(FONT), size)


F_TITLE = font(34)
F_SUB = font(20)
F_COORD = font(18)
F_BODY = font(17)
F_SMALL = font(15)
F_UNIT = font(20)


def cell_box(col, row, pad=0):
    x0 = GRID_X + (ord(col) - 65) * CELL + pad
    y0 = GRID_Y + (row - 1) * CELL + pad
    return (x0, y0, x0 + CELL - 2 * pad, y0 + CELL - 2 * pad)


def center(col, row):
    x0, y0, x1, y1 = cell_box(col, row)
    return ((x0 + x1) // 2, (y0 + y1) // 2)


def draw_grid(draw):
    for row in range(1, 10):
        for i in range(12):
            col = chr(65 + i)
            draw.rectangle(cell_box(col, row), fill=FLOOR, outline=GRID, width=2)
    for i in range(12):
        col = chr(65 + i)
        x, _ = center(col, 1)
        draw.text((x, GRID_Y - 20), col, font=F_COORD, fill=INK, anchor="mm")
    for row in range(1, 10):
        _, y = center("A", row)
        draw.text((GRID_X - 20, y), str(row), font=F_COORD, fill=INK, anchor="mm")


def draw_water(draw, col, row):
    draw.rectangle(cell_box(col, row, 2), fill=WATER)
    x0, y0, x1, y1 = cell_box(col, row)
    cy = (y0 + y1) // 2
    for dy, inset in [(-8, 14), (8, 22)]:
        draw.line((x0 + inset, cy + dy, x1 - inset, cy + dy), fill="#d7edf0", width=3)


def draw_vine(draw, col, row):
    x0, y0, x1, y1 = cell_box(col, row, 2)
    draw.rectangle((x0, y0, x1, y1), fill=VINE)
    cx, cy = center(col, row)
    draw.ellipse((cx - 25, cy - 25, cx + 25, cy + 25), fill="#b9e5d8")
    draw.ellipse((cx - 20, cy - 20, cx + 20, cy + 20), fill=VINE_DARK)


def draw_crystal(draw, col, row, durability):
    cx, cy = center(col, row)
    points = [(cx, cy - 26), (cx + 21, cy - 5), (cx + 14, cy + 24), (cx - 14, cy + 24), (cx - 21, cy - 5)]
    draw.polygon(points, fill=CRYSTAL, outline="#e4ffff")
    inner = [(cx, cy - 18), (cx + 12, cy - 2), (cx + 7, cy + 15), (cx - 8, cy + 15), (cx - 12, cy - 2)]
    draw.polygon(inner, fill=CRYSTAL_DARK)
    draw.text((cx, cy + 2), str(durability), font=F_SMALL, fill=INK, anchor="mm")


def draw_cover(draw, col, row):
    x0, y0, x1, y1 = cell_box(col, row)
    draw.rectangle((x0 + 10, y0 + 22, x1 - 10, y1 - 12), fill="#765634")
    draw.rectangle((x0 + 12, y0 + 18, x1 - 12, y1 - 16), fill=COVER, outline="#5a4935", width=2)


def draw_block(draw, col, row):
    x0, y0, x1, y1 = cell_box(col, row, 2)
    draw.rectangle((x0, y0, x1, y1), fill=BLOCK)
    draw.line((x0 + 14, y0 + 14, x1 - 14, y1 - 14), fill="#756f63", width=2)
    draw.line((x1 - 14, y0 + 14, x0 + 14, y1 - 14), fill="#756f63", width=2)


def draw_chest(draw, col, row):
    cx, cy = center(col, row)
    draw.rectangle((cx - 24, cy - 14, cx + 24, cy + 18), fill="#6f4c2d", outline="#3f3225", width=2)
    draw.rectangle((cx - 22, cy - 20, cx + 22, cy - 6), fill=GOLD, outline="#5c4528", width=2)
    draw.rectangle((cx - 4, cy - 8, cx + 4, cy + 5), fill="#efe1a2")


def draw_unit(draw, col, row, label, color):
    cx, cy = center(col, row)
    draw.ellipse((cx - 20, cy - 20, cx + 20, cy + 20), fill=color, outline="#eaf4ef", width=3)
    draw.text((cx, cy + 1), label, font=F_UNIT, fill="white", anchor="mm")


def wrap(draw, text, width_px, font_obj):
    lines, current = [], ""
    for ch in text:
        trial = current + ch
        if current and draw.textlength(trial, font=font_obj) > width_px:
            lines.append(current)
            current = ch
        else:
            current = trial
    if current:
        lines.append(current)
    return lines


def side_text(draw, y, text, font_obj=F_BODY, fill=MUTED, width=205, gap=22):
    for line in wrap(draw, text, width, font_obj):
        draw.text((970, y), line, font=font_obj, fill=fill)
        y += gap
    return y


def legend_item(draw, y, color, label, shape="square"):
    if shape == "circle":
        draw.ellipse((936, y, 960, y + 24), fill=color, outline="#eaf4ef", width=2)
    elif shape == "diamond":
        draw.polygon([(948, y), (960, y + 12), (948, y + 24), (936, y + 12)], fill=color, outline="#eaf4ef")
    else:
        draw.rectangle((936, y, 960, y + 24), fill=color)
    draw.text((973, y + 12), label, font=F_BODY, fill=INK, anchor="lm")


def base(title, subtitle):
    im = Image.new("RGB", (W, H), BG)
    draw = ImageDraw.Draw(im)
    draw.text((52, 30), title, font=F_TITLE, fill=INK)
    draw.text((52, 78), subtitle, font=F_SUB, fill=MUTED)
    draw_grid(draw)
    draw.text((938, 126), "图例", font=F_SUB, fill=INK)
    return im, draw


def battle3():
    im, draw = base("第三关｜雨痕晶庭", "破晶开匣，入水熄火")
    for row in range(2, 9):
        draw_water(draw, "D", row)
    for col in "EF":
        draw_water(draw, col, 2)
    for col, row in [("B", 3), ("B", 7), ("I", 3), ("J", 7)]:
        draw_cover(draw, col, row)
    for col, row in [("H", 4), ("H", 6), ("I", 5)]:
        draw_block(draw, col, row)
    draw_crystal(draw, "G", 5, 24)
    draw_chest(draw, "H", 5)
    draw_unit(draw, "B", 5, "P", PLAYER)
    draw_unit(draw, "F", 5, "D", ENEMY)
    draw_unit(draw, "G", 2, "Y", ENEMY)

    legend_item(draw, 166, PLAYER, "P  主角")
    legend_item(draw, 205, ENEMY, "D/Y  敌人", "circle")
    legend_item(draw, 244, WATER, "浅水")
    legend_item(draw, 283, CRYSTAL, "封门晶簇 24", "diamond")
    legend_item(draw, 322, GOLD, "器材匣")
    legend_item(draw, 361, COVER, "轻掩体")
    legend_item(draw, 400, BLOCK, "永久重物块")
    y = side_text(draw, 462, "D：承压检验偶")
    y = side_text(draw, y + 8, "Y：火矢陪练生")
    y = side_text(draw, y + 24, "核心选择：破晶开匣，或经 D9 干路绕行。", F_BODY, INK)
    side_text(draw, y + 24, "浅水只移除单位燃烧，不清除其他格的火场。", F_SMALL, MUTED, gap=19)
    return im


def elite():
    im, draw = base("精英战｜三材承压场", "公开锁线，借敌手改写战场")
    for row in range(2, 9):
        draw_water(draw, "D", row)
    for row in (3, 6):
        for col in "EFG":
            draw_vine(draw, col, row)
    draw_crystal(draw, "G", 5, 32)
    draw_unit(draw, "B", 5, "P", PLAYER)
    draw_unit(draw, "H", 5, "R", ENEMY)

    # First disclosed charge route: H5 toward the locked B5 target, stopped by G5.
    hx, hy = center("H", 5)
    gx, gy = center("G", 5)
    draw.line((hx - 24, hy, gx + 25, gy), fill=ROUTE, width=5)
    draw.polygon([(gx + 22, gy), (gx + 34, gy - 8), (gx + 34, gy + 8)], fill=ROUTE)
    draw.text((hx, hy - 31), "首条锁线", font=F_SMALL, fill=ROUTE, anchor="mm")

    legend_item(draw, 166, PLAYER, "P  主角")
    legend_item(draw, 205, ENEMY, "R  楔角", "circle")
    legend_item(draw, 244, WATER, "浅水")
    legend_item(draw, 283, VINE, "灯藤")
    legend_item(draw, 322, CRYSTAL, "稳压晶簇 32", "diamond")
    draw.line((936, 373, 960, 373), fill=ROUTE, width=5)
    draw.text((973, 373), "公开冲压路线", font=F_BODY, fill=INK, anchor="lm")
    y = side_text(draw, 430, "R：贯阵承压机·楔角")
    y = side_text(draw, y + 24, "首条冲压锁定 B5，并在 G5 晶簇处停止。", F_BODY, INK)
    side_text(draw, y + 24, "之后玩家可诱导撞晶、搜藤、入水冷却或干地卸压。", F_SMALL, MUTED, gap=19)
    return im


if __name__ == "__main__":
    battle3().save(OUT / "battle3_rain_prism_court_layout.png", optimize=True)
    elite().save(OUT / "elite_three_material_pressure_layout.png", optimize=True)
    print(OUT / "battle3_rain_prism_court_layout.png")
    print(OUT / "elite_three_material_pressure_layout.png")
