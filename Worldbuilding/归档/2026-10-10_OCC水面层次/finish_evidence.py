import json, hashlib, shutil, subprocess
from pathlib import Path
archive=Path(__file__).resolve().parent
root=archive.parents[2]
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
report=json.loads((archive/'quality_candidate.json').read_text(encoding='utf-8-sig'))
report['changed_files']=['UnityProject/Assets/Game/Runtime/Presentation/'+n for n in ['Hd2DAcademyEnvironment.cs','Hd2DBattlefieldSample.cs','Hd2DCourtyardWaterMaterial.cs','Hd2DCourtyardWaterMaterial.cs.meta']]+['UnityProject/Assets/Game/Resources/Shaders/Hd2DCourtyardWater.shader'+s for s in ['', '.meta']]
inputs={i['path'] for i in report['inputs']}|set(report['changed_files'])
report['changed_files'].append('UnityProject/Assets/Game/Editor/Hd2DCombatReviewAudit.cs')
inputs.add('UnityProject/Assets/Game/Editor/Hd2DCombatReviewAudit.cs')
inputs|={'UnityProject/Assets/Game/Resources/Art/FormalFirstBattle32/rain_court_water.png','UnityProject/Assets/Game/Resources/Art/FormalAcademyIndependentFloors32/academy_block_court_b.png'}
report['inputs']=[{'path':p,'sha256':sha(root/p)} for p in sorted(inputs)]
evidence=archive/'evidence';evidence.mkdir(exist_ok=True)
report['additional_evidence']=[]
for name in ['occ_water_candidate_1080.png','occ_water_final_1080.png','occ_water_final_540.png','occ_water_victory.png','occ_water_motion_t1.png','occ_water_motion_t6.png']:
 target=evidence/name;shutil.copy2(root/'UnityProject/ReviewEvidence'/name,target)
 report['additional_evidence'].append({'path':target.relative_to(root).as_posix(),'sha256':sha(target)})
motion=json.loads(json.loads((archive/'water_motion_receipt.json').read_text(encoding='utf-8'))['content'][0]['text'])['data']
video=evidence/'water_motion.mp4';shutil.copy2(motion['path'],video)
report['additional_evidence'].append({'path':video.relative_to(root).as_posix(),'sha256':sha(video)})
def read(n):
 return json.loads(json.loads(json.loads((archive/n).read_text(encoding='utf-8'))['content'][0]['text'])['data']['result'])
report['runtime_observations']={'water':read('water_runtime.json'),'combat':read('combat_outcome.json'),'cleanup':read('cleanup.json'),'motion':{'duration_seconds':motion['elapsed_seconds'],'frames':motion['frame_count'],'ready':motion['ready'],'not_performance_measurement':True}}
report['configuration']['material_profile']={'shader':'OCC/HD2D/CourtyardWater','source_sampling':'Point,32 texels/world unit','optical_depth_pool':.5,'optical_depth_tactical_water':.12,'smoothness':.9,'wave_strength':.028,'wave_speed':.7,'water_f0':.02,'shore':'authored rectangle distance, independent of albedo brightness','limitations':'Opaque authored absorption approximation, not screen-space refraction; existing local cubemap, not planar reflections.'}
report['status']='QA_PENDING'
report['blockers']=['Full density/construction inventory and human scene approval pending','Standalone Player performance budget unmeasured','Bright water highlights still sharp; physical refraction and accurate moving-object reflections incomplete']
(archive/'quality_candidate.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
r=subprocess.run(['python','-X','utf8',str(root/'Tools/OCCArt/validate_hd2d_quality.py'),str(archive/'quality_candidate.json'),'--scope','battlefield'],capture_output=True,text=True,encoding='utf-8')
(archive/'quality_gate_result.json').write_text(r.stdout,encoding='utf-8')
print(json.dumps({'status':report['status'],'gate_exit':r.returncode,'evidence_count':len(report['additional_evidence'])}))
