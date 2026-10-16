from pathlib import Path
import json,hashlib,shutil
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[3];OUT=Path(__file__).resolve().parent
folder=OUT/'crystal_density_r4';GEN=Path('C:/Users/FNHF/.codex/generated_images/01a12f14-7a73-7871-b3db-2c160edcd4d0')
ident='academy_aether_crystal_intact';ref=ROOT/f'UnityProject/Assets/Game/Resources/Art/HD2DBattleCrystalCandidates/{ident}.png'
def bind(p):return dict(path=p.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(p.read_bytes()).hexdigest())
records=[]
for kind,file in [('normal','exec-f2d86370-dccc-4af9-bf55-8c9326d90fdd.png'),('regions','exec-556c3728-13f9-43bb-805d-adc2c830a976.png')]:
 raw=folder/(kind+'_raw.png');shutil.copy2(GEN/file,raw)
 im=Image.open(raw).convert('RGBA');assert im.width==im.height
 src=np.asarray(im);assert np.all(src[:,:,3]==255)
 out=np.zeros((64,64,4),np.uint8)
 for y in range(64):
  for x in range(64):
   values=src[int((y+.25)*im.height/64):int((y+.75)*im.height/64),int((x+.25)*im.width/64):int((x+.75)*im.width/64),:3].reshape(-1,3)
   median=np.median(values.astype(float),axis=0);out[y,x,:3]=values[np.argmin(((values.astype(float)-median)**2).sum(axis=1))];out[y,x,3]=255
 print(kind,out[:,:,:3].min(axis=(0,1)).tolist(),out[:,:,:3].max(axis=(0,1)).tolist())
 if kind=='normal':assert out[:,:,2].min()>=190,'Invalid shallow-normal B component; regenerate source'
 else:assert out[:,:,1].max()>=240,'Missing crystal surface region'
 dest=ROOT/f'UnityProject/Assets/Game/Resources/HD2DMaterialData/CrystalCandidates/{ident}/{kind}.png';dest.parent.mkdir(parents=True,exist_ok=True);Image.fromarray(out).save(dest)
 Image.fromarray(out).resize((1024,1024),Image.Resampling.NEAREST).save(folder/(kind+'_qa16x.png'))
 records.append(dict(kind=kind,source=bind(raw),delivery=bind(dest),reference=bind(ref),method='Known native64 UV central-cell observed RGB medoid; no albedo brightness conversion, interpolation or new RGB',status='REVIEW_READY',normal_convention='OpenGL +Y, no green flip',channels='R UNUSED: dielectric locked to uniform metallic0; G crystal coat/surface; B declared energy region' if kind=='regions' else 'Tangent-space independent numeric normals',human_registration_review='PENDING'))
(folder/'material_provenance.json').write_text(json.dumps(dict(schema='occ-crystal-candidate-material-v1',status='QA_PENDING',id=ident,maps=records,roughness=dict(stone=1,crystal=.25),world_pixel_pitch=1/32),ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
