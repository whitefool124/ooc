import hashlib, json
from pathlib import Path
from PIL import Image
root=Path(__file__).resolve().parents[4]
folder=Path(__file__).resolve().parent
def rel(p):return p.relative_to(root).as_posix()
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
image=Image.open(folder/'delivery_r2.png').convert('RGBA')
image.save(folder/'one_x.png')
image.resize((512,512),Image.Resampling.NEAREST).save(folder/'four_x.png')
image.convert('LA').convert('RGBA').save(folder/'grayscale.png')
board=Image.new('RGBA',image.size,(204,204,204,255))
for y in range(0,128,8):
 for x in range(0,128,8):
  if (x//8+y//8)%2:board.paste((238,238,238,255),(x,y,x+8,y+8))
board.alpha_composite(image);board.save(folder/'checkerboard.png')
c=json.loads((root/'Tools/OCCArt/occ_art_contract_v1.json').read_text(encoding='utf-8'))
print('Required evidence:',c['required_evidence'])
evidence={k:rel(folder/(v+'.png')) for k,v in [('one_x','one_x'),('four_x','four_x'),('grayscale','grayscale'),('checker','checkerboard')]}
evidence['application_contact']='UnityProject/ReviewEvidence/20261010_hd2d_desk/wood_v2_after_1920.png'
m={'schema':'occ-art-manifest-v1','contract_version':c['version'],'asset_id':'desk_wood_material_v2','status':'QA_PENDING','role':'hd2d_environment_module',
 'provenance':{'source_channel':'codex_builtin_imagegen','source_descriptor':'Independent built-in image generation, retry 2: correct mixed grid only. Source is neutral top-down wood albedo, not a desk illustration.','source_path':rel(folder/'source_r2.png'),'source_sha256':sha(folder/'source_r2.png')},
 'delivery':{'output_path':rel(folder/'delivery_r2.png'),'output_sha256':sha(folder/'delivery_r2.png'),'logical_cells':None,'palette_max':16},
 'processing':{'grid_report':rel(folder/'decode_r2.json'),'source_lattice_confidence':1.9052,'geometric_resize':False,'new_colors':False,'native_patch':[15,1,98,98]},
 'application':{'rendering_mode':'main_menu_hd2d','camera_projection':'perspective','runtime_draw_rect':'Existing desk model UVs; 98 x 98 native patch repeated at 32 PPU; desktop 9.6 x 2.8 world units. Runtime preview only.','default_integer_scale':1,'minimum_integer_scale':1,'world_pixel_pitch':1/32,'scale_note':'1 logical texel per authored native pixel; final perspective screen projection is not an integer display scale.'},
 'evidence':evidence,'human_review':{'overall':'PENDING','scene_pixel_size_consistency':'PENDING'},'unity_import':None}
(folder/'manifest.json').write_text(json.dumps(m,ensure_ascii=False,indent=2),encoding='utf-8')
report_path=folder/'quality_pending.json'
r=json.loads(report_path.read_text(encoding='utf-8'))
def bound(p):return {'path':rel(p),'sha256':sha(p)}
changed=root/'UnityProject/Assets/Game/Runtime/Presentation/Hd2DDeskArtCandidate.cs'
r['changed_files']=[rel(changed)]
for p in [changed,folder/'source_r2.png',folder/'delivery_r2.png',folder/'decode_r2.json',folder/'manifest.json',folder/'apply.cs']:
 r['inputs']=[i for i in r['inputs'] if i['path']!=rel(p)];r['inputs'].append(bound(p))
r['captures']=[bound(root/('UnityProject/ReviewEvidence/20261010_hd2d_desk/wood_v2_'+name+'.png')) for name in ['before_1920','after_1920','before_960','after_960']]
base=folder.parent
r['runtime_records']=[bound(base/(name+'.json')) for name in ['wood_v2_reentry_play','wood_v2_final_new_entry_status','wood_v2_final_new_applied','wood_v2_final_readback','wood_v2_final_lifetime','wood_v2_console_final','wood_v2_final_exit']]
r['scope']='desk only; new independent wood material preview; no formal resource replacement, no shared shader/light/output changes'
r['configuration'].update(source_sampling='Point; decoded wood native patch 98x98 sampled at 32 PPU',material_preview={'texture':bound(folder/'delivery_r2.png'),'native_patch':[15,1,98,98],'smoothness':.52,'relief':1.15,'roughness_variation':.28,'native_ppu':32})
r['runtime'].update(normal_entry=True,interaction_pass=True,resource_lifetime_pass=True,console_clear=False,record=rel(base/'wood_v2_final_new_entry_status.json'))
r['blockers']=['Artistic assessment: candidate 2 grain is still too dense; neither Octopath-equivalent quality nor sample promotion established','First and third independently generated sources failed uniform grid confidence; rejected, not used','Complete scene pixel-density human review and motion evidence pending','Standalone Player performance remains unmeasured','Existing QuickInstaller byte-sequence exception remains in Console; no surface shader messages']
report_path.write_text(json.dumps(r,ensure_ascii=False,indent=2),encoding='utf-8')


