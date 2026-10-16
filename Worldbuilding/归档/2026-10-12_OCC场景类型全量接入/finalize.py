import hashlib
import json
import shutil
import subprocess
from pathlib import Path

folder=Path(__file__).resolve().parent
root=folder.parents[2]
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
bind=lambda p:{'path':p.relative_to(root).as_posix(),'sha256':sha(p)}
write=lambda p,data:p.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
audit=json.loads((folder/'integration_audit.json').read_text(encoding='utf-8'))
assert len(audit['levels'])==18 and audit['reviewDisabled'] and not audit['dirty']
assert len({l['environment'] for l in audit['levels']})==6
assert not any(m['severity']=='Error' for s in audit['shaders'] for m in s['messages'])
registry=root/'Tools/OCCArt/hd2d_baselines_v1.json'
backup=folder/'before/hd2d_baselines_v1.json'
if not backup.exists():shutil.copy2(registry,backup)
data=json.loads(registry.read_text(encoding='utf-8'))
data['baselines']['battlefield-v1']['reason']='QA_PENDING: 18 formal catalog levels now route through six environment families; current import/profile/shader checks pass. Current scene screenshots, full density/construction review, human approval and Player performance remain pending. Historical four-battle journeys do not approve this revision.'
write(registry,data)

original=root/'Worldbuilding/归档/2026-10-12_OCC温室资产样板'
manifest=json.loads((original/'manifest.json').read_text(encoding='utf-8'))
delivery=root/'UnityProject'/audit['candidate']['asset']
assert sha(delivery)==manifest['delivery']['output_sha256']
manifest['delivery']['output_path']=delivery.relative_to(root).as_posix()
manifest['evidence'].pop('application_contact',None)
manifest['application']['runtime_draw_rect']='Conservatory background card; 8x3m native canvas at32PPU; position defined by Hd2DAcademyEnvironment; current application capture pending'
manifest['unity_import']={'stable_guid':audit['candidate']['guid'],'filter_mode':'Point','compression':'None','mipmaps':False,'importer_verified':True,'runtime_verified':False}
manifest['blockers']=['Current placement changed: historical1080 contact screenshot is not current evidence','Normal/surface/metal companion maps and scene density/human approval missing']
write(folder/'greenhouse_runtime_manifest.json',manifest)

files=['UnityProject/Assets/Game/Runtime/Presentation/Hd2DBattlefieldProfile.cs','UnityProject/Assets/Game/Runtime/Presentation/Hd2DAcademyEnvironment.cs','UnityProject/Assets/Game/Editor/Hd2DLocalIntegrationTools.cs','UnityProject/Assets/Game/Editor/Hd2DLocalIntegrationTools.cs.meta','UnityProject/Packages/com.unity.pipeline/Runtime/Common/BasePipelineServer.cs','UnityProject/Assets/Game/Resources/Art/HD2DAcademyCandidates/greenhouse_facade.png','UnityProject/Assets/Game/Resources/Art/HD2DAcademyCandidates/greenhouse_facade.png.meta','UnityProject/Assets/Game/Resources/Art/HD2DAcademyCandidates.meta','Tools/OCCArt/hd2d_baselines_v1.json']
report_path=folder/'quality_candidate.json'
report=json.loads(report_path.read_text(encoding='utf-8'))
report['changed_files']=files
inputs={b['path']:b for b in report['inputs']}
for p in files: inputs[p]=bind(root/p)
for level in audit['levels']:
    p=root/'UnityProject/Assets/Game/Resources'/(level['MasonryResource']+'.png');inputs[p.relative_to(root).as_posix()]=bind(p)
for p in (original/'greenhouse_raw_r1.png',original/'decode_r1.json',folder/'integration_audit.json',folder/'greenhouse_runtime_manifest.json'):
    inputs[p.relative_to(root).as_posix()]=bind(p)
report['inputs']=list(inputs.values())
report['additional_evidence']=[bind(folder/'integration_audit.json'),bind(folder/'README.md')]
report['configuration']['environment_families']=sorted({l['environment'] for l in audit['levels']})
report['runtime_observations']={'integration':audit,'verification_scope':'Editor import/profile/shader checks only. No new Play Mode, window focus or full journey.','pipeline_fix':'IPv4 loopback only; detect occupied ports with active TCP listeners and exclusive bind; current two-Editor addresses are7800 and7801'}
report['blockers']=['No current screenshots or motion review after family/placement changes','Paired material maps and dedicated setting artwork incomplete','Full visible density/construction inventories and genuine human approval pending','Player60FPS/P9516.7ms unmeasured']
report['status']='QA_PENDING';write(report_path,report)
result=subprocess.run(['python','-X','utf8',str(root/'Tools/OCCArt/validate_hd2d_quality.py'),str(report_path),'--scope','battlefield'],capture_output=True,text=True,encoding='utf-8')
(folder/'quality_gate.json').write_text(result.stdout,encoding='utf-8')
print(json.dumps({'levels':len(audit['levels']),'families':report['configuration']['environment_families'],'quality_exit':result.returncode,'status':'QA_PENDING'}))
