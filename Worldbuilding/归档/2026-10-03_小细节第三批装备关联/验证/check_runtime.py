"""Create traceable previews from actual frames, then update the approved ledger."""
from pathlib import Path
import csv,json
import numpy as np
from PIL import Image

folder=Path(__file__).resolve().parent
repo=folder.parents[3]
evidence={}
for prefix in ['open','close','link']:
    images=[Image.open(folder/f'{prefix}-{i:02}.png').convert('RGB') for i in range(20)]
    times=[float(line.split()[0]) for line in (folder/f'{prefix}-times.txt').read_text().splitlines()]
    durations=[max(20,round((times[i+1]-times[i])*1000)) for i in range(19)]+[1200]
    if prefix=='link':
        crop=(530,260,1460,930)
        previews=[im.crop(crop) for im in images]
    else:
        crop=(904,522,928,540)
        previews=[im.crop((884,504,944,550)).resize((360,276),Image.Resampling.NEAREST) for im in images]
    data=np.stack([np.asarray(im.crop(crop)) for im in images])
    changed=int(np.any(data!=data[-1],axis=(0,3)).sum())
    assert changed>0,(prefix,changed)
    evidence[prefix]=dict(frames=20,crop=list(crop),changed_pixels_in_crop=changed,
                         note='Actual buffered screenshots versus settled last frame; ROI change proves visible motion, not all changes in the full UI belong to this effect.')
    previews[0].save(folder/f'{prefix}-runtime.gif',save_all=True,append_images=previews[1:],duration=durations,loop=0)
(folder/'pixel-evidence.json').write_text(json.dumps(evidence,ensure_ascii=False,indent=2),encoding='utf-8')
ledger=repo/'Worldbuilding/归档/2026-10-03_六十项小细节实装/执行状态.csv'
with ledger.open(encoding='utf-8-sig',newline='') as f:rows=list(csv.DictReader(f))
updates={9:('LOCAL_VERIFIED','第三批真实胸甲卸下／装备开合，非法／取消不播；动画0静态正确，重进无重播'),
         12:('LOCAL_VERIFIED','第三批实际道具关联脉冲，物件仍在包中；重复关联无重播，动画0与退出清理通过')}
for row in rows:
    if int(row['编号']) in updates:row['状态'],row['说明']=updates[int(row['编号'])]
assert len(rows)==60
with ledger.open('w',encoding='utf-8-sig',newline='') as f:
    writer=csv.DictWriter(f,fieldnames=['编号','模式','状态','说明']);writer.writeheader();writer.writerows(rows)
print(json.dumps(evidence,ensure_ascii=False))
