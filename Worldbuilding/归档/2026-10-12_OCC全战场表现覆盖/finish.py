import json,hashlib,shutil,subprocess
from pathlib import Path
from PIL import Image
a=Path(__file__).resolve().parent;root=a.parents[2]
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
report=json.loads((a/'quality_candidate.json').read_text(encoding='utf-8'))
files=['Hd2DBattlefieldProfile.cs','Hd2DBattlefieldProfile.cs.meta','Hd2DBattlefieldSample.cs','Hd2DAcademyEnvironment.cs','Hd2DServiceScene.cs']
report['changed_files']=['UnityProject/Assets/Game/Runtime/Presentation/'+n for n in files]+['UnityProject/Assets/Game/Editor/Hd2DBattlefieldCoverageAudit.cs','UnityProject/Assets/Game/Editor/Hd2DBattlefieldCoverageAudit.cs.meta','UnityProject/Assets/Game/Editor/AcademyJourneyAudit.cs']
report['changed_files'].append('Tools/OCCArt/hd2d_baselines_v1.json')
paths=set(report['changed_files'])|{'UnityProject/Assets/Game/Runtime/Campaign/GeneratedBattleMapCatalog.cs','UnityProject/Assets/Game/Runtime/Presentation/Hd2DCourtyardWaterMaterial.cs','UnityProject/Assets/Game/Resources/Shaders/Hd2DCourtyardWater.shader','UnityProject/Assets/Game/Resources/Shaders/Hd2DWetCourtyard.shader','UnityProject/ProjectSettings/ProjectVersion.txt'}
contract=json.loads((root/'Tools/OCCArt/occ_art_contract_v1.json').read_text(encoding='utf-8-sig'))
paths.update(contract['hd2d_quality_policy']['profiles']['battlefield']['required_inputs'])
report['inputs']=[{'path':p,'sha256':sha(root/p)} for p in sorted(paths)]
source=root/'UnityProject/ReviewEvidence/20261012_hd2d_coverage';dest=a/'evidence';shutil.copytree(source,dest,dirs_exist_ok=True)
report['additional_evidence']=[{'path':p.relative_to(root).as_posix(),'sha256':sha(p)} for p in sorted(dest.rglob('*')) if p.is_file()]
catalog=json.loads((dest/'catalog.json').read_text(encoding='utf-8'))
journeys={r:json.loads((dest/r/'journey-status.json').read_text(encoding='utf-8')) for r in ['1080','540']}
for r,expected in [('1080',(1920,1080)),('540',(960,540))]:
 for node in ['B1','B2','B3','X']:
  assert Image.open(dest/r/(node+'.png')).size==expected,(r,node,'wrong actual framebuffer size')
cleanup={r:json.loads((dest/r/'cleanup.json').read_text(encoding='utf-8')) for r in ['1080','540']}
report['runtime_observations']={'catalog':catalog,'journeys':journeys,'cleanup':cleanup,'image_sizes':{p.relative_to(dest).as_posix():Image.open(p).size for p in dest.rglob('*.png')}}
report['configuration']['material_profile']={'resolver':'Hd2DBattlefieldProfile','families':sorted({x['family'] for x in catalog['levels']}),'wet_overrides':[x['id'] for x in catalog['levels'] if x['wetStone']],'water':'actual connected map regions; exact topology invalidation; native32 world sampling','crystals':'all IsAetherCrystal with positive durability, 1.8 intensity, range2.3, layer15','reflection':'scene-owned256 cubemap; existing water garden; coalesced captures'}
report['runtime']['normal_entry']=all(j['status'].startswith('PASS:') for j in journeys.values())
report['runtime']['interaction_pass']=report['runtime']['normal_entry']
report['runtime']['resource_lifetime_pass']=all(not c['play'] and c['battleWorlds']==0 and not c['dirty'] for c in cleanup.values())
report['status']='QA_PENDING';report['blockers']=['18 catalog checks are build/assets coverage, not 18 normal-entry visual approvals','Scene-specific art still incomplete','Complete pixel density/construction inventories and human review pending','Player60FPS/P95 16.7ms unmeasured','Water shoreline uses component bounding rectangle; nonrectangular shores remain approximate','Dynamic water invalidation is wired; no dedicated gameplay cast/removal route in this run']
(a/'quality_candidate.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
r=subprocess.run(['python','-X','utf8',str(root/'Tools/OCCArt/validate_hd2d_quality.py'),str(a/'quality_candidate.json'),'--scope','battlefield'],capture_output=True,text=True,encoding='utf-8');(a/'quality_gate.json').write_text(r.stdout,encoding='utf-8')
print(json.dumps({'catalog':catalog['count'],'catalog_failures':catalog['failures'],'journeys':journeys,'gate_exit':r.returncode},ensure_ascii=False))
