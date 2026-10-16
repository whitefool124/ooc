from pathlib import Path
from PIL import Image
import json,hashlib
root=Path('Worldbuilding/归档/2026-10-09_简洁纸边')
native=Image.open(root/'native-02.png').convert('RGBA')
b=native.getbbox();im=native.crop((b[0]-1,b[1]-1,b[2]+1,b[3]+1));im.save(root/'delivery.png')
im.resize((im.width*4,im.height*4),Image.Resampling.NEAREST).save(root/'4x.png')
im.convert('LA').convert('RGBA').save(root/'gray.png')
bg=Image.new('RGBA',im.size)
for y in range(im.height):
 for x in range(im.width):
  c=210 if (x//4+y//4)%2 else 90
  bg.putpixel((x,y),(c,c,c,255))
bg.alpha_composite(im);bg.save(root/'checker.png')
m=json.loads(Path('Worldbuilding/归档/2026-10-09_装订纸页与紧凑整备/manifest.json').read_text(encoding='utf-8'))
m['asset_id']='hd2d_clean_paper_edge'
m['provenance']['source_path']=str(root/'source-02.png').replace('\\','/')
m['provenance']['source_sha256']=hashlib.sha256((root/'source-02.png').read_bytes()).hexdigest()
for k in ['output_path','native_output_path']:m['delivery'][k]=str(root/'delivery.png').replace('\\','/')
m['delivery']['output_sha256']=hashlib.sha256((root/'delivery.png').read_bytes()).hexdigest()
m['delivery']['palette_max']=3
m['application']['runtime_draw_rect']='One low contrast paper card, 4px slice border and tile strips, same edge for documents and buttons; resource metrics without separate boxes'
m['evidence']={k:str(root/f).replace('\\','/') for k,f in [('one_x','delivery.png'),('four_x','4x.png'),('grayscale','gray.png'),('checker','checker.png')]}
m['evidence']['application_contact']='UnityProject/ReviewEvidence/MainMenuE/clean-edge-equipment-20261009.png'
m['decoder']=json.loads((root/'qa-02.json').read_text(encoding='utf-8'))
(root/'manifest.json').write_text(json.dumps(m,ensure_ascii=False,indent=2),encoding='utf-8')
print(im.size)

