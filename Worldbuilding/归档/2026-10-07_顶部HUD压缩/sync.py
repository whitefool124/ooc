from pathlib import Path
import json,xml.etree.ElementTree as ET
from html import escape
root=Path(__file__).resolve().parents[3];f=Path(__file__).resolve().parent
d=json.loads((f/'feishu-before.json').read_text(encoding='utf-8-sig'))
p=next(p for p in ET.fromstring(d['data']['document']['content']) if '战场参考视口' in ''.join(p.itertext()));old=''.join(p.itertext())
new=old.replace('正面480×288','正面480×224').replace('三项资源在头像右侧对齐','生命与个人魔力两项资源在头像右侧对齐，生命旁以盾牌图标和当前护盾数值显示防护，不再使用独立盾条；护盾为零隐藏徽记，较大数值简写且悬停显示完整值').replace('高152','高112')
start=new.index('左侧标明行动顺序和从左向右的方向');end=new.index('根据单位数收短',start)
new=new[:start]+'左侧只显示方向箭头，条内嵌入最多七名单位的立绘头像，不使用标题、当前文字或顺位号，不使用独立装饰卡框，不用战棋形象；当前单位以独立顶部标记和槽底突出，预览变化使用上／下箭头。缺少头像显示问号符号，姓名仅在悬停提示中显示。'+new[end:]
new=new.replace('末端显示剩余数量','末端显示省略符号').replace('资源缺失显示姓名占位。','').replace('顺位及预览箭头','预览箭头')
(f/'layout.xml').write_text('<p>'+escape(new)+'</p>',encoding='utf-8');(f/'block-id.txt').write_text(p.attrib['id'],encoding='utf-8')
(f/'old.txt').write_text(old,encoding='utf-8');(f/'new.txt').write_text(new,encoding='utf-8')
print(p.attrib['id'])
