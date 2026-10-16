from pathlib import Path
import sys
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parents[3]
folder = root / 'UnityProject/ReviewEvidence/MainMenuE'
prefix = sys.argv[1] if len(sys.argv) > 1 else 'physical'
sheet = Image.new('RGB', (1920, 1160), '#202626')
draw = ImageDraw.Draw(sheet)
font = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 23)
for i, (key, title) in enumerate([
    ('workshop', '工坊 · 承载面 / 托盘 / 前排卡片'),
    ('shop', '市集 · 独立陈列 / 前排操作'),
    ('medical', '医务室 · 档案 / 服务牌 / 返回牌'),
    ('event', '事件 · 便笺 / 选择牌 / 桌面落点'),
]):
    x, y = (i % 2) * 960, (i // 2) * 580
    draw.text((x + 16, y + 10), title, fill='#efede4', font=font)
    suffix = '-final' if prefix == 'physical' else ''
    shot = Image.open(folder / f'{prefix}-{key}{suffix}.png').convert('RGB')
    sheet.paste(shot.resize((960, 540), Image.Resampling.LANCZOS), (x, y + 40))
sheet.save(folder / f'{prefix}-depth-review.jpg', quality=92)
