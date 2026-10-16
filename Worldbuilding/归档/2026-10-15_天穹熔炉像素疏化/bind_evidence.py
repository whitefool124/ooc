"""Bind actual voxel candidate evidence. Human approval and performance stay pending."""
from pathlib import Path
import hashlib,json
root=Path.cwd();archive=Path(__file__).resolve().parent
previous=root/'Worldbuilding/归档/2026-10-14_天穹熔炉演出方案/光影迭代v2'
def read(p):return json.loads(Path(p).read_text(encoding='utf-8-sig'))
def write(p,o):Path(p).write_text(json.dumps(o,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def bind(p):
 p=Path(p);p=p if p.is_absolute() else root/p
 return {'path':p.relative_to(root).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
director='UnityProject/Assets/Game/Runtime/Presentation/SkyFurnaceDirector.cs'
stage='UnityProject/Assets/Game/Runtime/Presentation/SkyFurnaceVoxelStage.cs'
mesher='UnityProject/Assets/Game/Runtime/Presentation/VfxVoxelMesh.cs'
shader='UnityProject/Assets/Game/Runtime/Presentation/Shaders/Hd2DVfxVoxelSurface.shader'
texture='UnityProject/Assets/Game/Resources/Art/SkyFurnace/quiet_energy_pixel.png'
movie=root/'Artifacts/SkyFurnacePixel_20261015/天穹熔炉_像素疏化版.mp4'
density=read(previous/'fixed_pixel_density.json')
density['assets']=[a for a in density['assets'] if a['id'] not in {'fire_crown','fire_wave','chamber_fire','compressed_core','ring_band'}]
for a in density['assets']:
 a['source']=bind(a['source']['path']);a['placement']=bind(director)
 if a['id']=='ring_band':
  a['placement']=bind(stage);a['density_basis']='Voxel ring faces use native32texels/m; fully opaque source window x18,y4,width90,height14 in Unity UV origin. No texture stretching.'
density['assets'].append({'id':'quiet_energy_pixel','source':bind(texture),'placement':bind(stage),
 'density_basis':'Sparse one-cell voxel strokes on1/32m lattice, face UV in metres, one native texel per1/32m. Explicit opaque source window x8,y1,width16,height15; full32x32delivery retained.',
 'source_uniformity':'PASS','effective_world_pitch_xy':[1/32,1/32],'review_limit':'Machine source lattice PASS1.5452; geometry/UV PASS is separate from human scene/style review.'})
density['all_visible_pixel_assets_reviewed']=False
density['limitations']=['Source and geometry grid metrics do not replace human scene pixel-density review.',
 'Four auxiliary static views inspect effect structures only. Actor/environment paper cards remain authored for the fixed primary camera.',
 'Continuous object rotation projects one unchanged native lattice; no per-object density adjustment or full-frame pixelation.']
density['comparisons']=[{'resolution':res,'capture':bind(archive/f'fixed_comparison_{res[0]}x{res[1]}.png'),
 'motion_record':bind(movie),'sampled_asset_ids':[a['id'] for a in density['assets']],
 'same_frame_verdict':'PENDING','projection_verdict':'PENDING','motion_verdict':'PENDING'} for res in [[1920,1080],[960,540]]]
write(archive/'pixel_density.json',density)
construction=read(previous/'fixed_construction.json')
for model in construction['visible_models']:
 for key in ['geometry','material','pixel_texture']:model[key]=bind(model[key]['path'])
for identity,purpose,count,art in [
 ('voxel_constraint_rings','Three levels of one-cell broken arcs, no junction blocks or doubled conducting rims',3,texture),
 ('voxel_fire_currents','Two one-cell spatial fire currents, eight native-grid compression poses each',2,texture),
 ('voxel_core','Open shell plus luminous enclosed core; real depth and silhouette',2,texture),
 ('voxel_piercing_beam','Actual thickness, local-height reveal without texture scaling',1,texture),
 ('voxel_fire_crown','Four sparse curling one-cell outward strokes in one mesh,25native-grid cached shapes; no wall or repeated flame cards',1,texture)]:
 construction['visible_models'].append({'id':identity,'role':'spatial_effect_model','style':'simple_polygonal',
  'construction_method':'native32 voxel solid with merged external planar faces','rationale':purpose,'instance_count':count,
  'exception_rationale':'User explicitly selected voxel effect modelling with materials/textures on2026-10-15 and then authorized implementation. Scoped to this standalone spell; does not replace general paper-card policy.',
  'geometry':bind(stage),'material':bind(shader),'pixel_texture':bind(art)})
construction['pixel_cards']='Existing actor, environment and smoke remain pixel cards. Furnace rings, chamber, core, beam and crown are voxel geometry.'
construction['curved_pixel_sheets']='Previous cylinder and flame-card main forms removed from runtime.'
construction['geometry_builder']=bind(mesher);construction['model_checks']=bind(archive/'model_views/model_checks.json')
construction['human_style_review']='PENDING';write(archive/'construction.json',construction)
report=read(archive/'quality_candidate.json')
changed=[director,stage,mesher,shader,'UnityProject/Assets/Game/Runtime/Presentation/Shaders/SkyFurnaceEnergy.shader','UnityProject/Assets/Game/Editor/SkyFurnaceAuthoring.cs','UnityProject/Assets/Game/Editor/SkyFurnaceVerification.cs',texture,'UnityProject/Assets/Scenes/SkyFurnace.unity']
paths=[i['path'] for i in report['inputs']]+changed+[director,mesher,shader,'UnityProject/Assets/Scenes/SkyFurnace.unity',texture]+[a['source']['path'] for a in density['assets']]+[
 'UnityProject/Assets/Game/Runtime/Presentation/SkyFurnacePlayerProbe.cs',
 'UnityProject/Assets/Game/Runtime/Presentation/Hd2DFireLightRecipe.cs',
 'UnityProject/Assets/Game/Runtime/Presentation/Shaders/Hd2DVfxLitCard.shader',
 'UnityProject/Assets/Game/Runtime/Presentation/Shaders/SkyFurnacePixelLayer.shader',
 'UnityProject/Assets/Game/Runtime/Presentation/Shaders/SkyFurnaceEnergy.shader',
 'UnityProject/Assets/Game/Runtime/Presentation/Shaders/SkyFurnaceHeat.shader',
 'UnityProject/Assets/Game/Resources/Art/SkyFurnace/sky_furnace_fire_light.asset',
 'UnityProject/Assets/Game/Resources/Art/SkyFurnace/sky_furnace_vfx_lit_card.mat',
 'UnityProject/Assets/Game/Resources/Art/SkyFurnace/sky_furnace_cutout.mat',
 'UnityProject/Assets/Resources/Audio/OCC/SkyFurnace/music_16s.wav','UnityProject/Assets/Resources/Audio/OCC/SkyFurnace/sfx_16s.wav',
 'ArtSource/SkyFurnacePixel_20261015/quiet_energy.occ-art-manifest-v1.json']
paths += [(archive/p).relative_to(root).as_posix() for p in ['pixel_density.json','construction.json','model_views/model_checks.json']]
for p in list(paths):
 if (root/(p+'.meta')).is_file():paths.append(p+'.meta')
report['changed_files']=changed;report['inputs']=[bind(p) for p in dict.fromkeys(paths)]
report['configuration'].update({'hdr':True,'msaa':False,'output_policy':'Owned URP clone renderScale1,2048additional shadow atlas; global pipeline unchanged',
 'tone_mapping':'ACES','exposure':'No exposure override','bloom':{'threshold':2.0,'intensity':.06,'scatter':.30},'depth_of_field':'disabled',
 'shadows':'Soft directional and one moving fire point light; upright actor and existing obstacle casters. Sparse energy contours do not cast opaque mass shadows.',
 'main_light':{'base_intensity':1.25,'fire_suppression':.38},'fill_light':{'intensity':.42},
 'local_lights':'Owned fire-light recipe retained; visible charge/contact source, staff and existing dim courtyard lamps',
 'material_profile':'New four-colour native pixel swatch on sparse one-cell geometry. Energy emission and restrained external light response preserve pixel strokes; lower bloom; no inferred PBR maps.',
 'source_sampling':'Point, no mipmaps/compression for new source; native32world lattice and UV density',
 'construction_manifest':bind(archive/'construction.json'),'pixel_density_manifest':bind(archive/'pixel_density.json'),
 'camera_candidate':{'pitch':45,'yaw':0,'field_of_view':34,'distance':13.2,'locked':True},
 'auxiliary_static_model_views':[0,90,180,270],'formal_battle_skill':False,'duration':16,'terrain_destruction':False})
player=read(archive/'PlayerCheck/player_checks.json');local=read(archive/'fixed_runtime_checks.json')
report['runtime'].update({'normal_entry':player['normalCompletePlayback'],'console_clear':not player['errors'],
 'interaction_pass':all(player[k] for k in ['pauseResume','replay','skip','reset']),
 'isolation_pass':local['pipelineRestored'],'resource_lifetime_pass':player['resourceStable'],'record':bind(archive/'PlayerCheck/player_checks.json')})
report['performance']['device']=player['device'];report['performance']['quality_tier']='Not profiled; cached vertex totals are not frame cost'
for e in report['evidence']:
 if e['case']=='default':e.update(bind(archive/f"fixed_comparison_{e['resolution'][0]}x{e['resolution'][1]}.png"))
report['blockers']=['Genuine human approval of voxel style, scene density and motion remains pending.',
 'Formal gameplay integration and performance-budget checks were not run. No APPROVED_BASELINE or FORMAL promotion.']
write(archive/'quality_candidate.json',report)
write(archive/'delivery_verification.json',{'status':'RUNTIME_VERIFIED_CANDIDATE','duration':16,'fixed_camera':True,'terrain_destruction':False,
 'editor_checks':bind(archive/'fixed_runtime_checks.json'),'player_checks':bind(archive/'PlayerCheck/player_checks.json'),
 'model_checks':bind(archive/'model_views/model_checks.json'),'build_report':bind(archive/'build_report.json'),'movie':bind(movie),
 'movie_type':'Unity runtime timeline frame export plus synchronized stems; not desktop recording or FPS benchmark',
 'human_aesthetic_review':'PENDING','asset_status':'QA_PENDING','inputs':report['inputs']})
print('Bound current voxel models, material source, two-resolution captures, playback and standalone evidence. QA_PENDING.')
