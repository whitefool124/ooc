from pathlib import Path
import shutil,json,xml.etree.ElementTree as ET
from html import escape
root=Path(__file__).resolve().parents[3];f=Path(__file__).resolve().parent
files=[p.relative_to(root/'Worldbuilding/归档/2026-10-07_顶部HUD压缩/before') for p in (root/'Worldbuilding/归档/2026-10-07_顶部HUD压缩/before').rglob('*') if p.is_file()]
files += [Path('UnityProject/Assets/Game/Runtime/Presentation/CombatHudPortraitAssets.cs')]
for rel in files:
    for suffix in ['', '.meta']:
        src=root/(str(rel)+suffix)
        if src.exists():
            dst=f/'before'/(str(rel)+suffix);dst.parent.mkdir(parents=True,exist_ok=True)
            if not dst.exists():shutil.copy2(src,dst)
d=json.loads((f/'feishu-before.json').read_text(encoding='utf-8-sig'))
p=next(p for p in ET.fromstring(d['data']['document']['content']) if '战场参考视口' in ''.join(p.itertext()));old=''.join(p.itertext())
new=old.replace('正面480×224','正面480×160').replace('底部显示状态图标','姓名行显示最多两枚状态图标，更多状态以加号提示并悬停查看全部，无状态时不显示空态行').replace('高112','高88').replace('48×48原生头部窗口','48×36原生头部窗口')
(f/'layout.xml').write_text('<p>'+escape(new)+'</p>',encoding='utf-8');(f/'block-id.txt').write_text(p.attrib['id'],encoding='utf-8')
(f/'old.txt').write_text(old,encoding='utf-8');(f/'new.txt').write_text(new,encoding='utf-8')
print(p.attrib['id'])
