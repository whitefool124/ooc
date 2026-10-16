from pathlib import Path
import json,hashlib
from PIL import Image
folder=Path(__file__).resolve().parent
root=folder.parents[2]
rel=lambda p:p.relative_to(root).as_posix()
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
im=Image.open(folder/'sprite_r1.png').convert('RGBA')
im.save(folder/'one_x.png');im.resize((256,256),Image.Resampling.NEAREST).save(folder/'four_x.png')
grey=im.convert('LA').convert('RGBA');grey.save(folder/'grayscale.png')
checker=Image.new('RGBA',im.size)
for y in range(im.height):
 for x in range(im.width):checker.putpixel((x,y),(180,180,180,255) if (x//8+y//8)%2 else (230,230,230,255))
checker.alpha_composite(im);checker.save(folder/'checker.png')
manifest={'schema':'occ-art-manifest-v1','contract_version':1,'asset_id':'research.town.yellow_laughing_guest.r1','role':'hd2d_library_display','status':'QA_PENDING',
 'provenance':{'source_channel':'codex_builtin_imagegen','source_descriptor':'Independent pixel redraw of user-supplied reference; local town practice only','source_path':rel(folder/'raw_r1.png'),'source_sha256':sha(folder/'raw_r1.png'),'user_reference':rel(folder/'reference.png')},
 'delivery':{'output_path':rel(folder/'sprite_r1.png'),'output_sha256':sha(folder/'sprite_r1.png'),'native_ppu':32,'logical_cells':None,'palette_max':1000000,'required_color_families':[]},
 'display':{'uniform_pixel_size_with_siblings':True,'integer_only':False},
 'application':{'runtime_draw_rect':'world bottom-anchored card at (-17,2.008,0), 32PPU','rendering_mode':'noncombat_hd2d_layered_sprite','camera_projection':'perspective','scope':'local non-shipping reference town experiment'},
 'evidence':{k:rel(folder/(k+'.png')) for k in ['one_x','four_x','grayscale','checker']},
 'human_review':{'overall':'PENDING','reviewer':'','date':'','scene_pixel_size_consistency':'PENDING','notes':'Generation and application performed; no formal approval claimed'},
 'processing':{'decoder_report':rel(folder/'decode_r1.json'),'alternate_pitch_diagnostic':rel(folder/'decode12.json'),'notes':'Decoder selected coarse 25px grid; automatic confidence does not prove source/style conformity. Research-only native candidate.'},
 'unity_import':{'filter_mode':'Point','compression':'None','mipmaps':False,'wrap_mode':'Clamp','runtime_only':True},
 'blockers':['Human style/pixel density acceptance pending; no FORMAL promotion']}
contact=root/'UnityProject/ReviewEvidence/town_guest_final_close.png'
if contact.is_file():manifest['evidence']['application_contact']=rel(contact)
(folder/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
