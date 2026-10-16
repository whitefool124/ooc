from pathlib import Path
import json,hashlib,csv,io
root=Path(__file__).resolve().parents[3];folder=Path(__file__).resolve().parent
rel=lambda p:p.relative_to(root).as_posix()
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
manifest=root/'ArtSource/breach_ram_triangle_20261016/manifest.occ-art-manifest-v1.json'
m=json.loads(manifest.read_text('utf-8'))
capture=json.loads((folder/'runtime/capture.json').read_text('utf-8'))
assert capture['texture']=='breach_ram'
asset=root/m['unity_import']['asset_path'];meta=asset.with_suffix('.png.meta')
m['unity_import'].update(stable_guid=next(l.split(':',1)[1].strip() for l in meta.read_text('utf-8').splitlines() if l.startswith('guid:')),importer_verified=True,runtime_verified=True,review_scope='Actual first elite encounter in an isolated preview run; explicit Editor candidate; human review pending')
m['application']['review_scope']='Native64x64/32PPU candidate in actual first elite encounter; no natural full journey or formal art promotion'
manifest.write_text(json.dumps(m,ensure_ascii=False,indent=2),'utf-8')
for name in ['OCC_学院第一阶段美术资产生产清单_v1.0.csv','OCC_第一阶段战斗地图与敌人美术缺口矩阵_v1.0.csv','OCC_学院第一阶段全流程内容清单_v1.0.csv']:
 p=root/'Worldbuilding/数据表'/name;s=p.read_text('utf-8-sig');s=s.replace('对应像素待机候选处理中','64×64／32 PPU待机候选已接入Editor预览，尚未转为正式资源')
 if name.startswith('OCC_学院第一阶段全流程'):
  old='楔角独立外形、角色动作帧接入与塔内机关三类用途辨识仍待补'
  assert old in s
  s=s.replace(old,'楔角三角头待机候选已接入预览；动作采用已有位移和局部抖动；三类塔内机关以名称、放行状态和效果文字辨识，不再补成套动作帧或专用机关图标')
 if name.startswith('OCC_学院第一阶段美术'):
  rows=list(csv.reader(io.StringIO(s)))
  for row in rows[1:]:
   if row[3]=='breach_ram':
    row[5]='32 PPU；自适应64×64；旧总案三角楔头与侧面稳压环形象'
    row[6]='用户2026-10-16指定旧形象；独立待机候选通过网格与导入检查，实机预览已接入；QA_PENDING'
    row[7]='使用单张待机形象＋既有位移与抖动；锁线、浅水冷却与干地卸压使用公开意图和状态文字'
    row[8]='FIRST-X中保持三角头识别与实际状态可读；不再要求专属动作帧'
  out=io.StringIO(newline='');csv.writer(out,quoting=csv.QUOTE_ALL,lineterminator='\n').writerows(rows);s=out.getvalue()
 p.write_text('\ufeff'+s,'utf-8')
report=json.loads((folder/'quality_report.json').read_text('utf-8'))
files=[
 'UnityProject/Assets/Game/Runtime/Presentation/CombatBattlefieldCellPresenter.cs',
 'UnityProject/Assets/Game/Runtime/Presentation/CombatVisualFeedback.cs',
 'UnityProject/Assets/Game/Runtime/Presentation/EnemyUnitArtCandidates.cs',
 m['unity_import']['asset_path'],rel(meta),rel(manifest),
 'ArtSource/breach_ram_triangle_20261016/round2_source.png',
 'ArtSource/breach_ram_triangle_20261016/round2_qa.json']
report['changed_files']=files
for f in files:
 if not any(i['path']==f for i in report['inputs']):report['inputs'].append({'path':f,'sha256':sha(root/f)})
report['local_evidence']=[{'path':rel(p),'sha256':sha(p)} for p in [folder/'runtime/ram.png',folder/'runtime/ram_960.png',folder/'runtime/capture.json',folder/'runtime/cleanup.json',folder/'tests_initial.xml',folder/'tests_candidate.xml']]
report['blockers']=['Candidate only; no full scene pixel-density/human/performance baseline approval. User-selected old concept identity does not approve the new delivery image.']
(folder/'quality_report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),'utf-8')
print('runtime binding and importer evidence recorded; QA_PENDING retained')
