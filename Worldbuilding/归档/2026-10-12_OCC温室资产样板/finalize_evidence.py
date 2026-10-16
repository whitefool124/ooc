import hashlib
import json
import subprocess
import sys
from pathlib import Path

folder=Path(__file__).resolve().parent
root=folder.parents[2]
report_path=folder/'quality_candidate.json'
report=json.loads(report_path.read_text(encoding='utf-8'))
files=[p for p in folder.iterdir() if p.is_file() and p.name not in {'quality_candidate.json','quality_gate.json'}]
bindings=[{'path':p.relative_to(root).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(files)]
code_paths=['UnityProject/Assets/Game/Resources/Shaders/Hd2DBakedArchitecture.shader','UnityProject/Assets/Game/Runtime/Presentation/Hd2DAcademyEnvironment.cs','UnityProject/Assets/Game/Editor/Hd2DBattlefieldCoverageAudit.cs']
code_bindings=[{'path':p,'sha256':hashlib.sha256((root/p).read_bytes()).hexdigest()} for p in code_paths]
report['changed_files']=[b['path'] for b in bindings+code_bindings]
all_inputs={b['path']:b for b in report['inputs']+bindings+code_bindings}
report['inputs']=list(all_inputs.values())
report['additional_evidence']=bindings
report['configuration']['material_profile']={'candidate':'independent greenhouse facade albedo; Editor-only preview','source_density':'one decoded native pixel per 1/32m; 1080 same-frame available; human review pending','architecture_visibility':'SurfaceData.occlusion; final RGB multiplication removed','normal':'NOT_PROVIDED','surface':'NOT_PROVIDED','metal_mask':'NOT_PROVIDED'}
report['runtime_observations']={'local_application_1080':json.loads((folder/'application_1080.json').read_text(encoding='utf-8')),'full_journey':'Interrupted at B3; not completed and not claimed as PASS','compilation':'Completed with no errors before interruption; current API authentication unavailable'}
report['blockers']=['Editor-only 1080 application available; 540/motion evidence missing','Building upper facade cropped by current camera/HUD; complete composition needs review','Pipeline now returns401; no further Editor actions performed','Companion normal/surface/metal data, full density/construction inventory, human review and Player performance remain pending']
report['status']='QA_PENDING'
report_path.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
result=subprocess.run([sys.executable,'-X','utf8',str(root/'Tools/OCCArt/validate_hd2d_quality.py'),str(report_path),'--scope','battlefield'],capture_output=True,text=True,encoding='utf-8')
(folder/'quality_gate.json').write_text(result.stdout,encoding='utf-8')
print(json.dumps({'quality_gate_exit':result.returncode,'candidate':'QA_PENDING','applied_to_Unity':'Editor-only1080preview'}))
