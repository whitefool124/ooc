from pathlib import Path
import json,xml.etree.ElementTree as ET
from html import escape
f=Path(__file__).resolve().parent;d=json.loads((f/'feishu-after.json').read_text(encoding='utf-8-sig'));count=0
for p in ET.fromstring(d['data']['document']['content']):
    old=''.join(p.itertext());new=old.replace('（16，20，32，32）','（14，16，36，36）').replace('左上32×32近景窗口','左上36×36近景窗口')
    if new!=old:
        (f/f'fit-{count}.txt').write_text(p.attrib['id'],encoding='utf-8');(f/f'fit-{count}.xml').write_text('<p>'+escape(new)+'</p>',encoding='utf-8');count+=1
assert count==2
p=f/'finalize.py';s=p.read_text(encoding='utf-8').replace('（16，20，32，32）','（14，16，36，36）').replace('[16,20,32,32]','[14,16,36,36]').replace('左上32×32原生近景窗口显示128×128','左上36×36原生近景窗口显示144×144');p.write_text(s,encoding='utf-8')
