from pathlib import Path
import json,difflib,shutil,xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[3];folder=Path(__file__).resolve().parent
p=root/'Tools/OCCArt/occ_art_contract_v1.json';d=json.loads((folder/'before/Tools/OCCArt/occ_art_contract_v1.json').read_text(encoding='utf-8-sig'))
d['combat_hud_portrait_design_20261006']['hero']=[16,80,480,224]
d['combat_hud_portrait_design_20261006']['order_height']=112
d['combat_hud_portrait_design_20261006']['order_style']='continuous icon-only shared strip; native48 head window; 96 reference pixels; information in hover'
d['combat_hud_portrait_design_20261006']['shield_display']='resource12 shield icon at24 reference plus current value; zero hidden; full value in hover; no shield track'
p.write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf-8')
fragment=json.loads((folder/'feishu-after.json').read_text(encoding='utf-8-sig'))['data']['document']['content'];text=''.join(ET.fromstring(fragment).itertext())
assert '正面480×224' in text and '高112' in text and '不再使用独立盾条' in text
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
(folder/'incremental.diff').write_text(''.join(diff),encoding='utf-8')
print('Compact top HUD contract synced and incremental archive saved')
