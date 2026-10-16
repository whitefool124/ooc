from pathlib import Path
import json,shutil,difflib,xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[3];f=Path(__file__).resolve().parent
d=json.loads((f/'feishu-after.json').read_text(encoding='utf-8-sig'));texts=[''.join(p.itertext()) for p in ET.fromstring(d['data']['document']['content'])]
assert any('2参考像素炭墨细线框' in t for t in texts)
p=root/'Worldbuilding/策划案/OCC_项目总策划案_v1.0.md';s=p.read_text(encoding='utf-8-sig')
lines=s.splitlines()
for i,line in enumerate(lines):
    if line.startswith('战场参考视口'):lines[i]=next(t for t in texts if t.startswith('战场参考视口'))
    if line.startswith('人物卡与行动顺序采用') or line.startswith('战斗身份卡美术：人物卡与行动顺序采用'):lines[i]=next(t for t in texts if t.startswith('战斗身份卡美术：人物卡与行动顺序采用'))
p.write_text('\n'.join(lines)+'\n',encoding='utf-8')
p=root/'Tools/OCCArt/occ_art_contract_v1.json';d=json.loads(p.read_text(encoding='utf-8-sig'));d['combat_hud_portrait_design_20261006']['hero_frame']='2 reference pixel charcoal outline; generated ornamental frame inactive';p.write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf-8')
p=root/'Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv';s=p.read_text(encoding='utf-8-sig');lines=s.splitlines();lines=[line.replace('人物正面480×160背面480×280','人物正面480×160背面480×280；头像2参考像素炭墨细框') if line.startswith('ART-COMBAT-STATUS,') and '头像2参考' not in line else line for line in lines];p.write_text('\n'.join(lines)+'\n',encoding='utf-8')
diff=[]
for old in (f/'before').rglob('*'):
    if not old.is_file():continue
    rel=old.relative_to(f/'before');src=root/rel
    dst=f/'after'/rel;dst.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(src,dst)
    if rel.suffix!='.meta':diff.extend(difflib.unified_diff(old.read_text(encoding='utf-8-sig').splitlines(True),src.read_text(encoding='utf-8-sig').splitlines(True),fromfile='before/'+rel.as_posix(),tofile=rel.as_posix()))
(f/'incremental.diff').write_text(''.join(diff),encoding='utf-8')
print('Thin-border specs synced; archive saved')
