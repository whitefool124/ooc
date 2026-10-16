import hashlib,json,re
import xml.etree.ElementTree as ET
from pathlib import Path

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[2]
ART=ROOT/'ArtSource/field_effect_candidates_20261016/loose_paper'
def read(path):return json.loads(path.read_text(encoding='utf-8-sig'))
def write(path,data):path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def relative(path):return path.relative_to(ROOT).as_posix()
def bind(path):return dict(path=relative(path),sha256=hashlib.sha256(path.read_bytes()).hexdigest())

tests=ET.parse(HERE/'tests.xml').getroot()
assert tests.get('passed')=='13' and tests.get('failed')=='0'
manifest_path=ART/'manifest.occ-art-manifest-v1.json'
manifest=read(manifest_path)
asset=ROOT/'UnityProject/Assets/Game/Resources/Art/FieldEffectCandidates32/field_loose_paper.png'
assert bind(asset)['sha256']==manifest['delivery']['output_sha256']
meta=Path(str(asset)+'.meta')
guid=re.search(r'^guid: (\w+)',meta.read_text(encoding='utf-8'),re.M).group(1)
manifest['unity_import']=dict(asset_path=relative(asset),stable_guid=guid,importer_verified=True,runtime_verified=False,
    filter_mode='Point',mesh_type='Full Rect',compression='None',mipmaps=False,wrap_mode='Clamp',native_ppu=32,
    review_scope='actual importer plus read-only cell presentation and projected-label component checks; game camera unverified')
write(manifest_path,manifest)
write(ART/'status.json',dict(status='QA_PENDING',selected_round=2,
    rejected_rounds={'1':'42x24 exceeds 32x32 intact-subject target; soft external dark halo rejected'},
    subject_size=[21,25],canvas=[32,32],lattice_confidence=8.2206,geometric_resize=False,
    alpha_preserved=True,human_review='PENDING',formal_registry_replaced=False))

table=ROOT/'Worldbuilding/数据表/OCC_学院第一阶段全流程内容清单_v1.0.csv'
text=table.read_text(encoding='utf-8-sig')
old='"待制作盘点"'
new='"首批真实绑定盘点完成：4单位独立战斗候选齐备；散页候选及4类场地效果中文占位已接入；痕迹／约束纹／页幕、3类装置与碎晶仍缺独立表现，关卡专属陈设继续盘点；候选未晋升正式美术"'
if old in text:
    assert text.count(old)==1
    # Preserve the source file's original newline convention and UTF-8 BOM.
    raw=table.read_bytes();newline='\r\n' if b'\r\n' in raw else '\n'
    updated=text.replace(old,new).replace('\r\n','\n').replace('\n',newline)
    table.write_bytes((b'\xef\xbb\xbf' if raw.startswith(b'\xef\xbb\xbf') else b'')+updated.encode('utf-8'))
else:assert new in text

changed=[
 'UnityProject/Assets/Game/Runtime/Presentation/BattlefieldCellPresentation.cs',
 'UnityProject/Assets/Game/Runtime/Presentation/CombatBattlefieldCellPresenter.cs',
 'UnityProject/Assets/Game/Runtime/Presentation/CombatFormalVisualAssets.cs',
 'UnityProject/Assets/Game/Runtime/Presentation/FormalBattlefieldView.cs',
 'UnityProject/Assets/Game/Runtime/Presentation/Hd2DBattlefieldTactics.cs',
 'UnityProject/Assets/Game/Runtime/Presentation/Hd2DFieldEffectLabels.cs',
 'UnityProject/Assets/Game/Runtime/Presentation/BattlefieldFieldArtCandidates.cs',
 'UnityProject/Assets/Game/Editor/PixelArtImportGuard.cs',
 'UnityProject/Assets/Game/Editor/BattlefieldFieldCandidateReview.cs',
 'UnityProject/Assets/Game/Tests/EditMode/FieldEffectPresentationTests.cs',relative(asset),relative(meta),relative(table)]
report_path=HERE/'quality_report.json'
report=read(report_path)
inputs={item['path']:item for item in report['inputs']}
for path in [ROOT/p for p in changed]+[manifest_path,ROOT/manifest['provenance']['source_path'],ROOT/manifest['delivery']['output_path'],
    ART/'qa/selected_decode.json',ART/'qa/asset_validation.json',ART/'qa/floor_contact_6x.png',HERE/'tests.xml',HERE/'editor.log',HERE/'完成记录.md']:
    inputs[relative(path)]=bind(path)
report['inputs']=list(inputs.values());report['changed_files']=changed
report['local_checks']=dict(asset_validation='PASS',editmode='13/13 PASS',
    normal_entry='NOT_RUN',human_review='PENDING',placeholder_names=['散页','痕迹','约束纹','页幕'],formal_art_promotion=False)
write(report_path,report)
print('13 local checks passed; delivery bound as QA_PENDING')
