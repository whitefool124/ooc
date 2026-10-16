from pathlib import Path
import json, hashlib, difflib, shutil, xml.etree.ElementTree as ET

folder=Path(__file__).resolve().parent
root=folder.parents[2]
art=root/'ArtSource/combat_hud_20261006'
guids={'hero':'9ef7352daaaac9c4dac672c3d202b9ff','tether_hound':'73c6047f2a55e384591db3870db2dd51','frame':'e1d75989f7691624287e9e8969d1ab7d'}
for name,guid in guids.items():
    p=art/name/'manifest.occ-art-manifest-v1.json'
    d=json.loads(p.read_text(encoding='utf-8'))
    unity='UnityProject/Assets/Game/Resources/Art/'+('CombatHudChrome/portrait_frame.png' if name=='frame' else 'CombatHudPortraits64/'+name+'.png')
    assert hashlib.sha256((root/unity).read_bytes()).hexdigest()==d['delivery']['output_sha256']
    d['unity_import']={'asset_path':unity,'stable_guid':guid,'filter_mode':'Point','wrap_mode':'Clamp','mipmaps':False,'compression':'Uncompressed','pixels_per_unit':32,'alpha_is_transparency':True,'readable':True,'importer_verified':True,'runtime_verified':True,'verification_scope':'Normal B1 entry, 1920x1080 and 960x540; exact gameplay flow evidence in archive; not a full cast or full-mode certification'}
    d['evidence']['actual_application_1920']='Worldbuilding/归档/2026-10-06_战斗HUD正式美术/verification/collapsed-1920x1080.png'
    d['evidence']['actual_application_960']='Worldbuilding/归档/2026-10-06_战斗HUD正式美术/verification/collapsed-960x540.png'
    if name=='frame':d['application']['runtime_draw_rect']='136x136 reference canvas; 128x128 native-subject footprint plus transparent padding'
    p.write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf-8')
changes=[]
changed_paths=[]
for before in sorted((folder/'before').rglob('*')):
    if not before.is_file():continue
    rel=before.relative_to(folder/'before')
    if len(rel.parts)==1 and rel.suffix=='.cs':
        parent='Runtime/Presentation' if rel.name=='CombatVisualFeedback.cs' else 'Editor'
        rel=Path('UnityProject/Assets/Game')/parent/rel
    current=root/rel
    if current.exists():
        changed_paths.append(rel.as_posix())
        old=before.read_text(encoding='utf-8-sig').splitlines(True)
        new=current.read_text(encoding='utf-8-sig').splitlines(True)
        changes.extend(difflib.unified_diff(old,new,fromfile='before/'+rel.as_posix(),tofile=rel.as_posix()))
new_paths=['UnityProject/Assets/Game/Runtime/Presentation/CombatPortraitCardView.cs','UnityProject/Assets/Game/Runtime/Presentation/CombatHudPortraitAssets.cs','UnityProject/Assets/Game/Editor/CombatPortraitHudAuthoring.cs','UnityProject/Assets/Game/Resources/UI/Prefabs/FormalCombatPortraitCard.prefab']
for rel in new_paths:
    p=root/rel
    changes.extend(difflib.unified_diff([],p.read_text(encoding='utf-8-sig').splitlines(True),fromfile='/dev/null',tofile=rel))
(folder/'incremental.diff').write_text(''.join(changes),encoding='utf-8')
(folder/'new-files.json').write_text(json.dumps(new_paths+['UnityProject/Assets/Game/Resources/Art/CombatHudPortraits64/hero.png','UnityProject/Assets/Game/Resources/Art/CombatHudPortraits64/tether_hound.png','UnityProject/Assets/Game/Resources/Art/CombatHudChrome/portrait_frame.png'],ensure_ascii=False,indent=2),encoding='utf-8')
for rel in changed_paths+json.loads((folder/'new-files.json').read_text(encoding='utf-8')):
    for suffix in ['', '.meta']:
        p=root/(rel+suffix)
        if p.exists():
            target=folder/'after'/(rel+suffix)
            target.parent.mkdir(parents=True,exist_ok=True)
            shutil.copy2(p,target)
evidence={}
for p in sorted(art.rglob('raw/*.png')):evidence[p.relative_to(root).as_posix()]=hashlib.sha256(p.read_bytes()).hexdigest()
(folder/'source-sha256.json').write_text(json.dumps(evidence,ensure_ascii=False,indent=2),encoding='utf-8')
data=json.loads((folder/'feishu-final.json').read_text(encoding='utf-8-sig'))
text=''.join(ET.fromstring(data['data']['document']['content']).itertext())
assert '（1432，288，416，436）' in text and '34×34' in text and '（512，80）' in text
print('Incremental archive, source hashes, importer and actual application evidence saved; human review remains pending.')
