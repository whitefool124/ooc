"""Presentation contact sheet of untouched runtime captures; not new game art."""
import json,sys
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
root=Path(__file__).resolve().parent
name=sys.argv[1] if len(sys.argv)>1 else 'surface_variety_families'
if name not in {'surface_variety_families','air_depth_families','surface_zone_families','spatial_frame_families','native_period_families','contact_shadow_families'}:raise ValueError('Known capture set required')
folder=root/name
families=[('AcademyCourt','学院庭院'),('Conservatory','温室'),('StoneRoad','石道'),('RuinSite','遗址'),('InteriorHall','室内厅堂'),('AetherTower','以太塔')]
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',21)
sheet=Image.new('RGB',(1920,812),(24,26,27));draw=ImageDraw.Draw(sheet)
draw.text((16,8),'六类战场当前材质样板 · 1920×1080 原图的缩略总览 · 人工审阅待定',font=font,fill=(239,234,219))
records=[]
for i,(family,label) in enumerate(families):
    capture=folder/family/'day.png';record=json.loads((capture.parent/'capture.json').read_text(encoding='utf-8-sig'))
    assert record['environment']==family and record['resolution']==[1920,1080]
    image=Image.open(capture).convert('RGB');assert image.size==(1920,1080)
    x=(i%3)*640;y=48+(i//3)*382
    draw.text((x+12,y),label+' · '+str(record['peripheralSurfaceVariants'])+'种外围材质',font=font,fill=(234,230,218))
    sheet.paste(image.resize((640,360),Image.Resampling.LANCZOS),(x,y+28))
    records.append(dict(family=family,level=record['level'],capture=str(capture.relative_to(root)),peripheral_variants=record['peripheralSurfaceVariants'],foliage_materials=record['foliageMaterials']))
sheet.save(folder/'six_family_overview.png')
(folder/'overview.json').write_text(json.dumps(dict(status='QA_PENDING',source='Unchanged runtime PNGs arranged as presentation thumbnails; inspect originals for pixel/art review',families=records),ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(records))
