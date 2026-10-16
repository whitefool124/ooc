"""Audit existing asset contact metadata and a buffered actual transaction capture."""
from pathlib import Path
import csv, hashlib, json
import numpy as np
from PIL import Image

folder = Path(__file__).resolve().parent
repo = folder.parents[3]
art = repo / 'UnityProject/Assets/Game/Resources/Art'
assets = ['FormalUIEmptyIllustrations/empty_archive_tray.png',
          'FormalAcademyStructures32/academy_map_table_2x1.png',
          'FormalArtifactIcons32/breach_wedge.png', 'FormalArtifactIcons32/phase_spindle.png',
          'FormalAcademyEquipmentIcons32/aca_eq_hd01.png']
records = []
for relative in assets:
    p = art / relative
    im = Image.open(p).convert('RGBA')
    records.append(dict(path=str(p.relative_to(repo)), sha256=hashlib.sha256(p.read_bytes()).hexdigest(),
                        size=list(im.size), alpha_bounds=list(im.getchannel('A').getbbox())))
assert [r['alpha_bounds'][3] for r in records[2:]] == [30, 30, 26]
(folder/'asset-reuse.json').write_text(json.dumps(records, ensure_ascii=False, indent=2), encoding='utf-8')

images = [Image.open(folder/f'trade-{i:02}.png').convert('RGB') for i in range(24)]
roi = (1664,66,1856,238)
frames = np.stack([np.asarray(im.crop(roi)) for im in images])
changed = np.any(frames != frames[-1], axis=(0,3))
allowed = np.zeros(changed.shape,dtype=bool)
allowed[35:66,62:130] = True
count, outside = int(changed.sum()), int((changed & ~allowed).sum())
assert count > 0 and outside == 0, (count, outside)
result = dict(roi=list(roi),frames=24,changed_pixels=count,changed_outside_coin_area=outside,
              note='Actual runtime frames; compare settled last frame. Fixed tray and table pixels unchanged. No acoustic proof.')
(folder/'coin-pixel-evidence.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
times = [float(line.split()[0]) for line in (folder/'trade-times.txt').read_text().splitlines()]
duration = [max(20,round((times[i+1]-times[i])*1000)) for i in range(23)]+[1200]
gif = [im.crop(roi).resize((576,516),Image.Resampling.NEAREST) for im in images]
gif[0].save(folder/'coin-runtime.gif',save_all=True,append_images=gif[1:],duration=duration,loop=0)

ledger = repo/'Worldbuilding/归档/2026-10-03_六十项小细节实装/执行状态.csv'
with ledger.open(encoding='utf-8-sig',newline='') as f:
    rows = list(csv.DictReader(f))
updates = {
    13: ('LOCAL_VERIFIED','第二批复用正式托盘，三个真实商品按不透明底部落位，不阻挡购买；新原料仍QA_PENDING'),
    14: ('LOCAL_VERIFIED','实际购买后对应商品和接触影移除，空托盘保留；重进保持售罄'),
    15: ('LOCAL_VERIFIED_VARIANT','以正式零钱托盘替代新碟素材，柜台承托；实际成交轻碰一次，禁用点击不响应'),
}
for row in rows:
    if int(row['编号']) in updates:
        row['状态'],row['说明'] = updates[int(row['编号'])]
assert len(rows) == 60
with ledger.open('w',encoding='utf-8-sig',newline='') as f:
    writer=csv.DictWriter(f,fieldnames=['编号','模式','状态','说明']);writer.writeheader();writer.writerows(rows)
print(json.dumps(result,ensure_ascii=False))
