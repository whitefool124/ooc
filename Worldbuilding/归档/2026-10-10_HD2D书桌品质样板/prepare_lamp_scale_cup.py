import json, hashlib, shutil
from pathlib import Path
from PIL import Image, ImageOps
root=Path(__file__).resolve().parents[3]
folder=root/'ArtSource/desk_candidate_lamp_scale_20261010/cup'
for name in ('raw','delivery','qa'): (folder/name).mkdir(parents=True,exist_ok=True)
raw=folder/'raw/source.png'
shutil.copy2(Path('C:/Users/FNHF/.codex/generated_images/01a12387-274a-76a3-9465-c7f69fa32080/exec-0c98337c-3451-455e-aab7-465836231e33.png'),raw)
im=Image.open(raw).convert('RGBA'); original=im.size
alpha=im.getchannel('A').point(lambda a:0 if a<64 else a); im.putalpha(alpha)
bounds=alpha.getbbox()
if not bounds: raise ValueError('Empty generated alpha')
im=im.crop(bounds)
im=im.resize((128,round(im.height*128/im.width)),Image.Resampling.NEAREST)
im=ImageOps.expand(im,border=2,fill=(0,0,0,0))
delivery=folder/'delivery/cup.png'; im.save(delivery)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
rel=lambda p:p.relative_to(root).as_posix()
manifest=json.loads((root/'ArtSource/desk_candidate_coarse_20261010/cup/manifest.occ-art-manifest-v1.json').read_text(encoding='utf-8'))
manifest['asset_id']='desk_candidate_lamp_scale_20261010_cup'
manifest['provenance'].update(source_path=rel(raw),source_sha256=sha(raw),source_descriptor='Independent built-in generation r4; reference lantern pixel scale')
manifest['delivery'].update(output_path=rel(delivery),output_sha256=sha(delivery))
manifest['processing'].update(original_size=list(original),delivery_size=list(im.size),operations=[{'crop':bounds},'remove alpha below 64','nearest delivery resize; no repaint','transparent safety border'])
manifest['evidence']={'application_contact':None}
for name,q in [('one_x',im),('four_x',im.resize((im.width*4,im.height*4),Image.Resampling.NEAREST)),('grayscale',ImageOps.grayscale(im).convert('RGBA'))]:
 if name=='grayscale':q.putalpha(im.getchannel('A'))
 path=folder/('qa/'+name+'.png');q.save(path);manifest['evidence'][name]=rel(path)
checker=Image.new('RGBA',im.size)
for y in range(im.height):
 for x in range(im.width): checker.putpixel((x,y),(192,192,192,255) if (x//8+y//8)%2 else (64,64,64,255))
checker.alpha_composite(im);path=folder/'qa/checker.png';checker.save(path);manifest['evidence']['checker']=rel(path)
manifest['human_review']={'overall':'PENDING','reviewer':'','date':'','notes':'Previous cup rejected as too fine. New lantern-scale candidate; approval pending.'}
(folder/'manifest.occ-art-manifest-v1.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(delivery,im.size)
