import json,hashlib,shutil,subprocess
from pathlib import Path
archive=Path(__file__).resolve().parent
root=archive.parents[2]
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
r=json.loads((archive/'quality_candidate.json').read_text(encoding='utf-8'))
r['changed_files']=['UnityProject/Assets/Game/Resources/Shaders/Hd2DCourtyardWater.shader','UnityProject/Assets/Game/Runtime/Presentation/Hd2DCourtyardWaterMaterial.cs']
inputs={i['path'] for i in r['inputs']}|set(r['changed_files'])|{'UnityProject/Assets/Game/Runtime/Presentation/Hd2DAcademyEnvironment.cs','UnityProject/Assets/Game/Runtime/Presentation/Hd2DBattlefieldSample.cs','UnityProject/Assets/Game/Resources/Art/FormalFirstBattle32/rain_court_water.png','UnityProject/Assets/Game/Resources/Art/FormalAcademyIndependentFloors32/academy_block_court_b.png'}
r['inputs']=[{'path':p,'sha256':sha(root/p)} for p in sorted(inputs)]
e=archive/'evidence';e.mkdir(exist_ok=True)
names=['occ_water_refract_1080.png','occ_water_refract_soft_1080.png','occ_water_refract_final_1080.png','occ_water_refract_final_540.png','occ_water_refract_previous_1080.png','occ_water_refract_victory.png','occ_water_refract_motion_t5.png']
r['additional_evidence']=[]
for n in names:
 p=e/n;shutil.copy2(root/'UnityProject/ReviewEvidence'/n,p);r['additional_evidence'].append({'path':p.relative_to(root).as_posix(),'sha256':sha(p)})
def unwrap(n):
 return json.loads(json.loads(json.loads((archive/n).read_text(encoding='utf-8'))['content'][0]['text'])['data']['result'])
r['runtime_observations']={'configuration':unwrap('runtime.json'),'combat':unwrap('combat.json'),'cleanup':unwrap('cleanup.json')}
motion=json.loads(json.loads((archive/'motion.json').read_text(encoding='utf-8'))['content'][0]['text'])['data']
p=e/'water_refraction.mp4';shutil.copy2(motion['path'],p);r['additional_evidence'].append({'path':p.relative_to(root).as_posix(),'sha256':sha(p)})
r['configuration']['material_profile']={'shader':'OCC/HD2D/CourtyardWater','smoothness':.82,'refraction_strength':.35,'air_water_ratio':1/1.333,'depth_source':'authored basin plane approximation','sampling':'Point; 32 texels/world unit after ray intersection','shore_limit':'min(.35, edge*.65)','reflection':'existing local cubemap; not planar reflection'}
r['runtime']['normal_entry']=True;r['runtime']['interaction_pass']='PASS' in r['runtime_observations']['combat']['status'];r['runtime']['resource_lifetime_pass']=r['runtime_observations']['cleanup']['waterMaterials']==0
r['status']='QA_PENDING';r['blockers']=['Human full-scene pixel density and aesthetic approval pending','Complete configuration/construction inventory pending','Player 60 FPS / P95 16.7ms unmeasured','Basin-only analytic refraction and cubemap approximation; not real displaced scene refraction or planar reflections']
(archive/'quality_candidate.json').write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
v=subprocess.run(['python','-X','utf8',str(root/'Tools/OCCArt/validate_hd2d_quality.py'),str(archive/'quality_candidate.json'),'--scope','battlefield'],capture_output=True,text=True,encoding='utf-8');(archive/'quality_gate.json').write_text(v.stdout,encoding='utf-8');print(json.dumps({'status':r['status'],'gate_exit':v.returncode,'evidence':len(r['additional_evidence'])}))
