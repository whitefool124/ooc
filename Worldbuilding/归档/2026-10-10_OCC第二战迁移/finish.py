import json,hashlib,shutil,subprocess
from pathlib import Path
a=Path(__file__).resolve().parent;root=a.parents[2]
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
report=json.loads((a/'quality_candidate.json').read_text(encoding='utf-8'))
report['changed_files']=['UnityProject/Assets/Game/Runtime/Presentation/FormalCombatHud.cs','UnityProject/Assets/Game/Runtime/Presentation/Hd2DBattlefieldSample.cs','UnityProject/Assets/Game/Editor/AcademyJourneyAudit.cs']
inputs=set(report['changed_files'])|{'UnityProject/Assets/Game/Runtime/Campaign/GeneratedBattleMapCatalog.cs','UnityProject/Assets/Game/Runtime/Presentation/Hd2DAcademyEnvironment.cs','UnityProject/Assets/Game/Resources/Shaders/Hd2DBakedArchitecture.shader'}
report['inputs']=[{'path':p,'sha256':sha(root/p)} for p in sorted(inputs)]
e=a/'evidence';e.mkdir(exist_ok=True);report['additional_evidence']=[]
for n in ['occ_route_reward_blocked.png','occ_greenhouse_1080.png','occ_greenhouse_final_1080.png','occ_greenhouse_lights_off_1080.png','occ_greenhouse_final_540.png','occ_greenhouse_victory.png']:
 p=e/n;shutil.copy2(root/'UnityProject/ReviewEvidence'/n,p);report['additional_evidence'].append({'path':p.relative_to(root).as_posix(),'sha256':sha(p)})
def read(n):return json.loads(json.loads(json.loads((a/n).read_text(encoding='utf-8'))['content'][0]['text'])['data']['result'])
report['runtime_observations']={'entry':read('runtime.json'),'combat':read('combat.json'),'cleanup':read('cleanup.json')}
report['configuration']['local_lights']={'greenhouse_crystal':{'type':'Point','intensity':1.8,'range':2.3,'colour':[.36,.67,1],'shadows':False,'layer':15,'condition':'IsAetherCrystal && Durability>0','scope':'only B2 formal level id; owned by battlefield world'}}
report['status']='QA_PENDING';report['blockers']=['Greenhouse-specific environment art incomplete; common courtyard still used','Complete density/construction inventory and human aesthetic approval pending','Player 60FPS / P95 16.7ms unmeasured']
(a/'quality_candidate.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
r=subprocess.run(['python','-X','utf8',str(root/'Tools/OCCArt/validate_hd2d_quality.py'),str(a/'quality_candidate.json'),'--scope','battlefield'],capture_output=True,text=True,encoding='utf-8');(a/'quality_gate.json').write_text(r.stdout,encoding='utf-8');print(json.dumps({'status':report['status'],'gate_exit':r.returncode}))
