from pathlib import Path
import shutil,difflib,json,xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[3];f=Path(__file__).resolve().parent
d=json.loads((f/'feishu-after.json').read_text(encoding='utf-8-sig'));text=''.join(ET.fromstring(d['data']['document']['content']).itertext());assert '正面480×160' in text and '高96' in text and '48×40原生头部窗口' in text
diff=[]
for old in (f/'before').rglob('*'):
    if not old.is_file():continue
    rel=old.relative_to(f/'before');new=root/rel
    if not new.exists():continue
    dst=f/'after'/rel;dst.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(new,dst)
    if rel.suffix!='.meta':diff.extend(difflib.unified_diff(old.read_text(encoding='utf-8-sig').splitlines(True),new.read_text(encoding='utf-8-sig').splitlines(True),fromfile='before/'+rel.as_posix(),tofile=rel.as_posix()))
(f/'incremental.diff').write_text(''.join(diff),encoding='utf-8')
print('Feishu readback verified; incremental archive saved')
