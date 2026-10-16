from pathlib import Path
import json
from PIL import Image
ROOT=Path(__file__).resolve().parents[3];OUT=Path(__file__).resolve().parent
inventory=json.loads((ROOT/'Worldbuilding/归档/2026-10-12_OCC画面提升盘点/material_coverage.json').read_text(encoding='utf-8'))
rows=[]
for group,assets in inventory['groups'].items():
 for a in assets:
  ref=ROOT/a['source_candidates'][0]['path'];im=Image.open(ref)
  target=OUT/(a['id']+'_reference16x.png')
  if not target.exists():im.resize((im.width*16,im.height*16),Image.Resampling.NEAREST).save(target)
  rows.append(dict(id=a['id'],group=group,source=a['source_candidates'][0],reference=target.relative_to(ROOT).as_posix(),bbox=im.getchannel('A').getbbox(),dimensions=im.size))
(OUT/'reference_inventory.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(references=len(rows),dimensions=sorted(set(tuple(x['dimensions']) for x in rows)))))
