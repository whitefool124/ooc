from pathlib import Path
import json,hashlib,sys
from PIL import Image,ImageDraw
root=Path.cwd();sys.path.insert(0,str(root/'Tools/OCCArt'));from decode_generated_pixel_grid import consolidate_to_observed_palette
folder=root/'ArtSource/SkyFurnaceSmokeAnimation_20261015';src=folder/'raw/smoke_sheet_r2.png';im=Image.open(src).convert('RGBA')
frames=[];rows=[]
# Explicit user-authorized reference-derived paper manufacture; not lossless grid decoding.
for i in range(8):
 x=i%4;y=i//4;box=(round(x*im.width/4),round(y*im.height/2),round((x+1)*im.width/4),round((y+1)*im.height/2));part=im.crop(box)
 px=part.load()
 for b in range(part.height):
  for a in range(part.width):
   r,g,bl,al=px[a,b]
   if (r>170 and bl>130 and g<110) or (r>g+20 and bl>g+20):px[a,b]=(0,0,0,0)
 part=part.resize((32,32),Image.Resampling.NEAREST);part.putalpha(part.getchannel('A').point(lambda a:255 if a>=128 else 0))
 frames.append(part);rows.append({'frame':i,'source_cell':box,'native_frame':[32,32],'visible_bounds':part.getbbox()})
atlas=Image.new('RGBA',(128,64))
for i,frame in enumerate(frames):atlas.paste(frame,(i%4*32,i//4*32))
atlas=consolidate_to_observed_palette(atlas,4);atlas.save(folder/'delivery/smoke_roll_sheet.png'); qa=folder/'qa';qa.mkdir(exist_ok=True)
atlas.save(qa/'1x.png');atlas.resize((1024,512),Image.Resampling.NEAREST).save(qa/'8x.png')
gray=atlas.convert('L').convert('RGBA');gray.putalpha(atlas.getchannel('A'));gray.save(qa/'grayscale.png')
checker=Image.new('RGBA',atlas.size);d=ImageDraw.Draw(checker)
for y in range(0,64,4):
 for x in range(0,128,4):d.rectangle((x,y,x+3,y+3),fill=(200,200,200,255) if (x//4+y//4)%2 else (240,240,240,255))
checker.alpha_composite(atlas);checker.resize((1024,512),Image.Resampling.NEAREST).save(qa/'checker.png')
for i in range(8):
 frame=atlas.crop((i%4*32,i//4*32,i%4*32+32,i//4*32+32));frame.save(folder/f'delivery/smoke_roll_{i}.png')
anim=[atlas.crop((i%4*32,i//4*32,i%4*32+32,i//4*32+32)).resize((256,256),Image.Resampling.NEAREST) for i in range(8)];anim[0].save(qa/'smoke_animation.gif',save_all=True,append_images=anim[1:],duration=[100,110,140,180,160,170,180,250],loop=0,disposal=2)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest();rel=lambda p:p.relative_to(root).as_posix()
record={'route':'User-authorized reference-derived paper production; NOT lossless source-grid decode','raw':rel(src),'raw_sha256':sha(src),'grid_confidence_pass_claimed':False,'geometric_resampling_performed':True,'resampling':'Fixed equal source cells -> native32x32, nearest neighbor; no independent bounding-box scaling','background':'Magenta key removal, hard alpha','palette':'Four observed source medoids, no new colours','frames':rows,'world_canvas_metres':[.75,.75],'effect_pixel_pitch_metres':.75/32,'reference_scope':'User2026-10-15 permits VFX pixel-size variation but requires coherent world scale','human_aesthetic_review':'PENDING'}
(folder/'production.json').write_text(json.dumps(record,ensure_ascii=False,indent=2),encoding='utf-8')
manifest={'schema':'occ-art-manifest-v1','contract_version':1,'asset_id':'sky_furnace_smoke_roll_20261015','role':'hd2d_environment_module','status':'QA_PENDING','provenance':{'source_channel':'codex_builtin_imagegen','source_descriptor':record['route'],'source_path':rel(src),'source_sha256':sha(src)},'delivery':{'output_path':rel(folder/'delivery/smoke_roll_sheet.png'),'native_output_path':rel(folder/'delivery/smoke_roll_sheet.png'),'output_sha256':sha(folder/'delivery/smoke_roll_sheet.png'),'native_ppu':32/.75,'palette_max':4,'required_color_families':[]},'application':{'rendering_mode':'battlefield_hd2d_environment','camera_projection':'perspective','runtime_draw_rect':'.75m fixed canvas, eight32x32frames; no sizeOverLifetime','default_integer_scale':1,'minimum_integer_scale':1,'integration_authorization':'User says go; VFX pixel pitch may differ, physical scale must remain coherent; isolated SkyFurnace sample'},'evidence':{'one_x':rel(qa/'1x.png'),'four_x':rel(qa/'8x.png'),'grayscale':rel(qa/'grayscale.png'),'checker':rel(qa/'checker.png')},'human_review':{'overall':'PENDING','notes':'Reference-derived animation manufacture, not strict generated-lattice PASS; VFX density exception explicitly authorized'},'unity_import':{'asset_path':'Assets/Game/Resources/Art/SkyFurnace/smoke_roll_sheet.png','formal_promotion':False}}
(folder/'smoke_roll.occ-art-manifest-v1.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(rows,ensure_ascii=False))
