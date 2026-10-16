import hashlib
import json
import shutil
import subprocess
from pathlib import Path

archive=Path(__file__).resolve().parent
root=archive.parents[2]
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
report_path=archive/'quality_candidate.json'
report=json.loads(report_path.read_text(encoding='utf-8-sig'))
report['changed_files']=['UnityProject/Assets/Game/Runtime/Presentation/'+n for n in ['Hd2DAcademyEnvironment.cs','Hd2DBattlefieldSample.cs','FormalBattlefieldView.cs']]
paths={i['path'] for i in report['inputs']}
paths.update(report['changed_files'])
paths.add('UnityProject/Assets/Game/Resources/Art/FormalAcademyIndependentFloors32/academy_block_court_b.png')
report['inputs']=[{'path':p,'sha256':sha(root/p)} for p in sorted(paths)]
folder=archive/'evidence';folder.mkdir(exist_ok=True)
report['additional_evidence']=[]
for n in ['occ_battle_light_before.png','occ_battle_stone_final_1080.png','occ_battle_stone_final_540.png','occ_battle_stone_victory.png']:
    target=folder/n;shutil.copy2(root/'UnityProject/ReviewEvidence'/n,target)
    report['additional_evidence'].append({'path':target.relative_to(root).as_posix(),'sha256':sha(target)})
report['runtime_observations']={'normal_entry':'New game / map B1 / enter combat, rain_lantern_court. Verification save scope, restored on ExitPlay.','resolutions':[[1920,1080],[960,540]],'cell_projection':'30/30 round trips before final wall/panel correction; final correction does not alter camera or picking.','combat':json.loads(json.loads((archive/'battle_outcome.json').read_text(encoding='utf-8'))['content'][0]['text'])['data']['result'],'material':'Existing native 32x32 stone tile reused with runtime repeat/Point copies; face UV measured in world units; 32 texels per world unit. Source textures/importers unchanged. Full visible-asset density audit remains pending.','rejected_verification':'Pre-fix 540p selected enemy panel blocked second click. Opposite-side placement corrected; post-fix full fight reached victory. Attempt to recapture through same occupied verification slot failed; no player save overwritten, restarted with fresh verification scope.'}
report['configuration'].update({'exposure':.08,'main_light':{'intensity':2.6,'color':[1,.94,.84]},'fill_light':{'intensity':.6,'color':[.7,.82,1]},'contrast':9,'saturation':2})
report['status']='QA_PENDING'
report['blockers']=['Full scene construction and pixel-density review pending','Human aesthetic approval pending','Player performance and full visual gate evidence pending']
report_path.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
r=subprocess.run(['python','-X','utf8',str(root/'Tools/OCCArt/validate_hd2d_quality.py'),str(report_path),'--scope','battlefield'],capture_output=True,text=True,encoding='utf-8')
(archive/'quality_gate_result.json').write_text(r.stdout,encoding='utf-8')
print(json.dumps({'status':report['status'],'evidence':len(report['additional_evidence']),'gate_exit':r.returncode}))
