"""Hand-constructed V03 of 16x16 material items after directional review.

V03 allows six colour roles: outline, shadow, local body, body light,
specular accent and a material mark.  This keeps the 16px grid while avoiding
the flat UI-glyph look of V01.
"""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).parent
OUT = ROOT / "Items16"
QA = ROOT / "QA"
OUT.mkdir(exist_ok=True)
QA.mkdir(exist_ok=True)

P = {
    "K": "#17141a", "S": "#512c26", "D": "#8a4730", "M": "#c96a3d", "L": "#f1ae58", "X": "#ffe38a",
    "g": "#214331", "G": "#397148", "H": "#75b452", "h": "#badb70",
    "r": "#7b2630", "R": "#bf3e38", "o": "#e85b35", "O": "#ff9a43",
    "y": "#9d6219", "Y": "#dc941f", "q": "#ffc53b", "Q": "#ffe371",
    "w": "#603922", "W": "#a86030", "t": "#d78d4b", "T": "#f0b765",
    "b": "#824225", "B": "#c87538", "u": "#e69d50", "U": "#ffd37b",
}

GLYPHS = {
 "medic_herb": [
  "................","................",".......K........","......KgK.......",
  "....KGGGGK......","...KGGHGGGK.....","..KGGGHGGGGK....","...KGGGGGGK.....",
  ".....KggK.......","......KDK.......",".....KDMDK......","....KDMMDK......",
  ".....KDXK.......","......KK........","................","................"],
 "cinder_pear": [
  "................",".......K........","......KDK.......",".....KRRK.......",
  "....KRRRrK......","...KRRooRK......","...KRoOORrK.....","...KRoOORrK.....",
  "...KRRooRK......","....KRRRRK......",".....KRRK.......","......KK........",
  "................","................","................","................"],
 "coin": [
  "................","................","......KKKK......",".....KyyyK......",
  "....KyYqYyK.....","...KyYqQqYyK....","...KyqYKYqK.....","...KyYKYqYyK....",
  "...KyYqQqYyK....","....KyYqYyK.....",".....KyyyK......","......KKKK......",
  "................","................","................","................"],
 "wood_cup": [
  "................","................","....KKKKKK......","...KwwwwwK......",
  "...KwwKKwwK.....","...KWtTTwK.KK...","...KWtTTwK.KtK..","...KWtTTwK.KtK..",
  "...KWtTTwK.KtK..","...KWtTTwK.KKK..","....KWttwK......",".....KWWK.......",
  "......KK........","................","................","................"],
 "bread": [
  "................","................","................",".....KKKKK......",
  "...KKBBBBBKK....","..KBBuBBBBBBK...",".KBBuuBBuBBBBK..",".KBBuKBBKBBBBK..",
  ".KBBBBKBBBKBBK..",".KBBBBBBKBBBBK..","..KBBBBBBBBBK...","...KbbbbbbbK....",
  "....KKKKKKK.....","................","................","................"],
}

LABELS = {"medic_herb":"Medic herb", "cinder_pear":"Cinder pear", "coin":"Coin", "wood_cup":"Wood cup", "bread":"Bread"}

def icon(rows: list[str]) -> Image.Image:
    if len(rows) != 16 or any(len(row) != 16 for row in rows):
        raise ValueError("not 16x16")
    im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    px = im.load()
    for y, row in enumerate(rows):
        for x, c in enumerate(row):
            if c != ".":
                px[x,y] = (*bytes.fromhex(P[c][1:]), 255)
    return im

def main() -> None:
    items = []
    for name, rows in GLYPHS.items():
        im = icon(rows)
        im.save(OUT / f"occ_{name}_16_v03.png")
        items.append((name,im))
    scale, cw, ch = 14, 240, 325
    board = Image.new("RGBA", (cw * len(items), ch), "#16161c")
    draw = ImageDraw.Draw(board); font = ImageFont.load_default()
    for i, (name, im) in enumerate(items):
        x0=i*cw
        draw.text((x0+10,10),LABELS[name],font=font,fill="#e9e5dc")
        draw.text((x0+10,29),"V03 native 16x16",font=font,fill="#aaa7b1")
        # exact 1x view
        for yy in range(16):
            for xx in range(16):
                draw.point((x0+87+xx,60+yy), fill="#33333e" if (xx+yy)%2 else "#2a2a33")
        board.alpha_composite(im,(x0+87,60))
        board.alpha_composite(im.resize((16*scale,16*scale),Image.Resampling.NEAREST),(x0+(cw-16*scale)//2,85))
    board.save(QA / "items16_v03_overview.png")

if __name__ == "__main__": main()
