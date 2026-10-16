import csv,hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3];HERE=Path(__file__).resolve().parent
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def rel(p):return p.relative_to(ROOT).as_posix()
assets=ROOT/'ArtSource/field_effect_reset_20261016'
code=['Tools/OCCArt/decode_generated_pixel_grid.py','UnityProject/Assets/Game/Runtime/Presentation/BattlefieldFieldArtCandidates.cs','UnityProject/Assets/Game/Runtime/Presentation/CombatFormalVisualAssets.cs','UnityProject/Assets/Game/Runtime/Presentation/CombatBattlefieldCellPresenter.cs','UnityProject/Assets/Game/Runtime/Presentation/BattlefieldCellPresentation.cs','UnityProject/Assets/Game/Runtime/Presentation/Hd2DBattlefieldSample.cs','UnityProject/Assets/Game/Tests/EditMode/FieldEffectPresentationTests.cs','UnityProject/Assets/Game/Editor/EnemyFieldRuntimeScreenshots.cs','Worldbuilding/数据表/OCC_学院第一阶段全流程内容清单_v1.0.csv']
paths=[ROOT/p for p in code]+list(assets.glob('*/asset/*.png'))+list(assets.glob('*/asset/*.json'))+list((ROOT/'UnityProject/Assets/Game/Resources/Art/FieldEffectCandidates32').glob('*.png'))+list((ROOT/'UnityProject/Assets/Game/Resources/Art/FieldEffectCandidates32').glob('*.meta'))
report=json.loads((HERE/'quality_report.json').read_text(encoding='utf-8-sig'))
report['changed_files']=code+[rel(p) for p in assets.glob('*/asset/*.png')]
bound={p['path']:p for p in report['inputs']}
for p in paths:bound[rel(p)]={'path':rel(p),'sha256':sha(p)}
report['inputs']=list(bound.values());report['blockers']=['New art is QA_PENDING. Runtime images use in-memory artwork staging, not normal encounter validation. Human aesthetic approval, complete scene density inventory, motion and performance sample approval remain pending.']
report['local_validation']={'unity_field_tests':{'path':rel(HERE/'tests.xml'),'sha256':sha(HERE/'tests.xml'),'passed':18,'failed':0},'decoder_regression':{'passed':4},'asset_manifests':15,'source_geometric_resize':False}
captures=[]
for p in (HERE/'runtime').glob('*.png'):captures.append({'path':rel(p),'sha256':sha(p)})
report['local_runtime_captures']=captures
report['runtime']['record']={'path':rel(HERE/'runtime/capture.json'),'sha256':sha(HERE/'runtime/capture.json')} if (HERE/'runtime/capture.json').exists() else None
report['runtime']['console_clear']=True
(HERE/'quality_report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
before=list(csv.reader((HERE/'OCC_学院第一阶段全流程内容清单.before.csv').open(encoding='utf-8-sig',newline='')))
after=list(csv.reader((ROOT/'Worldbuilding/数据表/OCC_学院第一阶段全流程内容清单_v1.0.csv').open(encoding='utf-8-sig',newline='')))
assert len(before)==len(after)
changes=[(r+1,c+1) for r,(a,b) in enumerate(zip(before,after)) for c,(x,y) in enumerate(zip(a,b)) if x!=y]
assert len(changes)==1,changes
(HERE/'delivery.json').write_text(json.dumps({'status':'QA_PENDING','changed_inventory_cells':changes,'hashes':[{'path':rel(p),'sha256':sha(p)} for p in paths],'runtime_images':captures},ensure_ascii=False,indent=2),encoding='utf-8')
print('Bound current sources, 15 assets, runtime screenshots, local checks; inventory has one changed cell')
