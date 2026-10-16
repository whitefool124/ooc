from pathlib import Path
import json,hashlib,shutil
root=Path(__file__).resolve().parents[3];folder=Path(__file__).resolve().parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
manifest=root/'ArtSource/breach_ram_triangle_20261016/manifest.occ-art-manifest-v1.json'
backup=folder/'manifest_before.json'
if not backup.exists():shutil.copy2(manifest,backup)
m=json.loads(manifest.read_text('utf-8'));adoption=json.loads((folder/'adoption.json').read_text('utf-8'))
capture=json.loads((folder/'runtime/capture.json').read_text('utf-8'))
assert capture['candidateReviewEnabled'] is False and capture['resource']=='Art/FormalUnits64/breach_ram'
runtime_asset=root/'UnityProject'/adoption['target']
assert sha(runtime_asset)==m['delivery']['output_sha256']
m['unity_import'].update(asset_path=runtime_asset.relative_to(root).as_posix(),stable_guid=adoption['guid'],importer_verified=True,runtime_verified=True,review_scope='Default runtime registry; Editor candidate switch false; same PNG and stable GUID as user-approved image')
m['human_review'].update(overall='PASS',reviewer='user',date='2026-10-16',notes='User explicitly approved the exact shown delivery image: 就用这个吧 接入正式资源 然后继续下一步 看看还有什么待办. This approves source artwork/adoption, not a complete scene quality baseline.')
m['production_binding']={'status':'USER_ADOPTED','default_enabled':True,'resource':capture['resource'],'approved_delivery_sha256':sha(runtime_asset),'approval_quote':'就用这个吧 接入正式资源 然后继续下一步 看看还有什么待办','adoption_record':(folder/'adoption.json').relative_to(root).as_posix()}
# Deployment approval is concrete; complete scene-quality promotion remains independent.
m['status']='QA_PENDING'
m['application']['review_scope']='User-adopted artwork in default FIRST-X runtime lookup; scene density/performance/motion quality approval remains separate'
m['evidence']['application_contact']=(folder/'runtime/ram.png').relative_to(root).as_posix()
manifest.write_text(json.dumps(m,ensure_ascii=False,indent=2),'utf-8')
report=json.loads((folder/'quality_report.json').read_text('utf-8'))
files=['UnityProject/Assets/Game/Runtime/Combat/FormalArtRegistry.cs','UnityProject/Assets/Game/Runtime/Combat/EnemyArchetypes.cs',
 'UnityProject/Assets/Game/Runtime/Presentation/EnemyUnitArtCandidates.cs','UnityProject/Assets/Game/Editor/FormalArtImportPostprocessor.cs',
 runtime_asset.relative_to(root).as_posix(),runtime_asset.with_suffix('.png.meta').relative_to(root).as_posix(),manifest.relative_to(root).as_posix()]
report['changed_files']=files
for f in files:
 if not any(item['path']==f for item in report['inputs']):report['inputs'].append({'path':f,'sha256':sha(root/f)})
report['local_evidence']=[{'path':p.relative_to(root).as_posix(),'sha256':sha(p)} for p in [folder/'adoption.json',folder/'runtime/capture.json',folder/'runtime/cleanup.json',folder/'runtime/ram.png',folder/'runtime/ram_960.png']]
report['blockers']=['Source artwork and default runtime adoption approved by user; complete scene-density, motion and performance baseline evidence remains pending.']
(folder/'quality_report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),'utf-8')
print('Same PNG and GUID adopted in default runtime; user approval recorded; no complete scene-quality approval invented')
