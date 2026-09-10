"""User-authorized background/palette/anchor normalization of independent pose sources."""
import hashlib
import importlib.util
import json
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageOps

ROOT=Path(__file__).resolve().parents[3]
HERE=Path(__file__).resolve().parent
OUT=ROOT/'Artifacts/OCC_HoundMotionReview_20260905'
BASE=ROOT/'Artifacts/OCC_UnitSilhouetteReview_20260905/processed/tether_hound_compact_v1.png'
NEAREST=Image.Resampling.NEAREST
# All three raw poses are 1254px square, matching the independent standing raw.
# One shared scale from the standing source width; never fit each pose to its own maximum box.
SCALE=40/759
POSES=('sniff','crouch','contact')
helper_path=ROOT/'Worldbuilding/归档/2026-09-05_寻迹兽轮廓返修/normalize_candidate.py'
spec=importlib.util.spec_from_file_location('standing_qa',helper_path)
helper=importlib.util.module_from_spec(spec); spec.loader.exec_module(helper)


def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,obj): p.write_text(json.dumps(obj,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')


def main():
    standing=Image.open(BASE).convert('RGBA')
    palette=sorted(set(p[:3] for p in standing.get_flattened_data() if p[3]))
    # One additional warm tooth color, sampled from the contact source's authored fang highlight.
    tooth=(189,168,135)
    palette.append(tooth)
    pal=np.array(palette,dtype=np.int32)
    frames={'standing':standing}
    report={'scale':SCALE,'scale_policy':'same factor for all 1254px raw canvases; pose boxes are not independently fitted',
            'canvas_anchor':[32,58],'palette':palette,'standing':helper.metrics(standing),'poses':{}}
    for pose in POSES:
        source=OUT/'raw'/f'{pose}_v1.png'
        im=Image.open(source).convert('RGB'); assert im.size==(1254,1254)
        a=np.array(im); rgb=a.astype(np.int16)
        background=(rgb.min(2)>=210)&((rgb.max(2)-rgb.min(2))<=35)
        rgba=np.dstack((a,np.where(background,0,255).astype(np.uint8))); rgba[background,:3]=0
        masked=Image.fromarray(rgba); bounds=masked.getchannel('A').getbbox(); cropped=masked.crop(bounds)
        size=(round(cropped.width*SCALE),round(cropped.height*SCALE))
        p=np.array(cropped.resize(size,NEAREST)); opaque=p[:,:,3]>0
        colors=p[opaque,:3].astype(np.int32)
        distances=((colors[:,None,:]-pal[None,:,:])**2).sum(2)
        p[opaque,:3]=pal[np.argmin(distances,axis=1)].astype(np.uint8); p[~opaque]=0
        out=Image.new('RGBA',(64,64)); offset=(round(32-size[0]/2),58-size[1])
        out.alpha_composite(Image.fromarray(p),offset)
        delivered=OUT/'processed'/f'{pose}_v1.png'; out.save(delivered); frames[pose]=out
        out.save(OUT/'qa'/f'{pose}_1x.png'); out.resize((256,256),NEAREST).save(OUT/'qa'/f'{pose}_4x.png')
        gray=ImageOps.grayscale(out).convert('RGBA'); gray.putalpha(out.getchannel('A'))
        gray.resize((256,256),NEAREST).save(OUT/'qa'/f'{pose}_gray.png')
        check=helper.checker((64,64)); check.alpha_composite(out)
        check.resize((256,256),NEAREST).save(OUT/'qa'/f'{pose}_checker.png')
        helper.contact(out,2,True)[0].save(OUT/'qa'/f'{pose}_contact_offline.png')
        metrics=helper.metrics(out); metrics.update(source_bounds=list(bounds),resized_box=list(size),offset=list(offset))
        metrics['opaque_area_ratio_to_standing']=metrics['opaque_pixels']/report['standing']['opaque_pixels']
        report['poses'][pose]=metrics
        path=OUT/f'{pose}.occ-art.json'; manifest=json.loads(path.read_text(encoding='utf-8'))
        manifest['status']='QA_PENDING'
        manifest['delivery'].update(output_path=delivered.relative_to(ROOT).as_posix(),output_sha256=sha(delivered))
        manifest['evidence']={key:(OUT/'qa'/f'{pose}_{suffix}.png').relative_to(ROOT).as_posix() for key,suffix in
            {'one_x':'1x','four_x':'4x','grayscale':'gray','checker':'checker','application_contact':'contact_offline'}.items()}
        manifest['human_review']['notes']='Independent motion candidate only. Same raw scale and shared standing palette plus one tooth highlight. Application contact currently an external reconstruction; no Unity adoption or human approval.'
        manifest['processing']={'authorization':'允许脚本规范化审核候选','script':Path(__file__).relative_to(ROOT).as_posix(),
            'script_sha256':sha(Path(__file__)),'shared_source_scale':SCALE,'source_canvas':[1254,1254],
            'palette_base_sha256':sha(BASE),'palette':palette,'canvas_anchor':[32,58],
            'alignment_note':'integer placement about X32; odd silhouette widths can have a half-pixel bounds-center offset, while canvas pivot remains X32'}
        write(path,manifest)
    for name,order,durations in (
        ('sniff_preview',['standing','sniff','standing'],[420,280,420]),
        ('bite_preview',['standing','crouch','contact','crouch','standing'],[300,120,100,140,300])):
        previews=[]
        for pose in order:
            bg=helper.checker((64,64)); bg.alpha_composite(frames[pose])
            previews.append(bg.resize((256,256),NEAREST).convert('RGB'))
        previews[0].save(OUT/'qa'/f'{name}.gif',save_all=True,append_images=previews[1:],duration=durations,loop=0,disposal=2,optimize=False)
        report[name]={'order':order,'durations_ms':durations,'scope':'pose timing review only, not final smooth animation or combat hit timing'}
    font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',18); small=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',14)
    sheet=Image.new('RGB',(1120,660),(22,29,32)); d=ImageDraw.Draw(sheet)
    d.text((20,16),'寻迹兽 · 统一尺度与调色板的关键姿态审核',font=font,fill='#e0ded0')
    labels={'standing':'站姿 · 方向已认可','sniff':'嗅探 · 待审核','crouch':'蓄力 · 待审核','contact':'扑咬接触 · 待审核'}
    strip=Image.new('RGBA',(256,64))
    for i,pose in enumerate(('standing',)+POSES):
        x=16+i*278; im=frames[pose]; bg=helper.checker((64,64)); bg.alpha_composite(im)
        d.text((x,52),labels[pose],font=small,fill='#c2c9c5')
        sheet.paste(bg.resize((256,256),NEAREST).convert('RGB'),(x,82))
        gray=ImageOps.grayscale(im).convert('RGBA'); gray.putalpha(im.getchannel('A'))
        sheet.paste(gray.resize((192,192),NEAREST),(x+24,376),gray.resize((192,192),NEAREST))
        d.text((x,350),'1×',font=small,fill='#c2c9c5'); sheet.paste(im,(x+32,336),im)
        strip.alpha_composite(im,(i*64,0))
    d.text((20,594),'所有动作共用40/759缩放；保留64×64画布、地面基准Y58与中心锚点X32，不按各自包围盒拉伸。',font=small,fill='#c2c9c5')
    d.text((20,621),'关键姿态与节奏预览，非成品连续动画；应用接触、体积连续性与人工审美仍待审核。',font=small,fill='#c2c9c5')
    sheet.save(OUT/'qa/pose_review.png'); strip.save(OUT/'qa/pose_strip.png')
    write(HERE/'normalization-report.json',report)
    print(json.dumps(report['poses'],ensure_ascii=False,indent=2))


if __name__=='__main__': main()
