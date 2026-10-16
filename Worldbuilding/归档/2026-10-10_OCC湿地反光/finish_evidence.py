import hashlib
import json
import shutil
import subprocess
from pathlib import Path
from PIL import Image, ImageChops, ImageStat

archive=Path(__file__).resolve().parent
root=archive.parents[2]
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
path=archive/'quality_candidate.json'
report=json.loads(path.read_text(encoding='utf-8-sig'))
report['changed_files']=[
 'UnityProject/Assets/Game/Runtime/Presentation/Hd2DBattlefieldSample.cs',
 'UnityProject/Assets/Game/Runtime/Presentation/Hd2DAcademyEnvironment.cs',
 'UnityProject/Assets/Game/Resources/Shaders/Hd2DWetCourtyard.shader',
 'UnityProject/Assets/Game/Resources/Shaders/Hd2DWetCourtyard.shader.meta']
inputs={i['path'] for i in report['inputs']}|set(report['changed_files'])
inputs.add('UnityProject/Packages/manifest.json')
report['inputs']=[{'path':p,'sha256':sha(root/p)} for p in sorted(inputs)]
evidence=archive/'evidence';evidence.mkdir(exist_ok=True)
names=['occ_wet_final_1080.png','occ_wet_final_540.png','occ_wet_dry_1080.png','occ_wet_no_reflection_1080.png','occ_wet_victory.png','occ_wet_candidate_1080.png']
report['additional_evidence']=[]
for name in names:
 target=evidence/name;shutil.copy2(root/'UnityProject/ReviewEvidence'/name,target)
 report['additional_evidence'].append({'path':target.relative_to(root).as_posix(),'sha256':sha(target)})
def unwrap(name):
 envelope=json.loads((archive/name).read_text(encoding='utf-8'))
 return json.loads(json.loads(envelope['content'][0]['text'])['data']['result'])
report['runtime_observations']={'configuration':unwrap('runtime_read.json'),'combat':unwrap('combat_outcome.json'),'shader':unwrap('shader_read.json')}
report['runtime_observations']['limitations']='Shader wetness uses an authored world-space analytic coverage profile and small analytic stone microfacet variation, independent of colour brightness. No complete authored normal/roughness maps; no performance budget claim or human approval. Initial 128px/weak wet-film candidate rejected for weak visible response. Final shader uses 256px local probe. Unity QuickInstaller assembly path conversion error remains in Console; stack does not enter OCC gameplay. Two temporary read snippets failed (namespace and Vector3 JSON loop) and were corrected; project source compiled.'
report['configuration'].update({'material_profile':{'shader':'OCC/HD2D/WetCourtyard','dry_smoothness':.08,'wet_smoothness':.76,'water_film_smoothness':.88,'stone_coverage':.42,'water_coverage':1,'normal_source':'analytic microfacet candidate, not albedo-derived or authored map'},'local_reflection':{'resolution':256,'hdr':True,'intensity':.8,'box_projection':True,'refresh':'after combat version changes settle; min 1s; no action/entry capture; waits for previous capture'}})
report['status']='QA_PENDING'
report['blockers']=['Complete construction/density and aesthetic review pending','Full visual gate and standalone Player performance pending','Authored normal/roughness data incomplete']
a=Image.open(evidence/'occ_wet_final_1080.png').convert('RGB')
diffs={}
for name in ['occ_wet_dry_1080.png','occ_wet_no_reflection_1080.png']:
 b=Image.open(evidence/name).convert('RGB')
 d=ImageChops.difference(a,b).crop((16,290,1904,845))
 diffs[name]={'mean_abs_rgb':ImageStat.Stat(d).mean,'pixels_over_3':sum(max(p)>3 for p in d.getdata()),'note':'Same camera and geometry; unpaused character animation can contribute. Pixel differences are not proof of aesthetic approval.'}
report['runtime_observations']['image_differences']=diffs
path.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
r=subprocess.run(['python','-X','utf8',str(root/'Tools/OCCArt/validate_hd2d_quality.py'),str(path),'--scope','battlefield'],capture_output=True,text=True,encoding='utf-8')
(archive/'quality_gate_result.json').write_text(r.stdout,encoding='utf-8')
print(json.dumps({'status':report['status'],'evidence_count':len(names),'gate_exit':r.returncode}))
