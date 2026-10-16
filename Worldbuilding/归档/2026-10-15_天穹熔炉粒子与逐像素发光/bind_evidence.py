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
movie=root/'Artifacts/SkyFurnacePixelIgnition_20261015/天穹熔炉_粒子与逐像素点亮_无声版.mp4'
density=read(previous/'fixed_pixel_density.json')
density['assets']=[a for a in density['assets'] if a['id'] not in {'fire_crown','fire_wave','chamber_fire','compressed_core','ring_band'}]
for a in density['assets']:
 a['source']=bind(a['source']['path']);a['placement']=bind(director)
 if a['id']=='ring_band':
  a['placement']=bind(stage);a['density_basis']='Voxel ring faces use native32texels/m; fully opaque source window x18,y4,width90,height14 in Unity UV origin. No texture stretching.'
density['assets'].append({'id':'quiet_energy_pixel','source':bind(texture),'placement':bind(director),
 'density_basis':'Allowed one-cell cube particles and one-cell cross-section columns on1/32m lattice, face UV in metres, one native texel per1/32m. Explicit opaque source window x8,y1,width16,height15; full32x32delivery retained.',
 'source_uniformity':'PASS','effective_world_pitch_xy':[1/32,1/32],'review_limit':'Registered four-colour source lattice PASS1.5452, now used only on one-pixel columns and cubes; actual dimensions checked in primitive audit. Human review remains separate.'})
density['assets']=[a for a in density['assets'] if a['id']!='procedural_spell_support']
for name in ['outer','sigils','core','caster']:
 path='UnityProject/Assets/Game/Resources/Art/SkyFurnace/magic_card_'+name+'.png'
 density['assets'].append({'id':'magic_card_'+name,'source':bind(path),'placement':bind(director),'density_basis':'User-authorized reference-to-card construction,160x160outer at5x5metres,128x128sigils at4x4metres,96x96core at3x3metres,64x64caster at2x2metres with smaller central cutout; all32PPU. No per-layer texture stretching. Generated kit is a design reference, not a lossless native source; delivery is explicitly authored onto the chosen grid.','source_uniformity':'PASS','effective_world_pitch_xy':[1/32,1/32],'review_limit':'Authored delivery grid only. Artistic reconstruction and actual scene-density review PENDING.'})
for a in density['assets']:
 if 'smoke' in a['id']:
  a['placement']=bind('UnityProject/Assets/Game/Runtime/Presentation/SkyFurnaceSmokeParticles.cs')
  a['density_basis']='Native Particle System mesh particles:32x32paper at1x1m,1/32m thickness, Point texture, constant XYZ size1, no size-over-lifetime. View alignment changes projection only.'
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
for identity,purpose,count in [('one_pixel_columns','Five exact1/32m square-section columns; fixed native world UVs, native height clipping, no scaling',5),('one_pixel_cube_particles','Twelve exact1/32m cubes, replaces all octahedral particles',12)]:
 construction['visible_models'].append({'id':identity,'role':'spatial_effect_model','style':'simple_polygonal','rationale':purpose,'instance_count':count,'exception_rationale':'User2026-10-15 explicitly removed sky furnace modelling and restricted effects to one-pixel thick image cards, one-pixel columns and one-pixel cubes. Does not change scene environment models.','geometry':bind(director),'material':bind(shader),'pixel_texture':bind(texture)})
construction['pixel_cards']='All effect image cards now have exactly1/32m physical depth, including smoke and the four seal images. No sky furnace, solid fire currents, volumetric core or crown are instantiated.'
construction['removed_shapes']='Sky furnace tiers, broad fire currents, volumetric core and crown, octahedra and procedural heat quad removed from the runtime scene.'
construction['geometry_builder']=bind('UnityProject/Assets/Game/Runtime/Presentation/PixelEffectCard.cs');construction['model_checks']=bind(archive/'model_views/model_checks.json')
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
 'ArtSource/SkyFurnacePixel_20261015/quiet_energy.occ-art-manifest-v1.json','ArtSource/SkyFurnaceCardsClean_20261015/build_cards.py','ArtSource/SkyFurnaceCardsClean_20261015/card_production.json','ArtSource/SkyFurnaceCardsClean_20261015/raw/modular_kit.png']+[f'ArtSource/SkyFurnaceCardsClean_20261015/{n}.occ-art-manifest-v1.json' for n in ['outer','sigils','core','caster']]
paths += [(archive/p).relative_to(root).as_posix() for p in ['pixel_density.json','construction.json','model_views/model_checks.json']]
for p in list(paths):
 if (root/(p+'.meta')).is_file():paths.append(p+'.meta')
changed += ['UnityProject/Assets/Game/Runtime/Presentation/PixelEffectCard.cs','UnityProject/Assets/Game/Runtime/Presentation/Shaders/SkyFurnacePixelLayer.shader','UnityProject/Assets/Game/Runtime/Presentation/SkyFurnacePlayerProbe.cs']+['UnityProject/Assets/Game/Resources/Art/SkyFurnace/magic_card_'+n+'.png' for n in ['outer','sigils','core','caster']]
changed=[p for p in changed if p not in {stage,mesher}]
extra=['UnityProject/Assets/Game/Runtime/Presentation/PixelIgnitionMap.cs','UnityProject/Assets/Game/Runtime/Presentation/SkyFurnaceSmokeParticles.cs']
changed+=extra
paths+=extra+[(archive/p).relative_to(root).as_posix() for p in ['model_views/ignition_checks.json','ignition_study/progression.json','fixed_film_capture.json']]
paths+=changed
for p in list(paths):
 if (root/(p+'.meta')).is_file():paths.append(p+'.meta')
paths=[p for p in paths if p not in {stage,mesher,stage+'.meta',mesher+'.meta'}]
report['changed_files']=changed;report['inputs']=[bind(p) for p in dict.fromkeys(paths)]
report['configuration'].update({'effect_geometry_contract':{'permitted':['one_pixel_thick_image','one_pixel_column','one_pixel_cube'],'pitch':1/32,'sky_furnace_removed':True,'thermal_quad_removed':True,'actual_geometry_audit':bind(archive/'model_views/model_checks.json')},'hdr':True,'msaa':False,'output_policy':'Owned URP clone renderScale1,2048additional shadow atlas; global pipeline unchanged',
 'tone_mapping':'ACES','exposure':'No exposure override','bloom':{'threshold':2.0,'intensity':.06,'scatter':.30},'depth_of_field':'disabled',
 'shadows':'Soft directional and one moving fire point light; upright actor and existing obstacle casters. Energy surfaces do not cast opaque mass shadows.',
 'main_light':{'base_intensity':1.25,'fire_suppression':.38},'fill_light':{'intensity':.42},
 'local_lights':'Owned fire-light recipe retained; visible charge/contact source, staff and existing dim courtyard lamps',
 'material_profile':'New four-colour native pixel swatch on sparse one-cell geometry. Energy emission and restrained external light response preserve pixel strokes; lower bloom; no inferred PBR maps.',
 'source_sampling':'Point, no mipmaps/compression for new source; native32world lattice and UV density',
 'construction_manifest':bind(archive/'construction.json'),'pixel_density_manifest':bind(archive/'pixel_density.json'),
 'magic_cards':{'world_pixel_pitch':1/32,'outer':{'pixels':[160,160],'metres':[5,5],'height':1/32,'degrees_per_second':-2},'sigils':{'pixels':[128,128],'metres':[4,4],'height':3/32,'degrees_per_second':4},'core':{'pixels':[96,96],'metres':[3,3],'height':6/32,'degrees_per_second':-3},'caster':{'pixels':[64,64],'metres':[2,2],'height':1/32,'degrees_per_second':0},'sampling':'Point/noMip/uncompressed','phase_choreography':{'reveal':'Complete silhouette at onset, ordered whole-texel brightness charging, no angle clipping or random dissolve','winding':'base rotation plus36degcharge, locks6.95to7.4s','release':'10degoutward settle then8degrelax','lift_cells':{'sigils':5,'core':8},'fade_windows':{'core':[7.9,8.7],'sigils':[8.5,10.0],'outer':[9,10.8],'caster':[7.5,8.2]},'travel':'two broad authored-colour light bands, no new lines or colours'},'source_route':'User-authorized reference-derived component production; not lossless generation decode'},'camera_candidate':{'pitch':45,'yaw':0,'field_of_view':34,'distance':13.2,'locked':True},
 'auxiliary_static_model_views':[0,90,180,270],'formal_battle_skill':False,'duration':16,'terrain_destruction':False,'audio':'disabled; no AudioSource created'})
report['configuration']['native_smoke_particles']={'systems':2,'render_mode':'Mesh','geometry':'1/32m thick image card,32x32 pixels at1x1m','size_over_lifetime':False,'fixed_seed':True,'simulation':'ParticleSystem.Simulate, controlled by public timeline; freeze/seek/reset','dust':'18 burst particles, outward radial velocity, low noise, alpha fade','smoke':'14 burst particles, cone rising velocity, low noise, alpha fade'}
report['configuration']['per_texel_emission']={'data':'RFloat Point timing map; geodesic distance along opaque native texel connectivity. Technical timing data, not generated artwork.','windows':{'caster':[0,.9],'outer':[.9,2.7],'sigils':[2.3,4.8],'core':[4.3,6.9]},'geometry':'Complete silhouette remains, darker uncharged pixels','emission':'Independent HDR per source texel, leading-front highlight plus charged pulse','real_light_proxy':'Two pooled .8-intensity front lights, fade out when layer fully charged','audit':bind(archive/'model_views/ignition_checks.json'),'progression':bind(archive/'ignition_study/progression.json')}
report['configuration']['magic_cards']['phase_choreography']['travel']='Geodesic propagation over opaque texels; exact per-texel timing, no angular wipe'
player=read(archive/'PlayerCheck/player_checks.json');local=read(archive/'fixed_runtime_checks.json')
report['runtime'].update({'normal_entry':player['normalCompletePlayback'],'console_clear':not player['errors'],
 'interaction_pass':all(player[k] for k in ['pauseResume','replay','skip','reset']),
 'isolation_pass':local['pipelineRestored'],'resource_lifetime_pass':player['resourceStable'],'record':bind(archive/'PlayerCheck/player_checks.json')})
report['performance']['device']=player['device'];report['performance']['quality_tier']='Not profiled; cached vertex totals are not frame cost'
for e in report['evidence']:
 if e['case']=='default':e.update(bind(archive/f"fixed_comparison_{e['resolution'][0]}x{e['resolution'][1]}.png"))
report['blockers']=['Human approval of paper-card style, scene density and motion remains pending; existing environment source pixel pitch is mixed/pending, not certified by this local VFX change.',
 'Formal gameplay integration and performance-budget checks were not run. No APPROVED_BASELINE or FORMAL promotion.']
write(archive/'quality_candidate.json',report)
write(archive/'delivery_verification.json',{'status':'RUNTIME_VERIFIED_CANDIDATE','duration':16,'fixed_camera':True,'terrain_destruction':False,
 'editor_checks':bind(archive/'fixed_runtime_checks.json'),'player_checks':bind(archive/'PlayerCheck/player_checks.json'),
 'model_checks':bind(archive/'model_views/model_checks.json'),'build_report':bind(archive/'build_report.json'),'movie':bind(movie),
 'native_particle_pause':player['particlePausePass'],'native_particle_reset':player['particleResetPass'],'peak_smoke_particles':player['peakSmokeParticles'],'per_texel_emission_data':player['perPixelEmissionData'],'three_primitive_contract':player['threePrimitiveContract'],'silent_throughout':player['silentThroughout'],'audio_source_count':player['audioSourceCount'],'card_pipeline':'Reference design -> explicitly authored native canvas and complete independently drawn components -> independently animated/emissive paper cards','movie_type':'Silent Unity runtime timeline frame export; no audio streams, not desktop recording or FPS benchmark',
 'human_aesthetic_review':'PENDING','asset_status':'QA_PENDING','inputs':report['inputs']})
print('Bound current voxel models, material source, two-resolution captures, playback and standalone evidence. QA_PENDING.')
