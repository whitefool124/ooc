"""Bind current local capture evidence without claiming quality approval."""
from pathlib import Path
import json,hashlib,shutil
from datetime import datetime,timezone
from PIL import Image
ROOT=Path(__file__).resolve().parents[3];OUT=Path(__file__).resolve().parent
def bind(p):
 return {'path':p.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
families=['AcademyCourt','Conservatory','StoneRoad','RuinSite','InteriorHall','AetherTower']
profiles=[]
for family in families:
 folder=OUT/'profiles'/family
 capture=json.loads((folder/'capture.json').read_text(encoding='utf-8'))
 cleanup=json.loads((folder/'cleanup.json').read_text(encoding='utf-8'))
 assert capture['status']=='captured' and capture['environment']==family
 assert capture['diffuseUpdates']>0 and capture['reflectionCaptures']>0
 assert all(f['cameraFacingDot']>0 for f in capture['mappedFaces'])
 assert not cleanup['play'] and not cleanup['dirty']
 assert cleanup['scenePreserved'] and cleanup['pipelineRestored'] and cleanup['windowPreferencesRestored']
 images=[]
 for resolution in [(1920,1080),(960,540)]:
  p=folder/f'full_output_offscreen_{resolution[0]}x{resolution[1]}.png'
  assert Image.open(p).size==resolution
  images.append(dict(resolution=list(resolution),**bind(p)))
 profiles.append(dict(family=family,level=capture['level'],capture=bind(folder/'capture.json'),cleanup=bind(folder/'cleanup.json'),full_runtime_canvas=images,battle_only_comparisons=[bind(folder/(case+'_battle_rt.png')) for case in ['full','no_contact','no_diffuse','no_local_lighting','no_post']],diffuse_surfaces=capture['diffuseSurfaces'],mapped_ground_materials=capture['mappedGroundMaterials'],mapped_prop_materials=capture['mappedPropMaterials'],positive_face_normals=True))
paths=[ROOT/'UnityProject/Assets/Game/Runtime/Presentation'/f for f in ['Hd2DBattleLighting.cs','Hd2DBattlefieldSample.cs','Hd2DBattlefieldProfile.cs','Hd2DBattlefieldMaterialMaps.cs','Hd2DBattlefieldPropMaps.cs','Hd2DBattleCrystalCandidates.cs']]
paths += [ROOT/'UnityProject/Assets/Game/Resources/Shaders'/f for f in ['Hd2DGroundedSprite.shader','Hd2DBakedArchitecture.shader','Hd2DWetCourtyard.shader']]
paths += [ROOT/'UnityProject/Assets/Game/Editor'/f for f in ['Hd2DContinuousBattleCapture.cs','Hd2DBattleCanvasCapture.cs','Hd2DBattlefieldPropProbe.cs']]
paths += [ROOT/'UnityProject/Assets/Game/Runtime/Presentation/CombatFormalVisualAssets.cs',ROOT/'UnityProject/Assets/Game/Runtime/Presentation/CombatPrototypeBootstrap.cs']
paths += [ROOT/'UnityProject/Assets/Game/Runtime/Presentation/Hd2DGroundAttachmentMaps.cs',ROOT/'UnityProject/Assets/Game/Editor/Hd2DGroundAttachmentProbe.cs']
paths += [ROOT/'Tools/OCCArt'/f for f in ['bake_authored_normal_fields.py','build_authored_material_batch.py','tests/test_authored_normal_fields.py','tests/test_authored_material_recipes.py']]
paths += [OUT/'ground_material_recipes_v1.json',OUT/'ground_authored_batch_r1/authored_heights.json']
paths += [ROOT/'UnityProject/Assets/Game/Runtime/Presentation/Hd2DEnvironmentMaterialMaps.cs',ROOT/'UnityProject/Assets/Game/Runtime/Presentation/Hd2DAcademyEnvironment.cs',ROOT/'UnityProject/Assets/Game/Editor/Hd2DBattleLocalEvidence.cs',ROOT/'Tools/OCCArt/build_authored_sprite_material_batch.py',OUT/'remaining_sprite_material_recipes_v2.json',OUT/'remaining_sprite_material_batch_r2/batch_result.json']
paths += [ROOT/r['source']['path'] for r in json.loads((OUT/'reference_inventory.json').read_text(encoding='utf-8'))]
assert all(p.exists() for p in paths),'Missing changed source; never silently omit its fingerprint'
paths += list((ROOT/'UnityProject/Assets/Game/Resources/HD2DMaterialData').rglob('*.png'))
paths += list((ROOT/'UnityProject/Assets/Game/Resources/Art/HD2DBattleCrystalCandidates').rglob('*.png'))
for p in list(paths):
 meta=Path(str(p)+'.meta')
 if meta.exists():paths.append(meta)
record=dict(schema='occ-battle-six-family-local-evidence-v1',status='QA_PENDING',collected_at=datetime.now(timezone.utc).isoformat(),source='Current Unity Editor isolated preview entry, complete offscreen runtime UI, no GameView focus; not normal-entry or Player verification',profiles=profiles,current_input_fingerprints=[bind(p) for p in sorted(set(paths))],editor=dict(project='E:/数据库/OCC_Codex/UnityProject',compilation='up_to_date',affected_shader_messages=0,console_errors_since_cursor1716=0,quality='Very Low',cpu='AMD Ryzen 7 5800H',gpu='NVIDIA GeForce RTX 3060 Laptop GPU',graphics_memory_mb=5994),probe=dict(remaining_state_materials=4,status='completed'),limitations=['Only local isolated Play Mode evidence, no normal-entry journey or Player build','No CPU/GPU/P95 performance measurement','No complete actual source-pixel/UV density or construction inventory','No human art approval','Ground attachments20 still lack authored surface-region data','Three crystal states remain incomplete; pressure automatic grid PASS is not admitted','Facade/glass/vegetation material limitations remain; cloud calibration not completed'],human_review='PENDING')
record['editor'].pop('console_errors_since_cursor1716')
record['editor'].update(console_errors_since_cursor1779=0,historical_exception='seq1779 UnityEditor.QuickInstall string conversion exception after reload; not a material pass or fixed by this change')
record['limitations'].remove('Ground attachments20 still lack authored surface-region data')
record['limitations'].append('Ground attachment structure templates are candidate approximations; exact region alignment and human review pending')
record['probe']['ground_attachment_batch']=bind(OUT/'ground_probe/batch_state.json')
record['probe']['authored_batch_integrity']=bind(OUT/'authored_ground_batch_integrity.json')
record['editor'].pop('console_errors_since_cursor1779',None)
record['editor'].pop('historical_exception',None)
console=json.loads((OUT/'remaining_batch_console.json').read_text(encoding='utf-8-sig'))['data']['result']
assert not console['groundTruth']['compilationFailed'] and not console['groundTruth']['compiling'] and console['groundTruth']['consoleErrors']==0
record['editor']['console_snapshot']=bind(OUT/'remaining_batch_console.json')
record['editor']['historical_errors']='Retained history includes QuickInstall, fixed namespace, and fixed texture-free rain inventory errors; current Console ground truth has zero errors.'
record['limitations']=['Local isolated Editor preview only; no normal-entry journey or standalone Player performance claim',
 'Local camera CPU submission samples exist; full-frame CPU/GPU/P95 budget remains unmeasured',
 'Complete authored-pixel density and construction inventory/human review pending',
 'Crystal candidate appearance approved by user; overall scene quality and remaining material registration review pending',
 'Ground and sprite geometry/semantic region templates are approximations; exact alignment review pending',
 'Opaque glass/coating approximation only; vegetation has no leaf transmission',
 'Sunset/rain-night are local previews; no production day cycle, roof rain occlusion or splashes']
record['probe']['crystal_material_variants']=8
record['probe']['vegetation_states']=2
record['probe']['remaining_numeric_batch']=bind(OUT/'remaining_batch_integrity_v2.json')
for profile in record['profiles']:
 capture=json.loads((ROOT/profile['capture']['path']).read_text(encoding='utf-8'))
 profile['mapped_ground_attachments']=capture['mappedGroundAttachments']
 profile['mapped_remaining_batch']=capture['mappedEnvironment']
 folder=OUT/'profiles'/profile['family']
 profile['current_local_density_configuration']=bind(folder/'local_density_configuration.json')
 if profile['family'] in ['AcademyCourt','Conservatory']:
  profile['local_camera_motion_cost']=bind(folder/'local_motion_render_cost.json')
(OUT/'six_family_local_evidence.json').write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'profiles':len(profiles),'input_files':len(paths),'status':'QA_PENDING'}))
report_path=OUT/'quality_candidate_20261012_r1.json'
if report_path.exists():
 report=json.loads(report_path.read_text(encoding='utf-8'))
 current_inputs={v['path']:v for v in report['inputs']}
 current_inputs.update({v['path']:v for v in record['current_input_fingerprints']})
 report['inputs']=list(current_inputs.values())
 report['changed_files']=[bind(p)['path'] for p in paths if p.suffix in ['.cs','.shader']]
 report['runtime']['record']=bind(OUT/'six_family_local_evidence.json')
 report['runtime']['console_clear']=True
 report['performance']['device']='Current computer: AMD Ryzen7 5800H / RTX3060 Laptop6GB'
 report['performance']['quality_tier']='Very Low'
 report['blockers']=record['limitations']
 report['local_diagnostic_evidence']=profiles
 report_path.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
