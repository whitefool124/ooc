import csv,hashlib,json,xml.etree.ElementTree as ET
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3];HERE=Path(__file__).resolve().parent
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def rel(p):return p.relative_to(ROOT).as_posix()
code=['UnityProject/Assets/Game/Runtime/Presentation/BattlefieldCellPresentation.cs','UnityProject/Assets/Game/Runtime/Presentation/CombatBattlefieldCellPresenter.cs','UnityProject/Assets/Game/Runtime/Presentation/CombatFormalVisualAssets.cs','UnityProject/Assets/Game/Runtime/Presentation/FormalBattlefieldView.cs','UnityProject/Assets/Game/Runtime/Presentation/Hd2DBattlefieldSample.cs','UnityProject/Assets/Game/Runtime/Presentation/Hd2DFieldEffectLabels.cs','UnityProject/Assets/Game/Runtime/Presentation/BattlefieldDeviceArtCandidates.cs','UnityProject/Assets/Game/Editor/BattlefieldDeviceCandidateReview.cs','UnityProject/Assets/Game/Tests/EditMode/DeviceMaterialPresentationTests.cs','UnityProject/Assets/Game/Editor/EnemyFieldRuntimeScreenshots.cs','Worldbuilding/数据表/OCC_学院第一阶段全流程内容清单_v1.0.csv']
art=ROOT/'ArtSource/device_material_candidates_20261016';unity=ROOT/'UnityProject/Assets/Game/Resources/Art/DeviceMaterialCandidates32'
files=[ROOT/p for p in code]+list(art.glob('*/asset/*'))+list(unity.glob('*'))
report=json.loads((HERE/'quality_report.json').read_text(encoding='utf-8-sig'))
bound={p['path']:p for p in report['inputs']}
for p in files:bound[rel(p)]={'path':rel(p),'sha256':sha(p)}
report['inputs']=list(bound.values());report['changed_files']=code+[rel(p) for p in unity.glob('*.png')]
test=ET.parse(HERE/'tests.xml').getroot().attrib
report['local_validation']={'tests':{'path':rel(HERE/'tests.xml'),'sha256':sha(HERE/'tests.xml'),'passed':int(test['passed']),'failed':int(test['failed'])},'asset_manifests_machine_passed':4,'source_geometric_resize':False}
report['local_runtime_captures']=[{'path':rel(p),'sha256':sha(p)} for p in (HERE/'runtime').glob('*.png')]
report['runtime']['record']={'path':rel(HERE/'runtime/capture.json'),'sha256':sha(HERE/'runtime/capture.json')}
report['blockers']=['QA_PENDING: artwork review is staged in-memory in B3, not a natural encounter or whole journey; no performance, full density inventory or formal human sample approval claimed. Intact and damaged devices share the new silhouette, destroyed devices retain existing shared rubble.']
(HERE/'quality_report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
a=list(csv.reader((HERE/'OCC_学院第一阶段全流程内容清单.before.csv').open(encoding='utf-8-sig',newline='')))
b=list(csv.reader((ROOT/'Worldbuilding/数据表/OCC_学院第一阶段全流程内容清单_v1.0.csv').open(encoding='utf-8-sig',newline='')))
changed=[(r+1,c+1) for r,(x,y) in enumerate(zip(a,b)) for c,(v,w) in enumerate(zip(x,y)) if v!=w];assert len(changed)==1
(HERE/'delivery.json').write_text(json.dumps({'status':'QA_PENDING','tests':test,'inventory_changed_cells':changed,'source_and_binding_inputs':report['inputs'],'runtime_images':report['local_runtime_captures']},ensure_ascii=False,indent=2),encoding='utf-8')
print('Current file hashes, four art manifests and runtime captures bound; inventory change isolated to one cell')
