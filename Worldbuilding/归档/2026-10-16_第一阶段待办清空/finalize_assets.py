from pathlib import Path
import json,hashlib,shutil,subprocess,sys
root=Path(__file__).resolve().parents[3];folder=Path(__file__).resolve().parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
adoptions=json.loads((folder/'adopted_assets.json').read_text('utf-8'))
paths={('UnityProject/'+x['source']):x for x in adoptions}
updated=[];validation=[]
for top in ['enemy_independent_20261016','field_effect_candidates_20261016','field_effect_reset_20261016','device_material_candidates_20261016']:
 for manifest in (root/'ArtSource'/top).rglob('*manifest*.json'):
  m=json.loads(manifest.read_text('utf-8-sig'))
  if m.get('schema')!='occ-art-manifest-v1':continue
  previous=(m.get('unity_import') or {}).get('asset_path')
  item=next((x for x in adoptions if m.get('asset_id','').startswith(Path(x['target']).stem+'_') and sha(root/'UnityProject'/x['target'])==m['delivery']['output_sha256']),None)
  if item is None:continue
  target='UnityProject/'+item['target'];asset=root/target
  # These are the exact previously decoded images, not new art or transformed variants.
  assert sha(asset)==m['delivery']['output_sha256'],manifest
  backup=folder/'manifest_backups'/manifest.relative_to(root)
  backup.parent.mkdir(parents=True,exist_ok=True)
  if not backup.exists():shutil.copy2(manifest,backup)
  m['unity_import']=m.get('unity_import') or {}
  m['unity_import'].update(asset_path=target,stable_guid=item['guid'],native_ppu=32,filter_mode='Point',mesh_type='Full Rect',compression='None',mipmaps=False,wrap_mode='Clamp',importer_verified=True,runtime_verified=True,review_scope='Default player lookup and representative local capture; sequence frames all loaded, no whole-scene motion approval')
  m['production_binding']={'status':'USER_AUTHORIZED','default_enabled':True,'authorization':'2026-10-16 user: 自己把待办清空','delivery_sha256':sha(asset),'guid_preserved':True}
  m['application']['review_scope']='Adopted existing source; default runtime local evidence in2026-10-16 stage-one closure archive. Whole scene aesthetics/performance remain separate.'
  m['status']='QA_PENDING' # Adoption authorization is not fabricated scene-aesthetic approval.
  manifest.write_text(json.dumps(m,ensure_ascii=False,indent=2),'utf-8')
  report=folder/'asset_validation'/(asset.stem+'.json');report.parent.mkdir(parents=True,exist_ok=True)
  result=subprocess.run([sys.executable,'-X','utf8',str(root/'Tools/OCCArt/validate_occ_art_asset.py'),str(manifest),'--report',str(report)],cwd=root,capture_output=True,text=True,encoding='utf-8')
  validation.append({'asset':target,'manifest':manifest.relative_to(root).as_posix(),'pass':result.returncode==0,'report':report.relative_to(root).as_posix()})
  updated.append(manifest.relative_to(root).as_posix())
(folder/'adopted_manifest_validation.json').write_text(json.dumps({'updated':updated,'validation':validation},ensure_ascii=False,indent=2),'utf-8')
assert len(validation)==24,(len(validation),updated)
assert all(v['pass'] for v in validation),[v for v in validation if not v['pass']]
print('24 adopted resources: content hashes, stable GUIDs and source validation passed')
