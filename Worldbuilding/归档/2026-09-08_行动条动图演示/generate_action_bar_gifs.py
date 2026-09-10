from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent
FONT_PATH = ROOT / "UnityProject/Assets/Game/Resources/Fonts/FusionPixel12ProportionalZhHans.ttf"

SCALE = 2
W, H = 480, 160
BG = "#17191b"
PAPER = "#d8c7a6"
PAPER_DARK = "#b5a37f"
INK = "#25241f"
MUTED = "#756c5c"
TRACK = "#413f39"
TRACK_INNER = "#252729"
CYAN = "#2e7b82"
CYAN_LIGHT = "#70b7b9"
RED = "#a4493f"
RED_LIGHT = "#d17868"
BRASS = "#a86f22"
GREEN = "#4f785b"
WHITE = "#f2e9d7"


def font(size: int):
    return ImageFont.truetype(str(FONT_PATH), size=size)


F8 = font(8)
F9 = font(9)
F10 = font(10)
F12 = font(12)
F14 = font(14)


def txt(draw, xy, content, fill=INK, f=F9, anchor=None):
    draw.text(xy, content, fill=fill, font=f, anchor=anchor)


def panel(draw, box, fill=PAPER, outline=PAPER_DARK):
    draw.rectangle(box, fill=fill, outline=outline, width=1)
    x0, y0, x1, _ = box
    draw.line((x0 + 2, y0 + 2, x1 - 2, y0 + 2), fill="#eadcbc", width=1)


def bar(draw, y, name, value, speed, color, flash=False, delta=None):
    x, width, height = 22, 278, 14
    txt(draw, (x, y - 12), name, fill=INK, f=F9)
    txt(draw, (x + width, y - 12), f"速度 {speed}", fill=MUTED, f=F8, anchor="ra")
    draw.rectangle((x, y, x + width, y + height), fill=TRACK, outline=INK, width=1)
    draw.rectangle((x + 2, y + 2, x + width - 2, y + height - 2), fill=TRACK_INNER)
    fill_width = max(0, int((width - 4) * min(max(value, 0), 199) / 199))
    if fill_width:
        draw.rectangle((x + 2, y + 2, x + 2 + fill_width, y + height - 2), fill=color)
    threshold_x = x + 2 + int((width - 4) * 100 / 199)
    draw.line((threshold_x, y - 2, threshold_x, y + height + 2), fill=WHITE, width=1)
    txt(draw, (threshold_x, y + height + 4), "100", fill=MUTED, f=F8, anchor="ma")
    value_fill = WHITE if value >= 100 else INK
    label_x = min(x + width - 5, max(x + 18, x + 2 + fill_width - 3))
    txt(draw, (label_x, y + 7), str(value), fill=value_fill, f=F9, anchor="mm")
    if flash:
        draw.rectangle((x - 2, y - 2, x + width + 2, y + height + 2), outline=CYAN_LIGHT, width=2)
    if delta is not None:
        txt(draw, (x + width - 2, y + 7), delta, fill=RED_LIGHT, f=F10, anchor="rm")


def compact_bar(draw, y, name, value, speed, color, flash=False, delta=None):
    """Three-row action-bar layout used by the full delay example."""
    x, width, height = 92, 208, 14
    txt(draw, (22, y + 7), f"{name} 速{speed}", fill=INK, f=F8, anchor="lm")
    draw.rectangle((x, y, x + width, y + height), fill=TRACK, outline=INK, width=1)
    draw.rectangle((x + 2, y + 2, x + width - 2, y + height - 2), fill=TRACK_INNER)
    fill_width = max(0, int((width - 4) * min(max(value, 0), 199) / 199))
    if fill_width:
        draw.rectangle((x + 2, y + 2, x + 2 + fill_width, y + height - 2), fill=color)
    threshold_x = x + 2 + int((width - 4) * 100 / 199)
    draw.line((threshold_x, y - 2, threshold_x, y + height + 2), fill=WHITE, width=1)
    value_fill = WHITE if value >= 100 else INK
    label_x = min(x + width - 5, max(x + 18, x + 2 + fill_width - 3))
    txt(draw, (label_x, y + 7), str(value), fill=value_fill, f=F9, anchor="mm")
    if flash:
        draw.rectangle((x - 2, y - 2, x + width + 2, y + height + 2), outline=CYAN_LIGHT, width=2)
    if delta is not None:
        txt(draw, (x + width - 2, y + 7), delta, fill=RED_LIGHT, f=F10, anchor="rm")


def queue(draw, title, entries, active=0):
    panel(draw, (318, 34, 466, 131), fill="#cbb997")
    txt(draw, (328, 42), title, fill=INK, f=F10)
    draw.line((327, 56, 456, 56), fill=MUTED, width=1)
    for idx, (label, value, color) in enumerate(entries):
        y = 64 + idx * 20
        if idx == active:
            draw.rectangle((325, y - 2, 459, y + 14), fill="#e7d6b5", outline=color, width=1)
        txt(draw, (331, y), f"{idx + 1}. {label}", fill=INK, f=F8)
        txt(draw, (453, y), str(value), fill=color, f=F9, anchor="ra")


def base(title, badge, subtitle):
    im = Image.new("RGB", (W, H), BG)
    draw = ImageDraw.Draw(im)
    panel(draw, (8, 8, 472, 151))
    draw.rectangle((8, 8, 472, 30), fill=INK)
    txt(draw, (18, 13), title, fill=WHITE, f=F12)
    draw.rectangle((399, 12, 461, 26), fill=CYAN if badge == "正常" else RED)
    txt(draw, (430, 19), badge, fill=WHITE, f=F9, anchor="mm")
    txt(draw, (18, 137), subtitle, fill=MUTED, f=F8)
    return im, draw


def upscale(im):
    return im.resize((W * SCALE, H * SCALE), Image.Resampling.NEAREST)


def tween(start, end, step, steps):
    """Integer display interpolation; game-state settlement remains discrete."""
    progress = min(max(step, 0), steps) / steps
    return round(start + (end - start) * progress)


def normal_frame(i):
    im, draw = base("行动条演示 01 · 正常推进", "正常", "规则：达到 100 获得回合；行动结束扣 100，溢出保留")
    if i < 12:
        mia, guard, scout = 62, 50, 24
        label = "状态 0 停顿：等待下一次速度结算"
        entries = [("米娅", mia, CYAN), ("铜铠守卫", guard, RED), ("灯藤猎手", scout, RED)]
        active = 0
        flash = False
    elif i < 20:
        mia = tween(62, 86, i - 11, 8)
        guard = tween(50, 68, i - 11, 8)
        scout = tween(24, 38, i - 11, 8)
        label = "视觉过渡：显示时间单位 1 的离散结算结果"
        entries = [("米娅", mia, CYAN), ("铜铠守卫", guard, RED), ("灯藤猎手", scout, RED)]
        active = 0
        flash = False
    elif i < 32:
        mia, guard, scout = 86, 68, 38
        label = "状态 1 停顿：本次速度结算已完成"
        entries = [("米娅", mia, CYAN), ("铜铠守卫", guard, RED), ("灯藤猎手", scout, RED)]
        active = 0
        flash = False
    elif i < 40:
        mia = tween(86, 110, i - 31, 8)
        guard = tween(68, 86, i - 31, 8)
        scout = tween(38, 52, i - 31, 8)
        label = "视觉过渡：显示时间单位 2 的离散结算结果"
        entries = [("米娅", mia, CYAN), ("铜铠守卫", guard, RED), ("灯藤猎手", scout, RED)]
        active = 0
        flash = False
    elif i < 52:
        mia, guard, scout = 110, 86, 52
        label = "状态 2 停顿：米娅达到 100，获得回合"
        entries = [("当前：米娅", mia, CYAN), ("铜铠守卫", guard, RED), ("灯藤猎手", scout, RED)]
        active = 0
        flash = i % 6 < 3
    elif i < 60:
        mia, guard, scout = 110, 86, 52
        label = "米娅行动中：数值保持不变，战斗时间暂停"
        entries = [("当前：米娅", mia, CYAN), ("铜铠守卫", guard, RED), ("灯藤猎手", scout, RED)]
        active = 0
        flash = False
    elif i < 68:
        mia = tween(110, 10, i - 59, 8)
        guard, scout = 86, 52
        label = "视觉过渡：行动结束扣除 100，溢出保留"
        entries = [("铜铠守卫", guard, RED), ("灯藤猎手", scout, RED), ("米娅", mia, CYAN)]
        active = 0
        flash = False
    else:
        mia, guard, scout = 10, 86, 52
        label = "状态 3 停顿：110 减 100，剩余 10"
        entries = [("铜铠守卫", guard, RED), ("灯藤猎手", scout, RED), ("米娅", mia, CYAN)]
        active = 0
        flash = False
    txt(draw, (22, 40), label, fill=INK, f=F10)
    bar(draw, 64, "米娅", mia, 24, CYAN, flash=flash)
    bar(draw, 99, "铜铠守卫", guard, 18, RED)
    queue(draw, "预计行动顺序", entries, active=active)
    return upscale(im)


def delay_frame(i):
    im, draw = base("行动条演示 02 · 行动延后", "延后", "规则：延后直接减少当前值；等待者跌破 100 后退出待行动队列")
    if i < 12:
        mia, guard, vine = 91, 86, 78
        label = "状态 0 停顿：米娅 91，守卫 86，猎手 78"
        entries = [("米娅", mia, CYAN), ("铜铠守卫", guard, RED), ("灯藤猎手", vine, RED)]
        active = 0
        delta = None
        queue_title = "实时排序"
    elif i < 22:
        mia = tween(91, 139, i - 11, 10)
        guard = tween(86, 124, i - 11, 10)
        vine = tween(78, 108, i - 11, 10)
        label = "视觉过渡：显示一次离散速度结算的结果"
        entries = [("米娅", mia, CYAN), ("铜铠守卫", guard, RED), ("灯藤猎手", vine, RED)]
        active = 0
        delta = None
        queue_title = "速度结算中"
    elif i < 36:
        mia, guard, vine = 139, 124, 108
        label = "状态 1 停顿：米娅 139，守卫 124，猎手 108"
        entries = [("米娅", mia, CYAN), ("铜铠守卫", guard, RED), ("灯藤猎手", vine, RED)]
        active = 0
        delta = None
        queue_title = "三人均已过 100"
    elif i < 48:
        mia, guard, vine = 139, 124, 108
        label = "按行动条排序：米娅第一，守卫第二，猎手第三"
        entries = [("当前 米娅", mia, CYAN), ("等待 守卫", guard, RED), ("等待 猎手", vine, RED)]
        active = 0
        delta = None
        queue_title = "行动顺序"
    elif i < 62:
        mia, guard, vine = 139, 124, 108
        label = "米娅预览行动延后：守卫预计减少 60，降至 64"
        entries = [("当前 米娅", mia, CYAN), ("等待 守卫", guard, RED), ("等待 猎手", vine, RED)]
        active = 1
        delta = "-60"
        queue_title = "结算前顺序"
    elif i < 72:
        mia, vine = 139, 108
        guard = tween(124, 64, i - 61, 10)
        label = "视觉过渡：行动延后使守卫从 124 降至 64"
        entries = [("当前 米娅", mia, CYAN), ("等待 猎手", vine, RED), ("守卫", guard, RED)]
        active = 2
        delta = "-60"
        queue_title = "延后结算中"
    elif i < 88:
        mia, guard, vine = 139, 64, 108
        label = "状态 2 停顿：守卫已降至 64，跌破 100"
        entries = [("当前 米娅", mia, CYAN), ("等待 猎手", vine, RED), ("守卫", guard, RED)]
        active = 2
        delta = None
        queue_title = "数值已更新"
    elif i < 100:
        mia, guard, vine = 139, 64, 108
        label = "米娅继续行动：时间暂停，猎手保持 108"
        entries = [("当前 米娅", mia, CYAN), ("下一位 猎手", vine, RED), ("守卫", guard, RED)]
        active = 1
        delta = None
        queue_title = "结算后顺序"
    elif i < 110:
        mia = tween(139, 39, i - 99, 10)
        guard, vine = 64, 108
        label = "视觉过渡：米娅结束回合，139 减 100"
        entries = [("当前 米娅", mia, CYAN), ("下一位 猎手", vine, RED), ("守卫", guard, RED)]
        active = 0
        delta = None
        queue_title = "回合结束中"
    elif i < 124:
        mia, guard, vine = 39, 64, 108
        label = "状态 3 停顿：米娅剩余 39，准备交接行动权"
        entries = [("下一位 猎手", vine, RED), ("守卫", guard, RED), ("米娅", mia, CYAN)]
        active = 0
        delta = None
        queue_title = "交接顺序"
    else:
        mia, guard, vine = 39, 64, 108
        label = "交接完成：猎手正式获得回合，守卫仍在等待"
        entries = [("当前 猎手", vine, RED), ("守卫", guard, RED), ("米娅", mia, CYAN)]
        active = 0
        delta = None
        queue_title = "当前行动者"
    txt(draw, (22, 40), label, fill=INK, f=F10)
    compact_bar(draw, 55, "米娅", mia, 48, CYAN, flash=36 <= i < 48 or 100 <= i < 124)
    compact_bar(draw, 86, "铜铠守卫", guard, 38, RED, flash=48 <= i < 88, delta=delta)
    compact_bar(draw, 117, "灯藤猎手", vine, 30, RED, flash=i >= 124 and i % 6 < 3)
    if 48 <= i < 72:
        arrow_start_x = 94 + int(204 * 124 / 199)
        arrow_end_x = 94 + int(204 * 64 / 199)
        draw.line((arrow_start_x, 82, arrow_end_x, 82), fill=RED_LIGHT, width=2)
        draw.polygon(
            [(arrow_end_x, 82), (arrow_end_x + 6, 78), (arrow_end_x + 6, 86)],
            fill=RED_LIGHT,
        )
    queue(draw, queue_title, entries, active=active)
    return upscale(im)


def save_gif(name, frames):
    path = OUT / name
    paletted = [frame.convert("P", palette=Image.Palette.ADAPTIVE, colors=128) for frame in frames]
    paletted[0].save(
        path,
        save_all=True,
        append_images=paletted[1:],
        duration=100,
        loop=0,
        optimize=True,
        disposal=2,
    )
    return path


def contact_sheet(name, frames, picks):
    sheet = Image.new("RGB", (W * SCALE * 2, H * SCALE * 2), "#0d0e0f")
    for slot, index in enumerate(picks):
        x = (slot % 2) * W * SCALE
        y = (slot // 2) * H * SCALE
        sheet.paste(frames[index], (x, y))
    path = OUT / name
    sheet.save(path)
    return path


def main():
    normal = [normal_frame(i) for i in range(80)]
    delayed = [delay_frame(i) for i in range(140)]
    paths = [
        save_gif("occ_action_bar_normal.gif", normal),
        save_gif("occ_action_bar_delay.gif", delayed),
        contact_sheet("qa_action_bar_normal.png", normal, [0, 16, 36, 72]),
        contact_sheet("qa_action_bar_delay.png", delayed, [0, 66, 105, 132]),
    ]
    for path in paths:
        print(f"{path.name}\t{path.stat().st_size}")


if __name__ == "__main__":
    main()
