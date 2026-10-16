"""Register generated material source and inspection previews; never generates artwork."""
from pathlib import Path
import json,hashlib
from PIL import Image
root=Path.cwd();folder=Path(__file__).resolve().parent
def rel(p):return p.relative_to(root).as_posix()
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
source=folder/'raw/fire_material_r1.png';delivery=folder/'delivery/fire_material.png'
image=Image.open(delivery).convert('RGBA');qa=folder/'qa/material';qa.mkdir(exist_ok=True)
image.save(qa/'1x.png');image.resize((image.width*4,image.height*4),Image.Resampling.NEAREST).save(qa/'4x.png')
grey=image.convert('L').convert('RGBA');grey.putalpha(image.getchannel('A'));grey.save(qa/'grayscale.png')
checker=Image.new('RGBA',image.size)
for y in range(image.height):
 for x in range(image.width):checker.putpixel((x,y),(190,190,190,255) if (x//4+y//4)%2 else (230,230,230,255))
checker.alpha_composite(image);checker.resize((image.width*4,image.height*4),Image.Resampling.NEAREST).save(qa/'checker.png')
manifest={'schema':'occ-art-manifest-v1','contract_version':1,'asset_id':'sky_furnace_voxel_fire_material_20261015','role':'hd2d_environment_module','status':'QA_PENDING',
 'provenance':{'source_channel':'codex_builtin_imagegen','source_descriptor':'Independent flat pixel energy material swatch for user-authorized voxel spell models; no programmatic painting or recolouring','source_path':rel(source),'source_sha256':sha(source)},
 'delivery':{'output_path':rel(delivery),'native_output_path':rel(delivery),'output_sha256':sha(delivery),'native_ppu':32,'palette_max':10,'required_color_families':[]},
 'application':{'rendering_mode':'battlefield_hd2d_environment','camera_projection':'perspective','runtime_draw_rect':'Native face UV measured in metres. Opaque source window x9,y20,width108,height106 in Unity UV origin; one source texel per1/32metre. All source pixels retained in delivery.',
 'default_integer_scale':1,'minimum_integer_scale':1,'integration_authorization':'User approved voxel effect geometry plus materials/textures on2026-10-15; scoped independent cinematic, no formal gameplay change'},
 'evidence':{key:rel(qa/file) for key,file in [('one_x','1x.png'),('four_x','4x.png'),('grayscale','grayscale.png'),('checker','checker.png')]},
 'human_review':{'overall':'PENDING','reviewer':'','date':'','notes':'Machine lattice PASS1.2178; real source/grid/style and current application review pending'},
 'unity_import':{'asset_path':'Assets/Game/Resources/Art/SkyFurnace/fire_material_voxel.png','formal_promotion':False}}
manifest['evidence']['application_contact']='Worldbuilding/归档/2026-10-15_天穹熔炉体素重构/fixed_frames/frame_04.50.png'
(folder/'fire_material.occ-art-manifest-v1.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Registered material QA_PENDING',image.size)
