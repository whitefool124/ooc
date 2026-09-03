"""Independently draw M-A18 P0 enemy action frames on native 64px grids."""
import json
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "normalized" / "enemy_actions"
QA = ROOT / "QA" / "enemy_actions"
C = {"ink": (24, 23, 24, 255), "coal": (56, 57, 62, 255), "stone": (91, 91, 95, 255),
     "leather": (98, 61, 40, 255), "red": (161, 52, 35, 255), "cyan": (51, 201, 210, 255),
     "pale": (188, 182, 164, 255), "ember": (224, 105, 35, 255)}

def box(d, xy, c): d.rectangle(xy, fill=C[c])
def line(d, xy, c, w=2): d.line(xy, fill=C[c], width=w)
def outline(d, xy): d.rectangle(xy, outline=C["ink"], width=2)

def humanoid(d, shift=0, narrow=False):
    """Four readable tactical masses with face, shoulders, arms and planted boots."""
    x = 29 + shift; w = 7 if narrow else 9
    # head: hair silhouette, face plane, one eye and jaw.
    box(d, (x-6, 11, x+5, 22), "ink"); box(d, (x-4, 13, x+5, 21), "pale")
    box(d, (x-4, 11, x+5, 14), "coal"); box(d, (x+2, 16, x+3, 17), "ink")
    box(d, (x+3, 20, x+5, 21), "leather")
    # broad torso, crossed harness, shoulder pads and arms.
    box(d, (x-w, 23, x+w, 41), "ink"); box(d, (x-w+2, 25, x+w-2, 40), "coal")
    box(d, (x-w-3, 25, x-w+1, 34), "stone"); box(d, (x+w-1, 25, x+w+3, 34), "stone")
    line(d, (x-w+2, 26, x+w-2, 39), "leather", 2); line(d, (x+w-2, 26, x-w+2, 39), "leather", 1)
    box(d, (x-w-4, 34, x-w+1, 42), "stone"); box(d, (x+w-1, 34, x+w+4, 42), "stone")
    # belt, split legs, knees and boots on common baseline.
    box(d, (x-w+1, 39, x+w-1, 42), "leather")
    box(d, (x-8, 43, x-1, 55), "stone"); box(d, (x+2, 43, x+9, 55), "stone")
    box(d, (x-8, 48, x-1, 50), "coal"); box(d, (x+2, 48, x+9, 50), "coal")
    box(d, (x-9, 55, x, 58), "ink"); box(d, (x+1, 55, x+11, 58), "ink")
    box(d, (x-8, 55, x-1, 56), "leather"); box(d, (x+2, 55, x+8, 56), "leather")

def mauler(i):
    im=Image.new("RGBA",(64,64),(0,0,0,0)); d=ImageDraw.Draw(im); humanoid(d)
    # Hammer moves from raised charge to grounded impact and then recoils.
    heads=[(43,14),(44,16),(44,22),(43,35),(42,30),(42,24)]
    hx,hy=heads[i]; line(d, (34,33,hx,hy), "leather", 3); box(d,(hx-6,hy-4,hx+6,hy+4),"stone"); outline(d,(hx-6,hy-4,hx+6,hy+4))
    if i in (0,1,2): line(d,(hx-4,hy,hx+4,hy),"cyan",1)
    if i in (3,4):
        for x in (20,25,31,37,43): line(d,(x,57,x+2,53),"ember",1)
    return im

def mender(i):
    im=Image.new("RGBA",(64,64),(0,0,0,0)); d=ImageDraw.Draw(im); humanoid(d,narrow=True)
    coilx=[20,21,24,29,33,35][i]; line(d,(31,31,coilx,27),"leather",2); box(d,(coilx-3,23,coilx+3,30),"pale"); outline(d,(coilx-3,23,coilx+3,30))
    line(d,(coilx+3,26,45,25),"cyan",1)
    if i>=2:
        box(d,(43,22,47,28),"cyan")
    if i>=3:
        line(d,(47,25,55,25),"cyan",2)
    return im

def hound(i):
    im=Image.new("RGBA",(64,64),(0,0,0,0)); d=ImageDraw.Draw(im)
    dx=[0,1,3,5,3,1][i]; y=[35,34,32,30,33,35][i]
    box(d,(17+dx,y,40+dx,y+11),"ink"); box(d,(19+dx,y+2,39+dx,y+9),"coal")
    box(d,(21+dx,y+3,30+dx,y+5),"stone"); box(d,(38+dx,y+2,50+dx,y+10),"ink"); box(d,(39+dx,y+4,48+dx,y+9),"stone")
    box(d,(46+dx,y+5,47+dx,y+6),"pale"); box(d,(49+dx,y+8,52+dx,y+10),"ink")
    # Four planted legs keep a shared foot baseline at 58.
    for lx in (20+dx,27+dx,34+dx,39+dx):
        box(d,(lx,y+10,lx+4,56),"stone"); box(d,(lx-1,56,lx+5,58),"ink")
    line(d,(18+dx,y+5,10+dx,y-1),"leather",2); box(d,(7+dx,y-3,12+dx,y+2),"pale")
    line(d,(17+dx,y+8,10+dx,y+12),"leather",2)
    if i in (2,3,4):
        # tether ring tightens near the bite line.
        d.ellipse((47+dx,y-2,55+dx,y+6), outline=C["cyan"], width=2)
    return im

DRAWERS={"sigil_mauler":mauler,"barrier_mender":mender,"tether_hound":hound}

def main():
    report={"status":"PASS","families":{}}
    for name, drawer in DRAWERS.items():
        folder=OUT/name; qaf=QA/name; folder.mkdir(parents=True,exist_ok=True); qaf.mkdir(parents=True,exist_ok=True)
        contact=Image.new("RGBA",(768,128),(28,28,28,255)); frames=[]
        for i in range(6):
            im=drawer(i); im.save(folder/f"frame_{i:02d}.png"); preview=im.resize((128,128),Image.Resampling.NEAREST); preview.save(qaf/f"frame_{i:02d}_4x.png"); contact.alpha_composite(preview,(i*128,0))
            alpha=set(im.getchannel("A").getdata()); colors={v[:3] for v in im.getdata() if v[3]}; frames.append({"frame":i,"size":[64,64],"hardAlpha":alpha.issubset({0,255}),"colors":len(colors),"bounds":list(im.getchannel("A").getbbox())})
        contact.save(qaf/f"{name}_contact_4x.png"); report["families"][name]=frames
    (QA/"p0_enemy_actions_report.json").write_text(json.dumps(report,indent=2),encoding="utf-8")

if __name__=="__main__": main()
