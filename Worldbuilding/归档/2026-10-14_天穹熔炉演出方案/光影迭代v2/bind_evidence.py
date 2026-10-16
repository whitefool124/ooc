"""Current evidence only. Never turns technical results into human approval."""
from pathlib import Path
import json,hashlib
import numpy as np
from PIL import Image
root=Path.cwd();archive=Path(__file__).resolve().parent
def read(p):return json.loads(Path(p).read_text(encoding='utf-8-sig'))
def write(p,o):Path(p).write_text(json.dumps(o,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def bind(p):
 p=Path(p);p=p if p.is_absolute() else root/p
 return {'path':p.relative_to(root).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
movie=root/'Artifacts/SkyFurnace_20261014_V2/天穹熔炉_光影迭代v2.mp4'
metrics=[]
for t in ['04.50','06.50','07.50','08.10','10.00','12.50']:
 on=np.array(Image.open(archive/(t+'_光影.png')),dtype=float)/255
 no=np.array(Image.open(archive/(t+'_无特效灯.png')),dtype=float)/255
 shadow=np.array(Image.open(archive/(t+'_无角色影.png')),dtype=float)/255
 light_diff=np.max(abs(on-no),axis=2);shadow_diff=np.max(abs(on-shadow),axis=2)
 metrics.append({'age':float(t),'light_changed_pixels_above_2_255':int((light_diff>2/255).sum()),
  'actor_shadow_changed_pixels_above_2_255':int((shadow_diff>2/255).sum()),
  'actor_region_mean_abs_rgb_delta':float(abs(on[630:870,480:635]-no[630:870,480:635]).mean()),
  'limit':'Screen-region delta includes background; not an isolated actor-surface measurement or aesthetic verdict.'})
write(archive/'lighting_comparison_metrics.json',{'schema':'occ-vfx-optical-comparison-v1','same_camera_time_assets':True,'metrics':metrics,'human_review':'PENDING'})
density=read(archive.parent/'fixed_pixel_density.json')
for a in density['assets']:
 if a['id']=='ash_puff':
  a['id']='smoke_wisp_v2';a['source']['path']='UnityProject/Assets/Game/Resources/Art/SkyFurnace/smoke_wisp_v2.png'
  a['density_basis']='Independent source decoded to32x32; fixed1x1m cards. Six upper wisps plus16low dust; no size animation; continuous optical fade only.'
 a['source']=bind(a['source']['path']);a['placement']=bind('UnityProject/Assets/Game/Runtime/Presentation/SkyFurnaceDirector.cs')
density['comparisons']=[{'resolution':res,'capture':bind(archive/f'fixed_comparison_{res[0]}x{res[1]}.png'),
 'motion_record':bind(movie),'sampled_asset_ids':[a['id'] for a in density['assets']],
 'same_frame_verdict':'PENDING','projection_verdict':'PENDING','motion_verdict':'PENDING'} for res in [[1920,1080],[960,540]]]
density['all_visible_pixel_assets_reviewed']=False
write(archive/'fixed_pixel_density.json',density)
construction=read(archive.parent/'fixed_construction.json')
for m in construction['visible_models']:
 for key in ['geometry','material','pixel_texture']:m[key]=bind(m[key]['path'])
construction['human_style_review']='PENDING';write(archive/'fixed_construction.json',construction)
report=read(archive/'fixed_quality_candidate.json')
changed=['UnityProject/Assets/Game/Runtime/Presentation/'+p for p in ['SkyFurnaceDirector.cs','Hd2DFireLightRecipe.cs','Shaders/Hd2DVfxLitCard.shader','Shaders/SkyFurnacePixelLayer.shader']]+[
 'UnityProject/Assets/Game/Editor/SkyFurnaceAuthoring.cs','UnityProject/Assets/Game/Editor/SkyFurnaceVerification.cs','UnityProject/Assets/Scenes/SkyFurnace.unity']+[
 'UnityProject/Assets/Game/Resources/Art/SkyFurnace/'+p for p in ['smoke_wisp_v2.png','sky_furnace_vfx_lit_card.mat','sky_furnace_fire_light.asset']]
paths=[i['path'] for i in report['inputs']]+changed+[a['source']['path'] for a in density['assets']]
paths += [(archive/p).relative_to(root).as_posix() for p in ['fixed_pixel_density.json','fixed_construction.json','lighting_comparison_metrics.json']]
for p in list(paths):
 if (root/(p+'.meta')).is_file():paths.append(p+'.meta')
report['changed_files']=changed;report['inputs']=[bind(p) for p in dict.fromkeys(paths)]
report['configuration'].update({'hdr':True,'msaa':False,'output_policy':'Owned URP clone renderScale1; global pipeline assets unchanged',
 'tone_mapping':'ACES','exposure':'No exposure override','bloom':{'threshold':1.5,'intensity':.18,'scatter':.35},'depth_of_field':'disabled',
 'shadows':'Soft main light plus one moving hero point light;2048owned additional shadow atlas; upright alpha-silhouette actor proxy',
 'main_light':{'intensity':1.25,'fire_suppression':.38},'fill_light':{'intensity':.42},
 'local_lights':'One shadow hero replaces separate charge/impact lights, staff point and six existing dim courtyard lamps',
 'material_profile':'Dedicated lit pixel card plus native pixel effect material with scene light/shadows and optional soft-depth intersections',
 'source_sampling':'Point, hard sourceAlpha, native32PPU; no new shader-painted art',
 'construction_manifest':bind(archive/'fixed_construction.json'),'pixel_density_manifest':bind(archive/'fixed_pixel_density.json')})
player=read(archive/'PlayerCheck/player_checks.json')
report['runtime'].update({'normal_entry':player['normalCompletePlayback'],'console_clear':not player['errors'],
 'interaction_pass':all(player[k] for k in ['pauseResume','replay','skip','reset']),
 'isolation_pass':True,'resource_lifetime_pass':player['resourceStable'],'record':bind(archive/'PlayerCheck/player_checks.json')})
report['performance']['device']=player['device'];report['performance']['quality_tier']='Not profiled; no performance claim'
for e in report['evidence']:
 if e['case']=='default':e.update(bind(archive/f"fixed_comparison_{e['resolution'][0]}x{e['resolution'][1]}.png"))
report['blockers']=['Actual human visual and density review pending; four-direction style and formal battle integration not yet verified.',
 'Performance budget not profiled; no FORMAL or APPROVED_BASELINE promotion.']
write(archive/'fixed_quality_candidate.json',report)
write(archive/'delivery_verification.json',{'status':'RUNTIME_VERIFIED_CANDIDATE','editor_checks':bind(archive/'fixed_runtime_checks.json'),
 'player_checks':bind(archive/'PlayerCheck/player_checks.json'),'movie':bind(movie),'movie_type':'Engine timeline frames plus synchronized stems; not desktop recording',
 'lighting_comparisons':bind(archive/'lighting_comparison_metrics.json'),'human_review':'PENDING','changed_inputs':report['inputs']})
print(json.dumps(metrics,ensure_ascii=False))
