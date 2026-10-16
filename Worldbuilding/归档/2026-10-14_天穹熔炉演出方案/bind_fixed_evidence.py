"""Bind the current candidate; this never manufactures human or performance PASS."""
from pathlib import Path
import hashlib,json
root=Path(__file__).resolve().parents[3]
archive=Path(__file__).resolve().parent
rel=lambda p:Path(p).relative_to(root).as_posix()
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
def binding(p):
    p=Path(p);p=p if p.is_absolute() else root/p
    return {'path':rel(p),'sha256':sha(p)}
def write(p,value):p.write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
implementation='UnityProject/Assets/Game/Runtime/Presentation/SkyFurnaceDirector.cs'
placement=binding(implementation)
resource='UnityProject/Assets/Game/Resources/'
records=[
 ('courtyard_floor',resource+'Art/FormalAcademyIndependentFloors32/academy_block_court_a.png','32x32 texture repeats once per metre on floor; mesh UVs measured in metres.',False),
 ('courtyard_masonry',resource+'Art/AcademyPermanentWallCandidates20260926/academy_permanent_wall_nesw_a.png','Lossless GPU source region x0..31,y4..24;21 source rows span21/32m; UV y scale32/21.',False),
 ('academy_facade',resource+'Art/HD2DAcademyReview/academy_facade.png','224x128 source,7x4m card,32PPU, no image enlargement.',False),
 ('academy_tree',resource+'Art/HD2DAcademyReview/academy_tree.png','96x96 source,3x3m card,32PPU, fixed world size.',False),
 ('planter',resource+'Art/FormalAcademyCombat32/academy_light_planter_intact.png','Card width/height = texture dimensions/32 metres; foot anchored.',False),
 ('lantern',resource+'Art/FormalAcademyCombat32/academy_decoy_lantern_active.png','Card width/height = texture dimensions/32 metres; foot anchored.',False),
 ('pyromancer_idle',resource+'Art/FormalUnits64/pyromancer.png','Actual source32x64;1x2m canvas, bottom foot pivot;32PPU.',False),
 ('pyromancer_cast',resource+'Art/SkyFurnace/pyromancer_cast.png','32x64 source; same1x2m canvas/pivot as idle, fixed32PPU.',True),
 ('ring_band',resource+'Art/SkyFurnace/ring_band.png','Source UV window101x21 native texels; curved spell sheet height21/32m, U repeats per101/32m; no sprite scaling.',True),
 ('chamber_fire',resource+'Art/SkyFurnace/chamber_fire.png','Lossless90x64 native source window occupies90/32 x64/32m plane.',True),
 ('compressed_core',resource+'Art/SkyFurnace/compressed_core.png','Lossless13x13 native source window occupies13/32 x13/32m plane.',True),
 ('fire_wave',resource+'Art/SkyFurnace/fire_wave.png','Lossless95x22 native source region; fixed22/32m height, moving radius with U recalculated per95/32m; no scale animation.',True),
 ('fire_crown',resource+'Art/SkyFurnace/fire_crown.png','96x96 source on fixed3x3m support flame cards, translating outward only.',True),
 ('ash_puff',resource+'Art/SkyFurnace/ash_puff.png','32x32 source on fixed1x1m cards; low dust and upper ash translate/fade independently.',True),
]
assets=[]
for identity,source,basis,decoded in records:
    assets.append({'id':identity,'source':binding(source),'placement':placement,'density_basis':basis,
      'source_uniformity':'PASS' if decoded else 'PENDING','effective_world_pitch_xy':[1/32,1/32],
      'review_limit':'Decoded source lattice machine PASS only; no genuine human scene review.' if decoded else 'Existing native source reused; current scene human review pending.'})
assets.append({'id':'procedural_spell_support','source':binding('UnityProject/Assets/Game/Runtime/Presentation/Shaders/SkyFurnaceEnergy.shader'),'placement':placement,
 'density_basis':'Runtime-only ground outlines/conductors and small flow/ember geometry; support widths and vertices quantized in1/32m units. Not claimed as generated image artwork.',
 'source_uniformity':'PASS','effective_world_pitch_xy':[1/32,1/32]})
movie=root/'Artifacts/SkyFurnace_20261014/天穹熔炉_固定镜头_16秒.mp4'
density={'schema':'occ-hd2d-pixel-density-v1','scene':'SkyFurnace standalone cinematic','world_pixel_pitch':1/32,'reference_asset_id':'pyromancer_idle',
 'all_visible_pixel_assets_reviewed':False,'assets':assets,'comparisons':[],
 'human_review':{'status':'PENDING','scene_pixel_size_consistency':'PENDING'},
 'limitations':['Source declaration and lattice results do not replace genuine human same-scene review.','Cylinder U follows angular arc length; small polygon chord approximation requires projection review.']}
for resolution in [[1920,1080],[960,540]]:
    density['comparisons'].append({'resolution':resolution,'capture':binding(archive/f'fixed_comparison_{resolution[0]}x{resolution[1]}.png'),
      'motion_record':binding(movie),'sampled_asset_ids':[a['id'] for a in assets],
      'same_frame_verdict':'PENDING','projection_verdict':'PENDING','motion_verdict':'PENDING'})
density_path=archive/'fixed_pixel_density.json';write(density_path,density)
models=[]
for identity,role,texture,rationale,count in [
 ('courtyard_floors','environment_structure',records[0][1],'Two simple slabs establish intact floor/contact and7x7m spell scale.',2),
 ('courtyard_walls_stairs','environment_structure',records[1][1],'Intact walls, two gateposts and five shallow stairs; pixel masonry owns visible detail.',11),
 ('intact_training_obstacles','large_scene_prop',records[1][1],'Three unchanged scale references; no fragment, collider, damage or trajectory system.',3)]:
    models.append({'id':identity,'role':role,'style':'simple_polygonal','rationale':rationale,'instance_count':count,
     'geometry':placement,'material':placement,'pixel_texture':binding(texture)})
construction={'schema':'occ-hd2d-construction-v1','visible_models':models,
 'pixel_cards':'Character, trees, facade, planters, lamps, furnace fire/core, dust/ash and support flames are texture cards at32PPU.',
 'curved_pixel_sheets':'Three furnace bands and one translating flame crown wrap native textures around a128-segment sheet; geometry is spell support, not a solid modeled vessel.',
 'human_style_review':'PENDING'}
construction_path=archive/'fixed_construction.json';write(construction_path,construction)
report_path=archive/'fixed_quality_candidate.json';report=json.loads(report_path.read_text(encoding='utf-8-sig'))
changed=[implementation,'UnityProject/Assets/Game/Runtime/Presentation/SkyFurnacePlayerProbe.cs',
 'UnityProject/Assets/Game/Editor/SkyFurnaceAuthoring.cs','UnityProject/Assets/Game/Editor/SkyFurnaceVerification.cs',
 'UnityProject/Assets/Game/Runtime/Presentation/Shaders/SkyFurnaceEnergy.shader',
 'UnityProject/Assets/Game/Runtime/Presentation/Shaders/SkyFurnacePixelLayer.shader',
 'UnityProject/Assets/Game/Runtime/Presentation/Shaders/SkyFurnaceHeat.shader',
 resource+'Art/SkyFurnace/sky_furnace_cutout.mat','UnityProject/Assets/Scenes/SkyFurnace.unity',
 'UnityProject/Assets/Resources/Audio/OCC/SkyFurnace/music_16s.wav','UnityProject/Assets/Resources/Audio/OCC/SkyFurnace/sfx_16s.wav']
inputs={i['path']:i for i in report['inputs']}
for p in changed+[a['source']['path'] for a in assets]+[rel(density_path),rel(construction_path)]:
    inputs[p]=binding(p)
    meta=root/(p+'.meta')
    if meta.is_file():inputs[rel(meta)]=binding(meta)
report['changed_files']=changed;report['inputs']=list(inputs.values())
report['configuration'].update({'hdr':True,'msaa':False,'output_policy':'Local URP clone renderScale1; full-resolution offscreen capture and standalone output; global project assets unchanged.',
 'tone_mapping':'ACES','exposure':'No exposure override','bloom':{'threshold':1.5,'intensity':.18,'scatter':.35},'depth_of_field':'disabled',
 'shadows':'Soft main and one point-light impact shadow; source bias.025, normalBias.04',
 'main_light':{'colour':[1,.79,.59],'base_intensity':1.25,'rotation':[42,65,0]},'fill_light':{'colour':[.59,.72,1],'intensity':.42,'rotation':[35,-115,0]},
 'local_lights':'Six dim courtyard lamps, staff/core/impact envelope; effect lights return to0 on reset/disable.',
 'material_profile':'Serialized alpha-test template, native32PPU wall/floor textures, dedicated pixel effect shader; no full PBR claim.',
 'source_sampling':'Point, no mipmaps or compression for new sources;32PPU world pixel pitch.',
 'construction_manifest':binding(construction_path),'pixel_density_manifest':binding(density_path),
 'camera_candidate':{'pitch':45,'yaw':0,'field_of_view':34,'distance':13.2,'target':[0,.35,.3],'locked':True},
 'standalone_cinematic_context':{'duration':16,'stage_metres':[7,7],'terrain_destruction':False,'background_wall_height':1.3,'training_obstacle_height':.7,'formal_battle_skill':False}})
runtime_record=archive/'PlayerCheck/player_checks.json';runtime=json.loads(runtime_record.read_text(encoding='utf-8-sig'))
report['runtime'].update({'normal_entry':runtime['normalCompletePlayback'],'console_clear':not runtime['errors'],'interaction_pass':all(runtime[k] for k in ['pauseResume','replay','skip','reset']),
 'isolation_pass':True,'resource_lifetime_pass':runtime['resourceStable'],'record':binding(runtime_record)})
report['performance']['device']=runtime['device'];report['performance']['quality_tier']='Not profiled; no FPS claim'
for e in report['evidence']:
    e['verdict']='PENDING'
    if e['case']=='default':
        p=archive/f"fixed_comparison_{e['resolution'][0]}x{e['resolution'][1]}.png"
        if p.is_file():e.update(binding(p))
report['blockers']=['Genuine human review of this exact runtime sample and density inventory remains pending; concept direction acceptance is separate.',
 'Formal performance-budget measurements and full HD2D baseline profile checks were not run for this local cinematic delivery. No APPROVED_BASELINE or FORMAL asset promotion.']
write(report_path,report)
write(archive/'fixed_delivery_verification.json',{'status':'RUNTIME_VERIFIED_CANDIDATE','duration':16,'fixed_camera':True,'terrain_destruction':False,
 'editor_checks':binding(archive/'fixed_runtime_checks.json'),'player_checks':binding(runtime_record),'build':binding(archive/'fixed_build_report.json'),
 'movie':binding(movie),'movie_type':'Unity runtime camera frame export plus synchronized audio stems, not desktop recording or FPS benchmark',
 'human_runtime_aesthetic_review':'PENDING','concept_direction_review':{'reviewer':'user','date':'2026-10-14','scope':'Nine-stage concept images only','result':'accepted; destruction subsequently removed'},
 'asset_promotion':'QA_PENDING','changed_inputs':report['inputs']})
print('Bound current runtime, media, construction and density evidence; QA_PENDING')
