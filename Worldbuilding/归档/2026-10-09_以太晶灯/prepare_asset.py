from pathlib import Path
from PIL import Image
import json,hashlib
root=Path('Worldbuilding/归档/2026-10-09_以太晶灯')
im=Image.open(root/'native-02.png').convert('RGBA')
b=im.getbbox()
out=im.crop((b[0]-1,b[1]-1,b[2]+1,b[3]+1));out.save(root/'delivery.png')
out.resize((out.width*4,out.height*4),Image.Resampling.NEAREST).save(root/'4x.png')
gray=out.convert('LA').convert('RGBA');gray.save(root/'gray.png')
checker=Image.new('RGBA',out.size)
for y in range(out.height):
 for x in range(out.width):
  c=210 if ((x//4+y//4)%2) else 90
  checker.putpixel((x,y),(c,c,c,255))
checker.alpha_composite(out);checker.save(root/'checker.png')
m=json.loads(Path('Worldbuilding/归档/2026-10-09_HD2D边框与灯光/台灯/manifest.json').read_text(encoding='utf-8'))
m['asset_id']='hd2d_academy_aether_crystal_lamp'
for key,file in [('source_path','source-02.png')]:m['provenance'][key]=str(root/file).replace('\\','/')
m['provenance']['source_sha256']=hashlib.sha256((root/'source-02.png').read_bytes()).hexdigest()
for k in ['output_path','native_output_path']:m['delivery'][k]=str(root/'delivery.png').replace('\\','/')
m['delivery']['output_sha256']=hashlib.sha256((root/'delivery.png').read_bytes()).hexdigest()
m['evidence']={k:str(root/f).replace('\\','/') for k,f in [('one_x','delivery.png'),('four_x','4x.png'),('grayscale','gray.png'),('checker','checker.png')]}
m['evidence']['application_contact']='UnityProject/ReviewEvidence/MainMenuE/aether-lamp-20261009.png'
m['decoder']=json.loads((root/'qa-02.json').read_text(encoding='utf-8'))
(root/'manifest.json').write_text(json.dumps(m,ensure_ascii=False,indent=2),encoding='utf-8')
print('native cropped delivery',out.size)
