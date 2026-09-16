#!/usr/bin/env python3
"""Build review-only target-grid cleanup variants without changing approved candidates."""
from pathlib import Path
from PIL import Image

ROOT=Path(__file__).resolve().parents[2]
ART=ROOT/"UnityProject/Assets/Game/Resources/Art/CombatTestArenaSephiriaDebug"
OUT=ROOT/"UnityProject/Reports/CombatTestArena/target_grid_cleanup_test"

def outline(im):
    im=im.convert("RGBA"); a=im.getchannel("A").point(lambda v:255 if v>=128 else 0); ap=a.load(); px=im.load()
    for y in range(im.height):
        for x in range(im.width):
            if ap[x,y] and any(nx<0 or ny<0 or nx>=im.width or ny>=im.height or not ap[nx,ny] for nx,ny in ((x-1,y),(x+1,y),(x,y-1),(x,y+1))): px[x,y]=(0,0,0,255)
    im.putalpha(a); return im

def cleanup(name, factor):
    src=Image.open(ART/name).convert("RGBA")
    small=src.resize((src.width//factor,src.height//factor),Image.Resampling.NEAREST)
    # Re-quantize after target-grid reduction so gray fringe and single-pixel
    # pseudo-detail do not survive as false texture.
    alpha=small.getchannel("A").point(lambda v:255 if v>=128 else 0)
    rgb=small.convert("RGB").quantize(colors=16,method=Image.Quantize.FASTOCTREE).convert("RGB")
    small=rgb.convert("RGBA"); small.putalpha(alpha)
    result=outline(small.resize(src.size,Image.Resampling.NEAREST))
    out=OUT/(Path(name).stem+"_target_grid_outline.png"); out.parent.mkdir(parents=True,exist_ok=True); result.save(out)
    zoom=result.resize((result.width*4,result.height*4),Image.Resampling.NEAREST); board=Image.new("RGBA",zoom.size,(180,180,180,255)); bp=board.load()
    for y in range(board.height):
        for x in range(board.width):
            if ((x//16)+(y//16))%2: bp[x,y]=(220,220,220,255)
    board.alpha_composite(zoom); board.save(OUT/(Path(name).stem+"_contact.png"))

if __name__=="__main__":
    cleanup("sephiria_unit_side_64.png",2)
    cleanup("sephiria_heavy_cover_64.png",2)
    cleanup("sephiria_heavy_cover_2x2_128.png",2)
    print(OUT)
