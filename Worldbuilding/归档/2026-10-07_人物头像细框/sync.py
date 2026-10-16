from pathlib import Path
import json,xml.etree.ElementTree as ET
from html import escape
root=Path(__file__).resolve().parents[3];f=Path(__file__).resolve().parent
d=json.loads((f/'feishu-before.json').read_text(encoding='utf-8-sig'));pairs=[]
for p in ET.fromstring(d['data']['document']['content']):
    old=''.join(p.itertext());new=old
    if '战场参考视口' in old:new=old.replace('独立头像框','炭墨细线头像框')
    if '战斗身份卡美术：人物卡与行动顺序采用' in old:
        new=old.replace('头像框主体采用独立原生32×32硬Alpha美术','左上头像使用2参考像素炭墨细线框，撤下厚重象牙黄铜装饰框；已生成装饰框主体为独立原生32×32硬Alpha美术').replace('头像框在1920与960分别4倍与2倍显示','备用装饰框在1920与960分别4倍与2倍显示')
    if new!=old:
        i=len(pairs);(f/f'block-{i}.txt').write_text(p.attrib['id'],encoding='utf-8');(f/f'update-{i}.xml').write_text('<p>'+escape(new)+'</p>',encoding='utf-8');pairs.append([old,new])
assert len(pairs)==2,len(pairs)
(f/'pairs.json').write_text(json.dumps(pairs,ensure_ascii=False,indent=2),encoding='utf-8')
print('Prepared 2 targeted cosmetic-spec updates')
