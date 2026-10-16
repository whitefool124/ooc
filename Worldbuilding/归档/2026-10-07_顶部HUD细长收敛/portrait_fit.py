from pathlib import Path
import json,xml.etree.ElementTree as ET
from html import escape
root=Path(__file__).resolve().parents[3];f=Path(__file__).resolve().parent
for rel in ['Worldbuilding/策划案/OCC_项目总策划案_v1.0.md','Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv','Worldbuilding/归档/2026-10-07_顶部HUD细长收敛/new.txt']:
    p=root/rel;s=p.read_text(encoding='utf-8-sig').replace('高88','高96').replace('48×36原生','48×40原生');p.write_text(s,encoding='utf-8')
p=root/'Tools/OCCArt/occ_art_contract_v1.json';d=json.loads(p.read_text(encoding='utf-8-sig'));d['combat_hud_portrait_design_20261006']['order_height']=96;d['combat_hud_portrait_design_20261006']['order_style']='continuous icon-only shared strip; native48x40 head window; 96x80 reference pixels; information in hover';p.write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf-8')
d=json.loads((f/'feishu-after.json').read_text(encoding='utf-8-sig'));p=next(p for p in ET.fromstring(d['data']['document']['content']) if '战场参考视口' in ''.join(p.itertext()));(f/'block-id.txt').write_text(p.attrib['id'],encoding='utf-8');(f/'layout.xml').write_text('<p>'+escape((f/'new.txt').read_text(encoding='utf-8'))+'</p>',encoding='utf-8')
