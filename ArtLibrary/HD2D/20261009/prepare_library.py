"""Package independent generated originals; no art synthesis or recolouring.

User-authorized noncombat display workflow, 2026-10-09. Originals stay intact.
"""
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageOps

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
batch = json.loads((HERE / 'batch-manifest.json').read_text(encoding='utf-8-sig'))
font_path = 'C:/Windows/Fonts/msyh.ttc'
font = ImageFont.truetype(font_path, 23)
small = ImageFont.truetype(font_path, 16)
titlefont = ImageFont.truetype(font_path, 34)

def rel(p):
    return p.relative_to(ROOT).as_posix()

def digest(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()

def write_json(p, data):
    p.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

def background(size, checker=False):
    im = Image.new('RGBA', size, '#293338')
    if checker:
        draw = ImageDraw.Draw(im)
        for y in range(0, size[1], 32):
            for x in range(0, size[0], 32):
                if (x // 32 + y // 32) % 2:
                    draw.rectangle((x,y,x+31,y+31), fill='#384349')
    return im

sheet = Image.new('RGBA', (1600, 1110), '#202a2f')
draw = ImageDraw.Draw(sheet)
draw.text((36, 24), 'OCC · HD2D 可复用素材库 / 01', font=titlefont, fill='#eee6d6')
draw.text((38, 78), '纸黄 · 胡桃木 · 青蓝 · 黄铜  /  独立透明素材  /  原料已生产，待场景应用与审美验收', font=small, fill='#a9bcc1')
records = []
for index, item in enumerate(batch['items']):
    folder = HERE / item['id']
    source = folder / 'source-01.png'
    im = Image.open(source).convert('RGBA')
    alpha = im.getchannel('A')
    # Imagegen returns near-opaque interiors (~253); keep soft edge coverage.
    alpha = alpha.point(lambda a: 0 if a <= 8 else 255 if a >= 248 else a)
    im.putalpha(alpha)
    clear = Image.new('RGBA', im.size, (0,0,0,0))
    im.paste(clear, mask=alpha.point(lambda a: 255 if a == 0 else 0))
    bounds = alpha.getbbox()
    if not bounds:
        raise ValueError(f'Empty alpha: {source}')
    im = ImageOps.expand(im.crop(bounds), border=16, fill=(0,0,0,0))
    output = folder / 'delivery.png'
    im.save(output)
    qa = folder / 'qa'
    qa.mkdir(exist_ok=True)
    im.resize((im.width*4, im.height*4), Image.Resampling.NEAREST).save(qa / 'four-x.png')
    gray = ImageOps.grayscale(im).convert('RGBA')
    gray.putalpha(im.getchannel('A'))
    gray.save(qa / 'grayscale.png')
    bg = background(im.size, True)
    bg.alpha_composite(im)
    bg.convert('RGB').save(qa / 'checker.png')
    thumb = im.copy()
    thumb.thumbnail((740, 390), Image.Resampling.LANCZOS)
    thumb.save(folder / 'preview.png')
    cx, cy = 28 + (index%2)*786, 122 + (index//2)*243
    cell = background((758, 220))
    pic = im.copy()
    pic.thumbnail((490, 202), Image.Resampling.LANCZOS)
    cell.alpha_composite(pic, (12+(490-pic.width)//2, (220-pic.height)//2))
    d = ImageDraw.Draw(cell)
    d.text((520, 63), item['title'], font=font, fill='#eee6d6')
    d.text((520, 105), f'{im.width} × {im.height}', font=small, fill='#99b0b7')
    d.text((520, 139), 'RGBA · 可复用', font=small, fill='#99b0b7')
    sheet.alpha_composite(cell,(cx,cy))
    manifest = json.loads((folder / 'manifest.json').read_text(encoding='utf-8-sig'))
    manifest['provenance'].update(source_sha256=digest(source), generation_attempt=1,
        style_reference=None if item['id']=='dossier' else rel(HERE/'dossier/source-01.png'),
        source_descriptor='Independent Codex builtin imagegen; dossier is material/style reference only.')
    if item['id']!='dossier':
        manifest['prompt'] += '\nThe provided open dossier is STYLE REFERENCE ONLY: match its cream/walnut/desaturated teal/brass materials, lighting softness, edge treatment and finish. Produce only the new described object, not another book.'
    manifest['delivery'].update(output_sha256=digest(output), size=list(im.size),
        alpha_policy='transparent_soft_alpha', native_anchor=[im.width//2,im.height-16],
        processing={'alpha_zero_at_or_below':8,'alpha_opaque_at_or_above':248,
            'zero_hidden_rgb':True,'crop_source_bbox':list(bounds),'transparent_padding_px':16,
            'delivery_resized':False,'palette_quantized':False,'art_redrawn':False})
    manifest['application']={'rendering_mode':'noncombat_hd2d_layered_sprite',
        'camera_projection':'perspective','runtime_draw_rect':'Pending Unity application; camera-parallel sprite, preserve aspect ratio; catalog uses proportional display.',
        'current_evidence_scope':'library display and live-text prototype only; no Unity runtime verification',
        'recommended_importer':{'filterMode':'Bilinear','mipmaps':False,'compression':'None','meshType':'FullRect'},
        'external_lighting':'Scene-owned; do not bake floor shadow into asset'}
    mount = 'wall_mounted' if item['id']=='plaque' else 'tabletop'
    baseline = 16 if mount=='wall_mounted' else im.height-16
    manifest['placement_semantics']={'application_scope':'concept_display','mount_type':mount,
        'logical_cell':None,'native_anchor':[im.width//2,baseline],
        'visible_contact_line':{'axis':'y','native_px':baseline},'depth_sort_baseline':baseline,
        'allowed_occlusion_direction':'front_to_back_by_contact_baseline',
        'screen_axis':'horizontal_vertical_locked','support_asset':'academy wall bracket' if mount=='wall_mounted' else 'shared academy desktop',
        'combined_room_shell':False,'contact_verification':'Pending physical scene placement; isolated display only'}
    manifest['evidence']={'one_x':rel(output),'four_x':rel(qa/'four-x.png'),
        'grayscale':rel(qa/'grayscale.png'),'checker':rel(qa/'checker.png'),
        'application_contact':rel(HERE/'index.html')}
    manifest['human_review']={'overall':'PENDING','application':'PENDING',
        'note':'Existing family direction is approved; these individual generated assets have not been approved or imported.'}
    write_json(folder/'manifest.json',manifest)
    item.update(source_path=rel(source),delivery_path=rel(output),source_sha256=digest(source),
        delivery_sha256=digest(output),delivery_size=list(im.size),status='QA_PENDING')
    records.append({'id':item['id'],'title':item['title'],'width':im.width,'height':im.height})

sheet.convert('RGB').save(HERE/'contact-sheet.jpg',quality=94)
batch['status']='QA_PENDING'
batch['application_status']='NOT_IMPORTED; no Unity runtime or contact proof'
write_json(HERE/'batch-manifest.json',batch)
write_json(HERE/'catalog.json',records)
print(json.dumps(records,ensure_ascii=False))
