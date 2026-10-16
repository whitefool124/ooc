"""Reproducible source audit and candidate QA. No Unity or source-game writes."""
import hashlib
import json
from pathlib import Path
from PIL import Image

folder = Path(__file__).resolve().parent
root = folder.parents[2]
study = root / 'Worldbuilding/归档/2026-10-10_八方旅人还原样板'
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
rel = lambda p: p.relative_to(root).as_posix()
bind = lambda p: {'path': rel(p), 'sha256': sha(p)}
def write(name, value):
    (folder / name).write_text(json.dumps(value, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')

# Source material fields are evidence, not a recovered complete shader graph.
building = study / 'scene_data/Octopath_Traveler/Content/Map/Resource/BP/BP_Building/BP_BD_Plain'
material = building / 'Material/MbdMI_Plain.json'
cl = building / 'Texture/MbdTX_Plain_cl.png'
normal = building / 'Texture/MbdTX_Plain_no.png'
data = json.loads(material.read_text(encoding='utf-8'))
meshes_file = study / 'scene_data/meshes.json'
meshes = json.loads(meshes_file.read_text(encoding='utf-8'))
write('source_asset_study.json', {
    'schema': 'occ-reference-asset-study-v1',
    'source_install': 'E:/SteamLibrary/steamapps/common/OCTOPATH TRAVELER',
    'method': 'Read-only existing local export; no reference textures or meshes copied into OCC runtime',
    'inputs': [bind(material), bind(cl), bind(normal), bind(meshes_file)],
    'building_material': {'textures': data['Textures'], 'parameters': {k:data['Parameters'].get(k) for k in ('Colors','Scalars','Switches')}},
    'paired_atlas_dimensions': {'albedo': Image.open(cl).size, 'normal': Image.open(normal).size},
    'export_mesh_count': len(meshes),
    'observed': ['Colour atlas contains separate roof, wall, window and door regions',
                 'A separate normal atlas has corresponding structural regions',
                 'Building material references these maps and a height gradient',
                 'PM_SpecularMasks aliases the colour map in this exported material; that alias does not prove channel semantics'],
    'transfer': ['Author facade/card and simple environment volume separately',
                 'Keep colour, normal, surface, metal and emission records separate',
                 'Keep every visible source pixel at world pitch 1/32m',
                 'Preserve direct lighting and specular response when applying ambient visibility'],
    'limits': ['Missing material function connections and runtime scalar overrides remain unresolved',
               'Reference-game atlas density is not an approved OCC density',
               'No normal, roughness, metal or emission data inferred from albedo luminance/hue']})

source = folder/'greenhouse_raw_r1.png'
candidate = folder/'greenhouse_candidate_r1.png'
image = Image.open(candidate).convert('RGBA')
image.save(folder/'one_x.png')
image.resize((image.width*4,image.height*4), Image.Resampling.NEAREST).save(folder/'four_x.png')
gray = image.convert('L').convert('RGBA'); gray.putalpha(image.getchannel('A')); gray.save(folder/'grayscale.png')
checker = Image.new('RGBA', image.size)
checker.putdata([(190,190,190,255) if (x//8+y//8)%2 else (238,238,238,255) for y in range(image.height) for x in range(image.width)])
Image.alpha_composite(checker,image).save(folder/'checker.png')
bounds = image.getchannel('A').getbbox()
evidence = {n: rel(folder/(n+'.png')) for n in ['one_x','four_x','grayscale','checker']}
if (folder/'application_contact_1080.png').exists():
    evidence['application_contact']=rel(folder/'application_contact_1080.png')
write('manifest.json', {
    'schema':'occ-art-manifest-v1','contract_version':1,
    'asset_id':'occ.battle.environment.greenhouse.facade.r1','role':'hd2d_environment_module','status':'QA_PENDING',
    'provenance':{'source_channel':'codex_builtin_imagegen','source_descriptor':'Independent greenhouse facade; no reference-game art reused','source_path':rel(source),'source_sha256':sha(source)},
    'delivery':{'output_path':rel(candidate),'output_sha256':sha(candidate),'native_ppu':32,'logical_cells':None,'palette_max':16,'required_color_families':[]},
    'application':{'rendering_mode':'battlefield_hd2d_environment','camera_projection':'perspective','runtime_draw_rect':'PLANNED: 8m x 3m upright noninteractive facade card; unit scale; contact anchor at visible base; not placed yet'},
    'placement_semantics':{'application_scope':'runtime_layout','mount_type':'structural','logical_cell':None,'native_anchor':[128,bounds[3]-1],'visible_contact_line':{'axis':'y','native_px':bounds[3]-1},'depth_sort_baseline':bounds[3]-1,'allowed_occlusion_direction':'noninteractive background only','screen_axis':'horizontal_vertical_locked','support_asset':'PLANNED: greenhouse stone plinth'},
    'evidence':evidence,
    'human_review':{'overall':'PENDING','reviewer':'','date':'','silhouette':'PENDING','material':'PENDING','perspective':'PENDING','style':'PENDING','application':'PENDING','illumination':'PENDING'},
    'blockers':['Editor-only 1080 application evidence available; 540 and motion review missing','Companion normal and surface maps not provided','Complete scene density inventory and human review pending']})
brief = json.loads((root/'Tools/OCCArt/hd2d_material_brief_template_v1.json').read_text(encoding='utf-8-sig'))
brief.update(asset_id='occ.battle.environment.greenhouse.facade.r1',asset_role='hd2d_environment_module',scene_profile='battlefield-v1 / greenhouse candidate',reading_goal='Recognize the botanical conservatory in the battle background without hiding tactical cells',stage_theme_source='OCC mother document 6.2.3.5; A.2.4')
brief['pixel_density'].update(reference_asset_id='academy_block_court_b native32 + current battle units',authored_pixel_pitch=1,world_pixel_pitch=1/32,world_size_or_uv_mapping='256x96 native canvas -> 8x3m card at unit scale; no per-item density compensation')
brief['construction'].update(type='upright pixel card with optional simple textured structural backing',camera_view='front elevation',support_plane_and_anchor=f'Flat ground contact; native anchor (128,{bounds[3]-1})',model_rationale_if_needed='Large environment only; candidate backing not yet built')
brief['material_regions']=[{'id':'ivory_stone','meaning':'matte architectural base'}, {'id':'bronze_frame','meaning':'slender conservatory structure; metal mask must be explicitly authored'}, {'id':'glass','meaning':'quiet translucent-looking colour; actual reflection/transmission not authored yet'}, {'id':'plant','meaning':'nonemissive subdued botanical silhouettes'}]
brief['maps']['albedo'].update(source=rel(candidate),sha256=sha(candidate))
write('material_brief.json',brief)
write('placement_candidate.json', {'schema':'occ-hd2d-local-placement-candidate-v1','status':'QA_PENDING','asset':bind(candidate),'world_pixel_pitch':1/32,'reference':bind(root/'UnityProject/Assets/Game/Resources/Art/FormalAcademyIndependentFloors32/academy_block_court_b.png'),'world_canvas_size_m':[8,3],'scale':[1,1,1],'contact_anchor_native':[128,bounds[3]-1],'applied':False,'same_frame_review':'PENDING'})
print(json.dumps({'candidate_size':image.size,'subject_bounds':bounds,'palette':len(set(p[:3] for p in image.getdata() if p[3])),'source_mesh_count':len(meshes)},ensure_ascii=False))
