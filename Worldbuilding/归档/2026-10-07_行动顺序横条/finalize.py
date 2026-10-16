from pathlib import Path
import json,difflib,shutil,xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[3];folder=Path(__file__).resolve().parent
p=root/'Tools/OCCArt/occ_art_contract_v1.json'
d=json.loads((folder/'before/Tools/OCCArt/occ_art_contract_v1.json').read_text(encoding='utf-8-sig'))
d['combat_hud_portrait_design_20261006']['order_height']=152
d['combat_hud_portrait_design_20261006']['order_style']='continuous shared strip; native48 head window; 96 reference pixels'
p.write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf-8')
fragment=json.loads((folder/'feishu-after.json').read_text(encoding='utf-8-sig'))['data']['document']['content']
assert '连续横向行动顺序条' in ''.join(ET.fromstring(fragment).itertext())
diff=[]
for old in (folder/'before').rglob('*'):
    if not old.is_file():continue
    rel=old.relative_to(folder/'before');new=root/rel
    if not new.exists():continue
    diff.extend(difflib.unified_diff(old.read_text(encoding='utf-8-sig').splitlines(True),new.read_text(encoding='utf-8-sig').splitlines(True),fromfile='before/'+rel.as_posix(),tofile=rel.as_posix()))
    for suffix in ['', '.meta']:
        current=root/(str(rel)+suffix)
        if current.exists():
            target=folder/'after'/(str(rel)+suffix);target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(current,target)
new='UnityProject/Assets/Game/Resources/UI/Prefabs/FormalCombatOrderStripItem.prefab'
diff.extend(difflib.unified_diff([],(root/new).read_text(encoding='utf-8-sig').splitlines(True),fromfile='/dev/null',tofile=new))
for suffix in ['', '.meta']:
    target=folder/'after'/(new+suffix);target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(root/(new+suffix),target)
(folder/'incremental.diff').write_text(''.join(diff),encoding='utf-8')
print('Synced strip contract and saved incremental archive')
