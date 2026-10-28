"""Normalize independent built-in sources; no drawing or recolouring.

Adaptive noncombat library: crop, nearest-neighbour delivery sizing, Alpha cleanup,
then source/QA manifests. Source files are never changed.
"""
from pathlib import Path
from collections import deque
import hashlib, json, shutil
from PIL import Image, ImageOps

ROOT=Path(__file__).resolve().parents[2]
BASE=Path(__file__).resolve().parent
def rel(path): return path.relative_to(ROOT).as_posix()
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()

def prepare(name, filename, size, border, colour_key=False):
    source=BASE/filename
    im=Image.open(source).convert('RGBA')
    if colour_key:
        px=im.load(); visited=set(); queue=deque()
        if name in {'Clamp','ToolRoll'}:
            im.putdata([(r,g,b,0) if r>g+20 and b>g+20 else (r,g,b,a) for r,g,b,a in im.getdata()])
            px=im.load()
        for x in range(im.width): queue.extend(((x,0),(x,im.height-1)))
        for y in range(im.height): queue.extend(((0,y),(im.width-1,y)))
        while queue:
            x,y=queue.popleft()
            if (x,y) in visited or x<0 or y<0 or x>=im.width or y>=im.height: continue
            visited.add((x,y));r,g,b,a=px[x,y]
            if not(r>g+20 and b>g+20): continue
            px[x,y]=(r,g,b,0)
            queue.extend(((x-1,y),(x+1,y),(x,y-1),(x,y+1)))
    bounds=im.getchannel('A').point(lambda a:255 if a>128 else 0).getbbox()
    im=im.crop(bounds)
    im=im.resize(size,Image.Resampling.NEAREST)
    # These are solid physical carriers. Keep transparent exterior, remove only
    # accidental semitransparency on solid pixels from the generation matting.
    im.putalpha(im.getchannel('A').point(lambda a:255 if a>128 else 0))
    im.putdata([(r,g,b,a) if a else (0,0,0,0) for r,g,b,a in im.getdata()])
    im=ImageOps.expand(im,border=1,fill=(0,0,0,0))
    dest=BASE/name;dest.mkdir(exist_ok=True)
    out=dest/'delivery.png';im.save(out)
    qa=dest/'qa';qa.mkdir(exist_ok=True)
    im.save(qa/'1x.png');im.resize((im.width*4,im.height*4),Image.Resampling.NEAREST).save(qa/'4x.png')
    gray=ImageOps.grayscale(im).convert('RGBA');gray.putalpha(im.getchannel('A'));gray.save(qa/'grayscale.png')
    checker=Image.new('RGBA',im.size)
    checker.putdata([(192,192,192,255) if (x//8+y//8)%2 else (238,238,238,255) for y in range(im.height) for x in range(im.width)])
    checker.alpha_composite(im);checker.save(qa/'checker.png')
    manifest={'schema':'occ-art-manifest-v1','contract_version':1,'asset_id':'academy_workshop_'+name.lower()+'_20261023',
      'role':'hd2d_library_display','status':'QA_PENDING',
      'provenance':{'source_channel':'codex_builtin_imagegen','source_path':rel(source),'source_sha256':sha(source),
        'source_descriptor':'Independent built-in generation; academy graphic functionalist physical carrier. See brief.md and generation conversation.',
        'processing':{'crop':bounds,'nearest_size':list(size),'transparent_border':1,'alpha_cleanup':'Solid carrier threshold128, exterior preserved','exterior_magenta_flood_key':colour_key,'recolouring':False}},
      'delivery':{'output_path':rel(out),'output_sha256':sha(out),'logical_cells':None,'native_ppu':32,'palette_max':1000000,'required_color_families':[],'sprite_border':border},
      'application':{'rendering_mode':'noncombat_hd2d_layered_sprite','camera_projection':'perspective',
        'runtime_draw_rect':'Nine-sliced independent carrier, corners at1/32 world unit per authored delivery pixel; final same-frame density review pending.',
        'integration_authorization':'User explicitly authorized production and integration of academy noncombat art, 2026-10-23'},
      'evidence':{key:rel(qa/file) for key,file in [('one_x','1x.png'),('four_x','4x.png'),('grayscale','grayscale.png'),('checker','checker.png')]},
      'human_review':{'overall':'PENDING','scene_pixel_size_consistency':'PENDING','notes':'Assistant source selection is not human approval.'}}
    (dest/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(name,im.size,'crop',bounds,'sha',sha(out))

if __name__=='__main__':
    prepare('Folio','folio-source.png',(96,96),[9,9,9,9])
    prepare('Canvas','canvas-source.png',(96,96),[12,12,12,12])
    prepare('Dossier','dossier-source.png',(96,96),[5,5,5,5],True)
    prepare('Socket','socket-source.png',(32,32),[4,4,4,4],True)
    prepare('Clamp','clamp-source.png',(18,12),[0,0,0,0],True)
    prepare('ToolRoll','toolroll-source.png',(96,18),[12,3,12,3],True)
    prepare('EmptySocket','empty-socket-source.png',(32,32),[3,3,3,3],True)
