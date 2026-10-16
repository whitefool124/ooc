"""Record real native candidate outputs and bounded checks; no promotion."""
import hashlib
import json
import re
import xml.etree.ElementTree as ET
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[2]
BASE=ROOT/'ArtSource/enemy_independent_20261016'
DETAILS={
 'elder_tracker_hound':dict(name='老寻',round=2,identity='灰毛老练寻迹兽；旧伤、补焊铜缚环、略僵的腿与下探鼻端。',
     rejections={'1':'108x47主体超过同尺度犬类画布；整体颗粒加粗，不缩放。'}),
 'wind_librarian':dict(name='小铃',round=3,identity='盘发图书管理员；夹起袖口、颈挂老花镜、灰蓝工作裙与引风短杖。',
     rejections={'1':'网格置信度1.0723，低于1.2；主体高62也超过人形46–58基准。','2':'网格稳定但主体仍高62；只修正原料信息密度。'}),
 'legacy_storekeeper':dict(name='老库管',round=4,identity='灰发老保管；工作帽、老花镜、袖套、臂下登记簿与铜质检定器。',
     rejections={'1':'27x67超过固定人形画布，登记簿辨识不足。','2':'38x90超出固定画布，细部次级网格仍过密。','3':'21x53机器合格，但登记簿仍不清楚；新原料强化该既有身份物。'})
}
def relative(p): return p.relative_to(ROOT).as_posix()
def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,data): p.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def binding(p): return dict(path=relative(p),sha256=hashlib.sha256(p.read_bytes()).hexdigest())

# Artifact contact only. Every unit is shown at the same 4x native scale.
board=Image.new('RGBA',(800,304),(103,108,111,255))
draw=ImageDraw.Draw(board)
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',18)
for unit,name,x in [('signal_keeper','灯台值守',32),('elder_tracker_hound','老寻',176),('wind_librarian','小铃',456),('legacy_storekeeper','老库管',608)]:
    manifest=read(BASE/unit/'manifest.occ-art-manifest-v1.json')
    image=Image.open(ROOT/manifest['delivery']['output_path']).convert('RGBA')
    board.alpha_composite(image.resize((image.width*4,image.height*4),Image.Resampling.NEAREST),(x,8+(64-image.height)*4))
    draw.text((x+image.width*2,282),name,font=font,fill=(246,240,220,255),anchor='mm')
board.save(BASE/'four_units_contact_4x.png')

for unit,details in DETAILS.items():
    folder=BASE/unit
    manifest=read(folder/'manifest.occ-art-manifest-v1.json')
    decode=read(folder/'qa'/f"r{details['round']}_decode.json")
    write(folder/'brief.json',dict(asset_id=manifest['asset_id'],content_id=unit,identity=details['identity'],
        identity_source='Worldbuilding/OC世界观/04_教育与组织/学院敌人设定.md',
        role=manifest['role'],canvas=decode['target_canvas'],native_ppu=32,world_pixel_pitch=1/32,logical_cells=[1,1],
        native_anchor=[decode['target_canvas'][0]//2,decode['target_canvas'][1]-2],
        grain_reference='ArtSource/enemy_independent_20261016/signal_keeper/asset/signal_keeper_battle_candidate_32x64.png',
        view='strict side profile facing right',source_channel='codex_builtin_imagegen',status='QA_PENDING'))
    write(folder/'status.json',dict(status='QA_PENDING',selected_round=details['round'],rejections=details['rejections'],
        subject_lattice_confidence=decode['subject_lattice_confidence'],source_logical_size=decode['subject_logical_size'],
        independent_source_each_round=True,geometric_resize=False,human_review='PENDING',formal_registry_replaced=False))

# Allow metadata/contact preparation while the Editor is still compiling.
test_path=HERE/'tests.xml'
if not test_path.exists():
    print('Candidate metadata/contact recorded; Unity checks still running')
    raise SystemExit(0)
tests=ET.parse(test_path).getroot()
assert tests.get('failed')=='0' and tests.get('passed')=='8', 'Do not record incomplete or failing checks as passed'
changed=[
 'UnityProject/Assets/Game/Runtime/Presentation/EnemyUnitArtCandidates.cs',
 'UnityProject/Assets/Game/Editor/EnemyUnitCandidateImporter.cs',
 'UnityProject/Assets/Game/Tests/EditMode/EnemyUnitArtCandidateTests.cs',
 'ArtSource/enemy_independent_20261016/prepare_candidate.py'
]
input_paths=[ROOT/v for v in changed]
for unit in DETAILS:
    folder=BASE/unit
    path=folder/'manifest.occ-art-manifest-v1.json'
    manifest=read(path)
    asset=ROOT/'UnityProject/Assets/Game/Resources/Art/EnemyUnitCandidates32x64'/f'{unit}.png'
    assert hashlib.sha256(asset.read_bytes()).hexdigest()==manifest['delivery']['output_sha256']
    meta=Path(str(asset)+'.meta')
    guid=re.search(r'^guid: (\w+)',meta.read_text(encoding='utf-8'),re.M).group(1)
    manifest['unity_import']=dict(asset_path=relative(asset),stable_guid=guid,importer_verified=True,runtime_verified=False,
        filter_mode='Point',mesh_type='Full Rect',compression='None',mipmaps=False,wrap_mode='Clamp',native_ppu=32,
        review_scope='Editor candidate texture lookup and actual importer checks; camera/motion unverified')
    write(path,manifest)
    changed.extend([relative(asset),relative(meta)])
    input_paths.extend([asset,meta,path,ROOT/manifest['provenance']['source_path'],ROOT/manifest['delivery']['output_path'],
        folder/'qa'/f"r{DETAILS[unit]['round']}_decode.json",folder/'qa/asset_validation.json'])
report_path=HERE/'quality_report.json'
report=read(report_path)
inputs={item['path']:item for item in report['inputs']}
for p in input_paths+[HERE/'tests.xml',HERE/'editor.log',HERE/'完成记录.md',BASE/'four_units_contact_4x.png']:
    inputs[relative(p)]=binding(p)
report['changed_files']=changed
report['inputs']=list(inputs.values())
report['local_checks']=dict(asset_validation='3/3 PASS',editmode='8/8 PASS for all four candidates',
    contact='2D 4x native composition only',normal_entry='NOT_RUN',human_review='PENDING',formal_registry_replaced=False)
write(report_path,report)
print('Three additional candidates recorded; QA_PENDING, no human approval asserted')
