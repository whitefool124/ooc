"""Produce the authorized relaxed-normalization M-A18 bulk pixel asset set."""
import json, math
from pathlib import Path
from PIL import Image, ImageDraw

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'normalized'; QA=ROOT/'QA'; REPORT=[]
INK=(26,25,26,255); STONE=(126,122,112,255); LIGHT=(174,168,151,255); DARK=(75,73,70,255)
WOOD=(105,66,39,255); COPPER=(166,96,48,255); CYAN=(42,191,200,255); RED=(156,48,35,255); GOLD=(232,157,50,255)
FIRE=[(82,26,24,255),(151,45,24,255),(218,80,24,255),(246,142,35,255),(255,225,145,255)]

def save(im,path,limit,kind):
    path.parent.mkdir(parents=True,exist_ok=True); im.save(path)
    a=set(im.getchannel('A').getdata()); colors=len({p[:3] for p in im.getdata() if p[3]}); bounds=im.getchannel('A').getbbox()
    ok=im.size in ((32,32),(64,64)) and a.issubset({0,255}) and colors<=limit
    REPORT.append({'id':path.stem if kind!='frame' else path.parent.name+'/'+path.stem,'kind':kind,'size':list(im.size),'hardAlpha':a.issubset({0,255}),'colors':colors,'limit':limit,'bounds':list(bounds) if bounds else None,'status':'PASS' if ok else 'WARN'})
def box(d,xy,c): d.rectangle(xy,fill=c)
def line(d,xy,c,w=1): d.line(xy,fill=c,width=w)

def floor_tile(family,variant):
    im=Image.new('RGBA',(32,32),LIGHT); d=ImageDraw.Draw(im); v=ord(variant)-97
    if family=='academy_stone_road':
        box(d,(0,0,31,31),STONE); line(d,(0,9+v,31,9+v),DARK,2); line(d,(0,22-v,31,22-v),DARK,2); line(d,(8+v*3,0,8+v*3,9+v),DARK)
    elif family=='academy_courtyard':
        line(d,(0,15,31,15),DARK,2); line(d,(15+v%2,0,15+v%2,15),DARK,2); box(d,(2+v*4,25,5+v*4,28),STONE)
    elif family=='academy_ruins':
        box(d,(0,0,31,31),STONE); line(d,(0,12,31,12),DARK,2); line(d,(13,0,13,12),DARK,2); line(d,(4+v*4,18,11+v*4,24),DARK); line(d,(6+v*3,17,4+v*3,21),LIGHT)
    elif family=='academy_aether_inlay':
        box(d,(0,0,31,31),STONE); line(d,(0,15,31,15),DARK,2); line(d,(15,0,15,31),COPPER,2); line(d,(15,8+v*4,15,13+v*4),CYAN)
    elif family=='academy_packed_earth':
        box(d,(0,0,31,31),(119,91,61,255));
        for k in range(6): box(d,((k*7+v*3)%31,(k*5+v*4)%31,(k*7+v*3)%31+1,(k*5+v*4)%31+1),DARK)
    elif family=='academy_grass_edge':
        box(d,(0,0,31,31),(112,87,60,255)); dirs={'n':(0,0,31,7),'e':(24,0,31,31),'s':(0,24,31,31),'w':(0,0,7,31)}; box(d,dirs[variant],(76,107,63,255))
    return im

def prop(asset):
    im=Image.new('RGBA',(32,32),(0,0,0,0)); d=ImageDraw.Draw(im); state=asset.rsplit('_',1)[-1]
    if 'bench' in asset:
        box(d,(4,15,27,22),INK); box(d,(6,14,25,19),STONE); box(d,(7,22,10,27),DARK); box(d,(22,22,25,27),DARK)
    elif 'planter' in asset:
        box(d,(5,15,26,27),INK); box(d,(7,17,24,25),STONE); box(d,(9,10,22,16),(72,105,62,255))
    elif 'archive_stack' in asset:
        box(d,(5,5,27,29),INK); box(d,(7,7,25,27),WOOD); line(d,(8,13,24,13),LIGHT,2); line(d,(8,20,24,20),LIGHT,2)
    elif 'masonry_screen' in asset:
        box(d,(3,6,28,29),INK); box(d,(5,8,26,27),STONE); line(d,(5,17,26,17),DARK,2); line(d,(15,8,15,27),DARK,2)
    elif 'aether_pillar' in asset:
        box(d,(9,3,23,29),INK); box(d,(11,5,21,27),STONE); line(d,(16,8,16,23),COPPER,3); line(d,(16,11,16,19),CYAN)
    elif 'seal_plinth' in asset:
        box(d,(5,20,27,29),INK); box(d,(8,10,24,22),STONE); line(d,(11,16,21,16),COPPER,2); line(d,(16,12,16,20),CYAN)
    elif 'loot_chest' in asset:
        box(d,(5,11,27,28),INK); box(d,(7,13,25,26),WOOD); line(d,(7,18,25,18),COPPER,2); box(d,(15,17,18,22),GOLD)
        if state=='open': box(d,(7,6,25,13),WOOD)
        if state=='empty': box(d,(9,14,23,24),DARK)
    if state in ('damaged','open'): line(d,(9,10,23,25),RED,2)
    if state in ('rubble','empty'):
        im=Image.new('RGBA',(32,32),(0,0,0,0)); d=ImageDraw.Draw(im); box(d,(5,23,13,28),DARK); box(d,(15,20,23,27),STONE); box(d,(24,25,28,29),INK)
    return im

def connector(kind):
    im=Image.new('RGBA',(32,32),STONE); d=ImageDraw.Draw(im); line(d,(16,0,16,31),COPPER,3); line(d,(16,4,16,28),CYAN)
    if kind in ('corner','tee','cross'): line(d,(16,16,31,16),COPPER,3); line(d,(18,16,28,16),CYAN)
    if kind in ('tee','cross'): line(d,(0,16,16,16),COPPER,3); line(d,(4,16,14,16),CYAN)
    if kind=='corner': box(d,(0,0,15,15),STONE)
    return im

def unit_frame(name,i):
    im=Image.new('RGBA',(64,64),(0,0,0,0)); d=ImageDraw.Draw(im); x=32; phase=[0,-1,-2,0,1,0][i]
    # common human silhouette; hound is distinct.
    if name=='tether_hound': return im
    box(d,(26,10+phase,37,21+phase),INK); box(d,(28,13+phase,36,20+phase),LIGHT); box(d,(33,15+phase,34,16+phase),INK)
    box(d,(21,22+phase,43,42+phase),INK); box(d,(24,25+phase,40,40+phase),DARK); line(d,(24,26+phase,40,39+phase),RED,2)
    box(d,(23,41+phase,30,56),STONE); box(d,(34,41+phase,41,56),STONE); box(d,(21,55,31,58),INK); box(d,(33,55,44,58),INK)
    if name in ('shieldguard','elite_vanguard','core_overseer'): box(d,(14,24+phase,23,48+phase),STONE)
    if name in ('pyromancer','barrier_mender','purifier_overseer','lantern_revealer'): line(d,(41,23+phase,48+i,13+phase-i),COPPER,3)
    if name in ('raider','stone_snare'): line(d,(18-i,33+phase,9,25+i),WOOD,3)
    if name in ('rune_arbalist',): line(d,(41,30+phase,56,26+i),WOOD,3); line(d,(44,24,44,34),COPPER)
    if name in ('elite_vanguard','core_overseer'): box(d,(38,21+phase,54,30+phase),STONE)
    if name=='lantern_revealer': box(d,(45,12+phase,52,20+phase),GOLD)
    if name=='stone_snare': box(d,(8,21+i,13,27+i),STONE)
    if name=='pyromancer': box(d,(46+i,11+phase-i,50+i,15+phase-i),FIRE[3])
    if name=='purifier_overseer': box(d,(44,17+phase,49,25+phase),CYAN)
    if name=='core_overseer': box(d,(46,20+phase,58,29+phase),STONE)
    return im

def vfx(name,i):
    im=Image.new('RGBA',(32,32),(0,0,0,0)); d=ImageDraw.Draw(im); t=[1,2,3,4,3,2][i]; cx=16; cy=16
    def flame(x,y,s):
        for yy in range(-s,s+1):
            for xx in range(-s,s+1):
                if abs(xx)+abs(yy)<=s+1: im.putpixel((x+xx,y+yy),FIRE[min(4,max(1,s-abs(xx)-abs(yy)+1))])
    if name in ('fire_impact','fire_detonate'):
        for a in range(0,360,45): line(d,(cx,cy,cx+int(math.cos(math.radians(a))*(3+t*2)),cy+int(math.sin(math.radians(a))*(3+t*2))),FIRE[2],2); flame(cx,cy,min(4,t))
    elif name in ('fire_projectile','fire_line'):
        flame(10+i*2,16,t if t<4 else 3); line(d,(3,16,9+i*2,16),FIRE[1],2)
    elif name=='fire_melee_arc': d.arc((5,5,27,27),210-i*8,330-i*8,fill=FIRE[3],width=2+t//2)
    elif name=='fire_attachment': line(d,(10,23,22,9),FIRE[1+i//2],2+t//2)
    elif name=='fire_spray':
        for y in range(13-t,20+t): line(d,(8,16,24,y),FIRE[2+(y%2)],1)
    elif name=='fire_cross_blast': line(d,(16,4,16,28),FIRE[3],t); line(d,(4,16,28,16),FIRE[3],t); flame(16,16,2)
    elif name=='fire_burning_ground':
        for x in (8,16,24): flame(x,23-(i+x)%3,2)
    elif name=='fire_wall':
        for x in range(5,28,5): flame(x,23-(x+i)%5,3)
    elif name=='fire_absorb':
        for a in range(0,360,60): line(d,(16+int(math.cos(math.radians(a))*10),16+int(math.sin(math.radians(a))*10),16,16),FIRE[2],1); flame(16,16,t//2+1)
    elif name=='fire_break_stance': line(d,(7,8,13,15,10,20,18,25,24,19),FIRE[1+i//2],2)
    elif name=='fire_overlimit': flame(16,25-i*2,min(4,t)); line(d,(16,27,16,11-i),FIRE[2],2)
    return im

def main():
    for fam,variants in {'academy_stone_road':'abcd','academy_courtyard':'bcd','academy_ruins':'abcd','academy_aether_inlay':'abcd','academy_packed_earth':'abc'}.items():
        for v in variants: save(floor_tile(fam,v),OUT/'terrain'/f'{fam}_{v}.png',8,'terrain')
    for v in 'nesw': save(floor_tile('academy_grass_edge',v),OUT/'terrain'/f'academy_grass_edge_{v}.png',8,'terrain')
    for fam in ('academy_light_stone_bench','academy_light_planter','academy_heavy_archive_stack','academy_heavy_masonry_screen','academy_aether_pillar','academy_seal_plinth'):
        for s in ('intact','damaged','rubble'): save(prop(f'{fam}_{s}'),OUT/'terrain'/f'{fam}_{s}.png',8,'terrain')
    for s in ('closed','open','empty'): save(prop(f'academy_loot_chest_{s}'),OUT/'terrain'/f'academy_loot_chest_{s}.png',8,'terrain')
    for k in ('straight','corner','tee','cross'): save(connector(k),OUT/'terrain'/f'academy_aether_line_{k}.png',8,'terrain')
    for name in ('shieldguard','pyromancer','raider','elite_vanguard','stone_snare','lantern_revealer','rune_arbalist','core_overseer','purifier_overseer'):
        for i in range(6): save(unit_frame(name,i),OUT/'enemy_actions'/name/f'frame_{i:02d}.png',24,'frame')
    for name in ('fire_projectile','fire_impact','fire_melee_arc','fire_attachment','fire_spray','fire_line','fire_cross_blast','fire_detonate','fire_burning_ground','fire_wall','fire_absorb','fire_break_stance','fire_overlimit'):
        for i in range(6): save(vfx(name,i),OUT/'vfx'/name/f'frame_{i:02d}.png',12,'frame')
    QA.mkdir(parents=True,exist_ok=True); (QA/'ma18_bulk_report.json').write_text(json.dumps({'status':'PASS' if all(x['status']=='PASS' for x in REPORT) else 'WARN','assetFiles':len(REPORT),'entries':REPORT},indent=2),encoding='utf-8')
    print(json.dumps({'files':len(REPORT),'warn':sum(x['status']!='PASS' for x in REPORT)}))
if __name__=='__main__': main()
