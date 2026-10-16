from pathlib import Path
import json,hashlib,sys,shutil
from PIL import Image
ROOT=Path(__file__).resolve().parents[3];OUT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'Tools/OCCArt'))
from bake_authored_normal_fields import normal_pixels,png_bytes
recipe_path=OUT/'crystal_numeric_recipe_v1.json';recipe=json.loads(recipe_path.read_text(encoding='utf-8'))
def bind(p):return dict(path=p.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(p.read_bytes()).hexdigest())
records=[]
for asset in recipe['assets']:
 ident=asset['id'];source=ROOT/f'UnityProject/Assets/Game/Resources/Art/HD2DBattleCrystalCandidates/{ident}.png'
 albedo=Image.open(source).convert('RGBA');assert albedo.size==(64,64)
 pixels=list(albedo.get_flattened_data());crystal={tuple(c) for c in asset['crystal_colors']}
 visible={p[:3] for p in pixels if p[3]};assert crystal<=visible,'Palette declaration contains missing source colours'
 support={p for p in visible if p not in crystal}
 # Explicit non-crystal declarations include stone shades and the hard outline.
 assert all(max(c)-min(c)<=2 for c in support),'Undeclared non-stone palette member; inspect source and declare it'
 heights=[[0.0]*64 for _ in range(64)]
 for x0,y0,x1,y1,peak in asset['columns']:
  mid=(x0+x1)*.5;half=(x1-x0)*.5
  for y in range(y0,y1+1):
   for x in range(x0,x1+1):
    relief=max(0,1-abs(x-mid)/half)*peak
    heights[y][x]=max(heights[y][x],relief)
 _,_,normal=normal_pixels(heights,recipe['world_pixel_pitch']);normal=bytearray(normal);regions=bytearray()
 for i,p in enumerate(pixels):
  active=p[3]>0 and p[:3] in crystal
  regions.extend([0,255,round(asset['energy']*255)] if active else [0,0,0])
  if not active:normal[i*3:i*3+3]=bytes([128,128,255])
 folder=OUT/'crystal_authored_fields_r1'/ident;folder.mkdir(parents=True,exist_ok=True)
 (folder/'normal.png').write_bytes(png_bytes(64,64,bytes(normal)))
 (folder/'regions.png').write_bytes(png_bytes(64,64,bytes(regions)))
 (folder/'authored_heights.json').write_text(json.dumps(dict(heights_world_m=heights,crystal_palette=asset['crystal_colors'],stone_and_outline_palette=[list(c) for c in sorted(support)]),indent=2)+'\n',encoding='utf-8')
 dest=ROOT/f'UnityProject/Assets/Game/Resources/HD2DMaterialData/CrystalCandidates/{ident}';dest.mkdir(parents=True,exist_ok=True)
 for kind in ['normal','regions']:
  target=dest/(kind+'.png')
  if target.exists() and target.read_bytes()!=(folder/(kind+'.png')).read_bytes():raise ValueError('Existing candidate differs; inspect before replacing')
  shutil.copy2(folder/(kind+'.png'),target)
  Image.open(target).resize((512,512),Image.Resampling.NEAREST).save(folder/(kind+'_qa8x.png'))
 record=dict(schema='occ-crystal-candidate-material-v1',status='QA_PENDING',id=ident,
  albedo=bind(source),recipe=bind(recipe_path),height=bind(folder/'authored_heights.json'),native_canvas=[64,64],world_pixel_pitch=recipe['world_pixel_pitch'],
  geometry_basis=recipe['geometry_basis'],material_region_basis=recipe['region_basis'],normal=bind(dest/'normal.png'),regions=bind(dest/'regions.png'),
  uniform_crystal_roughness=asset['roughness'],stone_roughness=1,metallic=0,normal_convention='OpenGL +Y, no green flip',
  channels='R unused dielectric0; G declared crystal coat/smoothness region; B authored energy intensity',
  human_registration_review='PENDING',runtime_review='PENDING',formal_promotion=False,limitation='Shallow relief/coat candidate, not physical crystal transmission')
 (folder/'material_provenance.json').write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');records.append(record)
(OUT/'crystal_authored_fields_r1/batch_result.json').write_text(json.dumps(records,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(status='QA_PENDING',assets=len(records),numeric_fields=len(records)*2)))
