"""Build labeled pose-timing previews from verified Unity static captures."""
from pathlib import Path
import hashlib
import json
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[3]
HERE=Path(__file__).resolve().parent
OUT=ROOT/'Artifacts/OCC_HoundMotionReview_20260905'; QA=OUT/'qa'
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',18)
small=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',14)
labels={'standing':'站姿','sniff':'嗅探','crouch':'蓄力','contact':'扑咬接触'}
panels={}
for pose in labels:
    panel=Image.new('RGB',(760,456),(22,29,32)); draw=ImageDraw.Draw(panel)
    draw.text((20,14),'寻迹兽关键姿态 · '+labels[pose],font=font,fill='#e0ded0')
    sprite=Image.open(ROOT/'Artifacts/OCC_UnitSilhouetteReview_20260905/qa/checker.png' if pose=='standing' else QA/f'{pose}_checker.png').convert('RGB')
    panel.paste(sprite,(20,70))
    actual=Image.open(QA/f'unity_{pose}_zoom_1920.png').convert('RGB')
    panel.paste(actual.crop((552,224,912,560)),(368,64))
    draw.text((20,42),'64px姿态 · 4×查看',font=small,fill='#c2c9c5')
    draw.text((368,42),'Unity放大档实际接触 · 原生裁切',font=small,fill='#c2c9c5')
    draw.text((20,366),'同一尺度和调色板',font=small,fill='#c2c9c5')
    draw.text((20,394),'角色原位，观察姿态及遮挡',font=small,fill='#c2c9c5')
    draw.text((20,426),'关键姿态节奏样片；由临时Unity静态帧制作，非实机连续动作或最终命中时序。',font=small,fill='#c2c9c5')
    panel.save(QA/f'{pose}_application.png'); panels[pose]=panel
    if pose!='standing':
        path=OUT/f'{pose}.occ-art.json'; m=json.loads(path.read_text(encoding='utf-8'))
        m['evidence']['application_contact']=(QA/f'{pose}_application.png').relative_to(ROOT).as_posix()
        m['application']['static_review']={'captures':[(QA/f'unity_{pose}_overview_960.png').relative_to(ROOT).as_posix(),(QA/f'unity_{pose}_zoom_1920.png').relative_to(ROOT).as_posix()],
            'mode':'temporary in-memory Point/Clamp candidate texture; production Presenter/View/HUD and occlusion contour',
            'assets_imported':False,'scene_saved':False,'play_mode_entered':False}
        m['human_review']['notes']='Key pose candidate. Unity static application contact captured at minimum and enlarged integer scales. Human pose/application review and continuous animation remain pending. No formal promotion.'
        path.write_text(json.dumps(m,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
# A shared preview palette prevents GIF background/palette flicker.
palette_source=Image.new('RGB',(760,456*4))
for i,panel in enumerate(panels.values()): palette_source.paste(panel,(0,456*i))
palette=palette_source.quantize(colors=256,dither=Image.Dither.NONE)
for name,order,durations in (
    ('unity_sniff_preview',['standing','sniff','standing'],[420,280,420]),
    ('unity_bite_preview',['standing','crouch','contact','crouch','standing'],[300,120,100,140,300])):
    frames=[panels[p].quantize(palette=palette,dither=Image.Dither.NONE) for p in order]
    frames[0].save(QA/f'{name}.gif',save_all=True,append_images=frames[1:],duration=durations,loop=0,disposal=2,optimize=False)
    check=Image.open(QA/f'{name}.gif'); assert check.n_frames==len(order)
old=json.loads((ROOT/'Worldbuilding/归档/2026-09-05_寻迹兽轮廓返修/final-state.json').read_text(encoding='utf-8'))
assert all(hashlib.sha256((ROOT/p).read_bytes()).hexdigest()==h for p,h in old['formal_hashes'].items())
(HERE/'application-evidence.json').write_text(json.dumps({'actual_captures':8,'formal_png_and_meta_unchanged':True,
    'gifs':'labeled pose timing from static captures, not real-time playback','human_review':'PENDING'},indent=2)+'\n',encoding='utf-8')
print('8 captures assembled; 3 pose manifests updated; 2 GIF sequences verified; formal assets unchanged.')
