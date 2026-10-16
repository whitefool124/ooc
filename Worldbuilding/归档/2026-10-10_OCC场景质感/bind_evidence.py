from pathlib import Path
import json, hashlib

folder=Path(__file__).resolve().parent
root=folder.parents[2]
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
report_path=folder/'quality_candidate.json'
report=json.loads(report_path.read_text(encoding='utf-8'))
changed=['UnityProject/Assets/Game/Resources/Art/MainMenuE/Hd2D/Hd2DSurface.shader','UnityProject/Assets/Game/Runtime/Presentation/Hd2DDeskArtCandidate.cs','UnityProject/Assets/Game/Runtime/Presentation/Hd2DLoadoutScene.Lighting.cs']
report['changed_files']=changed
for path in changed:
    report['inputs']=[x for x in report['inputs'] if x['path']!=path]
    report['inputs'].append({'path':path,'sha256':sha(root/path)})
def readback(name):
    payload=json.loads((folder/name).read_text(encoding='utf-8'))
    tool=json.loads(payload['result']['content'][0]['text'])
    return json.loads(tool['data']['result'])
report['runtime_observations']={name:readback(name+'.json') for name in ['desk540_readback','spell1080_readback','close_readback']}
report['additional_evidence']=[{'path':p.relative_to(root).as_posix(),'sha256':sha(p)} for p in sorted((folder/'evidence').glob('occ_scene_final*.png'))]
report['blockers']=['Human aesthetic and full pixel density acceptance pending','Player shader/AO inclusion and performance not verified','QuickInstaller Console exception recorded separately; Console not declared clear']
report_path.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Bound final source hashes, real readbacks and screenshots; QA_PENDING retained')
