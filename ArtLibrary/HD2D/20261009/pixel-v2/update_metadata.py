import json
import re
from pathlib import Path

HERE=Path(__file__).resolve().parent
items=json.loads((HERE/'catalog.json').read_text(encoding='utf-8'))
html=(HERE/'index.html').read_text(encoding='utf-8')
for x in items:
    pattern="(id:'"+x['id']+"',name:'[^']*',w:)\\d+(,h:)\\d+"
    html=re.sub(pattern,lambda m:m[1]+str(x['width'])+m[2]+str(x['height']),html)
html=html.replace('${x.id}/source-01.png',"${x.id}/${x.id==='dossier'?'source-03.png':'source-01.png'}")
(HERE/'index.html').write_text(html,encoding='utf-8')
readme=(HERE/'README.md').read_text(encoding='utf-8')
readme=readme.replace('新档案册首轮因书页透明错误淘汰，第二轮为当前母版。','新档案册前两轮因书页 Alpha 错误淘汰；第二轮仅作其余物件的像素语言参考。当前档案册是第三轮独立生图，用专用洋红底解决纸面透明错误；处理仅去除洋红底 Alpha，物件颜色没有改写。')
(HERE/'README.md').write_text(readme,encoding='utf-8')
areas={'dossier':[[.105,.13,.34,.70],[.565,.13,.335,.70]],'notes':[[.12,.22,.74,.60]],'plaque':[[.16,.39,.68,.44]],'price_tag':[[.43,.22,.50,.52]],'service_label':[[.17,.24,.66,.40]]}
for x in items:
    x['writing_areas_normalized_xywh']=areas.get(x['id'],[])
(HERE/'catalog.json').write_text(json.dumps(items,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Current dossier source and writing areas recorded.')
