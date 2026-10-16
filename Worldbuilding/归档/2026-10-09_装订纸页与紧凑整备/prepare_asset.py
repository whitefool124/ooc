from pathlib import Path
from PIL import Image
import json,hashlib
root=Path('Worldbuilding/归档/2026-10-09_装订纸页与紧凑整备')
im=Image.open(root/'delivery.png').convert('RGBA')
im.resize((im.width*4,im.height*4),Image.Resampling.NEAREST).save(root/'4x.png')
im.convert('LA').convert('RGBA').save(root/'gray.png')
bg=Image.new('RGBA',im.size)
for y in range(im.height):
 for x in range(im.width):
  c=210 if (x//4+y//4)%2 else 90
  bg.putpixel((x,y),(c,c,c,255))
bg.alpha_composite(im);bg.save(root/'checker.png')
m=json.loads(Path('Worldbuilding/归档/2026-10-09_以太晶灯/manifest.json').read_text(encoding='utf-8'))
m['asset_id']='hd2d_bound_parchment_nine_slice'
m['provenance']['source_path']=str(root/'source-01.png').replace('\\','/')
m['provenance']['source_sha256']=hashlib.sha256((root/'source-01.png').read_bytes()).hexdigest()
for k in ['output_path','native_output_path']:m['delivery'][k]=str(root/'delivery.png').replace('\\','/')
m['delivery']['output_sha256']=hashlib.sha256((root/'delivery.png').read_bytes()).hexdigest()
m['delivery']['palette_max']=8
m['application']['runtime_draw_rect']='One complete 32x32 card, fixed five-pixel slice borders, tiled strips and paper fill; same physical rim width regardless of canvas scale'
m['evidence']={k:str(root/f).replace('\\','/') for k,f in [('one_x','delivery.png'),('four_x','4x.png'),('grayscale','gray.png'),('checker','checker.png')]}
m['evidence']['application_contact']='UnityProject/ReviewEvidence/MainMenuE/compact-bound-equipment-20261009.png'
m['decoder']=json.loads((root/'qa-01.json').read_text(encoding='utf-8'))
(root/'manifest.json').write_text(json.dumps(m,ensure_ascii=False,indent=2),encoding='utf-8')
print('prepared',im.size)

