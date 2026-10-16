from pathlib import Path
from PIL import Image,ImageOps
import json,hashlib,shutil
root=Path(__file__).resolve().parents[2];folder=Path(__file__).resolve().parent
rel=lambda p:p.relative_to(root).as_posix()
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
qa=json.loads((folder/'round2_qa.json').read_text('utf-8'))
assert qa['status']=='PASS_LOSSLESS_TARGET' and qa['subject_lattice_confidence']>=1.2
im=Image.open(folder/'round2_native.png').convert('RGBA');box=im.getbbox();body=im.crop(box)
out=Image.new('RGBA',(64,64));out.alpha_composite(body,((64-body.width)//2,62-body.height))
# Normalize only the existing outer opaque ring; preserve the exact alpha silhouette.
alpha=out.getchannel('A');px=out.load()
for y in range(64):
 for x in range(64):
  if alpha.getpixel((x,y)) and any(nx<0 or ny<0 or nx>=64 or ny>=64 or not alpha.getpixel((nx,ny)) for nx,ny in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]):px[x,y]=(0,0,0,255)
asset=folder/'breach_ram.png';out.save(asset)
out.save(folder/'1x.png');out.resize((256,256),Image.Resampling.NEAREST).save(folder/'4x.png');out.resize((384,384),Image.Resampling.NEAREST).save(folder/'6x.png')
grey=ImageOps.grayscale(out).convert('RGBA');grey.putalpha(out.getchannel('A'));grey.save(folder/'grayscale.png')
checker=Image.new('RGBA',(64,64));cp=checker.load()
for y in range(64):
 for x in range(64):cp[x,y]=(200,200,200,255) if (x//4+y//4)%2 else (240,240,240,255)
checker.alpha_composite(out);checker.resize((256,256),Image.Resampling.NEAREST).save(folder/'checker.png')
m=json.loads((root/'ArtSource/enemy_independent_20261016/signal_keeper/manifest.occ-art-manifest-v1.json').read_text('utf-8'))
m['asset_id']='breach_ram_triangle_candidate_20261016';m['role']='tactical_unit_adaptive_32ppu'
m['provenance'].update(source_path=rel(folder/'round2_source.png'),source_sha256=sha(folder/'round2_source.png'),reference_path='Worldbuilding/归档/2026-10-16_楔角旧形象与文字反馈/old_reference.png',source_descriptor='Independent built-in generation preserving user-selected old triangle-head identity; round1 rejected unstable grid; round2 lossless decode')
m['delivery'].update(output_path=rel(asset),output_sha256=sha(asset))
m['application'].update(runtime_draw_rect='64x64 at32PPU; one logical cell; feetY62',review_scope='Old reference identity selected by user; new sprite and actual camera awaiting review')
for k in ['one_x','four_x','six_x','grayscale','checker']:m['evidence'][k]=rel(folder/({'one_x':'1x','four_x':'4x','six_x':'6x'}.get(k,k)+'.png'))
m['evidence']['application_contact']='Worldbuilding/归档/2026-10-16_楔角旧形象与文字反馈/runtime/ram.png'
m['unity_import'].update(asset_path='UnityProject/Assets/Game/Resources/Art/EnemyUnitCandidates32x64/breach_ram.png',stable_guid='',importer_verified=False,runtime_verified=False,review_scope='Explicit Editor review only')
m['processing'].update(decoder_report=rel(folder/'round2_qa.json'),source_logical_size=qa['subject_logical_size'],placement_offset=[(64-body.width)//2,62-body.height],palette_count=len(set(c for c in out.getdata() if c[3])))
(folder/'manifest.occ-art-manifest-v1.json').write_text(json.dumps(m,ensure_ascii=False,indent=2),'utf-8')
shutil.copy2(asset,root/m['unity_import']['asset_path'])
print('candidate64x64 body',body.size)
