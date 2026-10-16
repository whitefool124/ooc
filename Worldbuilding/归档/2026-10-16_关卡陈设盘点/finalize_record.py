import hashlib,json,xml.etree.ElementTree as ET
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3];HERE=Path(__file__).resolve().parent
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def rel(p):return p.relative_to(ROOT).as_posix()
code=['UnityProject/Assets/Game/Runtime/Presentation/AcademyBattlefieldLayoutCatalog.cs','UnityProject/Assets/Game/Runtime/Presentation/Hd2DBattlefieldProfile.cs','UnityProject/Assets/Game/Runtime/Presentation/Hd2DAcademyEnvironment.cs','UnityProject/Assets/Game/Editor/StageSceneryBindingAudit.cs','UnityProject/Assets/Game/Editor/StageSceneryRuntimeScreenshots.cs','UnityProject/Assets/Game/Tests/EditMode/StageSceneryBindingTests.cs']
csvs=['OCC_学院第一阶段美术资产生产清单_v1.0.csv','OCC_学院第一阶段全流程内容清单_v1.0.csv','OCC_第一阶段战斗地图与敌人美术缺口矩阵_v1.0.csv']
code += ['Worldbuilding/数据表/'+n for n in csvs]
assets=[ROOT/'UnityProject/Assets/Game/Resources/Art/FormalAcademyStructures32'/f'{id}.png' for id in ['academy_low_bookcase_2x1','academy_smithing_table_2x1','academy_tool_cabinet_2x1']]
report=json.loads((HERE/'quality_report.json').read_text(encoding='utf-8-sig'))
inputs={r['path']:r for r in report['inputs']}
for p in [ROOT/c for c in code]+assets+[HERE/'bindings.json']:
    inputs[rel(p)]={'path':rel(p),'sha256':sha(p)}
report['changed_files']=code;report['inputs']=list(inputs.values())
test=ET.parse(HERE/'tests.xml').getroot().attrib
report['local_validation']={'passed':int(test['passed']),'failed':int(test['failed']),'record':rel(HERE/'tests.xml'),'sha256':sha(HERE/'tests.xml')}
report['local_runtime_captures']=[{'path':rel(p),'sha256':sha(p)} for p in (HERE/'runtime').glob('*.png')]
report['runtime']['record']={'path':rel(HERE/'runtime/capture.json'),'sha256':sha(HERE/'runtime/capture.json')}
report['blockers']=['QA_PENDING: two actual catalog encounters were loaded in a disposable preview run; normal random journey, performance, full density inventory and human approval remain separate. Introductory goal/turn panels remain visible in the captures; they are not clean art-review images.']
(HERE/'quality_report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
bindings=json.loads((HERE/'bindings.json').read_text(encoding='utf-8-sig'))
(HERE/'delivery.json').write_text(json.dumps({'status':'QA_PENDING','totals':bindings['totals'],'tests':test,'inputs':report['inputs'],'runtime_images':report['local_runtime_captures']},ensure_ascii=False,indent=2),encoding='utf-8')
print('Bound current scene inputs, three reused assets, inventory and four runtime images')
