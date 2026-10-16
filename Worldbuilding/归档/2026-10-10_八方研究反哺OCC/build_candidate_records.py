"""Record independent generation and diagnostic views; never promote art."""
from pathlib import Path
import json, hashlib, sys
from PIL import Image

folder = Path(__file__).resolve().parent
root = folder.parents[2]
wood = folder / 'wood_candidate'
rel = lambda p: p.relative_to(root).as_posix()
digest = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
tile = wood / 'wood_tile64_candidate.png'
im = Image.open(tile).convert('RGBA')
im.save(wood / 'one_x.png')
im.resize((256,256), Image.Resampling.NEAREST).save(wood / 'four_x.png')
im.convert('L').save(wood / 'grayscale.png')
im.save(wood / 'checker.png')  # Opaque material: checker cannot show through.
repeat = Image.new('RGBA',(128,128))
for y in (0,64):
    for x in (0,64): repeat.paste(im,(x,y))
repeat.resize((512,512),Image.Resampling.NEAREST).save(wood/'repeat_2x2.png')
brief = json.loads((root/'Tools/OCCArt/hd2d_material_brief_template_v1.json').read_text(encoding='utf-8'))
brief.update(asset_id='occ.desk.wood.albedo.r1',asset_role='opaque desk material tile; admission role unresolved',scene_profile='desk',reading_goal='Rough honey-brown wood underneath readable paper objects')
brief['pixel_density'].update(reference_asset_id='desk blue/gold crystal lamp',authored_pixel_pitch='source lattice 12px; decoded tile 1px',world_pixel_pitch=1/32,world_size_or_uv_mapping='Candidate 64x64 tile at 32PPU; application not performed')
brief['maps']['albedo'].update(source=rel(tile),sha256=digest(tile))
brief['construction'].update(type='texture for existing low-poly desk',camera_view='neutral orthographic material source',model_rationale_if_needed='Existing large scene support surface')
brief['generation']={'source':rel(wood/'wood_albedo_raw_r1.png'),'sha256':digest(wood/'wood_albedo_raw_r1.png'),'prompt':rel(wood/'prompt_r1.txt'),'source_channel':'codex_builtin_imagegen','decode':rel(wood/'wood_tile64_decode.json')}
brief['blockers']=['Native tile seams and scene grain comparison pending','Opaque material tile has no applicable admission role in current contract','Independent normal and surface maps not provided','Human review and Unity application pending']
(wood/'material_brief.json').write_text(json.dumps(brief,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
manifest={'schema':'occ-art-manifest-v1','contract_version':1,'asset_id':brief['asset_id'],'role':'hd2d_material_tile_pending_definition','status':'QA_PENDING','provenance':{'source_channel':'codex_builtin_imagegen','source_path':brief['generation']['source'],'source_sha256':brief['generation']['sha256'],'source_descriptor':'Independent OCC wood albedo generation; no reference-game texture reuse'},'delivery':{'output_path':rel(tile),'output_sha256':digest(tile),'native_ppu':32,'logical_cells':None,'palette_max':len(set(im.getdata()))},'evidence':{k:rel(wood/(k+'.png')) for k in ['one_x','four_x','grayscale','checker']},'human_review':{'overall':'PENDING'},'blockers':brief['blockers']}
(wood/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
sys.path.insert(0,str(root/'Tools/OCCArt'))
from validate_occ_art_asset import validate_manifest
contract=json.loads((root/'Tools/OCCArt/occ_art_contract_v1.json').read_text(encoding='utf-8-sig'))
errors,result=validate_manifest(manifest,contract,root)
# The current CLI returns an empty report on an unknown role; preserve actual
# returned errors explicitly rather than interpreting {} as successful admission.
(wood/'admission_result.json').write_text(json.dumps({'validator_result':result,'errors':errors,'admitted':not errors},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'candidate':'QA_PENDING','dimensions':im.size,'colours':len(set(im.getdata())),'application':'NOT_APPLIED'},ensure_ascii=False))

report_path=folder/'desk_quality_candidate.json'
report=json.loads(report_path.read_text(encoding='utf-8'))
report['changed_files']=['UnityProject/Assets/Game/Runtime/Presentation/Hd2DLoadoutScene.Lighting.cs']
report['runtime_observations']={'normal_entry':'PASS','equipment_tab':'PASS','spell_tab':'PASS','return_map':'PASS','desk_root_count_after_close':0,'ambient_mode_after_close':'Flat','editor_compile':'READY','console_error_query':'NONE','equipment_surface_count':119,'spell_surface_count':51,'far_dof_start':14.1969,'far_dof_end':16.1969,'dof_radius':0.8,'lamp_preference_restored':True,'resolution_restored':True}
report['additional_evidence']=[{'path':rel(p),'sha256':digest(p)} for p in sorted((folder/'evidence').glob('*.png'))]
report['blockers']=['Human aesthetic approval pending','Full density and effect-isolation review pending','Editor/Player performance evidence not measured','Independent wood material maps and formal asset admission pending']
report_path.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
