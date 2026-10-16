"""Source evidence only; does not redraw or resize delivery assets."""
from pathlib import Path
from PIL import Image
import hashlib,json

root=Path(__file__).resolve().parents[2]
folder=Path(__file__).resolve().parent
entries=[('fire_crown','fire_crown_r1.png',16),('ash_puff','ash_puff_r3.png',12),('circuit_seal','circuit_seal_r1.png',16),('pyromancer_cast','pyromancer_cast_r1.png',24),('furnace_core','furnace_core_r1.png',12),('ring_band','ring_band_r1.png',16),('chamber_fire','chamber_fire_r2.png',16),('compressed_core','compressed_core_r2.png',8),('fire_wave','fire_wave_r2.png',16),('smoke_wisp_v2','smoke_wisp_v2_r2.png',6)]
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
rel=lambda p:p.relative_to(root).as_posix()
for name,source,palette in entries:
    image=Image.open(folder/'delivery'/f'{name}.png').convert('RGBA')
    qa=folder/'qa'/name;qa.mkdir(exist_ok=True)
    image.save(qa/'1x.png')
    image.resize((image.width*4,image.height*4),Image.Resampling.NEAREST).save(qa/'4x.png')
    image.resize((image.width*6,image.height*6),Image.Resampling.NEAREST).save(qa/'6x.png')
    grey=image.convert('L').convert('RGBA');grey.putalpha(image.getchannel('A'));grey.save(qa/'grayscale.png')
    checker=Image.new('RGBA',image.size)
    pixels=checker.load()
    for y in range(image.height):
        for x in range(image.width):pixels[x,y]=((190,190,190,255) if (x//4+y//4)%2 else (230,230,230,255))
    checker.alpha_composite(image);checker.resize((image.width*4,image.height*4),Image.Resampling.NEAREST).save(qa/'checker.png')
    manifest={
      'schema':'occ-art-manifest-v1','contract_version':1,'asset_id':f'sky_furnace_{name}_20261014',
      'role':'tactical_unit_adaptive_32ppu' if name=='pyromancer_cast' else 'hd2d_environment_module','status':'QA_PENDING',
      'provenance':{'source_channel':'codex_builtin_imagegen','source_descriptor':'Independent built-in source; noninteractive stage spatial-effect module, inferred grid decode without geometric resizing',
                    'source_path':rel(folder/'raw'/source),'source_sha256':sha(folder/'raw'/source)},
      'delivery':{'output_path':rel(folder/'delivery'/f'{name}.png'),'output_sha256':sha(folder/'delivery'/f'{name}.png'),
                  'native_output_path':rel(folder/'delivery'/f'{name}.png'),'native_ppu':32,'logical_cells':[1,1] if name=='pyromancer_cast' else None,'palette_max':palette,'required_color_families':[]},
      'application':{'rendering_mode':'battlefield_hd2d_environment','camera_projection':'perspective','runtime_draw_rect':f'{image.width/32} by {image.height/32} metres at fixed32PPU; independent noninteractive cinematic',
                     'default_integer_scale':1,'minimum_integer_scale':1,'integration_authorization':'User authorized all required stage assets and requested32px source art on2026-10-14'},
      'evidence':{**{key:rel(qa/file) for key,file in [('one_x','1x.png'),('four_x','4x.png'),('six_x','6x.png'),('grayscale','grayscale.png'),('checker','checker.png')]},
                  'application_contact':'Worldbuilding/归档/2026-10-14_天穹熔炉演出方案/fixed_frames/'+('frame_00.80.png' if name=='pyromancer_cast' else 'frame_06.50.png' if name in {'furnace_core','compressed_core'} else 'frame_04.50.png' if name in {'ring_band','chamber_fire'} else 'frame_12.50.png' if name=='ash_puff' else 'frame_08.10.png')},
      'human_review':{'overall':'PENDING','reviewer':'','date':'','notes':'Technical candidate only; actual scene density and aesthetic review pending'},
      'unity_import':{'asset_path':f'Assets/Game/Resources/Art/SkyFurnace/{name}.png','formal_promotion':False}
    }
    if name=='smoke_wisp_v2':manifest['evidence']['application_contact']='Worldbuilding/归档/2026-10-14_天穹熔炉演出方案/光影迭代v2/12.50_光影.png'
    (folder/f'{name}.occ-art-manifest-v1.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(name,image.size,'QA_PENDING')
